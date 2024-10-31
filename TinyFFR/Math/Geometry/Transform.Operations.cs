// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct Transform : 
	IInterpolatable<Transform> {
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