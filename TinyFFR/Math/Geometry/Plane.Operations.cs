// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

partial struct Plane : 
	IInvertible<Plane>,
	ITranslatable<Plane>,
	IPointRotatable<Plane>,
	IDistanceMeasurable<Plane, Plane>,
	ISignedDistanceMeasurable<Plane, Location>, IContainer<Plane, Location>, IClosestEndogenousPointDiscoverable<Plane, Location>,
	IAngleMeasurable<Plane, Direction>, IReflectionTarget<Plane, Direction, Direction>, IProjectionTarget<Plane, Direction>, IParallelizationTarget<Plane, Direction>, IOrthogonalizationTarget<Plane, Direction>,
	IAngleMeasurable<Plane, Vect>, IReflectionTarget<Plane, Vect, Vect>, IProjectionTarget<Plane, Vect>, IParallelizationTarget<Plane, Vect>, IOrthogonalizationTarget<Plane, Vect>,
	IPrecomputationInterpolatable<Plane, Rotation> {
	const float MaxPlaneToPlaneDistanceNormalSimilarity = 1 - 1E-8f;

	public Plane Flipped {
		get => new(-Normal, -_smallestDistanceFromOriginAlongNormal);
	}
	Plane IInvertible<Plane>.Inverted => Flipped;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator -(Plane operand) => operand.Flipped;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator +(Plane plane, Vect v) => plane.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator +(Vect v, Plane plane) => plane.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator -(Plane plane, Vect v) => plane.MovedBy(-v);
	public Plane MovedBy(Vect v) => new(Normal, ClosestPointToOrigin + v);
	

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *(Plane plane, (Location Pivot, Rotation Rotation) rotTuple) => plane.RotatedAroundPoint(rotTuple.Rotation, rotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *((Location Pivot, Rotation Rotation) rotTuple, Plane plane) => plane.RotatedAroundPoint(rotTuple.Rotation, rotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *(Plane plane, (Rotation Rotation, Location Pivot) rotTuple) => plane.RotatedAroundPoint(rotTuple.Rotation, rotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *((Rotation Rotation, Location Pivot) rotTuple, Plane plane) => plane.RotatedAroundPoint(rotTuple.Rotation, rotTuple.Pivot);
	public Plane RotatedAroundPoint(Rotation rot, Location pivotPoint) => new(Normal * rot, PointClosestTo(pivotPoint) * (pivotPoint, rot));

	// TODO explain in XML that this is a normalized value from 0 to 1, where 1 is a direction completely perpendicular to the plane and 0 is completely parallel; and is also the cosine of the angle formed with the normal
	public float PerpendicularityWith(Direction direction) => MathF.Abs(Normal.Dot(direction));

	// TODO I'd like a function here to convert locations to XYPairs on the surface of the plane given a centre point (default PointClosestToOrigin)

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Direction dir) => plane.AngleTo(dir);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Direction dir, Plane plane) => plane.AngleTo(dir);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Vect v) => plane.AngleTo(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Vect v, Plane plane) => plane.AngleTo(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane other) => Angle.FromRadians(MathF.Acos(PerpendicularityWith(other.Normal)));
	public Angle AngleTo(Direction direction) => Angle.FromRadians(MathF.Asin(PerpendicularityWith(direction)));
	public Angle AngleTo(Vect vect) => AngleTo(vect.Direction);
	public Direction ReflectionOf(Direction direction) { // TODO explain in XML that this returns the same direction if the input is parallel to the plane
		return Direction.FromVector3(-2f * Vector3.Dot(Normal.ToVector3(), direction.ToVector3()) * Normal.ToVector3() + direction.ToVector3());
	}
	public Vect ReflectionOf(Vect vect) { // TODO explain in XML that this returns the same direction if the input is parallel to the plane
		return Vect.FromVector3(-2f * Vector3.Dot(Normal.ToVector3(), vect.ToVector3()) * Normal.ToVector3() + vect.ToVector3());
	}
	Direction? IReflectionTarget<Direction, Direction>.ReflectionOf(Direction direction) => ReflectionOf(direction);
	Vect? IReflectionTarget<Vect, Vect>.ReflectionOf(Vect vect) => ReflectionOf(vect);
	public Direction ParallelizationOf(Direction direction) => direction.OrthogonalizedAgainst(Normal);

	public Location PointClosestTo(Location location) => location - ClosestPointToOrigin.VectTo(location).ProjectedOnTo(Normal);
	public float SignedDistanceFrom(Location location) => Vector3.Dot(location.ToVector3(), _normal) - _smallestDistanceFromOriginAlongNormal; // TODO xmldoc positive means normal faces towards, etc
	public float DistanceFrom(Location location) => MathF.Abs(SignedDistanceFrom(location));
	float IDistanceMeasurable<Location>.DistanceSquaredFrom(Location location) {
		var distanceSqrt = DistanceFrom(location);
		return distanceSqrt * distanceSqrt;
	}
	public float SignedDistanceFromOrigin() => -_smallestDistanceFromOriginAlongNormal;
	public float DistanceFromOrigin() => MathF.Abs(SignedDistanceFromOrigin());

	// TODO in Xml make it clear that "facing towards" means the normal is pointing out of this side of the plane; and that points on the plane (within the thickness value) will return false
	// Implementation note: We use a plane thickness by default because relying on the signed distance being exactly 0 for anything other than axis-aligned planes is pretty much stochastic
	// due to FP inaccuracy. Even for axis-aligned ones it's still pretty bad, but is possibly more consistent when moving around on the surface of the plane. In these cases, if users
	// really want 0-thickness planes, they can still specify as such using the overloads that take a thickness parameter.
	public bool FacesTowards(Location location) => FacesTowards(location, DefaultPlaneThickness);
	public bool FacesAwayFrom(Location location) => FacesAwayFrom(location, DefaultPlaneThickness);
	public bool FacesTowards(Location location, float planeThickness) => SignedDistanceFrom(location) > planeThickness;
	public bool FacesAwayFrom(Location location, float planeThickness) => SignedDistanceFrom(location) < -planeThickness;
	public bool FacesTowardsOrigin() => FacesTowardsOrigin(DefaultPlaneThickness);
	public bool FacesAwayFromOrigin() => FacesAwayFromOrigin(DefaultPlaneThickness);
	public bool FacesTowardsOrigin(float planeThickness) => SignedDistanceFromOrigin() > planeThickness;
	public bool FacesAwayFromOrigin(float planeThickness) => SignedDistanceFromOrigin() < -planeThickness;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, DefaultPlaneThickness);
	public bool Contains(Location location, float planeThickness) => DistanceFrom(location) <= planeThickness;

	public float DistanceFrom(Plane other) => MathF.Abs(Normal.Dot(other.Normal)) >= MaxPlaneToPlaneDistanceNormalSimilarity ? ClosestPointToOrigin.DistanceFrom(other.ClosestPointToOrigin) : 0f;
	public float DistanceSquaredFrom(Plane other) => MathF.Abs(Normal.Dot(other.Normal)) >= MaxPlaneToPlaneDistanceNormalSimilarity ? ClosestPointToOrigin.DistanceSquaredFrom(other.ClosestPointToOrigin) : 0f;
	
	public bool IsIntersectedBy(Plane other) => Vector3.Cross(_normal, other._normal).LengthSquared() != 0f;
	public Line? IntersectionWith(Plane other) {
		static (float A, float B) FindNonZeroComponents(float thisA, float thisB, float thisCoefficient, float otherA, float otherB, float otherCoefficient) {
			var divisor = thisA * otherB - otherA * thisB;

			return (
				(otherB * thisCoefficient - thisB * otherCoefficient) / divisor,
				(thisA * otherCoefficient - otherA * thisCoefficient) / divisor
			);
		}

		var lineDirection = Vector3.Cross(_normal, other._normal);
		if (lineDirection.LengthSquared() == 0f) return null; // parallel planes

		var dirXAbs = MathF.Abs(lineDirection.X);
		var dirYAbs = MathF.Abs(lineDirection.Y);
		var dirZAbs = MathF.Abs(lineDirection.Z);

		if (dirXAbs > dirYAbs) {
			if (dirXAbs > dirZAbs) goto calculateUsingZeroX;
			else goto calculateUsingZeroZ;
		}
		else if (dirYAbs > dirZAbs) goto calculateUsingZeroY;
		else goto calculateUsingZeroZ;

		calculateUsingZeroX: {
			var (y, z) = FindNonZeroComponents(_normal.Y, _normal.Z, _smallestDistanceFromOriginAlongNormal, other._normal.Y, other._normal.Z, other._smallestDistanceFromOriginAlongNormal);
			return new Line((0f, y, z), Direction.FromVector3(lineDirection));
		}

		calculateUsingZeroY: {
			var (x, z) = FindNonZeroComponents(_normal.X, _normal.Z, _smallestDistanceFromOriginAlongNormal, other._normal.X, other._normal.Z, other._smallestDistanceFromOriginAlongNormal);
			return new Line((x, 0f, z), Direction.FromVector3(lineDirection));
		}

		calculateUsingZeroZ: {
			var (x, y) = FindNonZeroComponents(_normal.X, _normal.Y, _smallestDistanceFromOriginAlongNormal, other._normal.X, other._normal.Y, other._smallestDistanceFromOriginAlongNormal);
			return new Line((x, y, 0f), Direction.FromVector3(lineDirection));
		}
	}

	public Vect ProjectionOf(Vect vect) => vect - vect.ProjectedOnTo(Normal); // TODO in xmldoc mention that length will be 0 if this is perpendicular, regardless
	public Vect ParallelizationOf(Vect vect) => ProjectionOf(vect).WithLength(vect.Length); // TODO in xmldoc mention that length will be 0 if this is perpendicular, regardless

	public Direction ProjectionOf(Direction direction) => direction.OrthogonalizedAgainst(Normal);

	// TODO xmldoc explain that these two methods will basically just make the vect/dir point either along the normal or opposite, whichever they're closer to
	public Vect OrthogonalizationOf(Vect vect) => OrthogonalizationOf(vect.Direction) * vect.Length;
	// Idea here is to pick the closest direction (normal or -normal) and have parallel directions just pick the positive normal, all without branching. There's probably a smarter way to do it but I'm not smart enough to know it
	public Direction OrthogonalizationOf(Direction direction) => Direction.FromVector3PreNormalized(Normal.ToVector3() * MathF.Sign(direction.Dot(Normal) * 2f + Single.Epsilon));

	public PlaneObjectRelationship RelationshipTo<TGeo>(TGeo element) where TGeo : IRelatable<Plane, PlaneObjectRelationship> => element.RelationshipTo(this);
	public bool IsIntersectedBy<TGeo>(TGeo element) where TGeo : IIntersectable<Plane> => element.IsIntersectedBy(this);
	public Location? IntersectionWith<TGeo>(TGeo element) where TGeo : IIntersectionDeterminable<Plane, Location> => element.IntersectionWith(this);


	public static Plane Interpolate(Plane start, Plane end, float distance) {
		return new(
			Direction.Interpolate(start.Normal, end.Normal, distance),
			Location.Interpolate(start.ClosestPointToOrigin, end.ClosestPointToOrigin, distance)
		);
	}

	public Plane Clamp(Plane min, Plane max) {
		return new(
			Normal.Clamp(min.Normal, max.Normal),
			ClosestPointToOrigin.Clamp(min.ClosestPointToOrigin, max.ClosestPointToOrigin)
		);
	}
	public static Rotation CreateInterpolationPrecomputation(Plane start, Plane end) => Direction.CreateInterpolationPrecomputation(start.Normal, end.Normal);
	public static Plane InterpolateUsingPrecomputation(Plane start, Plane end, Rotation precomputation, float distance) {
		return new(
			Direction.InterpolateUsingPrecomputation(start.Normal, end.Normal, precomputation, distance),
			Location.Interpolate(start.ClosestPointToOrigin, end.ClosestPointToOrigin, distance)
		);
	}
	public static Plane CreateNewRandom() => new(Direction.CreateNewRandom(), Location.CreateNewRandom());
	public static Plane CreateNewRandom(Plane minInclusive, Plane maxExclusive) => new(Direction.CreateNewRandom(minInclusive.Normal, maxExclusive.Normal), Location.CreateNewRandom(minInclusive.ClosestPointToOrigin, maxExclusive.ClosestPointToOrigin));
}