﻿// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

public readonly partial struct Line : IPhysicalValidityDeterminable {
	public Ray ToRay(float signedDistanceAlongLine, bool flipDirection) => new(LocationAtDistance(signedDistanceAlongLine), flipDirection ? Direction.Flipped : Direction);
	public BoundedRay ToBoundedRay(float startSignedDistanceAlongLine, float endSignedDistanceAlongLine) {
		return new(LocationAtDistance(startSignedDistanceAlongLine), LocationAtDistance(endSignedDistanceAlongLine));
	}

	Line IInvertible<Line>.Inverted => new(PointOnLine, -Direction);
	static Line IUnaryNegationOperators<Line, Line>.operator -(Line line) => new Line(line.PointOnLine, -line.Direction);

	public bool IsPhysicallyValid => _direction != Direction.None;

	#region Line-Like Methods
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
	#endregion

	#region Translation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Line line, Vect v) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator -(Line line, Vect v) => line.MovedBy(-v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Vect v, Line line) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line MovedBy(Vect v) => new(PointOnLine + v, Direction);
	#endregion

	#region Rotation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, Rotation rot) => line.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Rotation rot, Line line) => line.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line RotatedBy(Rotation rotation) => new(PointOnLine, Direction.RotatedBy(rotation));

	public Line RotatedAroundOriginBy(Rotation rot) => new(PointOnLine.AsVect().RotatedBy(rot).AsLocation(), Direction.RotatedBy(rot));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, (Rotation Rotation, Location Pivot) pivotRotationTuple) => line.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, (Location Pivot, Rotation Rotation) pivotRotationTuple) => line.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *((Rotation Rotation, Location Pivot) pivotRotationTuple, Line line) => line.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *((Location Pivot, Rotation Rotation) pivotRotationTuple, Line line) => line.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public Line RotatedBy(Rotation rotation, float signedPivotDistance) => RotatedBy(rotation, LocationAtDistance(signedPivotDistance));
	public Line RotatedBy(Rotation rotation, Location pivot) {
		return new(pivot + (pivot >> PointClosestTo(pivot)) * rotation, Direction * rotation);
	}
	#endregion

	#region Distance / Closest Point / Containment
	public Location PointClosestTo(Location location) {
		var distance = Vector3.Dot((location - PointOnLine).ToVector3(), Direction.ToVector3());
		return PointOnLine + Direction * distance;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToOrigin() {
		var distance = -Vector3.Dot(PointOnLine.ToVector3(), Direction.ToVector3());
		return PointOnLine + Direction * distance;
	}

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
	public bool Contains(Location location) => Contains(location, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location location) => location.DistanceFrom(PointClosestTo(location));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Location location) => location.DistanceSquaredFrom(PointClosestTo(location));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromOrigin() => ((Vect) PointClosestToOrigin()).Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromOrigin() => ((Vect) PointClosestToOrigin()).LengthSquared;

	public bool IsWithinDistanceAndAngleTo(Line other, float distance, Angle angle) {
		return DistanceFrom(other) <= distance && (Direction.IsWithinAngleTo(other.Direction, angle) || Direction.IsWithinAngleTo(-other.Direction, angle));
	}
	#endregion

	#region Plane Intersection / Split / Incident Angle / Reflection / Distance / Closest Point
	public Location? IntersectionWith(Plane plane) {
		var similarityToNormal = plane.Normal.Dot(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		var distance = (plane.PointClosestToOrigin - PointOnLine).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
		return LocationAtDistance(distance);
	}
	public Location FastIntersectionWith(Plane plane) {
		var similarityToNormal = plane.Normal.Dot(Direction);
		var distance = (plane.PointClosestToOrigin - PointOnLine).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
		return LocationAtDistance(distance);
	}
	public bool IsIntersectedBy(Plane plane) => plane.Normal.Dot(Direction) != 0f;

	public Pair<Ray, Ray>? SplitBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new(new Ray(intersectionPoint.Value, Direction), new Ray(intersectionPoint.Value, -Direction));
	}

	public Pair<Ray, Ray> FastSplitBy(Plane plane) {
		var intersectionPoint = FastIntersectionWith(plane);
		return new(new Ray(intersectionPoint, Direction), new Ray(intersectionPoint, -Direction));
	}

	public Angle? IncidentAngleWith(Plane plane) => IsIntersectedBy(plane) ? plane.IncidentAngleWith(Direction) : null;
	public Angle FastIncidentAngleWith(Plane plane) => plane.FastIncidentAngleWith(Direction);

	public Line? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new Line(intersectionPoint.Value, Direction.FastReflectedBy(plane));
	}
	public Line FastReflectedBy(Plane plane) => new(FastIntersectionWith(plane), Direction.FastReflectedBy(plane));

	// TODO in xmldoc explain that it's probably better to use IntersectionWith and/or DistanceFrom than these two methods
	public Location PointClosestTo(Plane plane) {
		// If we're parallel with the plane there are infinite answers so we just return the easiest one
		return IntersectionWith(plane) ?? PointOnLine;
	}
	public Location ClosestPointOn(Plane plane) {
		// If we're parallel with the plane there are infinite answers so we just return the easiest one
		return IntersectionWith(plane) ?? plane.PointClosestToOrigin;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => MathF.Abs(SignedDistanceFrom(plane));

	public float SignedDistanceFrom(Plane plane) {
		if (plane.Normal.Dot(Direction) != 0f) return 0f;
		var originToPlaneVect = (Vect) plane.PointClosestToOrigin;
		return ((Vect) PointOnLine).LengthWhenProjectedOnTo(originToPlaneVect.Direction) - originToPlaneVect.Length;
	}

	public PlaneObjectRelationship RelationshipTo(Plane plane) {
		return SignedDistanceFrom(plane) switch {
			> 0f => PlaneObjectRelationship.PlaneFacesTowardsObject,
			< 0f => PlaneObjectRelationship.PlaneFacesAwayFromObject,
			_ => PlaneObjectRelationship.PlaneIntersectsObject
		};
	}
	#endregion

	#region Parallelization / Orthogonalization / Projection
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizedWith(Direction dir) => ParallelizedWith(dir, 0f);
	public Line? ParallelizedWith(Direction dir, float pivotPointSignedDistance) {
		var newDir = Direction.ParallelizedWith(dir);
		if (newDir == null) return null;
		return new Line(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(Direction dir) => FastParallelizedWith(dir, 0f);
	public Line FastParallelizedWith(Direction dir, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastParallelizedWith(dir));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizedAgainst(Direction dir) => OrthogonalizedAgainst(dir, 0f);
	public Line? OrthogonalizedAgainst(Direction dir, float pivotPointSignedDistance) {
		var newDir = Direction.OrthogonalizedAgainst(dir);
		if (newDir == null) return null;
		return new Line(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizedAgainst(Direction dir) => FastOrthogonalizedAgainst(dir, 0f);
	public Line FastOrthogonalizedAgainst(Direction dir, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastOrthogonalizedAgainst(dir));


	public Line? ParallelizedWith(Line line, float pivotPointSignedDistance) {
		var newDir = Direction.ParallelizedWith(line.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	public Line FastParallelizedWith(Line line, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastParallelizedWith(line.Direction));
	public Line? ParallelizedWith(Ray ray, float pivotPointSignedDistance) {
		var newDir = Direction.ParallelizedWith(ray.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	public Line FastParallelizedWith(Ray ray, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastParallelizedWith(ray.Direction));
	public Line? ParallelizedWith(BoundedRay ray, float pivotPointSignedDistance) {
		var newDir = Direction.ParallelizedWith(ray.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	public Line FastParallelizedWith(BoundedRay ray, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastParallelizedWith(ray.Direction));

	public Line? OrthogonalizedAgainst(Line line, float pivotPointSignedDistance) {
		var newDir = Direction.OrthogonalizedAgainst(line.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	public Line FastOrthogonalizedAgainst(Line line, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastOrthogonalizedAgainst(line.Direction));
	public Line? OrthogonalizedAgainst(Ray ray, float pivotPointSignedDistance) {
		var newDir = Direction.OrthogonalizedAgainst(ray.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	public Line FastOrthogonalizedAgainst(Ray ray, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastOrthogonalizedAgainst(ray.Direction));
	public Line? OrthogonalizedAgainst(BoundedRay ray, float pivotPointSignedDistance) {
		var newDir = Direction.OrthogonalizedAgainst(ray.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	public Line FastOrthogonalizedAgainst(BoundedRay ray, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastOrthogonalizedAgainst(ray.Direction));


	public Line? ProjectedOnTo(Plane plane) {
		var projectedDirection = Direction.ParallelizedWith(plane);
		return projectedDirection == null ? null : new Line(PointOnLine.ClosestPointOn(plane), projectedDirection.Value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastProjectedOnTo(Plane plane) => new(PointOnLine.ClosestPointOn(plane), Direction.FastParallelizedWith(plane));

	public Line? ParallelizedWith(Plane plane) => ParallelizedWith(plane, 0f);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(Plane plane) => FastParallelizedWith(plane, 0f);
	public Line? ParallelizedWith(Plane plane, float pivotPointSignedDistance) {
		var projectedDirection = Direction.ParallelizedWith(plane);
		if (projectedDirection == null) return null;
		return new Line(LocationAtDistance(pivotPointSignedDistance), projectedDirection.Value);
	}
	public Line FastParallelizedWith(Plane plane, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastParallelizedWith(plane));

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
	#endregion

	#region Clamping and Interpolation
	public static Line Interpolate(Line start, Line end, float distance) {
		var startPoint = start.PointClosestTo(end);
		var endPoint = end.PointClosestTo(start);
		return new(
			Location.Interpolate(startPoint, endPoint, distance),
			Direction.Interpolate(start.Direction, end.Direction, distance)
		);
	}
	public static Rotation CreateInterpolationPrecomputation(Line start, Line end) {
		return Direction.CreateInterpolationPrecomputation(start.Direction, end.Direction);
	}
	public static Line InterpolateUsingPrecomputation(Line start, Line end, Rotation precomputation, float distance) {
		return new(
			Location.Interpolate(start.PointOnLine, end.PointOnLine, distance),
			Direction.InterpolateUsingPrecomputation(start.Direction, end.Direction, precomputation, distance)
		);
	}
	public Line Clamp(Line min, Line max) {
		var startPoint = min.PointClosestTo(max);
		var endPoint = max.PointClosestTo(min);
		return new(
			new BoundedRay(startPoint, endPoint).PointClosestTo(this).Clamp(startPoint, endPoint),
			Direction.Clamp(min.Direction, max.Direction)
		);
	}
	#endregion
}