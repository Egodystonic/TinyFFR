// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct SpotLight : ILight<SpotLight>, IEquatable<SpotLight> {
	public const float MaxBrightness = 1E+15f;
	public const float DefaultLumens = 1_250_000f;
	public static readonly Angle MinConeAngle = 1f;
	public static readonly Angle MaxConeAngle = 180f;

	#region ILight Impl
	readonly Light _base;
	internal Light Base => _base == default ? throw InvalidObjectException.InvalidDefault<SpotLight>() : _base;
	internal ILightImplProvider Implementation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Implementation;
	}
	internal ResourceHandle<Light> Handle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Handle;
	}
	ILightImplProvider IResource<Light, ILightImplProvider>.Implementation => Implementation;
	ResourceHandle<Light> IResource<Light>.Handle => Handle;
	internal SpotLight(Light @base) => _base = @base;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Base.Dispose();
	public static implicit operator Light(SpotLight operand) => operand.Base;
	public static explicit operator SpotLight(Light operand) {
		Light.ThrowIfInvalidType(operand, LightType.SpotLight);
		return new(operand);
	}
	static SpotLight ILight<SpotLight>.FromBaseLight(Light l) => (SpotLight) l;
	static Light IResource<Light>.CreateFromHandleAndImpl(ResourceHandle<Light> handle, IResourceImplProvider impl) {
		return new Light(handle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	public override string ToString() => $"Spot{Base}";
	#endregion

	#region Base Light Deferring Members
	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Name;
	}

	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Position;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Base.SetPosition(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Base.SetPosition(position);

	public ColorVect Color {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Color;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Base.SetColor(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColor(ColorVect color) => Base.SetColor(color);

	public float Brightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Brightness;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Base.SetBrightness(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetBrightness(float brightness) => Base.SetBrightness(brightness);

	public Angle ColorHue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.ColorHue;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Base.SetColorHue(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorHue(Angle hue) => Base.SetColorHue(hue);

	public float ColorSaturation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.ColorSaturation;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Base.SetColorSaturation(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorSaturation(float saturation) => Base.SetColorSaturation(saturation);

	public float ColorLightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.ColorLightness;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Base.SetColorLightness(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorLightness(float lightness) => Base.SetColorLightness(lightness);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => Base.MoveBy(translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustColorHueBy(Angle adjustment) => Base.AdjustColorHueBy(adjustment);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustColorSaturationBy(float adjustment) => Base.AdjustColorSaturationBy(adjustment);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustColorLightnessBy(float adjustment) => Base.AdjustColorLightnessBy(adjustment);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustBrightnessBy(float adjustment) => Base.AdjustBrightnessBy(adjustment);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBrightnessBy(float scalar) => Base.ScaleBrightnessBy(scalar);
	#endregion

	#region Equality
	bool IEquatable<Light>.Equals(Light other) => _base.Equals(other);
	public bool Equals(SpotLight other) => _base.Equals(other._base);
	public override bool Equals(object? obj) => obj is SpotLight other && Equals(other);
	public override int GetHashCode() => _base.GetHashCode();
	public static bool operator ==(SpotLight left, SpotLight right) => left.Equals(right);
	public static bool operator !=(SpotLight left, SpotLight right) => !left.Equals(right);
	#endregion

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
}