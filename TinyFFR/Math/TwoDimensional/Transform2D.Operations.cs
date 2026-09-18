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

	/// <inheritdoc/>
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

	/// <summary>
	/// Applies this transform to <paramref name="transformable"/>; equivalent to <c>transformable.TransformedBy(this)</c>.
	/// </summary>
	/// <typeparam name="T">The type of the value to transform.</typeparam>
	/// <param name="transformable">The value to transform.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T AppliedTo<T>(T transformable) where T : ITransformable2D<T> => transformable.TransformedBy(this);
	/// <summary>
	/// Applies the inverse of this transform to <paramref name="transformable"/>; equivalent to <c>transformable.TransformedByInverseOf(this)</c>.
	/// </summary>
	/// <typeparam name="T">The type of the value to transform.</typeparam>
	/// <param name="transformable">The value to transform.</param>
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
	/// <summary>
	/// Combines <paramref name="left"/> and <paramref name="right"/> into a single transform; equivalent to <c>left.TransformedBy(right)</c>.
	/// </summary>
	/// <param name="left">The transform to apply first.</param>
	/// <param name="right">The transform to apply on top of <paramref name="left"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D operator *(Transform2D left, Transform2D right) => left.TransformedBy(right);
	static Transform2D ITransformable2D<Transform2D>.operator *(Transform2D left, Transform2D right) => left.TransformedBy(right);
	/// <summary>
	/// Combines this transform with <paramref name="transform"/>, applying <paramref name="transform"/> on top of this one.
	/// </summary>
	/// <param name="transform">The transform to apply on top of this one.</param>
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
	/// <summary>
	/// Combines this transform with the inverse of <paramref name="transform"/>, applying that inverse on top of this one.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied on top of this one.</param>
	public Transform2D TransformedByInverseOf(Transform2D transform) {
		return ToMatrix() * MathUtils.ForceInvertMatrix(transform.ToMatrix());
	}
	#endregion

	#region Scaling
	/// <summary>
	/// Returns this transform with <paramref name="scalar"/> added to both axes of <see cref="Scaling"/>.
	/// </summary>
	/// <param name="scalar">The amount to add to <see cref="Scaling"/>'s X and Y components.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithScalingAdjustedBy(float scalar) => this with { Scaling = Scaling + new XYPair<float>(scalar) };
	/// <summary>
	/// Returns this transform with <paramref name="vect"/> added to <see cref="Scaling"/>, independently per axis.
	/// </summary>
	/// <param name="vect">The per-axis amounts to add to <see cref="Scaling"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithScalingAdjustedBy(XYPair<float> vect) => this with { Scaling = Scaling + vect };

	/// <summary>
	/// Returns this transform with <see cref="Scaling"/> multiplied uniformly by <paramref name="scalar"/>.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithScalingMultipliedBy(float scalar) => this with { Scaling = Scaling * scalar };
	/// <summary>
	/// Returns this transform with <see cref="Scaling"/> multiplied independently per axis by <paramref name="vect"/>'s corresponding component.
	/// </summary>
	/// <param name="vect">The per-axis scale factors.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithScalingMultipliedBy(XYPair<float> vect) => this with { Scaling = Scaling * vect };
	#endregion

	#region Rotation
	/// <summary>
	/// Returns this transform with <paramref name="rotation"/> added to <see cref="Rotation"/>.
	/// </summary>
	/// <remarks>
	/// A positive <paramref name="rotation"/> turns anticlockwise, matching <see cref="Rotation"/>'s own convention.
	/// </remarks>
	/// <param name="rotation">The additional rotation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithAdditionalRotation(Angle rotation) => this with { Rotation = Rotation + rotation };
	#endregion

	#region Translation
	/// <summary>
	/// Returns this transform with <paramref name="translation"/> added to <see cref="Translation"/>.
	/// </summary>
	/// <param name="translation">The additional translation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform2D WithAdditionalTranslation(XYPair<float> translation) => this with { Translation = Translation + translation };
	#endregion

	#region Clamping and Interpolation
	/// <inheritdoc/>
	public static Transform2D Interpolate(Transform2D start, Transform2D end, float distance) {
		CoerceToComponentRepresentation(ref start);
		CoerceToComponentRepresentation(ref end);
		return new(
			XYPair<float>.Interpolate(start.Translation, end.Translation, distance),
			Angle.Interpolate(start.Rotation, end.Rotation, distance),
			XYPair<float>.Interpolate(start.Scaling, end.Scaling, distance)
		);
	}

	/// <inheritdoc/>
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
