// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Environment;

[SuppressUnmanagedCodeSecurity]
sealed class NativeApplicationLoopBuilder : IApplicationLoopBuilder, IDisposable {
	static NativeApplicationLoopBuilder? _instance;
	readonly NativeInputTracker _inputTracker;
	ApplicationLoopConfig? _activeLoopConfig = null;
	ApplicationLoopHandle? _activeLoopHandle = null;
	int _nextLoopHandleIndex = 1;
	long _previousIterationStartTimestamp;
	long _previousIterationReturnTimestamp;
	TimeSpan _totalIteratedTime;
	bool _isDisposed = false;

	public NativeApplicationLoopBuilder() {
		_instance = this;
		_inputTracker = new();
	}

	public ApplicationLoop BuildLoop() => BuildLoop(new());
	public ApplicationLoop BuildLoop(in ApplicationLoopConfig config) {
		ThrowIfThisIsDisposed();
		if (_activeLoopHandle != null) throw new InvalidOperationException($"Only one {nameof(ApplicationLoop)} at a time may be built. Dispose the previous instance before building another.");
		config.ThrowIfInvalid();

		_activeLoopHandle = _nextLoopHandleIndex++;
		_activeLoopConfig = config;
		_previousIterationReturnTimestamp = _previousIterationStartTimestamp = Stopwatch.GetTimestamp();
		_totalIteratedTime = TimeSpan.Zero;
		return new(_activeLoopHandle.Value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IInputTracker GetInputTracker(ApplicationLoopHandle handle) => GetInstanceOrThrow().InstanceGetInputTracker(handle);
	NativeInputTracker InstanceGetInputTracker(ApplicationLoopHandle handle) {
		ThrowIfHandleIsDisposed(handle);
		return _inputTracker;
	}

	TimeSpan GetWaitTimeUntilNextFrameStart() {
		var timeSinceLastIteration = Stopwatch.GetElapsedTime(_previousIterationStartTimestamp);
		var result = _activeLoopConfig!.Value.FrameInterval - timeSinceLastIteration;
		return result > TimeSpan.Zero ? result : TimeSpan.Zero;
	}
	void ExecuteIteration() {
		_inputTracker.ExecuteIteration();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DeltaTime IterateOnce(ApplicationLoopHandle handle) => GetInstanceOrThrow().InstanceIterateOnce(handle);
	DeltaTime InstanceIterateOnce(ApplicationLoopHandle handle) {
		ThrowIfHandleIsDisposed(handle);

		var waitTime = GetWaitTimeUntilNextFrameStart();
		if (waitTime > _activeLoopConfig!.Value.MaxCpuBusyWaitTime) {
			Thread.Sleep(waitTime - _activeLoopConfig!.Value.MaxCpuBusyWaitTime);
		}
		while (GetWaitTimeUntilNextFrameStart() > TimeSpan.Zero) { }

		_previousIterationStartTimestamp = Stopwatch.GetTimestamp();
		ExecuteIteration();

		var result = Stopwatch.GetElapsedTime(_previousIterationReturnTimestamp);
		_previousIterationReturnTimestamp = Stopwatch.GetTimestamp();
		_totalIteratedTime += result;
		return result;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryIterateOnce(ApplicationLoopHandle handle, out DeltaTime outDeltaTime) => GetInstanceOrThrow().InstanceTryIterateOnce(handle, out outDeltaTime);
	bool InstanceTryIterateOnce(ApplicationLoopHandle handle, out DeltaTime outDeltaTime) {
		ThrowIfHandleIsDisposed(handle);
		
		if (GetWaitTimeUntilNextFrameStart() > TimeSpan.Zero) {
			outDeltaTime = default;
			return false;
		}

		_previousIterationStartTimestamp = Stopwatch.GetTimestamp();
		ExecuteIteration();

		var dt = Stopwatch.GetElapsedTime(_previousIterationReturnTimestamp);
		_previousIterationReturnTimestamp = Stopwatch.GetTimestamp();
		_totalIteratedTime += dt;
		outDeltaTime = dt;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TimeSpan GetTimeUntilNextIteration(ApplicationLoopHandle handle) => GetInstanceOrThrow().InstanceGetTimeUntilNextIteration(handle);
	TimeSpan InstanceGetTimeUntilNextIteration(ApplicationLoopHandle handle) {
		ThrowIfHandleIsDisposed(handle);
		return GetWaitTimeUntilNextFrameStart();
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TimeSpan GetTotalIteratedTime(ApplicationLoopHandle handle) => GetInstanceOrThrow().InstanceGetTotalIteratedTime(handle);
	TimeSpan InstanceGetTotalIteratedTime(ApplicationLoopHandle handle) {
		ThrowIfHandleIsDisposed(handle);
		return _totalIteratedTime;
	}

	public override string ToString() => "TinyFFR Native Application Loop Builder";

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Dispose(ApplicationLoopHandle handle) => GetInstanceOrThrow().InstanceDispose(handle);
	void InstanceDispose(ApplicationLoopHandle handle) {
		if (_activeLoopHandle != handle) return;
		_activeLoopHandle = null;
		_activeLoopConfig = null;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDisposed(ApplicationLoopHandle handle) => GetInstanceOrThrow().InstanceIsDisposed(handle);
	bool InstanceIsDisposed(ApplicationLoopHandle handle) {
		return _activeLoopHandle != handle;
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			if (_activeLoopHandle != null) Dispose(_activeLoopHandle.Value);
			_inputTracker.Dispose();
		}
		finally {
			_isDisposed = true;
			_instance = null;
		}
	}

	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	static void ThrowIfHandleIsDisposed(ApplicationLoopHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(ApplicationLoop));
	static NativeApplicationLoopBuilder GetInstanceOrThrow() {
		ObjectDisposedException.ThrowIf(_instance == null, typeof(NativeApplicationLoopBuilder));
		return _instance;
	}
	#endregion
}