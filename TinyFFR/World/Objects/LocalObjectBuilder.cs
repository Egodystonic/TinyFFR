// Created on 2024-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed unsafe class LocalObjectBuilder : IObjectBuilder, IModelInstanceImplProvider, IResourceDirectory<ModelInstance>, IDisposable {
	readonly record struct ActiveModelInstanceEffectsData(Material PerInstanceEffectMaterialCopy);

	// Present in _activeInstanceMutationData only after a ModelInstance's first UpdateVertices call. Absent = instance still shares the mesh's GPU vertex buffer.
	// LocalAabb is non-null only after a recalculateBoundingBox: true update; before that, Filament's AABB reflects the mesh's initial value.
	readonly record struct ActiveModelInstanceMutationData(UIntPtr PrivateVertexBufferHandle, PooledHeapMemory<MeshVertex> CpuShadow, PositionedCuboid? LocalAabb);

	const string DefaultModelInstanceName = "Unnamed Model Instance";
	readonly LocalFactoryGlobalObjectGroup _globals;
	// Because instance transforms are set so frequently, they're kept in their own separate map for performance
	readonly ArrayPoolBackedMap<ResourceHandle<ModelInstance>, Transform> _activeInstanceTransforms = new();
	readonly ArrayPoolBackedMap<ResourceHandle<ModelInstance>, ActiveModelInstanceEffectsData> _activeInstanceEffectsData = new();
	readonly ArrayPoolBackedMap<ResourceHandle<ModelInstance>, ActiveModelInstanceMutationData> _activeInstanceMutationData = new();
	bool _isDisposed = false;

	public LocalObjectBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	public ModelInstance CreateModelInstance(Mesh mesh, Material material, in ModelInstanceCreationConfig config) {
		ThrowIfThisIsDisposed();
		var meshBufferData = mesh.BufferData;
		var aabb = mesh.BoundingBox;

		AllocateModelInstance(
			config.InitialTransform.ToMatrix(),
			meshBufferData.VertexBufferHandle,
			meshBufferData.IndexBufferHandle,
			meshBufferData.IndexBufferStartIndex,
			meshBufferData.IndexBufferCount,
			meshBufferData.BoneCount,
			material.Handle,
			aabb.Position.ToVector3(),
			new Vector3(aabb.HalfWidth, aabb.HalfHeight, aabb.HalfDepth),
			out var handle
		).ThrowIfFailure();

		var result = HandleToInstance(handle);
		_activeInstanceTransforms.Add(handle, config.InitialTransform);
		_globals.StoreResourceNameOrDefaultIfEmpty(new ResourceHandle<ModelInstance>(handle).Ident, config.Name, DefaultModelInstanceName);
		_globals.DependencyTracker.RegisterDependency(result, mesh);
		_globals.DependencyTracker.RegisterDependency(result, material);

		if (meshBufferData.BoneCount > 0) mesh.ApplySkeletalBindPose(result);
		return result;
	}

	public ModelInstanceGroup CreateModelInstances(ReadOnlySpan<Model> models, in ModelInstanceCreationConfig config) {
		ThrowIfThisIsDisposed();
		var resourceGroup = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: models.Length > 0 ? models.Length : 1, name: config.Name);
		for (var i = 0; i < models.Length; ++i) {
			resourceGroup.Add(CreateModelInstance(models[i].Mesh, models[i].Material, in config));
		}
		resourceGroup.Seal();
		return new ModelInstanceGroup(resourceGroup);
	}
	public ModelInstanceGroup CreateModelInstances<TModelList>(TModelList models, in ModelInstanceCreationConfig config) where TModelList : IReadOnlyList<Model> {
		ThrowIfThisIsDisposed();
		var resourceGroup = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: models.Count > 0 ? models.Count : 1, name: config.Name);
		for (var i = 0; i < models.Count; ++i) {
			resourceGroup.Add(CreateModelInstance(models[i].Mesh, models[i].Material, in config));
		}
		resourceGroup.Seal();
		return new ModelInstanceGroup(resourceGroup);
	}
	public ModelInstanceGroup GroupModelInstances(ReadOnlySpan<ModelInstance> instances, bool disposingGroupDisposesInstances, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();
		var resourceGroup = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: disposingGroupDisposesInstances, initialCapacity: instances.Length > 0 ? instances.Length : 1, name: name);
		for (var i = 0; i < instances.Length; ++i) {
			resourceGroup.Add(instances[i]);
		}
		resourceGroup.Seal();
		return new ModelInstanceGroup(resourceGroup);
	}
	public ModelInstanceGroup GroupModelInstances<TInstanceList>(TInstanceList instances, bool disposingGroupDisposesInstances, ReadOnlySpan<char> name = default) where TInstanceList : IReadOnlyList<ModelInstance> {
		ThrowIfThisIsDisposed();
		var resourceGroup = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: disposingGroupDisposesInstances, initialCapacity: instances.Count > 0 ? instances.Count : 1, name: name);
		for (var i = 0; i < instances.Count; ++i) {
			resourceGroup.Add(instances[i]);
		}
		resourceGroup.Seal();
		return new ModelInstanceGroup(resourceGroup);
	}

	void UpdateTransformAndMatrix(ResourceHandle<ModelInstance> handle, Transform newTransform) {
		SetModelInstanceWorldMatrix(handle, newTransform.ToMatrix()).ThrowIfFailure();
		_activeInstanceTransforms[handle] = newTransform;
	}
	public Transform GetTransform(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceTransforms[handle];
	}
	public void SetTransform(ResourceHandle<ModelInstance> handle, Transform newTransform) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetModelInstanceWorldMatrix(handle, newTransform.ToMatrix()).ThrowIfFailure();
		_activeInstanceTransforms[handle] = newTransform;
	}

	public Location GetPosition(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceTransforms[handle].Translation.AsLocation();
	}
	public void SetPosition(ResourceHandle<ModelInstance> handle, Location newPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle] with { Translation = newPosition.AsVect() });
	}

	public Rotation GetRotation(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceTransforms[handle].Rotation;
	}
	public void SetRotation(ResourceHandle<ModelInstance> handle, Rotation newRotation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle] with { Rotation = newRotation });
	}

	public Vect GetScaling(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceTransforms[handle].Scaling;
	}
	public void SetScaling(ResourceHandle<ModelInstance> handle, Vect newScaling) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle] with { Scaling = newScaling });
	}

	public void TranslateBy(ResourceHandle<ModelInstance> handle, Vect translation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle].WithAdditionalTranslation(translation));
	}
	public void RotateBy(ResourceHandle<ModelInstance> handle, Rotation rotation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle].WithAdditionalRotation(rotation));
	}
	public void RotateBy(ResourceHandle<ModelInstance> handle, Rotation rotation, Location pivotPoint) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var curTransform = _activeInstanceTransforms[handle];
		var curLoc = curTransform.Translation.AsLocation();
		var pivotToCurLoc = pivotPoint >> curLoc;
		var newLoc = pivotPoint + (pivotToCurLoc * rotation); 
		UpdateTransformAndMatrix(handle, (curTransform with { Translation = newLoc.AsVect() }).WithAdditionalRotation(rotation));
	}
	public void ScaleBy(ResourceHandle<ModelInstance> handle, float scalar) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle].WithScalingMultipliedBy(scalar));
	}
	public void ScaleBy(ResourceHandle<ModelInstance> handle, Vect vect) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle].WithScalingMultipliedBy(vect));
	}
	public void AdjustScaleBy(ResourceHandle<ModelInstance> handle, float scalar) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle].WithScalingAdjustedBy(scalar));
	}
	public void AdjustScaleBy(ResourceHandle<ModelInstance> handle, Vect vect) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle].WithScalingAdjustedBy(vect));
	}

	public Mesh GetMesh(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.DependencyTracker.GetNthTargetOfGivenType<ModelInstance, Mesh, IMeshImplProvider>(HandleToInstance(handle), 0);
	}
	public void SetMesh(ResourceHandle<ModelInstance> handle, Mesh newMesh) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var meshBufferData = newMesh.BufferData;
		var aabb = newMesh.BoundingBox;
		SetModelInstanceMesh(
			handle,
			meshBufferData.VertexBufferHandle,
			meshBufferData.IndexBufferHandle,
			meshBufferData.IndexBufferStartIndex,
			meshBufferData.IndexBufferCount
		).ThrowIfFailure();
		SetModelInstanceAabb(
			handle,
			aabb.Position.ToVector3(),
			new Vector3(aabb.HalfWidth, aabb.HalfHeight, aabb.HalfDepth)
		).ThrowIfFailure();
		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), GetMesh(handle));
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), newMesh);
	}

	public void UpdateVertices(ResourceHandle<ModelInstance> handle, int startIndex, ReadOnlySpan<MeshVertex> newVertices, bool recalculateBoundingBox) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var mesh = GetMesh(handle);
		var meshImpl = mesh.Implementation;
		var meshHandle = mesh.Handle;

		if (!meshImpl.GetAllowsPerInstanceVertexMutation(meshHandle)) {
			throw new InvalidOperationException(
				$"Source mesh was not created with {nameof(MeshCreationConfig.AllowsPerInstanceVertexMutation)} = true.");
		}
		if (meshImpl.GetBufferData(meshHandle).BoneCount > 0) {
			throw new InvalidOperationException(
				"Skeletal vertex mutation is not supported in this version of TinyFFR.");
		}

		var vertexCount = meshImpl.GetVertexCount(meshHandle);
		if (startIndex < 0) {
			throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, "Start index must be non-negative.");
		}
		if ((long) startIndex + newVertices.Length > vertexCount) {
			throw new ArgumentOutOfRangeException(nameof(newVertices),
				$"Update range [{startIndex}, {startIndex + newVertices.Length}) exceeds mesh vertex count {vertexCount}.");
		}
		if (newVertices.Length == 0) return;

		// Lazy privatization on first mutation: clone mesh's CPU shadow into a private VB + CPU shadow owned by this instance.
		if (!_activeInstanceMutationData.TryGetValue(handle, out var mutationData)) {
			var meshShadow = meshImpl.GetDefaultVerticesIfMutable(meshHandle);
			var instanceShadow = _globals.HeapPool.BorrowAndCopy(meshShadow);
			var seedStaging = _globals.CreateGpuHoldingBufferAndCopyData<MeshVertex>(instanceShadow.Buffer);
			AllocateVertexBuffer(seedStaging.BufferIdentity, (MeshVertex*) seedStaging.DataPtr, vertexCount, out var privateVbHandle).ThrowIfFailure();

			var meshBuffer = meshImpl.GetBufferData(meshHandle);
			SetModelInstanceMesh(
				handle,
				privateVbHandle,
				meshBuffer.IndexBufferHandle,
				meshBuffer.IndexBufferStartIndex,
				meshBuffer.IndexBufferCount
			).ThrowIfFailure();

			mutationData = new ActiveModelInstanceMutationData(privateVbHandle, instanceShadow, LocalAabb: null);
			_activeInstanceMutationData[handle] = mutationData;
		}

		// Apply update to instance CPU shadow and push the region to its private GPU VB.
		newVertices.CopyTo(mutationData.CpuShadow.Buffer.Slice(startIndex, newVertices.Length));
		var updateStaging = _globals.CreateGpuHoldingBufferAndCopyData(newVertices);
		UpdateVertexBuffer(
			mutationData.PrivateVertexBufferHandle,
			updateStaging.BufferIdentity,
			(MeshVertex*) updateStaging.DataPtr,
			newVertices.Length,
			startIndex
		).ThrowIfFailure();

		if (recalculateBoundingBox) {
			var newAabb = CalculateBoundingBoxFromShadow(mutationData.CpuShadow.Buffer);
			_activeInstanceMutationData[handle] = mutationData with { LocalAabb = newAabb };
			SetModelInstanceAabb(
				handle,
				newAabb.Position.ToVector3(),
				new Vector3(newAabb.HalfWidth, newAabb.HalfHeight, newAabb.HalfDepth)
			).ThrowIfFailure();
		}
	}

	static PositionedCuboid CalculateBoundingBoxFromShadow(ReadOnlySpan<MeshVertex> vertices) {
		if (vertices.Length == 0) return PositionedCuboid.UnitCubeAtOrigin;

		var (minX, minY, minZ) = vertices[0].Location;
		var (maxX, maxY, maxZ) = vertices[0].Location;
		for (var i = 1; i < vertices.Length; ++i) {
			var loc = vertices[i].Location;
			if (loc.X < minX) minX = loc.X;
			if (loc.Y < minY) minY = loc.Y;
			if (loc.Z < minZ) minZ = loc.Z;
			if (loc.X > maxX) maxX = loc.X;
			if (loc.Y > maxY) maxY = loc.Y;
			if (loc.Z > maxZ) maxZ = loc.Z;
		}

		return new PositionedCuboid(
			maxX - minX,
			maxY - minY,
			maxZ - minZ,
			new Vect(minX + maxX, minY + maxY, minZ + maxZ).ScaledBy(0.5f).AsLocation()
		);
	}

	public Material GetMaterial(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.DependencyTracker.GetTargetsOfGivenType<ModelInstance, Material, IMaterialImplProvider>(HandleToInstance(handle))[0];
	}
	public void SetMaterial(ResourceHandle<ModelInstance> handle, Material newMaterial) {
		ThrowIfThisOrHandleIsDisposed(handle);

		SetModelInstanceMaterial(
			handle,
			newMaterial.Handle
		).ThrowIfFailure();
		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), GetMaterial(handle));
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), newMaterial);

		DisposeEffectMaterialCopyIfPresent(handle);
	}

	public void SetMaterialEffectTransform(ResourceHandle<ModelInstance> handle, Transform2D newTransform) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var effectsMaterialInstance = GetOrCreateEffectMaterialCopy(handle);
		effectsMaterialInstance?.SetEffectTransform(newTransform);
	}
	public void SetMaterialEffectBlendTexture(ResourceHandle<ModelInstance> handle, MaterialEffectMapType mapType, Texture mapTexture) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var effectsMaterialInstance = GetOrCreateEffectMaterialCopy(handle);
		effectsMaterialInstance?.SetEffectBlendTexture(mapType, mapTexture);
	}
	public void SetMaterialEffectBlendDistance(ResourceHandle<ModelInstance> handle, MaterialEffectMapType mapType, float distance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var effectsMaterialInstance = GetOrCreateEffectMaterialCopy(handle);
		effectsMaterialInstance?.SetEffectBlendDistance(mapType, distance);
	}

	Material? GetOrCreateEffectMaterialCopy(ResourceHandle<ModelInstance> handle) {
		if (_activeInstanceEffectsData.TryGetValue(handle, out var effectsData)) return effectsData.PerInstanceEffectMaterialCopy;

		var curMat = GetMaterial(handle);
		if (!curMat.SupportsPerInstanceEffects) return null;

		var result = curMat.Duplicate();
		SetModelInstanceMaterial(
			handle,
			result.Handle
		).ThrowIfFailure();
		_activeInstanceEffectsData[handle] = new(result);
		return result;
	}

	void DisposeEffectMaterialCopyIfPresent(ResourceHandle<ModelInstance> handle) {
		if (!_activeInstanceEffectsData.TryGetValue(handle, out var effectsData)) return;

		effectsData.PerInstanceEffectMaterialCopy.Dispose();
		_activeInstanceEffectsData.Remove(handle);
	}

	public string GetNameAsNewStringObject(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultModelInstanceName));
	}
	public int GetNameLength(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultModelInstanceName).Length;
	}
	public void CopyName(ResourceHandle<ModelInstance> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultModelInstanceName, destinationBuffer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	ModelInstance HandleToInstance(ResourceHandle<ModelInstance> h) => new(h, this);

	#region Resource Directory
	public unsafe IndirectEnumerable<object, ModelInstance> AllActiveInstances {
		get {
			static LocalObjectBuilder CastSelf(object self) => self as LocalObjectBuilder ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._activeInstanceTransforms.Count;
			static int GetVersion(object self) => CastSelf(self)._activeInstanceTransforms.Version;
			static ModelInstance GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._activeInstanceTransforms.GetPairAtIndex(index).Key);

			ThrowIfThisIsDisposed();
			return new(
				this,
				GetVersion(this),
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
	}
	public bool ResourceNameMatchIsMatching(ModelInstance resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultModelInstanceName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultModelInstanceName).Equals(name, comparisonType);
	}
	#endregion

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_model_instance")]
	static extern InteropResult AllocateModelInstance(
		in Matrix4x4 initialWorldMatrix,
		UIntPtr vertexBufferHandle,
		UIntPtr indexBufferHandle,
		int indexBufferStartIndex,
		int indexBufferCount,
		int boneCount,
		UIntPtr materialHandle,
		Vector3 aabbCenter,
		Vector3 aabbHalfExtents,
		out UIntPtr outModelInstanceHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_model_instance_mesh")]
	static extern InteropResult SetModelInstanceMesh(
		UIntPtr modelInstanceHandle,
		UIntPtr vertexBufferHandle,
		UIntPtr indexBufferHandle,
		int indexBufferStartIndex,
		int indexBufferCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_model_instance_aabb")]
	static extern InteropResult SetModelInstanceAabb(
		UIntPtr modelInstanceHandle,
		Vector3 aabbCenter,
		Vector3 aabbHalfExtents
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_vertex_buffer")]
	static extern InteropResult AllocateVertexBuffer(
		nuint bufferId,
		MeshVertex* verticesPtr,
		int numVertices,
		out UIntPtr outBufferHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "update_vertex_buffer")]
	static extern InteropResult UpdateVertexBuffer(
		UIntPtr bufferHandle,
		nuint bufferId,
		MeshVertex* verticesPtr,
		int vertexCount,
		int startingIndex
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_vertex_buffer")]
	static extern InteropResult DisposeVertexBuffer(UIntPtr bufferHandle);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_model_instance_material")]
	static extern InteropResult SetModelInstanceMaterial(
		UIntPtr modelInstanceHandle,
		UIntPtr materialHandle
	);

	[SuppressGCTransition]
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_model_instance_world_mat")]
	static extern InteropResult SetModelInstanceWorldMatrix(
		UIntPtr modelInstanceHandle,
		in Matrix4x4 newWorldMatrix
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_model_instance")]
	static extern InteropResult DisposeModelInstance(
		UIntPtr modelInstanceHandle
	);
	#endregion

	#region Disposal
	public bool IsDisposed(ResourceHandle<ModelInstance> handle) => _isDisposed || !_activeInstanceTransforms.ContainsKey(handle);
	public void Dispose(ResourceHandle<ModelInstance> handle) => Dispose(handle, removeFromMap: true);

	void Dispose(ResourceHandle<ModelInstance> handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DependencyTracker.DeregisterAllDependencies(HandleToInstance(handle));
		DisposeModelInstance(handle).ThrowIfFailure();
		DisposeEffectMaterialCopyIfPresent(handle);
		DisposeMutationDataIfPresent(handle);
		if (removeFromMap) _activeInstanceTransforms.Remove(handle);
	}

	void DisposeMutationDataIfPresent(ResourceHandle<ModelInstance> handle) {
		if (!_activeInstanceMutationData.Remove(handle, out var mutationData)) return;
		mutationData.CpuShadow.Dispose();
		LocalFrameSynchronizationManager.QueueResourceDisposal(mutationData.PrivateVertexBufferHandle, &DisposeVertexBuffer);
	}

	public void Dispose() {
		try {
			if (_isDisposed) return;
			foreach (var kvp in _activeInstanceTransforms) Dispose(kvp.Key, removeFromMap: false);
			_activeInstanceTransforms.Dispose();
			_activeInstanceEffectsData.Dispose();
			_activeInstanceMutationData.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<ModelInstance> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(ModelInstance));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}