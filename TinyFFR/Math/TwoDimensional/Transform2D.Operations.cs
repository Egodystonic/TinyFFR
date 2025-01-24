// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct Transform2D : 
	IInterpolatable<Transform2D>,
	IMultiplicativeInvertible<Transform2D>,
	IMultiplicativeIdentity<Transform2D, Transform2D> {

	Transform2D? IMultiplicativeInvertible<Transform2D>.Reciprocal => Inverse;
	public Transform2D Inverse => new(-Translation, -Rotation, Scaling.Reciprocal ?? XYPair<float>.Zero);
	static Transform2D IMultiplicativeIdentity<Transform2D, Transform2D>.MultiplicativeIdentity => None;

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