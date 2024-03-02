// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;

namespace Egodystonic.TinyFFR;

public readonly partial struct Ray : 
	IUnaryNegationOperators<Ray, Ray>,
	IMultiplyOperators<Ray, Rotation, Ray>, 
	IAdditionOperators<Ray, Vect, Ray> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ToLine(float length) => new(_startPoint, _direction * length);

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


	
}