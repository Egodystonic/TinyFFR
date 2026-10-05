// Created on 2026-10-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Rendering;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// A camera exposure expressed in real photographic terms.
/// </summary>
/// <remarks>
/// <para>
/// The settings combine exactly as they would on a real camera; a wider aperture (a smaller f-number), a longer shutter speed or a higher sensitivity each brightens the image.
/// The aperture also controls the strength of the depth-of-field effect (alongside a linear control at <see cref="RenderQualityConfig.DepthOfFieldStrength"/>).
/// </para>
/// <para>
/// Most of the time it is simplest to start from a <see cref="CameraExposurePreset"/> (see <see cref="CameraExposurePresetExtensions.ToExposureParams"/>).
/// Multiplying or dividing by a <see langword="float"/> scales only the <see cref="Sensitivity"/>, and therefore the brightness of the resulting image, by exactly that factor;
/// e.g. <c>camera.Exposure *= 2f</c> makes a camera's image exactly twice as bright without altering its depth of field.
/// </para>
/// <para>
/// When assigned to <see cref="Camera.Exposure"/>, each setting is clamped to its permitted range (e.g. <see cref="SensitivityMin"/> to <see cref="SensitivityMax"/>).
/// </para>
/// </remarks>
/// <param name="Aperture">How wide the lens opening is, as an f-number, where smaller numbers mean a wider opening and a brighter image.</param>
/// <param name="ShutterSpeed">How long the shutter stays open, in seconds; longer means brighter.</param>
/// <param name="Sensitivity">How sensitive the sensor is to light, as an ISO value; higher means brighter.</param>
public readonly record struct CameraExposureParams(float Aperture, float ShutterSpeed, float Sensitivity) : IMultiplicative<CameraExposureParams, float, CameraExposureParams>, IMathPrimitive<CameraExposureParams>, IInterpolatable<CameraExposureParams> {
	/// <summary>
	/// The aperture a new camera starts with (that of <see cref="CameraExposurePreset.InsideBrightLighting"/>): <c>f/2.8</c>.
	/// </summary>
	public static readonly float ApertureDefault = 2.8f;
	/// <summary>
	/// The smallest permitted aperture value: <c>f/0.5</c>.
	/// </summary>
	public static readonly float ApertureMin = 0.5f;
	/// <summary>
	/// The largest permitted aperture value: <c>f/64</c>.
	/// </summary>
	public static readonly float ApertureMax = 64f;
	/// <summary>
	/// The shutter speed a new camera starts with (that of <see cref="CameraExposurePreset.InsideBrightLighting"/>): <c>1/60</c> of a second.
	/// </summary>
	public static readonly float ShutterSpeedDefault = 1f / 60f;
	/// <summary>
	/// The shortest permitted shutter speed: <c>1/25,000</c> of a second.
	/// </summary>
	public static readonly float ShutterSpeedMin = 1f / 25_000f;
	/// <summary>
	/// The longest permitted shutter speed: <c>60</c> seconds.
	/// </summary>
	public static readonly float ShutterSpeedMax = 60f;
	/// <summary>
	/// The sensitivity (ISO) a new camera starts with (that of <see cref="CameraExposurePreset.InsideBrightLighting"/>): <c>200</c>.
	/// </summary>
	public static readonly float SensitivityDefault = 200f;
	/// <summary>
	/// The lowest permitted sensitivity (ISO): <c>10</c>.
	/// </summary>
	public static readonly float SensitivityMin = 10f;
	/// <summary>
	/// The highest permitted sensitivity (ISO): <c>204,800</c>.
	/// </summary>
	public static readonly float SensitivityMax = 204_800f;
	
	/// <summary>
	/// Creates the parameters according to the given <paramref name="preset"/>.
	/// </summary>
	/// <param name="preset">The preset to apply.</param>
	public CameraExposureParams(CameraExposurePreset preset) : this(0f, 0f, 0f) {
		this = preset.ToExposureParams();
	}
	
	/// <summary>
	/// The exposure value (EV100) of these settings (the photographic measure of how much light they let through, in stops.
	/// </summary>
	public float Ev100 {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Log2(Aperture * Aperture / ShutterSpeed * 100f / Sensitivity);
	}
	
	/// <summary>
	/// Returns a copy of these parameters with <see cref="Sensitivity"/> multiplied by <paramref name="scalar"/>, making the resulting image exactly that
	/// many times as bright.
	/// </summary>
	public CameraExposureParams WithSensitivityScaledBy(float scalar) => this * scalar;

	CameraExposureParams IMultiplicative<CameraExposureParams, float, CameraExposureParams>.MultipliedBy(float other) => this * other;
	CameraExposureParams IMultiplicative<CameraExposureParams, float, CameraExposureParams>.DividedBy(float other) => this / other;

	/// <summary>
	/// Returns a copy of <paramref name="exposure"/> with its <see cref="Sensitivity"/> multiplied by <paramref name="brightnessMultiplier"/>, making the resulting image exactly that
	/// many times as bright.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CameraExposureParams operator *(CameraExposureParams exposure, float brightnessMultiplier) => exposure with { Sensitivity = exposure.Sensitivity * brightnessMultiplier };
	/// <summary>
	/// Returns a copy of <paramref name="exposure"/> with its <see cref="Sensitivity"/> multiplied by <paramref name="brightnessMultiplier"/>, making the resulting image exactly that
	/// many times as bright.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CameraExposureParams operator *(float brightnessMultiplier, CameraExposureParams exposure) => exposure * brightnessMultiplier;
	/// <summary>
	/// Returns a copy of <paramref name="exposure"/> with its <see cref="Sensitivity"/> divided by <paramref name="brightnessDivisor"/>, making the resulting image exactly that
	/// many times dimmer.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CameraExposureParams operator /(CameraExposureParams exposure, float brightnessDivisor) => exposure with { Sensitivity = exposure.Sensitivity / brightnessDivisor };

	/// <summary>
	/// Converts <paramref name="operand"/> to the settings it applies (see <see cref="CameraExposurePresetExtensions.ToExposureParams"/>).
	/// </summary>
	/// <param name="operand">The preset to convert.</param>
	public static implicit operator CameraExposureParams(CameraExposurePreset operand) => operand.ToExposureParams();

	#region Random / Interpolate / Clamp
	/// <summary>
	/// Produces random exposure settings, with each setting independently random between its permitted minimum and maximum (e.g. <see cref="ApertureMin"/> and <see cref="ApertureMax"/>).
	/// </summary>
	/// <remarks>
	/// Each setting is distributed evenly in stops rather than linearly (see <see cref="Random(CameraExposureParams, CameraExposureParams)"/>).
	/// </remarks>
	public static CameraExposureParams Random() => Random(new(ApertureMin, ShutterSpeedMin, SensitivityMin), new(ApertureMax, ShutterSpeedMax, SensitivityMax));
	/// <summary>
	/// Produces random exposure settings, with each setting independently random between the corresponding settings of <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// </summary>
	/// <remarks>
	/// Each setting is distributed evenly in stops rather than linearly, so (for example) a random sensitivity between <c>ISO 100</c> and <c>ISO 400</c> is as likely to be
	/// below <c>ISO 200</c> as above it. Every setting of both bounds must therefore be greater than zero.
	/// </remarks>
	/// <param name="minInclusive">The lower bound for each setting.</param>
	/// <param name="maxExclusive">The exclusive ceiling for each setting.</param>
	public static CameraExposureParams Random(CameraExposureParams minInclusive, CameraExposureParams maxExclusive) {
		return new(
			RandomLogarithmic(minInclusive.Aperture, maxExclusive.Aperture),
			RandomLogarithmic(minInclusive.ShutterSpeed, maxExclusive.ShutterSpeed),
			RandomLogarithmic(minInclusive.Sensitivity, maxExclusive.Sensitivity)
		);
	}
	static float RandomLogarithmic(float minInclusive, float maxExclusive) {
		if (minInclusive == maxExclusive) return minInclusive;
		var result = MathF.Exp(RandomUtils.NextSingle(MathF.Log(minInclusive), MathF.Log(maxExclusive)));
		return Single.Clamp(result, Single.Min(minInclusive, maxExclusive), Single.Max(minInclusive, maxExclusive));
	}

	/// <summary>
	/// Interpolates from <paramref name="start"/> to <paramref name="end"/> according to the normalized <paramref name="distance"/>, moving each setting by an equal
	/// number of stops for an equal change in <paramref name="distance"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Each setting is interpolated geometrically (i.e. evenly in stops, rather than linearly), so the <see cref="Ev100"/> of the result (and therefore the brightness of the
	/// resulting image, in stops) changes evenly as <paramref name="distance"/> does. This makes it suitable for smoothly transitioning a camera between two exposures
	/// (e.g. from <see cref="CameraExposurePreset.OutsideMidday"/> to <see cref="CameraExposurePreset.OutsideTwilight"/>). Every setting of both values must therefore be
	/// greater than zero.
	/// </para>
	/// <para>
	/// Use <see cref="InterpolateArithmetically"/> to interpolate each setting linearly instead.
	/// </para>
	/// </remarks>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate (i.e. <c>0.5f</c> returns the value exactly halfway between start &amp; end).
	/// Values outside the range 0-1 are permitted and will extend the interpolation calculation beyond the start or end value respectively.</param>
	public static CameraExposureParams Interpolate(CameraExposureParams start, CameraExposureParams end, float distance) {
		return new(
			InterpolateGeometric(start.Aperture, end.Aperture, distance),
			InterpolateGeometric(start.ShutterSpeed, end.ShutterSpeed, distance),
			InterpolateGeometric(start.Sensitivity, end.Sensitivity, distance)
		);
	}
	static float InterpolateGeometric(float start, float end, float distance) {
		if (start == end) return start;
		return start * MathF.Pow(end / start, distance);
	}
	/// <summary>
	/// Interpolates from <paramref name="start"/> to <paramref name="end"/> according to the normalized <paramref name="distance"/>, interpolating each setting linearly.
	/// </summary>
	/// <remarks>
	/// Because each stop of exposure doubles or halves a setting, the brightness of the resulting image does not change evenly with <paramref name="distance"/>; in most cases
	/// <see cref="Interpolate"/> is what you want instead.
	/// </remarks>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate (i.e. <c>0.5f</c> returns the value exactly halfway between start &amp; end).
	/// Values outside the range 0-1 are permitted and will extend the interpolation calculation beyond the start or end value respectively.</param>
	public static CameraExposureParams InterpolateArithmetically(CameraExposureParams start, CameraExposureParams end, float distance) {
		return new(
			Single.Lerp(start.Aperture, end.Aperture, distance),
			Single.Lerp(start.ShutterSpeed, end.ShutterSpeed, distance),
			Single.Lerp(start.Sensitivity, end.Sensitivity, distance)
		);
	}

	/// <summary>
	/// Clamps each of these settings independently between the corresponding settings of <paramref name="min"/> and <paramref name="max"/>.
	/// </summary>
	/// <remarks>
	/// As with other TinyFFR clamp functions, <paramref name="min"/> and <paramref name="max"/> may be swapped freely (for each setting individually) and still give the same answer.
	/// </remarks>
	/// <param name="min">The lower bound for each setting (inclusive).</param>
	/// <param name="max">The upper bound for each setting (inclusive).</param>
	public CameraExposureParams Clamp(CameraExposureParams min, CameraExposureParams max) {
		return new(
			ClampEitherOrder(Aperture, min.Aperture, max.Aperture),
			ClampEitherOrder(ShutterSpeed, min.ShutterSpeed, max.ShutterSpeed),
			ClampEitherOrder(Sensitivity, min.Sensitivity, max.Sensitivity)
		);
	}
	static float ClampEitherOrder(float value, float a, float b) => Single.Clamp(value, Single.Min(a, b), Single.Max(a, b));
	#endregion

	#region Span Conversions
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 3;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, CameraExposureParams src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.Aperture);
		BinaryPrimitives.WriteSingleLittleEndian(dest[sizeof(float)..], src.ShutterSpeed);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Sensitivity);
	}

	/// <inheritdoc />
	public static CameraExposureParams DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[sizeof(float)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..])
		);
	}
	#endregion

	#region String Conversions
	/// <inheritdoc />
	public override string ToString() => ToString(null, null);
	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(CameraExposureParams), (nameof(Aperture), Aperture), (nameof(ShutterSpeed), ShutterSpeed), (nameof(Sensitivity), Sensitivity));
	/// <inheritdoc />
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(CameraExposureParams), (nameof(Aperture), Aperture), (nameof(ShutterSpeed), ShutterSpeed), (nameof(Sensitivity), Sensitivity));

	/// <inheritdoc />
	public static CameraExposureParams Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out CameraExposureParams result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static CameraExposureParams Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out float aperture, out float shutterSpeed, out float sensitivity);
		return new(aperture, shutterSpeed, sensitivity);
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out CameraExposureParams result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out float aperture, out float shutterSpeed, out float sensitivity)) return false;
		result = new(aperture, shutterSpeed, sensitivity);
		return true;
	}
	#endregion

	#region Equality
	/// <summary>
	/// Determines whether each of these settings is equal to the corresponding setting of <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(CameraExposureParams other, float tolerance) {
		return MathF.Abs(Aperture - other.Aperture) <= tolerance
			&& MathF.Abs(ShutterSpeed - other.ShutterSpeed) <= tolerance
			&& MathF.Abs(Sensitivity - other.Sensitivity) <= tolerance;
	}
	#endregion
}
