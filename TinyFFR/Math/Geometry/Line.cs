// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 4 * 2)]
public readonly partial struct Line : ILine<Line, Ray>, IDescriptiveStringProvider {
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
	Location ILine.StartPoint => PointOnLine;
	bool ILine.IsUnboundedInBothDirections => true;
	float? ILine.Length => null;
	float? ILine.LengthSquared => null;
	Vect? ILine.StartToEndVect => null;
	Location? ILine.EndPoint => null;

	public Line(Location pointOnLine, Direction direction) {
		_pointOnLine = pointOnLine;
		_direction = direction;
	}

	public Line(Location firstPointOnLine, Location secondPointOnLine) {
		_pointOnLine = firstPointOnLine;
		_direction = (secondPointOnLine - firstPointOnLine).Direction;
	}

	#region Span Conversions
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Line src) => MemoryMarshal.Cast<Line, float>(new ReadOnlySpan<Line>(in src));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line ConvertFromSpan(ReadOnlySpan<float> src) => new(Location.ConvertFromSpan(src), Direction.ConvertFromSpan(src[4..]));
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