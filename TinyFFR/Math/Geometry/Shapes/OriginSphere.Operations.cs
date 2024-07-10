// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

partial struct OriginSphere {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static OriginSphere operator *(OriginSphere sphere, float scalar) => sphere.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static OriginSphere operator /(OriginSphere sphere, float scalar) => new(sphere.Radius / scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static OriginSphere operator *(float scalar, OriginSphere sphere) => sphere.ScaledBy(scalar);
	public OriginSphere ScaledBy(float scalar) => new(Radius * scalar);

	public float GetCircleRadiusAtDistanceFromCenter(float distanceFromCenter) => GetCircleRadiusAtDistanceFromCenterSquared(distanceFromCenter * distanceFromCenter);
	float GetCircleRadiusAtDistanceFromCenterSquared(float distanceFromCenterSquared) {
		var resultSquared = RadiusSquared - distanceFromCenterSquared;
		return MathF.Sqrt(MathF.Max(0f, resultSquared));
	}

	public float DistanceFrom(Location location) => MathF.Max(0f, ((Vect) location).Length - Radius);
	public float SurfaceDistanceFrom(Location location) => MathF.Abs(((Vect) location).Length - Radius);
	float IDistanceMeasurable<Location>.DistanceSquaredFrom(Location location) { var sqrt = DistanceFrom(location); return sqrt * sqrt; }
	float IConvexShape.SurfaceDistanceSquaredFrom(Location location) { var sqrt = SurfaceDistanceFrom(location); return sqrt * sqrt; }

	public float DistanceFrom(Line line) => MathF.Max(0f, line.DistanceFromOrigin() - Radius);
	public float DistanceFrom(Ray ray) => MathF.Max(0f, ray.DistanceFromOrigin() - Radius);
	public float DistanceFrom(BoundedRay ray) => MathF.Max(0f, ray.DistanceFromOrigin() - Radius);
	float IDistanceMeasurable<Line>.DistanceSquaredFrom(Line line) { var sqrt = DistanceFrom(line); return sqrt * sqrt; }
	float IDistanceMeasurable<Ray>.DistanceSquaredFrom(Ray ray) { var sqrt = DistanceFrom(ray); return sqrt * sqrt; }
	float IDistanceMeasurable<BoundedRay>.DistanceSquaredFrom(BoundedRay ray) { var sqrt = DistanceFrom(ray); return sqrt * sqrt; }
	public float SurfaceDistanceFrom(Line line) => SurfaceDistanceFrom(line.PointClosestToSurfaceOf(this));
	public float SurfaceDistanceFrom(Ray ray) => SurfaceDistanceFrom(ray.PointClosestToSurfaceOf(this));
	public float SurfaceDistanceFrom(BoundedRay ray) => SurfaceDistanceFrom(ray.PointClosestToSurfaceOf(this));
	float IConvexShape.SurfaceDistanceSquaredFrom(Line line) { var sqrt = SurfaceDistanceFrom(line); return sqrt * sqrt; }
	float IConvexShape.SurfaceDistanceSquaredFrom(Ray ray) { var sqrt = SurfaceDistanceFrom(ray); return sqrt * sqrt; }
	float IConvexShape.SurfaceDistanceSquaredFrom(BoundedRay ray) { var sqrt = SurfaceDistanceFrom(ray); return sqrt * sqrt; }

	public bool Contains(Location location) => ((Vect) location).LengthSquared <= RadiusSquared;
	
	public Location PointClosestTo(Location location) {
		var vectFromLocToCentre = (Vect) location;
		if (vectFromLocToCentre.LengthSquared <= RadiusSquared) return location;
		else return location - vectFromLocToCentre.ShortenedBy(Radius);
	}
	public Location SurfacePointClosestTo(Location location) {
		var vectFromLocToCentre = (Vect) location;
		if (vectFromLocToCentre == Vect.Zero) return new(0f, Radius, 0f);
		return (Location) vectFromLocToCentre.WithLength(Radius);
	}
	public Location FastSurfacePointClosestTo(Location location) { // TODO xmldoc that if location == Origin this will return Origin
		var vectFromLocToCentre = (Vect) location;
		return (Location) vectFromLocToCentre.WithLength(Radius);
	}

	public Location ClosestPointOn(Line line) => line.PointClosestToOrigin();
	public Location ClosestPointOn(Ray ray) => ray.PointClosestToOrigin();
	public Location ClosestPointOn(BoundedRay ray) => ray.PointClosestToOrigin();
	public Location PointClosestTo(Line line) => (Location) ((Vect) line.PointClosestToOrigin()).WithMaxLength(Radius);
	public Location PointClosestTo(Ray ray) => (Location) ((Vect) ray.PointClosestToOrigin()).WithMaxLength(Radius);
	public Location PointClosestTo(BoundedRay ray) => (Location) ((Vect) ray.PointClosestToOrigin()).WithMaxLength(Radius);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location SurfacePointClosestTo(Line line) => SurfacePointClosestToLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location SurfacePointClosestTo(Ray ray) => SurfacePointClosestToLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location SurfacePointClosestTo(BoundedRay ray) => SurfacePointClosestToLineLike(ray);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointToSurfaceOn(Line line) => ClosestPointToSurfaceOnLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointToSurfaceOn(Ray ray) => ClosestPointToSurfaceOnLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointToSurfaceOn(BoundedRay ray) => ClosestPointToSurfaceOnLineLike(ray);


	Location SurfacePointClosestToLineLike<TLine>(TLine line) where TLine : ILineLike {
		var potentialIntersectionDistances = GetUnboundedLineLikeSurfaceIntersectionDistances(line);
		if (potentialIntersectionDistances == null) {
			// Line would never intersect even if infinite, so the answer is easy: It's the vector with length Radius that points to the closest point on the line to the sphere centre
			return (Location) ((Vect) line.PointClosestToOrigin()).WithLength(Radius);
		}

		// Find the distance from the potential intersection points to the line. Pick the one that is closest to the line
		var intersectionPointOne = line.UnboundedLocationAtDistance(potentialIntersectionDistances.Value.First);
		var intersectionPointTwo = line.UnboundedLocationAtDistance(potentialIntersectionDistances.Value.Second);
		if (line.DistanceFrom(intersectionPointTwo) < line.DistanceFrom(intersectionPointOne)) return intersectionPointTwo;
		else return intersectionPointOne;
	}
	Location ClosestPointToSurfaceOnLineLike<TLine>(TLine line) where TLine : ILineLike {
		var potentialIntersectionDistances = GetUnboundedLineLikeSurfaceIntersectionDistances(line);
		if (potentialIntersectionDistances == null) {
			// Line would never intersect even if infinite, so the answer is easy: It's the point on the line that's closest to the sphere centre
			return line.PointClosestToOrigin();
		}

		// Find the distance from the potential intersection points to the line. Pick the one that is closest to the line, then find the closest point on the line to that point
		var intersectionPointOne = line.UnboundedLocationAtDistance(potentialIntersectionDistances.Value.First);
		var intersectionPointTwo = line.UnboundedLocationAtDistance(potentialIntersectionDistances.Value.Second);
		if (line.DistanceFrom(intersectionPointTwo) < line.DistanceFrom(intersectionPointOne)) return line.PointClosestTo(intersectionPointTwo);
		else return line.PointClosestTo(intersectionPointOne);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedByLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedByLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedByLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith(Line line) => IntersectionWithLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith(Ray ray) => IntersectionWithLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith(BoundedRay ray) => IntersectionWithLineLike(ray);

	bool IsIntersectedByLineLike<TLine>(TLine line) where TLine : ILineLike {
		var distanceTuple = GetUnboundedLineLikeSurfaceIntersectionDistances(line);
		if (distanceTuple == null) return false;
		return line.DistanceIsWithinLineBounds(distanceTuple.Value.First) || line.DistanceIsWithinLineBounds(distanceTuple.Value.Second);
	}
	ConvexShapeLineIntersection? IntersectionWithLineLike<TLine>(TLine line) where TLine : ILineLike {
		var distanceTuple = GetUnboundedLineLikeSurfaceIntersectionDistances(line);
		if (distanceTuple == null) return null;

		return ConvexShapeLineIntersection.FromTwoPotentiallyNullArgs(
			line.LocationAtDistanceOrNull(distanceTuple.Value.First), 
			distanceTuple.Value.SecondIsIdenticalToFirst ? null : line.LocationAtDistanceOrNull(distanceTuple.Value.Second)
		);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleTo(Line line) => IncidentAngleToLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleTo(Ray ray) => IncidentAngleToLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleTo(BoundedRay ray) => IncidentAngleToLineLike(ray);

	public Ray? ReflectionOf(Line line) { var reflection = ReflectionOfLineLike(line); return reflection != null ? new Ray(reflection.Value.Item1, reflection.Value.Item2) : null; }
	public Ray? ReflectionOf(Ray ray) { var reflection = ReflectionOfLineLike(ray); return reflection != null ? new Ray(reflection.Value.Item1, reflection.Value.Item2) : null; }
	public BoundedRay? ReflectionOf(BoundedRay ray) {
		var reflection = ReflectionOfLineLike(ray); 
		return reflection != null
			? new Ray(reflection.Value.Item1, reflection.Value.Item2).ToBoundedRay(ray.Length - ray.UnboundedDistanceAtPointClosestTo(reflection.Value.Item1)) 
			: null;
	}

	public bool Contains(BoundedRay ray) => Contains(ray.StartPoint) && Contains(ray.EndPoint);

	Angle? IncidentAngleToLineLike<TLine>(TLine line) where TLine : ILineLike {
		return GetLineLikeIntersectionPointClosestToStartPoint(line)?.AsVect().Direction.Inverted.AngleTo(line.Direction);
	}

	(Location, Direction)? ReflectionOfLineLike<TLine>(TLine line) where TLine : ILineLike {
		var reflectionPoint = GetLineLikeIntersectionPointClosestToStartPoint(line);
		if (reflectionPoint == null) return null;

		return (reflectionPoint.Value, new Plane(reflectionPoint.Value.AsVect().Direction, Radius).ReflectionOf(line.Direction));
	}

	(float First, float Second, bool SecondIsIdenticalToFirst)? GetUnboundedLineLikeSurfaceIntersectionDistances<TLine>(TLine line) where TLine : ILineLike {
		// We solve this always as a simple unbounded line as it lets us solve as a quadratic, e.g. distance-from-start = (-b +/- sqrt(b^2 - 4ac)) / 2a
		//																								where a = direction dot direction	(always 1 for unit-length vectors)
		//																								where b = 2(start dot direction)
		//																								where c = (start dot start) - radius^2	(v dot v is equal to v.LengthSquared)
		// This can have zero, one, or two real solutions (just like any quadratic) where the discriminant (the part inside the sqrt) can be:
		//	- negative -> sqrt(negative number) has no real solutions, this indicates there is no intersection
		//	- zero -> sqrt(zero) is zero, meaning "both" solutions are actually identical, meaning the line is exactly tangent to the sphere surface (there is one intersection)
		//	- positive -> sqrt(positive number) has two solutions, this indicates the line enters and exits

		var direction = line.Direction.ToVector3();
		var start = line.StartPoint.ToVector3();
		var b = 2f * Vector3.Dot(start, direction);
		var c = start.LengthSquared() - RadiusSquared;

		var discriminant = b * b - 4 * c;
		if (discriminant < 0f) return null;

		var sqrtDiscriminant = MathF.Sqrt(discriminant);
		var negB = -b;

		return ((negB + sqrtDiscriminant) * 0.5f, (negB - sqrtDiscriminant) * 0.5f, discriminant == 0f);
	}

	Location? GetLineLikeIntersectionPointClosestToStartPoint<TLine>(TLine line) where TLine : ILineLike {
		var intersectionPoints = GetUnboundedLineLikeSurfaceIntersectionDistances(line);
		if (intersectionPoints == null) return null;
		var firstLocation = line.LocationAtDistanceOrNull(intersectionPoints.Value.First);
		var secondLocation = line.LocationAtDistanceOrNull(intersectionPoints.Value.Second);

		if (firstLocation == null) return secondLocation;
		if (secondLocation == null || MathF.Abs(intersectionPoints.Value.First) < MathF.Abs(intersectionPoints.Value.Second)) return firstLocation;
		return secondLocation;
	}

	public Location PointClosestTo(Plane plane) => (Location) ((Vect) plane.PointClosestToOrigin).WithMaxLength(Radius);
	public Location ClosestPointOn(Plane plane) => plane.PointClosestToOrigin;

	public float SignedDistanceFrom(Plane plane) {
		var distanceFromSphereCentre = plane.SignedDistanceFromOrigin();
		var distanceFromSphereSurface = distanceFromSphereCentre - MathF.Sign(distanceFromSphereCentre) * Radius;
		return MathF.Sign(distanceFromSphereCentre) == MathF.Sign(distanceFromSphereSurface) ? distanceFromSphereSurface : 0f;
	}
	public float DistanceFrom(Plane plane) => MathF.Max(0f, plane.DistanceFromOrigin() - Radius);
	public PlaneObjectRelationship RelationshipTo(Plane plane) => SignedDistanceFrom(plane) switch {
		> 0f => PlaneObjectRelationship.PlaneFacesTowardsObject,
		< 0f => PlaneObjectRelationship.PlaneFacesAwayFromObject,
		_ => PlaneObjectRelationship.PlaneIntersectsObject
	};

	// TODO this will become a circle when we implement IIntersectionDeterminable properly
	public bool TrySplit(Plane plane, out Location circleCentrePoint, out float circleRadius) {
		circleCentrePoint = plane.PointClosestToOrigin;
		var vectToPlane = (Vect) circleCentrePoint;
		var vectLengthSquared = vectToPlane.LengthSquared;
		if (vectLengthSquared > RadiusSquared) {
			circleRadius = default;
			return false;
		}
		circleRadius = GetCircleRadiusAtDistanceFromCenterSquared(vectLengthSquared);
		return true;
	}

	public Location SurfacePointClosestTo(Plane plane) {
		// If the plane doesn't intersect this sphere, we can just return the simple closest point
		if (!TrySplit(plane, out var circleCentrePoint, out var circleRadius)) return PointClosestTo(plane);

		// Otherwise there are infinite valid answers around the circle formed by the intersection. Any will do
		var centrePointDirection = circleCentrePoint == Location.Origin ? plane.Normal : ((Vect) circleCentrePoint).Direction;
		return circleCentrePoint + centrePointDirection.AnyPerpendicular() * circleRadius;
	}
	public Location ClosestPointToSurfaceOn(Plane plane) {
		// If the plane doesn't intersect this sphere, we can just return the plane's closest point to the sphere
		if (!TrySplit(plane, out var circleCentrePoint, out var circleRadius)) return plane.PointClosestToOrigin;

		// Otherwise there are infinite valid answers around the circle formed by the intersection. Any will do
		var centrePointDirection = circleCentrePoint == Location.Origin ? plane.Normal : ((Vect) circleCentrePoint).Direction;
		return circleCentrePoint + centrePointDirection.AnyPerpendicular() * circleRadius;
	}

	float IDistanceMeasurable<Plane>.DistanceSquaredFrom(Plane plane) { var sqrt = DistanceFrom(plane); return sqrt * sqrt; }
	float IConvexShape.SurfaceDistanceSquaredFrom(Plane plane) { var sqrt = SurfaceDistanceFrom(plane); return sqrt * sqrt; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SurfaceDistanceFrom(Plane plane) => DistanceFrom(plane);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedSurfaceDistanceFrom(Plane plane) => SignedDistanceFrom(plane);

	public static OriginSphere Interpolate(OriginSphere start, OriginSphere end, float distance) => new(Single.Lerp(start.Radius, end.Radius, distance));

	public static OriginSphere CreateNewRandom() => new(RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax));
	public static OriginSphere CreateNewRandom(OriginSphere minInclusive, OriginSphere maxExclusive) => new(RandomUtils.NextSingle(minInclusive.Radius, maxExclusive.Radius));
}