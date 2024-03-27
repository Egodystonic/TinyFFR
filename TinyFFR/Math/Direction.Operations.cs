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
	public float this[Axis axis] => axis switch {
		Axis.X => X,
		Axis.Y => Y,
		Axis.Z => Z,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} must not be anything except {nameof(Axis.X)}, {nameof(Axis.Y)} or {nameof(Axis.Z)}.")
	};
	public XYPair<float> this[Axis first, Axis second] => new(this[first], this[second]);
	public Direction this[Axis first, Axis second, Axis third] => new(this[first], this[second], this[third]);

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
	public Vect ToVect() => (Vect) this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Direction directionOperand, float scalarOperand) => directionOperand.ToVect(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(float scalarOperand, Direction directionOperand) => directionOperand.ToVect(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ToVect(float length) => new(AsVector4 * length);


	// TODO in XMLDoc indicate that this is the dot product of the two directions, and that therefore the range is 1 for identical, to -1 for complete opposite, with 0 being orthogonal; and that this is the cosine of the angle
	public float SimilarityTo(Direction other) => Dot(AsVector4, other.AsVector4);
	


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Direction d1, Direction d2) => Angle.FromAngleBetweenDirections(d1, d2);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Direction other) => Angle.FromAngleBetweenDirections(this, other);

	public Direction GetAnyPerpendicular() {
		return FromVector3(Vector3.Cross(
			ToVector3(),
			MathF.Abs(Z) > MathF.Abs(X) ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f)
		));
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
	public Direction RotatedBy(Rotation rotation) => rotation.Rotate(this);

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction GetNearestDirectionInSpan(ReadOnlySpan<Direction> span) => span[GetIndexOfNearestDirectionInSpan(span)];
	public int GetIndexOfNearestDirectionInSpan(ReadOnlySpan<Direction> span) {
		var result = -1;
		var resultAngle = Angle.FullCircle;
		for (var i = 0; i < span.Length; ++i) {
			var newAngle = span[i] ^ this;
			if (newAngle >= resultAngle) continue;

			resultAngle = newAngle;
			result = i;
		}
		return result;
	}
	public void GetNearestOrientationCardinal(out CardinalOrientation3D orientation, out Direction direction) {
		GetNearestOrientation(AllCardinals, out var o, out direction);
		orientation = (CardinalOrientation3D) o;
	}
	public void GetNearestOrientationIntercardinal(out IntercardinalOrientation3D orientation, out Direction direction) {
		GetNearestOrientation(AllIntercardinals, out var o, out direction);
		orientation = (IntercardinalOrientation3D) o;
	}
	public void GetNearestOrientationDiagonal(out DiagonalOrientation3D orientation, out Direction direction) {
		GetNearestOrientation(AllDiagonals, out var o, out direction);
		orientation = (DiagonalOrientation3D) o;
	}
	public void GetNearestOrientation(out Orientation3D orientation, out Direction direction) {
		GetNearestOrientation(AllOrientations, out orientation, out direction);
	}
	void GetNearestOrientation(ReadOnlySpan<Direction> span, out Orientation3D orientation, out Direction direction) {
		if (Equals(None, 0.0001f)) {
			orientation = Orientation3D.None;
			direction = None;
			return;
		}
	
		direction = default;
		var dirAngle = Angle.FullCircle;
		for (var i = 0; i < span.Length; ++i) {
			var testDir = span[i];
			if (X != 0f && Single.Sign(testDir.X) == -Single.Sign(X)) continue;
			if (Y != 0f && Single.Sign(testDir.Y) == -Single.Sign(Y)) continue;
			if (Z != 0f && Single.Sign(testDir.Z) == -Single.Sign(Z)) continue;
	
			var newAngle = testDir ^ this;
			if (newAngle >= dirAngle) continue;
	
			dirAngle = newAngle;
			direction = testDir;
		}
	
		orientation = OrientationUtils.CreateOrientationFromValueSigns(direction.X, direction.Y, direction.Z);
	}
}