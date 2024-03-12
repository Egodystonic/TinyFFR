// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Vect :
	IUnaryNegationOperators<Vect, Vect>,
	IAdditionOperators<Vect, Vect, Vect>,
	ISubtractionOperators<Vect, Vect, Vect>,
	IMultiplyOperators<Vect, float, Vect>,
	IDivisionOperators<Vect, float, Vect>,
	IInterpolatable<Vect>,
	IBoundedRandomizable<Vect> {
	internal const float DefaultRandomRange = 100f;

	public float Length {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Length();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4 = Direction.AsVector4 * value;
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
	public float LengthWhenProjectedOnTo(Direction d) => Dot(AsVector4, d.AsVector4);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ProjectedOnTo(Direction d) => d * LengthWhenProjectedOnTo(d);
	public Vect ProjectedOnTo(Direction d, bool preserveLength) {
		var projectedVect = ProjectedOnTo(d);
		if (!preserveLength) return projectedVect;

		if (projectedVect.LengthSquared < 0.0001f) return Zero;
		else return projectedVect.WithLength(Length);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect OrthogonalizedAgainst(Direction d) => Direction.OrthogonalizedAgainst(d) * Length;



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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithLength(float newLength) => this with { Length = newLength };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ShortenedBy(float lengthDecrease) => this with { Length = Length - lengthDecrease };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect LengthenedBy(float lengthIncrease) => this with { Length = Length + lengthIncrease };


	public static Vect Interpolate(Vect start, Vect end, float distance) {
		return start + (end - start) * distance;
	}

	public static Vect CreateNewRandom() {
		return FromVector3(new Vector3(
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive()
		) * DefaultRandomRange);
	}
	public static Vect CreateNewRandom(Vect minInclusive, Vect maxExclusive) {
		return FromVector3(new(
			RandomUtils.NextSingle(minInclusive.X, maxExclusive.X),
			RandomUtils.NextSingle(minInclusive.Y, maxExclusive.Y),
			RandomUtils.NextSingle(minInclusive.Z, maxExclusive.Z)
		));
	}
}