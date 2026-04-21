// Created on 2026-04-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

readonly record struct CameraEffectStrengthMap(float None, float VeryMild, float Mild, float Standard, float Strong, float VeryStrong) {
	public float From(Strength s) {
		return s switch {
			Strength.None => None,
			Strength.VeryMild => VeryMild,
			Strength.Mild => Mild,
			Strength.Strong => Strong,
			Strength.VeryStrong => VeryStrong,
			_ => Standard
		};
	}
	
	public Strength From(float f) {
		var dist = Single.MaxValue;
		var result = Strength.Standard;
		ReadOnlySpan<float> values = stackalloc float[] { None, VeryMild, Mild, Standard, Strong, VeryStrong };
		for (var i = 0; i < 6; ++i) {
			var thisValueDist = MathF.Abs(values[i] - f);
			if (thisValueDist < dist) {
				result = (Strength) i;
				dist = thisValueDist;
			}
		}
		return result;
	}
}