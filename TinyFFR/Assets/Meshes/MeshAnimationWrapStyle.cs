using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public enum MeshAnimationWrapStyle {
	Once,
	OncePingPonged,
	Loop,
	LoopPingPonged,
}

public static class MeshAnimationWrapStyleExtensions {
	public static float ApplyToTimePoint(this MeshAnimationWrapStyle @this, float nonWrappedTimePoint, float animationDefaultDuration) {
		return @this switch {
			MeshAnimationWrapStyle.Loop => MathUtils.TrueModulus(nonWrappedTimePoint, animationDefaultDuration),
			MeshAnimationWrapStyle.LoopPingPonged => Angle.FromRadians(nonWrappedTimePoint).TriangularizeRectified(Angle.FromRadians(animationDefaultDuration)).Radians,
			MeshAnimationWrapStyle.OncePingPonged => Angle.FromRadians(((Real) nonWrappedTimePoint).Clamp(0f, animationDefaultDuration * 2f)).TriangularizeRectified(Angle.FromRadians(animationDefaultDuration)).Radians,
			MeshAnimationWrapStyle.Once => ((Real) nonWrappedTimePoint).Clamp(0f, animationDefaultDuration),
			_ => nonWrappedTimePoint
		};
	}
}