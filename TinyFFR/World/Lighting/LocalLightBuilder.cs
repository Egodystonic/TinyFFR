// Created on 2024-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed class LocalLightBuilder : ILightBuilder, ILightImplProvider, IDisposable {
	readonly record struct LightData(LightType Type, nint TypeHandle, float Brightness, Angle SpotLightInner, Angle SpotLightOuter);
	const string DefaultLightName = "Unnamed Light";
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPoolBackedMap<ResourceHandle, LightData> _activeLightMap = new();
	bool _isDisposed = false;

	public LocalLightBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	public PointLight CreatePointLight(in PointLightCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		AllocatePointLight(out var handle).ThrowIfFailure();
		var resHandle = new ResourceHandle<PointLight>(handle);
		SetUpBaseLight(config.BaseConfig, resHandle.Ident, LightType.Point);
		SetUniversalBrightness(resHandle, config.InitialBrightness);
		SetPointLightPosition(resHandle, config.InitialPosition);
		SetPointLightMaxIlluminationRadius(resHandle, config.InitialMaxIlluminationRadius);
		return HandleToInstance(resHandle);
	}

	public SpotLight CreateSpotLight(in SpotLightCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		AllocateSpotLight(config.IsHighQuality, out var handle).ThrowIfFailure();
		var resHandle = new ResourceHandle<SpotLight>(handle);
		var beamAngle = config.InitialIntenseBeamAngle;
		var coneAngle = config.InitialConeAngle;
		AdjustSpotlightAngles(ref coneAngle, ref beamAngle, adjustingCone: true);
		SetUpBaseLight(config.BaseConfig, resHandle.Ident, LightType.Spot, beamAngle, coneAngle);
		SetUniversalBrightness(resHandle, config.InitialBrightness);
		SetSpotLightPosition(resHandle, config.InitialPosition);
		SetSpotLightMaxIlluminationDistance(resHandle, config.InitialMaxIlluminationDistance);
		SetSpotLightConeDirection(resHandle, config.InitialConeDirection);
		SetSpotLightRadii(handle, ConvertSpotLightAngleToFilamentAngle(beamAngle), ConvertSpotLightAngleToFilamentAngle(coneAngle));
		return HandleToInstance(resHandle);
	}

	public DirectionalLight CreateDirectionalLight(in DirectionalLightCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		AllocateSunLight(config.ShowSunDisc, out var handle).ThrowIfFailure();
		var resHandle = new ResourceHandle<DirectionalLight>(handle);
		SetUpBaseLight(config.BaseConfig, resHandle.Ident, LightType.Directional);
		SetUniversalBrightness(resHandle, config.InitialBrightness);
		SetDirectionalLightDirection(resHandle, config.InitialDirection);
		return HandleToInstance(resHandle);
	}

	void SetUpBaseLight(in LightCreationConfig config, ResourceIdent ident, LightType lightType, Angle? spotLightInner = null, Angle? spotLightOuter = null) {
		_activeLightMap.Add(ident.RawResourceHandle, new(lightType, ident.TypeHandle, config.InitialBrightness, spotLightInner ?? Angle.Zero, spotLightOuter ?? Angle.Zero));
		_globals.StoreResourceNameIfNotEmpty(ident, config.Name);
		SetLightColor(ident.RawResourceHandle, config.InitialColor.ToVector3());
		SetLightShadowCaster(ident.RawResourceHandle, config.CastsShadows);
	}

	public LightType GetType(ResourceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeLightMap[handle].Type;
	}

	public ColorVect GetColor(ResourceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetLightColor(handle, out var result);
		return ColorVect.FromVector3(result);
	}
	public void SetColor(ResourceHandle handle, ColorVect newColor) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetLightColor(handle, newColor.ToVector3()).ThrowIfFailure();
	}

	public float GetUniversalBrightness(ResourceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeLightMap[handle].Brightness;
	}
	public void SetUniversalBrightness(ResourceHandle handle, float newBrightness) {
		ThrowIfThisOrHandleIsDisposed(handle);

		switch (_activeLightMap[handle].Type) {
			case LightType.Point:
				newBrightness = PointLight.ClampBrightnessToValidRange(newBrightness);
				var pointLightLumens = PointLight.BrightnessToLumensNoClamp(newBrightness);
				SetPointLightLumens(handle, pointLightLumens).ThrowIfFailure();
				break;
			case LightType.Spot:
				newBrightness = SpotLight.ClampBrightnessToValidRange(newBrightness);
				var spotLightLumens = SpotLight.BrightnessToLumensNoClamp(newBrightness);
				SetSpotLightLumens(handle, spotLightLumens).ThrowIfFailure();
				break;
			case LightType.Directional:
				newBrightness = DirectionalLight.ClampBrightnessToValidRange(newBrightness);
				var directionalLightLumens = DirectionalLight.BrightnessToLuxNoClamp(newBrightness);
				SetSunLightLux(handle, directionalLightLumens).ThrowIfFailure();
				break;
		}
		_activeLightMap[handle] = _activeLightMap[handle] with { Brightness = newBrightness };
	}

	public void AdjustBrightnessBy(ResourceHandle handle, float adjustment) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetUniversalBrightness(handle, _activeLightMap[handle].Brightness + adjustment);
	}
	public void ScaleBrightnessBy(ResourceHandle handle, float scalar) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetUniversalBrightness(handle, _activeLightMap[handle].Brightness * scalar);
	}

	public bool GetIsShadowCaster(ResourceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetLightShadowCaster(handle, out var result).ThrowIfFailure();
		return result;
	}
	public void SetIsShadowCaster(ResourceHandle handle, bool isShadowCaster) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetLightShadowCaster(handle, isShadowCaster).ThrowIfFailure();
	}

	public void SetShadowFidelity(ResourceHandle handle, LightShadowFidelityData fidelityArgs) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetLightShadowFidelity(handle, fidelityArgs.MapSize, fidelityArgs.CascadeCount).ThrowIfFailure();
	}

	public Location GetPointLightPosition(ResourceHandle<PointLight> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetLightPosition(handle, out var result).ThrowIfFailure();
		return Location.FromVector3(result);
	}
	public void SetPointLightPosition(ResourceHandle<PointLight> handle, Location newPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetLightPosition(handle, newPosition.ToVector3()).ThrowIfFailure();
	}

	public float GetPointLightMaxIlluminationRadius(ResourceHandle<PointLight> handle) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Point);
		GetPointLightMaxIlluminationRadius(handle, out var result).ThrowIfFailure();
		return result;
	}
	public void SetPointLightMaxIlluminationRadius(ResourceHandle<PointLight> handle, float newRadius) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Point);
		if (!newRadius.IsNonNegativeAndFinite()) newRadius = 0f;
		LocalLightBuilder.SetPointLightMaxIlluminationRadius(handle, newRadius).ThrowIfFailure();
	}

	public Location GetSpotLightPosition(ResourceHandle<SpotLight> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetLightPosition(handle, out var result).ThrowIfFailure();
		return Location.FromVector3(result);
	}
	public void SetSpotLightPosition(ResourceHandle<SpotLight> handle, Location newPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetLightPosition(handle, newPosition.ToVector3()).ThrowIfFailure();
	}

	public float GetSpotLightMaxIlluminationDistance(ResourceHandle<SpotLight> handle) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Spot);
		GetSpotLightMaxDistance(handle, out var result).ThrowIfFailure();
		return result;
	}
	public void SetSpotLightMaxIlluminationDistance(ResourceHandle<SpotLight> handle, float newDistance) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Spot);
		if (!newDistance.IsNonNegativeAndFinite()) newDistance = 0f;
		SetSpotLightMaxDistance(handle, newDistance).ThrowIfFailure();
	}

	public Direction GetSpotLightConeDirection(ResourceHandle<SpotLight> handle) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Spot);
		GetSpotLightDirection(handle, out var result).ThrowIfFailure();
		return Direction.FromVector3PreNormalized(result);
	}
	public void SetSpotLightConeDirection(ResourceHandle<SpotLight> handle, Direction newDirection) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Spot);
		if (newDirection == Direction.None) newDirection = SpotLightCreationConfig.DefaultInitialConeDirection;
		SetSpotLightDirection(handle, newDirection.ToVector3()).ThrowIfFailure();
	}

	public Angle GetSpotLightConeAngle(ResourceHandle<SpotLight> handle) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Spot);
		return _activeLightMap[handle].SpotLightOuter;
	}
	public void SetSpotLightConeAngle(ResourceHandle<SpotLight> handle, Angle coneAngle) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Spot);
		var curLightParams = _activeLightMap[handle];
		var beamAngle = curLightParams.SpotLightInner;
		AdjustSpotlightAngles(ref coneAngle, ref beamAngle, adjustingCone: true);
		_activeLightMap[handle] = curLightParams with { SpotLightOuter = coneAngle, SpotLightInner = beamAngle };
		
		SetSpotLightRadii(handle, ConvertSpotLightAngleToFilamentAngle(beamAngle), ConvertSpotLightAngleToFilamentAngle(coneAngle)).ThrowIfFailure();
	}

	public Angle GetSpotLightIntenseBeamAngle(ResourceHandle<SpotLight> handle) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Spot);
		return _activeLightMap[handle].SpotLightInner;
	}
	public void SetSpotLightIntenseBeamAngle(ResourceHandle<SpotLight> handle, Angle beamAngle) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Spot);
		var curLightParams = _activeLightMap[handle];
		var coneAngle = curLightParams.SpotLightOuter;
		AdjustSpotlightAngles(ref coneAngle, ref beamAngle, adjustingCone: false);
		_activeLightMap[handle] = curLightParams with { SpotLightOuter = coneAngle, SpotLightInner = beamAngle };

		SetSpotLightRadii(handle, ConvertSpotLightAngleToFilamentAngle(beamAngle), ConvertSpotLightAngleToFilamentAngle(coneAngle)).ThrowIfFailure();
	}

	static void AdjustSpotlightAngles(ref Angle coneAngle, ref Angle beamAngle, bool adjustingCone) {
		if (coneAngle < SpotLight.MinConeAngle) coneAngle = SpotLight.MinConeAngle;
		else if (coneAngle > SpotLight.MaxConeAngle) coneAngle = SpotLight.MaxConeAngle;

		if (beamAngle < SpotLight.MinConeAngle) beamAngle = SpotLight.MinConeAngle;
		else if (beamAngle > SpotLight.MaxConeAngle) beamAngle = SpotLight.MaxConeAngle;

		if (beamAngle > coneAngle) {
			if (adjustingCone) beamAngle = coneAngle;
			else coneAngle = beamAngle;
		}
	}
	static float ConvertSpotLightAngleToFilamentAngle(Angle angle) => angle.Radians * 0.5f;

	public Direction GetDirectionalLightDirection(ResourceHandle<DirectionalLight> handle) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Directional);
		GetSunLightDirection(handle, out var result).ThrowIfFailure();
		return Direction.FromVector3PreNormalized(result);
	}
	public void SetDirectionalLightDirection(ResourceHandle<DirectionalLight> handle, Direction newDirection) {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Directional);
		SetSunLightDirection(handle, newDirection.ToVector3()).ThrowIfFailure();
	}

	public void SetDirectionalLightSunDiscParameters(ResourceHandle<DirectionalLight> handle, SunDiscConfig config) {
		const float DefaultAngularSizeDegrees = 0.545f;
		const float DefaultHaloCoefficient = 10f;
		const float DefaultHaloFalloffExponent = 80f;
		
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(handle, LightType.Directional);

		SetSunParameters(
			handle,
			DefaultAngularSizeDegrees * Single.Clamp(config.Scaling, 0f, 36f),
			DefaultHaloCoefficient * MathF.Sqrt(Single.Clamp(config.FringingScaling, 0.05f, 10f)),
			DefaultHaloFalloffExponent / Single.Clamp(config.FringingOuterRadiusScaling, 0.05f, 10f)
		).ThrowIfFailure();
	}

	public string GetNameAsNewStringObject(ResourceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(RawHandleToIdent(handle), DefaultLightName));
	}
	public int GetNameLength(ResourceHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(RawHandleToIdent(handle), DefaultLightName).Length;
	}
	public void CopyName(ResourceHandle handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(RawHandleToIdent(handle), DefaultLightName, destinationBuffer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	PointLight HandleToInstance(ResourceHandle<PointLight> h) => new(h, this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	SpotLight HandleToInstance(ResourceHandle<SpotLight> h) => new(h, this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	DirectionalLight HandleToInstance(ResourceHandle<DirectionalLight> h) => new(h, this);

	#region Native Methods
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_light_shadow_caster")]
	static extern InteropResult GetLightShadowCaster(
		UIntPtr lightHandle,
		out InteropBool outIsShadowCaster
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_light_shadow_caster")]
	static extern InteropResult SetLightShadowCaster(
		UIntPtr lightHandle,
		InteropBool isShadowCaster
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_light_shadow_fidelity")]
	static extern InteropResult SetLightShadowFidelity(
		UIntPtr lightHandle,
		uint mapSize,
		byte cascadeCount
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



	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_point_light")]
	static extern InteropResult AllocatePointLight(
		out UIntPtr outLightHandle
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



	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_spot_light")]
	static extern InteropResult AllocateSpotLight(
		InteropBool highAccuracy,
		out UIntPtr outLightHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_spot_light_lumens")]
	static extern InteropResult GetSpotLightLumens(
		UIntPtr lightHandle,
		out float outLumens
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_spot_light_lumens")]
	static extern InteropResult SetSpotLightLumens(
		UIntPtr lightHandle,
		float newLumens
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_spot_light_direction")]
	static extern InteropResult GetSpotLightDirection(
		UIntPtr lightHandle,
		out Vector3 outDirection
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_spot_light_direction")]
	static extern InteropResult SetSpotLightDirection(
		UIntPtr lightHandle,
		Vector3 newDirection
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_spot_light_radii")]
	static extern InteropResult GetSpotLightRadii(
		UIntPtr lightHandle,
		out float outInnerRadius,
		out float outOuterRadius
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_spot_light_radii")]
	static extern InteropResult SetSpotLightRadii(
		UIntPtr lightHandle,
		float newInnerRadius,
		float newOuterRadius
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_spot_light_max_distance")]
	static extern InteropResult GetSpotLightMaxDistance(
		UIntPtr lightHandle,
		out float outDistance
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_spot_light_max_distance")]
	static extern InteropResult SetSpotLightMaxDistance(
		UIntPtr lightHandle,
		float newDistance
	);





	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_sun_light")]
	static extern InteropResult AllocateSunLight(
		InteropBool includeSunDisc,
		out UIntPtr outLightHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_sun_light_lux")]
	static extern InteropResult GetSunLightLux(
		UIntPtr lightHandle,
		out float outLux
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_sun_light_lux")]
	static extern InteropResult SetSunLightLux(
		UIntPtr lightHandle,
		float newLux
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_sun_light_direction")]
	static extern InteropResult GetSunLightDirection(
		UIntPtr lightHandle,
		out Vector3 outDirection
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_sun_light_direction")]
	static extern InteropResult SetSunLightDirection(
		UIntPtr lightHandle,
		Vector3 newDirection
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_sun_parameters")]
	static extern InteropResult SetSunParameters(
		UIntPtr lightHandle,
		float angularSize,
		float haloCoefficient,
		float haloFalloffExponent
	);




	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_light")]
	static extern InteropResult DisposeLight(
		UIntPtr lightHandle
	);
	#endregion

	#region Disposal
	public bool IsDisposed(ResourceHandle handle) => _isDisposed || !_activeLightMap.ContainsKey(handle);
	public void Dispose(ResourceHandle handle) => Dispose(handle, removeFromMap: true);

	void Dispose(ResourceHandle handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		switch (_activeLightMap[handle].Type) {
			case LightType.Point:
				_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance((ResourceHandle<PointLight>) handle));
				break;
			case LightType.Spot:
				_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance((ResourceHandle<SpotLight>) handle));
				break;
			case LightType.Directional:
				_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance((ResourceHandle<DirectionalLight>) handle));
				break;
			default:
				throw new InvalidOperationException($"Unexpected light type '{_activeLightMap[handle].Type}'.");
		}
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

	ResourceIdent RawHandleToIdent(ResourceHandle rawHandle) => new(_activeLightMap[rawHandle].TypeHandle, rawHandle);
	ResourceHandle<T> SpecifyHandleTypeWithCheck<T>(ResourceHandle rawHandle) where T : ILight<T> {
		ThrowIfThisOrHandleIsDisposedOrIncorrectType(rawHandle, T.SelfType);
		return (ResourceHandle<T>) rawHandle;
	}
	void ThrowIfThisOrHandleIsDisposedOrIncorrectType(ResourceHandle handle, LightType type) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var actualType = _activeLightMap[handle].Type;
		if (actualType == type) return;

		throw new InvalidOperationException($"{handle} is valid but expected it to be a {type}; it was instead a {actualType}.");
	}
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Light));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}