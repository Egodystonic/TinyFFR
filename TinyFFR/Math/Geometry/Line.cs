// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 4 * 2)]
public readonly partial struct Line : ILine<Line>, IDescriptiveStringProvider {
	readonly Location _closestPointToOrigin;
	readonly Direction _direction;

	public Location ClosestPointToOrigin {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _closestPointToOrigin;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _closestPointToOrigin = value;
	}
	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _direction;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _direction = value;
	}
	Location ILine.StartPoint => _closestPointToOrigin;
	bool ILine.IsUnboundedInBothDirections => true;
	float? ILine.Length => null;
	float? ILine.LengthSquared => null;
	Vect? ILine.StartToEndVect => null;
	Location? ILine.EndPoint => null;

	public Line(Location closestPointToOrigin, Direction direction) {
		_closestPointToOrigin = closestPointToOrigin;
		_direction = direction;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Line src) => MemoryMarshal.Cast<Line, float>(new ReadOnlySpan<Line>(in src));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line ConvertFromSpan(ReadOnlySpan<float> src) => new(Location.ConvertFromSpan(src), Direction.ConvertFromSpan(src[3..]));
	
	public override string ToString() => ToString(null, null);
	public string ToStringDescriptive() => $"{nameof(Line)}{GeometryUtils.ParameterStartToken}{nameof(ClosestPointToOrigin)}{GeometryUtils.ParameterKeyValueSeparatorToken}{_closestPointToOrigin}{GeometryUtils.ParameterKeyValueSeparatorToken}{nameof(Direction)}{_direction.ToStringDescriptive()}{GeometryUtils.ParameterEndToken}";
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Line), (nameof(ClosestPointToOrigin), _closestPointToOrigin), (nameof(Direction), _direction));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Line), (nameof(ClosestPointToOrigin), _closestPointToOrigin), (nameof(Direction), _direction));
	
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
	
	#region Equality
	public bool Equals(Line other) => _closestPointToOrigin.Equals(other._closestPointToOrigin) && _direction.Equals(other._direction);
	public bool Equals(Line other, float tolerance) => _closestPointToOrigin.Equals(other._closestPointToOrigin, tolerance) && _direction.Equals(other.Direction, tolerance);
	public override bool Equals(object? obj) => obj is Line other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_closestPointToOrigin, _direction);
	public static bool operator ==(Line left, Line right) => left.Equals(right);
	public static bool operator !=(Line left, Line right) => !left.Equals(right);
	#endregion
}