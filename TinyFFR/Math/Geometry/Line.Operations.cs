// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;

namespace Egodystonic.TinyFFR;

public readonly partial struct Line :
	IAdditionOperators<Line, Vect, Line>,
	IMultiplyOperators<Line, Rotation, Line> {
	public Ray ToRay(float signedDistanceFromPointOnLine, bool flipDirection) => new(UnboundedLocationAtDistance(signedDistanceFromPointOnLine), flipDirection ? _direction.Reversed : _direction);
	public BoundedLine ToBoundedLine(float startPointSignedDistanceFromPointOnLine, float endPointSignedDistanceFromPointOnLine) {
		return new(UnboundedLocationAtDistance(startPointSignedDistanceFromPointOnLine), UnboundedLocationAtDistance(endPointSignedDistanceFromPointOnLine));
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
		return new(
			Location.Interpolate(start.PointOnLine, end.PointOnLine, distance),
			Direction.Interpolate(start._direction, end._direction, distance)
		);
	}
	public static Rotation CreateInterpolationPrecomputation(Line start, Line end) {
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
	public bool Contains(Location location) => Contains(location, ILine.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine => ILine.CalculateClosestLocationToOtherLine(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn<TLine>(TLine line) where TLine : ILine => ILine.CalculateClosestLocationToOtherLine(line, this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TLine>(TLine line) where TLine : ILine => DistanceFrom(ClosestPointTo(line));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionPointWith<TLine>(TLine line) where TLine : ILine => IntersectionPointWith(line, ILine.DefaultLineThickness);
	public Location? IntersectionPointWith<TLine>(TLine line, float lineThickness) where TLine : ILine {
		var closestPointOnLine = line.ClosestPointTo(this);
		return DistanceFrom(closestPointOnLine) <= lineThickness ? closestPointOnLine : null;
	}

	// These are implemented explicitly because an unbounded line isn't really meant to have a "distance" or a "start point" etc.
	Location ILine.BoundedLocationAtDistance(float distanceFromStart) => BoundedLocationAtDistance(distanceFromStart);
	Location ILine.UnboundedLocationAtDistance(float distanceFromStart) => UnboundedLocationAtDistance(distanceFromStart);
	Location BoundedLocationAtDistance(float distanceFromStart) => UnboundedLocationAtDistance(distanceFromStart);
	Location UnboundedLocationAtDistance(float distanceFromStart) => _pointOnLine + _direction * distanceFromStart;

	public Ray? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionPointWith(plane);
		if (intersectionPoint == null) return null;
		return new Ray(intersectionPoint.Value, _direction.ReflectedBy(plane));
	}

	public Location? IntersectionPointWith(Plane plane) {
		var similarityToNormal = plane.Normal.SimilarityTo(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		var distance = (plane.ClosestPointToOrigin - PointOnLine).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
		return UnboundedLocationAtDistance(distance);
	}

	public float SignedDistanceFrom(Plane plane) {
		if (plane.Normal.SimilarityTo(Direction) != 0f) return 0f;
		var originToPlaneVect = (Vect) plane.ClosestPointToOrigin;
		return ((Vect) PointOnLine).LengthWhenProjectedOnTo(originToPlaneVect.Direction) - originToPlaneVect.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => MathF.Abs(SignedDistanceFrom(plane));

	// TODO in xmldoc explain that it's probably better to use IntersectionPointWith and/or DistanceFrom than these two methods
	public Location ClosestPointTo(Plane plane) {
		// If we're parallel with the plane there are infinite answers so we just return the easiest one
		return IntersectionPointWith(plane) ?? PointOnLine;
	}
	public Location ClosestPointOn(Plane plane) {
		// If we're parallel with the plane there are infinite answers so we just return the easiest one
		return IntersectionPointWith(plane) ?? plane.ClosestPointToOrigin;
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
	public Line ParallelizedWith(Plane plane) => ParallelizedWith(plane, pivotPoint: PointOnLine);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line OrthogonalizedAgainst(Plane plane) => OrthogonalizedAgainst(plane, pivotPoint: PointOnLine);

	public Line ParallelizedWith(Plane plane, Location pivotPoint) {
		var projectedDirection = Direction.ProjectedOnTo(plane);
		if (projectedDirection == Direction.None) projectedDirection = Direction; // TODO xmldoc this behaviour
		return new Line(pivotPoint, projectedDirection);
	}
	public Line OrthogonalizedAgainst(Plane plane, Location pivotPoint) {
		return new Line(pivotPoint, Direction.OrthogonalizedAgainst(plane));
	}

	public Ray? SplitBy(Plane plane) {
		if (TrySplit(plane, out _, out var result)) return result;
		else return null;
	}

	public bool TrySplit(Plane plane, out Ray outWithLineDir, out Ray outOpposingLineDir) {
		var intersectionPoint = IntersectionPointWith(plane);
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