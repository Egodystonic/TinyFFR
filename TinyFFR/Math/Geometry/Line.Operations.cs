// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;

namespace Egodystonic.TinyFFR;

public readonly partial struct Line :
	IAdditionOperators<Line, Vect, Line>,
	IMultiplyOperators<Line, Rotation, Line> {
	public enum LineToRayStrategy {
		RayPointsInLineDirection = 0,
		RayPointsOppositeToLineDirection = 1
	}
	public enum LineToBoundedLineStrategy {
		ClosestPointToOriginIsBoundedLineStart = 0,
		ClosestPointToOriginIsBoundedLineMiddle = 1,
		ClosestPointToOriginIsBoundedLineEnd = 2,
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRay(LineToRayStrategy conversionStrategy) => new(_closestPointToOrigin, Direction.FromVector3(_direction.ToVector3() * (((int) conversionStrategy) + 1f)));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine ToBoundedLine(float length, LineToBoundedLineStrategy conversionStrategy) {
		var vect = _direction * length;
		return new(_closestPointToOrigin - vect * (((int) conversionStrategy) * 0.5f), vect);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, Rotation rot) => line.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Rotation rot, Line line) => line.RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line RotatedBy(Rotation rotation) => new(_closestPointToOrigin, _direction.RotatedBy(rotation));
	
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Line line, Vect v) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Vect v, Line line) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line MovedBy(Vect v) => new(_closestPointToOrigin + v, _direction);
	
	
	public static Line Interpolate(Line start, Line end, float distance) {
		return new(
			Location.Interpolate(start._closestPointToOrigin, end._closestPointToOrigin, distance),
			Direction.Interpolate(start._direction, end._direction, distance)
		);
	}
	public static Rotation CreateInterpolationPrecomputation(Line start, Line end) {
		return Direction.CreateInterpolationPrecomputation(start._direction, end._direction);
	}
	public static Line InterpolateUsingPrecomputation(Line start, Line end, Rotation precomputation, float distance) {
		return new(
			Location.Interpolate(start._closestPointToOrigin, end._closestPointToOrigin, distance),
			Direction.InterpolateUsingPrecomputation(start._direction, end._direction, precomputation, distance)
		);
	}
	public static Line CreateNewRandom() => new(Location.CreateNewRandom(), Direction.CreateNewRandom());
	public static Line CreateNewRandom(Line minInclusive, Line maxExclusive) => new(Location.CreateNewRandom(minInclusive._closestPointToOrigin, maxExclusive._closestPointToOrigin), Direction.CreateNewRandom(minInclusive._direction, maxExclusive._direction));
	

	public Location ClosestPointTo(Location location) {
		var distance = Vector3.Dot((location - _closestPointToOrigin).ToVector3(), Direction.ToVector3());
		return _closestPointToOrigin + Direction * distance;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location location) => location.DistanceFrom(ClosestPointTo(location));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, ILine.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine => ILine.CalculateClosestPointToOtherLine(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TLine>(TLine line) where TLine : ILine => DistanceFrom(ClosestPointTo(line));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionPointWith<TLine>(TLine line) where TLine : ILine => GetIntersectionPointOn(line, ILine.DefaultLineThickness);
	public Location? GetIntersectionPointOn<TLine>(TLine line, float lineThickness) where TLine : ILine {
		var closestPointOnLine = line.ClosestPointTo(this);
		return DistanceFrom(closestPointOnLine) <= lineThickness ? closestPointOnLine : null;
	}
}