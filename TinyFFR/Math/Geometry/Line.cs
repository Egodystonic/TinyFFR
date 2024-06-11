// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 4 * 2)]
public readonly partial struct Line : ILineLike<Line, Ray>, IPrecomputationInterpolatable<Line, Rotation>, IDescriptiveStringProvider {
	readonly Location _pointOnLine;
	readonly Direction _direction;

	public Location PointOnLine {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _pointOnLine;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _pointOnLine = value;
	}
	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _direction;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _direction = value;
	}
	Location ILineLike.StartPoint => PointOnLine;
	bool ILineLike.IsUnboundedInBothDirections => true;
	bool ILineLike.IsFiniteLength => false;
	float? ILineLike.Length => null;
	float? ILineLike.LengthSquared => null;
	Vect? ILineLike.StartToEndVect => null;
	Location? ILineLike.EndPoint => null;

	public Line(Location pointOnLine, Direction direction) {
		_pointOnLine = pointOnLine;
		_direction = direction;
	}

	#region Factories and Conversions
	public static Line FromTwoPoints(Location firstPointOnLine, Location secondPointOnLine) {
		return new(firstPointOnLine, (secondPointOnLine - firstPointOnLine).Direction);
	}
	#endregion

	#region Span Conversions
	public static int SerializationByteSpanLength { get; } = Location.SerializationByteSpanLength + Direction.SerializationByteSpanLength;

	public static void SerializeToBytes(Span<byte> dest, Line src) {
		Location.SerializeToBytes(dest, src.PointOnLine);
		Direction.SerializeToBytes(dest[Location.SerializationByteSpanLength..], src.Direction);
	}

	public static Line DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			Location.DeserializeFromBytes(src),
			Direction.DeserializeFromBytes(src[Location.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region String Conversions
	public override string ToString() => ToString(null, null);
	public string ToStringDescriptive() => $"{nameof(Line)}{GeometryUtils.ParameterStartToken}{nameof(PointOnLine)}{GeometryUtils.ParameterKeyValueSeparatorToken}{PointOnLine}{GeometryUtils.ParameterSeparatorToken}{nameof(ClosestPointToOrigin)}{GeometryUtils.ParameterKeyValueSeparatorToken}{ClosestPointToOrigin()}{GeometryUtils.ParameterSeparatorToken}{nameof(Direction)}{GeometryUtils.ParameterKeyValueSeparatorToken}{_direction.ToStringDescriptive()}{GeometryUtils.ParameterEndToken}";
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Line), (nameof(PointOnLine), PointOnLine), (nameof(Direction), _direction));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Line), (nameof(PointOnLine), PointOnLine), (nameof(Direction), _direction));
	
	public static Line Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Line result) => TryParse(s.AsSpan(), provider, out result);
	
	public static Line Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Location closestPointToOrigin, out Direction direction);
		return new(closestPointToOrigin, direction);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Line result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Location closestPointToOrigin, out Direction direction)) return false;
		result = new(closestPointToOrigin, direction);
		return true;
	}
	#endregion

	#region Equality
	public bool Equals(Line other) => DistanceFrom(other) == 0f && (Direction.Equals(other.Direction) || Direction.Equals(-other.Direction));
	public bool Equals(Line other, float tolerance) => DistanceFrom(other) <= tolerance && (Direction.Equals(other.Direction, tolerance) || Direction.Equals(-other.Direction, tolerance));
	public bool EqualsWithinDistanceAndAngle(Line other, float distance, Angle angle) {
		return DistanceFrom(other) <= distance && (Direction.EqualsWithinAngle(other.Direction, angle) || Direction.EqualsWithinAngle(-other.Direction, angle));
	}
	public override bool Equals(object? obj) => obj is Line other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_pointOnLine, _direction);
	public static bool operator ==(Line left, Line right) => left.Equals(right);
	public static bool operator !=(Line left, Line right) => !left.Equals(right);
	#endregion
}