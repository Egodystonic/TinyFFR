// Created on 2024-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

partial struct ColorVect : 
	IAdditive<ColorVect, ColorVect, ColorVect>,
	IScalable<ColorVect> {
	/// <summary>
	/// Returns a new colour which is the same as this one but with its <see cref="Hue"/> set to <paramref name="newHue"/>.
	/// </summary>
	/// <remarks>
	/// Every access to <see cref="WithHue"/>, <see cref="WithSaturation"/>, or <see cref="WithLightness"/> requires a full
	/// RGB-&gt;HSL conversion <i>and back again</i>. Consider using <see cref="ToHueSaturationLightness"/> and <see cref="FromHueSaturationLightness(Angle, float, float)"/>
	/// if you intend to modify more than one of these properties at the same time.
	/// </remarks>
	public ColorVect WithHue(Angle newHue) {
		ToHueSaturationLightness(out _, out var s, out var l);
		return FromHueSaturationLightness(newHue, s, l, Alpha);
	}
	/// <summary>
	/// Returns a new colour which is the same as this one but with its <see cref="Saturation"/> set to <paramref name="newSaturation"/>.
	/// </summary>
	/// <remarks>
	/// Every access to <see cref="WithHue"/>, <see cref="WithSaturation"/>, or <see cref="WithLightness"/> requires a full
	/// RGB-&gt;HSL conversion <i>and back again</i>. Consider using <see cref="ToHueSaturationLightness"/> and <see cref="FromHueSaturationLightness(Angle, float, float)"/>
	/// if you intend to modify more than one of these properties at the same time.
	/// </remarks>
	/// <param name="newSaturation">The new saturation, clamped to <c>[0, 1]</c>.</param>
	public ColorVect WithSaturation(float newSaturation) {
		ToHueSaturationLightness(out var h, out _, out var l);
		return FromHueSaturationLightness(h, newSaturation, l, Alpha);
	}
	/// <summary>
	/// Returns a new colour which is the same as this one but with its <see cref="Lightness"/> set to <paramref name="newLightness"/>.
	/// </summary>
	/// <remarks>
	/// Every access to <see cref="WithHue"/>, <see cref="WithSaturation"/>, or <see cref="WithLightness"/> requires a full
	/// RGB-&gt;HSL conversion <i>and back again</i>. Consider using <see cref="ToHueSaturationLightness"/> and <see cref="FromHueSaturationLightness(Angle, float, float)"/>
	/// if you intend to modify more than one of these properties at the same time.
	/// </remarks>
	/// <param name="newLightness">The new lightness, clamped to <c>[0, 1]</c>.</param>
	public ColorVect WithLightness(float newLightness) {
		ToHueSaturationLightness(out var h, out var s, out _);
		return FromHueSaturationLightness(h, s, newLightness, Alpha);
	}

	/// <summary>
	/// Returns a new colour which is the same as this one but with <paramref name="adjustment"/> added to its <see cref="Hue"/>.
	/// </summary>
	/// <remarks>
	/// This requires a full RGB-&gt;HSL conversion <i>and back again</i>; see the remarks on <see cref="WithHue"/>.
	/// </remarks>
	/// <param name="adjustment">The amount to add to <see cref="Hue"/>.</param>
	public ColorVect WithHueAdjustedBy(Angle adjustment) {
		ToHueSaturationLightness(out var h, out var s, out var l);
		return FromHueSaturationLightness(h + adjustment, s, l, Alpha);
	}
	/// <summary>
	/// Returns a new colour which is the same as this one but with <paramref name="adjustment"/> added to its <see cref="Saturation"/>.
	/// </summary>
	/// <remarks>
	/// This requires a full RGB-&gt;HSL conversion <i>and back again</i>; see the remarks on <see cref="WithHue"/>. The result is clamped to <c>[0, 1]</c>.
	/// </remarks>
	/// <param name="adjustment">The amount to add to <see cref="Saturation"/>.</param>
	public ColorVect WithSaturationAdjustedBy(float adjustment) {
		ToHueSaturationLightness(out var h, out var s, out var l);
		return FromHueSaturationLightness(h, s + adjustment, l, Alpha);
	}
	/// <summary>
	/// Returns a new colour which is the same as this one but with <paramref name="adjustment"/> added to its <see cref="Lightness"/>.
	/// </summary>
	/// <remarks>
	/// This requires a full RGB-&gt;HSL conversion <i>and back again</i>; see the remarks on <see cref="WithHue"/>. The result is clamped to <c>[0, 1]</c>.
	/// </remarks>
	/// <param name="adjustment">The amount to add to <see cref="Lightness"/>.</param>
	public ColorVect WithLightnessAdjustedBy(float adjustment) {
		ToHueSaturationLightness(out var h, out var s, out var l);
		return FromHueSaturationLightness(h, s, l + adjustment, Alpha);
	}

	/// <summary>
	/// Adds together <paramref name="left"/> and <paramref name="right"/>, clamping the result to <c>[0, 1]</c> per channel; equivalent to <c>left.Plus(right)</c>.
	/// </summary>
	/// <param name="left">The left-hand operand.</param>
	/// <param name="right">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect operator +(ColorVect left, ColorVect right) => left.Plus(right);
	/// <summary>
	/// Subtracts <paramref name="right"/> from <paramref name="left"/>, clamping the result to <c>[0, 1]</c> per channel; equivalent to <c>left.Minus(right)</c>.
	/// </summary>
	/// <param name="left">The left-hand operand.</param>
	/// <param name="right">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect operator -(ColorVect left, ColorVect right) => left.Minus(right);
	/// <summary>
	/// Adds <paramref name="other"/> to this colour, clamping the result to <c>[0, 1]</c> per channel.
	/// </summary>
	/// <remarks>
	/// Use <see cref="PlusWithoutNormalization"/> instead if you want the raw, unclamped result.
	/// </remarks>
	/// <param name="other">The value to add.</param>
	public ColorVect Plus(ColorVect other) => PlusWithoutNormalization(other).ClampToNormalizedRange();
	/// <summary>
	/// Subtracts <paramref name="other"/> from this colour, clamping the result to <c>[0, 1]</c> per channel.
	/// </summary>
	/// <remarks>
	/// Use <see cref="MinusWithoutNormalization"/> instead if you want the raw, unclamped result.
	/// </remarks>
	/// <param name="other">The value to subtract.</param>
	public ColorVect Minus(ColorVect other) => MinusWithoutNormalization(other).ClampToNormalizedRange();
	/// <summary>
	/// Adds <paramref name="other"/> to this colour without clamping the result, which may leave one or more channels outside <c>[0, 1]</c>.
	/// </summary>
	/// <param name="other">The value to add.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect PlusWithoutNormalization(ColorVect other) => new(AsVector4 + other.AsVector4);
	/// <summary>
	/// Subtracts <paramref name="other"/> from this colour without clamping the result, which may leave one or more channels outside <c>[0, 1]</c>.
	/// </summary>
	/// <param name="other">The value to subtract.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect MinusWithoutNormalization(ColorVect other) => new(AsVector4 - other.AsVector4);

	/// <summary>
	/// Scales <paramref name="left"/> by <paramref name="right"/> (excluding <see cref="Alpha"/>), clamping the result to <c>[0, 1]</c>; equivalent to <c>left.ScaledBy(right)</c>.
	/// </summary>
	/// <param name="left">The colour to scale.</param>
	/// <param name="right">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect operator *(ColorVect left, float right) => left.ScaledBy(right);
	/// <summary>
	/// Divides <paramref name="left"/> by <paramref name="right"/>, including <see cref="Alpha"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <c>operator *</c> and <see cref="ScaledBy(float)"/>, this does not clamp its result: the returned value's channels may fall outside <c>[0, 1]</c>.
	/// </remarks>
	/// <param name="left">The colour to divide.</param>
	/// <param name="right">The divisor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect operator /(ColorVect left, float right) => new(left.AsVector4 / right);
	/// <inheritdoc cref="operator *(ColorVect,float)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect operator *(float left, ColorVect right) => right.ScaledBy(left);
	/// <summary>
	/// Scales this colour's <see cref="Red"/>, <see cref="Green"/>, and <see cref="Blue"/> channels by <paramref name="scalar"/>, leaving <see cref="Alpha"/> unchanged, and clamps the result to <c>[0, 1]</c>.
	/// </summary>
	/// <remarks>
	/// Equivalent to <c>ScaledBy(scalar, includeAlpha: false)</c>. Use <see cref="ScaledWithoutNormalizationBy(float)"/> instead if you want the raw, unclamped result.
	/// </remarks>
	/// <param name="scalar">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect ScaledBy(float scalar) => ScaledBy(scalar, includeAlpha: false);
	/// <summary>
	/// Scales this colour's <see cref="Red"/>, <see cref="Green"/>, and <see cref="Blue"/> channels by <paramref name="scalar"/>, leaving <see cref="Alpha"/> unchanged, without clamping the result.
	/// </summary>
	/// <remarks>
	/// Equivalent to <c>ScaledWithoutNormalizationBy(scalar, includeAlpha: false)</c>. The result may have one or more channels outside <c>[0, 1]</c>.
	/// </remarks>
	/// <param name="scalar">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect ScaledWithoutNormalizationBy(float scalar) => ScaledWithoutNormalizationBy(scalar, includeAlpha: false);
	/// <summary>
	/// Scales this colour by <paramref name="scalar"/>, and clamps the result to <c>[0, 1]</c>.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	/// <param name="includeAlpha">If <see langword="true"/>, <see cref="Alpha"/> is scaled (and clamped) along with the other three channels; otherwise <see cref="Alpha"/> is left unchanged.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect ScaledBy(float scalar, bool includeAlpha) => ScaledWithoutNormalizationBy(scalar, includeAlpha).ClampToNormalizedRange(includeAlpha);
	/// <summary>
	/// Scales this colour by <paramref name="scalar"/>, without clamping the result.
	/// </summary>
	/// <remarks>
	/// The result may have one or more channels outside <c>[0, 1]</c>.
	/// </remarks>
	/// <param name="scalar">The scale factor.</param>
	/// <param name="includeAlpha">If <see langword="true"/>, <see cref="Alpha"/> is scaled along with the other three channels; otherwise <see cref="Alpha"/> is left unchanged.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect ScaledWithoutNormalizationBy(float scalar, bool includeAlpha) {
		var result = new ColorVect(AsVector4 * scalar);
		if (!includeAlpha) result = result with { Alpha = Alpha };
		return result;
	}

	#region Clamping and Interpolation
	/// <inheritdoc/>
	public ColorVect Clamp(ColorVect min, ColorVect max) {
		return new(
			max.Red < min.Red ? Single.Clamp(Red, max.Red, min.Red) : Single.Clamp(Red, min.Red, max.Red),
			max.Green < min.Green ? Single.Clamp(Green, max.Green, min.Green) : Single.Clamp(Green, min.Green, max.Green),
			max.Blue < min.Blue ? Single.Clamp(Blue, max.Blue, min.Blue) : Single.Clamp(Blue, min.Blue, max.Blue),
			max.Alpha < min.Alpha ? Single.Clamp(Alpha, max.Alpha, min.Alpha) : Single.Clamp(Alpha, min.Alpha, max.Alpha)
		);
	}
	/// <summary>
	/// Returns this colour with each of <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/>, and <see cref="Alpha"/> clamped to <c>[0, 1]</c>.
	/// </summary>
	public ColorVect ClampToNormalizedRange() {
		return new(
			Single.Clamp(Red, 0f, 1f),
			Single.Clamp(Green, 0f, 1f),
			Single.Clamp(Blue, 0f, 1f),
			Single.Clamp(Alpha, 0f, 1f)
		);
	}
	/// <summary>
	/// Returns this colour with <see cref="Red"/>, <see cref="Green"/>, and <see cref="Blue"/> clamped to <c>[0, 1]</c>.
	/// </summary>
	/// <param name="includeAlpha">If <see langword="true"/>, <see cref="Alpha"/> is also clamped to <c>[0, 1]</c>; otherwise <see cref="Alpha"/> is left unchanged.</param>
	public ColorVect ClampToNormalizedRange(bool includeAlpha) {
		if (includeAlpha) return ClampToNormalizedRange();
		return new(
			Single.Clamp(Red, 0f, 1f),
			Single.Clamp(Green, 0f, 1f),
			Single.Clamp(Blue, 0f, 1f),
			Alpha
		);
	}

	/// <inheritdoc/>
	public static ColorVect Interpolate(ColorVect start, ColorVect end, float distance) {
		return new(
			Single.Lerp(start.Red, end.Red, distance),
			Single.Lerp(start.Green, end.Green, distance),
			Single.Lerp(start.Blue, end.Blue, distance),
			Single.Lerp(start.Alpha, end.Alpha, distance)
		);
	}
	#endregion

	#region Colorspace Conversion
	const float SrgbToLinearThreshold = 0.04045f;
	const float LinearToSrgbThreshold = 0.0031308f;
	const float SrgbLinearSegmentSlope = 12.92f;
	const float SrgbCurveOffset = 0.055f;
	const float SrgbCurveScale = 1.055f;
	const float SrgbCurveExponent = 2.4f;

	/// <summary>
	/// Converts a single colour channel value from gamma-corrected sRGB (the space most colour-carrying textures work in) to linear colour space (the space most data-carrying textures work in).
	/// </summary>
	/// <remarks>
	/// This is the inverse of <see cref="LinearToSrgb(float)"/>.
	/// </remarks>
	/// <param name="srgbChannel">The channel value in sRGB space.</param>
	public static float SrgbToLinear(float srgbChannel) {
		if (srgbChannel <= SrgbToLinearThreshold) return srgbChannel / SrgbLinearSegmentSlope;
		return MathF.Pow((srgbChannel + SrgbCurveOffset) / SrgbCurveScale, SrgbCurveExponent);
	}

	/// <summary>
	/// Converts a single colour channel value from linear colour space (the space most data-carrying textures work in) to gamma-corrected sRGB (the space most colour-carrying textures work in).
	/// </summary>
	/// <remarks>
	/// This is the inverse of <see cref="SrgbToLinear(float)"/>.
	/// </remarks>
	/// <param name="linearChannel">The channel value in linear space.</param>
	public static float LinearToSrgb(float linearChannel) {
		if (linearChannel <= LinearToSrgbThreshold) return linearChannel * SrgbLinearSegmentSlope;
		return SrgbCurveScale * MathF.Pow(linearChannel, 1f / SrgbCurveExponent) - SrgbCurveOffset;
	}

	/// <summary>
	/// Converts <paramref name="srgb"/> from gamma-corrected sRGB to linear colour space, channel by channel (leaving <see cref="Alpha"/> unchanged, since opacity is not gamma-corrected).
	/// </summary>
	/// <remarks>
	/// This is the inverse of <see cref="LinearToSrgb(ColorVect)"/>.
	/// </remarks>
	/// <param name="srgb">The colour in sRGB space.</param>
	/// <seealso cref="SrgbToLinear(float)"/>
	public static ColorVect SrgbToLinear(ColorVect srgb) {
		return new(
			SrgbToLinear(srgb.Red),
			SrgbToLinear(srgb.Green),
			SrgbToLinear(srgb.Blue),
			srgb.Alpha
		);
	}

	/// <summary>
	/// Converts <paramref name="linear"/> from linear colour space to gamma-corrected sRGB, channel by channel (leaving <see cref="Alpha"/> unchanged, since opacity is not gamma-corrected).
	/// </summary>
	/// <remarks>
	/// This is the inverse of <see cref="SrgbToLinear(ColorVect)"/>.
	/// </remarks>
	/// <param name="linear">The colour in linear space.</param>
	/// <seealso cref="LinearToSrgb(float)"/>
	public static ColorVect LinearToSrgb(ColorVect linear) {
		return new(
			LinearToSrgb(linear.Red),
			LinearToSrgb(linear.Green),
			LinearToSrgb(linear.Blue),
			linear.Alpha
		);
	}
	#endregion
}