// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;
using System.Net;

namespace Egodystonic.TinyFFR;

public readonly partial struct Ray {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay ToBoundedRay(float signedDistanceToEndPoint) => BoundedRay.FromStartPointAndVect(StartPoint, Direction * signedDistanceToEndPoint);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ToLine() => new(StartPoint, Direction);

	public Ray Flipped {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(StartPoint, -Direction);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator -(Ray operand) => operand.Flipped;
	Ray IInvertible<Ray>.Inverted => Flipped;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *(Ray ray, Rotation rot) => ray.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *(Rotation rot, Ray ray) => ray.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray RotatedBy(Rotation rotation) => new(StartPoint, Direction.RotatedBy(rotation));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *(Ray ray, (Rotation Rotation, Location Pivot) pivotRotationTuple) => ray.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *(Ray ray, (Location Pivot, Rotation Rotation) pivotRotationTuple) => ray.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *((Rotation Rotation, Location Pivot) pivotRotationTuple, Ray ray) => ray.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *((Location Pivot, Rotation Rotation) pivotRotationTuple, Ray ray) => ray.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public Ray RotatedAroundPoint(Rotation rot, Location pivot) {
		var boundedRay = new BoundedRay(StartPoint, UnboundedLocationAtDistance(UnboundedDistanceAtPointClosestTo(pivot)));
		var rotatedRay = boundedRay.RotatedAroundPoint(rot, pivot);
		return new Ray(rotatedRay.StartPoint, Direction * rot);
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator +(Ray ray, Vect v) => ray.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator -(Ray ray, Vect v) => ray.MovedBy(-v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator +(Vect v, Ray ray) => ray.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray MovedBy(Vect v) => new(StartPoint + v, Direction);


	public static Ray Interpolate(Ray start, Ray end, float distance) {
		return new(
			Location.Interpolate(start.StartPoint, end.StartPoint, distance),
			Direction.Interpolate(start.Direction, end.Direction, distance)
		);
	}
	public Ray Clamp(Ray min, Ray max) => new(StartPoint.Clamp(min.StartPoint, max.StartPoint), Direction.Clamp(min.Direction, max.Direction));
	public static Rotation CreateInterpolationPrecomputation(Ray start, Ray end) {
		return Direction.CreateInterpolationPrecomputation(start.Direction, end.Direction);
	}
	public static Ray InterpolateUsingPrecomputation(Ray start, Ray end, Rotation precomputation, float distance) {
		return new(
			Location.Interpolate(start.StartPoint, end.StartPoint, distance),
			Direction.InterpolateUsingPrecomputation(start.Direction, end.Direction, precomputation, distance)
		);
	}
	public static Ray CreateNewRandom() => new(Location.CreateNewRandom(), Direction.CreateNewRandom());
	public static Ray CreateNewRandom(Ray minInclusive, Ray maxExclusive) => new(Location.CreateNewRandom(minInclusive.StartPoint, maxExclusive.StartPoint), Direction.CreateNewRandom(minInclusive.Direction, maxExclusive.Direction));


	public Location PointClosestTo(Location location) {
		var distance = Vector3.Dot((location - StartPoint).ToVector3(), Direction.ToVector3());
		return distance switch {
			< 0f => StartPoint,
			_ => StartPoint + Direction * distance
		};
	}
	public Location PointClosestToOrigin() {
		var distance = -Vector3.Dot(StartPoint.ToVector3(), Direction.ToVector3());
		return distance switch {
			< 0f => StartPoint,
			_ => StartPoint + Direction * distance
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location location) => location.DistanceFrom(PointClosestTo(location));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Location location) => location.DistanceSquaredFrom(PointClosestTo(location));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromOrigin() => ((Vect) PointClosestToOrigin()).Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromOrigin() => ((Vect) PointClosestToOrigin()).LengthSquared;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;

	public Location PointClosestTo(Line line) {
		var intersectionDistance = ILineLike.CalculateUnboundedIntersectionDistanceOnThisLine(this, line);
		return intersectionDistance != null ? BoundedLocationAtDistance(intersectionDistance.Value) : StartPoint;
	}
	public Location PointClosestTo(Ray ray) {
		var intersectionDistances = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(this, ray);
		if (intersectionDistances == null || !ray.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return PointClosestTo(ray.StartPoint);
		else return BoundedLocationAtDistance(intersectionDistances.Value.ThisDistance);
	}
	public Location PointClosestTo(BoundedRay boundedRay) {
		var intersectionDistances = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(this, boundedRay);
		if (intersectionDistances == null || intersectionDistances.Value.OtherDistance < 0f) return PointClosestTo(boundedRay.StartPoint);
		else if (!boundedRay.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return PointClosestTo(boundedRay.EndPoint);
		else return BoundedLocationAtDistance(intersectionDistances.Value.ThisDistance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool DistanceIsWithinLineBounds(float signedDistanceFromStart) => signedDistanceFromStart >= 0f;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float BindDistance(float signedDistanceFromStart) => MathF.Max(0f, signedDistanceFromStart);
	public Location BoundedLocationAtDistance(float signedDistanceFromStart) => UnboundedLocationAtDistance(BindDistance(signedDistanceFromStart));
	public Location UnboundedLocationAtDistance(float signedDistanceFromStart) => StartPoint + Direction * signedDistanceFromStart;
	public Location? LocationAtDistanceOrNull(float signedDistanceFromStart) => DistanceIsWithinLineBounds(signedDistanceFromStart) ? UnboundedLocationAtDistance(signedDistanceFromStart) : null;
	public float UnboundedDistanceAtPointClosestTo(Location point) => ToLine().DistanceAtPointClosestTo(point);
	public float BoundedDistanceAtPointClosestTo(Location point) => PointClosestTo(point).DistanceFrom(StartPoint);

	public Ray? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane)?.StartPoint;
		if (intersectionPoint == null) return null;
		return new Ray(intersectionPoint.Value, Direction.FastReflectedBy(plane));
	}
	public Ray FastReflectedBy(Plane plane) => new(FastIntersectionWith(plane).StartPoint, Direction.FastReflectedBy(plane));

	public Angle? IncidentAngleWith(Plane plane) => IsIntersectedBy(plane) ? plane.IncidentAngleWith(Direction) : null;
	public Angle FastIncidentAngleWith(Plane plane) => plane.FastIncidentAngleWith(Direction);

	float? GetUnboundedPlaneIntersectionDistance(Plane plane) {
		var similarityToNormal = plane.Normal.Dot(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		return (plane.PointClosestToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
	}

	public Ray? IntersectionWith(Plane plane) {
		var distance = GetUnboundedPlaneIntersectionDistance(plane);
		if (distance >= 0f) return new(UnboundedLocationAtDistance(distance.Value), Direction);
		else return null; // Plane behind ray or parallel with ray
	}
	public Ray FastIntersectionWith(Plane plane) {
		var distance = (plane.PointClosestToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / plane.Normal.Dot(Direction);
		return new(UnboundedLocationAtDistance(distance), Direction);
	}
	public bool IsIntersectedBy(Plane plane) => GetUnboundedPlaneIntersectionDistance(plane) >= 0f;

	public float SignedDistanceFrom(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		if (unboundedDistance >= 0f) return 0f;
		else return plane.SignedDistanceFrom(StartPoint);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => MathF.Abs(SignedDistanceFrom(plane));

	public Location PointClosestTo(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		return BoundedLocationAtDistance(unboundedDistance ?? 0f); // If unboundedDistance is null we're parallel so the StartPoint is as close as any other point
	}
	public Location ClosestPointOn(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		var closestPointOnLine = BoundedLocationAtDistance(unboundedDistance ?? 0f);
		if (unboundedDistance >= 0f) return closestPointOnLine; // Actual intersection
		else return plane.PointClosestTo(closestPointOnLine);
	}

	public PlaneObjectRelationship RelationshipTo(Plane plane) {
		return SignedDistanceFrom(plane) switch {
			> 0f => PlaneObjectRelationship.PlaneFacesTowardsObject,
			< 0f => PlaneObjectRelationship.PlaneFacesAwayFromObject,
			_ => PlaneObjectRelationship.PlaneIntersectsObject
		};
	}

	public Ray? ProjectedOnTo(Plane plane) {
		var projectedDirection = Direction.ProjectedOnTo(plane);
		if (projectedDirection == null) return null;
		return new Ray(StartPoint.ClosestPointOn(plane), projectedDirection.Value);
	}
	public Ray FastProjectedOnTo(Plane plane) => new(StartPoint.ClosestPointOn(plane), Direction.FastProjectedOnTo(plane));

	public Ray? ParallelizedWith(Plane plane) {
		var projectedDirection = Direction.ProjectedOnTo(plane);
		if (projectedDirection == null) return null;
		return new Ray(StartPoint, projectedDirection.Value);
	}
	public Ray FastParallelizedWith(Plane plane) => new(StartPoint, Direction.FastProjectedOnTo(plane));

	public Ray? OrthogonalizedAgainst(Plane plane) {
		var newDirection = Direction.OrthogonalizedAgainst(plane);
		if (newDirection == null) return null;
		return new(StartPoint, newDirection.Value);
	}
	public Ray FastOrthogonalizedAgainst(Plane plane) => new(StartPoint, Direction.FastOrthogonalizedAgainst(plane));

	public bool TrySplit(Plane plane, out BoundedRay outStartPointToPlane, out Ray outPlaneToInfinity) {
		var intersection = IntersectionWith(plane);
		if (intersection == null) {
			outStartPointToPlane = default;
			outPlaneToInfinity = default;
			return false;
		}

		outStartPointToPlane = new BoundedRay(StartPoint, intersection.Value.StartPoint);
		outPlaneToInfinity = intersection.Value;
		return true;
	}
}