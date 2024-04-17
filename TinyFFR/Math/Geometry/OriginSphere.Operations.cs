// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct OriginSphere
	: IMultiplyOperators<OriginSphere, float, OriginSphere> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static OriginSphere operator *(OriginSphere sphere, float scalar) => sphere.ScaledBy(scalar);
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
	public float DistanceFrom<TLine>(TLine line) where TLine : ILine => MathF.Max(0f, line.DistanceFromOrigin() - Radius);
	public float SurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine => SurfaceDistanceFrom(ClosestPointToSurfaceOn(line));

	public bool Contains(Location location) => ((Vect) location).LengthSquared <= RadiusSquared;
	
	public Location ClosestPointTo(Location location) {
		var vectFromLocToCentre = (Vect) location;
		if (vectFromLocToCentre.LengthSquared <= RadiusSquared) return location;
		else return location - vectFromLocToCentre.ShortenedBy(Radius);
	}
	public Location ClosestPointOnSurfaceTo(Location location) { // TODO xmldoc that if location == Origin this will return Origin (or we should add an overload?)
		var vectFromLocToCentre = (Vect) location;
		return (Location) vectFromLocToCentre.WithLength(Radius);
	}

	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine => (Location) ((Vect) line.ClosestPointToOrigin()).WithMaxLength(Radius);
	public Location ClosestPointOn<TLine>(TLine line) where TLine : ILine => line.ClosestPointToOrigin();
	public Location ClosestPointOnSurfaceTo<TLine>(TLine line) where TLine : ILine {
		var potentialIntersectionDistances = GetUnboundedSurfaceIntersectionDistances(line);
		if (potentialIntersectionDistances == null) {
			// Line would never intersect even if infinite, so the answer is easy: It's the vector with length Radius that points to the closest point on the line to the sphere centre
			return (Location) ((Vect) line.ClosestPointToOrigin()).WithLength(Radius);
		}

		// Find the distance from the potential intersection points to the line. Pick the one that is closest to the line
		var intersectionPointOne = line.UnboundedLocationAtDistance(potentialIntersectionDistances.Value.First);
		var intersectionPointTwo = line.UnboundedLocationAtDistance(potentialIntersectionDistances.Value.Second);
		if (line.DistanceFrom(intersectionPointTwo) < line.DistanceFrom(intersectionPointOne)) return intersectionPointTwo;
		else return intersectionPointOne;
	}
	public Location ClosestPointToSurfaceOn<TLine>(TLine line) where TLine : ILine {
		var potentialIntersectionDistances = GetUnboundedSurfaceIntersectionDistances(line);
		if (potentialIntersectionDistances == null) {
			// Line would never intersect even if infinite, so the answer is easy: It's the point on the line that's closest to the sphere centre
			return line.ClosestPointToOrigin();
		}

		// Find the distance from the potential intersection points to the line. Pick the one that is closest to the line, then find the closest point on the line to that point
		var intersectionPointOne = line.UnboundedLocationAtDistance(potentialIntersectionDistances.Value.First);
		var intersectionPointTwo = line.UnboundedLocationAtDistance(potentialIntersectionDistances.Value.Second);
		if (line.DistanceFrom(intersectionPointTwo) < line.DistanceFrom(intersectionPointOne)) return line.ClosestPointTo(intersectionPointTwo);
		else return line.ClosestPointTo(intersectionPointOne);
	}

	public bool IsIntersectedBy<TLine>(TLine line) where TLine : ILine {
		var distanceTuple = GetUnboundedSurfaceIntersectionDistances(line);
		if (distanceTuple == null) return false;
		return line.DistanceIsWithinLineBounds(distanceTuple.Value.First) || line.DistanceIsWithinLineBounds(distanceTuple.Value.Second);
	}
	public ConvexShapeLineIntersection? IntersectionWith<TLine>(TLine line) where TLine : ILine {
		var distanceTuple = GetUnboundedSurfaceIntersectionDistances(line);
		if (distanceTuple == null) return null;

		return ConvexShapeLineIntersection.FromTwoPotentiallyNullArgs(
			line.LocationAtDistanceOrNull(distanceTuple.Value.First), 
			distanceTuple.Value.SecondIsIdenticalToFirst ? null : line.LocationAtDistanceOrNull(distanceTuple.Value.Second)
		);
	}

	(float First, float Second, bool SecondIsIdenticalToFirst)? GetUnboundedSurfaceIntersectionDistances<TLine>(TLine line) where TLine : ILine {
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

	public Location ClosestPointTo(Plane plane) => (Location) ((Vect) plane.ClosestPointToOrigin).WithMaxLength(Radius);
	public Location ClosestPointOn(Plane plane) => plane.ClosestPointToOrigin;

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

	// TODO this will become a circle when we implement IIntersectable properly
	public bool TrySplit(Plane plane, out Location circleCentrePoint, out float circleRadius) {
		circleCentrePoint = plane.ClosestPointToOrigin;
		var vectToPlane = (Vect) circleCentrePoint;
		var vectLengthSquared = vectToPlane.LengthSquared;
		if (vectLengthSquared > RadiusSquared) {
			circleRadius = default;
			return false;
		}
		circleRadius = GetCircleRadiusAtDistanceFromCenterSquared(vectLengthSquared);
		return true;
	}

	public Location ClosestPointOnSurfaceTo(Plane plane) {
		// If the plane doesn't intersect this sphere, we can just return the simple closest point
		if (!TrySplit(plane, out var circleCentrePoint, out var circleRadius)) return ClosestPointTo(plane);

		// Otherwise there are infinite valid answers around the circle formed by the intersection. Any will do
		var centrePointDirection = circleCentrePoint == Location.Origin ? plane.Normal : ((Vect) circleCentrePoint).Direction;
		return circleCentrePoint + centrePointDirection.AnyPerpendicular() * circleRadius;
	}
	public Location ClosestPointToSurfaceOn(Plane plane) {
		// If the plane doesn't intersect this sphere, we can just return the plane's closest point to the sphere
		if (!TrySplit(plane, out var circleCentrePoint, out var circleRadius)) return plane.ClosestPointToOrigin;

		// Otherwise there are infinite valid answers around the circle formed by the intersection. Any will do
		var centrePointDirection = circleCentrePoint == Location.Origin ? plane.Normal : ((Vect) circleCentrePoint).Direction;
		return circleCentrePoint + centrePointDirection.AnyPerpendicular() * circleRadius;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SurfaceDistanceFrom(Plane plane) => DistanceFrom(plane);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedSurfaceDistanceFrom(Plane plane) => SignedDistanceFrom(plane);
}