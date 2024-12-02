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
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Environment.Local;

[SuppressUnmanagedCodeSecurity]
sealed class LocalApplicationLoopBuilder : ILocalApplicationLoopBuilder, IApplicationLoopImplProvider, IDisposable {
	readonly record struct HandleTrackingData(
		TimeSpan MaxCpuBusyWaitTime, 
		TimeSpan FrameInterval, 
		long PreviousIterationStartTimestamp, 
		long PreviousIterationReturnTimestamp, 
		TimeSpan TotalIteratedTime,
		bool ShouldIterateInput
	);

	const string DefaultLoopName = "Unnamed Loop";
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPoolBackedMap<ApplicationLoopHandle, HandleTrackingData> _handleDataMap = new();
	readonly LocalLatestInputRetriever _latestInputRetriever;
	readonly LocalApplicationLoopBuilderConfig _config;
	nuint _nextLoopHandleIndex = 1;
	bool _isDisposed = false;

	public LocalApplicationLoopBuilder(LocalFactoryGlobalObjectGroup globals, LocalApplicationLoopBuilderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_globals = globals;
		_config = config;
		_latestInputRetriever = LocalInputManager.IncrementRefCountAndGetRetriever();
	}

	public ApplicationLoop CreateLoop(int? frameRateCapHz = null, ReadOnlySpan<char> name = default) => CreateLoop(frameRateCapHz, null, name);
	public ApplicationLoop CreateLoop(int? frameRateCapHz = null, bool? waitForVsync = null, ReadOnlySpan<char> name = default) {
		return CreateLoop(new LocalApplicationLoopConfig {
			Name = name,
			FrameRateCapHz = frameRateCapHz,
			WaitForVSync = waitForVsync ?? LocalApplicationLoopConfig.DefaultWaitForVSync
		});
	}
	public ApplicationLoop CreateLoop(in ApplicationLoopConfig config) => CreateLoop(new LocalApplicationLoopConfig(config));
	public ApplicationLoop CreateLoop(in LocalApplicationLoopConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var curTime = Stopwatch.GetTimestamp();
		var handle = (ApplicationLoopHandle) _nextLoopHandleIndex;
		_handleDataMap.Add(handle, new(config.MaxCpuBusyWaitTime, config.BaseConfig.FrameInterval, curTime, curTime, TimeSpan.Zero, config.IterationShouldRefreshGlobalInputStates));
		_globals.StoreResourceNameIfNotDefault(handle.Ident, config.BaseConfig.Name);
		_nextLoopHandleIndex++;
		return new(handle, this);
	}

	public ILatestInputRetriever GetInputStateProvider(ApplicationLoopHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _latestInputRetriever;
	}

	TimeSpan GetWaitTimeUntilNextFrameStart(ApplicationLoopHandle handle) {
		var timeSinceLastIteration = Stopwatch.GetElapsedTime(_handleDataMap[handle].PreviousIterationStartTimestamp);
		var result = _handleDataMap[handle].FrameInterval - timeSinceLastIteration;
		return result > TimeSpan.Zero ? result : TimeSpan.Zero;
	}
	void ExecuteIteration(bool shouldIterateInput) {
		if (shouldIterateInput) _latestInputRetriever.IterateSystemWideInput();
	}

	public TimeSpan IterateOnce(ApplicationLoopHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);

		var waitTime = GetWaitTimeUntilNextFrameStart(handle);
		var maxCpuBusyWaitTime = _handleDataMap[handle].MaxCpuBusyWaitTime;
		if (waitTime > maxCpuBusyWaitTime) {
			Thread.Sleep(waitTime - maxCpuBusyWaitTime);
		}
		while (GetWaitTimeUntilNextFrameStart(handle) > TimeSpan.Zero) { }

		_handleDataMap[handle] = _handleDataMap[handle] with { PreviousIterationStartTimestamp = Stopwatch.GetTimestamp() };
		ExecuteIteration(_handleDataMap[handle].ShouldIterateInput);

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
		ExecuteIteration(_handleDataMap[handle].ShouldIterateInput);

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

	public ReadOnlySpan<char> GetName(ApplicationLoopHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultLoopName);
	}

	public override string ToString() => _isDisposed ? "TinyFFR Local Application Loop Builder [Disposed]" : "TinyFFR Local Application Loop Builder";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	ApplicationLoop HandleToInstance(ApplicationLoopHandle h) => new(h, this);

	#region Disposal
	public void Dispose(ApplicationLoopHandle handle) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DisposeResourceNameIfExists(handle.Ident);
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
			LocalInputManager.DecrementRefCount();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ApplicationLoopHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(ApplicationLoop));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}