// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct SpotLight : ILight<SpotLight> {
	public const float MaxBrightness = 1E+15f;
	public const float DefaultLumens = 1_250_000f;
	public static readonly Angle MinConeAngle = 1f;
	public static readonly Angle MaxConeAngle = 180f;

	readonly ResourceHandle<SpotLight> _handle;
	readonly ILightImplProvider _impl;

	internal ResourceHandle<SpotLight> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(SpotLight)) : _handle;
	internal ILightImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<SpotLight>();

	ILightImplProvider IResource<SpotLight, ILightImplProvider>.Implementation => Implementation;
	ResourceHandle<SpotLight> IResource<SpotLight>.Handle => Handle;

	internal SpotLight(ResourceHandle<SpotLight> handle, ILightImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static SpotLight IResource<SpotLight>.CreateFromHandleAndImpl(ResourceHandle<SpotLight> handle, IResourceImplProvider impl) {
		return new SpotLight(handle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Light Type Casting
	public Light AsBaseLight() => new(_handle, _impl);

	public static implicit operator Light(SpotLight operand) => operand.AsBaseLight();
	public static explicit operator SpotLight(Light operand) => operand.As<SpotLight>();
	static SpotLight ILight<SpotLight>.FromBaseLight(Light l) {
		Light.ThrowIfInvalidType(l, LightType.SpotLight);
		return new((ResourceHandle<SpotLight>) l.Handle, l.Implementation);
	}
	public override string ToString() => AsBaseLight().ToString();
	#endregion

	#region Common Light Members
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	public void MoveBy(Vect translation) => Implementation.TranslateBy(_handle, translation);
	public void AdjustColorHueBy(Angle adjustment) => Color = Color.WithHueAdjustedBy(adjustment);
	public void AdjustColorSaturationBy(float adjustment) => Color = Color.WithSaturationAdjustedBy(adjustment);
	public void AdjustColorLightnessBy(float adjustment) => Color = Color.WithLightnessAdjustedBy(adjustment);
	public void AdjustBrightnessBy(float adjustment) => Implementation.AdjustBrightnessBy(_handle, adjustment);
	public void ScaleBrightnessBy(float scalar) => Implementation.ScaleBrightnessBy(_handle, scalar);
	#endregion

	#region SpotLight Specific
	public static LightType SelfType { get; } = LightType.SpotLight;
	public LightType Type {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => SelfType;
	}

	public float MaxIlluminationDistance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSpotLightMaxIlluminationDistance(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSpotLightMaxIlluminationDistance(Handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaxIlluminationDistance(float distance) => MaxIlluminationDistance = distance;

	public Direction ConeDirection {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSpotLightConeDirection(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSpotLightConeDirection(Handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetConeDirection(Direction direction) => ConeDirection = direction;

	public Angle ConeAngle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSpotLightConeAngle(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSpotLightConeAngle(Handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetConeAngle(Angle angle) => ConeAngle = angle;

	public Angle IntenseBeamAngle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSpotLightIntenseBeamAngle(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSpotLightIntenseBeamAngle(Handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetIntenseBeamAngle(Angle angle) => IntenseBeamAngle = angle;

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
	public bool Equals(SpotLight other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is ILight other && AsBaseLight().Equals(other.AsBaseLight());
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(SpotLight left, SpotLight right) => left.Equals(right);
	public static bool operator !=(SpotLight left, SpotLight right) => !left.Equals(right);
	public static bool operator ==(Light left, SpotLight right) => right.Equals(left);
	public static bool operator !=(Light left, SpotLight right) => !right.Equals(left);
	public static bool operator ==(SpotLight left, Light right) => left.Equals(right);
	public static bool operator !=(SpotLight left, Light right) => !left.Equals(right);
	#endregion
}