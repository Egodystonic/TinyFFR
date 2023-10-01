// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.VectorUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Angle {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator -(Angle operand) => operand.Reversed;
	public Angle Reversed {
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

	// TODO Sine, Cosine, and other convenience properties

	// TODO interactions with other types

	// TODO consider overrideable operators

	// TODO clamp?
}