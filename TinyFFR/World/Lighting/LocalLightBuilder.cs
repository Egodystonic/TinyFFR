// Created on 2024-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed class LocalLightBuilder : ILightBuilder, ILightImplProvider, IDisposable {
	readonly record struct LightData(LightType Type, float Brightness);
	const string DefaultModelInstanceName = "Unnamed Light";
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPoolBackedMap<ResourceHandle<Light>, LightData> _activeLightMap = new();
	bool _isDisposed = false;

	public LocalLightBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	public PointLight CreatePointLight(in PointLightCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		AllocatePointLight(out var handle).ThrowIfFailure();
		_activeLightMap.Add(handle, new(LightType.PointLight, config.InitialBrightness));
		_globals.StoreResourceNameIfNotEmpty(new ResourceHandle<Light>(handle).Ident, config.Name);
		SetLightPosition(handle, config.InitialPosition.ToVector3());
		SetLightColor(handle, config.InitialColor.ToVector3());
		SetPointLightLumens(handle, PointLight.BrightnessToLumens(config.InitialBrightness));
		SetPointLightMaxIlluminationRadius(handle, config.InitialMaxIlluminationRadius);
		return HandleToInstance<PointLight>(handle);
	}

	public LightType GetType(ResourceHandle<Light> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeLightMap[handle].Type;
	}

	public Location GetPosition(ResourceHandle<Light> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetLightPosition(handle, out var result).ThrowIfFailure();
		return Location.FromVector3(result);
	}
	public void SetPosition(ResourceHandle<Light> handle, Location newPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetLightPosition(handle, newPosition.ToVector3()).ThrowIfFailure();
	}
	public void TranslateBy(ResourceHandle<Light> handle, Vect translation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetLightPosition(handle, out var result).ThrowIfFailure();
		SetLightPosition(handle, result + translation.ToVector3()).ThrowIfFailure();
	}

	public ColorVect GetColor(ResourceHandle<Light> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetLightColor(handle, out var result);
		return ColorVect.FromVector3(result);
	}
	public void SetColor(ResourceHandle<Light> handle, ColorVect newColor) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetLightColor(handle, newColor.ToVector3()).ThrowIfFailure();
	}

	public float GetUniversalBrightness(ResourceHandle<Light> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeLightMap[handle].Brightness;
	}
	public void SetUniversalBrightness(ResourceHandle<Light> handle, float newBrightness) {
		ThrowIfThisOrHandleIsDisposed(handle);
		switch (HandleToInstance(handle).Type) {
			case LightType.PointLight:
				newBrightness = PointLight.ClampBrightnessToValidRange(newBrightness);
				var pointLightLumens = PointLight.BrightnessToLumensNoClamp(newBrightness);
				SetPointLightLumens(handle, pointLightLumens).ThrowIfFailure();
				break;
		}
		_activeLightMap[handle] = _activeLightMap[handle] with { Brightness = newBrightness };
	}

	public void AdjustBrightnessBy(ResourceHandle<Light> handle, float adjustment) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetUniversalBrightness(handle, _activeLightMap[handle].Brightness + adjustment);
	}
	public void ScaleBrightnessBy(ResourceHandle<Light> handle, float scalar) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetUniversalBrightness(handle, _activeLightMap[handle].Brightness * scalar);
	}

	public float GetPointLightMaxIlluminationRadius(ResourceHandle<Light> handle) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.PointLight);
		GetPointLightMaxIlluminationRadius(handle, out var result).ThrowIfFailure();
		return result;
	}
	public void SetPointLightMaxIlluminationRadius(ResourceHandle<Light> handle, float newRadius) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.PointLight);
		LocalLightBuilder.SetPointLightMaxIlluminationRadius(handle, newRadius).ThrowIfFailure();
	}

	public ReadOnlySpan<char> GetName(ResourceHandle<Light> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultModelInstanceName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Light HandleToInstance(ResourceHandle<Light> h) => new(h, this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	T HandleToInstance<T>(ResourceHandle<Light> h) where T : ILight<T> => T.FromBaseLight(HandleToInstance(h));

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_point_light")]
	static extern InteropResult AllocatePointLight(
		out UIntPtr outLightHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_light_position")]
	static extern InteropResult GetLightPosition(
		UIntPtr lightHandle,
		out Vector3 outPosition
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_light_position")]
	static extern InteropResult SetLightPosition(
		UIntPtr lightHandle,
		Vector3 newPosition
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_light_color")]
	static extern InteropResult GetLightColor(
		UIntPtr lightHandle,
		out Vector3 outColor
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_light_color")]
	static extern InteropResult SetLightColor(
		UIntPtr lightHandle,
		Vector3 newColor
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_point_light_lumens")]
	static extern InteropResult GetPointLightLumens(
		UIntPtr lightHandle,
		out float outLumens
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_point_light_lumens")]
	static extern InteropResult SetPointLightLumens(
		UIntPtr lightHandle,
		float newLumens
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_point_light_max_illumination_radius")]
	static extern InteropResult GetPointLightMaxIlluminationRadius(
		UIntPtr lightHandle,
		out float outRadius
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_point_light_max_illumination_radius")]
	static extern InteropResult SetPointLightMaxIlluminationRadius(
		UIntPtr lightHandle,
		float newRadius
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_light")]
	static extern InteropResult DisposeLight(
		UIntPtr lightHandle
	);
	#endregion

	#region Disposal
	public bool IsDisposed(ResourceHandle<Light> handle) => _isDisposed || !_activeLightMap.ContainsKey(handle);
	public void Dispose(ResourceHandle<Light> handle) => Dispose(handle, removeFromMap: true);

	void Dispose(ResourceHandle<Light> handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		DisposeLight(handle).ThrowIfFailure();
		if (removeFromMap) _activeLightMap.Remove(handle);
	}

	public void Dispose() {
		try {
			if (_isDisposed) return;
			foreach (var kvp in _activeLightMap) Dispose(kvp.Key, removeFromMap: false);
			_activeLightMap.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposedOrIncorrectType(ResourceHandle<Light> handle, LightType type) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var actualType = _activeLightMap[handle].Type;
		if (actualType == type) return;

		throw new InvalidOperationException($"{handle} is valid but expected it to be a {type}; it was instead a {actualType}.");
	}
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Light> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Light));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}