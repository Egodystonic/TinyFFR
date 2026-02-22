// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct MeshAnimation : IResource<MeshAnimation, IMeshAnimationImplProvider> {
	readonly ResourceHandle<MeshAnimation> _handle;
	readonly IMeshAnimationImplProvider _impl;

	internal ResourceHandle<MeshAnimation> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(MeshAnimation)) : _handle;
	internal IMeshAnimationImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<MeshAnimation>();

	IMeshAnimationImplProvider IResource<MeshAnimation, IMeshAnimationImplProvider>.Implementation => Implementation;
	ResourceHandle<MeshAnimation> IResource<MeshAnimation>.Handle => Handle;
	
	public MeshAnimationType Type {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetType(_handle);
	}
	
	public float DefaultCompletionTimeSeconds {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDefaultCompletionTimeSeconds(_handle);
	}

	internal MeshAnimation(ResourceHandle<MeshAnimation> handle, IMeshAnimationImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}
	
	public float GetTargetTimeCoefficientForDesiredCompletionTime(float desiredAnimationCompletionTimeSeconds) {
		var result = DefaultCompletionTimeSeconds / desiredAnimationCompletionTimeSeconds;
		if (Single.IsNaN(result) || !Single.IsFinite(result)) return 1f;
		return result;
	}
	
	public void ApplyLooped(ModelInstance targetInstance, float targetTimePointSeconds, float desiredAnimationCompletionTimeSeconds) {
		ApplyLooped(targetInstance, GetTargetTimeCoefficientForDesiredCompletionTime(desiredAnimationCompletionTimeSeconds) * targetTimePointSeconds);	
	} 
	public void ApplyLooped(ModelInstance targetInstance, float targetTimePointSeconds) => Apply(targetInstance, MathUtils.TrueModulus(targetTimePointSeconds, DefaultCompletionTimeSeconds));
	
	public void ApplyLoopedWithPingPong(ModelInstance targetInstance, float targetTimePointSeconds, float desiredAnimationCompletionTimeSeconds) {
		ApplyLoopedWithPingPong(targetInstance, GetTargetTimeCoefficientForDesiredCompletionTime(desiredAnimationCompletionTimeSeconds) * targetTimePointSeconds);	
	} 
	public void ApplyLoopedWithPingPong(ModelInstance targetInstance, float targetTimePointSeconds) {
		Apply(targetInstance, Angle.FromRadians(targetTimePointSeconds).TriangularizeRectified(Angle.FromRadians(DefaultCompletionTimeSeconds)).Radians);
	}

	public void ApplyClamped(ModelInstance targetInstance, float targetTimePointSeconds, float desiredAnimationCompletionTimeSeconds) {
		ApplyClamped(targetInstance, GetTargetTimeCoefficientForDesiredCompletionTime(desiredAnimationCompletionTimeSeconds) * targetTimePointSeconds);	
	} 
	public void ApplyClamped(ModelInstance targetInstance, float targetTimePointSeconds) => Apply(targetInstance, ((Real) targetTimePointSeconds).Clamp(0f, DefaultCompletionTimeSeconds));
	
	public void ApplyClampedWithPingPong(ModelInstance targetInstance, float targetTimePointSeconds, float desiredAnimationCompletionTimeSeconds) {
		ApplyClampedWithPingPong(targetInstance, GetTargetTimeCoefficientForDesiredCompletionTime(desiredAnimationCompletionTimeSeconds) * targetTimePointSeconds);	
	} 
	public void ApplyClampedWithPingPong(ModelInstance targetInstance, float targetTimePointSeconds) {
		var clampedTargetTime = ((Real) targetTimePointSeconds).Clamp(0f, DefaultCompletionTimeSeconds * 2f); 
		ApplyLoopedWithPingPong(targetInstance, clampedTargetTime);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Apply(ModelInstance targetInstance, float targetTimePointSeconds) {
		Implementation.Apply(_handle, targetInstance, targetTimePointSeconds);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static MeshAnimation IResource<MeshAnimation>.CreateFromHandleAndImpl(ResourceHandle<MeshAnimation> handle, IResourceImplProvider impl) {
		return new MeshAnimation(handle, impl as IMeshAnimationImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	
	#region Disposal
	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion
	
	public override string ToString() => $"Mesh Animation {GetNameAsNewStringObject()}";

	#region Equality
	public bool Equals(MeshAnimation other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is MeshAnimation other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(MeshAnimation left, MeshAnimation right) => left.Equals(right);
	public static bool operator !=(MeshAnimation left, MeshAnimation right) => !left.Equals(right);
	#endregion
}