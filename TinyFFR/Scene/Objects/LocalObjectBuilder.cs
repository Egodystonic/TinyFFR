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
	readonly ArrayPoolBackedVector<ModelInstanceHandle> _activeInstances = new();
	bool _isDisposed = false;

	public LocalObjectBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	public ModelInstance CreateModelInstance(Mesh mesh, Material material) => CreateModelInstance(mesh, material, new());
	public ModelInstance CreateModelInstance(Mesh mesh, Material material, in ModelInstanceCreationConfig config) {
		ThrowIfThisIsDisposed();
		var meshBufferData = mesh.BufferData;
		AllocateModelInstance(
			meshBufferData.VertexBufferHandle,
			meshBufferData.IndexBufferHandle,
			meshBufferData.IndexBufferStartIndex,
			meshBufferData.IndexBufferCount,
			material.Handle,
			out var handle
		).ThrowIfFailure();
		var result = HandleToInstance(handle);
		_globals.DependencyTracker.RegisterDependency(result, mesh);
		_globals.DependencyTracker.RegisterDependency(result, material);
		_activeInstances.Add(handle);
		return result;
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

	public string GetName(ModelInstanceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceNameAsNewStringObject(handle.Ident, DefaultModelInstanceName);
	}

	public int GetNameUsingSpan(ModelInstanceHandle handle, Span<char> dest) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.CopyResourceName(handle.Ident, DefaultModelInstanceName, dest);
	}

	public int GetNameSpanLength(ModelInstanceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceNameLength(handle.Ident, DefaultModelInstanceName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	ModelInstance HandleToInstance(ModelInstanceHandle h) => new(h, this);

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_model_instance")]
	static extern InteropResult AllocateModelInstance(
		in Matrix4x4 initialTransform,
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_model_instance")]
	static extern InteropResult DisposeModelInstance(
		UIntPtr modelInstanceHandle
	);
	#endregion

	#region Disposal
	public bool IsDisposed(ModelInstanceHandle handle) => _isDisposed || !_activeInstances.Contains(handle);
	public void Dispose(ModelInstanceHandle handle) => Dispose(handle, removeFromVector: true);

	void Dispose(ModelInstanceHandle handle, bool removeFromVector) {
		if (IsDisposed(handle)) return;
		DisposeModelInstance(handle).ThrowIfFailure();
		if (removeFromVector) _activeInstances.Remove(handle);
	}

	public void Dispose() {
		try {
			if (_isDisposed) return;
			foreach (var instance in _activeInstances) Dispose(instance, removeFromVector: false);
			_activeInstances.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ModelInstanceHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(ModelInstance));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}