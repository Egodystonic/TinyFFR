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

	public Ray Reversed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_startPoint, -_direction);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator -(Ray operand) => operand.Reversed;


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
	public Location? IntersectionPointWith<TLine>(TLine line) where TLine : ILine => IntersectionPointWith(line, ILine.DefaultLineThickness);
	public Location? IntersectionPointWith<TLine>(TLine line, float lineThickness) where TLine : ILine {
		var closestPointOnLine = line.ClosestPointTo(this);
		return DistanceFrom(closestPointOnLine) <= lineThickness ? closestPointOnLine : null;
	}

	public Location LocationAtDistance(float distanceFromStart) => UnboundedLocationAtDistance(distanceFromStart > 0f ? distanceFromStart : 0f);
	public Location UnboundedLocationAtDistance(float distanceFromStart) => _startPoint + _direction * distanceFromStart;

	public Ray? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionPointWith(plane);
		if (intersectionPoint == null) return null;
		return new Ray(intersectionPoint.Value, _direction.ReflectedBy(plane));
	}
}