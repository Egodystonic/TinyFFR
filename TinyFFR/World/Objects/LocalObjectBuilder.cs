// Created on 2024-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Assets.Materials.Local.LocalShaderPackageConstants.DefaultMaterialShaderConstants;

namespace Egodystonic.TinyFFR.World;

sealed unsafe class LocalObjectBuilder : IObjectBuilder, IModelInstanceImplProvider, IResourceDirectory<ModelInstance>, IDisposable {
	readonly record struct LocalVertexMutationData(UIntPtr PrivateVertexBufferHandle, PooledHeapMemory<MeshVertex> CurrentVertices);
	readonly record struct VertexLeaseData(Range Range, bool RecalculateBoundingBox);
	readonly record struct PrivateMaterialData(Material Material, bool IsDefault, ShadingModeVariant CurrentShadingMode, ColorVect BaseColor);
	readonly record struct TextInstanceData(FontPen Pen, FontString String, TextLayout Layout);
	
	const string DefaultModelInstanceName = "Unnamed Model Instance";
	const ShadingModeVariant DefaultPrimitiveShadingMode = ShadingModeVariant.Plain3DOpaque;
	static readonly ColorVect DefaultPrimitiveBaseColor = ColorVect.WhiteOpaque;
	readonly LocalFactoryGlobalObjectGroup _globals;
	// Because these properties are set frequently, they're all kept in their own separate maps for performance
	readonly ArrayPoolBackedMap<ResourceHandle<ModelInstance>, Transform> _activeInstanceTransforms = new();
	readonly ArrayPoolBackedMap<ResourceHandle<ModelInstance>, PrivateMaterialData> _privateMaterialInstances = new();
	readonly ArrayPoolBackedMap<ResourceHandle<ModelInstance>, LocalVertexMutationData> _activeInstanceVertexMutationData = new();
	readonly ArrayPoolBackedMap<ResourceHandle<ModelInstance>, TextInstanceData> _activeInstanceTextInstanceData = new();
	readonly ResourceHandleBasedSpanLeaseTracker<MeshVertex, VertexLeaseData> _vertexLeaseTracker;
	readonly LocalMaterialBuilder _materialBuilder;
	bool _isDisposed = false;

