// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents an infinitely-long line that passes through <see cref="PointOnLine"/> and extends forever in both <see cref="Direction"/> and its opposite.
/// </summary>
/// <remarks>
/// Unlike <see cref="Ray"/> and <see cref="BoundedRay"/>, a <see cref="Line"/> has no start or end: it is the same line whichever way <see cref="Direction"/> points, and <see cref="PointOnLine"/> can be any point that lies on it.
/// </remarks>
/// <seealso cref="Ray"/>
/// <seealso cref="BoundedRay"/>
[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 4 * 2)]
public readonly partial struct Line : ILineLike<Line, Ray, Ray>, IPrecomputationInterpolatable<Line, Rotation>, IDescriptiveStringProvider {
	readonly Location _pointOnLine;
	readonly Direction _direction;

	/// <summary>
	/// An arbitrary point that lies on this line.
	/// </summary>
	public Location PointOnLine {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _pointOnLine;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _pointOnLine = value;
	}
	/// <summary>
	/// One of the two directions this line extends towards from <see cref="PointOnLine"/>.
	/// </summary>
	/// <remarks>
	/// Because a line has no start or end, its opposite (<c>-Direction</c>) describes the exact same line.
	/// </remarks>
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

	/// <summary>
	/// Constructs a new <see cref="Line"/> passing through <paramref name="pointOnLine"/> in <paramref name="direction"/> (and its opposite).
	/// </summary>
	/// <param name="pointOnLine">Any point that lies on the line.</param>
	/// <param name="direction">One of the two directions the line extends towards.</param>
	public Line(Location pointOnLine, Direction direction) {
		_pointOnLine = pointOnLine;
		_direction = direction;
	}

	#region Factories and Conversions
	/// <summary>
	/// Constructs the <see cref="Line"/> that passes through both <paramref name="firstPointOnLine"/> and <paramref name="secondPointOnLine"/>.
	/// </summary>
	/// <param name="firstPointOnLine">A point on the desired line. Becomes <see cref="PointOnLine"/>.</param>
	/// <param name="secondPointOnLine">A different point on the desired line.</param>
	public static Line FromTwoPoints(Location firstPointOnLine, Location secondPointOnLine) {
		return new(firstPointOnLine, (secondPointOnLine - firstPointOnLine).Direction);
	}
	#endregion

	#region Random
	/// <summary>
	/// Produces a random line, with both <see cref="PointOnLine"/> and <see cref="Direction"/> independently randomized (see <see cref="Location.Random()"/> and <see cref="Direction.Random()"/>).
	/// </summary>
	public static Line Random() => new(Location.Random(), Direction.Random());
	/// <summary>
	/// Produces a random line, with <see cref="PointOnLine"/> and <see cref="Direction"/> each independently randomized between the corresponding values of <paramref name="minInclusive"/> and <paramref name="maxExclusive"/> (see <see cref="Location.Random(Location,Location)"/> and <see cref="Direction.Random(Direction,Direction)"/>).
	/// </summary>
	/// <param name="minInclusive">The lower bound for <see cref="PointOnLine"/> and <see cref="Direction"/>.</param>
	/// <param name="maxExclusive">The upper bound for <see cref="PointOnLine"/> and <see cref="Direction"/>.</param>
	public static Line Random(Line minInclusive, Line maxExclusive) => new(Location.Random(minInclusive.PointOnLine, maxExclusive.PointOnLine), Direction.Random(minInclusive.Direction, maxExclusive.Direction));
	#endregion

	#region Span Conversions
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = Location.SerializationByteSpanLength + Direction.SerializationByteSpanLength;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, Line src) {
		Location.SerializeToBytes(dest, src.PointOnLine);
		Direction.SerializeToBytes(dest[Location.SerializationByteSpanLength..], src.Direction);
	}

	/// <inheritdoc />
	public static Line DeserializeFromBytes(ReadOnlySpan<byte> src) {
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
	public string ToStringDescriptive() => $"{nameof(Line)}{GeometryUtils.ParameterStartToken}{nameof(PointOnLine)}{GeometryUtils.ParameterKeyValueSeparatorToken}{PointOnLine}{GeometryUtils.ParameterSeparatorToken}{nameof(PointClosestToOrigin)}{GeometryUtils.ParameterKeyValueSeparatorToken}{PointClosestToOrigin()}{GeometryUtils.ParameterSeparatorToken}{nameof(Direction)}{GeometryUtils.ParameterKeyValueSeparatorToken}{_direction.ToStringDescriptive()}{GeometryUtils.ParameterEndToken}";
	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Line), (nameof(PointOnLine), PointOnLine), (nameof(Direction), _direction));
	/// <inheritdoc />
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Line), (nameof(PointOnLine), PointOnLine), (nameof(Direction), _direction));

	/// <inheritdoc />
	public static Line Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Line result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static Line Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Location closestPointToOrigin, out Direction direction);
		return new(closestPointToOrigin, direction);
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Line result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Location closestPointToOrigin, out Direction direction)) return false;
		result = new(closestPointToOrigin, direction);
		return true;
	}
	#endregion

	#region Equality
	/// <summary>
	/// Determines whether this line describes the exact same infinite line as <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// Because a line has no start/end or canonical position, this returns <see langword="true"/> whenever the two lines are colinear and parallel — <paramref name="other"/>'s <see cref="Direction"/> may point the same way or the exact opposite way, and its <see cref="PointOnLine"/> may be a different point altogether, so long as it still lies on this line.
	/// </remarks>
	/// <param name="other">The other value.</param>
	public bool Equals(Line other) => DistanceFrom(other) == 0f && (Direction.Equals(other.Direction) || Direction.Equals(-other.Direction));
	/// <summary>
	/// Determines whether this line is equal to <paramref name="other"/> (see <see cref="Equals(Line)"/>) within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	public bool Equals(Line other, float tolerance) => DistanceFrom(other) <= tolerance && (Direction.Equals(other.Direction, tolerance) || Direction.Equals(-other.Direction, tolerance));
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Line other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_pointOnLine, _direction);
	/// <inheritdoc />
	public static bool operator ==(Line left, Line right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(Line left, Line right) => !left.Equals(right);
	#endregion
}