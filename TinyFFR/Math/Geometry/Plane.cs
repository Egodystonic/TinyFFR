// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
public readonly partial struct Plane : IGeometryPrimitive<Plane>, IPrecomputationInterpolatable<Plane, Rotation>, IDescriptiveStringProvider {
	public const float DefaultPlaneThickness = ILine.DefaultLineThickness;
	readonly Vector3 _normal;
	readonly float _smallestDistanceFromOriginAlongNormal;

	public Direction Normal {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Direction.FromVector3(_normal);
	}

	public Location ClosestPointToOrigin {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Location.FromVector3(_normal * _smallestDistanceFromOriginAlongNormal);
	}

	public Plane(Direction normal, Location anyPointOnPlane) : this(normal.ToVector3(), Vector3.Dot(normal.ToVector3(), anyPointOnPlane.ToVector3())) { }

	Plane(Vector3 normal, float coefficientOfNormal) {
		_normal = normal;
		_smallestDistanceFromOriginAlongNormal = coefficientOfNormal;
	}

	#region Factories and Conversions
	// TODO in xmldoc note that this is the minimum distance from the origin to the plane along the normal, e.g. positive means the normal points away from the origin
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane FromNormalAndDistanceFromOrigin(Direction normal, float signedDistanceFromOrigin) => new(normal.ToVector3(), signedDistanceFromOrigin);

	// TODO in xmldoc recommend using FromNormalAndDistanceFromOrigin where possible as it can't throw an exception and it's faster
	public static Plane FromPointClosestToOrigin(Location pointClosestToOrigin, bool normalFacesOrigin) {
		var vectFromOriginToClosestPoint = (Vect) pointClosestToOrigin;
		var direction = vectFromOriginToClosestPoint.Direction;
		if (direction == Direction.None) {
			throw new ArgumentException($"{nameof(FromPointClosestToOrigin)} can not be used when {nameof(pointClosestToOrigin)} is equal to {nameof(Location.Origin)} " +
										$"as there are infinite possible solutions.", nameof(pointClosestToOrigin));
		}
		return new(direction.ToVector3() * (normalFacesOrigin ? -1f : 1f), vectFromOriginToClosestPoint.Length * (normalFacesOrigin ? -1f : 1f));
	}

	public static Plane FromTriangleOnSurface(Location a, Location b, Location c) {
		var normal = Direction.FromVector3(Vector3.Cross(b.ToVector3() - a.ToVector3(), c.ToVector3() - a.ToVector3()));
		if (normal != Direction.None) return new(normal, a);

		// Everything below this line is just handling the fact that the points are colinear and creating the right exception message
		Line? line;
		if (!a.Equals(b, 0.001f)) line = new Line(a, b);
		else if (!b.Equals(c, 0.001f)) line = new Line(b, c);
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
	public static ReadOnlySpan<float> ConvertToSpan(in Plane src) => MemoryMarshal.Cast<Plane, float>(new ReadOnlySpan<Plane>(in src));
	public static Plane ConvertFromSpan(ReadOnlySpan<float> src) => MemoryMarshal.Cast<float, Plane>(src)[0];
	#endregion

	#region String Conversions
	public string ToStringDescriptive() => $"{nameof(Plane)}{GeometryUtils.ParameterStartToken}" +
										   $"{nameof(Normal)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Normal.ToStringDescriptive()}{GeometryUtils.ParameterSeparatorToken}" +
										   $"{nameof(ClosestPointToOrigin)}{GeometryUtils.ParameterKeyValueSeparatorToken}{ClosestPointToOrigin}" +
										   $"{GeometryUtils.ParameterEndToken}";

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Plane), (nameof(Normal), Normal), (nameof(ClosestPointToOrigin), ClosestPointToOrigin));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Plane), (nameof(Normal), Normal), (nameof(ClosestPointToOrigin), ClosestPointToOrigin));

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
	public bool Equals(Plane other, float tolerance) => Normal.Equals(other.Normal, tolerance) && ClosestPointToOrigin.Equals(other.ClosestPointToOrigin, tolerance);
	public bool EqualsWithinAngleAndLocation(Plane other, Angle angle, float distance) => Normal.EqualsWithinAngle(other.Normal, angle) && ClosestPointToOrigin.EqualsWithinDistance(other.ClosestPointToOrigin, distance);
	public override bool Equals(object? obj) => obj is Plane other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_normal, _smallestDistanceFromOriginAlongNormal);
	public static bool operator ==(Plane left, Plane right) => left.Equals(right);
	public static bool operator !=(Plane left, Plane right) => !left.Equals(right);
	#endregion
}