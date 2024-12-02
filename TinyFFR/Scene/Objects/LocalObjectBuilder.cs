// Created on 2024-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Scene;

sealed class LocalObjectBuilder : IObjectBuilder, IModelInstanceImplProvider, IDisposable {
	const string DefaultModelInstanceName = "Unnamed Model Instance";
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPoolBackedMap<ModelInstanceHandle, Transform> _activeInstanceMap = new();
	bool _isDisposed = false;

	public LocalObjectBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	public ModelInstance CreateModelInstance(Mesh mesh, Material material, Location? initialPosition = null, Rotation? initialRotation = null, Vect? initialScaling = null, ReadOnlySpan<char> name = default) {
		return CreateModelInstance(
			mesh, 
			material, 
			new Transform(
				translation: initialPosition?.AsVect() ?? ModelInstanceCreationConfig.DefaultInitialTransform.Translation,
				rotation: initialRotation ?? ModelInstanceCreationConfig.DefaultInitialTransform.Rotation,
				scaling: initialScaling ?? ModelInstanceCreationConfig.DefaultInitialTransform.Scaling
			),
			name
		);
	}
	public ModelInstance CreateModelInstance(Mesh mesh, Material material, Transform initialTransform, ReadOnlySpan<char> name = default) {
		return CreateModelInstance(
			mesh,
			material,
			new ModelInstanceCreationConfig {
				InitialTransform = initialTransform,
				Name = name
			}
		);
	}
	public ModelInstance CreateModelInstance(Mesh mesh, Material material, in ModelInstanceCreationConfig config) {
		ThrowIfThisIsDisposed();
		var meshBufferData = mesh.BufferData;
		AllocateModelInstance(
			config.InitialTransform.ToMatrix(),
			meshBufferData.VertexBufferHandle,
			meshBufferData.IndexBufferHandle,
			meshBufferData.IndexBufferStartIndex,
			meshBufferData.IndexBufferCount,
			material.Handle,
			out var handle
		).ThrowIfFailure();
		var result = HandleToInstance(handle);
		_activeInstanceMap.Add(handle, config.InitialTransform);
		_globals.StoreResourceNameIfNotDefault(new ModelInstanceHandle(handle).Ident, config.Name);
		_globals.DependencyTracker.RegisterDependency(result, mesh);
		_globals.DependencyTracker.RegisterDependency(result, material);
		return result;
	}

	void UpdateTransformAndMatrix(ModelInstanceHandle handle, Transform newTransform) {
		SetModelInstanceWorldMatrix(handle, newTransform.ToMatrix()).ThrowIfFailure();
		_activeInstanceMap[handle] = newTransform;
	}
	public Transform GetTransform(ModelInstanceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceMap[handle];
	}
	public void SetTransform(ModelInstanceHandle handle, Transform newTransform) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, newTransform);
	}

	public Location GetPosition(ModelInstanceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceMap[handle].Translation.AsLocation();
	}
	public void SetPosition(ModelInstanceHandle handle, Location newPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceMap[handle] with { Translation = newPosition.AsVect() });
	}

	public Rotation GetRotation(ModelInstanceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceMap[handle].Rotation;
	}
	public void SetRotation(ModelInstanceHandle handle, Rotation newRotation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceMap[handle] with { Rotation = newRotation });
	}

	public Vect GetScaling(ModelInstanceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeInstanceMap[handle].Scaling;
	}
	public void SetScaling(ModelInstanceHandle handle, Vect newScaling) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceMap[handle] with { Scaling = newScaling });
	}

	public void TranslateBy(ModelInstanceHandle handle, Vect translation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceMap[handle].WithAdditionalTranslation(translation));
	}
	public void RotateBy(ModelInstanceHandle handle, Rotation rotation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceMap[handle].WithAdditionalRotation(rotation));
	}
	public void ScaleBy(ModelInstanceHandle handle, float scalar) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceMap[handle].WithScalingMultipliedBy(scalar));
	}
	public void ScaleBy(ModelInstanceHandle handle, Vect vect) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceMap[handle].WithScalingMultipliedBy(vect));
	}
	public void AdjustScaleBy(ModelInstanceHandle handle, float scalar) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceMap[handle].WithScalingAdjustedBy(scalar));
	}
	public void AdjustScaleBy(ModelInstanceHandle handle, Vect vect) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UpdateTransformAndMatrix(handle, _activeInstanceMap[handle].WithScalingAdjustedBy(vect));
	}

	public Mesh GetMesh(ModelInstanceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.DependencyTracker.GetNthTargetOfGivenType<ModelInstance, Mesh, MeshHandle, IMeshImplProvider>(HandleToInstance(handle), 0);
	}
	public void SetMesh(ModelInstanceHandle handle, Mesh newMesh) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var meshBufferData = newMesh.BufferData;
		SetModelInstanceMesh(
			handle,
			meshBufferData.VertexBufferHandle,
			meshBufferData.IndexBufferHandle,
			meshBufferData.IndexBufferStartIndex,
			meshBufferData.IndexBufferCount
		).ThrowIfFailure();
		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), GetMesh(handle));
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), newMesh);
	}

	public Material GetMaterial(ModelInstanceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.DependencyTracker.EnumerateTargetsOfGivenType<ModelInstance, Material, MaterialHandle, IMaterialImplProvider>(HandleToInstance(handle))[0];
	}
	public void SetMaterial(ModelInstanceHandle handle, Material newMaterial) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetModelInstanceMaterial(
			handle,
			newMaterial.Handle
		).ThrowIfFailure();
		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), GetMaterial(handle));
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), newMaterial);
	}

	public ReadOnlySpan<char> GetName(ModelInstanceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultModelInstanceName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	ModelInstance HandleToInstance(ModelInstanceHandle h) => new(h, this);

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_model_instance")]
	static extern InteropResult AllocateModelInstance(
		in Matrix4x4 initialWorldMatrix,
		UIntPtr vertexBufferHandle,
		UIntPtr indexBufferHandle,
		int indexBufferStartIndex,
		int indexBufferCount,
		UIntPtr materialHandle,
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
	public bool IsDisposed(ModelInstanceHandle handle) => _isDisposed || !_activeInstanceMap.ContainsKey(handle);
	public void Dispose(ModelInstanceHandle handle) => Dispose(handle, removeFromMap: true);

	void Dispose(ModelInstanceHandle handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		DisposeModelInstance(handle).ThrowIfFailure();
		if (removeFromMap) _activeInstanceMap.Remove(handle);
	}

	public void Dispose() {
		try {
			if (_isDisposed) return;
			foreach (var kvp in _activeInstanceMap) Dispose(kvp.Key, removeFromMap: false);
			_activeInstanceMap.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ModelInstanceHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(ModelInstance));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}