using System;

namespace Egodystonic.TinyFFR;

public enum AnimationWrapStyle {
	Once,
	OncePingPonged,
	Loop,
	LoopPingPonged,
}

public static class AnimationWrapStyleExtensions {
	public static float ApplyToTimePoint(this AnimationWrapStyle @this, float nonWrappedTimePoint, float animationDefaultDuration) {
		return @this switch {
			AnimationWrapStyle.Loop => MathUtils.TrueModulus(nonWrappedTimePoint, animationDefaultDuration),
			AnimationWrapStyle.LoopPingPonged => Angle.FromRadians(nonWrappedTimePoint).TriangularizeRectified(Angle.FromRadians(animationDefaultDuration)).Radians,
			AnimationWrapStyle.OncePingPonged => Angle.FromRadians(((Real) nonWrappedTimePoint).Clamp(0f, animationDefaultDuration * 2f)).TriangularizeRectified(Angle.FromRadians(animationDefaultDuration)).Radians,
			AnimationWrapStyle.Once => ((Real) nonWrappedTimePoint).Clamp(0f, animationDefaultDuration),
			_ => nonWrappedTimePoint
		};
	}
}