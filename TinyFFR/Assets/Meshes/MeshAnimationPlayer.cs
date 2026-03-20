// Created on 2026-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct MeshAnimationPlayer : IEquatable<MeshAnimationPlayer> {
	public ModelInstance Instance { get; init; }
	public MeshAnimation Animation { get; init; }
	public float SpeedMultiplier { get; init; }
	public float DurationSeconds {
		get => Animation.DefaultDurationSeconds / SpeedMultiplier;
		init {
			SpeedMultiplier = Animation.DefaultDurationSeconds / value;
			if (Single.IsNaN(SpeedMultiplier) || !Single.IsFinite(SpeedMultiplier)) SpeedMultiplier = 1f;
		}
	}

	public MeshAnimationPlayer(ModelInstance instance, MeshAnimation animation) : this(instance, animation, 1f) { }
	MeshAnimationPlayer(ModelInstance instance, MeshAnimation animation, float speedMultiplier) {
		if (speedMultiplier == 0f) speedMultiplier = 1f;
		Instance = instance;
		Animation = animation;
		SpeedMultiplier = speedMultiplier;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MeshAnimationPlayer CreateWithSpeedMultiplier(ModelInstance instance, MeshAnimation animation, float speedMultiplier) {
		return new MeshAnimationPlayer(instance, animation, speedMultiplier);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MeshAnimationPlayer CreateWithTargetDuration(ModelInstance instance, MeshAnimation animation, float targetAnimationCompletionTimeSeconds) {
		return new MeshAnimationPlayer(instance, animation) { DurationSeconds = targetAnimationCompletionTimeSeconds };
	}
	
	#region Time Point
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePoint(float timePointSeconds) {
		Animation.Apply(Instance, timePointSeconds * SpeedMultiplier);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransform(float timePointSeconds, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Animation.ApplyAndGetNodeTransform(Instance, timePointSeconds * SpeedMultiplier, node, out modelSpaceTransform);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float timePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, timePointSeconds * SpeedMultiplier, nodes, modelSpaceTransforms);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePoint(float timePointSeconds, MeshAnimationWrapStyle wrapStyle) {
		Animation.Apply(Instance, wrapStyle.ApplyToTimePoint(timePointSeconds * SpeedMultiplier, Animation.DefaultDurationSeconds));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransform(float timePointSeconds, MeshAnimationWrapStyle wrapStyle, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Animation.ApplyAndGetNodeTransform(Instance, wrapStyle.ApplyToTimePoint(timePointSeconds * SpeedMultiplier, Animation.DefaultDurationSeconds), node, out modelSpaceTransform);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePointAndGetNodeTransforms(float timePointSeconds, MeshAnimationWrapStyle wrapStyle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, wrapStyle.ApplyToTimePoint(timePointSeconds * SpeedMultiplier, Animation.DefaultDurationSeconds), nodes, modelSpaceTransforms);
	}
	#endregion

	#region Completion Fraction
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFraction(float fraction) {
		Animation.Apply(Instance, Animation.DefaultDurationSeconds * fraction);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransform(float fraction, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Animation.ApplyAndGetNodeTransform(Instance, Animation.DefaultDurationSeconds * fraction, node, out modelSpaceTransform);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float fraction, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, Animation.DefaultDurationSeconds * fraction, nodes, modelSpaceTransforms);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFraction(float fraction, MeshAnimationWrapStyle wrapStyle) {
		Animation.Apply(Instance, wrapStyle.ApplyToTimePoint(Animation.DefaultDurationSeconds * fraction, Animation.DefaultDurationSeconds));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransform(float fraction, MeshAnimationWrapStyle wrapStyle, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Animation.ApplyAndGetNodeTransform(Instance, wrapStyle.ApplyToTimePoint(Animation.DefaultDurationSeconds * fraction, Animation.DefaultDurationSeconds), node, out modelSpaceTransform);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFractionAndGetNodeTransforms(float fraction, MeshAnimationWrapStyle wrapStyle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Animation.ApplyAndGetNodeTransforms(Instance, wrapStyle.ApplyToTimePoint(Animation.DefaultDurationSeconds * fraction, Animation.DefaultDurationSeconds), nodes, modelSpaceTransforms);
	}
	#endregion

	public bool Equals(MeshAnimationPlayer other) => Instance.Equals(other.Instance) && Animation.Equals(other.Animation) && SpeedMultiplier.Equals(other.SpeedMultiplier);
	public override bool Equals(object? obj) => obj is MeshAnimationPlayer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(Instance, Animation, SpeedMultiplier);
	public static bool operator ==(MeshAnimationPlayer left, MeshAnimationPlayer right) => left.Equals(right);
	public static bool operator !=(MeshAnimationPlayer left, MeshAnimationPlayer right) => !left.Equals(right);
}