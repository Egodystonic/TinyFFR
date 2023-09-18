// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.VectorUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Direction {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Direction directionOperand, float scalarOperand) => directionOperand.WithDistance(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(float scalarOperand, Direction directionOperand) => directionOperand.WithDistance(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithDistance(float scalar) => new(AsVector4 * scalar);



	public Angle AngleTo(Direction other) => Angle.FromRadians(MathF.Acos(Dot(AsVector4, other.AsVector4)));



	public Direction GetAnyPerpendicularDirection() {
		return FromVector3PreNormalized(Vector3.Cross(
			ToVector3(),
			Z > X ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f)
		));
	}
	public Direction OrthogonalizedAgainst(Direction d) {
		return new(NormalizeOrZero(AsVector4 - d.AsVector4 * Dot(AsVector4, d.AsVector4)));
	}



	public static Direction FromThirdOrthogonal(Direction ortho1, Direction ortho2) => FromVector3PreNormalized(Vector3.Cross(ortho1.ToVector3(), ortho2.ToVector3()));
}