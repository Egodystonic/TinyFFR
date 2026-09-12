using System;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Enumeration of different techniques that can be applied to animations (e.g. skeletal animations, pre-programmed camera moves, etc)
/// when they reach their final keyframe.
/// </summary>
public enum AnimationWrapStyle {
	/// <summary>
	/// Indicates the animation should be played once and only once; and should simply stop/end when reaching the final keyframe.
	/// </summary>
	Once,
	/// <summary>
	/// Indicates the animation should be played once until it reaches its ending keyframe
	/// and again in reverse all the way back to the start; stopping when returning to the start.
	/// </summary>
	OncePingPonged,
	/// <summary>
	/// Indicates the animation should snap back to its start keyframe every time it reaches its end; replaying from start to end again and again indefinitely.
	/// </summary>
	Loop,
	/// <summary>
	/// Indicates the animation should play in reverse when it reaches its end keyframe until it rewinds all the way back to the start;
	/// and then should begin moving forwards again; repeating this entire motion again and again indefinitely.
	/// </summary>
	LoopPingPonged,
}

/// <summary>
/// A static class holding extension methods for <see cref="AnimationWrapStyle"/>.
/// </summary>
public static class AnimationWrapStyleExtensions {
	/// <summary>
	/// Uses this wrap style to convert an elapsed time to a timepoint in an animation track.
	/// </summary>
	/// <param name="this">The wrap style to apply.</param>
	/// <param name="nonWrappedTimePoint">The unaltered, original timepoint (i.e. the amount of time elapsed in total playing this animation).</param>
	/// <param name="animationDefaultDuration">The amount of time it takes for the animation to play once from start to finish.</param>
	/// <returns>A new timepoint t, such that <c>0 &lt;= t &lt; animationDefaultDuration</c>, that correctly specifies which timepoint
	/// in the original animation should be selected according to <paramref name="this"/> wrap style and the current <paramref name="nonWrappedTimePoint"/>.</returns>
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