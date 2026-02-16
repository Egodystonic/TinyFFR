// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly record struct MeshAnimation {
	// TODO name
	readonly SkeletalAnimationData? _skeletalAnimationData;
	readonly MorphingAnimationData? _morphingAnimationData;
	
	public float DefaultCompletionTimeSeconds => _skeletalAnimationData?.DefaultCompletionTimeSeconds ?? _morphingAnimationData?.DefaultCompletionTimeSeconds ?? throw InvalidObjectException.InvalidDefault<MeshAnimation>(); 

	internal MeshAnimation(SkeletalAnimationData skeletalAnimationData) => _skeletalAnimationData = skeletalAnimationData;
	internal MeshAnimation(MorphingAnimationData morphingAnimationData) => _morphingAnimationData = morphingAnimationData;
	
	public float GetTargetTimeCoefficientForAlteredCompletionTime(float desiredAnimationCompletionTimeSeconds) {
		var result = DefaultCompletionTimeSeconds / desiredAnimationCompletionTimeSeconds;
		if (Single.IsNaN(result) || !Single.IsFinite(result)) return 1f;
		return result;
	}
	
	public void ApplyLooped(ModelInstance instance, float targetTimePointSeconds, float desiredAnimationCompletionTimeSeconds) {
		ApplyLooped(instance, GetTargetTimeCoefficientForAlteredCompletionTime(desiredAnimationCompletionTimeSeconds) * targetTimePointSeconds);	
	} 
	public void ApplyLooped(ModelInstance instance, float targetTimePointSeconds) => Apply(instance, MathUtils.TrueModulus(targetTimePointSeconds, DefaultCompletionTimeSeconds));
	
	public void ApplyClamped(ModelInstance instance, float targetTimePointSeconds, float desiredAnimationCompletionTimeSeconds) {
		ApplyClamped(instance, GetTargetTimeCoefficientForAlteredCompletionTime(desiredAnimationCompletionTimeSeconds) * targetTimePointSeconds);	
	} 
	public void ApplyClamped(ModelInstance instance, float targetTimePointSeconds) => Apply(instance, ((Real) targetTimePointSeconds).Clamp(0f, DefaultCompletionTimeSeconds));
	
	public void Apply(ModelInstance instance, float targetTimePointSeconds) {
		
	}
}