// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Local;

[SuppressUnmanagedCodeSecurity]
sealed class LocalApplicationLoopBuilder : ILocalApplicationLoopBuilder, IApplicationLoopImplProvider, IDisposable {
	readonly record struct HandleTrackingData(LocalApplicationLoopConfig Config, long PreviousIterationStartTimestamp, long PreviousIterationReturnTimestamp, TimeSpan TotalIteratedTime);

	readonly ArrayPoolBackedMap<ApplicationLoopHandle, HandleTrackingData> _handleDataMap = new();
	readonly LocalInputTracker _inputTracker = new();
	readonly LocalApplicationLoopBuilderConfig _config;
	ApplicationLoopHandle _nextLoopHandleIndex = 1;
	bool _isDisposed = false;

	public LocalApplicationLoopBuilder(LocalApplicationLoopBuilderConfig config) {
		ArgumentNullException.ThrowIfNull(config);
		_config = config;
	}

	public ApplicationLoop BuildLoop() => BuildLoop(new LocalApplicationLoopConfig());
	public ApplicationLoop BuildLoop(in ApplicationLoopConfig config) => BuildLoop(new LocalApplicationLoopConfig(config));
	public ApplicationLoop BuildLoop(in LocalApplicationLoopConfig config) {
		ThrowIfThisIsDisposed();
		if (!_config.AllowMultipleSimultaneousLoops && _handleDataMap.Count > 0) {
			throw new InvalidOperationException(
				"This loop builder is not configured to allow multiple simultaneous loops. In most applications, having more than one loop active " +
				"is an error and will lead to buggy behaviour. If you intend to replace the previously-created loop with a new one, dispose the previous " +
				"loop first before building the subsequent one. If you actually wish to have multiple loops active simultaneously, set " +
				nameof(LocalApplicationLoopBuilderConfig.AllowMultipleSimultaneousLoops) + " to 'true' in the " + nameof(LocalApplicationLoopBuilderConfig) + " " +
				"that is provided to the " + nameof(LocalRendererFactory) + " constructor."
			);
		}
		config.ThrowIfInvalid();

		var curTime = Stopwatch.GetTimestamp();
		_handleDataMap.Add(_nextLoopHandleIndex, new(config, curTime, curTime, TimeSpan.Zero));
		return new(_nextLoopHandleIndex++, this);
	}

	public IInputTracker GetInputTracker(ApplicationLoopHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _inputTracker;
	}

	TimeSpan GetWaitTimeUntilNextFrameStart(ApplicationLoopHandle handle) {
		var timeSinceLastIteration = Stopwatch.GetElapsedTime(_handleDataMap[handle].PreviousIterationStartTimestamp);
		var result = _handleDataMap[handle].Config.BaseConfig.FrameInterval - timeSinceLastIteration;
		return result > TimeSpan.Zero ? result : TimeSpan.Zero;
	}
	void ExecuteIteration() {
		_inputTracker.ExecuteIteration();
	}

	public TimeSpan IterateOnce(ApplicationLoopHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);

		var waitTime = GetWaitTimeUntilNextFrameStart(handle);
		var maxCpuBusyWaitTime = _handleDataMap[handle].Config.MaxCpuBusyWaitTime;
		if (waitTime > maxCpuBusyWaitTime) {
			Thread.Sleep(waitTime - maxCpuBusyWaitTime);
		}
		while (GetWaitTimeUntilNextFrameStart(handle) > TimeSpan.Zero) { }

		_handleDataMap[handle] = _handleDataMap[handle] with { PreviousIterationStartTimestamp = Stopwatch.GetTimestamp() };
		ExecuteIteration();

		var dt = Stopwatch.GetElapsedTime(_handleDataMap[handle].PreviousIterationReturnTimestamp);
		_handleDataMap[handle] = _handleDataMap[handle] with {
			PreviousIterationReturnTimestamp = Stopwatch.GetTimestamp(),
			TotalIteratedTime = _handleDataMap[handle].TotalIteratedTime + dt
		};
		return dt;
	}
	public bool TryIterateOnce(ApplicationLoopHandle handle, out TimeSpan outDeltaTime) {
		ThrowIfThisOrHandleIsDisposed(handle);

		if (GetWaitTimeUntilNextFrameStart(handle) > TimeSpan.Zero) {
			outDeltaTime = default;
			return false;
		}

		_handleDataMap[handle] = _handleDataMap[handle] with { PreviousIterationStartTimestamp = Stopwatch.GetTimestamp() };
		ExecuteIteration();

		var dt = Stopwatch.GetElapsedTime(_handleDataMap[handle].PreviousIterationReturnTimestamp);
		_handleDataMap[handle] = _handleDataMap[handle] with {
			PreviousIterationReturnTimestamp = Stopwatch.GetTimestamp(),
			TotalIteratedTime = _handleDataMap[handle].TotalIteratedTime + dt
		};
		outDeltaTime = dt;
		return true;
	}

	public TimeSpan GetTimeUntilNextIteration(ApplicationLoopHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return GetWaitTimeUntilNextFrameStart(handle);
	}
	public TimeSpan GetTotalIteratedTime(ApplicationLoopHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _handleDataMap[handle].TotalIteratedTime;
	}

	public override string ToString() => _isDisposed ? "TinyFFR Local Application Loop Builder [Disposed]" : "TinyFFR Local Application Loop Builder";

	#region Disposal
	public void Dispose(ApplicationLoopHandle handle) {
		if (IsDisposed(handle)) return;
		_handleDataMap.Remove(handle);
	}
	public bool IsDisposed(ApplicationLoopHandle handle) {
		return _isDisposed || !_handleDataMap.ContainsKey(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _handleDataMap) Dispose(kvp.Key);
			_handleDataMap.Dispose();
			_inputTracker.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	void ThrowIfThisOrHandleIsDisposed(ApplicationLoopHandle handle) {
		ThrowIfThisIsDisposed();
		ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(ApplicationLoop));
	}
	#endregion
}