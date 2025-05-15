// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed unsafe class LocalSceneBuilder : ISceneBuilder, ISceneImplProvider, IDisposable {
	readonly record struct BackdropData(EnvironmentCubemap? Cubemap, UIntPtr SkyboxHandle, UIntPtr IndirectLightHandle);
	const string DefaultSceneName = "Unnamed Scene";
	
	readonly ArrayPoolBackedVector<ResourceHandle<Scene>> _activeSceneHandles = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedVector<ModelInstance>> _modelInstanceMap = new();
	readonly ObjectPool<ArrayPoolBackedVector<ModelInstance>> _modelInstanceVectorPool;
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedVector<Light>> _lightMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, Quality> _shadowQualityActivePresetMap = new();
	readonly ObjectPool<ArrayPoolBackedVector<Light>> _lightVectorPool;
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, BackdropData> _backdropMap = new();
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

		if (config.InitialBackdropColor is { } color) SetBackdrop(handle, color, 1f);
		return HandleToInstance(handle);
	}

	public string GetNameAsNewStringObject(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultSceneName));
	}
	public int GetNameLength(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultSceneName).Length;
	}
	public void CopyName(ResourceHandle<Scene> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultSceneName, destinationBuffer);
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
	public void Add<TLight>(ResourceHandle<Scene> handle, TLight light) where TLight : ILight<TLight> {
		ThrowIfThisOrHandleIsDisposed(handle);
		var instanceVector = _lightMap[handle];
		if (instanceVector.Contains(light.AsBaseLight())) return;
		if (light.Type == LightType.Directional) {
			foreach (var otherLight in instanceVector) {
				if (otherLight.Type == LightType.Directional) {
					throw new InvalidOperationException($"Each scene may only have one {nameof(DirectionalLight)} added at any given time. " +
														$"Remove {otherLight} first before attempting to add {light} to this scene.");
				}
			}
		}

		AddLightToScene(
			handle,
			light.Handle
		).ThrowIfFailure();

		instanceVector.Add(light.AsBaseLight());
		_shadowQualityActivePresetMap.Remove(handle);
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), light);
	}

	public void Remove<TLight>(ResourceHandle<Scene> handle, TLight light) where TLight : ILight<TLight> {
		ThrowIfThisOrHandleIsDisposed(handle);
		var instanceVector = _lightMap[handle];
		if (!instanceVector.Remove(light.AsBaseLight())) return;

		RemoveLightFromScene(
			handle,
			light.Handle
		).ThrowIfFailure();

		_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), light);
	}

	public void SetLightShadowFidelity(ResourceHandle<Scene> handle, Quality qualityPreset, LightShadowFidelityData pointLightFidelity, LightShadowFidelityData spotLightFidelity, LightShadowFidelityData directionalLightFidelity) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (_shadowQualityActivePresetMap.TryGetValue(handle, out var activePreset) && activePreset == qualityPreset) return;

		foreach (var light in _lightMap[handle]) {
			switch (light.Type) {
				case LightType.Point:
					light.SetShadowFidelity(pointLightFidelity);
					break;
				case LightType.Spot:
					light.SetShadowFidelity(spotLightFidelity);
					break;
				case LightType.Directional:
					light.SetShadowFidelity(directionalLightFidelity);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(light.Type), light.Type, "Unhandled light type");
			}
		}

		_shadowQualityActivePresetMap[handle] = qualityPreset;
	}
	#endregion

	#region Backdrop
	public void SetBackdrop(ResourceHandle<Scene> handle, EnvironmentCubemap cubemap, float indirectLightingIntensity) {
		ThrowIfThisOrHandleIsDisposed(handle);

		RemoveBackdrop(handle);

		CreateSceneBackdrop(
			cubemap.SkyboxTextureHandle,
			cubemap.IndirectLightingTextureHandle,
			Scene.BrightnessToLux(indirectLightingIntensity),
			out var skyboxHandle,
			out var indirectLightHandle
		).ThrowIfFailure();

		SetSceneBackdrop(
			handle,
			skyboxHandle,
			indirectLightHandle
		).ThrowIfFailure();

		_backdropMap[handle] = new(cubemap, skyboxHandle, indirectLightHandle);
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), cubemap);
	}
	public void SetBackdrop(ResourceHandle<Scene> handle, ColorVect color, float indirectLightingIntensity) {
		ThrowIfThisOrHandleIsDisposed(handle);

		RemoveBackdrop(handle);

		CreateSceneBackdrop(
			color.AsVector4,
			Scene.BrightnessToLux(indirectLightingIntensity),
			out var skyboxHandle,
			out var indirectLightHandle
		).ThrowIfFailure();

		SetSceneBackdrop(
			handle,
			skyboxHandle,
			indirectLightHandle
		).ThrowIfFailure();

		_backdropMap[handle] = new(null, skyboxHandle, indirectLightHandle);
	}
	public void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, EnvironmentCubemap cubemap, float backdropIntensity) {
		ThrowIfThisOrHandleIsDisposed(handle);

		RemoveBackdrop(handle);

		CreateSceneBackdrop(
			cubemap.SkyboxTextureHandle,
			cubemap.IndirectLightingTextureHandle,
			Scene.BrightnessToLux(backdropIntensity),
			out var skyboxHandle,
			out var indirectLightHandle
		).ThrowIfFailure();

		SetSceneBackdrop(
			handle,
			skyboxHandle,
			UIntPtr.Zero
		).ThrowIfFailure();

		_backdropMap[handle] = new(cubemap, skyboxHandle, indirectLightHandle);
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), cubemap);
	}
	public void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, ColorVect color) {
		ThrowIfThisOrHandleIsDisposed(handle);

		RemoveBackdrop(handle);

		CreateSceneBackdrop(
			color.AsVector4,
			0f,
			out var skyboxHandle,
			out var indirectLightHandle
		).ThrowIfFailure();

		SetSceneBackdrop(
			handle,
			skyboxHandle,
			UIntPtr.Zero
		).ThrowIfFailure();

		_backdropMap[handle] = new(null, skyboxHandle, indirectLightHandle);
	}
	public void RemoveBackdrop(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_backdropMap.Remove(handle, out var curBackdropData)) return;

		UnsetSceneBackdrop(handle).ThrowIfFailure();
		DisposeSceneBackdrop(curBackdropData.SkyboxHandle, curBackdropData.IndirectLightHandle).ThrowIfFailure();
		if (curBackdropData.Cubemap is { } cubemap) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), cubemap);
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "create_scene_backdrop_texture")]
	static extern InteropResult CreateSceneBackdrop(
		UIntPtr skyboxTextureHandle,
		UIntPtr iblTextureHandle,
		float indirectLightingIntensity,
		out UIntPtr outSkyboxHandle,
		out UIntPtr outIndirectLightHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "create_scene_backdrop_color")]
	static extern InteropResult CreateSceneBackdrop(
		Vector4 color,
		float indirectLightingIntensity,
		out UIntPtr outSkyboxHandle,
		out UIntPtr outIndirectLightHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_scene_backdrop")]
	static extern InteropResult SetSceneBackdrop(
		UIntPtr sceneHandle,
		UIntPtr skyboxHandle,
		UIntPtr indirectLightHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unset_scene_backdrop")]
	static extern InteropResult UnsetSceneBackdrop(
		UIntPtr sceneHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_scene_backdrop")]
	static extern InteropResult DisposeSceneBackdrop(
		UIntPtr skyboxHandle,
		UIntPtr indirectLightHandle
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
			_shadowQualityActivePresetMap.Dispose();

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