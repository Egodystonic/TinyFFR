// Created on 2026-02-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IAnimationKeyframe<out T> : ITimeKeyedItem where T : IInterpolatable<T> {
	static abstract T FallbackValue { get; }
	T Value { get; }
}

public readonly record struct AnimationTranslationKeyframe(float TimeKeySeconds, Vect Value) : IAnimationKeyframe<Vect> {
	public static Vect FallbackValue { get; } = Vect.Zero;
}
public readonly record struct AnimationScalingKeyframe(float TimeKeySeconds, Vect Value) : IAnimationKeyframe<Vect> {
	public static Vect FallbackValue { get; } = Vect.One;
}
public readonly record struct AnimationRotationKeyframe(float TimeKeySeconds, Rotation Value) : IAnimationKeyframe<Rotation> {
	public static Rotation FallbackValue { get; } = Rotation.None;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct AnimationChannelHeader(
	int BoneIndex,
	int TranslationKeyStart, int TranslationKeyCount,
	int RotationKeyStart, int RotationKeyCount,
	int ScalingKeyStart, int ScalingKeyCount
);
