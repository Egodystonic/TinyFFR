// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed unsafe class LocalSceneBuilder : ISceneBuilder, ISceneImplProvider, IDisposable {
	const string DefaultSceneName = "Unnamed Scene";

	readonly ArrayPoolBackedVector<ResourceHandle<Scene>> _activeSceneHandles = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedVector<ModelInstance>> _modelInstanceMap = new();
	readonly ObjectPool<ArrayPoolBackedVector<ModelInstance>> _modelInstanceVectorPool;
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedVector<Light>> _lightMap = new();
	readonly ObjectPool<ArrayPoolBackedVector<Light>> _lightVectorPool;
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, EnvironmentCubemap> _backdropMap = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalSceneBuilder(LocalFactoryGlobalObjectGroup globals) {
		static ArrayPoolBackedVector<ModelInstance> CreateModelInstanceVector() => new();
		static ArrayPoolBackedVector<Light> CreateLightVector() => new();

		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
		_modelInstanceVectorPool = new(&CreateModelInstanceVector);
		_lightVectorPool = new(&CreateLightVector);
	}

	public Scene CreateScene(in SceneCreationConfig config) {
		ThrowIfThisIsDisposed();
		AllocateScene(
			out var handle
		).ThrowIfFailure();

		_activeSceneHandles.Add(handle);
		_modelInstanceMap.Add(handle, _modelInstanceVectorPool.Rent());
		_lightMap.Add(handle, _lightVectorPool.Rent());
		
		_globals.StoreResourceNameIfNotEmpty(new ResourceHandle<Scene>(handle).Ident, config.Name);
		
		return HandleToInstance(handle);
	}

	public ReadOnlySpan<char> GetName(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultSceneName);
	}

	#region Model Instance
	public void Add(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var instanceVector = _modelInstanceMap[handle];
		if (instanceVector.Contains(modelInstance)) return;

		AddModelInstanceToScene(
			handle,
			modelInstance.Handle
		).ThrowIfFailure();

		instanceVector.Add(modelInstance);
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), modelInstance);
	}

	public void Remove(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var instanceVector = _modelInstanceMap[handle];
		if (!instanceVector.Remove(modelInstance)) return;

		RemoveModelInstanceFromScene(
			handle,
			modelInstance.Handle
		).ThrowIfFailure();

		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), modelInstance);
	}
	#endregion

	#region Light
	public void Add(ResourceHandle<Scene> handle, Light light) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var instanceVector = _lightMap[handle];
		if (instanceVector.Contains(light)) return;

		AddLightToScene(
			handle,
			light.Handle
		).ThrowIfFailure();

		instanceVector.Add(light);
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), light);
	}

	public void Remove(ResourceHandle<Scene> handle, Light light) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var instanceVector = _lightMap[handle];
		if (!instanceVector.Remove(light)) return;

		RemoveLightFromScene(
			handle,
			light.Handle
		).ThrowIfFailure();

		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), light);
	}
	#endregion

	#region Backdrop
	public EnvironmentCubemap? GetBackdrop(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _backdropMap.TryGetValue(handle, out var cubemap) ? cubemap : null;
	}
	public void SetBackdrop(ResourceHandle<Scene> handle, EnvironmentCubemap? newBackdrop) {
		ThrowIfThisOrHandleIsDisposed(handle);

		if (_backdropMap.TryGetValue(handle, out var existingCubemap)) {
			_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), existingCubemap);
		}

		if (newBackdrop == null) {
			_backdropMap.Remove(handle);
			RemoveSceneBackdrop(handle).ThrowIfFailure();
		}
		else {
			_backdropMap[handle] = newBackdrop.Value;
			SetSceneBackdrop(handle, newBackdrop.Value.SkyboxHandle, newBackdrop.Value.IndirectLightingHandle).ThrowIfFailure();
			_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), newBackdrop.Value);
		}
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "add_light_to_scene")]
	static extern InteropResult AddLightToScene(
		UIntPtr sceneHandle,
		UIntPtr lightHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "remove_light_from_scene")]
	static extern InteropResult RemoveLightFromScene(
		UIntPtr sceneHandle,
		UIntPtr lightHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_scene_backdrop")]
	static extern InteropResult SetSceneBackdrop(
		UIntPtr sceneHandle,
		UIntPtr skyboxHandle,
		UIntPtr indirectLightHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "remove_scene_backdrop")]
	static extern InteropResult RemoveSceneBackdrop(
		UIntPtr sceneHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_scene")]
	static extern InteropResult DisposeScene(
		UIntPtr sceneHandle
	);
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Scene HandleToInstance(ResourceHandle<Scene> h) => new(h, this);

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			while (_activeSceneHandles.Count > 0) Dispose(_activeSceneHandles[^1]);

			_modelInstanceMap.Dispose();
			_modelInstanceVectorPool.Dispose();

			_backdropMap.Dispose();
			_lightMap.Dispose();
			_lightVectorPool.Dispose();

			_activeSceneHandles.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	public bool IsDisposed(ResourceHandle<Scene> handle) => _isDisposed || !_activeSceneHandles.Contains(handle);

	public void Dispose(ResourceHandle<Scene> handle) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DependencyTracker.DeregisterAllDependencies(HandleToInstance(handle));
		DisposeScene(handle).ThrowIfFailure();

		_backdropMap.Remove(handle);

		_modelInstanceVectorPool.Return(_modelInstanceMap[handle]);
		_modelInstanceMap.Remove(handle);

		_lightVectorPool.Return(_lightMap[handle]);
		_lightMap.Remove(handle);
		
		_activeSceneHandles.Remove(handle);
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Scene> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Scene));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}