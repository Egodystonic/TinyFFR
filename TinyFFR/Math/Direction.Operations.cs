// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Direction :
	IUnaryNegationOperators<Direction, Direction>,
	IMultiplyOperators<Direction, float, Vect>,
	IModulusOperators<Direction, Angle, Rotation>,
	IPrecomputationInterpolatable<Direction, Rotation>,
	IBoundedRandomizable<Direction> {
	internal bool IsUnitLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Abs(AsVector4.LengthSquared() - 1f) < 0.002f;
	}
	public Direction Renormalized {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(NormalizeOrZero(AsVector4));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction operator -(Direction operand) => operand.Reversed;
	public Direction Reversed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-AsVector4);
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ToVect() => ToVect(1f);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Direction directionOperand, float scalarOperand) => directionOperand.ToVect(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(float scalarOperand, Direction directionOperand) => directionOperand.ToVect(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ToVect(float length) => new(AsVector4 * length);




	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Direction d1, Direction d2) => Angle.FromAngleBetweenDirections(d1, d2);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Direction other) => Angle.FromAngleBetweenDirections(this, other);



	public Direction GetAnyPerpendicularDirection() {
		return FromVector3(Vector3.Cross(
			ToVector3(),
			MathF.Abs(Z) > MathF.Abs(X) ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f)
		));
	}
	public Direction GetAnyPerpendicularDirection(Direction additionalConstrainingDirection) {
		var cross = Vector3.Cross(ToVector3(), additionalConstrainingDirection.ToVector3());
		var crossLength = cross.LengthSquared();

		if (MathF.Abs(crossLength - 1f) <= 0.001f) return FromPreNormalizedComponents(cross);
		else if (crossLength >= 0.001f) return FromVector3(cross);
		else if (this.Equals(None, 0.001f) || additionalConstrainingDirection.Equals(None, 0.001f)) return None;
		else return GetAnyPerpendicularDirection();
	}

	public Direction OrthogonalizedAgainst(Direction d) {
		var dot = Dot(AsVector4, d.AsVector4);
		// These checks are important to protect against fp inaccuracy with cases where we're orthogonalizing against the self or reverse of self etc
		dot = MathF.Abs(dot) switch {
			> 0.9999f => 1f * MathF.Sign(dot),
			< 0.0001f => 0f,
			_ => dot
		};
		var nonNormalizedResult = AsVector4 - d.AsVector4 * dot;
		if (nonNormalizedResult.LengthSquared() < 0.00001f) return None;
		else return new(Normalize(nonNormalizedResult));
	}



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator >>(Direction start, Direction end) => Rotation.FromStartAndEndDirection(start, end);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator <<(Direction end, Direction start) => Rotation.FromStartAndEndDirection(start, end);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation RotationTo(Direction other) => Rotation.FromStartAndEndDirection(this, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation RotationFrom(Direction other) => Rotation.FromStartAndEndDirection(other, this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator %(Direction axis, Angle angle) => Rotation.FromAngleAroundAxis(angle, axis);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator %(Angle angle, Direction axis) => Rotation.FromAngleAroundAxis(angle, axis);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction RotateBy(Rotation rotation) => rotation.Rotate(this);

	public static Direction Interpolate(Direction start, Direction end, float distance) {
		return Rotation.FromStartAndEndDirection(start, end).ScaledBy(distance) * start;
	}
	public static Rotation CreateInterpolationPrecomputation(Direction start, Direction end) {
		return Rotation.FromStartAndEndDirection(start, end);
	}
	public static Direction InterpolateUsingPrecomputation(Direction start, Direction end, Rotation precomputation, float distance) {
		return precomputation.ScaledBy(distance) * start;
	}

	public static Direction CreateNewRandom() {
		Direction result;
		do {
			result = new(
				RandomUtils.NextSingleNegOneToOneInclusive(),
				RandomUtils.NextSingleNegOneToOneInclusive(),
				RandomUtils.NextSingleNegOneToOneInclusive()
			);
		} while (result == None);
		return result;
	}
	public static Direction CreateNewRandom(Direction minInclusive, Direction maxExclusive) {
		return (minInclusive >> maxExclusive).ScaledBy(RandomUtils.NextSingle()) * minInclusive;
	}
}