// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Egodystonic.TinyFFR.Resources;
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
	
	public bool IsCompleted => TrackingData.GetIsCompleted(CycleCount);

	internal TinyFfrAsyncOperation(IAsyncOperationTrackingData trackingData, ulong cycleCount) {
		TrackingData = trackingData;
		CycleCount = cycleCount;
	}
	
	public void WaitForCompletion() => WaitForCompletion(TimeSpan.FromMilliseconds(-1d), default);
	public bool WaitForCompletion(TimeSpan timeout) => WaitForCompletion(timeout, default);
	public void WaitForCompletion(CancellationToken cancellationToken) => WaitForCompletion(TimeSpan.FromMilliseconds(-1d), cancellationToken);
	public bool WaitForCompletion(TimeSpan timeout, CancellationToken cancellationToken) {
		if (TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
		return TrackingData.WaitForCompletion(CycleCount, timeout, cancellationToken);
	}
	
	public static void WaitForAllToComplete(params ReadOnlySpan<TinyFfrAsyncOperation> operations) => WaitForAllToComplete(TimeSpan.FromMilliseconds(-1d), default, operations);
	public static bool WaitForAllToComplete(TimeSpan timeout, params ReadOnlySpan<TinyFfrAsyncOperation> operations) => WaitForAllToComplete(timeout, default, operations);
	public static void WaitForAllToComplete(CancellationToken cancellationToken, params ReadOnlySpan<TinyFfrAsyncOperation> operations) => WaitForAllToComplete(TimeSpan.FromMilliseconds(-1d), cancellationToken, operations);
	public static bool WaitForAllToComplete(TimeSpan timeout, CancellationToken cancellationToken, params ReadOnlySpan<TinyFfrAsyncOperation> operations) {
		foreach (var operation in operations) {
			var startTime = Stopwatch.GetTimestamp();
			if (!operation.WaitForCompletion(timeout, cancellationToken)) return false;
			if (timeout >= TimeSpan.Zero && operation != operations[^1]) {
				timeout -= Stopwatch.GetElapsedTime(startTime);
				if (timeout < TimeSpan.Zero) return false;
			}
		}
		return true;
	}
}

