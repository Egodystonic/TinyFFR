// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static System.Numerics.Quaternion;
using static Egodystonic.TinyFFR.MathUtils;

namespace Egodystonic.TinyFFR;

partial struct Rotation {
	static Vector4 Rotate(Quaternion q, Vector4 v) {
		var quatVec = new Vector3(q.X, q.Y, q.Z);
		var targetVec = new Vector3(v.X, v.Y, v.Z);
		var t = Vector3.Cross(quatVec, targetVec) * 2f;
		return new Vector4(
			targetVec + q.W * t + Vector3.Cross(quatVec, t),
			v.W
		);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator -(Rotation operand) => operand.Reversed;
	public Rotation Reversed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(new(-AsQuaternion.X, -AsQuaternion.Y, -AsQuaternion.Z, AsQuaternion.W));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction operator *(Direction d, Rotation r) => r.Rotate(d);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction operator *(Rotation r, Direction d) => r.Rotate(d);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Vect d, Rotation r) => r.Rotate(d);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Rotation r, Vect d) => r.Rotate(d);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction Rotate(Direction d) => new(Rotate(AsQuaternion, d.AsVector4));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Rotate(Vect v) => new(Rotate(AsQuaternion, v.AsVector4));



	// We provide this as a probably more intuitive way of adding rotations, even if it's not the arithmetic operation used to combine quaternions.
	// Ultimately this type is meant to be an abstraction of a Rotation, not a Quaternion, which is a type I don't want users to have to care about or even know about if they don't want to.
	// Notice that (lhs + rhs) is the OPPOSITE of (lhs.AsQuaternion * rhs.AsQuaternion) (see how we multiply other.AsQuaternion by this.AsQuaternion in FollowedBy())
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator +(Rotation lhs, Rotation rhs) => lhs.FollowedBy(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation FollowedBy(Rotation other) => new(other.AsQuaternion * AsQuaternion);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator *(Rotation rotation, float scalar) => rotation.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator *(float scalar, Rotation rotation) => rotation.ScaledBy(scalar);
	public Rotation ScaledBy(float scalar) {
		var halfAngleRadians = MathF.Acos(AsQuaternion.W);
		if (halfAngleRadians < 0.0001f) return None;
		
		var newHalfAngleRadians = halfAngleRadians * scalar;
		var sinNewHalfAngle = MathF.Sin(newHalfAngleRadians);
		if (MathF.Abs(sinNewHalfAngle) < 0.0001f) return None;
		var cosNewHalfAngle = MathF.Cos(newHalfAngleRadians);

		var normalizedVectorComponent = Vector3.Normalize(new(AsQuaternion.X, AsQuaternion.Y, AsQuaternion.Z));
		// Shouldn't be possible unless someone's scaling default(Rotation) (or some other non-unit quaternion) but we already have two other branches so we may as well check for the user here
		if (Single.IsNaN(normalizedVectorComponent.X)) return None;

		return new(new(
			normalizedVectorComponent * sinNewHalfAngle,
			cosNewHalfAngle
		));
	}
}