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

	/// <summary>
	/// Determines whether this transform's <see cref="Translation"/>, <see cref="Rotation"/>, and <see cref="Scaling"/> are all physically valid.
	/// </summary>
	/// <remarks>
	/// This requires a finite, physically-valid translation and rotation, and a scaling whose components are all
	/// finite and strictly positive (a zero or negative scale is not considered valid).
	/// </remarks>
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

	/// <summary>
	/// Applies this transform to <paramref name="transformable"/>; equivalent to <c>transformable.TransformedBy(this)</c>.
	/// </summary>
	/// <typeparam name="T">The type being transformed.</typeparam>
	/// <param name="transformable">The value to transform.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T AppliedTo<T>(T transformable) where T : ITransformable<T> => transformable.TransformedBy(this);
	/// <summary>
	/// Applies the inverse of this transform to <paramref name="transformable"/>; equivalent to <c>transformable.TransformedByInverseOf(this)</c>.
	/// </summary>
	/// <typeparam name="T">The type being transformed.</typeparam>
	/// <param name="transformable">The value to transform.</param>
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
	Transform IRotatable<Transform>.RotatedBy(Quaternion rotQuat) => this * FromRotationOnly(rotQuat);

	static Transform IAdditionOperators<Transform, Vect, Transform>.operator +(Transform left, Vect right) => left * FromTranslationOnly(right);
	static Transform ISubtractionOperators<Transform, Vect, Transform>.operator -(Transform left, Vect right) => left * FromTranslationOnly(-right);
	static Transform IAdditive<Transform, Vect, Transform>.operator +(Vect left, Transform right) => right * FromTranslationOnly(left);
	Transform ITranslatable<Transform>.MovedBy(Vect v) => this * FromTranslationOnly(v);
	
	static Transform IMultiplyOperators<Transform, Transform, Transform>.operator *(Transform left, Transform right) => left * right;
	/// <summary>
	/// Combines <paramref name="left"/> and <paramref name="right"/> in to a single transform; equivalent to <c>left.TransformedBy(right)</c>.
	/// </summary>
	/// <param name="left">The transform applied first.</param>
	/// <param name="right">The transform applied second.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform operator *(Transform left, Transform right) => left.TransformedBy(right);
	/// <summary>
	/// Returns the combined transform equivalent to applying this transform first, then <paramref name="transform"/>.
	/// </summary>
	/// <param name="transform">The transform to apply after this one.</param>
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
	/// <summary>
	/// Returns the combined transform equivalent to applying this transform first, then the inverse of <paramref name="transform"/>.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied after this one.</param>
	public Transform TransformedByInverseOf(Transform transform) {
		return ToMatrix() * MathUtils.ForceInvertMatrix(transform.ToMatrix());
	}
	#endregion

	#region Scaling
	/// <summary>
	/// Returns this transform with <paramref name="scalar"/> added uniformly to all three axes of its <see cref="Scaling"/>.
	/// </summary>
	/// <param name="scalar">The amount to add to each axis of the scaling.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithScalingAdjustedBy(float scalar) => this with { Scaling = Scaling + new Vect(scalar) };
	/// <summary>
	/// Returns this transform with <paramref name="vect"/> added independently per axis to its <see cref="Scaling"/>.
	/// </summary>
	/// <param name="vect">The per-axis amounts to add to the scaling.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithScalingAdjustedBy(Vect vect) => this with { Scaling = Scaling + vect };

	/// <summary>
	/// Returns this transform with its <see cref="Scaling"/> multiplied uniformly by <paramref name="scalar"/>.
	/// </summary>
	/// <param name="scalar">The scale factor to apply to each axis of the scaling.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithScalingMultipliedBy(float scalar) => this with { Scaling = Scaling * scalar };
	/// <summary>
	/// Returns this transform with its <see cref="Scaling"/> multiplied independently per axis by <paramref name="vect"/>.
	/// </summary>
	/// <param name="vect">The per-axis scale factors to apply to the scaling.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithScalingMultipliedBy(Vect vect) => this with { Scaling = Scaling * vect };
	#endregion

	#region Rotation
	/// <summary>
	/// Returns this transform with <paramref name="rotation"/> combined in to its <see cref="Rotation"/>, applied after the existing rotation.
	/// </summary>
	/// <param name="rotation">The rotation to combine.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithAdditionalRotation(Rotation rotation) => WithAdditionalRotation(rotation.ToQuaternion());
	/// <summary>
	/// Returns this transform with <paramref name="rotationQuaternion"/> combined in to its <see cref="Rotation"/>, applied after the existing rotation.
	/// </summary>
	/// <param name="rotationQuaternion">The rotation, as a raw <see cref="Quaternion"/>, to combine.</param>
	public Transform WithAdditionalRotation(Quaternion rotationQuaternion) => this with { RotationQuaternion = Rotation.CombineAndNormalize(RotationQuaternion, rotationQuaternion) };
	#endregion

	#region Translation
	/// <summary>
	/// Returns this transform with <paramref name="translation"/> added to its <see cref="Translation"/>.
	/// </summary>
	/// <param name="translation">The vector to add to the translation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithAdditionalTranslation(Vect translation) => this with { Translation = Translation + translation };
	#endregion

	#region Clamping and Interpolation
	/// <inheritdoc />
	/// <remarks>
	/// This interpolates the <see cref="Translation"/>, <see cref="Rotation"/>, and <see cref="Scaling"/> components
	/// independently and recombines them, coercing both inputs to component representation first if necessary (see <see cref="IsInternallyRepresentedByMatrix"/>).
	/// </remarks>
	public static Transform Interpolate(Transform start, Transform end, float distance) {
		CoerceToComponentRepresentation(ref start);
		CoerceToComponentRepresentation(ref end);
		return new(
			Vect.Interpolate(start.Translation, end.Translation, distance),
			Rotation.Interpolate(start.RotationQuaternion, end.RotationQuaternion, distance),
			Vect.Interpolate(start.Scaling, end.Scaling, distance)
		);
	}

	/// <summary>
	/// Clamps this transform's <see cref="Translation"/>, <see cref="Rotation"/>, and <see cref="Scaling"/> components independently between the corresponding components of <paramref name="min"/> and <paramref name="max"/>.
	/// </summary>
	/// <remarks>
	/// All three transforms are coerced to component representation first if necessary (see <see cref="IsInternallyRepresentedByMatrix"/>).
	/// See <see cref="Vect.Clamp"/> and <see cref="Egodystonic.TinyFFR.Rotation.Clamp"/> for how each component's clamp behaves.
	/// </remarks>
	/// <param name="min">The lower bound for each component.</param>
	/// <param name="max">The upper bound for each component.</param>
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