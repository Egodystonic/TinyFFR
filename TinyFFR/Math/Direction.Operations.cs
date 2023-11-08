// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Direction {
	internal bool IsUnitLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Abs(AsVector4.LengthSquared() - 1f) < 0.001f;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction operator -(Direction operand) => operand.Reversed;
	public Direction Reversed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-AsVector4);
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ToVect() => new(AsVector4);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Direction directionOperand, float scalarOperand) => directionOperand.WithDistance(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(float scalarOperand, Direction directionOperand) => directionOperand.WithDistance(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithDistance(float scalar) => new(AsVector4 * scalar);




	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Direction d1, Direction d2) => Angle.FromAngleBetweenDirections(d1, d2);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Direction other) => Angle.FromAngleBetweenDirections(this, other);



	public Direction GetAnyPerpendicularDirection() {
		return FromPreNormalizedComponents(Vector3.Cross(
			ToVector3(),
			MathF.Abs(Z) > MathF.Abs(X) ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f)
		));
	}
	public Direction OrthogonalizedAgainst(Direction d) {
		return new(NormalizeOrZero(AsVector4 - d.AsVector4 * Dot(AsVector4, d.AsVector4)));
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
}