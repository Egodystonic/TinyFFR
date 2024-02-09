// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Environment;

[SuppressUnmanagedCodeSecurity]
sealed class NativeApplicationLoopBuilder : IApplicationLoopBuilder, IApplicationLoopImplProvider, IDisposable {
	static ApplicationLoopConfig? _activeLoopConfig = null;
	static ApplicationLoopHandle? _activeLoopHandle = null;
	readonly NativeInputTracker _inputTracker;
	int _nextLoopHandleIndex = 1;
	long _previousIterationStartTimestamp;
	long _previousIterationReturnTimestamp;
	bool _isDisposed = false;

	public NativeApplicationLoopBuilder(ApplicationLoopBuilderConfig config) {
		_inputTracker = new(config.InputTrackerConfig);
	}

	public ApplicationLoop BuildLoop() => BuildLoop(new());
	public ApplicationLoop BuildLoop(in ApplicationLoopConfig config) {
		ThrowIfThisIsDisposed();
		if (_activeLoopHandle != null) throw new InvalidOperationException($"Only one {nameof(ApplicationLoop)} at a time may be built. Dispose the previous instance before building another.");
		config.ThrowIfInvalid();

		_activeLoopHandle = new(_nextLoopHandleIndex++);
		_activeLoopConfig = config;
		_previousIterationReturnTimestamp = _previousIterationStartTimestamp = Stopwatch.GetTimestamp();
		return new(_activeLoopHandle.Value, this);
	}

	TimeSpan GetWaitTimeUntilNextFrameStart() {
		var timeSinceLastIteration = Stopwatch.GetElapsedTime(_previousIterationStartTimestamp);
		var result = _activeLoopConfig!.Value.FrameInterval - timeSinceLastIteration;
		return result > TimeSpan.Zero ? result : TimeSpan.Zero;
	}
	void ExecuteIteration() {
		_inputTracker.ExecuteIteration();
	}

	public DeltaTime IterateOnce(ApplicationLoopHandle handle) {
		ThrowIfHandleOrThisIsDisposed(handle);

		var waitTime = GetWaitTimeUntilNextFrameStart();
		if (waitTime > _activeLoopConfig!.Value.MaxCpuBusyWaitTime) {
			Thread.Sleep(waitTime - _activeLoopConfig!.Value.MaxCpuBusyWaitTime);
		}
		while (GetWaitTimeUntilNextFrameStart() > TimeSpan.Zero) { }

		_previousIterationStartTimestamp = Stopwatch.GetTimestamp();
		ExecuteIteration();

		var result = Stopwatch.GetElapsedTime(_previousIterationReturnTimestamp);
		_previousIterationReturnTimestamp = Stopwatch.GetTimestamp();
		return result;
	}
	public bool TryIterateOnce(ApplicationLoopHandle handle, out DeltaTime outDeltaTime) {
		ThrowIfHandleOrThisIsDisposed(handle);
		
		if (GetWaitTimeUntilNextFrameStart() > TimeSpan.Zero) {
			outDeltaTime = default;
			return false;
		}

		_previousIterationStartTimestamp = Stopwatch.GetTimestamp();
		ExecuteIteration();

		outDeltaTime = Stopwatch.GetElapsedTime(_previousIterationReturnTimestamp);
		_previousIterationReturnTimestamp = Stopwatch.GetTimestamp();
		return true;
	}

	public TimeSpan GetTimeUntilNextIteration(ApplicationLoopHandle handle) {
		ThrowIfHandleOrThisIsDisposed(handle);
		return GetWaitTimeUntilNextFrameStart();
	}

	public void Dispose(ApplicationLoopHandle handle) {
		if (_activeLoopHandle != handle) return;
		_activeLoopHandle = null;
		_activeLoopConfig = null;
	}
	public bool IsDisposed(ApplicationLoopHandle handle) {
		return _activeLoopHandle != handle;
	}

	public IInputTracker GetInputTracker(ApplicationLoopHandle handle) {
		ThrowIfHandleOrThisIsDisposed(handle);
		return _inputTracker;
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			if (_activeLoopHandle != null) Dispose(_activeLoopHandle.Value);
			_inputTracker.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
	void ThrowIfHandleOrThisIsDisposed(ApplicationLoopHandle handle) {
		ObjectDisposedException.ThrowIf(IsDisposed(handle), handle);
		ThrowIfThisIsDisposed();
	}
}