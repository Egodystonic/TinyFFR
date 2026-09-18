// Created on 2026-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Plays two animations at once on one object and mixes the result, so that one animation can give way to another smoothly.
/// </summary>
/// <remarks>
/// <para>
/// This is what stops a character snapping from a walk to a run: both animations are evaluated and the poses interpolated, with
/// the blend moved from one end to the other over a few frames. Each animation keeps its own time point, speed and wrapping.
/// </para>
/// <para>
/// This is an ordinary value, not a resource: It is cheap to construct, needs no disposal, and there is no harm in making one
/// per frame.
/// </para>
/// </remarks>
public readonly struct MeshBlendedAnimationPlayer : IEquatable<MeshBlendedAnimationPlayer> {
	/// <summary>
	/// The object this player poses.
	/// </summary>
	public ModelInstance Instance { get; init; }
	/// <summary>
	/// The animation blended away from.
	/// </summary>
	public MeshAnimation StartAnimation { get; init; }
	/// <summary>
	/// The animation blended towards.
	/// </summary>
	public MeshAnimation EndAnimation { get; init; }
	/// <summary>
	/// How fast this player runs the start animation relative to its authored speed, where <c>1f</c> is its own speed.
	/// </summary>
	/// <remarks>
	/// A value of <c>0f</c> is treated as <c>1f</c>.
	/// </remarks>
	public float StartAnimationSpeedMultiplier { get; init; }
	/// <summary>
	/// How long the start animation takes to run at this player's speed, in seconds.
	/// </summary>
	/// <remarks>
	/// This is the same setting as <see cref="StartAnimationSpeedMultiplier"/>'s reciprocal.
	/// </remarks>
	public float StartAnimationDurationSeconds {
		get => StartAnimation.DefaultDurationSeconds / StartAnimationSpeedMultiplier;
		init {
			StartAnimationSpeedMultiplier = StartAnimation.DefaultDurationSeconds / value;
			if (Single.IsNaN(StartAnimationSpeedMultiplier) || !Single.IsFinite(StartAnimationSpeedMultiplier)) StartAnimationSpeedMultiplier = 1f;
		}
	}
	/// <summary>
	/// How fast this player runs the end animation relative to its authored speed, where <c>1f</c> is its own speed.
	/// </summary>
	/// <remarks>
	/// A value of <c>0f</c> is treated as <c>1f</c>.
	/// </remarks>
	public float EndAnimationSpeedMultiplier { get; init; }
	/// <summary>
	/// How long the end animation takes to run at this player's speed, in seconds.
	/// </summary>
	/// <remarks>
	/// This is the same setting as <see cref="EndAnimationSpeedMultiplier"/>'s reciprocal.
	/// </remarks>
	public float EndAnimationDurationSeconds {
		get => EndAnimation.DefaultDurationSeconds / EndAnimationSpeedMultiplier;
		init {
			EndAnimationSpeedMultiplier = EndAnimation.DefaultDurationSeconds / value;
			if (Single.IsNaN(EndAnimationSpeedMultiplier) || !Single.IsFinite(EndAnimationSpeedMultiplier)) EndAnimationSpeedMultiplier = 1f;
		}
	}

	/// <summary>
	/// Constructs a new <see cref="MeshBlendedAnimationPlayer"/> that blends between two animations, each at its authored speed.
	/// </summary>
	/// <remarks>
	/// Players are cheap to construct and hold nothing that needs disposing, so there is no harm in making one per frame.
	/// </remarks>
	/// <param name="instance">The object to pose. Its mesh must be the one both animations belong to.</param>
	/// <param name="startAnimation">The animation to blend away from.</param>
	/// <param name="endAnimation">The animation to blend towards.</param>
	public MeshBlendedAnimationPlayer(ModelInstance instance, MeshAnimation startAnimation, MeshAnimation endAnimation) : this(instance, startAnimation, endAnimation, 1f, 1f) { }
	MeshBlendedAnimationPlayer(ModelInstance instance, MeshAnimation startAnimation, MeshAnimation endAnimation, float startAnimationSpeedMultiplier, float endAnimationSpeedMultiplier) {
		if (startAnimationSpeedMultiplier == 0f) startAnimationSpeedMultiplier = 1f;
		if (endAnimationSpeedMultiplier == 0f) endAnimationSpeedMultiplier = 1f;
		StartAnimation = startAnimation;
		EndAnimation = endAnimation;
		Instance = instance;
		StartAnimationSpeedMultiplier = startAnimationSpeedMultiplier;
		EndAnimationSpeedMultiplier = endAnimationSpeedMultiplier;
	}

	/// <summary>
	/// Constructs a new <see cref="MeshBlendedAnimationPlayer"/> running each animation at a multiple of its authored speed.
	/// </summary>
	/// <param name="instance">The object to pose. Its mesh must be the one both animations belong to.</param>
	/// <param name="startAnimation">The animation to blend away from.</param>
	/// <param name="endAnimation">The animation to blend towards.</param>
	/// <param name="startAnimationSpeedMultiplier">How fast to run the start animation, where <c>1f</c> is its authored speed.</param>
	/// <param name="endAnimationSpeedMultiplier">How fast to run the end animation, where <c>1f</c> is its authored speed.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MeshBlendedAnimationPlayer CreateWithSpeedMultiplier(ModelInstance instance, MeshAnimation startAnimation, MeshAnimation endAnimation, float startAnimationSpeedMultiplier, float endAnimationSpeedMultiplier) {
		return new MeshBlendedAnimationPlayer(instance, startAnimation, endAnimation, startAnimationSpeedMultiplier, endAnimationSpeedMultiplier);
	}
	
	/// <summary>
	/// Constructs a new <see cref="MeshBlendedAnimationPlayer"/> running each animation over a particular length of time.
	/// </summary>
	/// <param name="instance">The object to pose. Its mesh must be the one both animations belong to.</param>
	/// <param name="startAnimation">The animation to blend away from.</param>
	/// <param name="endAnimation">The animation to blend towards.</param>
	/// <param name="startAnimationCompletionTimeSeconds">How long the start animation should take from start to finish, in seconds.</param>
	/// <param name="endAnimationCompletionTimeSeconds">How long the end animation should take from start to finish, in seconds.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MeshBlendedAnimationPlayer CreateWithTargetDuration(ModelInstance instance, MeshAnimation startAnimation, MeshAnimation endAnimation, float startAnimationCompletionTimeSeconds, float endAnimationCompletionTimeSeconds) {
		return new MeshBlendedAnimationPlayer(instance, startAnimation, endAnimation) {
			StartAnimationDurationSeconds = startAnimationCompletionTimeSeconds,
			EndAnimationDurationSeconds = endAnimationCompletionTimeSeconds
		};
	}
	
	#region Time Point
	/// <summary>
	/// Poses the object as a blend of the two animations at the given moments.
	/// </summary>
	/// <remarks>
	/// Time points beyond an animation's duration are not wrapped; use the overload taking wrap styles for that.
	/// </remarks>
	/// <param name="startAnimTimePointSeconds">How far in to the start animation to set its pose, in seconds.</param>
	/// <param name="endAnimTimePointSeconds">How far in to the end animation to set its pose, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePoint(float startAnimTimePointSeconds, float endAnimTimePointSeconds, float interpolationDistance) {
		StartAnimation.ApplyBlended(Instance, startAnimTimePointSeconds * StartAnimationSpeedMultiplier, EndAnimation, endAnimTimePointSeconds * EndAnimationSpeedMultiplier, interpolationDistance);
	}
	/// <summary>
	/// Poses the object as a blend at the given moments, and reports where the given node ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimTimePointSeconds">How far in to the start animation to set its pose, in seconds.</param>
	/// <param name="endAnimTimePointSeconds">How far in to the end animation to set its pose, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float startAnimTimePointSeconds, float endAnimTimePointSeconds, float interpolationDistance, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, startAnimTimePointSeconds * StartAnimationSpeedMultiplier, EndAnimation, endAnimTimePointSeconds * EndAnimationSpeedMultiplier, interpolationDistance, node, out modelSpaceTransform);
	}
	/// <summary>
	/// Poses the object as a blend at the given moments, and reports where the given nodes ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimTimePointSeconds">How far in to the start animation to set its pose, in seconds.</param>
	/// <param name="endAnimTimePointSeconds">How far in to the end animation to set its pose, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float startAnimTimePointSeconds, float endAnimTimePointSeconds, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, startAnimTimePointSeconds * StartAnimationSpeedMultiplier, EndAnimation, endAnimTimePointSeconds * EndAnimationSpeedMultiplier, interpolationDistance, nodes, modelSpaceTransforms);
	}
	/// <summary>
	/// Poses the object as a blend at the given moments, and reports where the nodes at the given indices ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimTimePointSeconds">How far in to the start animation to set its pose, in seconds.</param>
	/// <param name="endAnimTimePointSeconds">How far in to the end animation to set its pose, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float startAnimTimePointSeconds, float endAnimTimePointSeconds, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, startAnimTimePointSeconds * StartAnimationSpeedMultiplier, EndAnimation, endAnimTimePointSeconds * EndAnimationSpeedMultiplier, interpolationDistance, nodeIndices, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the object as a blend of the two animations at the given moments, wrapping each moment in to its animation's duration first.
	/// </summary>
	/// <param name="startAnimTimePointSeconds">How far in to the start animation to set its pose, in seconds.</param>
	/// <param name="startWrapStyle">What to do when the start animation's given point falls outside its duration.</param>
	/// <param name="endAnimTimePointSeconds">How far in to the end animation to set its pose, in seconds.</param>
	/// <param name="endWrapStyle">What to do when the end animation's given point falls outside its duration.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePoint(float startAnimTimePointSeconds, AnimationWrapStyle startWrapStyle, float endAnimTimePointSeconds, AnimationWrapStyle endWrapStyle, float interpolationDistance) {
		StartAnimation.ApplyBlended(
			Instance,
			startWrapStyle.ApplyToTimePoint(startAnimTimePointSeconds * StartAnimationSpeedMultiplier, StartAnimation.DefaultDurationSeconds),
			EndAnimation,
			endWrapStyle.ApplyToTimePoint(endAnimTimePointSeconds * EndAnimationSpeedMultiplier, EndAnimation.DefaultDurationSeconds),
			interpolationDistance
		);
	}
	/// <summary>
	/// Poses the object as a blend at the given wrapped moments, and reports where the given node ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimTimePointSeconds">How far in to the start animation to set its pose, in seconds.</param>
	/// <param name="startWrapStyle">What to do when the start animation's given point falls outside its duration.</param>
	/// <param name="endAnimTimePointSeconds">How far in to the end animation to set its pose, in seconds.</param>
	/// <param name="endWrapStyle">What to do when the end animation's given point falls outside its duration.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float startAnimTimePointSeconds, AnimationWrapStyle startWrapStyle, float endAnimTimePointSeconds, AnimationWrapStyle endWrapStyle, float interpolationDistance, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(
			Instance,
			startWrapStyle.ApplyToTimePoint(startAnimTimePointSeconds * StartAnimationSpeedMultiplier, StartAnimation.DefaultDurationSeconds),
			EndAnimation,
			endWrapStyle.ApplyToTimePoint(endAnimTimePointSeconds * EndAnimationSpeedMultiplier, EndAnimation.DefaultDurationSeconds),
			interpolationDistance,
			node, 
			out modelSpaceTransform
		);
	}
	/// <summary>
	/// Poses the object as a blend at the given wrapped moments, and reports where the given nodes ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimTimePointSeconds">How far in to the start animation to set its pose, in seconds.</param>
	/// <param name="startWrapStyle">What to do when the start animation's given point falls outside its duration.</param>
	/// <param name="endAnimTimePointSeconds">How far in to the end animation to set its pose, in seconds.</param>
	/// <param name="endWrapStyle">What to do when the end animation's given point falls outside its duration.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float startAnimTimePointSeconds, AnimationWrapStyle startWrapStyle, float endAnimTimePointSeconds, AnimationWrapStyle endWrapStyle, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(
			Instance,
			startWrapStyle.ApplyToTimePoint(startAnimTimePointSeconds * StartAnimationSpeedMultiplier, StartAnimation.DefaultDurationSeconds),
			EndAnimation,
			endWrapStyle.ApplyToTimePoint(endAnimTimePointSeconds * EndAnimationSpeedMultiplier, EndAnimation.DefaultDurationSeconds),
			interpolationDistance,
			nodes, 
			modelSpaceTransforms
		);
	}
	/// <summary>
	/// Poses the object as a blend at the given wrapped moments, and reports where the nodes at the given indices ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimTimePointSeconds">How far in to the start animation to set its pose, in seconds.</param>
	/// <param name="startWrapStyle">What to do when the start animation's given point falls outside its duration.</param>
	/// <param name="endAnimTimePointSeconds">How far in to the end animation to set its pose, in seconds.</param>
	/// <param name="endWrapStyle">What to do when the end animation's given point falls outside its duration.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float startAnimTimePointSeconds, AnimationWrapStyle startWrapStyle, float endAnimTimePointSeconds, AnimationWrapStyle endWrapStyle, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(
			Instance,
			startWrapStyle.ApplyToTimePoint(startAnimTimePointSeconds * StartAnimationSpeedMultiplier, StartAnimation.DefaultDurationSeconds),
			EndAnimation,
			endWrapStyle.ApplyToTimePoint(endAnimTimePointSeconds * EndAnimationSpeedMultiplier, EndAnimation.DefaultDurationSeconds),
			interpolationDistance,
			nodeIndices, 
			modelSpaceTransforms
		);
	}
	#endregion

	#region Completion Fraction
	/// <summary>
	/// Poses the object as a blend of the two animations at the given fractions through each.
	/// </summary>
	/// <param name="startAnimFraction">How far through the start animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="endAnimFraction">How far through the end animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFraction(float startAnimFraction, float endAnimFraction, float interpolationDistance) {
		StartAnimation.ApplyBlended(Instance, StartAnimation.DefaultDurationSeconds * startAnimFraction, EndAnimation, EndAnimation.DefaultDurationSeconds * endAnimFraction, interpolationDistance);
	}
	/// <summary>
	/// Poses the object as a blend at the given fractions, and reports where the given node ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimFraction">How far through the start animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="endAnimFraction">How far through the end animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float startAnimFraction, float endAnimFraction, float interpolationDistance, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, StartAnimation.DefaultDurationSeconds * startAnimFraction, EndAnimation, EndAnimation.DefaultDurationSeconds * endAnimFraction, interpolationDistance, node, out modelSpaceTransform);
	}
	/// <summary>
	/// Poses the object as a blend at the given fractions, and reports where the given nodes ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimFraction">How far through the start animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="endAnimFraction">How far through the end animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float startAnimFraction, float endAnimFraction, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, StartAnimation.DefaultDurationSeconds * startAnimFraction, EndAnimation, EndAnimation.DefaultDurationSeconds * endAnimFraction, interpolationDistance, nodes, modelSpaceTransforms);
	}
	/// <summary>
	/// Poses the object as a blend at the given fractions, and reports where the nodes at the given indices ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimFraction">How far through the start animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="endAnimFraction">How far through the end animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float startAnimFraction, float endAnimFraction, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, StartAnimation.DefaultDurationSeconds * startAnimFraction, EndAnimation, EndAnimation.DefaultDurationSeconds * endAnimFraction, interpolationDistance, nodeIndices, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the object as a blend at the given fractions, wrapping each in to the range <c>0f</c> to <c>1f</c> first.
	/// </summary>
	/// <param name="startAnimFraction">How far through the start animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="startWrapStyle">What to do when the start animation's given point falls outside its duration.</param>
	/// <param name="endAnimFraction">How far through the end animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="endWrapStyle">What to do when the end animation's given point falls outside its duration.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFraction(float startAnimFraction, AnimationWrapStyle startWrapStyle, float endAnimFraction, AnimationWrapStyle endWrapStyle, float interpolationDistance) {
		StartAnimation.ApplyBlended(
			Instance,
			startWrapStyle.ApplyToTimePoint(StartAnimation.DefaultDurationSeconds * startAnimFraction, StartAnimation.DefaultDurationSeconds),
			EndAnimation,
			endWrapStyle.ApplyToTimePoint(EndAnimation.DefaultDurationSeconds * endAnimFraction, EndAnimation.DefaultDurationSeconds),
			interpolationDistance
		);
	}
	/// <summary>
	/// Poses the object as a blend at the given wrapped fractions, and reports where the given node ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimFraction">How far through the start animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="startWrapStyle">What to do when the start animation's given point falls outside its duration.</param>
	/// <param name="endAnimFraction">How far through the end animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="endWrapStyle">What to do when the end animation's given point falls outside its duration.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float startAnimFraction, AnimationWrapStyle startWrapStyle, float endAnimFraction, AnimationWrapStyle endWrapStyle, float interpolationDistance, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(
			Instance,
			startWrapStyle.ApplyToTimePoint(StartAnimation.DefaultDurationSeconds * startAnimFraction, StartAnimation.DefaultDurationSeconds),
			EndAnimation,
			endWrapStyle.ApplyToTimePoint(EndAnimation.DefaultDurationSeconds * endAnimFraction, EndAnimation.DefaultDurationSeconds),
			interpolationDistance,
			node, 
			out modelSpaceTransform
		);
	}
	/// <summary>
	/// Poses the object as a blend at the given wrapped fractions, and reports where the given nodes ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimFraction">How far through the start animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="startWrapStyle">What to do when the start animation's given point falls outside its duration.</param>
	/// <param name="endAnimFraction">How far through the end animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="endWrapStyle">What to do when the end animation's given point falls outside its duration.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float startAnimFraction, AnimationWrapStyle startWrapStyle, float endAnimFraction, AnimationWrapStyle endWrapStyle, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(
			Instance,
			startWrapStyle.ApplyToTimePoint(StartAnimation.DefaultDurationSeconds * startAnimFraction, StartAnimation.DefaultDurationSeconds),
			EndAnimation,
			endWrapStyle.ApplyToTimePoint(EndAnimation.DefaultDurationSeconds * endAnimFraction, EndAnimation.DefaultDurationSeconds),
			interpolationDistance,
			nodes, 
			modelSpaceTransforms
		);
	}
	/// <summary>
	/// Poses the object as a blend at the given wrapped fractions, and reports where the nodes at the given indices ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position. That is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="startAnimFraction">How far through the start animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="startWrapStyle">What to do when the start animation's given point falls outside its duration.</param>
	/// <param name="endAnimFraction">How far through the end animation to set its pose, where <c>0f</c> is its start and <c>1f</c> its end.</param>
	/// <param name="endWrapStyle">What to do when the end animation's given point falls outside its duration.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely the start animation and <c>1f</c> entirely the end animation.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float startAnimFraction, AnimationWrapStyle startWrapStyle, float endAnimFraction, AnimationWrapStyle endWrapStyle, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(
			Instance,
			startWrapStyle.ApplyToTimePoint(StartAnimation.DefaultDurationSeconds * startAnimFraction, StartAnimation.DefaultDurationSeconds),
			EndAnimation,
			endWrapStyle.ApplyToTimePoint(EndAnimation.DefaultDurationSeconds * endAnimFraction, EndAnimation.DefaultDurationSeconds),
			interpolationDistance,
			nodeIndices, 
			modelSpaceTransforms
		);
	}
	#endregion

	/// <inheritdoc />
	public bool Equals(MeshBlendedAnimationPlayer other) => Instance.Equals(other.Instance) && StartAnimation.Equals(other.StartAnimation) && EndAnimation.Equals(other.EndAnimation) && StartAnimationSpeedMultiplier.Equals(other.StartAnimationSpeedMultiplier) && EndAnimationSpeedMultiplier.Equals(other.EndAnimationSpeedMultiplier);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is MeshBlendedAnimationPlayer other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(Instance, StartAnimation, EndAnimation, StartAnimationSpeedMultiplier, EndAnimationSpeedMultiplier);
	/// <summary>
	/// Returns whether the two given players pose the same object with the same pair of animations at the same speeds.
	/// </summary>
	/// <param name="left">The first player to compare.</param>
	/// <param name="right">The second player to compare.</param>
	public static bool operator ==(MeshBlendedAnimationPlayer left, MeshBlendedAnimationPlayer right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given players differ in their object, animations or speeds.
	/// </summary>
	/// <param name="left">The first player to compare.</param>
	/// <param name="right">The second player to compare.</param>
	public static bool operator !=(MeshBlendedAnimationPlayer left, MeshBlendedAnimationPlayer right) => !left.Equals(right);
}