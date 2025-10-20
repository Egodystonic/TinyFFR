// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

public readonly partial struct UnitSphericalCoordinate : 
	INormalizable<UnitSphericalCoordinate>,
	IInvertible<UnitSphericalCoordinate>,
	IInterpolatable<UnitSphericalCoordinate> {

	// TODO xmldoc it's expected but not ultimately required that the two parameters are orthogonal
	public Direction ToDirection(Direction azimuthZero, Direction polarZero) {
		var planarBearing = (AzimuthalOffset % polarZero) * azimuthZero;
		return polarZero * (polarZero >> planarBearing) with { Angle = PolarOffset };
	}

	public UnitSphericalCoordinate Normalized => new(AzimuthalOffset.Normalized, PolarOffset.Normalized);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static UnitSphericalCoordinate operator -(UnitSphericalCoordinate coord) => coord.Inverted;
	public UnitSphericalCoordinate Inverted => new UnitSphericalCoordinate(AzimuthalOffset + Angle.HalfCircle, Angle.HalfCircle - PolarOffset).Normalized;

	public static UnitSphericalCoordinate Interpolate(UnitSphericalCoordinate start, UnitSphericalCoordinate end, float distance) {
		return new(Angle.Interpolate(start.AzimuthalOffset, end.AzimuthalOffset, distance), Angle.Interpolate(start.PolarOffset, end.PolarOffset, distance));
	}

	public UnitSphericalCoordinate Clamp(UnitSphericalCoordinate min, UnitSphericalCoordinate max) {
		return new(AzimuthalOffset.Clamp(min.AzimuthalOffset, max.AzimuthalOffset), PolarOffset.Clamp(min.PolarOffset, max.PolarOffset));
	}
}