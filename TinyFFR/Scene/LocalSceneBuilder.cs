// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;
using System.Reflection.Metadata;

namespace Egodystonic.TinyFFR.Scene;

sealed unsafe class LocalSceneBuilder : ISceneBuilder, ISceneImplProvider, IDisposable {
	const string DefaultSceneName = "Unnamed Scene";

	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ObjectPool<ArrayPoolBackedVector<ModelInstance>> _modelInstanceVectorPool;
	readonly ArrayPoolBackedMap<SceneHandle, ArrayPoolBackedVector<ModelInstance>> _modelInstanceMap = new();
	bool _isDisposed = false;

	public LocalSceneBuilder(LocalFactoryGlobalObjectGroup globals) {
		static ArrayPoolBackedVector<ModelInstance> CreateModelInstanceVector() => new();

		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
		_modelInstanceVectorPool = new(&CreateModelInstanceVector);
	}

	public Scene CreateScene() => CreateScene(new());
	public Scene CreateScene(in SceneCreationConfig config) {
		ThrowIfThisIsDisposed();
		AllocateScene(
			out var handle
		).ThrowIfFailure();
		var result = HandleToInstance(handle);
		_modelInstanceMap.Add(handle, _modelInstanceVectorPool.Rent());
		return result;
	}

	public string GetName(SceneHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceNameAsNewStringObject(handle.Ident, DefaultSceneName);
	}
	public int GetNameUsingSpan(SceneHandle handle, Span<char> dest) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.CopyResourceName(handle.Ident, DefaultSceneName, dest);
	}
	public int GetNameSpanLength(SceneHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceNameLength(handle.Ident, DefaultSceneName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Scene HandleToInstance(SceneHandle h) => new(h, this);

	#region Model Instance
	public void Add(SceneHandle handle, ModelInstance modelInstance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var instanceVector = _modelInstanceMap[handle];
		if (instanceVector.Contains(modelInstance)) {
			throw new InvalidOperationException($"{modelInstance} has already been added to {HandleToInstance(handle)}.");
		}

		AddModelInstanceToScene(
			handle,
			modelInstance.Handle
		).ThrowIfFailure();

		instanceVector.Add(modelInstance);
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), modelInstance);
	}

	public void Remove(SceneHandle handle, ModelInstance modelInstance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var instanceVector = _modelInstanceMap[handle];
		if (!instanceVector.Remove(modelInstance)) {
			throw new InvalidOperationException($"{modelInstance} is not currently added to {HandleToInstance(handle)}.");
		}

		RemoveModelInstanceFromScene(
			handle,
			modelInstance.Handle
		).ThrowIfFailure();

		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), modelInstance);
	}
	#endregion

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_scene")]
	static extern InteropResult AllocateScene(
		out UIntPtr outSceneHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "add_model_instance_to_scene")]
	static extern InteropResult AddModelInstanceToScene(
		UIntPtr sceneHandle,
		UIntPtr modelInstanceHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "remove_model_instance_from_scene")]
	static extern InteropResult RemoveModelInstanceFromScene(
		UIntPtr sceneHandle,
		UIntPtr modelInstanceHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_scene")]
	static extern InteropResult DisposeScene(
		UIntPtr sceneHandle
	);
	#endregion

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _modelInstanceMap) {
				Dispose(kvp.Key, removeFromMaps: false);
				_modelInstanceVectorPool.Return(kvp.Value);
			}
			_modelInstanceMap.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	public bool IsDisposed(SceneHandle handle) => _isDisposed || !_modelInstanceMap.ContainsKey(handle);

	public void Dispose(SceneHandle handle) => Dispose(handle, removeFromMaps: true);
	void Dispose(SceneHandle handle, bool removeFromMaps) {
		if (IsDisposed(handle)) return;
		DisposeScene(handle).ThrowIfFailure();
		if (!removeFromMaps) return;

		_modelInstanceVectorPool.Return(_modelInstanceMap[handle]);
		_modelInstanceMap.Remove(handle);
	}

	void ThrowIfThisOrHandleIsDisposed(SceneHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Scene));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}