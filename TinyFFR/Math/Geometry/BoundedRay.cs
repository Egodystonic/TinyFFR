// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 4 * 2)]
public readonly partial struct BoundedRay : ILineLike<BoundedRay, BoundedRay, BoundedRay>, IDescriptiveStringProvider {
	readonly Location _startPoint;
	readonly Vect _vect;

	public Location StartPoint {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _startPoint;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _startPoint = value;
	}
	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect.Direction;
	}
	public float Length {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect.Length;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = _vect.WithLength(value);
	}
	public float LengthSquared {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect.LengthSquared;
	}
	public Vect StartToEndVect {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = value;
	}
	public Location EndPoint {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _startPoint + _vect;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = value - _startPoint;
	}
	public Location MiddlePoint {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _startPoint + _vect * 0.5f;
	}
	bool ILineLike.IsUnboundedInBothDirections => false;
	bool ILineLike.IsFiniteLength => true;
	float? ILineLike.Length => Length;
	float? ILineLike.LengthSquared => LengthSquared;
	Vect? ILineLike.StartToEndVect => StartToEndVect;
	Location? ILineLike.EndPoint => EndPoint;

	public BoundedRay(Location startPoint, Location endPoint) : this(startPoint, endPoint - startPoint) { }
	public BoundedRay(Location startPoint, Vect startToEndVect) {
		_startPoint = startPoint;
		_vect = startToEndVect;
	}

	#region Random
	public static BoundedRay Random() => new(Location.Random(), Location.Random());
	public static BoundedRay Random(BoundedRay minInclusive, BoundedRay maxExclusive) => new(Location.Random(minInclusive.StartPoint, maxExclusive.StartPoint), Location.Random(minInclusive.EndPoint, maxExclusive.EndPoint));
	#endregion

	#region Span Conversions
	public static int SerializationByteSpanLength { get; } = Location.SerializationByteSpanLength * 2;

	public static void SerializeToBytes(Span<byte> dest, BoundedRay src) {
		Location.SerializeToBytes(dest, src.StartPoint);
		Location.SerializeToBytes(dest[Location.SerializationByteSpanLength..], src.EndPoint);
	}

	public static BoundedRay DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			Location.DeserializeFromBytes(src),
			Location.DeserializeFromBytes(src[Location.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region String Conversions
	public override string ToString() => ToString(null, null);
	public string ToStringDescriptive() {
		return $"{nameof(BoundedRay)}{GeometryUtils.ParameterStartToken}" +
			   $"{nameof(StartPoint)}{GeometryUtils.ParameterKeyValueSeparatorToken}{StartPoint}{GeometryUtils.ParameterSeparatorToken}" +
			   $"{nameof(EndPoint)}{GeometryUtils.ParameterKeyValueSeparatorToken}{EndPoint}{GeometryUtils.ParameterSeparatorToken}" +
			   $"{nameof(Length)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Length:N2}{GeometryUtils.ParameterSeparatorToken}" +
			   $"{nameof(Direction)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Direction.ToStringDescriptive()}" +
			   $"{GeometryUtils.ParameterEndToken}";
	}
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(BoundedRay), (nameof(StartPoint), _startPoint), (nameof(EndPoint), EndPoint));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(BoundedRay), (nameof(StartPoint), _startPoint), (nameof(EndPoint), EndPoint));

	public static BoundedRay Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out BoundedRay result) => TryParse(s.AsSpan(), provider, out result);

	public static BoundedRay Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Location startPoint, out Location endPoint);
		return new(startPoint, endPoint);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BoundedRay result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Location startPoint, out Location endPoint)) return false;
		result = new(startPoint, endPoint);
		return true;
	}
	#endregion

	#region Equality
	public bool Equals(BoundedRay other) => _startPoint.Equals(other._startPoint) && _vect.Equals(other._vect);
	public bool Equals(BoundedRay other, float tolerance) => StartPoint.Equals(other.StartPoint, tolerance) && EndPoint.Equals(other.EndPoint, tolerance);
	public bool EqualsDisregardingDirection(BoundedRay other) => Equals(other) || Equals(other.Flipped);
	public bool EqualsDisregardingDirection(BoundedRay other, float tolerance) => Equals(other, tolerance) || Equals(other.Flipped, tolerance);
	public override bool Equals(object? obj) => obj is BoundedRay other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_startPoint, _vect);
	public static bool operator ==(BoundedRay left, BoundedRay right) => left.Equals(right);
	public static bool operator !=(BoundedRay left, BoundedRay right) => !left.Equals(right);
	#endregion
}