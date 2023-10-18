// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.VectorUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Angle {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator -(Angle operand) => operand.Negated;
	public Angle Negated {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromRadians(-_asRadians);
	}



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator *(Angle angle, float scalar) => angle.MultipliedBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator *(float scalar, Angle angle) => angle.MultipliedBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator /(Angle angle, float scalar) => angle.DividedBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle MultipliedBy(float scalar) => FromRadians(_asRadians * scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle DividedBy(float scalar) => FromRadians(_asRadians / scalar);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator +(Angle lhs, Angle rhs) => lhs.Plus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator -(Angle lhs, Angle rhs) => lhs.Minus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle Plus(Angle other) => FromRadians(_asRadians + other._asRadians);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle Minus(Angle other) => FromRadians(_asRadians - other._asRadians);



	public float Sine {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Sin(_asRadians);
	}
	public float Cosine {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Cos(_asRadians);
	}
	// TODO Sine, Cosine, and other convenience properties



	// TODO interactions with other types

	// TODO consider overrideable operators


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle Clamp(Angle min, Angle max) => FromRadians(Math.Clamp(_asRadians, min._asRadians, max._asRadians));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampZeroToFullCircle() => Clamp(None, FullCircle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampNegativeFullCircleToFullCircle() => Clamp(-FullCircle, FullCircle);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(Angle other) => Radians.CompareTo(other.Radians);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(Angle left, Angle right) => left.Radians > right.Radians;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(Angle left, Angle right) => left.Radians >= right.Radians;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(Angle left, Angle right) => left.Radians < right.Radians;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(Angle left, Angle right) => left.Radians <= right.Radians;

	// TODO parameter validation + exception throwing here and everywhere where it's relevant and not performance critical
	// TODO and then check that validation in unit tests
}