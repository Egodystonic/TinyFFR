// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Environment.Desktop;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Environment.Loop;

[SuppressUnmanagedCodeSecurity]
sealed class NativeApplicationLoopBuilder : IApplicationLoopBuilder, IApplicationLoopImplProvider, IDisposable {
	readonly NativeInputTracker _inputTracker = new();
	int _nextLoopHandleIndex = 1;
	ApplicationLoopCreationConfig? _activeLoopConfig = null;
	ApplicationLoopHandle? _activeLoopHandle = null;
	long? _lastIterationTimestamp = null;
	bool _isDisposed = false;

	public ApplicationLoop BuildLoop() => BuildLoop(new());
	public ApplicationLoop BuildLoop(in ApplicationLoopCreationConfig config) {
		ThrowIfThisIsDisposed();
		if (_activeLoopHandle != null) throw new InvalidOperationException($"Only one {nameof(ApplicationLoop)} at a time may be built. Dispose the previous instance before building another.");
		config.ThrowIfInvalid();

		_activeLoopHandle = new(_nextLoopHandleIndex++);
		_activeLoopConfig = config;
		_lastIterationTimestamp = null;
		return new(_activeLoopHandle.Value, this);
	}

	TimeSpan GetTimeUntilNextFrame() {
		if (_lastIterationTimestamp == null) return TimeSpan.Zero;

		var timeSinceLastIteration = Stopwatch.GetElapsedTime(_lastIterationTimestamp.Value);
		var result = _activeLoopConfig!.Value.FrameInterval - timeSinceLastIteration;
		return result > TimeSpan.Zero ? result : TimeSpan.Zero;
	}
	TimeSpan GetTimeSinceLastFrame() {
		if (_lastIterationTimestamp != null) return Stopwatch.GetElapsedTime(_lastIterationTimestamp.Value);
		return _activeLoopConfig!.Value.FrameInterval;
	}
	void ExecuteIteration() {
		_inputTracker.ExecuteIteration();
	}
	public DeltaTime IterateOnce(ApplicationLoopHandle handle) {
		ThrowIfHandleOrThisIsDisposed(handle);

		var waitTime = GetTimeUntilNextFrame();
		while (waitTime > TimeSpan.Zero) Thread.Sleep(waitTime);

		ExecuteIteration();

		return (float) GetTimeSinceLastFrame().TotalSeconds;
	}
	public DeltaTime? TryIterateOnce(ApplicationLoopHandle handle) {
		ThrowIfHandleOrThisIsDisposed(handle);

		if (GetTimeUntilNextFrame() > TimeSpan.Zero) return null;

		ExecuteIteration();

		return (float) GetTimeSinceLastFrame().TotalSeconds;
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
		_inputTracker.Dispose();
	}

	void ThrowIfThisIsDisposed() {
		if (_isDisposed) throw new InvalidOperationException("Builder has been disposed.");
	}
	void ThrowIfHandleOrThisIsDisposed(ApplicationLoopHandle handle) {
		if (IsDisposed(handle)) throw new InvalidOperationException($"{nameof(ApplicationLoop)} has been disposed.");
		ThrowIfThisIsDisposed();
	}
}