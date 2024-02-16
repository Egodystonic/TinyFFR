// Created on 2024-01-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Environment;

public readonly struct ApplicationLoop : IEquatable<ApplicationLoop>, ITrackedDisposable {
	readonly IApplicationLoopImplProvider _impl;
	internal ApplicationLoopHandle Handle { get; }

	public IInputTracker Input {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetInputTracker(Handle);
	}

	internal ApplicationLoop(ApplicationLoopHandle handle, IApplicationLoopImplProvider impl) {
		_impl = impl;
		Handle = handle;
	}

	public TimeSpan TotalIteratedTime {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetTotalIteratedTime(Handle);
	}

	public TimeSpan TimeUntilNextIteration {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetTimeUntilNextIteration(Handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // TODO make it clear here and in TryIterateOnce that the DeltaTime returned is the time since the last iteration, not the time it took to iterate
	public DeltaTime IterateOnce() => _impl.IterateOnce(Handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryIterateOnce(out DeltaTime outDeltaTime) => _impl.TryIterateOnce(Handle, out outDeltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _impl.Dispose(Handle);

	public bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.IsDisposed(Handle);
	}

	public bool Equals(ApplicationLoop other) => Handle.Equals(other.Handle);
	public override bool Equals(object? obj) => obj is ApplicationLoop other && Equals(other);
	public override int GetHashCode() => Handle.GetHashCode();
	public static bool operator ==(ApplicationLoop left, ApplicationLoop right) => left.Equals(right);
	public static bool operator !=(ApplicationLoop left, ApplicationLoop right) => !left.Equals(right);
}