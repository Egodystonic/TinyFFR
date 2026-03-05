// Created on 2026-02-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IAnimationKeyframe<out T> : ITimeKeyedItem where T : IInterpolatable<T> {
	static abstract T FallbackValue { get; }
	T Value { get; }
}

public readonly record struct SkeletalAnimationTranslationKeyframe(float TimeKeySeconds, Vect Value) : IAnimationKeyframe<Vect> {
	public static Vect FallbackValue { get; } = Vect.Zero;
	public override string ToString() => $"[{Value.ToStringDescriptive()} @ {TimeKeySeconds}s]";
}
public readonly record struct SkeletalAnimationScalingKeyframe(float TimeKeySeconds, Vect Value) : IAnimationKeyframe<Vect> {
	public static Vect FallbackValue { get; } = Vect.One;
	public override string ToString() => $"[{Value.ToStringDescriptive()} @ {TimeKeySeconds}s]";
}
public readonly record struct SkeletalAnimationRotationKeyframe(float TimeKeySeconds, Rotation Value) : IAnimationKeyframe<Rotation> {
	public static Rotation FallbackValue { get; } = Rotation.None;
	public override string ToString() => $"[{Value.ToStringDescriptive()} @ {TimeKeySeconds}s]";
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
