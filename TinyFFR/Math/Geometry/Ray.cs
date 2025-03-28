﻿// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 4 * 2)]
public readonly partial struct Ray : ILineLike<Ray, BoundedRay, Ray>, IPrecomputationInterpolatable<Ray, Rotation>, IDescriptiveStringProvider {
	readonly Location _startPoint;
	readonly Direction _direction;

	public Location StartPoint {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _startPoint;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _startPoint = value;
	}
	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _direction;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _direction = value;
	}
	bool ILineLike.IsUnboundedInBothDirections => false;
	bool ILineLike.IsFiniteLength => false;
	float? ILineLike.Length => null;
	float? ILineLike.LengthSquared => null;
	Vect? ILineLike.StartToEndVect => null;
	Location? ILineLike.EndPoint => null;

	public Ray(Location startPoint, Direction direction) {
		_startPoint = startPoint;
		_direction = direction;
	}

	#region Random
	public static Ray Random() => new(Location.Random(), Direction.Random());
	public static Ray Random(Ray minInclusive, Ray maxExclusive) => new(Location.Random(minInclusive.StartPoint, maxExclusive.StartPoint), Direction.Random(minInclusive.Direction, maxExclusive.Direction));
	#endregion

	#region Span Conversions
	public static int SerializationByteSpanLength { get; } = Location.SerializationByteSpanLength + Direction.SerializationByteSpanLength;

	public static void SerializeToBytes(Span<byte> dest, Ray src) {
		Location.SerializeToBytes(dest, src.StartPoint);
		Direction.SerializeToBytes(dest[Location.SerializationByteSpanLength..], src.Direction);
	}

	public static Ray DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			Location.DeserializeFromBytes(src),
			Direction.DeserializeFromBytes(src[Location.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region String Conversions
	public override string ToString() => ToString(null, null);
	public string ToStringDescriptive() => $"{nameof(Ray)}{GeometryUtils.ParameterStartToken}{nameof(StartPoint)}{GeometryUtils.ParameterKeyValueSeparatorToken}{_startPoint}{GeometryUtils.ParameterSeparatorToken}{nameof(Direction)}{GeometryUtils.ParameterKeyValueSeparatorToken}{_direction.ToStringDescriptive()}{GeometryUtils.ParameterEndToken}";
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Ray), (nameof(StartPoint), _startPoint), (nameof(Direction), _direction));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Ray), (nameof(StartPoint), _startPoint), (nameof(Direction), _direction));

	public static Ray Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Ray result) => TryParse(s.AsSpan(), provider, out result);

	public static Ray Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Location startPoint, out Direction direction);
		return new(startPoint, direction);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Ray result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Location startPoint, out Direction direction)) return false;
		result = new(startPoint, direction);
		return true;
	}
	#endregion

	#region Equality
	public bool Equals(Ray other) => _startPoint.Equals(other._startPoint) && _direction.Equals(other._direction);
	public bool Equals(Ray other, float tolerance) => StartPoint.Equals(other.StartPoint, tolerance) && Direction.Equals(other.Direction, tolerance);
	public override bool Equals(object? obj) => obj is Ray other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_startPoint, _direction);
	public static bool operator ==(Ray left, Ray right) => left.Equals(right);
	public static bool operator !=(Ray left, Ray right) => !left.Equals(right);
	#endregion
}