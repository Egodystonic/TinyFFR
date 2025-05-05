// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct PointLight : ILight<PointLight>, IEquatable<PointLight> {
	public const float MaxBrightness = 1E+15f;
	public const float DefaultLumens = 1_250_000f;

	#region ILight Impl
	readonly Light _base;
	internal Light Base => _base == default ? throw InvalidObjectException.InvalidDefault<PointLight>() : _base;
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
	internal PointLight(Light @base) => _base = @base;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Base.Dispose();
	public static implicit operator Light(PointLight operand) => operand.Base;
	public static explicit operator PointLight(Light operand) {
		Light.ThrowIfInvalidType(operand, LightType.PointLight);
		return new(operand);
	}
	static PointLight ILight<PointLight>.FromBaseLight(Light l) => (PointLight) l;
	static Light IResource<Light>.CreateFromHandleAndImpl(ResourceHandle<Light> handle, IResourceImplProvider impl) {
		return new Light(handle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	public override string ToString() => $"Point{Base}";
	#endregion

	#region Base Light Deferring Members
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Base.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Base.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Base.CopyName(destinationBuffer);
	#endregion

	#region Equality
	bool IEquatable<Light>.Equals(Light other) => _base.Equals(other);
	public bool Equals(PointLight other) => _base.Equals(other._base);
	public override bool Equals(object? obj) => obj is PointLight other && Equals(other);
	public override int GetHashCode() => _base.GetHashCode();
	public static bool operator ==(PointLight left, PointLight right) => left.Equals(right);
	public static bool operator !=(PointLight left, PointLight right) => !left.Equals(right);
	#endregion

	public float MaxIlluminationRadius {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPointLightMaxIlluminationRadius(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPointLightMaxIlluminationRadius(Handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaxIlluminationRange(float range) => MaxIlluminationRadius = range;

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