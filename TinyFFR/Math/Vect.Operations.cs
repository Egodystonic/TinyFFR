// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Vect {
	public float Length {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Length();
	}
	public float LengthSquared {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.LengthSquared();
	}
	public bool IsNormalized {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Abs(1f - LengthSquared) < 0.001f;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator -(Vect operand) => operand.Reversed;
	public Vect Reversed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-AsVector4);
	}



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator +(Vect lhs, Vect rhs) => lhs.Plus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Plus(Vect other) => new(AsVector4 + other.AsVector4);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator -(Vect lhs, Vect rhs) => lhs.Minus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Minus(Vect other) => new(AsVector4 - other.AsVector4);



	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(NormalizeOrZero(AsVector4));
	}
	public Vect Normalized {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(NormalizeOrZero(AsVector4));
	}



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ProjectedOnTo(Direction d) => d * Dot(AsVector4, d.AsVector4);
	public Vect ProjectedOnTo(Direction d, bool retainLength) {
		var projectedVect = ProjectedOnTo(d);
		if (!retainLength) return projectedVect;

		if (projectedVect.LengthSquared < 0.0001f) return Zero;
		else return projectedVect.WithLength(Length);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect OrthogonalizedAgainst(Direction d) => Direction.OrthogonalizedAgainst(d) * Length;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithLength(float length) => Direction * length;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect RotateBy(Rotation rotation) => rotation.Rotate(this);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Vect vectOperand, float scalarOperand) => vectOperand.ScaledBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(float scalarOperand, Vect vectOperand) => vectOperand.ScaledBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator /(Vect vectOperand, float scalarOperand) => vectOperand.ScaledBy(1f / scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ScaledBy(float scalar) => new(Multiply(AsVector4, scalar));
}