// Created on 2026-02-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// One moment in an animation at which a particular value is specified, with the rest filled in by interpolating between such moments.
/// </summary>
/// <typeparam name="T">The kind of value this keyframe holds.</typeparam>
public interface IAnimationKeyframe<T> : ITimeKeyedItem {
	/// <summary>
	/// The value a keyframe of this kind falls back to when an animation supplies none.
	/// </summary>
	static abstract T FallbackValue { get; }
	/// <summary>
	/// Interpolates between two of this kind of keyframe's values.
	/// </summary>
	/// <param name="start">The value at the earlier keyframe.</param>
	/// <param name="end">The value at the later keyframe.</param>
	/// <param name="distance">How far between the two, where <c>0f</c> is <paramref name="start"/> and <c>1f</c> is <paramref name="end"/>.</param>
	static abstract T InterpolateValues(T start, T end, float distance);
	/// <summary>
	/// The value this keyframe holds.
	/// </summary>
	T Value { get; }
}

/// <summary>
/// Specifies where one joint of a skeleton sits at one moment in an animation.
/// </summary>
/// <param name="TimeKeySeconds">When in the animation this keyframe takes effect, in seconds.</param>
/// <param name="Value">How far the joint is moved from its resting position at that moment.</param>
public readonly record struct SkeletalAnimationTranslationKeyframe(float TimeKeySeconds, Vect Value) : IAnimationKeyframe<Vect> {
	/// <summary>
	/// The value used when an animation supplies no translation keyframes: no translation at all.
	/// </summary>
	public static Vect FallbackValue { get; } = Vect.Zero;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect InterpolateValues(Vect start, Vect end, float distance) => Vect.Interpolate(start, end, distance);
	/// <inheritdoc />
	public override string ToString() => $"[{Value.ToStringDescriptive()} @ {TimeKeySeconds}s]";
}
/// <summary>
/// Specifies how large one joint of a skeleton is at one moment in an animation.
/// </summary>
/// <param name="TimeKeySeconds">When in the animation this keyframe takes effect, in seconds.</param>
/// <param name="Value">The per-axis scaling the joint takes at that moment, where <c>(1, 1, 1)</c> is its resting size.</param>
public readonly record struct SkeletalAnimationScalingKeyframe(float TimeKeySeconds, Vect Value) : IAnimationKeyframe<Vect> {
	/// <summary>
	/// The value used when an animation supplies no scaling keyframes: no scaling at all.
	/// </summary>
	public static Vect FallbackValue { get; } = Vect.One;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect InterpolateValues(Vect start, Vect end, float distance) => Vect.Interpolate(start, end, distance);
	/// <inheritdoc />
	public override string ToString() => $"[{Value.ToStringDescriptive()} @ {TimeKeySeconds}s]";
}
/// <summary>
/// Specifies how one joint of a skeleton is oriented at one moment in an animation.
/// </summary>
/// <param name="TimeKeySeconds">When in the animation this keyframe takes effect, in seconds.</param>
/// <param name="Value">The rotation the joint takes at that moment.</param>
public readonly record struct SkeletalAnimationRotationKeyframe(float TimeKeySeconds, Quaternion Value) : IAnimationKeyframe<Quaternion> {
	/// <summary>
	/// The value used when an animation supplies no rotation keyframes: no rotation at all.
	/// </summary>
	public static Quaternion FallbackValue { get; } = Quaternion.Identity;
	
	/// <summary>
	/// Constructs a new <see cref="SkeletalAnimationRotationKeyframe"/> from a rotation rather than a quaternion.
	/// </summary>
	/// <param name="timeKeySeconds">When in the animation this keyframe takes effect, in seconds.</param>
	/// <param name="value">The rotation the joint takes at that moment.</param>
	public SkeletalAnimationRotationKeyframe(float timeKeySeconds, Rotation value) : this(timeKeySeconds, value.ToQuaternion()) {} 
	
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion InterpolateValues(Quaternion start, Quaternion end, float distance) => Rotation.Interpolate(start, end, distance);
	/// <inheritdoc />
	public override string ToString() => $"[{Rotation.FromQuaternion(Value).ToStringDescriptive()} @ {TimeKeySeconds}s]";
}

/// <summary>
/// Maps one joint of a skeleton to the runs of keyframes that drive it.
/// </summary>
/// <remarks>
/// An animation's keyframes are supplied as three flat lists — one each for scaling, rotation and translation — covering every
/// joint the animation touches. One of these descriptors per joint says which stretch of each list belongs to it, which is what
/// allows the whole animation to be handed over without one list per joint.
/// </remarks>
/// <param name="TargetNodeIndex">Which node in the skeleton these keyframes drive.</param>
/// <param name="ScalingKeyframeStartIndex">Where this node's scaling keyframes begin in the scaling keyframe list.</param>
/// <param name="ScalingKeyframeCount">How many scaling keyframes belong to this node. May be <c>0</c>.</param>
/// <param name="RotationKeyframeStartIndex">Where this node's rotation keyframes begin in the rotation keyframe list.</param>
/// <param name="RotationKeyframeCount">How many rotation keyframes belong to this node. May be <c>0</c>.</param>
/// <param name="TranslationKeyframeStartIndex">Where this node's translation keyframes begin in the translation keyframe list.</param>
/// <param name="TranslationKeyframeCount">How many translation keyframes belong to this node. May be <c>0</c>.</param>
public readonly record struct SkeletalAnimationNodeMutationDescriptor(
	int TargetNodeIndex,
	int ScalingKeyframeStartIndex, int ScalingKeyframeCount,
	int RotationKeyframeStartIndex, int RotationKeyframeCount,
	int TranslationKeyframeStartIndex, int TranslationKeyframeCount
) {
	/// <inheritdoc />
	public override string ToString() {
		return $"[Node #{TargetNodeIndex} => S={ScalingKeyframeStartIndex}+{ScalingKeyframeCount}; R={RotationKeyframeStartIndex}+{RotationKeyframeCount}; T={TranslationKeyframeStartIndex}+{TranslationKeyframeCount}]";
	}
}
