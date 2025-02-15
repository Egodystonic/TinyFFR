// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct Transform : 
	IInterpolatable<Transform>,
	IMultiplicativeInvertible<Transform>,
	IMultiplicativeIdentity<Transform, Transform> {
	
	Transform? IMultiplicativeInvertible<Transform>.Reciprocal => Inverse;
	public Transform Inverse => new(-Translation, -Rotation, Scaling.Reciprocal ?? Vect.Zero);
	static Transform IMultiplicativeIdentity<Transform, Transform>.MultiplicativeIdentity => None;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T AppliedTo<T>(T transformable) where T : ITransformable<T> => transformable.TransformedBy(this);

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
	public Transform WithAdditionalRotation(Rotation rotation) => this with { Rotation = Rotation + rotation };
	#endregion

	#region Translation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Transform WithAdditionalTranslation(Vect translation) => this with { Translation = Translation + translation };
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