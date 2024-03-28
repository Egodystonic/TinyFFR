// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;
using System.Net;

namespace Egodystonic.TinyFFR;

public readonly partial struct Ray : 
	IUnaryNegationOperators<Ray, Ray>,
	IMultiplyOperators<Ray, Rotation, Ray>, 
	IAdditionOperators<Ray, Vect, Ray> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine ToBoundedLine(float length) => new(_startPoint, _direction * length);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ToLine() => new(_startPoint, _direction);

	public Ray Flipped {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_startPoint, -_direction);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator -(Ray operand) => operand.Flipped;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *(Ray ray, Rotation rot) => ray.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *(Rotation rot, Ray ray) => ray.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray RotatedBy(Rotation rotation) => new(_startPoint, _direction.RotatedBy(rotation));


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator +(Ray ray, Vect v) => ray.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator +(Vect v, Ray ray) => ray.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray MovedBy(Vect v) => new(_startPoint + v, _direction);


	public static Ray Interpolate(Ray start, Ray end, float distance) {
		return new(
			Location.Interpolate(start.StartPoint, end.StartPoint, distance),
			Direction.Interpolate(start.Direction, end.Direction, distance)
		);
	}
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


	public Location ClosestPointTo(Location location) {
		var distance = Vector3.Dot((location - _startPoint).ToVector3(), Direction.ToVector3());
		return distance switch {
			< 0f => _startPoint,
			_ => _startPoint + Direction * distance
		};
	}
	public Location ClosestPointToOrigin() {
		var distance = -Vector3.Dot(_startPoint.ToVector3(), Direction.ToVector3());
		return distance switch {
			< 0f => _startPoint,
			_ => _startPoint + Direction * distance
		};
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
	Location? ILineIntersectable<Location>.IntersectionWith<TLine>(TLine line) => IntersectionWith(line, ILine.DefaultLineThickness);
	public Location? IntersectionWith<TLine>(TLine line, float lineThickness = ILine.DefaultLineThickness) where TLine : ILine {
		var closestPointOnLine = line.ClosestPointTo(this);
		return DistanceFrom(closestPointOnLine) <= lineThickness ? closestPointOnLine : null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool DistanceIsWithinLineBounds(float signedDistanceFromStart) => signedDistanceFromStart >= 0f;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float BindDistance(float signedDistanceFromStart) => MathF.Max(0f, signedDistanceFromStart);
	public Location BoundedLocationAtDistance(float signedDistanceFromStart) => UnboundedLocationAtDistance(BindDistance(signedDistanceFromStart));
	public Location UnboundedLocationAtDistance(float signedDistanceFromStart) => _startPoint + _direction * signedDistanceFromStart;
	public Location? LocationAtDistanceOrNull(float signedDistanceFromStart) => DistanceIsWithinLineBounds(signedDistanceFromStart) ? UnboundedLocationAtDistance(signedDistanceFromStart) : null;

	public Ray? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new Ray(intersectionPoint.Value, _direction.ReflectedBy(plane));
	}

	float? GetUnboundedPlaneIntersectionDistance(Plane plane) {
		var similarityToNormal = plane.Normal.SimilarityTo(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		return (plane.ClosestPointToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
	}

	public Location? IntersectionWith(Plane plane) {
		var distance = GetUnboundedPlaneIntersectionDistance(plane);
		if (distance >= 0f) return UnboundedLocationAtDistance(distance.Value);
		else return null; // Plane behind ray or parallel with ray
	}

	public float SignedDistanceFrom(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		if (unboundedDistance >= 0f) return 0f;
		else return plane.SignedDistanceFrom(StartPoint);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => MathF.Abs(SignedDistanceFrom(plane));

	public Location ClosestPointTo(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		return BoundedLocationAtDistance(unboundedDistance ?? 0f); // If unboundedDistance is null we're parallel so the StartPoint is as close as any other point
	}
	public Location ClosestPointOn(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		var closestPointOnLine = BoundedLocationAtDistance(unboundedDistance ?? 0f);
		if (unboundedDistance >= 0f) return closestPointOnLine; // Actual intersection
		else return plane.ClosestPointTo(closestPointOnLine);
	}

	public PlaneObjectRelationship RelationshipTo(Plane plane) {
		return SignedDistanceFrom(plane) switch {
			> 0f => PlaneObjectRelationship.PlaneFacesTowardsObject,
			< 0f => PlaneObjectRelationship.PlaneFacesAwayFromObject,
			_ => PlaneObjectRelationship.PlaneIntersectsObject
		};
	}

	public Ray ProjectedOnTo(Plane plane) {
		var projectedDirection = Direction.ProjectedOnTo(plane);
		if (projectedDirection == Direction.None) projectedDirection = Direction;
		return new Ray(StartPoint.ClosestPointOn(plane), projectedDirection);
	}
	public Ray ParallelizedWith(Plane plane) {
		var projectedDirection = Direction.ProjectedOnTo(plane);
		if (projectedDirection == Direction.None) projectedDirection = Direction;
		return new Ray(StartPoint, projectedDirection);
	}
	public Ray OrthogonalizedAgainst(Plane plane) {
		return new Ray(StartPoint, Direction.OrthogonalizedAgainst(plane));
	}

	public Ray? SplitBy(Plane plane) {
		if (TrySplit(plane, out _, out var result)) return result;
		else return null;
	}

	public bool TrySplit(Plane plane, out BoundedLine outStartPointToPlane, out Ray outPlaneToInfinity) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) {
			outStartPointToPlane = default;
			outPlaneToInfinity = default;
			return false;
		}

		outStartPointToPlane = new BoundedLine(StartPoint, intersectionPoint.Value);
		outPlaneToInfinity = new Ray(intersectionPoint.Value, Direction);
		return true;
	}
}