﻿// Created on 2024-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

partial struct ColorVect : 
	IAdditive<ColorVect, ColorVect, ColorVect>,
	IScalable<ColorVect> {
	public ColorVect WithHue(Angle newHue) {
		ToHueSaturationLightness(out _, out var s, out var l);
		return FromHueSaturationLightness(newHue, s, l, Alpha);
	}
	public ColorVect WithSaturation(float newSaturation) {
		ToHueSaturationLightness(out var h, out _, out var l);
		return FromHueSaturationLightness(h, newSaturation, l, Alpha);
	}
	public ColorVect WithLightness(float newLightness) {
		ToHueSaturationLightness(out var h, out var s, out _);
		return FromHueSaturationLightness(h, s, newLightness, Alpha);
	}

	public ColorVect WithHueAdjustedBy(Angle adjustment) {
		ToHueSaturationLightness(out var h, out var s, out var l);
		return FromHueSaturationLightness(h + adjustment, s, l, Alpha);
	}
	public ColorVect WithSaturationAdjustedBy(float adjustment) {
		ToHueSaturationLightness(out var h, out var s, out var l);
		return FromHueSaturationLightness(h, s + adjustment, l, Alpha);
	}
	public ColorVect WithLightnessAdjustedBy(float adjustment) {
		ToHueSaturationLightness(out var h, out var s, out var l);
		return FromHueSaturationLightness(h, s, l + adjustment, Alpha);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect operator +(ColorVect left, ColorVect right) => left.Plus(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect operator -(ColorVect left, ColorVect right) => left.Minus(right);
	public ColorVect Plus(ColorVect other) => PlusWithoutNormalization(other).ClampToNormalizedRange();
	public ColorVect Minus(ColorVect other) => MinusWithoutNormalization(other).ClampToNormalizedRange();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect PlusWithoutNormalization(ColorVect other) => new(AsVector4 + other.AsVector4);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect MinusWithoutNormalization(ColorVect other) => new(AsVector4 - other.AsVector4);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect operator *(ColorVect left, float right) => left.ScaledBy(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect operator /(ColorVect left, float right) => new(left.AsVector4 / right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect operator *(float left, ColorVect right) => right.ScaledBy(left);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect ScaledBy(float scalar) => ScaledBy(scalar, includeAlpha: false);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect ScaledWithoutNormalizationBy(float scalar) => ScaledWithoutNormalizationBy(scalar, includeAlpha: false);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect ScaledBy(float scalar, bool includeAlpha) => ScaledWithoutNormalizationBy(scalar, includeAlpha).ClampToNormalizedRange(includeAlpha);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect ScaledWithoutNormalizationBy(float scalar, bool includeAlpha) {
		var result = new ColorVect(AsVector4 * scalar);
		if (!includeAlpha) result = result with { Alpha = Alpha };
		return result;
	}

	#region Clamping and Interpolation
	public ColorVect Clamp(ColorVect min, ColorVect max) {
		return new(
			max.Red < min.Red ? Single.Clamp(Red, max.Red, min.Red) : Single.Clamp(Red, min.Red, max.Red),
			max.Green < min.Green ? Single.Clamp(Green, max.Green, min.Green) : Single.Clamp(Green, min.Green, max.Green),
			max.Blue < min.Blue ? Single.Clamp(Blue, max.Blue, min.Blue) : Single.Clamp(Blue, min.Blue, max.Blue),
			max.Alpha < min.Alpha ? Single.Clamp(Alpha, max.Alpha, min.Alpha) : Single.Clamp(Alpha, min.Alpha, max.Alpha)
		);
	}
	public ColorVect ClampToNormalizedRange() {
		return new(
			Single.Clamp(Red, 0f, 1f),
			Single.Clamp(Green, 0f, 1f),
			Single.Clamp(Blue, 0f, 1f),
			Single.Clamp(Alpha, 0f, 1f)
		);
	}
	public ColorVect ClampToNormalizedRange(bool includeAlpha) {
		if (includeAlpha) return ClampToNormalizedRange();
		return new(
			Single.Clamp(Red, 0f, 1f),
			Single.Clamp(Green, 0f, 1f),
			Single.Clamp(Blue, 0f, 1f),
			Alpha
		);
	}

	public static ColorVect Interpolate(ColorVect start, ColorVect end, float distance) {
		return new(
			Single.Lerp(start.Red, end.Red, distance),
			Single.Lerp(start.Green, end.Green, distance),
			Single.Lerp(start.Blue, end.Blue, distance),
			Single.Lerp(start.Alpha, end.Alpha, distance)
		);
	}
	#endregion
}