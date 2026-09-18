// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

public readonly partial struct SphericalTranslation : 
	INormalizable<SphericalTranslation>,
	IInvertible<SphericalTranslation>,
	IInterpolatable<SphericalTranslation> {

	/// <summary>
	/// Resolves this translation into a concrete <see cref="Direction"/>, given a reference frame.
	/// </summary>
	/// <remarks>
	/// The reference frame is constructed according to the given <paramref name="azimuthZero"/> and <paramref name="polarZero"/>. Together these define
	/// the starting direction (azimuthal "forward") and polar "up".
	/// <paramref name="azimuthZero"/> and <paramref name="polarZero"/> are expected (but not strictly required) to be
	/// orthogonal to each other, in the same way a compass "north" reference and an "up" pole would be.
	/// </remarks>
	/// <param name="azimuthZero">The direction treated as the 0° azimuthal reference. If this is <see cref="Direction.None"/>, the result is <see cref="Direction.None"/>.</param>
	/// <param name="polarZero">The pole the polar angle is measured from. If this is <see cref="Direction.None"/>, the result is <see cref="Direction.None"/>.</param>
	/// <returns>The spherical translation of <paramref name="azimuthZero"/>.</returns>
	public Direction Translate(Direction azimuthZero, Direction polarZero) {
		var planarBearing = (AzimuthalOffset % polarZero) * azimuthZero;
		return polarZero * (polarZero >> planarBearing) with { Angle = PolarOffset };
	}

	/// <summary>
	/// Returns this translation with both its <see cref="AzimuthalOffset"/> and <see cref="PolarOffset"/> normalized (folded into the range 0° to 360°).
	/// </summary>
	public SphericalTranslation Normalized => new(AzimuthalOffset.Normalized, PolarOffset.Normalized);

	/// <summary>
	/// Negates <paramref name="coord"/>; equivalent to reading <see cref="Inverted"/>.
	/// </summary>
	/// <param name="coord">The value to negate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SphericalTranslation operator -(SphericalTranslation coord) => coord.Inverted;
	/// <summary>
	/// Returns the translation that exactly undoes this one: applying this translation followed by its inverse (or vice versa) returns to the original direction.
	/// </summary>
	public SphericalTranslation Inverted => new SphericalTranslation(AzimuthalOffset + Angle.HalfCircle, Angle.HalfCircle - PolarOffset).Normalized;

	static SphericalTranslation IInterpolatable<SphericalTranslation>.Interpolate(SphericalTranslation start, SphericalTranslation end, float distance) {
		return InterpolateGeometrically(start, end, distance);
	}
	/// <summary>
	/// Interpolates a value from <paramref name="start"/> to <paramref name="end"/> according to the normalized <paramref name="distance"/>, specifically
	/// taking the shortest path around the sphere between them.
	/// </summary>
	/// <remarks>
	/// This interpolates each of <see cref="AzimuthalOffset"/> and <see cref="PolarOffset"/> via <see cref="Angle.InterpolateShortestPath"/>.
	/// If you want to interpolate the raw offset values arithmetically instead, use <see cref="InterpolateArithmetically"/>.
	/// </remarks>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate (i.e. <c>0.5f</c> returns the value exactly halfway between start &amp; end).
	/// Values outside the range 0-1 are permitted and will extend the interpolation calculation beyond the start or end value respectively.</param>
	public static SphericalTranslation InterpolateGeometrically(SphericalTranslation start, SphericalTranslation end, float distance) {
		return new(Angle.InterpolateShortestPath(start.AzimuthalOffset, end.AzimuthalOffset, distance), Angle.InterpolateShortestPath(start.PolarOffset, end.PolarOffset, distance));
	}
	/// <summary>
	/// Interpolates a value from <paramref name="start"/> to <paramref name="end"/> according to the normalized <paramref name="distance"/>, treating each offset as a plain numeric value rather than a position on a circle.
	/// </summary>
	/// <remarks>
	/// This interpolates each of <see cref="AzimuthalOffset"/> and <see cref="PolarOffset"/> via <see cref="Angle.Interpolate"/>,
	/// so it does not take the shortest path around the sphere. In most cases <see cref="InterpolateGeometrically"/> is what you want instead.
	/// </remarks>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate (i.e. <c>0.5f</c> returns the value exactly halfway between start &amp; end).
	/// Values outside the range 0-1 are permitted and will extend the interpolation calculation beyond the start or end value respectively.</param>
	public static SphericalTranslation InterpolateArithmetically(SphericalTranslation start, SphericalTranslation end, float distance) {
		return new(Angle.Interpolate(start.AzimuthalOffset, end.AzimuthalOffset, distance), Angle.Interpolate(start.PolarOffset, end.PolarOffset, distance));
	}

	/// <inheritdoc />
	public SphericalTranslation Clamp(SphericalTranslation min, SphericalTranslation max) {
		return new(AzimuthalOffset.Clamp(min.AzimuthalOffset, max.AzimuthalOffset), PolarOffset.Clamp(min.PolarOffset, max.PolarOffset));
	}
}