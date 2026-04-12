// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct Transform2D :
	ITransformable2D<Transform2D>,
	IPhysicalValidityDeterminable,
	IInterpolatable<Transform2D>,
	IMultiplicativeIdentity<Transform2D, Transform2D> {

	static Transform2D IMultiplicativeIdentity<Transform2D, Transform2D>.MultiplicativeIdentity => None;

	public bool IsPhysicallyValid {
		get {
			var componentCopy = this;
			CoerceToComponentRepresentation(ref componentCopy);
			return Single.IsFinite(componentCopy.Translation.X)
				&& Single.IsFinite(componentCopy.Translation.Y)
				&& Single.IsFinite(componentCopy.Rotation.Radians)
				&& componentCopy.Scaling.X.IsPositiveAndFinite()
				&& componentCopy.Scaling.Y.IsPositiveAndFinite();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T AppliedTo<T>(T transformable) where T : ITransformable2D<T> => transformable.TransformedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T InverseAppliedTo<T>(T transformable) where T : ITransformable2D<T> => transformable.TransformedByInverseOf(this);

	#region Transform
	static Transform2D IMultiplyOperators<Transform2D, float, Transform2D>.operator *(Transform2D left, float right) => left * FromScalingOnly(right);
	static Transform2D IDivisionOperators<Transform2D, float, Transform2D>.operator /(Transform2D left, float right) => left * FromScalingOnly(1f / right);
	static Transform2D IMultiplicative<Transform2D, float, Transform2D>.operator *(float left, Transform2D right) => right * FromScalingOnly(left);
	Transform2D IScalable<Transform2D>.ScaledBy(float scalar) => this * FromScalingOnly(scalar);
	Transform2D IIndependentAxisScalable2D<Transform2D>.ScaledBy(XYPair<float> vect) => this * FromScalingOnly(vect);
	Transform2D IRotatable2D<Transform2D>.RotatedBy(Angle rot) => this * FromRotationOnly(rot);
	Transform2D ITranslatable2D<Transform2D>.MovedBy(XYPair<float> v) => this * FromTranslationOnly(v);

	static Transform2D IMultiplyOperators<Transform2D, Transform2D, Transform2D>.operator *(Transform2D left, Transform2D right) => left.TransformedBy(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D operator *(Transform2D left, Transform2D right) => left.TransformedBy(right);
	static Transform2D ITransformable2D<Transform2D>.operator *(Transform2D left, Transform2D right) => left.TransformedBy(right);
	public Transform2D TransformedBy(Transform2D transform) {
		var canDoSimpleTranslationModification =
			!IsInternallyRepresentedByMatrix &&
			!transform.IsInternallyRepresentedByMatrix &&
			transform.Scaling == XYPair<float>.One &&
			transform.Rotation == Angle.Zero;

		return canDoSimpleTranslationModification
			? WithAdditionalTranslation(transform.Translation)
			: ToMatrix() * transform.ToMatrix();
	}
	public Transform2D TransformedByInverseOf(Transform2D transform) {
		return ToMatrix() * MathUtils.ForceInvertMatrix(transform.ToMatrix());
	}
	#endregion

	#region Scaling
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithAdditionalRotation(Angle rotation) => this with { Rotation = Rotation + rotation };
	#endregion

	#region Translation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithAdditionalTranslation(XYPair<float> translation) => this with { Translation = Translation + translation };
	#endregion

	#region Clamping and Interpolation
	public static Transform2D Interpolate(Transform2D start, Transform2D end, float distance) {
		CoerceToComponentRepresentation(ref start);
		CoerceToComponentRepresentation(ref end);
		return new(
			XYPair<float>.Interpolate(start.Translation, end.Translation, distance),
			Angle.Interpolate(start.Rotation, end.Rotation, distance),
			XYPair<float>.Interpolate(start.Scaling, end.Scaling, distance)
		);
	}

	public Transform2D Clamp(Transform2D min, Transform2D max) {
		CoerceToComponentRepresentation(ref min);
		CoerceToComponentRepresentation(ref max);
		return new(
			Translation.Clamp(min.Translation, max.Translation),
			Rotation.Clamp(min.Rotation, max.Rotation),
			Scaling.Clamp(min.Scaling, max.Scaling)
		);
	}
	#endregion
}
