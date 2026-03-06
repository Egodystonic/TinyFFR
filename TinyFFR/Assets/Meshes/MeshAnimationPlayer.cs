// Created on 2026-03-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public enum MeshAnimationTimestampWrapStyle {
	None,
	Loop,
	LoopPingPonged,
	Clamp,
	ClampPingPonged
}

public readonly struct MeshAnimationPlayer : IEquatable<MeshAnimationPlayer> {
	public MeshAnimation Animation { get; init; }
	public ModelInstance Instance { get; init; }
	public float SpeedMultiplier { get; init; }
	public float DurationSeconds {
		get => Animation.DefaultDurationSeconds / SpeedMultiplier;
		init {
			SpeedMultiplier = Animation.DefaultDurationSeconds / value;
			if (Single.IsNaN(SpeedMultiplier) || !Single.IsFinite(SpeedMultiplier)) SpeedMultiplier = 1f;
		}
	}

	public MeshAnimationPlayer(MeshAnimation animation, ModelInstance instance) : this(animation, instance, 1f) { }
	MeshAnimationPlayer(MeshAnimation animation, ModelInstance instance, float speedMultiplier) {
		if (speedMultiplier == 0f) speedMultiplier = 1f;
		Animation = animation;
		Instance = instance;
		SpeedMultiplier = speedMultiplier;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MeshAnimationPlayer CreateWithSpeedMultiplier(MeshAnimation animation, ModelInstance instance, float speedMultiplier) {
		return new MeshAnimationPlayer(animation, instance, speedMultiplier);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static MeshAnimationPlayer CreateWithTargetDuration(MeshAnimation animation, ModelInstance instance, float targetAnimationCompletionTimeSeconds) {
		return new MeshAnimationPlayer(animation, instance) { DurationSeconds = targetAnimationCompletionTimeSeconds };
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTimePoint(float timePointSeconds) {
		Animation.Apply(Instance, timePointSeconds * SpeedMultiplier);
	}
	
	public void SetTimePoint(float timePointSeconds, MeshAnimationTimestampWrapStyle wrapStyle) {
		Animation.Apply(Instance, ApplyWrapping(timePointSeconds * SpeedMultiplier, wrapStyle, Animation.DefaultDurationSeconds));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFraction(float fraction) {
		Animation.Apply(Instance, Animation.DefaultDurationSeconds * fraction);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCompletionFraction(float fraction, MeshAnimationTimestampWrapStyle wrapStyle) {
		Animation.Apply(Instance, ApplyWrapping(Animation.DefaultDurationSeconds * fraction, wrapStyle, Animation.DefaultDurationSeconds));
	}
	
	public static float ApplyWrapping(float nonWrappedTimePoint, MeshAnimationTimestampWrapStyle wrapStyle, float animationDefaultDuration) {
		return wrapStyle switch {
			MeshAnimationTimestampWrapStyle.Loop => MathUtils.TrueModulus(nonWrappedTimePoint, animationDefaultDuration),
			MeshAnimationTimestampWrapStyle.LoopPingPonged => Angle.FromRadians(nonWrappedTimePoint).TriangularizeRectified(Angle.FromRadians(animationDefaultDuration)).Radians,
			MeshAnimationTimestampWrapStyle.Clamp => (float) ((Real) nonWrappedTimePoint).Clamp(0f, animationDefaultDuration),
			MeshAnimationTimestampWrapStyle.ClampPingPonged => Angle.FromRadians(((Real) nonWrappedTimePoint).Clamp(0f, animationDefaultDuration * 2f)).TriangularizeRectified(Angle.FromRadians(animationDefaultDuration)).Radians,
			_ => nonWrappedTimePoint
		};
	}

	public bool Equals(MeshAnimationPlayer other) => Animation.Equals(other.Animation) && Instance.Equals(other.Instance);
	public override bool Equals(object? obj) => obj is MeshAnimationPlayer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(Animation, Instance);
	public static bool operator ==(MeshAnimationPlayer left, MeshAnimationPlayer right) => left.Equals(right);
	public static bool operator !=(MeshAnimationPlayer left, MeshAnimationPlayer right) => !left.Equals(right);
}