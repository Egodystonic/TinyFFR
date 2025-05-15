// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct PointLight : ILight<PointLight>, IPositionedSceneObject {
	public const float MaxBrightness = 1E+15f;
	public const float DefaultLumens = 1_250_000f;

	readonly ResourceHandle<PointLight> _handle;
	readonly ILightImplProvider _impl;

	internal ResourceHandle<PointLight> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(PointLight)) : _handle;
	internal ILightImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<PointLight>();

	ILightImplProvider IResource<PointLight, ILightImplProvider>.Implementation => Implementation;
	ResourceHandle<PointLight> IResource<PointLight>.Handle => Handle;

	internal PointLight(ResourceHandle<PointLight> handle, ILightImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static PointLight IResource<PointLight>.CreateFromHandleAndImpl(ResourceHandle<PointLight> handle, IResourceImplProvider impl) {
		return new PointLight(handle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Light Type Casting
	public Light AsBaseLight() => new(_handle, _impl);

	public static implicit operator Light(PointLight operand) => operand.AsBaseLight();
	public static explicit operator PointLight(Light operand) => operand.As<PointLight>();
	static PointLight ILight<PointLight>.FromBaseLight(Light l) {
		Light.ThrowIfInvalidType(l, LightType.Point);
		return new((ResourceHandle<PointLight>) l.Handle, l.Implementation);
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

	#region PointLight Specific
	static LightType ILight<PointLight>.SelfType { get; } = LightType.Point;
	LightType ILight.Type => LightType.Point;

	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPointLightPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPointLightPosition(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	public float MaxIlluminationRadius {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPointLightMaxIlluminationRadius(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPointLightMaxIlluminationRadius(Handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaxIlluminationRange(float range) => MaxIlluminationRadius = range;

	public void MoveBy(Vect translation) => Position += translation;

	public static float LumensToBrightness(float lumens) {
		if (!lumens.IsNonNegativeAndFinite()) return 0f;
		return Single.Min(MathF.Sqrt(lumens / DefaultLumens), MaxBrightness);
	}

	public static float BrightnessToLumens(float brightness) {
		return BrightnessToLumensNoClamp(ClampBrightnessToValidRange(brightness));
	}

	internal static float BrightnessToLumensNoClamp(float brightness) {
		return DefaultLumens * brightness * brightness;
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
	public bool Equals(PointLight other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is ILight other && AsBaseLight().Equals(other.AsBaseLight());
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(PointLight left, PointLight right) => left.Equals(right);
	public static bool operator !=(PointLight left, PointLight right) => !left.Equals(right);
	public static bool operator ==(Light left, PointLight right) => right.Equals(left);
	public static bool operator !=(Light left, PointLight right) => !right.Equals(left);
	public static bool operator ==(PointLight left, Light right) => left.Equals(right);
	public static bool operator !=(PointLight left, Light right) => !left.Equals(right);
	#endregion
}