// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;

namespace Egodystonic.TinyFFR;

public enum PlaneObjectRelationship {
	PlaneIntersectsObject,
	PlaneFacesTowardsObject,
	PlaneFacesAwayFromObject
}

[DebuggerDisplay("{ToStringDescriptive()}")]
public readonly partial struct Plane : IMathPrimitive<Plane>, IDescriptiveStringProvider {
	public const float DefaultPlaneThickness = ILineLike.DefaultLineThickness;
	readonly Vector3 _normal;
	readonly float _smallestDistanceFromOriginAlongNormal;

	public Direction Normal {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Direction.FromVector3(_normal);
	}

	public Location PointClosestToOrigin {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Location.FromVector3(_normal * _smallestDistanceFromOriginAlongNormal);
	}

	public Plane(Direction normal, Location anyPointOnPlane) : this(normal, Vector3.Dot(normal.ToVector3(), anyPointOnPlane.ToVector3())) { }
	// TODO in xmldoc note that this is the minimum distance from the origin to the plane along the normal, e.g. positive means the normal points away from the origin
	public Plane(Direction normal, float coefficientOfNormalFromOrigin) {
		_normal = normal.ToVector3();
		_smallestDistanceFromOriginAlongNormal = coefficientOfNormalFromOrigin;
	}

	#region Factories and Conversions
	// TODO in xmldoc recommend using FromNormalAndDistanceFromOrigin where possible as it can't throw an exception and it's faster
	public static Plane FromPointClosestToOrigin(Location pointClosestToOrigin, bool normalFacesOrigin) {
		var vectFromOriginToClosestPoint = (Vect) pointClosestToOrigin;
		var direction = vectFromOriginToClosestPoint.Direction;
		if (direction == Direction.None) {
			throw new ArgumentException($"{nameof(FromPointClosestToOrigin)} can not be used when {nameof(pointClosestToOrigin)} is equal to {nameof(Location.Origin)} " +
										$"as there are infinite possible solutions.", nameof(pointClosestToOrigin));
		}
		return new(normalFacesOrigin ? direction.Flipped : direction, vectFromOriginToClosestPoint.Length * (normalFacesOrigin ? -1f : 1f));
	}

	public static Plane FromTriangleOnSurface(Location a, Location b, Location c) {
		var normal = Direction.FromVector3(Vector3.Cross(b.ToVector3() - a.ToVector3(), c.ToVector3() - a.ToVector3()));
		if (normal != Direction.None) return new(normal, a);

		// Everything below this line is just handling the fact that the points are colinear and creating the right exception message
		const float FloatingPointErrorMargin = 1E-2f; // Note: If changing this, Direction.Clamp(Direction,Direction) needs to be updated too
		Line? line;
		if (!a.Equals(b, FloatingPointErrorMargin)) line = Line.FromTwoPoints(a, b);
		else if (!b.Equals(c, FloatingPointErrorMargin)) line = Line.FromTwoPoints(b, c);
		else line = null;

		if (line != null) {
			throw new ArgumentException($"The three given locations ({a}, {b}, {c}) were colinear along {line}, " +
										$"which is not sufficient to specify a single unique {nameof(Plane)} (i.e. the locations do not form a triangle).");
		}
		else {
			throw new ArgumentException($"The three given locations were all identical, " +
										$"which is not sufficient to specify a single unique {nameof(Plane)} (i.e. the locations do not form a triangle).");
		}
	}
	#endregion

	#region Span Conversions
	public static int SerializationByteSpanLength { get; } = Direction.SerializationByteSpanLength + Location.SerializationByteSpanLength;

	public static void SerializeToBytes(Span<byte> dest, Plane src) {
		Direction.SerializeToBytes(dest, src.Normal);
		Location.SerializeToBytes(dest[Direction.SerializationByteSpanLength..], src.PointClosestToOrigin);
	}

	public static Plane DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			Direction.DeserializeFromBytes(src),
			Location.DeserializeFromBytes(src[Direction.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region String Conversions
	public string ToStringDescriptive() => $"{nameof(Plane)}{GeometryUtils.ParameterStartToken}" +
										   $"{nameof(Normal)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Normal.ToStringDescriptive()}{GeometryUtils.ParameterSeparatorToken}" +
										   $"{nameof(PointClosestToOrigin)}{GeometryUtils.ParameterKeyValueSeparatorToken}{PointClosestToOrigin}" +
										   $"{GeometryUtils.ParameterEndToken}";

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Plane), (nameof(Normal), Normal), (nameof(PointClosestToOrigin), PointClosestToOrigin));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Plane), (nameof(Normal), Normal), (nameof(PointClosestToOrigin), PointClosestToOrigin));

	public static Plane Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Plane result) => TryParse(s.AsSpan(), provider, out result);

	public static Plane Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Direction normal, out Location pointClosestToOrigin);
		return new(normal, pointClosestToOrigin);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Plane result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Direction normal, out Location pointClosestToOrigin)) return false;
		result = new(normal, pointClosestToOrigin);
		return true;
	}
	#endregion

	#region Equality
	public bool Equals(Plane other) => _normal.Equals(other._normal) && _smallestDistanceFromOriginAlongNormal.Equals(other._smallestDistanceFromOriginAlongNormal);
	public bool Equals(Plane other, float tolerance) => Normal.Equals(other.Normal, tolerance) && PointClosestToOrigin.Equals(other.PointClosestToOrigin, tolerance);
	public bool EqualsWithinDistanceAndAngle(Plane other, float distance, Angle angle) => Normal.EqualsWithinAngle(other.Normal, angle) && PointClosestToOrigin.EqualsWithinDistance(other.PointClosestToOrigin, distance);
	public override bool Equals(object? obj) => obj is Plane other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_normal, _smallestDistanceFromOriginAlongNormal);
	public static bool operator ==(Plane left, Plane right) => left.Equals(right);
	public static bool operator !=(Plane left, Plane right) => !left.Equals(right);
	#endregion
}