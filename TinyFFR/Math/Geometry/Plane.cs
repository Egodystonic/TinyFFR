// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct Plane : IMathPrimitive<Plane, float>, IPointTestable, ILineTestable, IPrecomputationInterpolatable<Plane, Rotation>, IBoundedRandomizable<Plane>, IDescriptiveStringProvider {
	public const float DefaultPlaneThickness = ILine.DefaultLineThickness;
	readonly Vector3 _normal;
	readonly float _coefficientOfNormalToCreateMinimalVectFromPlaneToOrigin;

	public Direction Normal {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Direction.FromVector3(_normal);
	}

	public Location PointClosestToOrigin {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Location.FromVector3(_normal * -_coefficientOfNormalToCreateMinimalVectFromPlaneToOrigin);
	}

	public Plane(Direction normal, Location anyPointOnPlane) : this(normal.ToVector3(), -Vector3.Dot(normal.ToVector3(), -anyPointOnPlane.ToVector3())) { }

	Plane(Vector3 normal, float coefficientOfNormal) {
		_normal = normal;
		_coefficientOfNormalToCreateMinimalVectFromPlaneToOrigin = coefficientOfNormal;
	}

	// TODO in xmldoc note that this is a translation in the direction of the normal vector, e.g. how far in the direction of the normal vector is the plane's closest point away from the origin
	public static Plane FromNormalAndTranslationFromOrigin(Direction normal, float translationFromOrigin) => new(normal.ToVector3(), -translationFromOrigin);

	public static Plane FromPointClosestToOriginAndOrientation(bool normalFacesOrigin, Location pointClosestToOrigin) {
		var vectFromOriginToClosestPoint = (Vect) pointClosestToOrigin;
		return new(vectFromOriginToClosestPoint.Direction.ToVector3(), vectFromOriginToClosestPoint.Length * (normalFacesOrigin ? 1f : -1f));
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
			throw new ArgumentException($"The three given locations ({a}, {b}, {c}) were colinear (along {line}) (i.e. they do not form a triangle), " +
										$"which is not sufficient to specify a single unique {nameof(Plane)}.");
		}
		else {
			throw new ArgumentException($"The three given locations were all identical, " +
										$"which is not sufficient to specify a single unique {nameof(Plane)}.");
		}
	}

	public static ReadOnlySpan<float> ConvertToSpan(in Plane src) => MemoryMarshal.Cast<Plane, float>(new ReadOnlySpan<Plane>(in src));
	public static Plane ConvertFromSpan(ReadOnlySpan<float> src) => MemoryMarshal.Cast<float, Plane>(src)[0];

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

	public static Plane Interpolate(Plane start, Plane end, float distance) {
		return new(
			Direction.Interpolate(start.Normal, end.Normal, distance),
			Location.Interpolate(start.PointClosestToOrigin, end.PointClosestToOrigin, distance)
		);
	}
	public static Rotation CreateInterpolationPrecomputation(Plane start, Plane end) => Direction.CreateInterpolationPrecomputation(start.Normal, end.Normal);
	public static Plane InterpolateUsingPrecomputation(Plane start, Plane end, Rotation precomputation, float distance) {
		return new(
			Direction.InterpolateUsingPrecomputation(start.Normal, end.Normal, precomputation, distance),
			Location.Interpolate(start.PointClosestToOrigin, end.PointClosestToOrigin, distance)
		);
	}
	public static Plane CreateNewRandom() => new(Direction.CreateNewRandom(), Location.CreateNewRandom());
	public static Plane CreateNewRandom(Plane minInclusive, Plane maxExclusive) => new(Direction.CreateNewRandom(minInclusive.Normal, maxExclusive.Normal), Location.CreateNewRandom(minInclusive.PointClosestToOrigin, maxExclusive.PointClosestToOrigin));

	#region Equality
	public bool Equals(Plane other) => _normal.Equals(other._normal) && _coefficientOfNormalToCreateMinimalVectFromPlaneToOrigin.Equals(other._coefficientOfNormalToCreateMinimalVectFromPlaneToOrigin);
	public bool Equals(Plane other, float tolerance) => _normal.Equals(other._normal) && _coefficientOfNormalToCreateMinimalVectFromPlaneToOrigin.Equals(other._coefficientOfNormalToCreateMinimalVectFromPlaneToOrigin);
	public bool EqualsWithinAngleAndDistanceFromOrigin(Plane other, Angle angle, float distance) => Normal.EqualsWithinAngle(other.Normal, angle) && PointClosestToOrigin.EqualsWithinDistance(other.PointClosestToOrigin, distance);
	public override bool Equals(object? obj) => obj is Plane other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_normal, _coefficientOfNormalToCreateMinimalVectFromPlaneToOrigin);
	public static bool operator ==(Plane left, Plane right) => left.Equals(right);
	public static bool operator !=(Plane left, Plane right) => !left.Equals(right);
	#endregion
}