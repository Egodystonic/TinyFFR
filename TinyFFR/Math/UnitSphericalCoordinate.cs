// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 2)]
public readonly partial struct UnitSphericalCoordinate : IMathPrimitive<UnitSphericalCoordinate> {
	readonly Angle _azimuthalOffset;
	readonly Angle _polarOffset;

	// TODO xmldoc: Azimuthal offset is anti-clockwise (when the polar direction is looking towards you)
	public Angle AzimuthalOffset {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _azimuthalOffset;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _azimuthalOffset = value;
	}
	public Angle PolarOffset {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _polarOffset;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _polarOffset = value;
	}
	
	public UnitSphericalCoordinate(Angle azimuthalOffset, Angle polarOffset) {
		_azimuthalOffset = azimuthalOffset;
		_polarOffset = polarOffset;
	}

	#region Random
	public static UnitSphericalCoordinate Random() => new(Angle.Random(Angle.Zero, Angle.FullCircle), Angle.Random(Angle.Zero, Angle.FullCircle));
	public static UnitSphericalCoordinate Random(UnitSphericalCoordinate minInclusive, UnitSphericalCoordinate maxExclusive) {
		return new(Angle.Random(minInclusive.AzimuthalOffset, maxExclusive.AzimuthalOffset), Angle.Random(minInclusive.PolarOffset, maxExclusive.PolarOffset));
	}
	#endregion

	#region Span Conversions
	public static int SerializationByteSpanLength { get; } = Angle.SerializationByteSpanLength * 2;

	public static void SerializeToBytes(Span<byte> dest, UnitSphericalCoordinate src) {
		Angle.SerializeToBytes(dest, src.AzimuthalOffset);
		Angle.SerializeToBytes(dest[Angle.SerializationByteSpanLength..], src.PolarOffset);
	}

	public static UnitSphericalCoordinate DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			Angle.DeserializeFromBytes(src),
			Angle.DeserializeFromBytes(src[Angle.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region String Conversions
	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(UnitSphericalCoordinate), (nameof(AzimuthalOffset), AzimuthalOffset), (nameof(PolarOffset), PolarOffset));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(UnitSphericalCoordinate), (nameof(AzimuthalOffset), AzimuthalOffset), (nameof(PolarOffset), PolarOffset));

	public static UnitSphericalCoordinate Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out UnitSphericalCoordinate result) => TryParse(s.AsSpan(), provider, out result);

	public static UnitSphericalCoordinate Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Angle azimuthalOffset, out Angle polarOffset);
		return new(azimuthalOffset, polarOffset);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UnitSphericalCoordinate result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Angle azimuthalOffset, out Angle polarOffset)) return false;
		result = new(azimuthalOffset, polarOffset);
		return true;
	}
	#endregion

	#region Equality
	public bool Equals(UnitSphericalCoordinate other) => _azimuthalOffset.Equals(other._azimuthalOffset) && _polarOffset.Equals(other._polarOffset);
	public bool Equals(UnitSphericalCoordinate other, float tolerance) => _azimuthalOffset.Equals(other._azimuthalOffset, tolerance) && _polarOffset.Equals(other._polarOffset, tolerance);
	public bool IsEquivalentWithinSphereTo(UnitSphericalCoordinate other) => IsEquivalentWithinSphereTo(other, 0f);
	public bool IsEquivalentWithinSphereTo(UnitSphericalCoordinate other, Angle tolerance) => IsEquivalentWithinSphereTo(other, tolerance.Degrees);
	public bool IsEquivalentWithinSphereTo(UnitSphericalCoordinate other, float toleranceDegrees) {
		return _azimuthalOffset.IsEquivalentWithinCircleTo(other._azimuthalOffset, toleranceDegrees)
			&& _polarOffset.IsEquivalentWithinCircleTo(other._polarOffset, toleranceDegrees);
	}

	public override bool Equals(object? obj) => obj is Ray other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_azimuthalOffset, _polarOffset);
	public static bool operator ==(UnitSphericalCoordinate left, UnitSphericalCoordinate right) => left.Equals(right);
	public static bool operator !=(UnitSphericalCoordinate left, UnitSphericalCoordinate right) => !left.Equals(right);
	#endregion
}