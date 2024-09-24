// Created on 2024-01-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;

namespace Egodystonic.TinyFFR.Environment;

public readonly struct ApplicationLoop : IEquatable<ApplicationLoop>, IDisposable {
	readonly ApplicationLoopHandle _handle;
	readonly IApplicationLoopImplProvider _impl;

	internal IApplicationLoopImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ApplicationLoop>();
	internal ApplicationLoopHandle Handle => _handle;

	public IInputTracker Input {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetInputTracker(_handle);
	}

	internal ApplicationLoop(ApplicationLoopHandle handle, IApplicationLoopImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	public TimeSpan TotalIteratedTime {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTotalIteratedTime(_handle);
	}

	public TimeSpan TimeUntilNextIteration {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTimeUntilNextIteration(_handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // TODO make it clear here and in TryIterateOnce that the DeltaTime returned is the time since the last iteration, not the time it took to iterate
	public TimeSpan IterateOnce() => Implementation.IterateOnce(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryIterateOnce(out TimeSpan outDeltaTime) => Implementation.TryIterateOnce(_handle, out outDeltaTime);

	public override string ToString() => $"Application Loop #{_handle}{(IsDisposed ? " (Disposed)" : "")}";

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	public bool Equals(ApplicationLoop other) => _handle.Equals(other._handle) && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is ApplicationLoop other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(ApplicationLoop left, ApplicationLoop right) => left.Equals(right);
	public static bool operator !=(ApplicationLoop left, ApplicationLoop right) => !left.Equals(right);
	#endregion
}