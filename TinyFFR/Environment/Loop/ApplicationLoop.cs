// Created on 2024-01-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment;

public readonly struct ApplicationLoop : IDisposableResource<ApplicationLoop, IApplicationLoopImplProvider> {
	readonly ResourceHandle<ApplicationLoop> _handle;
	readonly IApplicationLoopImplProvider _impl;

	internal IApplicationLoopImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ApplicationLoop>();
	internal ResourceHandle<ApplicationLoop> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ApplicationLoop)) : _handle;

	IApplicationLoopImplProvider IResource<ApplicationLoop, IApplicationLoopImplProvider>.Implementation => Implementation;
	ResourceHandle<ApplicationLoop> IResource<ApplicationLoop>.Handle => Handle;

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	public ILatestInputRetriever Input {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetInputStateProvider(_handle);
	}

	internal ApplicationLoop(ResourceHandle<ApplicationLoop> handle, IApplicationLoopImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	static ApplicationLoop IResource<ApplicationLoop>.CreateFromHandleAndImpl(ResourceHandle<ApplicationLoop> handle, IResourceImplProvider impl) {
		return new ApplicationLoop(handle, impl as IApplicationLoopImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	public TimeSpan TotalIteratedTime {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTotalIteratedTime(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetTotalIteratedTime(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTotalIteratedTime(TimeSpan newValue) => TotalIteratedTime = newValue;

	public TimeSpan TimeUntilNextIteration {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTimeUntilNextIteration(_handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // TODO make it clear here and in TryIterateOnce that the DeltaTime returned is the time since the last iteration, not the time it took to iterate
	public TimeSpan IterateOnce() => Implementation.IterateOnce(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryIterateOnce(out TimeSpan outDeltaTime) => Implementation.TryIterateOnce(_handle, out outDeltaTime);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ResetTotalIteratedTime() => TotalIteratedTime = TimeSpan.Zero;

	public override string ToString() => $"Application Loop {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
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