public readonly unsafe record struct TinyFfrAsyncOperation<T> {
#pragma warning disable CA1001 // "Should be disposable" -- These objects live the lifetime of the application
	sealed class AsyncOperationTrackingData : IAsyncOperationTrackingData {
#pragma warning restore CA1001
		readonly ReaderWriterLockSlim _rwls = new(LockRecursionPolicy.NoRecursion);
		readonly ManualResetEventSlim _completionIndicator = new(false);
		IPrimaryThreadDispatcher _primaryThreadDispatcher = null!;
		ulong _currentCycleCount = 0UL;
		T _result = default!;
		Exception? _exception = null;
		
		public ulong CycleCount => _currentCycleCount;

		public AsyncOperationTrackingData() { Clear(); }

		void ThrowIfIncorrectCycleCount(ulong cycleCount) {
			Debug.Assert(_rwls.IsReadLockHeld);
			if (cycleCount != _currentCycleCount) {
				throw new InvalidOperationException($"Cycle counts do not match (this is a concurrency failure regarding {nameof(TinyFfrAsyncOperation)}).");
			}
		}
		
		public bool GetIsCompleted(ulong cycleCount) {
			_rwls.EnterReadLock();
			try {
				if (cycleCount != _currentCycleCount) {
					throw new InvalidOperationException($"It is not permitted to check {nameof(IsCompleted)} after invoking {nameof(GetResultAndDisposeOperation)}.");
				}
				return _completionIndicator.IsSet;
			}
			finally {
				_rwls.ExitReadLock();
			}
		}

		public void Reset(IPrimaryThreadDispatcher primaryThreadDispatcher) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			_rwls.EnterWriteLock();
			try {
				_primaryThreadDispatcher = primaryThreadDispatcher;
			}
			finally {
				_rwls.ExitWriteLock();
			}
		}
		
		public void Clear() {
			_rwls.EnterWriteLock();
			try {
				_result = default!;
				_exception = null;
				_primaryThreadDispatcher = null!;
				_completionIndicator.Reset();
				++_currentCycleCount;
			}
			finally {
				_rwls.ExitWriteLock();
			}
		}
		
		public void SetResult(ulong cycleCount, T result) {
			_rwls.EnterWriteLock();
			try {
				ThrowIfIncorrectCycleCount(cycleCount);
				if (_completionIndicator.IsSet) return;
				_result = result;
				_completionIndicator.Set();
			}
			finally {
				_rwls.ExitWriteLock();
			}
		}
		
		public void SetException(ulong cycleCount, Exception error) {
			_rwls.EnterWriteLock();
			try {
				ThrowIfIncorrectCycleCount(cycleCount);
				if (_completionIndicator.IsSet) return;
				_exception = error;
				_completionIndicator.Set();
			}
			finally {
				_rwls.ExitWriteLock();
			}
		}

		public bool WaitForCompletion(ulong cycleCount, TimeSpan timeout, CancellationToken cancellationToken) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			_rwls.EnterReadLock();
			try {
				ThrowIfIncorrectCycleCount(cycleCount);
			}
			finally {
				_rwls.ExitReadLock();
			}
			return _primaryThreadDispatcher.BlockPrimaryThreadUntilConditionSatisfied(_completionIndicator, timeout, cancellationToken);
		}

		public bool GetResultAndClear(ulong cycleCount, out T result, TimeSpan timeout, CancellationToken cancellationToken) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			if (!WaitForCompletion(cycleCount, timeout, cancellationToken)) {
				result = default!;
				return false;
			}
			_rwls.EnterUpgradeableReadLock();
			try {
				ThrowIfIncorrectCycleCount(cycleCount);
				var exceptionLocal = _exception;
				result = _result;
				Clear();
				if (exceptionLocal != null) {
					throw new AggregateException($"{nameof(TinyFfrAsyncOperation)} failed due to async exception ({exceptionLocal.GetAllMessages()}).", exceptionLocal);
				}
				return true;
			}
			finally {
				_rwls.ExitUpgradeableReadLock();
			}
		} 
	}
	
	static readonly ArrayPoolBackedObjectPool<AsyncOperationTrackingData> _trackerPool = new(&CreateDataObject);
	static AsyncOperationTrackingData CreateDataObject() => new();
	
	readonly AsyncOperationTrackingData _trackingData;
	readonly ulong _cycleCount;
	
	public bool IsCompleted => _trackingData.GetIsCompleted(_cycleCount);

	internal TinyFfrAsyncOperation(IPrimaryThreadDispatcher primaryThreadDispatcher) {
		_trackingData = _trackerPool.Rent();
		_trackingData.Reset(primaryThreadDispatcher);
		_cycleCount = _trackingData.CycleCount;
	}
	
	TinyFfrAsyncOperation(AsyncOperationTrackingData trackingData, ulong cycleCount) {
		_trackingData = trackingData;
		_cycleCount = cycleCount;
	}
	
	public void WaitForCompletion() => WaitForCompletion(TimeSpan.FromMilliseconds(-1d), default);
	public bool WaitForCompletion(TimeSpan timeout) => WaitForCompletion(timeout, default);
	public void WaitForCompletion(CancellationToken cancellationToken) => WaitForCompletion(TimeSpan.FromMilliseconds(-1d), cancellationToken);
	public bool WaitForCompletion(TimeSpan timeout, CancellationToken cancellationToken) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
		return _trackingData.WaitForCompletion(_cycleCount, timeout, cancellationToken);
	}
	
	public T GetResultAndDisposeOperation() {
		_ = GetResultAndDisposeOperation(TimeSpan.FromMilliseconds(-1d), default, out var result);
		return result;
	}
	public bool GetResultAndDisposeOperation(TimeSpan timeout, out T result) => GetResultAndDisposeOperation(timeout, default, out result);
	public T GetResultAndDisposeOperation(CancellationToken cancellationToken) {
		_ = GetResultAndDisposeOperation(TimeSpan.FromMilliseconds(-1d), cancellationToken, out var result);
		return result;
	}
	public bool GetResultAndDisposeOperation(TimeSpan timeout, CancellationToken cancellationToken, out T result) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
		try {
			return _trackingData.GetResultAndClear(_cycleCount, out result, timeout, cancellationToken);
		}
		finally {
			_trackerPool.Return(_trackingData);
		}
	}
	
	internal void SetResult(T result) {
		_trackingData.SetResult(_cycleCount, result);
	}
	
	internal void SetException(Exception error) {
		_trackingData.SetException(_cycleCount, error);
	}
	
	public static implicit operator TinyFfrAsyncOperation(TinyFfrAsyncOperation<T> operand) => new(operand._trackingData, operand._cycleCount);
	public static explicit operator TinyFfrAsyncOperation<T>(TinyFfrAsyncOperation operand) => new((AsyncOperationTrackingData) operand.TrackingData, operand.CycleCount);
	
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