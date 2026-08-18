// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Threading;

interface IAsyncOperationTrackingData {
	ulong CycleCount { get; }
	bool GetIsCompleted(ulong cycleCount);
	bool WaitForCompletion(ulong cycleCount, TimeSpan timeout, CancellationToken cancellationToken);
}

public readonly record struct TinyFfrAsyncOperation {
	internal const int SmuggleSizeBytes = 16;
	internal IAsyncOperationTrackingData TrackingData { get; }
	internal ulong CycleCount { get; }

	public bool IsCompleted {
		get {
			if (TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
			return TrackingData.GetIsCompleted(CycleCount);
		}
	}

	internal TinyFfrAsyncOperation(IAsyncOperationTrackingData trackingData, ulong cycleCount) {
		TrackingData = trackingData;
		CycleCount = cycleCount;
	}

	public void WaitForCompletion() => WaitForCompletion(Timeout.InfiniteTimeSpan, default);
	public bool WaitForCompletion(TimeSpan timeout) => WaitForCompletion(timeout, default);
	public void WaitForCompletion(CancellationToken cancellationToken) => WaitForCompletion(Timeout.InfiniteTimeSpan, cancellationToken);
	public bool WaitForCompletion(TimeSpan timeout, CancellationToken cancellationToken) {
		if (TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
		return TrackingData.WaitForCompletion(CycleCount, timeout, cancellationToken);
	}

	public static void WaitForAllToComplete(params ReadOnlySpan<TinyFfrAsyncOperation> operations) => WaitForAllToComplete(Timeout.InfiniteTimeSpan, default, operations);
	public static bool WaitForAllToComplete(TimeSpan timeout, params ReadOnlySpan<TinyFfrAsyncOperation> operations) => WaitForAllToComplete(timeout, default, operations);
	public static void WaitForAllToComplete(CancellationToken cancellationToken, params ReadOnlySpan<TinyFfrAsyncOperation> operations) => WaitForAllToComplete(Timeout.InfiniteTimeSpan, cancellationToken, operations);
	public static bool WaitForAllToComplete(TimeSpan timeout, CancellationToken cancellationToken, params ReadOnlySpan<TinyFfrAsyncOperation> operations) {
		var startTimestamp = Stopwatch.GetTimestamp();
		var hasTimeout = timeout >= TimeSpan.Zero;

		for (var i = 0; i < operations.Length; ++i) {
			var remainingTime = timeout;
			if (hasTimeout) {
				remainingTime = timeout - Stopwatch.GetElapsedTime(startTimestamp);
				if (remainingTime < TimeSpan.Zero) return false;
			}
			if (!operations[i].WaitForCompletion(remainingTime, cancellationToken)) return false;
		}

		return true;
	}
}

public readonly unsafe record struct TinyFfrAsyncOperation<T> {
#pragma warning disable CA1001 // "Should be disposable" -- These objects live the lifetime of the application
	sealed class AsyncOperationTrackingData : IAsyncOperationTrackingData {
#pragma warning restore CA1001
		readonly Lock _lock = new();
		readonly ManualResetEventSlim _completionIndicator = new(false);
		IPrimaryThreadDispatcher _primaryThreadDispatcher = null!;
		ulong _currentCycleCount;
		T _result = default!;
		Exception? _exception;

		public ulong CycleCount {
			get {
				lock (_lock) return _currentCycleCount;
			}
		}

		public AsyncOperationTrackingData() {
			lock (_lock) Clear();
		}

		void ThrowIfIncorrectCycleCount(ulong cycleCount) {
			Debug.Assert(_lock.IsHeldByCurrentThread);
			if (cycleCount != _currentCycleCount) {
				throw new InvalidOperationException($"Cycle counts do not match (this is a concurrency failure regarding {nameof(TinyFfrAsyncOperation)}).");
			}
		}

		void Clear() {
			Debug.Assert(_lock.IsHeldByCurrentThread);
			_result = default!;
			_exception = null;
			_primaryThreadDispatcher = null!;
			_completionIndicator.Reset();
			++_currentCycleCount;
		}

		public void Reset(IPrimaryThreadDispatcher primaryThreadDispatcher) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			lock (_lock) {
				Clear();
				_primaryThreadDispatcher = primaryThreadDispatcher;
			}
		}

		public bool GetIsCompleted(ulong cycleCount) {
			lock (_lock) {
				if (cycleCount != _currentCycleCount) {
					throw new InvalidOperationException($"It is not permitted to check {nameof(IsCompleted)} after invoking {nameof(GetResultAndDisposeOperation)}.");
				}
				return _completionIndicator.IsSet;
			}
		}

		public void SetResult(ulong cycleCount, T result) {
			IPrimaryThreadDispatcher? dispatcher;
			lock (_lock) {
				ThrowIfIncorrectCycleCount(cycleCount);
				if (_completionIndicator.IsSet) return;
				_result = result;
				_completionIndicator.Set();
				dispatcher = _primaryThreadDispatcher;
			}
			dispatcher?.NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
		}

		public void SetException(ulong cycleCount, Exception error) {
			IPrimaryThreadDispatcher? dispatcher;
			lock (_lock) {
				ThrowIfIncorrectCycleCount(cycleCount);
				if (_completionIndicator.IsSet) return;
				_exception = error;
				_completionIndicator.Set();
				dispatcher = _primaryThreadDispatcher;
			}
			dispatcher?.NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
		}

		public bool WaitForCompletion(ulong cycleCount, TimeSpan timeout, CancellationToken cancellationToken) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();

			IPrimaryThreadDispatcher dispatcher;
			lock (_lock) {
				ThrowIfIncorrectCycleCount(cycleCount);
				dispatcher = _primaryThreadDispatcher;
			}
			return dispatcher.BlockPrimaryThreadUntilConditionSatisfied(_completionIndicator, timeout, cancellationToken);
		}

		public bool TryGetResultAndClear(ulong cycleCount, out T result, TimeSpan timeout, CancellationToken cancellationToken, out bool canBeReturnedToPool) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			canBeReturnedToPool = false;

			if (!WaitForCompletion(cycleCount, timeout, cancellationToken)) {
				result = default!;
				return false;
			}

			Exception? exceptionLocal;
			lock (_lock) {
				ThrowIfIncorrectCycleCount(cycleCount);
				exceptionLocal = _exception;
				result = _result;
				Clear();
				canBeReturnedToPool = true;
			}

			if (exceptionLocal != null) {
				throw new AggregateException($"{nameof(TinyFfrAsyncOperation)} failed due to async exception ({exceptionLocal.GetAllMessages()}).", exceptionLocal);
			}
			return true;
		}
	}

	static readonly ArrayPoolBackedObjectPool<AsyncOperationTrackingData> _trackerPool = new(&CreateDataObject);
	static AsyncOperationTrackingData CreateDataObject() => new();

	static AsyncOperationTrackingData RentTracker() {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		return _trackerPool.Rent();
	}
	static void ReturnTracker(AsyncOperationTrackingData trackingData) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		_trackerPool.Return(trackingData);
	}

	readonly AsyncOperationTrackingData _trackingData;
	readonly ulong _cycleCount;

	public bool IsCompleted {
		get {
			if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
			return _trackingData.GetIsCompleted(_cycleCount);
		}
	}

	internal TinyFfrAsyncOperation(IPrimaryThreadDispatcher primaryThreadDispatcher) {
		ArgumentNullException.ThrowIfNull(primaryThreadDispatcher);
		_trackingData = RentTracker();
		_trackingData.Reset(primaryThreadDispatcher);
		_cycleCount = _trackingData.CycleCount;
	}

	TinyFfrAsyncOperation(AsyncOperationTrackingData trackingData, ulong cycleCount) {
		_trackingData = trackingData;
		_cycleCount = cycleCount;
	}

	public void WaitForCompletion() => WaitForCompletion(Timeout.InfiniteTimeSpan, default);
	public bool WaitForCompletion(TimeSpan timeout) => WaitForCompletion(timeout, default);
	public void WaitForCompletion(CancellationToken cancellationToken) => WaitForCompletion(Timeout.InfiniteTimeSpan, cancellationToken);
	public bool WaitForCompletion(TimeSpan timeout, CancellationToken cancellationToken) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
		return _trackingData.WaitForCompletion(_cycleCount, timeout, cancellationToken);
	}

	public T GetResultAndDisposeOperation() {
		_ = GetResultAndDisposeOperation(Timeout.InfiniteTimeSpan, default, out var result);
		return result;
	}
	public bool GetResultAndDisposeOperation(TimeSpan timeout, out T result) => GetResultAndDisposeOperation(timeout, default, out result);
	public T GetResultAndDisposeOperation(CancellationToken cancellationToken) {
		_ = GetResultAndDisposeOperation(Timeout.InfiniteTimeSpan, cancellationToken, out var result);
		return result;
	}
	public bool GetResultAndDisposeOperation(TimeSpan timeout, CancellationToken cancellationToken, out T result) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();

		var canBeReturnedToPool = false;
		try {
			return _trackingData.TryGetResultAndClear(_cycleCount, out result, timeout, cancellationToken, out canBeReturnedToPool);
		}
		finally {
			if (canBeReturnedToPool) ReturnTracker(_trackingData);
		}
	}

	internal void SetResult(T result) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>(); // (20)
		_trackingData.SetResult(_cycleCount, result);
	}

	internal void SetException(Exception error) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>(); // (20)
		_trackingData.SetException(_cycleCount, error);
	}

	public static implicit operator TinyFfrAsyncOperation(TinyFfrAsyncOperation<T> operand) => new(operand._trackingData, operand._cycleCount);
	public static explicit operator TinyFfrAsyncOperation<T>(TinyFfrAsyncOperation operand) {
		if (operand.TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
		if (operand.TrackingData is not AsyncOperationTrackingData typedTrackingData) {
			throw new InvalidCastException($"Given {nameof(TinyFfrAsyncOperation)} does not represent an operation whose result type is {typeof(T).Name}.");
		}
		return new(typedTrackingData, operand.CycleCount);
	}

	internal static void Smuggle(in TinyFfrAsyncOperation<T> target, Span<byte> dest) {
		var wrapperHandle = GCHandle.Alloc(target._trackingData, GCHandleType.Normal);
		BinaryPrimitives.WriteIntPtrLittleEndian(dest, GCHandle.ToIntPtr(wrapperHandle));
		BinaryPrimitives.WriteUInt64LittleEndian(dest[sizeof(IntPtr)..], target._cycleCount);
	}

	internal static TinyFfrAsyncOperation<T> DeSmuggle(ReadOnlySpan<byte> src) {
		var gcHandle = GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(src));
		var trackingData = (AsyncOperationTrackingData) gcHandle.Target!;
		gcHandle.Free();

		return new(trackingData, BinaryPrimitives.ReadUInt64LittleEndian(src[sizeof(IntPtr)..]));
	}
}
