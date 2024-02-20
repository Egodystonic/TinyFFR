// Created on 2024-01-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Environment;

public readonly struct ApplicationLoop : IEquatable<ApplicationLoop>, ITrackedDisposable {
	readonly ApplicationLoopHandle _handle;

	public IInputTracker Input {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => NativeApplicationLoopBuilder.GetInputTracker(_handle);
	}

	internal ApplicationLoop(ApplicationLoopHandle handle) {
		_handle = handle;
	}

	public TimeSpan TotalIteratedTime {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => NativeApplicationLoopBuilder.GetTotalIteratedTime(_handle);
	}

	public TimeSpan TimeUntilNextIteration {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => NativeApplicationLoopBuilder.GetTimeUntilNextIteration(_handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // TODO make it clear here and in TryIterateOnce that the DeltaTime returned is the time since the last iteration, not the time it took to iterate
	public DeltaTime IterateOnce() => NativeApplicationLoopBuilder.IterateOnce(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryIterateOnce(out DeltaTime outDeltaTime) => NativeApplicationLoopBuilder.TryIterateOnce(_handle, out outDeltaTime);

	public override string ToString() => $"Application Loop #{_handle}";

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => NativeApplicationLoopBuilder.Dispose(_handle);

	public bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => NativeApplicationLoopBuilder.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	public bool Equals(ApplicationLoop other) => _handle.Equals(other._handle);
	public override bool Equals(object? obj) => obj is ApplicationLoop other && Equals(other);
	public override int GetHashCode() => _handle.GetHashCode();
	public static bool operator ==(ApplicationLoop left, ApplicationLoop right) => left.Equals(right);
	public static bool operator !=(ApplicationLoop left, ApplicationLoop right) => !left.Equals(right);
	#endregion
}