// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct Transform2D : 
	IInterpolatable<Transform2D>,
	ITransformable2D<Transform2D>,
	IMultiplicativeInvertible<Transform2D>,
	IMultiplicativeIdentity<Transform2D, Transform2D> {

	Transform2D? IMultiplicativeInvertible<Transform2D>.Reciprocal => Inverse;
	public Transform2D Inverse => new(-Translation, -Rotation, Scaling.Reciprocal ?? XYPair<float>.Zero);
	static Transform2D IMultiplicativeIdentity<Transform2D, Transform2D>.MultiplicativeIdentity => None;

	#region Scaling
	static Transform2D IMultiplyOperators<Transform2D, float, Transform2D>.operator *(Transform2D left, float right) => left.WithScalingMultipliedBy(right);
	static Transform2D IDivisionOperators<Transform2D, float, Transform2D>.operator /(Transform2D left, float right) => left.WithScalingMultipliedBy(1f / right);
	static Transform2D IMultiplicative<Transform2D, float, Transform2D>.operator *(float left, Transform2D right) => right.WithScalingMultipliedBy(left);
	Transform2D IScalable<Transform2D>.ScaledBy(float scalar) => WithScalingMultipliedBy(scalar);
	Transform2D IIndependentAxisScalable2D<Transform2D>.ScaledBy(XYPair<float> vect) => WithScalingMultipliedBy(vect);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithScalingAdjustedBy(float scalar) => this with { Scaling = Scaling + new XYPair<float>(scalar) };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithScalingAdjustedBy(XYPair<float> vect) => this with { Scaling = Scaling + vect };

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithScalingMultipliedBy(float scalar) => this with { Scaling = Scaling * scalar };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithScalingMultipliedBy(XYPair<float> vect) => this with { Scaling = Scaling * vect };
	#endregion

	#region Rotation
	static Transform2D IMultiplyOperators<Transform2D, Angle, Transform2D>.operator *(Transform2D left, Angle right) => left.WithAdditionalRotation(right);
	static Transform2D IRotatable2D<Transform2D>.operator *(Angle left, Transform2D right) => right.WithAdditionalRotation(left);
	Transform2D IRotatable2D<Transform2D>.RotatedBy(Angle rot) => WithAdditionalRotation(rot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithAdditionalRotation(Angle rotation) => this with { Rotation = Rotation + rotation };
	#endregion

	#region Translation
	static Transform2D IAdditionOperators<Transform2D, XYPair<float>, Transform2D>.operator +(Transform2D left, XYPair<float> right) => left.WithAdditionalTranslation(right);
	static Transform2D ISubtractionOperators<Transform2D, XYPair<float>, Transform2D>.operator -(Transform2D left, XYPair<float> right) => left.WithAdditionalTranslation(-right);
	static Transform2D IAdditive<Transform2D, XYPair<float>, Transform2D>.operator +(XYPair<float> left, Transform2D right) => right.WithAdditionalTranslation(left);
	Transform2D ITranslatable2D<Transform2D>.MovedBy(XYPair<float> v) => WithAdditionalTranslation(v);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithAdditionalTranslation(XYPair<float> translation) => this with { Translation = Translation + translation };
	#endregion

	#region Transformation
	static Transform2D IMultiplyOperators<Transform2D, Transform2D, Transform2D>.operator *(Transform2D left, Transform2D right) => left.WithComponentsCombinedWith(right);
	static Transform2D ITransformable2D<Transform2D>.operator *(Transform2D left, Transform2D right) => left.WithComponentsCombinedWith(right);
	Transform2D ITransformable2D<Transform2D>.TransformedBy(Transform2D transform) => WithComponentsCombinedWith(transform);

	public Transform2D WithComponentsCombinedWith(Transform2D transform) {
		return new(
			Translation + transform.Translation,
			Rotation + transform.Rotation,
			Scaling * transform.Scaling
		);
	}
	#endregion

	#region Clamping and Interpolation
	public static Transform2D Interpolate(Transform2D start, Transform2D end, float distance) {
		return new(
			XYPair<float>.Interpolate(start.Translation, end.Translation, distance),
			Angle.Interpolate(start.Rotation, end.Rotation, distance),
			XYPair<float>.Interpolate(start.Scaling, end.Scaling, distance)
		);
	}

	public Transform2D Clamp(Transform2D min, Transform2D max) {
		return new(
			Translation.Clamp(min.Translation, max.Translation),
			Rotation.Clamp(min.Rotation, max.Rotation),
			Scaling.Clamp(min.Scaling, max.Scaling)
		);
	}
	#endregion
}