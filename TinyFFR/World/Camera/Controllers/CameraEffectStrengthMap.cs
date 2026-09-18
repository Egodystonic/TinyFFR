// Created on 2026-04-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

readonly record struct CameraEffectStrengthMap(float None, float VeryMild, float Mild, float Standard, float Strong, float VeryStrong) {
	public float From(SmoothingStrength s) {
		return s switch {
			SmoothingStrength.None => None,
			SmoothingStrength.VeryMild => VeryMild,
			SmoothingStrength.Mild => Mild,
			SmoothingStrength.Strong => Strong,
			SmoothingStrength.VeryStrong => VeryStrong,
			_ => Standard
		};
	}
	
	public SmoothingStrength From(float f) {
		var dist = Single.MaxValue;
		var result = SmoothingStrength.Moderate;
		ReadOnlySpan<float> values = stackalloc float[] { None, VeryMild, Mild, Standard, Strong, VeryStrong };
		for (var i = 0; i < 6; ++i) {
			var thisValueDist = MathF.Abs(values[i] - f);
			if (thisValueDist < dist) {
				result = (SmoothingStrength) i;
				dist = thisValueDist;
			}
		}
		return result;
	}
}