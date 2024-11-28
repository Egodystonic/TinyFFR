// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct Transform : 
	IInterpolatable<Transform>,
	ITransformable<Transform>,
	IMultiplicativeInvertible<Transform>,
	IMultiplicativeIdentity<Transform, Transform> {
	
	Transform? IMultiplicativeInvertible<Transform>.Reciprocal => Inverse;
	public Transform Inverse => new(-Translation, -Rotation, Scaling.Reciprocal ?? Vect.Zero);
	static Transform IMultiplicativeIdentity<Transform, Transform>.MultiplicativeIdentity => None;

	#region Scaling
	static Transform IMultiplyOperators<Transform, float, Transform>.operator *(Transform left, float right) => left.WithScalingMultipliedBy(right);
	static Transform IDivisionOperators<Transform, float, Transform>.operator /(Transform left, float right) => left.WithScalingMultipliedBy(1f / right);
	static Transform IMultiplicative<Transform, float, Transform>.operator *(float left, Transform right) => right.WithScalingMultipliedBy(left);
	Transform IScalable<Transform>.ScaledBy(float scalar) => WithScalingMultipliedBy(scalar);
	Transform IIndependentAxisScalable<Transform>.ScaledBy(Vect vect) => WithScalingMultipliedBy(vect);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithScalingAdjustedBy(float scalar) => this with { Scaling = Scaling + new Vect(scalar) };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithScalingAdjustedBy(Vect vect) => this with { Scaling = Scaling + vect };

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithScalingMultipliedBy(float scalar) => this with { Scaling = Scaling * scalar };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithScalingMultipliedBy(Vect vect) => this with { Scaling = Scaling * vect };
	#endregion

	#region Rotation
	static Transform IMultiplyOperators<Transform, Rotation, Transform>.operator *(Transform left, Rotation right) => left.WithAdditionalRotation(right);
	static Transform IRotatable<Transform>.operator *(Rotation left, Transform right) => right.WithAdditionalRotation(left);
	Transform IRotatable<Transform>.RotatedBy(Rotation rot) => WithAdditionalRotation(rot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithAdditionalRotation(Rotation rotation) => this with { Rotation = Rotation + rotation };
	#endregion

	#region Translation
	static Transform IAdditionOperators<Transform, Vect, Transform>.operator +(Transform left, Vect right) => left.WithAdditionalTranslation(right);
	static Transform ISubtractionOperators<Transform, Vect, Transform>.operator -(Transform left, Vect right) => left.WithAdditionalTranslation(-right);
	static Transform IAdditive<Transform, Vect, Transform>.operator +(Vect left, Transform right) => right.WithAdditionalTranslation(left);
	Transform ITranslatable<Transform>.MovedBy(Vect v) => WithAdditionalTranslation(v);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithAdditionalTranslation(Vect translation) => this with { Translation = Translation + translation };
	#endregion

	#region Transformation
	static Transform IMultiplyOperators<Transform, Transform, Transform>.operator *(Transform left, Transform right) => left.WithComponentsCombinedWith(right);
	static Transform ITransformable<Transform>.operator *(Transform left, Transform right) => left.WithComponentsCombinedWith(right);
	Transform ITransformable<Transform>.TransformedBy(Transform transform) => WithComponentsCombinedWith(transform);

	public Transform WithComponentsCombinedWith(Transform transform) {
		return new(
			Translation + transform.Translation,
			Rotation + transform.Rotation,
			Scaling * transform.Scaling
		);
	}
	#endregion

	#region Clamping and Interpolation
	public static Transform Interpolate(Transform start, Transform end, float distance) {
		return new(
			Vect.Interpolate(start.Translation, end.Translation, distance),
			Rotation.Interpolate(start.Rotation, end.Rotation, distance),
			Vect.Interpolate(start.Scaling, end.Scaling, distance)
		);
	}

	public Transform Clamp(Transform min, Transform max) {
		return new(
			Translation.Clamp(min.Translation, max.Translation),
			Rotation.Clamp(min.Rotation, max.Rotation),
			Scaling.Clamp(min.Scaling, max.Scaling)
		);
	}
	#endregion
}