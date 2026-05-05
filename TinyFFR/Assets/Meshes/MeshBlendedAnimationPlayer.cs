// Created on 2026-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct MeshBlendedAnimationPlayer : IEquatable<MeshBlendedAnimationPlayer> {
	public ModelInstance Instance { get; init; }
	public MeshAnimation StartAnimation { get; init; }
	public MeshAnimation EndAnimation { get; init; }
	public float StartAnimationSpeedMultiplier { get; init; }
	public float StartAnimationDurationSeconds {
		get => StartAnimation.DefaultDurationSeconds / StartAnimationSpeedMultiplier;
		init {
			StartAnimationSpeedMultiplier = StartAnimation.DefaultDurationSeconds / value;
			if (Single.IsNaN(StartAnimationSpeedMultiplier) || !Single.IsFinite(StartAnimationSpeedMultiplier)) StartAnimationSpeedMultiplier = 1f;
		}
	}
	public float EndAnimationSpeedMultiplier { get; init; }
	public float EndAnimationDurationSeconds {
		get => EndAnimation.DefaultDurationSeconds / EndAnimationSpeedMultiplier;
		init {
			EndAnimationSpeedMultiplier = EndAnimation.DefaultDurationSeconds / value;
			if (Single.IsNaN(EndAnimationSpeedMultiplier) || !Single.IsFinite(EndAnimationSpeedMultiplier)) EndAnimationSpeedMultiplier = 1f;
		}
	}

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MeshBlendedAnimationPlayer CreateWithSpeedMultiplier(ModelInstance instance, MeshAnimation startAnimation, MeshAnimation endAnimation, float startAnimationSpeedMultiplier, float endAnimationSpeedMultiplier) {
		return new MeshBlendedAnimationPlayer(instance, startAnimation, endAnimation, startAnimationSpeedMultiplier, endAnimationSpeedMultiplier);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MeshBlendedAnimationPlayer CreateWithTargetDuration(ModelInstance instance, MeshAnimation startAnimation, MeshAnimation endAnimation, float startAnimationCompletionTimeSeconds, float endAnimationCompletionTimeSeconds) {
		return new MeshBlendedAnimationPlayer(instance, startAnimation, endAnimation) {
			StartAnimationDurationSeconds = startAnimationCompletionTimeSeconds,
			EndAnimationDurationSeconds = endAnimationCompletionTimeSeconds
		};
	}
	
	#region Time Point
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePoint(float startAnimTimePointSeconds, float endAnimTimePointSeconds, float interpolationDistance) {
		StartAnimation.ApplyBlended(Instance, startAnimTimePointSeconds * StartAnimationSpeedMultiplier, EndAnimation, endAnimTimePointSeconds * EndAnimationSpeedMultiplier, interpolationDistance);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float startAnimTimePointSeconds, float endAnimTimePointSeconds, float interpolationDistance, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, startAnimTimePointSeconds * StartAnimationSpeedMultiplier, EndAnimation, endAnimTimePointSeconds * EndAnimationSpeedMultiplier, interpolationDistance, node, out modelSpaceTransform);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float startAnimTimePointSeconds, float endAnimTimePointSeconds, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, startAnimTimePointSeconds * StartAnimationSpeedMultiplier, EndAnimation, endAnimTimePointSeconds * EndAnimationSpeedMultiplier, interpolationDistance, nodes, modelSpaceTransforms);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float startAnimTimePointSeconds, float endAnimTimePointSeconds, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, startAnimTimePointSeconds * StartAnimationSpeedMultiplier, EndAnimation, endAnimTimePointSeconds * EndAnimationSpeedMultiplier, interpolationDistance, nodeIndices, modelSpaceTransforms);
	}
	
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFraction(float startAnimFraction, float endAnimFraction, float interpolationDistance) {
		StartAnimation.ApplyBlended(Instance, StartAnimation.DefaultDurationSeconds * startAnimFraction, EndAnimation, EndAnimation.DefaultDurationSeconds * endAnimFraction, interpolationDistance);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float startAnimFraction, float endAnimFraction, float interpolationDistance, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, StartAnimation.DefaultDurationSeconds * startAnimFraction, EndAnimation, EndAnimation.DefaultDurationSeconds * endAnimFraction, interpolationDistance, node, out modelSpaceTransform);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float startAnimFraction, float endAnimFraction, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, StartAnimation.DefaultDurationSeconds * startAnimFraction, EndAnimation, EndAnimation.DefaultDurationSeconds * endAnimFraction, interpolationDistance, nodes, modelSpaceTransforms);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float startAnimFraction, float endAnimFraction, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		StartAnimation.ApplyBlendedAndGetNodeTransforms(Instance, StartAnimation.DefaultDurationSeconds * startAnimFraction, EndAnimation, EndAnimation.DefaultDurationSeconds * endAnimFraction, interpolationDistance, nodeIndices, modelSpaceTransforms);
	}
	
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

	public bool Equals(MeshBlendedAnimationPlayer other) => Instance.Equals(other.Instance) && StartAnimation.Equals(other.StartAnimation) && EndAnimation.Equals(other.EndAnimation) && StartAnimationSpeedMultiplier.Equals(other.StartAnimationSpeedMultiplier) && EndAnimationSpeedMultiplier.Equals(other.EndAnimationSpeedMultiplier);
	public override bool Equals(object? obj) => obj is MeshBlendedAnimationPlayer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(Instance, StartAnimation, EndAnimation, StartAnimationSpeedMultiplier, EndAnimationSpeedMultiplier);
	public static bool operator ==(MeshBlendedAnimationPlayer left, MeshBlendedAnimationPlayer right) => left.Equals(right);
	public static bool operator !=(MeshBlendedAnimationPlayer left, MeshBlendedAnimationPlayer right) => !left.Equals(right);
}