// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct DirectionalLight : ILight<DirectionalLight>, IOrientedSceneObject {
	public const float MaxBrightness = 1E+15f;
	public const float DefaultLux = 125_000f;

	readonly ResourceHandle<DirectionalLight> _handle;
	readonly ILightImplProvider _impl;

	internal ResourceHandle<DirectionalLight> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(DirectionalLight)) : _handle;
	internal ILightImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<DirectionalLight>();

	ILightImplProvider IResource<DirectionalLight, ILightImplProvider>.Implementation => Implementation;
	ResourceHandle<DirectionalLight> IResource<DirectionalLight>.Handle => Handle;

	internal DirectionalLight(ResourceHandle<DirectionalLight> handle, ILightImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static DirectionalLight IResource<DirectionalLight>.CreateFromHandleAndImpl(ResourceHandle<DirectionalLight> handle, IResourceImplProvider impl) {
		return new DirectionalLight(handle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Light Type Casting
	public Light AsBaseLight() => new(_handle, _impl);

	public static implicit operator Light(DirectionalLight operand) => operand.AsBaseLight();
	public static explicit operator DirectionalLight(Light operand) => operand.As<DirectionalLight>();
	static DirectionalLight ILight<DirectionalLight>.FromBaseLight(Light l) {
		Light.ThrowIfInvalidType(l, LightType.Directional);
		return new((ResourceHandle<DirectionalLight>) l.Handle, l.Implementation);
	}
	public override string ToString() => AsBaseLight().ToString();
	#endregion

	#region Common Light Members
	public ColorVect Color {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetColor(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetColor(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColor(ColorVect color) => Color = color;

	public Angle ColorHue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Color.Hue;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Color = Color.WithHue(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorHue(Angle hue) => ColorHue = hue;

	public float ColorSaturation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Color.Saturation;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Color = Color.WithSaturation(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorSaturation(float saturation) => ColorSaturation = saturation;

	public float ColorLightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Color.Lightness;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Color = Color.WithLightness(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorLightness(float lightness) => ColorLightness = lightness;

	public float Brightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetUniversalBrightness(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetUniversalBrightness(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetBrightness(float brightness) => Brightness = brightness;

	public bool CastsShadows {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIsShadowCaster(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetIsShadowCaster(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetCastsShadows(bool castsShadows) => CastsShadows = castsShadows;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	public void AdjustColorHueBy(Angle adjustment) => Color = Color.WithHueAdjustedBy(adjustment);
	public void AdjustColorSaturationBy(float adjustment) => Color = Color.WithSaturationAdjustedBy(adjustment);
	public void AdjustColorLightnessBy(float adjustment) => Color = Color.WithLightnessAdjustedBy(adjustment);
	public void AdjustBrightnessBy(float adjustment) => Implementation.AdjustBrightnessBy(_handle, adjustment);
	public void ScaleBrightnessBy(float scalar) => Implementation.ScaleBrightnessBy(_handle, scalar);

	void ILight.SetShadowFidelity(LightShadowFidelityData fidelityArgs) => SetShadowFidelity(fidelityArgs);
	internal void SetShadowFidelity(LightShadowFidelityData fidelityArgs) => Implementation.SetShadowFidelity(_handle, fidelityArgs);
	#endregion

	#region DirectionalLight Specific
	static LightType ILight<DirectionalLight>.SelfType { get; } = LightType.Directional;
	LightType ILight.Type => LightType.Directional;

	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDirectionalLightDirection(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetDirectionalLightDirection(Handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetDirection(Direction direction) => Direction = direction;

	Rotation IOrientedSceneObject.Rotation {
		get => Rotation.FromStartAndEndDirection(DirectionalLightCreationConfig.DefaultInitialDirection, Direction);
		set => Direction = DirectionalLightCreationConfig.DefaultInitialDirection * value;
	}

	public void RotateBy(Rotation rotation) => Direction *= rotation;

	public void SetSunDiscParameters(SunDiscConfig config) => Implementation.SetDirectionalLightSunDiscParameters(Handle, config);

	public static float LuxToBrightness(float lux) {
		if (!lux.IsNonNegativeAndFinite()) return 0f;
		return Single.Min(lux / DefaultLux, MaxBrightness);
	}

	public static float BrightnessToLux(float brightness) {
		return BrightnessToLuxNoClamp(ClampBrightnessToValidRange(brightness));
	}

	internal static float BrightnessToLuxNoClamp(float brightness) {
		return DefaultLux * brightness;
	}

	internal static float ClampBrightnessToValidRange(float input) {
		if (!input.IsNonNegativeAndFinite()) return 0f;
		return Single.Min(input, MaxBrightness);
	}
	#endregion

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	public bool Equals(Light other) => AsBaseLight().Equals(other);
	public bool Equals(DirectionalLight other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is ILight other && AsBaseLight().Equals(other.AsBaseLight());
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(DirectionalLight left, DirectionalLight right) => left.Equals(right);
	public static bool operator !=(DirectionalLight left, DirectionalLight right) => !left.Equals(right);
	public static bool operator ==(Light left, DirectionalLight right) => right.Equals(left);
	public static bool operator !=(Light left, DirectionalLight right) => !right.Equals(left);
	public static bool operator ==(DirectionalLight left, Light right) => left.Equals(right);
	public static bool operator !=(DirectionalLight left, Light right) => !left.Equals(right);
	#endregion
}