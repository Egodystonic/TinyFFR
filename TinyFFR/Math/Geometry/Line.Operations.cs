// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

public readonly partial struct Line {
	public Ray ToRay(float signedDistanceAlongLine, bool flipDirection) => new(LocationAtDistance(signedDistanceAlongLine), flipDirection ? Direction.Inverted : Direction);
	public BoundedRay ToBoundedRay(float startSignedDistanceAlongLine, float endSignedDistanceAlongLine) {
		return new(LocationAtDistance(startSignedDistanceAlongLine), LocationAtDistance(endSignedDistanceAlongLine));
	}

	Line IInvertible<Line>.Inverted => new(PointOnLine, -Direction);
	static Line IUnaryNegationOperators<Line, Line>.operator -(Line line) => new Line(line.PointOnLine, -line.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, Rotation rot) => line.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Rotation rot, Line line) => line.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line RotatedBy(Rotation rotation) => new(PointOnLine, Direction.RotatedBy(rotation));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, (Rotation Rotation, Location Pivot) pivotRotationTuple) => line.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, (Location Pivot, Rotation Rotation) pivotRotationTuple) => line.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *((Rotation Rotation, Location Pivot) pivotRotationTuple, Line line) => line.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *((Location Pivot, Rotation Rotation) pivotRotationTuple, Line line) => line.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public Line RotatedAroundPoint(Rotation rot, Location pivot) {
		var lineToPivotVect = PointClosestTo(pivot) >> pivot;
		return new(pivot + lineToPivotVect * rot, Direction * rot);
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Line line, Vect v) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator -(Line line, Vect v) => line.MovedBy(-v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Vect v, Line line) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line MovedBy(Vect v) => new(PointOnLine + v, Direction);
	
	
	public static Line Interpolate(Line start, Line end, float distance) {
		var startPoint = start.PointClosestTo(end);
		var endPoint = end.PointClosestTo(start);
		return new(
			Location.Interpolate(startPoint, endPoint, distance),
			Direction.Interpolate(start.Direction, end.Direction, distance)
		);
	}
	public static Rotation CreateInterpolationPrecomputation(Line start, Line end) { // TODO add startPoint/endPoint to precomputation object
		return Direction.CreateInterpolationPrecomputation(start.Direction, end.Direction);
	}
	public static Line InterpolateUsingPrecomputation(Line start, Line end, Rotation precomputation, float distance) {
		return new(
			Location.Interpolate(start.PointOnLine, end.PointOnLine, distance),
			Direction.InterpolateUsingPrecomputation(start.Direction, end.Direction, precomputation, distance)
		);
	}
	public Line Clamp(Line min, Line max) => new(PointOnLine.Clamp(min.PointOnLine, max.PointOnLine), Direction.Clamp(min.Direction, max.Direction));

	public static Line CreateNewRandom() => new(Location.CreateNewRandom(), Direction.CreateNewRandom());
	public static Line CreateNewRandom(Line minInclusive, Line maxExclusive) => new(Location.CreateNewRandom(minInclusive.PointOnLine, maxExclusive.PointOnLine), Direction.CreateNewRandom(minInclusive.Direction, maxExclusive.Direction));
	

	public Location PointClosestTo(Location location) {
		var distance = Vector3.Dot((location - PointOnLine).ToVector3(), Direction.ToVector3());
		return PointOnLine + Direction * distance;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToOrigin() {
		var distance = -Vector3.Dot(PointOnLine.ToVector3(), Direction.ToVector3());
		return PointOnLine + Direction * distance;
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
		return LocationAtDistance(intersectionDistance ?? 0f);
	}
	public Location PointClosestTo(Ray ray) {
		var intersectionDistances = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(this, ray);
		if (intersectionDistances == null || !ray.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return PointClosestTo(ray.StartPoint);
		else return LocationAtDistance(intersectionDistances.Value.ThisDistance);
	}
	public Location PointClosestTo(BoundedRay boundedRay) {
		var intersectionDistances = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(this, boundedRay);
		if (intersectionDistances == null || intersectionDistances.Value.OtherDistance < 0f) return PointClosestTo(boundedRay.StartPoint);
		else if (!boundedRay.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return PointClosestTo(boundedRay.EndPoint);
		else return LocationAtDistance(intersectionDistances.Value.ThisDistance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location LocationAtDistance(float signedDistanceFromStart) => PointOnLine + Direction * signedDistanceFromStart;

	// These are implemented explicitly because they're all basically the same thing or useless for an unbounded line.
	bool ILineLike.DistanceIsWithinLineBounds(float signedDistanceFromStart) => true;
	float ILineLike.BindDistance(float signedDistanceFromStart) => signedDistanceFromStart;
	Location ILineLike.BoundedLocationAtDistance(float signedDistanceFromStart) => LocationAtDistance(signedDistanceFromStart);
	Location ILineLike.UnboundedLocationAtDistance(float signedDistanceFromStart) => LocationAtDistance(signedDistanceFromStart);
	Location? ILineLike.LocationAtDistanceOrNull(float signedDistanceFromStart) => LocationAtDistance(signedDistanceFromStart);

	float ILineLike.BoundedDistanceAtPointClosestTo(Location point) => DistanceAtPointClosestTo(point);
	float ILineLike.UnboundedDistanceAtPointClosestTo(Location point) => DistanceAtPointClosestTo(point);
	public float DistanceAtPointClosestTo(Location point) {
		var closestPoint = PointClosestTo(point);
		return closestPoint.DistanceFrom(PointOnLine) * MathF.Sign((PointOnLine >> closestPoint).Dot(Direction));
	}

	public Ray? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane)?.StartPoint;
		if (intersectionPoint == null) return null;
		return new Ray(intersectionPoint.Value, Direction.ReflectedBy(plane));
	}

	public Ray? IntersectionWith(Plane plane) {
		var similarityToNormal = plane.Normal.Dot(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		var distance = (plane.PointClosestToOrigin - PointOnLine).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
		return new(LocationAtDistance(distance), Direction);
	}

	public bool IsIntersectedBy(Plane plane) => plane.Normal.Dot(Direction) != 0f;

	public float SignedDistanceFrom(Plane plane) {
		if (plane.Normal.Dot(Direction) != 0f) return 0f;
		var originToPlaneVect = (Vect) plane.PointClosestToOrigin;
		return ((Vect) PointOnLine).LengthWhenProjectedOnTo(originToPlaneVect.Direction) - originToPlaneVect.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => MathF.Abs(SignedDistanceFrom(plane));

	// TODO in xmldoc explain that it's probably better to use IntersectionWith and/or DistanceFrom than these two methods
	public Location PointClosestTo(Plane plane) {
		// If we're parallel with the plane there are infinite answers so we just return the easiest one
		return IntersectionWith(plane)?.StartPoint ?? PointOnLine;
	}
	public Location ClosestPointOn(Plane plane) {
		// If we're parallel with the plane there are infinite answers so we just return the easiest one
		return IntersectionWith(plane)?.StartPoint ?? plane.PointClosestToOrigin;
	}

	public PlaneObjectRelationship RelationshipTo(Plane plane) {
		return SignedDistanceFrom(plane) switch {
			> 0f => PlaneObjectRelationship.PlaneFacesTowardsObject,
			< 0f => PlaneObjectRelationship.PlaneFacesAwayFromObject,
			_ => PlaneObjectRelationship.PlaneIntersectsObject
		};
	}

	public Line? ProjectedOnTo(Plane plane) {
		var projectedDirection = Direction.ProjectedOnTo(plane);
		return projectedDirection == null ? null : new Line(PointOnLine.ClosestPointOn(plane), projectedDirection.Value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastProjectedOnTo(Plane plane) => new(PointOnLine.ClosestPointOn(plane), Direction.FastProjectedOnTo(plane));

	public Line? ParallelizedWith(Plane plane) => ParallelizedWith(plane, 0f);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(Plane plane) => FastParallelizedWith(plane, 0f);
	public Line? ParallelizedWith(Plane plane, float pivotPointSignedDistance) {
		var projectedDirection = Direction.ProjectedOnTo(plane);
		if (projectedDirection == null) return null;
		return new Line(LocationAtDistance(pivotPointSignedDistance), projectedDirection.Value);
	}
	public Line FastParallelizedWith(Plane plane, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastProjectedOnTo(plane));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizedAgainst(Plane plane) => OrthogonalizedAgainst(plane, 0f);
	public Line? OrthogonalizedAgainst(Plane plane, float pivotPointSignedDistance) {
		var newDir = Direction.OrthogonalizedAgainst(plane);
		if (newDir == null) return null;
		return new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizedAgainst(Plane plane) => FastOrthogonalizedAgainst(plane, 0f);
	public Line FastOrthogonalizedAgainst(Plane plane, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastOrthogonalizedAgainst(plane));

	public bool TrySplit(Plane plane, out Ray outWithLineDir, out Ray outOpposingLineDir) {
		var intersection = IntersectionWith(plane);
		if (intersection == null) {
			outWithLineDir = default;
			outOpposingLineDir = default;
			return false;
		}

		outWithLineDir = intersection.Value;
		outOpposingLineDir = intersection.Value.Flipped;
		return true;
	}
}