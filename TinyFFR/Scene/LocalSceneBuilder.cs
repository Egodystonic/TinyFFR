// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Scene;

sealed unsafe class LocalSceneBuilder : ISceneBuilder, ISceneImplProvider, IDisposable {
	const string DefaultSceneName = "Unnamed Scene";

	readonly ArrayPoolBackedMap<SceneHandle, ArrayPoolBackedVector<ModelInstance>> _modelInstanceMap = new();
	readonly ObjectPool<ArrayPoolBackedVector<ModelInstance>> _modelInstanceVectorPool;
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalSceneBuilder(LocalFactoryGlobalObjectGroup globals) {
		static ArrayPoolBackedVector<ModelInstance> CreateModelInstanceVector() => new();

		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
		_modelInstanceVectorPool = new(&CreateModelInstanceVector);
	}

	public Scene CreateScene(ReadOnlySpan<char> name = default) => CreateScene(new SceneCreationConfig { Name = name });
	public Scene CreateScene(in SceneCreationConfig config) {
		ThrowIfThisIsDisposed();
		AllocateScene(
			out var handle
		).ThrowIfFailure();
		var result = HandleToInstance(handle);
		_modelInstanceMap.Add(handle, _modelInstanceVectorPool.Rent());
		_globals.StoreResourceNameIfNotDefault(new SceneHandle(handle).Ident, config.Name);
		return result;
	}

	public ReadOnlySpan<char> GetName(SceneHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultSceneName);
	}

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Scene HandleToInstance(SceneHandle h) => new(h, this);

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _modelInstanceMap) {
				Dispose(kvp.Key, removeFromMaps: false);
				_modelInstanceVectorPool.Return(kvp.Value);
			}
			_modelInstanceMap.Dispose();
			_modelInstanceVectorPool.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	public bool IsDisposed(SceneHandle handle) => _isDisposed || !_modelInstanceMap.ContainsKey(handle);

	public void Dispose(SceneHandle handle) => Dispose(handle, removeFromMaps: true);
	void Dispose(SceneHandle handle, bool removeFromMaps) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DependencyTracker.DeregisterAllDependencies(HandleToInstance(handle));
		DisposeScene(handle).ThrowIfFailure();
		if (!removeFromMaps) return;

		_modelInstanceVectorPool.Return(_modelInstanceMap[handle]);
		_modelInstanceMap.Remove(handle);
	}

	void ThrowIfThisOrHandleIsDisposed(SceneHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Scene));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}