// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

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
	readonly ArrayPoolBackedMap<ResourceHandle<ApplicationLoop>, HandleTrackingData> _handleDataMap = new();
#pragma warning disable CA2213 // Wants us to dispose _latestInputRetriever, but this is taken care of by the LocalInputManager
	readonly LocalLatestInputRetriever _latestInputRetriever;
#pragma warning restore CA2213
	nuint _nextLoopHandleIndex = 1;
	bool _isDisposed = false;

	public LocalApplicationLoopBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
		_latestInputRetriever = LocalInputManager.IncrementRefCountAndGetRetriever();
	}

	public ApplicationLoop CreateLoop(in LocalApplicationLoopConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var curTime = Stopwatch.GetTimestamp();
		var handle = (ResourceHandle<ApplicationLoop>) _nextLoopHandleIndex;
		_handleDataMap.Add(handle, new(config.MaxCpuBusyWaitTime, config.BaseConfig.FrameInterval, curTime, curTime, TimeSpan.Zero, config.IterationShouldRefreshGlobalInputStates));
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.BaseConfig.Name, DefaultLoopName);
		_nextLoopHandleIndex++;
		return new(handle, this);
	}

	public ILatestInputRetriever GetInputStateProvider(ResourceHandle<ApplicationLoop> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _latestInputRetriever;
	}

	TimeSpan GetWaitTimeUntilNextFrameStart(ResourceHandle<ApplicationLoop> handle) {
		var timeSinceLastIteration = Stopwatch.GetElapsedTime(_handleDataMap[handle].PreviousIterationStartTimestamp);
		var result = _handleDataMap[handle].FrameInterval - timeSinceLastIteration;
		return result > TimeSpan.Zero ? result : TimeSpan.Zero;
	}
	void ExecuteIteration(bool shouldIterateInput) {
		if (shouldIterateInput) _latestInputRetriever.IterateSystemWideInput();
	}

	public TimeSpan IterateOnce(ResourceHandle<ApplicationLoop> handle) {
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
	public bool TryIterateOnce(ResourceHandle<ApplicationLoop> handle, out TimeSpan outDeltaTime) {
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

	public TimeSpan GetTimeUntilNextIteration(ResourceHandle<ApplicationLoop> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return GetWaitTimeUntilNextFrameStart(handle);
	}
	public TimeSpan GetTotalIteratedTime(ResourceHandle<ApplicationLoop> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _handleDataMap[handle].TotalIteratedTime;
	}
	public void SetTotalIteratedTime(ResourceHandle<ApplicationLoop> handle, TimeSpan newValue) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_handleDataMap[handle] = _handleDataMap[handle] with { TotalIteratedTime = newValue };
	}
	public TimeSpan GetDesiredIterationInterval(ResourceHandle<ApplicationLoop> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _handleDataMap[handle].FrameInterval;
	}

	public string GetNameAsNewStringObject(ResourceHandle<ApplicationLoop> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultLoopName));
	}
	public int GetNameLength(ResourceHandle<ApplicationLoop> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultLoopName).Length;
	}
	public void CopyName(ResourceHandle<ApplicationLoop> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultLoopName, destinationBuffer);
	}

	public override string ToString() => _isDisposed ? "TinyFFR Local Application Loop Builder [Disposed]" : "TinyFFR Local Application Loop Builder";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	ApplicationLoop HandleToInstance(ResourceHandle<ApplicationLoop> h) => new(h, this);

	#region Disposal
	public void Dispose(ResourceHandle<ApplicationLoop> handle) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DisposeResourceNameIfExists(handle.Ident);
		_handleDataMap.Remove(handle);
	}
	public bool IsDisposed(ResourceHandle<ApplicationLoop> handle) {
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

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<ApplicationLoop> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(ApplicationLoop));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}