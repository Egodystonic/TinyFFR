// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct Transform : 
	ITransformable<Transform>,
	IPhysicalValidityDeterminable,
	IInterpolatable<Transform>,
	IMultiplicativeIdentity<Transform, Transform> {
	static Transform IMultiplicativeIdentity<Transform, Transform>.MultiplicativeIdentity => None;

	public bool IsPhysicallyValid {
		get {
			var componentCopy = this;
			CoerceToComponentRepresentation(ref componentCopy);
			return componentCopy.Translation.IsPhysicallyValid
				&& componentCopy.Rotation.IsPhysicallyValid
				&& componentCopy.Scaling.X.IsPositiveAndFinite()
				&& componentCopy.Scaling.Y.IsPositiveAndFinite()
				&& componentCopy.Scaling.Z.IsPositiveAndFinite();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T AppliedTo<T>(T transformable) where T : ITransformable<T> => transformable.TransformedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T InverseAppliedTo<T>(T transformable) where T : ITransformable<T> => transformable.TransformedByInverseOf(this);
	
	#region Transform
	static Transform IMultiplyOperators<Transform, float, Transform>.operator *(Transform left, float right) => left * FromScalingOnly(right);
	static Transform IDivisionOperators<Transform, float, Transform>.operator /(Transform left, float right) => left * FromScalingOnly(1f / right);
	static Transform IMultiplicative<Transform, float, Transform>.operator *(float left, Transform right) => right * FromScalingOnly(left);
	Transform IScalable<Transform>.ScaledBy(float scalar) => this * FromScalingOnly(scalar);
	Transform IIndependentAxisScalable<Transform>.ScaledBy(Vect vect) => this * FromScalingOnly(vect);

	static Transform IMultiplyOperators<Transform, Rotation, Transform>.operator *(Transform left, Rotation right) => left * FromRotationOnly(right);
	static Transform IRotatable<Transform>.operator *(Rotation left, Transform right) => right * FromRotationOnly(left);
	Transform IRotatable<Transform>.RotatedBy(Rotation rot) => this * FromRotationOnly(rot);

	static Transform IAdditionOperators<Transform, Vect, Transform>.operator +(Transform left, Vect right) => left * FromTranslationOnly(right);
	static Transform ISubtractionOperators<Transform, Vect, Transform>.operator -(Transform left, Vect right) => left * FromTranslationOnly(-right);
	static Transform IAdditive<Transform, Vect, Transform>.operator +(Vect left, Transform right) => right * FromTranslationOnly(left);
	Transform ITranslatable<Transform>.MovedBy(Vect v) => this * FromTranslationOnly(v);
	
	static Transform IMultiplyOperators<Transform, Transform, Transform>.operator *(Transform left, Transform right) => left * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform operator *(Transform left, Transform right) => left.TransformedBy(right);
	public Transform TransformedBy(Transform transform) {
		var canDoSimpleTranslationModification = 
			!IsInternallyRepresentedByMatrix && 
			!transform.IsInternallyRepresentedByMatrix &&
			transform.Scaling == Vect.One && 
			transform.Rotation == Rotation.None;
		
		return canDoSimpleTranslationModification
			? WithAdditionalTranslation(transform.Translation)
			: ToMatrix() * transform.ToMatrix();
	}
	public Transform TransformedByInverseOf(Transform transform) {
		return ToMatrix() * MathUtils.ForceInvertMatrix(transform.ToMatrix());
	}
	#endregion

	#region Scaling
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithAdditionalRotation(Rotation rotation) => WithAdditionalRotation(rotation.ToQuaternion());
	public Transform WithAdditionalRotation(Quaternion rotationQuaternion) => this with { RotationQuaternion = Rotation.CombineAndNormalize(RotationQuaternion, rotationQuaternion) };
	#endregion

	#region Translation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithAdditionalTranslation(Vect translation) => this with { Translation = Translation + translation };
	#endregion

	#region Clamping and Interpolation
	public static Transform Interpolate(Transform start, Transform end, float distance) {
		CoerceToComponentRepresentation(ref start);
		CoerceToComponentRepresentation(ref end);
		return new(
			Vect.Interpolate(start.Translation, end.Translation, distance),
			Rotation.Interpolate(start.RotationQuaternion, end.RotationQuaternion, distance),
			Vect.Interpolate(start.Scaling, end.Scaling, distance)
		);
	}

	public Transform Clamp(Transform min, Transform max) {
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