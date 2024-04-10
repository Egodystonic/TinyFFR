// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

public readonly partial struct Line :
	IAdditionOperators<Line, Vect, Line>,
	IMultiplyOperators<Line, Rotation, Line> {
	public Ray ToRay(float signedDistanceAlongLine, bool flipDirection) => new(LocationAtDistance(signedDistanceAlongLine), flipDirection ? _direction.Reversed : _direction);
	public BoundedLine ToBoundedLine(float startSignedDistanceAlongLine, float endSignedDistanceAlongLine) {
		return new(LocationAtDistance(startSignedDistanceAlongLine), LocationAtDistance(endSignedDistanceAlongLine));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, Rotation rot) => line.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Rotation rot, Line line) => line.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line RotatedBy(Rotation rotation) => new(PointOnLine, _direction.RotatedBy(rotation));



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Line line, Vect v) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Vect v, Line line) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line MovedBy(Vect v) => new(PointOnLine + v, _direction);
	
	
	public static Line Interpolate(Line start, Line end, float distance) {
		var startPoint = start.ClosestPointTo(end);
		var endPoint = end.ClosestPointTo(start);
		return new(
			Location.Interpolate(startPoint, endPoint, distance),
			Direction.Interpolate(start._direction, end._direction, distance)
		);
	}
	public static Rotation CreateInterpolationPrecomputation(Line start, Line end) { // TODO add startPoint/endPoint to precomputation object
		return Direction.CreateInterpolationPrecomputation(start._direction, end._direction);
	}
	public static Line InterpolateUsingPrecomputation(Line start, Line end, Rotation precomputation, float distance) {
		return new(
			Location.Interpolate(start.PointOnLine, end.PointOnLine, distance),
			Direction.InterpolateUsingPrecomputation(start._direction, end._direction, precomputation, distance)
		);
	}
	public static Line CreateNewRandom() => new(Location.CreateNewRandom(), Direction.CreateNewRandom());
	public static Line CreateNewRandom(Line minInclusive, Line maxExclusive) => new(Location.CreateNewRandom(minInclusive.PointOnLine, maxExclusive.PointOnLine), Direction.CreateNewRandom(minInclusive._direction, maxExclusive._direction));
	

	public Location ClosestPointTo(Location location) {
		var distance = Vector3.Dot((location - PointOnLine).ToVector3(), Direction.ToVector3());
		return PointOnLine + Direction * distance;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointToOrigin() {
		var distance = -Vector3.Dot(PointOnLine.ToVector3(), Direction.ToVector3());
		return PointOnLine + Direction * distance;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location location) => location.DistanceFrom(ClosestPointTo(location));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromOrigin() => ((Vect) ClosestPointToOrigin()).Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, ILine.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;

	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine {
		return line switch {
			Line l => ClosestPointTo(l),
			Ray r => ClosestPointTo(r),
			BoundedLine b => ClosestPointTo(b),
			_ => line.ClosestPointOn(this) // Possible stack overflow if the other line doesn't implement both sides (To/On), but this allows users to add their own line implementations
		};
	}
	public Location ClosestPointTo(Line line) {
		var intersectionDistance = ILine.CalculateUnboundedIntersectionDistanceOnThisLine(this, line);
		return LocationAtDistance(intersectionDistance ?? 0f);
	}
	public Location ClosestPointTo(Ray ray) {
		var intersectionDistances = ILine.CalculateUnboundedIntersectionDistancesOnBothLines(this, ray);
		if (intersectionDistances == null || !ray.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return ClosestPointTo(ray.StartPoint);
		else return LocationAtDistance(intersectionDistances.Value.ThisDistance);
	}
	public Location ClosestPointTo(BoundedLine boundedLine) {
		var intersectionDistances = ILine.CalculateUnboundedIntersectionDistancesOnBothLines(this, boundedLine);
		if (intersectionDistances == null || intersectionDistances.Value.OtherDistance < 0f) return ClosestPointTo(boundedLine.StartPoint);
		else if (!boundedLine.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return ClosestPointTo(boundedLine.EndPoint);
		else return LocationAtDistance(intersectionDistances.Value.ThisDistance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn<TLine>(TLine line) where TLine : ILine => line.ClosestPointTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.ClosestPointTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.ClosestPointTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedLine boundedLine) => boundedLine.ClosestPointTo(this);

	public float DistanceFrom<TLine>(TLine line) where TLine : ILine => DistanceFrom(ClosestPointOn(line));
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	public float DistanceFrom(BoundedLine boundedLine) => DistanceFrom(ClosestPointOn(boundedLine));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location? ILineIntersectable<Location>.IntersectionWith<TLine>(TLine line) => IntersectionWith(line, ILine.DefaultLineThickness);
	public Location? IntersectionWith<TLine>(TLine line, float lineThickness = ILine.DefaultLineThickness) where TLine : ILine {
		var closestPointOnLine = ClosestPointOn(line);
		return DistanceFrom(closestPointOnLine) <= lineThickness ? closestPointOnLine : null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool ILineIntersectable<Location>.IsIntersectedBy<TLine>(TLine line) => IsIntersectedBy(line, ILine.DefaultLineThickness);
	public bool IsIntersectedBy<TLine>(TLine line, float lineThickness = ILine.DefaultLineThickness) where TLine : ILine {
		return DistanceFrom(ClosestPointOn(line)) <= lineThickness;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location LocationAtDistance(float signedDistanceFromStart) => _pointOnLine + _direction * signedDistanceFromStart;

	// These are implemented explicitly because they're all basically the same thing or useless for an unbounded line.
	bool ILine.DistanceIsWithinLineBounds(float signedDistanceFromStart) => true;
	float ILine.BindDistance(float signedDistanceFromStart) => signedDistanceFromStart;
	Location ILine.BoundedLocationAtDistance(float signedDistanceFromStart) => LocationAtDistance(signedDistanceFromStart);
	Location ILine.UnboundedLocationAtDistance(float signedDistanceFromStart) => LocationAtDistance(signedDistanceFromStart);
	Location? ILine.LocationAtDistanceOrNull(float signedDistanceFromStart) => LocationAtDistance(signedDistanceFromStart);

	public Ray? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new Ray(intersectionPoint.Value, _direction.ReflectedBy(plane));
	}

	public Location? IntersectionWith(Plane plane) {
		var similarityToNormal = plane.Normal.SimilarityTo(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		var distance = (plane.ClosestPointToOrigin - PointOnLine).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
		return LocationAtDistance(distance);
	}

	public bool IsIntersectedBy(Plane plane) => plane.Normal.SimilarityTo(Direction) != 0f;

	public float SignedDistanceFrom(Plane plane) {
		if (plane.Normal.SimilarityTo(Direction) != 0f) return 0f;
		var originToPlaneVect = (Vect) plane.ClosestPointToOrigin;
		return ((Vect) PointOnLine).LengthWhenProjectedOnTo(originToPlaneVect.Direction) - originToPlaneVect.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => MathF.Abs(SignedDistanceFrom(plane));

	// TODO in xmldoc explain that it's probably better to use IntersectionWith and/or DistanceFrom than these two methods
	public Location ClosestPointTo(Plane plane) {
		// If we're parallel with the plane there are infinite answers so we just return the easiest one
		return IntersectionWith(plane) ?? PointOnLine;
	}
	public Location ClosestPointOn(Plane plane) {
		// If we're parallel with the plane there are infinite answers so we just return the easiest one
		return IntersectionWith(plane) ?? plane.ClosestPointToOrigin;
	}

	public PlaneObjectRelationship RelationshipTo(Plane plane) {
		return SignedDistanceFrom(plane) switch {
			> 0f => PlaneObjectRelationship.PlaneFacesTowardsObject,
			< 0f => PlaneObjectRelationship.PlaneFacesAwayFromObject,
			_ => PlaneObjectRelationship.PlaneIntersectsObject
		};
	}

	public Line ProjectedOnTo(Plane plane) {
		var projectedDirection = Direction.ProjectedOnTo(plane);
		if (projectedDirection == Direction.None) projectedDirection = Direction; // TODO xmldoc this behaviour
		return new Line(PointOnLine.ClosestPointOn(plane), projectedDirection);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ParallelizedWith(Plane plane) => ParallelizedWith(plane, 0f);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line OrthogonalizedAgainst(Plane plane) => OrthogonalizedAgainst(plane, 0f);

	public Line ParallelizedWith(Plane plane, float pivotPointSignedDistance) {
		var projectedDirection = Direction.ProjectedOnTo(plane);
		if (projectedDirection == Direction.None) projectedDirection = Direction; // TODO xmldoc this behaviour
		return new Line(LocationAtDistance(pivotPointSignedDistance), projectedDirection);
	}
	public Line OrthogonalizedAgainst(Plane plane, float pivotPointSignedDistance) {
		return new Line(LocationAtDistance(pivotPointSignedDistance), Direction.OrthogonalizedAgainst(plane));
	}

	public Ray? SplitBy(Plane plane) {
		if (TrySplit(plane, out _, out var result)) return result;
		else return null;
	}

	public bool TrySplit(Plane plane, out Ray outWithLineDir, out Ray outOpposingLineDir) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) {
			outWithLineDir = default;
			outOpposingLineDir = default;
			return false;
		}

		outWithLineDir = new Ray(intersectionPoint.Value, Direction);
		outOpposingLineDir = new Ray(intersectionPoint.Value, -Direction);
		return true;
	}
}