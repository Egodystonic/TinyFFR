// Created on 2026-02-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IAnimationKeyframe<T> : ITimeKeyedItem {
	static abstract T FallbackValue { get; }
	static abstract T InterpolateValues(T start, T end, float distance);
	T Value { get; }
}

public readonly record struct SkeletalAnimationTranslationKeyframe(float TimeKeySeconds, Vect Value) : IAnimationKeyframe<Vect> {
	public static Vect FallbackValue { get; } = Vect.Zero;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect InterpolateValues(Vect start, Vect end, float distance) => Vect.Interpolate(start, end, distance);
	public override string ToString() => $"[{Value.ToStringDescriptive()} @ {TimeKeySeconds}s]";
}
public readonly record struct SkeletalAnimationScalingKeyframe(float TimeKeySeconds, Vect Value) : IAnimationKeyframe<Vect> {
	public static Vect FallbackValue { get; } = Vect.One;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect InterpolateValues(Vect start, Vect end, float distance) => Vect.Interpolate(start, end, distance);
	public override string ToString() => $"[{Value.ToStringDescriptive()} @ {TimeKeySeconds}s]";
}
public readonly record struct SkeletalAnimationRotationKeyframe(float TimeKeySeconds, Quaternion Value) : IAnimationKeyframe<Quaternion> {
	public static Quaternion FallbackValue { get; } = Quaternion.Identity;
	
	public SkeletalAnimationRotationKeyframe(float timeKeySeconds, Rotation value) : this(timeKeySeconds, value.ToQuaternion()) {} 
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion InterpolateValues(Quaternion start, Quaternion end, float distance) => Rotation.Interpolate(start, end, distance);
	public override string ToString() => $"[{Rotation.FromQuaternion(Value).ToStringDescriptive()} @ {TimeKeySeconds}s]";
}

public readonly record struct SkeletalAnimationNodeMutationDescriptor(
	int TargetNodeIndex,
	int ScalingKeyframeStartIndex, int ScalingKeyframeCount,
	int RotationKeyframeStartIndex, int RotationKeyframeCount,
	int TranslationKeyframeStartIndex, int TranslationKeyframeCount
) {
	public override string ToString() {
		return $"[Node #{TargetNodeIndex} => S={ScalingKeyframeStartIndex}+{ScalingKeyframeCount}; R={RotationKeyframeStartIndex}+{RotationKeyframeCount}; T={TranslationKeyframeStartIndex}+{TranslationKeyframeCount}]";
	}
}
