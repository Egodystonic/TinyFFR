// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents an infinite flat plane in 3D space, defined by a <see cref="Normal"/> direction and a distance from the origin.
/// </summary>
[DebuggerDisplay("{ToStringDescriptive()}")]
public readonly partial struct Plane : IMathPrimitive<Plane>, IDescriptiveStringProvider {
	/// <summary>
	/// A sensible default value to use as a "thickness" when testing whether something is considered to intersect or lie on a plane, to account for floating-point imprecision.
	/// </summary>
	public const float DefaultPlaneThickness = 0.01f;
	readonly Vector3 _normal;
	readonly float _smallestDistanceFromOriginAlongNormal;

	/// <summary>
	/// The direction this plane faces.
	/// </summary>
	public Direction Normal {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Direction.FromVector3(_normal);
	}

	/// <summary>
	/// The point on this plane that is closest to the world origin (<c>(0f, 0f, 0f)</c>).
	/// </summary>
	public Location PointClosestToOrigin {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Location.FromVector3(_normal * _smallestDistanceFromOriginAlongNormal);
	}

	/// <summary>
	/// Constructs a new <see cref="Plane"/> facing <paramref name="normal"/> and passing through the origin.
	/// </summary>
	/// <param name="normal">The direction the plane faces.</param>
	public Plane(Direction normal) : this(normal, 0f) { }
	/// <summary>
	/// Constructs a new <see cref="Plane"/> facing <paramref name="normal"/> and passing through <paramref name="anyPointOnPlane"/>.
	/// </summary>
	/// <param name="normal">The direction the plane faces.</param>
	/// <param name="anyPointOnPlane">Any point that should lie on the plane.</param>
	public Plane(Direction normal, Location anyPointOnPlane) : this(normal, Vector3.Dot(normal.ToVector3(), anyPointOnPlane.ToVector3())) { }
	/// <summary>
	/// Constructs a new <see cref="Plane"/> facing <paramref name="normal"/>, offset from the origin along that normal by <paramref name="coefficientOfNormalFromOrigin"/>.
	/// </summary>
	/// <remarks>
	/// <paramref name="coefficientOfNormalFromOrigin"/> is the (signed) distance from the origin to the plane, measured along <paramref name="normal"/>: a positive value means <see cref="PointClosestToOrigin"/> is out in front of the origin (i.e. <paramref name="normal"/> points away from the origin), and a negative value means it is behind (i.e. <paramref name="normal"/> points towards the origin).
	/// </remarks>
	/// <param name="normal">The direction the plane faces.</param>
	/// <param name="coefficientOfNormalFromOrigin">The signed distance from the origin to the plane, along <paramref name="normal"/>.</param>
	public Plane(Direction normal, float coefficientOfNormalFromOrigin) {
		_normal = normal.ToVector3();
		_smallestDistanceFromOriginAlongNormal = coefficientOfNormalFromOrigin;
	}

	#region Factories and Conversions
	/// <summary>
	/// Constructs the <see cref="Plane"/> whose <see cref="PointClosestToOrigin"/> is <paramref name="pointClosestToOrigin"/>.
	/// </summary>
	/// <param name="pointClosestToOrigin">The desired <see cref="PointClosestToOrigin"/>.</param>
	/// <param name="normalFacesOrigin">If <see langword="true"/>, <see cref="Normal"/> will point back towards the origin; if <see langword="false"/>, it will point away from the origin.</param>
	/// <returns><see langword="null"/> if <paramref name="pointClosestToOrigin"/> is the origin itself (in which case <see cref="Normal"/> would be undefined); the constructed plane otherwise.</returns>
	public static Plane? FromPointClosestToOrigin(Location pointClosestToOrigin, bool normalFacesOrigin) {
		var vectFromOriginToClosestPoint = (Vect) pointClosestToOrigin;
		var direction = vectFromOriginToClosestPoint.Direction;
		if (direction == Direction.None) return null;
		return new(normalFacesOrigin ? direction.Flipped : direction, vectFromOriginToClosestPoint.Length * (normalFacesOrigin ? -1f : 1f));
	}

	/// <summary>
	/// Constructs the <see cref="Plane"/> that passes through all three of <paramref name="a"/>, <paramref name="b"/> and <paramref name="c"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="Normal"/> will face "out" from the triangle formed by the three points, where the "out" direction is the one you'd be looking from if <paramref name="a"/>, <paramref name="b"/> and <paramref name="c"/> appeared to cycle anticlockwise from your point of view.
	/// </remarks>
	/// <param name="a">The first point on the plane.</param>
	/// <param name="b">The second point on the plane.</param>
	/// <param name="c">The third point on the plane.</param>
	/// <returns><see langword="null"/> if the three points are colinear (and so do not uniquely determine a plane); the constructed plane otherwise.</returns>
	public static Plane? FromTriangleOnSurface(Location a, Location b, Location c) {
		var normal = Direction.FromVector3(Vector3.Cross(b.ToVector3() - a.ToVector3(), c.ToVector3() - a.ToVector3()));
		if (normal == Direction.None) return null;
		return new(normal, a);
	}
	#endregion

	#region Random
	/// <summary>
	/// Produces a random plane, with both <see cref="Normal"/> and <see cref="PointClosestToOrigin"/> independently randomized (see <see cref="Direction.Random()"/> and <see cref="Location.Random()"/>).
	/// </summary>
	public static Plane Random() => new(Direction.Random(), Location.Random());
	/// <summary>
	/// Produces a random plane, with <see cref="Normal"/> and <see cref="PointClosestToOrigin"/> each independently randomized between the corresponding values of <paramref name="minInclusive"/> and <paramref name="maxExclusive"/> (see <see cref="Direction.Random(Direction,Direction)"/> and <see cref="Location.Random(Location,Location)"/>).
	/// </summary>
	/// <param name="minInclusive">The lower bound for <see cref="Normal"/> and <see cref="PointClosestToOrigin"/>.</param>
	/// <param name="maxExclusive">The upper bound for <see cref="Normal"/> and <see cref="PointClosestToOrigin"/>.</param>
	public static Plane Random(Plane minInclusive, Plane maxExclusive) => new(Direction.Random(minInclusive.Normal, maxExclusive.Normal), Location.Random(minInclusive.PointClosestToOrigin, maxExclusive.PointClosestToOrigin));
	#endregion

	#region Span Conversions
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = Direction.SerializationByteSpanLength + Location.SerializationByteSpanLength;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, Plane src) {
		Direction.SerializeToBytes(dest, src.Normal);
		Location.SerializeToBytes(dest[Direction.SerializationByteSpanLength..], src.PointClosestToOrigin);
	}

	/// <inheritdoc />
	public static Plane DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			Direction.DeserializeFromBytes(src),
			Location.DeserializeFromBytes(src[Direction.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region String Conversions
	/// <inheritdoc />
	public string ToStringDescriptive() => $"{nameof(Plane)}{GeometryUtils.ParameterStartToken}" +
										   $"{nameof(Normal)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Normal.ToStringDescriptive()}{GeometryUtils.ParameterSeparatorToken}" +
										   $"{nameof(PointClosestToOrigin)}{GeometryUtils.ParameterKeyValueSeparatorToken}{PointClosestToOrigin}" +
										   $"{GeometryUtils.ParameterEndToken}";

	/// <inheritdoc />
	public override string ToString() => ToString(null, null);

	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Plane), (nameof(Normal), Normal), (nameof(PointClosestToOrigin), PointClosestToOrigin));
	/// <inheritdoc />
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Plane), (nameof(Normal), Normal), (nameof(PointClosestToOrigin), PointClosestToOrigin));

	/// <inheritdoc />
	public static Plane Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Plane result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static Plane Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Direction normal, out Location pointClosestToOrigin);
		return new(normal, pointClosestToOrigin);
	}

	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Plane result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Direction normal, out Location pointClosestToOrigin)) return false;
		result = new(normal, pointClosestToOrigin);
		return true;
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	public bool Equals(Plane other) => _normal.Equals(other._normal) && _smallestDistanceFromOriginAlongNormal.Equals(other._smallestDistanceFromOriginAlongNormal);
	/// <summary>
	/// Determines whether this plane is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares <see cref="Normal"/> and <see cref="PointClosestToOrigin"/> independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	public bool Equals(Plane other, float tolerance) => Normal.Equals(other.Normal, tolerance) && PointClosestToOrigin.Equals(other.PointClosestToOrigin, tolerance);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Plane other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_normal, _smallestDistanceFromOriginAlongNormal);
	/// <inheritdoc />
	public static bool operator ==(Plane left, Plane right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(Plane left, Plane right) => !left.Equals(right);
	#endregion
}