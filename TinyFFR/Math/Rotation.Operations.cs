// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static System.Numerics.Quaternion;
using static Egodystonic.TinyFFR.MathUtils;

namespace Egodystonic.TinyFFR;

partial struct Rotation : 
	IAlgebraicGroup<Rotation>,
	IAngleMeasurable<Rotation, Rotation>,
	IScalable<Rotation>,
	IInterpolatable<Rotation> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator -(Rotation operand) => operand.Inverted;
	public Rotation Inverted {
		get => new(new(-AsQuaternion.X, -AsQuaternion.Y, -AsQuaternion.Z, AsQuaternion.W));
	}
	static Rotation IAdditiveIdentity<Rotation, Rotation>.AdditiveIdentity => None;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation WithAngle(Angle angle) => new(angle, Axis);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation WithAxis(Direction axis) => new(Angle, axis);

	// Renormalize because we can accrue a lot of error here and we're doing a heavy operation anyway. Offer RotateWithoutRenormalizing for perf sensitive cases
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction Rotate(Direction d) => Direction.Renormalize(RotateWithoutRenormalizing(d));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction RotateWithoutRenormalizing(Direction d) => new(Rotate(AsQuaternion, d.AsVector4));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Rotate(Vect v) => new(Rotate(AsQuaternion, v.AsVector4));



	// We provide this as a probably more intuitive way of adding rotations, even if it's not the arithmetic operation used to combine quaternions.
	// Ultimately this type is meant to be an abstraction of a Rotation, not a Quaternion, which is a type I don't want users to have to care about or even know about if they don't want to.
	// Notice, for example, that (lhs + rhs) is the OPPOSITE of (lhs.AsQuaternion * rhs.AsQuaternion) (see how we multiply other.AsQuaternion by this.AsQuaternion in Plus())
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator +(Rotation lhs, Rotation rhs) => lhs.Plus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator -(Rotation lhs, Rotation rhs) => lhs.Minus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation Plus(Rotation other) => new(other.AsQuaternion * AsQuaternion);
	// Was previously known as "DifferenceTo()" because this method is trying to mimic the standard - function for Reals/Integers (e.g. 7 - 3 = 4, 4 is the difference of 7 to 3).
	public Rotation Minus(Rotation other) => FromQuaternion(Inverted.AsQuaternion * other.AsQuaternion);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Rotation left, Rotation right) => left.AngleTo(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Rotation other) => Minus(other).Angle;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator *(Rotation rotation, float scalar) => rotation.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator *(float scalar, Rotation rotation) => rotation.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator /(Rotation rotation, float scalar) => rotation.ScaledBy(1f / scalar);
	public Rotation ScaledBy(float scalar) { // Quaternion exponentiation
		const float FloatingPointErrorMargin = 1E-4f;

		var halfAngleRadians = MathF.Acos(AsQuaternion.W);
		if (halfAngleRadians < FloatingPointErrorMargin) return None;
		
		var newHalfAngleRadians = halfAngleRadians * scalar;
		var sinNewHalfAngle = MathF.Sin(newHalfAngleRadians);
		if (MathF.Abs(sinNewHalfAngle) < FloatingPointErrorMargin) return None;
		var cosNewHalfAngle = MathF.Cos(newHalfAngleRadians);

		var normalizedVectorComponent = Vector3.Normalize(new(AsQuaternion.X, AsQuaternion.Y, AsQuaternion.Z));
		// Shouldn't be possible unless someone's scaling default(Rotation) (or some other non-unit quaternion) but we already have two other branches so we may as well check for the user here
		if (Single.IsNaN(normalizedVectorComponent.X)) return None;

		return new(new(
			normalizedVectorComponent * sinNewHalfAngle,
			cosNewHalfAngle
		));
	}


	public static Rotation Interpolate(Rotation start, Rotation end, float distance) {
		const float CosPhiMinForLinearRenormalization = 1f - 0.001f;
		return MathF.Abs(Dot(start.AsQuaternion, end.AsQuaternion)) > CosPhiMinForLinearRenormalization
			? ApproximatelyInterpolate(start, end, distance)
			: AccuratelyInterpolate(start, end, distance);
	}
	public static Rotation AccuratelyInterpolate(Rotation start, Rotation end, float distance) { // Quaternion slerp
		return FromQuaternionPreNormalized(Slerp(start.AsQuaternion, end.AsQuaternion, distance));
	}
	public static Rotation ApproximatelyInterpolate(Rotation start, Rotation end, float distance) { // Vector lerp
		return FromQuaternion(start.AsQuaternion + (end.AsQuaternion - start.AsQuaternion) * distance);
	}
	public static Rotation Interpolate(Angle startAngle, Angle endAngle, Direction axis, float distance) {
		return new(Angle.Interpolate(startAngle, endAngle, distance), axis);
	}

	public Rotation Clamp(Rotation min, Rotation max) {
		var (minAngle, minAxis) = min;
		var (maxAngle, maxAxis) = max;
		return new(Angle.Clamp(minAngle, maxAngle), Axis.Clamp(minAxis, maxAxis));
	}


	public static Rotation CreateNewRandom() {
		return FromQuaternion(new(
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive()
		));
	}
	public static Rotation CreateNewRandom(Rotation minInclusive, Rotation maxExclusive) {
		var difference = minInclusive.Minus(maxExclusive);
		return minInclusive + difference.ScaledBy(RandomUtils.NextSingle());
	}

	static Vector4 Rotate(Quaternion q, Vector4 v) {
		var quatVec = new Vector3(q.X, q.Y, q.Z);
		var targetVec = new Vector3(v.X, v.Y, v.Z);
		var t = Vector3.Cross(quatVec, targetVec) * 2f;
		return new Vector4(
			targetVec + q.W * t + Vector3.Cross(quatVec, t),
			v.W
		);
	}
}