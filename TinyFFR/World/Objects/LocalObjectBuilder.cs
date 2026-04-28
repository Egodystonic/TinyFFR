// Created on 2024-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed class LocalObjectBuilder : IObjectBuilder, IModelInstanceImplProvider, IResourceDirectory<ModelInstance>, IDisposable {
	readonly record struct ActiveModelInstanceEffectsData(Material PerInstanceEffectMaterialCopy);

	const string DefaultModelInstanceName = "Unnamed Model Instance";
	readonly LocalFactoryGlobalObjectGroup _globals;
	// Because instance transforms are set so frequently, they're kept in their own separate map for performance
	readonly ArrayPoolBackedMap<ResourceHandle<ModelInstance>, Transform> _activeInstanceTransforms = new();
	readonly ArrayPoolBackedMap<ResourceHandle<ModelInstance>, ActiveModelInstanceEffectsData> _activeInstanceEffectsData = new();
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

	public ModelInstanceGroup CreateModelInstances(IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Model> models, in ModelInstanceCreationConfig config) {
		ThrowIfThisIsDisposed();
		var resourceGroup = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: models.Count > 0 ? models.Count : 1, name: config.Name);
		foreach (var model in models) {
			resourceGroup.Add(CreateModelInstance(model.Mesh, model.Material, in config));
		}
		resourceGroup.Seal();
		return new ModelInstanceGroup(resourceGroup);
	}
	public ModelInstanceGroup CreateModelInstances(ReadOnlySpan<Model> models, in ModelInstanceCreationConfig config) {
		ThrowIfThisIsDisposed();
		var resourceGroup = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: models.Length > 0 ? models.Length : 1, name: config.Name);
		foreach (var model in models) {
			resourceGroup.Add(CreateModelInstance(model.Mesh, model.Material, in config));
		}
		resourceGroup.Seal();
		return new ModelInstanceGroup(resourceGroup);
	}
	public ModelInstanceGroup GroupModelInstances(ReadOnlySpan<ModelInstance> instances, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();
		var resourceGroup = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: instances.Length > 0 ? instances.Length : 1, name: name);
		foreach (var instance in instances) {
			resourceGroup.Add(instance);
		}
		resourceGroup.Seal();
		return new ModelInstanceGroup(resourceGroup);
	}
	public ModelInstanceGroup GroupModelInstances(IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, ModelInstance> instances, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();
		var resourceGroup = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: instances.Count > 0 ? instances.Count : 1, name: name);
		foreach (var instance in instances) {
			resourceGroup.Add(instance);
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
			meshBufferData.IndexBufferCount,
			aabb.Position.ToVector3(),
			new Vector3(aabb.HalfWidth, aabb.HalfHeight, aabb.HalfDepth)
		).ThrowIfFailure();
		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), GetMesh(handle));
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), newMesh);
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
		int indexBufferCount,
		Vector3 aabbCenter,
		Vector3 aabbHalfExtents
	);

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
		if (removeFromMap) _activeInstanceTransforms.Remove(handle);
	}

	public void Dispose() {
		try {
			if (_isDisposed) return;
			foreach (var kvp in _activeInstanceTransforms) Dispose(kvp.Key, removeFromMap: false);
			_activeInstanceTransforms.Dispose();
			_activeInstanceEffectsData.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<ModelInstance> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(ModelInstance));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}