	public LocalObjectBuilder(LocalFactoryGlobalObjectGroup globals, LocalMaterialBuilder materialBuilder) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_materialBuilder = materialBuilder;
		_vertexLeaseTracker = new(null, true, globals.InEnhancedSecurityEnvironment, &HandleVertexLeaseDisposal, this);
	}

	public ModelInstance CreateModelInstance(Mesh mesh, Material? material, in ModelInstanceCreationConfig config) {
		ThrowIfThisIsDisposed();
		var meshBufferData = mesh.BufferData;
		var aabb = mesh.BoundingBox;
		
		var materialArg = material ?? _materialBuilder.DefaultMaterial;
		var isDefault = materialArg == _materialBuilder.DefaultMaterial;
		var materialActual = isDefault ? _materialBuilder.AllocateDefaultMaterialInstance(DefaultPrimitiveShadingMode, DefaultPrimitiveBaseColor) : materialArg;

		AllocateModelInstance(
			config.InitialTransform.ToMatrix(),
			meshBufferData.VertexBufferHandle,
			meshBufferData.IndexBufferHandle,
			meshBufferData.IndexBufferStartIndex,
			meshBufferData.IndexBufferCount,
			meshBufferData.BoneCount,
			materialActual.Handle,
			aabb.Position.ToVector3(),
			new Vector3(aabb.HalfWidth, aabb.HalfHeight, aabb.HalfDepth),
			out var handle
		).ThrowIfFailure();

		var result = HandleToInstance(handle);
		SetShadowOptionsAccordingToMaterial(handle, materialActual);
		_activeInstanceTransforms.Add(handle, config.InitialTransform);
		_globals.StoreResourceNameOrDefaultIfEmpty(new ResourceHandle<ModelInstance>(handle).Ident, config.Name, DefaultModelInstanceName);
		_globals.DependencyTracker.RegisterDependency(result, mesh);
		if (!isDefault) _globals.DependencyTracker.RegisterDependency(result, materialActual);
		else _privateMaterialInstances[handle] = new(materialActual, true, DefaultPrimitiveShadingMode, DefaultPrimitiveBaseColor);

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

	public void SetTransformWithoutUpdatingWorldMatrix(ResourceHandle<ModelInstance> handle, in Transform newTransform) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_activeInstanceTransforms[handle] = newTransform;
	}
	public void SetWorldMatrixWithoutUpdatingTransform(ResourceHandle<ModelInstance> handle, in Matrix4x4 worldMatrix) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetModelInstanceWorldMatrix(handle, worldMatrix).ThrowIfFailure();
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

	public Quaternion GetRotationQuaternion(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceTransforms[handle].RotationQuaternion;
	}
	public void SetRotationQuaternion(ResourceHandle<ModelInstance> handle, Quaternion newRotationQuaternion) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle] with { RotationQuaternion = newRotationQuaternion });
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
	public void RotateBy(ResourceHandle<ModelInstance> handle, Quaternion rotationQuaternion) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceTransforms[handle].WithAdditionalRotation(rotationQuaternion));
	}
	public void RotateBy(ResourceHandle<ModelInstance> handle, Quaternion rotationQuaternion, Location pivotPoint) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var curTransform = _activeInstanceTransforms[handle];
		var curLoc = curTransform.Translation.AsLocation();
		var pivotToCurLoc = pivotPoint >> curLoc;
		var newLoc = pivotPoint + pivotToCurLoc.RotatedBy(rotationQuaternion);
		UpdateTransformAndMatrix(handle, (curTransform with { Translation = newLoc.AsVect() }).WithAdditionalRotation(rotationQuaternion));
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
		_vertexLeaseTracker.ThrowIfAnyActiveRentals(handle, nameof(ModelInstance), _globals.GetResourceName(handle.Ident, DefaultModelInstanceName));
		
		var newMeshBufferData = newMesh.BufferData;
		var newMeshBoundingBox = newMesh.BoundingBox;

		DisposeMutationDataIfPresent(handle);

		if (_privateMaterialInstances.TryGetValue(handle, out var privateMaterialData) && privateMaterialData is { IsDefault: true, CurrentShadingMode: ShadingModeVariant.Wireframe }) {
			if (newMesh.WireframeBufferData is { } newWireframeData) newMeshBufferData = newWireframeData;
			else {
				var replacementDefaultMaterial = _materialBuilder.AllocateDefaultMaterialInstance(DefaultPrimitiveShadingMode, privateMaterialData.BaseColor);
				SetModelInstanceMaterial(handle, replacementDefaultMaterial.GetHandleWithoutDisposeCheck()).ThrowIfFailure();
				privateMaterialData.Material.Dispose();
				_privateMaterialInstances[handle] = privateMaterialData with { CurrentShadingMode = DefaultPrimitiveShadingMode, Material = replacementDefaultMaterial };
			}
		}
		
		SetModelInstanceMesh(
			handle,
			newMeshBufferData.VertexBufferHandle,
			newMeshBufferData.IndexBufferHandle,
			newMeshBufferData.IndexBufferStartIndex,
			newMeshBufferData.IndexBufferCount
		).ThrowIfFailure();
		SetModelInstanceAabb(
			handle,
			newMeshBoundingBox.Position.ToVector3(),
			new Vector3(newMeshBoundingBox.HalfWidth, newMeshBoundingBox.HalfHeight, newMeshBoundingBox.HalfDepth)
		).ThrowIfFailure();
		
		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), GetMesh(handle));
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), newMesh);
	}

	LocalVertexMutationData GetOrAllocateActiveVertexMutationData(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var mesh = GetMesh(handle);
		if (!mesh.AllowsPerInstanceVertexMutation) {
			throw new InvalidOperationException(
				$"Can not load or modify vertices for instances of {mesh} as it was not created " +
				$"with the '{nameof(MeshCreationConfig.AllowsPerInstanceVertexMutation)}' flag set to true."
			);
		}

		if (_activeInstanceVertexMutationData.TryGetValue(handle, out var vertexMutationData)) return vertexMutationData;
		
		using (var defaultVertsLease = mesh.BorrowDefaultVerticesSpan()) {
			var currentVertexData = _globals.HeapPool.BorrowAndCopy(defaultVertsLease.Span);
			var gpuHoldingBuffer = _globals.CreateGpuHoldingBufferAndCopyData(currentVertexData.Span);

			var allocResult = AllocateVertexBuffer(
				gpuHoldingBuffer.BufferIdentity,
				(MeshVertex*) gpuHoldingBuffer.DataPtr,
				defaultVertsLease.Span.Length,
				out var privateVbHandle
			);
			if (!allocResult) {
				currentVertexData.Dispose();
				allocResult.ThrowIfFailure();
				throw new InvalidOperationException(); // Should never reach here but it's a guard just in case
			}
			vertexMutationData = _activeInstanceVertexMutationData[handle] = new(privateVbHandle, currentVertexData);

			var meshBufferData = mesh.BufferData;
			SetModelInstanceMesh(
				handle,
				privateVbHandle,
				meshBufferData.IndexBufferHandle,
				meshBufferData.IndexBufferStartIndex,
				meshBufferData.IndexBufferCount
			).ThrowIfFailure();
		}
		
		return vertexMutationData;
	}
	public ScopedReadOnlySpanLease<MeshVertex> BorrowVerticesSpanReadOnly(ResourceHandle<ModelInstance> handle) {
		var vertexMutationData = GetOrAllocateActiveVertexMutationData(handle);
		return _vertexLeaseTracker.CreateScopedLeaseOrThrow(handle, vertexMutationData.CurrentVertices.Span);
	}
	public ScopedSpanLease<MeshVertex> BorrowVerticesSpan(ResourceHandle<ModelInstance> handle, Range range, bool recalculateBoundingBox) {
		var vertexMutationData = GetOrAllocateActiveVertexMutationData(handle);
		return _vertexLeaseTracker.CreateScopedLeaseOrThrow(handle, vertexMutationData.CurrentVertices.Span[range], new VertexLeaseData(range, recalculateBoundingBox));
	}
	static void HandleVertexLeaseDisposal(object? builder, ResourceHandle handle, VertexLeaseData leaseData, int _) {
		((LocalObjectBuilder) builder!).ExecuteVertexMutation((ResourceHandle<ModelInstance>) handle, leaseData);
	} 
	void ExecuteVertexMutation(ResourceHandle<ModelInstance> handle, VertexLeaseData leaseData) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var vertexMutationData = _activeInstanceVertexMutationData[handle];

		// In theory, this isn't possible. But we'll check anyway in case something went very wrong
		// as the consequence is potentially passing out-of-bounds arguments to native side
		var vertexCount = vertexMutationData.CurrentVertices.Span.Length;
		var (startIndex, length) = leaseData.Range.GetOffsetAndLength(vertexCount);
		if (startIndex < 0 || startIndex + length > vertexCount) {
			throw new ArgumentOutOfRangeException(
				nameof(leaseData), 
				$"Start index ({startIndex}) + replacement vertex span length ({length}) " +
				$"would exceed mesh vertex count ({vertexCount}). This is a bug in TinyFFR."
			);
		}

		var gpuHoldingBuffer = _globals.CreateGpuHoldingBufferAndCopyData(vertexMutationData.CurrentVertices.Span[leaseData.Range]);
		UpdateVertexBuffer(
			vertexMutationData.PrivateVertexBufferHandle,
			gpuHoldingBuffer.BufferIdentity,
			(MeshVertex*) gpuHoldingBuffer.DataPtr,
			length,
			startIndex
		).ThrowIfFailure();
		
		if (leaseData.RecalculateBoundingBox) CalculateAndUpdateBoundingBox(handle, vertexMutationData);
	}
	public void TriggerManualBoundingBoxRecalculation(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (_activeInstanceVertexMutationData.TryGetValue(handle, out var vertexMutationData)) CalculateAndUpdateBoundingBox(handle, vertexMutationData);
	}
	void CalculateAndUpdateBoundingBox(ResourceHandle<ModelInstance> handle, LocalVertexMutationData vertexMutationData) {
		var newAabb = MathUtils.CalculateBoundingBox(vertexMutationData.CurrentVertices.Span, MeshCreationConfig.DefaultBoundingBoxAdditionalMargin);
		SetModelInstanceAabb(
			handle,
			newAabb.Position.ToVector3(),
			new Vector3(newAabb.HalfWidth, newAabb.HalfHeight, newAabb.HalfDepth)
		).ThrowIfFailure();
	}

	public Material GetMaterial(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var targets = _globals.DependencyTracker.GetTargetsOfGivenType<ModelInstance, Material, IMaterialImplProvider>(HandleToInstance(handle));
		if (targets.Count > 0) return targets[0];
		else return _materialBuilder.DefaultMaterial;
	}
	public void SetMaterial(ResourceHandle<ModelInstance> handle, Material newMaterial) {
		ThrowIfThisOrHandleIsDisposed(handle);

		var oldMat = GetMaterial(handle);
		var oldIsDefault = oldMat == _materialBuilder.DefaultMaterial;
		var newIsDefault = newMaterial == _materialBuilder.DefaultMaterial;

		if (!oldIsDefault) {
			_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), oldMat);
		}
		else if (newIsDefault) return; // Return early if old mat is the default and we're setting the default again (no-op)

		if (newIsDefault) {
			var primitiveMat = _materialBuilder.AllocateDefaultMaterialInstance(DefaultPrimitiveShadingMode, DefaultPrimitiveBaseColor);
			SetModelInstanceMaterial(
				handle,
				primitiveMat.Handle
			).ThrowIfFailure();
			SetShadowOptionsAccordingToMaterial(handle, primitiveMat);
			DisposePrivateMaterialIfPresent(handle);
			_privateMaterialInstances[handle] = new(primitiveMat, true, DefaultPrimitiveShadingMode, DefaultPrimitiveBaseColor);
		}
		else {
			SetModelInstanceMaterial(
				handle,
				newMaterial.Handle
			).ThrowIfFailure();
			SetShadowOptionsAccordingToMaterial(handle, newMaterial);
			DisposePrivateMaterialIfPresent(handle);
			_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), newMaterial);
		}
	}

	void SetShadowOptionsAccordingToMaterial(ResourceHandle<ModelInstance> handle, Material material) {
		var supportsShadows = _materialBuilder.GetSupportsShadows(material.GetHandleWithoutDisposeCheck());
		SetModelInstanceShadowOptions(handle, supportsShadows, supportsShadows).ThrowIfFailure();
	}

	ShadingModeVariant DetermineCorrectShadingModeVariant(ShadingModeVariant targetMode, ColorVect baseColor) {
		return targetMode switch {
			(ShadingModeVariant.Plain3D or ShadingModeVariant.Plain3DOpaque) when baseColor.Alpha < 1f => ShadingModeVariant.Plain3D, 
			(ShadingModeVariant.Plain3D or ShadingModeVariant.Plain3DOpaque) => ShadingModeVariant.Plain3DOpaque,
			(ShadingModeVariant.Plain or ShadingModeVariant.PlainOpaque) when baseColor.Alpha < 1f => ShadingModeVariant.Plain, 
			(ShadingModeVariant.Plain or ShadingModeVariant.PlainOpaque) => ShadingModeVariant.PlainOpaque,
			_ => targetMode	
		};
	}
	public void SetDefaultMaterialBaseColor(ResourceHandle<ModelInstance> handle, ColorVect newBaseColor) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_privateMaterialInstances.TryGetValue(handle, out var privateMaterialData) || !privateMaterialData.IsDefault) {
			SetMaterial(handle, _materialBuilder.DefaultMaterial);
			privateMaterialData = _privateMaterialInstances[handle];
		}
		var newShadingMode = DetermineCorrectShadingModeVariant(privateMaterialData.CurrentShadingMode, newBaseColor);
		if (newShadingMode != privateMaterialData.CurrentShadingMode) {
			var replacementMaterial = _materialBuilder.AllocateDefaultMaterialInstance(newShadingMode, newBaseColor);
			SetModelInstanceMaterial(
				handle,
				replacementMaterial.GetHandleWithoutDisposeCheck()
			).ThrowIfFailure();
			privateMaterialData.Material.Dispose();
			_privateMaterialInstances[handle] = privateMaterialData with { Material = replacementMaterial, CurrentShadingMode = newShadingMode, BaseColor = newBaseColor };
		}
		else {
			_materialBuilder.SetDefaultMaterialBaseColor(privateMaterialData.Material, newBaseColor);
			_privateMaterialInstances[handle] = privateMaterialData with { BaseColor = newBaseColor };
		}
	}
	public void SetDefaultMaterialShadingStyle(ResourceHandle<ModelInstance> handle, DefaultMaterialShadingStyle newStyle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_privateMaterialInstances.TryGetValue(handle, out var privateMaterialData) || !privateMaterialData.IsDefault) {
			SetMaterial(handle, _materialBuilder.DefaultMaterial);
			privateMaterialData = _privateMaterialInstances[handle];
		}
		
		var newShadingMode = DetermineCorrectShadingModeVariant((ShadingModeVariant) newStyle, privateMaterialData.BaseColor);
		var oldShadingMode = privateMaterialData.CurrentShadingMode;
		if (newShadingMode == oldShadingMode) return;

		if (newShadingMode == ShadingModeVariant.Wireframe) {
			if (GetMesh(handle).WireframeBufferData is not { } wireframeBufferData) return;
			SetModelInstanceMesh(
				handle,
				wireframeBufferData.VertexBufferHandle,
				wireframeBufferData.IndexBufferHandle,
				wireframeBufferData.IndexBufferStartIndex,
				wireframeBufferData.IndexBufferCount
			).ThrowIfFailure();
		}
		else if (oldShadingMode == ShadingModeVariant.Wireframe) {
			var nonWireframeBufferData = GetMesh(handle).BufferData;
			SetModelInstanceMesh(
				handle,
				// Maintainer's note: Shouldn't be possible right now because we don't allow generation of wireframe data at the same time as setting the mutation-permitted flag
				// But it's possible that might change in the future and in case we forget to alter this area I'm pre-emptively checking for it here
				_activeInstanceVertexMutationData.TryGetValue(handle, out var mutationData) ? mutationData.PrivateVertexBufferHandle : nonWireframeBufferData.VertexBufferHandle,
				nonWireframeBufferData.IndexBufferHandle,
				nonWireframeBufferData.IndexBufferStartIndex,
				nonWireframeBufferData.IndexBufferCount
			).ThrowIfFailure();
		}

		var replacementMaterial = _materialBuilder.AllocateDefaultMaterialInstance(newShadingMode, privateMaterialData.BaseColor);
		SetModelInstanceMaterial(
			handle,
			replacementMaterial.GetHandleWithoutDisposeCheck()
		).ThrowIfFailure();
		SetShadowOptionsAccordingToMaterial(handle, replacementMaterial);
		privateMaterialData.Material.Dispose();
		_privateMaterialInstances[handle] = privateMaterialData with { Material = replacementMaterial, CurrentShadingMode = newShadingMode };
	}

	public void SetKeyedMaterialColor(ResourceHandle<ModelInstance> handle, ColorChannel key, ColorVect color) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetOrCreateColorKeyedMaterialCopy(handle)?.SetKeyedColor(key, color);
	}

	public void SetMaterialEffectTransform(ResourceHandle<ModelInstance> handle, Transform2D newTransform) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetOrCreateEffectMaterialCopy(handle)?.SetEffectTransform(newTransform);
	}
	public void SetMaterialEffectBlendTexture(ResourceHandle<ModelInstance> handle, MaterialEffectMapType mapType, Texture mapTexture) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetOrCreateEffectMaterialCopy(handle)?.SetEffectBlendTexture(mapType, mapTexture);
	}
	public void SetMaterialEffectBlendDistance(ResourceHandle<ModelInstance> handle, MaterialEffectMapType mapType, float distance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetOrCreateEffectMaterialCopy(handle)?.SetEffectBlendDistance(mapType, distance);
	}

	Material? GetOrCreateEffectMaterialCopy(ResourceHandle<ModelInstance> handle) {
		if (_privateMaterialInstances.TryGetValue(handle, out var privateMatData)) {
			return privateMatData.IsDefault ? null : privateMatData.Material;
		}

		var curMat = GetMaterial(handle);
		if (!curMat.SupportsPerInstanceEffects) return null;

		return DuplicateAndTrackPrivateMaterialCopy(handle, curMat);
	}

	Material? GetOrCreateColorKeyedMaterialCopy(ResourceHandle<ModelInstance> handle) {
		if (_privateMaterialInstances.TryGetValue(handle, out var privateMatData)) {
			return privateMatData.IsDefault ? null : privateMatData.Material;
		}

		var curMat = GetMaterial(handle);
		if (!curMat.SupportsColorKeying) return null;

		return DuplicateAndTrackPrivateMaterialCopy(handle, curMat);
	}

	Material DuplicateAndTrackPrivateMaterialCopy(ResourceHandle<ModelInstance> handle, Material curMat) {
		DisposePrivateMaterialIfPresent(handle);

		var result = curMat.Duplicate();
		SetModelInstanceMaterial(
			handle,
			result.Handle
		).ThrowIfFailure();
		_privateMaterialInstances[handle] = new(result, false, default, default);
		return result;
	}

	public void SetTextInstanceInitialPenAndString(ResourceHandle<ModelInstance> handle, FontPen pen, FontString @string, TextLayout layout) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_activeInstanceTextInstanceData[handle] = new(pen, @string, layout);
	}
	public void UpdateTextInstancePen(ResourceHandle<ModelInstance> handle, FontPen pen) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_activeInstanceTextInstanceData[handle] = _activeInstanceTextInstanceData[handle] with { Pen = pen };
	}
	public void SetTextInstanceLayout(ResourceHandle<ModelInstance> handle, TextLayout layout) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_activeInstanceTextInstanceData[handle] = _activeInstanceTextInstanceData[handle] with { Layout = layout };
	}
	public void UpdateTextInstanceString(ResourceHandle<ModelInstance> handle, FontString @string) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var data = _activeInstanceTextInstanceData[handle];
		_activeInstanceTextInstanceData[handle] = data with { String = @string };
		
		var layout = data.Layout;
		var font = @string.Font;
		
		var currentTransform = _activeInstanceTransforms[handle];
		var currentScaling = font.GetTextInstanceScaling(data.String.Size, layout);
		var currentOffset = font.GetTextInstanceAnchorOffset(data.String.Size, currentScaling, layout.PositionAnchor);
		
		var anchorPoint = currentTransform.Translation - currentOffset * currentTransform.Rotation;
		var newScaling = font.GetTextInstanceScaling(@string.Size, layout);
		var newOffset = font.GetTextInstanceAnchorOffset(@string.Size, newScaling, layout.PositionAnchor);
		
		UpdateTransformAndMatrix(handle, new Transform(anchorPoint + newOffset * currentTransform.Rotation, currentTransform.Rotation, new Vect(newScaling.X, newScaling.Y, 1f)));
	}
	public FontPen GetTextInstancePen(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceTextInstanceData[handle].Pen;
	}
	public FontString GetTextInstanceString(ResourceHandle<ModelInstance> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceTextInstanceData[handle].String;
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
	public IndirectEnumerable<object, ModelInstance> AllActiveInstances {
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_model_instance_shadow_options")]
	static extern InteropResult SetModelInstanceShadowOptions(
		UIntPtr modelInstanceHandle,
		InteropBool castShadows,
		InteropBool receiveShadows
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
		_vertexLeaseTracker.ThrowIfAnyActiveRentals(handle, nameof(ModelInstance), _globals.GetResourceName(handle.Ident, DefaultModelInstanceName));
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DependencyTracker.DeregisterAllDependencies(HandleToInstance(handle));
		DisposeModelInstance(handle).ThrowIfFailure();
		DisposePrivateMaterialIfPresent(handle);
		DisposeMutationDataIfPresent(handle);
		DisposeTextInstanceDataIfPresent(handle);
		if (removeFromMap) _activeInstanceTransforms.Remove(handle);
	}
	
	void DisposePrivateMaterialIfPresent(ResourceHandle<ModelInstance> handle) {
		if (_privateMaterialInstances.Remove(handle, out var privateMatData)) privateMatData.Material.Dispose();
	}

	void DisposeMutationDataIfPresent(ResourceHandle<ModelInstance> handle) {
		if (!_activeInstanceVertexMutationData.Remove(handle, out var mutationData)) return;
		
		LocalFrameSynchronizationManager.QueueResourceDisposal(mutationData.PrivateVertexBufferHandle, &DisposeVertexBuffer);
		mutationData.CurrentVertices.Dispose();
	}
	
	void DisposeTextInstanceDataIfPresent(ResourceHandle<ModelInstance> handle) => _activeInstanceTextInstanceData.Remove(handle);

	public void Dispose() {
		try {
			if (_isDisposed) return;
			foreach (var kvp in _activeInstanceTransforms) Dispose(kvp.Key, removeFromMap: false);
			_activeInstanceTransforms.Dispose();
			_privateMaterialInstances.Dispose();
			_vertexLeaseTracker.Dispose();
			_activeInstanceVertexMutationData.Dispose();
			_activeInstanceTextInstanceData.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<ModelInstance> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(ModelInstance));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}