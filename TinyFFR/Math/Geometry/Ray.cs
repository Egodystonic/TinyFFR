// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents an infinitely-long line that starts at <see cref="StartPoint"/> and extends forever in one <see cref="Direction"/>.
/// </summary>
/// <remarks>
/// In contrast to <see cref="Line"/>, a <see cref="Ray"/> only extends in one direction.
/// </remarks>
/// <seealso cref="Line"/>
/// <seealso cref="BoundedRay"/>
[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 4 * 2)]
public readonly partial struct Ray : ILineLike<Ray, BoundedRay, Ray>, IPrecomputationInterpolatable<Ray, Rotation>, IDescriptiveStringProvider {
	readonly Location _startPoint;
	readonly Direction _direction;

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
	/// The direction this ray extends towards from <see cref="StartPoint"/>.
	/// </summary>
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

	/// <summary>
	/// Constructs a new <see cref="Ray"/> starting at <paramref name="startPoint"/> and extending towards <paramref name="direction"/>.
	/// </summary>
	/// <param name="startPoint">Where the ray starts.</param>
	/// <param name="direction">The direction the ray extends towards.</param>
	public Ray(Location startPoint, Direction direction) {
		_startPoint = startPoint;
		_direction = direction;
	}

	#region Random
	/// <summary>
	/// Produces a random ray, with both <see cref="StartPoint"/> and <see cref="Direction"/> independently randomized (see <see cref="Location.Random()"/> and <see cref="Direction.Random()"/>).
	/// </summary>
	public static Ray Random() => new(Location.Random(), Direction.Random());
	/// <summary>
	/// Produces a random ray, with <see cref="StartPoint"/> and <see cref="Direction"/> each independently randomized between the corresponding values of <paramref name="minInclusive"/> and <paramref name="maxExclusive"/> (see <see cref="Location.Random(Location,Location)"/> and <see cref="Direction.Random(Direction,Direction)"/>).
	/// </summary>
	/// <param name="minInclusive">The lower bound for <see cref="StartPoint"/> and <see cref="Direction"/>.</param>
	/// <param name="maxExclusive">The upper bound for <see cref="StartPoint"/> and <see cref="Direction"/>.</param>
	public static Ray Random(Ray minInclusive, Ray maxExclusive) => new(Location.Random(minInclusive.StartPoint, maxExclusive.StartPoint), Direction.Random(minInclusive.Direction, maxExclusive.Direction));
	#endregion

	#region Span Conversions
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = Location.SerializationByteSpanLength + Direction.SerializationByteSpanLength;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, Ray src) {
		Location.SerializeToBytes(dest, src.StartPoint);
		Direction.SerializeToBytes(dest[Location.SerializationByteSpanLength..], src.Direction);
	}

	/// <inheritdoc />
	public static Ray DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			Location.DeserializeFromBytes(src),
			Direction.DeserializeFromBytes(src[Location.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region String Conversions
	/// <inheritdoc />
	public override string ToString() => ToString(null, null);
	/// <inheritdoc />
	public string ToStringDescriptive() => $"{nameof(Ray)}{GeometryUtils.ParameterStartToken}{nameof(StartPoint)}{GeometryUtils.ParameterKeyValueSeparatorToken}{_startPoint}{GeometryUtils.ParameterSeparatorToken}{nameof(Direction)}{GeometryUtils.ParameterKeyValueSeparatorToken}{_direction.ToStringDescriptive()}{GeometryUtils.ParameterEndToken}";
	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Ray), (nameof(StartPoint), _startPoint), (nameof(Direction), _direction));
	/// <inheritdoc />
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Ray), (nameof(StartPoint), _startPoint), (nameof(Direction), _direction));

	/// <inheritdoc />
	public static Ray Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Ray result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static Ray Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Location startPoint, out Direction direction);
		return new(startPoint, direction);
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Ray result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Location startPoint, out Direction direction)) return false;
		result = new(startPoint, direction);
		return true;
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	public bool Equals(Ray other) => _startPoint.Equals(other._startPoint) && _direction.Equals(other._direction);
	/// <summary>
	/// Determines whether this ray is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares <see cref="StartPoint"/> and <see cref="Direction"/> independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	public bool Equals(Ray other, float tolerance) => StartPoint.Equals(other.StartPoint, tolerance) && Direction.Equals(other.Direction, tolerance);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Ray other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_startPoint, _direction);
	/// <inheritdoc />
	public static bool operator ==(Ray left, Ray right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(Ray left, Ray right) => !left.Equals(right);
	#endregion
}