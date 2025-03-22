// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static System.Numerics.Quaternion;
using static Egodystonic.TinyFFR.MathUtils;

namespace Egodystonic.TinyFFR;

partial struct Rotation : 
	IAlgebraicGroup<Rotation>,
	IAngleMeasurable<Rotation, Rotation>,
	IScalable<Rotation>,
	IPrecomputationInterpolatable<Rotation, Pair<Quaternion, Quaternion>> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator -(Rotation operand) => operand.Reversed;
	public Rotation Reversed => new(-Angle, Axis);
	Rotation IInvertible<Rotation>.Inverted => Reversed;
	static Rotation IAdditiveIdentity<Rotation, Rotation>.AdditiveIdentity => None;

	// TODO xmldoc that this returns a rotation whose angle will always be in the range [0..<180], but that is equivalent for all directions to this rotation
	// TODO xmldoc In the range [180..<360] it flips the axis and then [360..<540] the axis stays the same, and so on
	public Rotation Normalized {
		get {
			var normalizedAngle = Angle.Normalized;
			return normalizedAngle < Angle.HalfCircle
				? new Rotation(normalizedAngle, Axis)
				: new Rotation(Angle.FullCircle - normalizedAngle, -Axis);
		}
	}

	#region Scaling and Addition/Subtraction
	static Rotation IAdditive<Rotation, Rotation, Rotation>.operator +(Rotation lhs, Rotation rhs) => lhs.CombinedAndNormalizedWith(rhs);
	static Rotation IAdditionOperators<Rotation, Rotation, Rotation>.operator +(Rotation lhs, Rotation rhs) => lhs.CombinedAndNormalizedWith(rhs);
	static Rotation ISubtractionOperators<Rotation, Rotation, Rotation>.operator -(Rotation lhs, Rotation rhs) => lhs.NormalizedDifferenceTo(rhs);
	Rotation IAdditive<Rotation, Rotation, Rotation>.Plus(Rotation other) => CombinedAndNormalizedWith(other);
	Rotation IAdditive<Rotation, Rotation, Rotation>.Minus(Rotation other) => NormalizedDifferenceTo(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation NormalizedDifferenceTo(Rotation other) => CombineAndNormalize(other, Reversed);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation CombinedAndNormalizedWith(Rotation other) => CombineAndNormalize(this, other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation CombineAndNormalize(Rotation initial, Rotation following) => FromQuaternionPreNormalized(CombineAndNormalize(initial.ToQuaternion(), following.ToQuaternion()));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion CombineAndNormalize(Quaternion initial, Quaternion following) => NormalizeOrIdentity(following * initial);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion Combine(Quaternion initial, Quaternion following) => following * initial;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator *(Rotation rotation, float scalar) => rotation.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator *(float scalar, Rotation rotation) => rotation.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator /(Rotation rotation, float scalar) => rotation.ScaledBy(1f / scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation ScaledBy(float scalar) => new(Angle * scalar, Axis);

	public static Quaternion ScaleQuaternion(Quaternion q, float scalar) {
		// Quaternion exponentiation
		var halfAngleRadians = MathF.Acos(q.W);
		var newHalfAngleRadians = halfAngleRadians * scalar;
		var (sinNewHalfAngle, cosNewHalfAngle) = MathF.SinCos(newHalfAngleRadians);
		
		var normalizedVectorComponent = Vector3.Normalize(new(q.X, q.Y, q.Z));
		if (!Single.IsFinite(normalizedVectorComponent.X)) return Identity;
		
		return NormalizeOrIdentity(new(
			normalizedVectorComponent * sinNewHalfAngle,
			cosNewHalfAngle
		));
	}
	#endregion

	#region Interactions w/ Rotation
	public static Angle operator ^(Rotation left, Rotation right) => left.NormalizedAngleTo(right);
	Angle IAngleMeasurable<Rotation>.AngleTo(Rotation other) => NormalizedDifferenceTo(other).Angle;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle NormalizedAngleTo(Rotation other) => NormalizedDifferenceTo(other).Angle;
	#endregion

	#region Rotation
	static Vector4 Rotate(Quaternion q, Vector4 v) {
		var quatVec = new Vector3(q.X, q.Y, q.Z);
		var targetVec = new Vector3(v.X, v.Y, v.Z);
		var t = Vector3.Cross(quatVec, targetVec) * 2f;
		return new Vector4(
			targetVec + q.W * t + Vector3.Cross(quatVec, t),
			v.W
		);
	}

	// Renormalize because we can accrue a lot of error here and we're doing a heavy operation anyway. Offer RotateWithoutRenormalizing for perf sensitive cases
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction Rotate(Direction d) => Rotate(d, ToQuaternion());
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction RotateWithoutRenormalizing(Direction d) => RotateWithoutRenormalizing(d, ToQuaternion());
	public static Direction Rotate(Direction d, Quaternion q) => Direction.Renormalize(RotateWithoutRenormalizing(d, q));
	public static Direction RotateWithoutRenormalizing(Direction d, Quaternion q) => new(Rotate(q, d.AsVector4));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Rotate(Vect v) => Rotate(v, ToQuaternion());
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect RotateWithoutCorrectingLength(Vect v) => RotateWithoutCorrectingLength(v, ToQuaternion());
	public static Vect Rotate(Vect v, Quaternion q) => RotateWithoutCorrectingLength(v, q).WithLength(v.Length);
	public static Vect RotateWithoutCorrectingLength(Vect v, Quaternion q) => new(Rotate(q, v.AsVector4));

	public Angle AngleAroundAxis(Direction axis) {
		var orthogonalVect = axis.AnyOrthogonal();
		return orthogonalVect.SignedAngleTo(orthogonalVect * this, axis);
	}
	#endregion

	#region Clamping and Interpolation
	public static Rotation Interpolate(Rotation start, Rotation end, float distance) => FromQuaternion(Interpolate(start.ToQuaternion(), end.ToQuaternion(), distance));
	public static Quaternion Interpolate(Quaternion start, Quaternion end, float distance) {
		const float CosPhiMinForLinearRenormalization = 1f - 1E-3f;
		return MathF.Abs(Dot(start, end)) > CosPhiMinForLinearRenormalization
			? ApproximatelyInterpolate(start, end, distance)
			: AccuratelyInterpolate(start, end, distance);
	}

	public static Rotation AccuratelyInterpolate(Rotation start, Rotation end, float distance) => FromQuaternion(AccuratelyInterpolate(start.ToQuaternion(), end.ToQuaternion(), distance));
	public static Quaternion AccuratelyInterpolate(Quaternion start, Quaternion end, float distance) { // Quaternion slerp
		return Slerp(start, end, distance);
	}

	public static Rotation ApproximatelyInterpolate(Rotation start, Rotation end, float distance) => FromQuaternion(ApproximatelyInterpolate(start.ToQuaternion(), end.ToQuaternion(), distance));
	public static Quaternion ApproximatelyInterpolate(Quaternion start, Quaternion end, float distance) { // Vector lerp
		return start + (end - start) * distance;
	}

	public static Pair<Quaternion, Quaternion> CreateInterpolationPrecomputation(Rotation start, Rotation end) => new(start.ToQuaternion(), end.ToQuaternion());

	public static Rotation InterpolateUsingPrecomputation(Rotation start, Rotation end, Pair<Quaternion, Quaternion> precomputation, float distance) {
		return FromQuaternion(Interpolate(precomputation.First, precomputation.Second, distance));
	}

	public static Rotation Interpolate(Angle startAngle, Angle endAngle, Direction axis, float distance) {
		return new(Angle.Interpolate(startAngle, endAngle, distance), axis);
	}

	// TODO in xmldoc explain that this is an esoteric function that you almost always don't actually want to use (are you sure you don't want to clamp between two directions?)
	// TODO in xmldoc explain that this function breaks the rotation in to its constituent parts (angle + axis) and clamps on those separately
	public Rotation Clamp(Rotation min, Rotation max) {
		if (this == None || min == None || max == None) return this;

		min = min.Normalized;
		max = max.Normalized;

		var (minAngle, minAxis) = min;
		var (maxAngle, maxAxis) = max;
		return new(Angle.Clamp(minAngle, maxAngle), Axis.Clamp(minAxis, maxAxis));
	}
	#endregion
}