// Created on 2026-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Plays one animation on one object, handling playback speed and what happens past the end of the animation.
/// </summary>
/// <remarks>
/// <para>
/// A player does not advance by itself: each frame you tell it how far through the animation to be, and it poses the object
/// accordingly. That keeps the timeline yours to control — to pause it, run it backwards, or drive it from something other than
/// elapsed time.
/// </para>
/// <para>
/// This is an ordinary value, not a resource: It is cheap to construct, needs no disposal, and there is no harm in making one
/// per animation per frame.
/// </para>
/// </remarks>
public readonly struct MeshAnimationPlayer : IEquatable<MeshAnimationPlayer> {
	/// <summary>
	/// The object this player poses.
	/// </summary>
	public ModelInstance Instance { get; init; }
	/// <summary>
	/// The animation this player plays.
	/// </summary>
	public MeshAnimation Animation { get; init; }
	/// <summary>
	/// How fast this player runs the animation relative to its authored speed, where <c>1f</c> is its own speed.
	/// </summary>
	/// <remarks>
	/// Values above <c>1f</c> run it faster and below <c>1f</c> slower. A value of <c>0f</c> is treated as <c>1f</c>.
	/// </remarks>
	public float SpeedMultiplier { get; init; }
	/// <summary>
	/// How long the animation takes to run at this player's speed, in seconds.
	/// </summary>
	/// <remarks>
	/// This is the same setting as <see cref="SpeedMultiplier"/> expressed reciprocally: Setting a duration here picks
	/// whatever speed achieves it. A duration that would give a non-finite speed leaves the speed at <c>1f</c>.
	/// </remarks>
	public float DurationSeconds {
		get => Animation.DefaultDurationSeconds / SpeedMultiplier;
		init {
			SpeedMultiplier = Animation.DefaultDurationSeconds / value;
			if (Single.IsNaN(SpeedMultiplier) || !Single.IsFinite(SpeedMultiplier)) SpeedMultiplier = 1f;
		}
	}

	/// <summary>
	/// Constructs a new <see cref="MeshAnimationPlayer"/> that runs the given animation at its authored speed.
	/// </summary>
	/// <remarks>
	/// Players are cheap to construct and hold nothing that needs disposing, so there is no harm in making one per frame.
	/// </remarks>
	/// <param name="instance">The object to pose. Its mesh must be the one the animation belongs to.</param>
	/// <param name="animation">The animation to play.</param>
	public MeshAnimationPlayer(ModelInstance instance, MeshAnimation animation) : this(instance, animation, 1f) { }
	MeshAnimationPlayer(ModelInstance instance, MeshAnimation animation, float speedMultiplier) {
		if (speedMultiplier == 0f) speedMultiplier = 1f;
		Instance = instance;
		Animation = animation;
		SpeedMultiplier = speedMultiplier;
	}

	/// <summary>
	/// Constructs a new <see cref="MeshAnimationPlayer"/> that runs the given animation at a multiple of its authored speed.
	/// </summary>
	/// <param name="instance">The object to pose. Its mesh must be the one the animation belongs to.</param>
	/// <param name="animation">The animation to play.</param>
	/// <param name="speedMultiplier">How fast to run it, where <c>1f</c> is its authored speed. A value of <c>0f</c> is treated as <c>1f</c>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MeshAnimationPlayer CreateWithSpeedMultiplier(ModelInstance instance, MeshAnimation animation, float speedMultiplier) {
		return new MeshAnimationPlayer(instance, animation, speedMultiplier);
	}
	
	/// <summary>
	/// Constructs a new <see cref="MeshAnimationPlayer"/> that runs the given animation over a particular length of time.
	/// </summary>
	/// <param name="instance">The object to pose. Its mesh must be the one the animation belongs to.</param>
	/// <param name="animation">The animation to play.</param>
	/// <param name="targetAnimationCompletionTimeSeconds">How long the animation should take from start to finish, in seconds.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MeshAnimationPlayer CreateWithTargetDuration(ModelInstance instance, MeshAnimation animation, float targetAnimationCompletionTimeSeconds) {
		return new MeshAnimationPlayer(instance, animation) { DurationSeconds = targetAnimationCompletionTimeSeconds };
	}
	
	#region Time Point
	/// <summary>
	/// Poses the object as the animation has it at the given moment.
	/// </summary>
	/// <remarks>
	/// Time points beyond the animation's duration are not wrapped; use the overload taking a wrap style for that.
	/// </remarks>
	/// <param name="timePointSeconds">How far in to the animation to set the pose, in seconds.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePoint(float timePointSeconds) {
		Animation.Apply(Instance, timePointSeconds * SpeedMultiplier);
	}
	/// <summary>
	/// Poses the object as the animation has it at the given moment, and reports where the given node ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="timePointSeconds">How far in to the animation to set the pose, in seconds.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float timePointSeconds, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Animation.ApplyAndGetNodeTransforms(Instance, timePointSeconds * SpeedMultiplier, node, out modelSpaceTransform);
	}
	/// <summary>
	/// Poses the object as the animation has it at the given moment, and reports where the given nodes ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="timePointSeconds">How far in to the animation to set the pose, in seconds.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float timePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, timePointSeconds * SpeedMultiplier, nodes, modelSpaceTransforms);
	}
	/// <summary>
	/// Poses the object as the animation has it at the given moment, and reports where the nodes at the given indices ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="timePointSeconds">How far in to the animation to set the pose, in seconds.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float timePointSeconds, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, timePointSeconds * SpeedMultiplier, nodeIndices, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the object as the animation has it at the given moment, wrapping that moment in to the animation's duration first.
	/// </summary>
	/// <param name="timePointSeconds">How far in to the animation to set the pose, in seconds.</param>
	/// <param name="wrapStyle">What to do when the given point falls outside the animation's duration.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePoint(float timePointSeconds, AnimationWrapStyle wrapStyle) {
		Animation.Apply(Instance, wrapStyle.ApplyToTimePoint(timePointSeconds * SpeedMultiplier, Animation.DefaultDurationSeconds));
	}
	/// <summary>
	/// Poses the object at the given wrapped moment, and reports where the given node ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="timePointSeconds">How far in to the animation to set the pose, in seconds.</param>
	/// <param name="wrapStyle">What to do when the given point falls outside the animation's duration.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float timePointSeconds, AnimationWrapStyle wrapStyle, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Animation.ApplyAndGetNodeTransforms(Instance, wrapStyle.ApplyToTimePoint(timePointSeconds * SpeedMultiplier, Animation.DefaultDurationSeconds), node, out modelSpaceTransform);
	}
	/// <summary>
	/// Poses the object at the given wrapped moment, and reports where the given nodes ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="timePointSeconds">How far in to the animation to set the pose, in seconds.</param>
	/// <param name="wrapStyle">What to do when the given point falls outside the animation's duration.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float timePointSeconds, AnimationWrapStyle wrapStyle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, wrapStyle.ApplyToTimePoint(timePointSeconds * SpeedMultiplier, Animation.DefaultDurationSeconds), nodes, modelSpaceTransforms);
	}
	/// <summary>
	/// Poses the object at the given wrapped moment, and reports where the nodes at the given indices ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="timePointSeconds">How far in to the animation to set the pose, in seconds.</param>
	/// <param name="wrapStyle">What to do when the given point falls outside the animation's duration.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float timePointSeconds, AnimationWrapStyle wrapStyle, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, wrapStyle.ApplyToTimePoint(timePointSeconds * SpeedMultiplier, Animation.DefaultDurationSeconds), nodeIndices, modelSpaceTransforms);
	}
	#endregion

	#region Completion Fraction
	/// <summary>
	/// Poses the object at the given fraction of the way through the animation.
	/// </summary>
	/// <remarks>
	/// This is the same as setting a time point, expressed as a proportion of the animation's length rather than in seconds.
	/// </remarks>
	/// <param name="fraction">How far through the animation to set the pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFraction(float fraction) {
		Animation.Apply(Instance, Animation.DefaultDurationSeconds * fraction);
	}
	/// <summary>
	/// Poses the object at the given fraction through the animation, and reports where the given node ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="fraction">How far through the animation to set the pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float fraction, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Animation.ApplyAndGetNodeTransforms(Instance, Animation.DefaultDurationSeconds * fraction, node, out modelSpaceTransform);
	}
	/// <summary>
	/// Poses the object at the given fraction through the animation, and reports where the given nodes ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="fraction">How far through the animation to set the pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float fraction, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, Animation.DefaultDurationSeconds * fraction, nodes, modelSpaceTransforms);
	}
	/// <summary>
	/// Poses the object at the given fraction through the animation, and reports where the nodes at the given indices ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="fraction">How far through the animation to set the pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float fraction, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, Animation.DefaultDurationSeconds * fraction, nodeIndices, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the object at the given fraction through the animation, wrapping that fraction in to the range <c>0f</c> to <c>1f</c> first.
	/// </summary>
	/// <param name="fraction">How far through the animation to set the pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="wrapStyle">What to do when the given point falls outside the animation's duration.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFraction(float fraction, AnimationWrapStyle wrapStyle) {
		Animation.Apply(Instance, wrapStyle.ApplyToTimePoint(Animation.DefaultDurationSeconds * fraction, Animation.DefaultDurationSeconds));
	}
	/// <summary>
	/// Poses the object at the given wrapped fraction, and reports where the given node ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="fraction">How far through the animation to set the pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="wrapStyle">What to do when the given point falls outside the animation's duration.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float fraction, AnimationWrapStyle wrapStyle, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Animation.ApplyAndGetNodeTransforms(Instance, wrapStyle.ApplyToTimePoint(Animation.DefaultDurationSeconds * fraction, Animation.DefaultDurationSeconds), node, out modelSpaceTransform);
	}
	/// <summary>
	/// Poses the object at the given wrapped fraction, and reports where the given nodes ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="fraction">How far through the animation to set the pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="wrapStyle">What to do when the given point falls outside the animation's duration.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float fraction, AnimationWrapStyle wrapStyle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, wrapStyle.ApplyToTimePoint(Animation.DefaultDurationSeconds * fraction, Animation.DefaultDurationSeconds), nodes, modelSpaceTransforms);
	}
	/// <summary>
	/// Poses the object at the given wrapped fraction, and reports where the nodes at the given indices ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="fraction">How far through the animation to set the pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="wrapStyle">What to do when the given point falls outside the animation's duration.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float fraction, AnimationWrapStyle wrapStyle, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, wrapStyle.ApplyToTimePoint(Animation.DefaultDurationSeconds * fraction, Animation.DefaultDurationSeconds), nodeIndices, modelSpaceTransforms);
	}
	#endregion

	/// <inheritdoc />
	public bool Equals(MeshAnimationPlayer other) => Instance.Equals(other.Instance) && Animation.Equals(other.Animation) && SpeedMultiplier.Equals(other.SpeedMultiplier);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is MeshAnimationPlayer other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(Instance, Animation, SpeedMultiplier);
	/// <summary>
	/// Returns whether the two given players pose the same object with the same animation at the same speed.
	/// </summary>
	/// <param name="left">The first player to compare.</param>
	/// <param name="right">The second player to compare.</param>
	public static bool operator ==(MeshAnimationPlayer left, MeshAnimationPlayer right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given players differ in their object, animation or speed.
	/// </summary>
	/// <param name="left">The first player to compare.</param>
	/// <param name="right">The second player to compare.</param>
	public static bool operator !=(MeshAnimationPlayer left, MeshAnimationPlayer right) => !left.Equals(right);
}