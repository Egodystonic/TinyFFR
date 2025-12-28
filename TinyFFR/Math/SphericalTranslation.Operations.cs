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

	// TODO xmldoc it's expected but not ultimately required that the two parameters are orthogonal
	public Direction Translate(Direction azimuthZero, Direction polarZero) {
		var planarBearing = (AzimuthalOffset % polarZero) * azimuthZero;
		return polarZero * (polarZero >> planarBearing) with { Angle = PolarOffset };
	}

	public SphericalTranslation Normalized => new(AzimuthalOffset.Normalized, PolarOffset.Normalized);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SphericalTranslation operator -(SphericalTranslation coord) => coord.Inverted;
	public SphericalTranslation Inverted => new SphericalTranslation(AzimuthalOffset + Angle.HalfCircle, Angle.HalfCircle - PolarOffset).Normalized;

	static SphericalTranslation IInterpolatable<SphericalTranslation>.Interpolate(SphericalTranslation start, SphericalTranslation end, float distance) {
		return InterpolateGeometrically(start, end, distance);
	}
	// TODO xmldoc this interpolates around the shortest distance between the two coords; if a non-geometric interpolation is required use InterpolateArithmetically
	public static SphericalTranslation InterpolateGeometrically(SphericalTranslation start, SphericalTranslation end, float distance) {
		return new(Angle.InterpolateShortestDifference(start.AzimuthalOffset, end.AzimuthalOffset, distance), Angle.InterpolateShortestDifference(start.PolarOffset, end.PolarOffset, distance));
	}
	public static SphericalTranslation InterpolateArithmetically(SphericalTranslation start, SphericalTranslation end, float distance) {
		return new(Angle.Interpolate(start.AzimuthalOffset, end.AzimuthalOffset, distance), Angle.Interpolate(start.PolarOffset, end.PolarOffset, distance));
	}

	public SphericalTranslation Clamp(SphericalTranslation min, SphericalTranslation max) {
		return new(AzimuthalOffset.Clamp(min.AzimuthalOffset, max.AzimuthalOffset), PolarOffset.Clamp(min.PolarOffset, max.PolarOffset));
	}
}