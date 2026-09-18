// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a finite-length line segment with a <see cref="StartPoint"/> and an <see cref="EndPoint"/>.
/// </summary>
/// <seealso cref="Line"/>
/// <seealso cref="Ray"/>
[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 4 * 2)]
public readonly partial struct BoundedRay : ILineLike<BoundedRay, BoundedRay, BoundedRay>, IDescriptiveStringProvider {
	readonly Location _startPoint;
	readonly Vect _vect;

	/// <summary>
	/// Where in space this ray starts.
	/// </summary>
	public Location StartPoint {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _startPoint;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _startPoint = value;
	}
	/// <summary>
	/// The direction from <see cref="StartPoint"/> to <see cref="EndPoint"/>.
	/// Can be <see cref="Direction.None"/> if <see cref="StartPoint"/> and <see cref="EndPoint"/> are the same.
	/// </summary>
	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect.Direction;
	}
	/// <summary>
	/// The distance between <see cref="StartPoint"/> and <see cref="EndPoint"/>.
	/// </summary>
	public float Length {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect.Length;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = _vect.WithLength(value);
	}
	/// <summary>
	/// The distance between <see cref="StartPoint"/> and <see cref="EndPoint"/>, squared.
	/// </summary>
	public float LengthSquared {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect.LengthSquared;
	}
	/// <summary>
	/// The <see cref="Vect"/> that when added to <see cref="StartPoint"/> returns <see cref="EndPoint"/>.
	/// </summary>
	public Vect StartToEndVect {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = value;
	}
	/// <summary>
	/// Where in space this ray ends.
	/// </summary>
	public Location EndPoint {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _startPoint + _vect;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = value - _startPoint;
	}
	/// <summary>
	/// The middle point of this ray (i.e. exactly half way between <see cref="StartPoint"/> and <see cref="EndPoint"/>).
	/// </summary>
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

	/// <summary>
	/// Constructs a new <see cref="BoundedRay"/> from <paramref name="startPoint"/> to <paramref name="endPoint"/>.
	/// </summary>
	/// <param name="startPoint">Where the ray starts.</param>
	/// <param name="endPoint">Where the ray ends.</param>
	public BoundedRay(Location startPoint, Location endPoint) : this(startPoint, endPoint - startPoint) { }
	/// <summary>
	/// Constructs a new <see cref="BoundedRay"/> starting at <paramref name="startPoint"/> and extending by <paramref name="startToEndVect"/>.
	/// </summary>
	/// <param name="startPoint">Where the ray starts.</param>
	/// <param name="startToEndVect">The vector from <paramref name="startPoint"/> to the ray's end point.</param>
	public BoundedRay(Location startPoint, Vect startToEndVect) {
		_startPoint = startPoint;
		_vect = startToEndVect;
	}

	#region Random
	/// <summary>
	/// Produces a random ray. <see cref="StartPoint"/> and <see cref="EndPoint"/> will both be within
	/// the bounds <c>&lt;-100, -100, -100&gt; to &lt;100, 100, 100&gt;</c>.
	/// May (extremely rarely) have <see cref="Length"/> of <c>0</c>.
	/// </summary>
	public static BoundedRay Random() => new(Location.Random(), Location.Random());
	/// <summary>
	/// Produces a random ray, with <see cref="StartPoint"/> and <see cref="EndPoint"/> each independently randomized between the corresponding endpoints of <paramref name="minInclusive"/> and <paramref name="maxExclusive"/> (see <see cref="Location.Random(Location,Location)"/>).
	/// </summary>
	/// <param name="minInclusive">The lower bound for each endpoint.</param>
	/// <param name="maxExclusive">The upper bound for each endpoint.</param>
	public static BoundedRay Random(BoundedRay minInclusive, BoundedRay maxExclusive) => new(Location.Random(minInclusive.StartPoint, maxExclusive.StartPoint), Location.Random(minInclusive.EndPoint, maxExclusive.EndPoint));
	#endregion

	#region Span Conversions
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = Location.SerializationByteSpanLength * 2;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, BoundedRay src) {
		Location.SerializeToBytes(dest, src.StartPoint);
		Location.SerializeToBytes(dest[Location.SerializationByteSpanLength..], src.EndPoint);
	}

	/// <inheritdoc />
	public static BoundedRay DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			Location.DeserializeFromBytes(src),
			Location.DeserializeFromBytes(src[Location.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region String Conversions
	/// <inheritdoc />
	public override string ToString() => ToString(null, null);
	/// <inheritdoc />
	public string ToStringDescriptive() {
		return $"{nameof(BoundedRay)}{GeometryUtils.ParameterStartToken}" +
			   $"{nameof(StartPoint)}{GeometryUtils.ParameterKeyValueSeparatorToken}{StartPoint}{GeometryUtils.ParameterSeparatorToken}" +
			   $"{nameof(EndPoint)}{GeometryUtils.ParameterKeyValueSeparatorToken}{EndPoint}{GeometryUtils.ParameterSeparatorToken}" +
			   $"{nameof(Length)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Length:N2}{GeometryUtils.ParameterSeparatorToken}" +
			   $"{nameof(Direction)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Direction.ToStringDescriptive()}" +
			   $"{GeometryUtils.ParameterEndToken}";
	}
	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(BoundedRay), (nameof(StartPoint), _startPoint), (nameof(EndPoint), EndPoint));
	/// <inheritdoc />
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(BoundedRay), (nameof(StartPoint), _startPoint), (nameof(EndPoint), EndPoint));

	/// <inheritdoc />
	public static BoundedRay Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out BoundedRay result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static BoundedRay Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Location startPoint, out Location endPoint);
		return new(startPoint, endPoint);
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BoundedRay result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Location startPoint, out Location endPoint)) return false;
		result = new(startPoint, endPoint);
		return true;
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	public bool Equals(BoundedRay other) => _startPoint.Equals(other._startPoint) && _vect.Equals(other._vect);
	/// <summary>
	/// Determines whether this ray is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares <see cref="StartPoint"/> and <see cref="EndPoint"/> independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(BoundedRay other, float tolerance) => StartPoint.Equals(other.StartPoint, tolerance) && EndPoint.Equals(other.EndPoint, tolerance);
	/// <summary>
	/// Determines whether this ray occupies the same physical line segment as <paramref name="other"/>, regardless of which end is considered the start.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="Equals(BoundedRay)"/>, this also returns <see langword="true"/> if this ray is equal to <paramref name="other"/> flipped end-to-end (see <see cref="Flipped"/>).
	/// </remarks>
	/// <param name="other">The other ray to compare to.</param>
	public bool IsEquivalentDisregardingDirection(BoundedRay other) => Equals(other) || Equals(other.Flipped);
	/// <summary>
	/// Determines whether this ray occupies the same physical line segment as <paramref name="other"/>, within a given <paramref name="tolerance"/>, regardless of which end is considered the start.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="Equals(BoundedRay,float)"/>, this also returns <see langword="true"/> if this ray is equal to <paramref name="other"/> flipped end-to-end (see <see cref="Flipped"/>).
	/// </remarks>
	/// <param name="other">The other ray to compare to.</param>
	/// <param name="tolerance">The tolerance value.</param>
	public bool IsEquivalentDisregardingDirection(BoundedRay other, float tolerance) => Equals(other, tolerance) || Equals(other.Flipped, tolerance);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is BoundedRay other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_startPoint, _vect);
	/// <inheritdoc />
	public static bool operator ==(BoundedRay left, BoundedRay right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(BoundedRay left, BoundedRay right) => !left.Equals(right);
	#endregion
}