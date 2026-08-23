// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Threading;

interface IAsyncOperationTrackingData {
	ulong Version { get; }
	bool GetIsCompleted(ulong version);
	bool WaitForCompletion(ulong version, TimeSpan timeout, CancellationToken cancellationToken);
	void ScheduleContinuation(ulong version, Action continuation);
	void DiscardResultAndClearWithoutWaiting(ulong version);
	Action? FaultIfIncompleteAndExtractContinuations(Exception error);
}

public readonly record struct OngoingAsynchronousOperationStatistics(int OperationCount, int CompletedCount, float CompletedFraction);

static class OutstandingAsyncOperationRegistry {
	static readonly List<IAsyncOperationTrackingData> _outstandingOperations = new();
	static readonly List<Action> _deferredContinuations = new();

	public static int OutstandingCount {
		get {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			return _outstandingOperations.Count;
		}
	}

	public static void Register(IAsyncOperationTrackingData trackingData) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		_outstandingOperations.Add(trackingData);
	}

	public static void Unregister(IAsyncOperationTrackingData trackingData) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		_outstandingOperations.Remove(trackingData);
	}

	public static void FaultAllOutstanding(Exception error) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		var snapshot = _outstandingOperations.ToArray();
		_outstandingOperations.Clear();
		foreach (var trackingData in snapshot) {
			try {
				var continuations = trackingData.FaultIfIncompleteAndExtractContinuations(error);
				if (continuations != null) _deferredContinuations.Add(continuations);
			}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- One operation must not prevent the rest being faulted
			catch (Exception e) {
#pragma warning restore CA1031
				Console.WriteLine($"Could not fault outstanding async operation during teardown: {e.GetAllMessages()}.");
			}
		}
	}

	public static void InvokeDeferredContinuations() {
		if (_deferredContinuations.Count == 0) return;
		var snapshot = _deferredContinuations.ToArray();
		_deferredContinuations.Clear();

		if (!ThreadSafetyTracker.CurrentThreadIsPrimary()) {
			Console.WriteLine($"Discarding {snapshot.Length} deferred async operation continuation set(s): teardown did not complete on the primary thread.");
			return;
		}

		foreach (var continuations in snapshot) {
			foreach (var invocationListEntry in continuations.GetInvocationList()) {
				try {
					((Action) invocationListEntry)();
				}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- One continuation must not prevent the rest being invoked
				catch (Exception e) {
#pragma warning restore CA1031
					Console.WriteLine($"Deferred {nameof(TinyFfrAsyncOperation)} continuation failed with unhandled exception '{e}' | {e.GetAllMessages()}.");
					Console.WriteLine(e.StackTrace);
				}
			}
		}
	}
}

public interface ITinyFfrAsyncOperation {
	// TODO xmldoc the difference between these three properties:
	//	IsCompleted = The async operation has finished (whether or not this operation has been disposed)
	//	IsDisposed = The async operation has been disposed
	//	IsResultAvailable = The async operation is complete and GetResultAndDisposeOperation will return immediately
	bool IsCompleted { get; }
	bool IsDisposed { get; }
	bool IsResultAvailable { get; }
}

//TODO xmldoc Every TinyFfrAsyncOperation returned by the library must be consumed exactly once, either by awaiting it or by
//TODO xmldoc calling GetResultAndDisposeOperation(). Consuming an operation is what releases its internal tracking data back to
//TODO xmldoc the pool; an operation that is never consumed retains that tracking data (and its wait handle) for the lifetime of
//TODO xmldoc the process. An operation whose wait timed out has not been consumed, and must still be consumed once it completes.
public readonly record struct TinyFfrAsyncOperation : ITinyFfrAsyncOperation {
#pragma warning disable CA1034 // "Don't nest public classes" -- I'll do what I want
	public readonly record struct Awaiter : ICriticalNotifyCompletion {
#pragma warning restore CA1034
		readonly TinyFfrAsyncOperation _operation;

		internal Awaiter(TinyFfrAsyncOperation operation) => _operation = operation;

		public bool IsCompleted => _operation.IsCompleted && ThreadSafetyTracker.CurrentThreadIsPrimary();
		public void OnCompleted(Action continuation) => _operation.ScheduleContinuation(continuation);
		public void UnsafeOnCompleted(Action continuation) => _operation.ScheduleContinuation(continuation);
		public void GetResult() => _operation.DiscardResultAndDisposeOperationWithoutWaiting();
	}
	
	internal const int SmuggleSizeBytes = 16;
	internal IAsyncOperationTrackingData TrackingData { get; }
	internal ulong Version { get; }

	public bool IsCompleted {
		get {
			return IsDisposed || TrackingData.GetIsCompleted(Version);
		}
	}
	
	public bool IsDisposed {
		get {
			return TrackingData == null || Version != TrackingData.Version;
		}
	}
	
	public bool IsResultAvailable {
		get {
			return !IsDisposed && TrackingData.GetIsCompleted(Version);
		}
	}

	internal TinyFfrAsyncOperation(IAsyncOperationTrackingData trackingData, ulong version) {
		TrackingData = trackingData;
		Version = version;
	}

	public Awaiter GetAwaiter() {
		if (TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
		return new(this);
	}

	internal void ScheduleContinuation(Action continuation) {
		if (TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
		TrackingData.ScheduleContinuation(Version, continuation);
	}

	internal void DiscardResultAndDisposeOperationWithoutWaiting() {
		if (TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
		TrackingData.DiscardResultAndClearWithoutWaiting(Version);
	}
	
	public static OngoingAsynchronousOperationStatistics GetCompletionStats(params ReadOnlySpan<TinyFfrAsyncOperation> operations) {
		var completed = 0;
		for (var i = 0; i < operations.Length; ++i) {
			if (operations[i].IsCompleted) ++completed;
		}
		return new OngoingAsynchronousOperationStatistics(operations.Length, completed, (float) completed / (float) operations.Length);
	}
	public static OngoingAsynchronousOperationStatistics GetCompletionStats<TCollection>(TCollection operations) where TCollection : IReadOnlyList<TinyFfrAsyncOperation> {
		var completed = 0;
		var count = operations.Count;
		for (var i = 0; i < count; ++i) {
			if (operations[i].IsCompleted) ++completed;
		}
		return new OngoingAsynchronousOperationStatistics(count, completed, (float) completed / (float) count);
	}

	public void WaitForCompletion() => WaitForCompletion(Timeout.InfiniteTimeSpan, default);
	public bool WaitForCompletion(TimeSpan timeout) => WaitForCompletion(timeout, default);
	public void WaitForCompletion(CancellationToken cancellationToken) => WaitForCompletion(Timeout.InfiniteTimeSpan, cancellationToken);
	public bool WaitForCompletion(TimeSpan timeout, CancellationToken cancellationToken) {
		if (TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
		return TrackingData.WaitForCompletion(Version, timeout, cancellationToken);
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
	
	public static void WaitForAllToComplete<TCollection>(TCollection operations) where TCollection : IReadOnlyList<TinyFfrAsyncOperation> => WaitForAllToComplete(Timeout.InfiniteTimeSpan, default, operations);
	public static bool WaitForAllToComplete<TCollection>(TimeSpan timeout, TCollection operations) where TCollection : IReadOnlyList<TinyFfrAsyncOperation> => WaitForAllToComplete(timeout, default, operations);
#pragma warning disable CA1068 // "CancellationToken should be last parameter" -- Not wrong, but done this way for consistency with params overloads above
	public static void WaitForAllToComplete<TCollection>(CancellationToken cancellationToken, TCollection operations) where TCollection : IReadOnlyList<TinyFfrAsyncOperation> => WaitForAllToComplete(Timeout.InfiniteTimeSpan, cancellationToken, operations);
	public static bool WaitForAllToComplete<TCollection>(TimeSpan timeout, CancellationToken cancellationToken, TCollection operations) where TCollection : IReadOnlyList<TinyFfrAsyncOperation> {
#pragma warning restore CA1068
		var startTimestamp = Stopwatch.GetTimestamp();
		var hasTimeout = timeout >= TimeSpan.Zero;

		for (var i = 0; i < operations.Count; ++i) {
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

//TODO xmldoc Every TinyFfrAsyncOperation<T> returned by the library must be consumed exactly once, either by awaiting it or by
//TODO xmldoc calling GetResultAndDisposeOperation(). Consuming an operation is what releases its internal tracking data back to
//TODO xmldoc the pool; an operation that is never consumed retains that tracking data (and its wait handle) for the lifetime of
//TODO xmldoc the process. An operation whose wait timed out has not been consumed, and must still be consumed once it completes.
public readonly unsafe record struct TinyFfrAsyncOperation<T> : ITinyFfrAsyncOperation {
#pragma warning disable CA1034 // "Don't nest public classes" -- I'll do what I want
	public readonly record struct Awaiter : ICriticalNotifyCompletion {
#pragma warning restore CA1034
		readonly TinyFfrAsyncOperation<T> _operation;

		internal Awaiter(TinyFfrAsyncOperation<T> operation) => _operation = operation;

		public bool IsCompleted => _operation.IsCompleted && ThreadSafetyTracker.CurrentThreadIsPrimary();
		public void OnCompleted(Action continuation) => _operation.ScheduleContinuation(continuation);
		public void UnsafeOnCompleted(Action continuation) => _operation.ScheduleContinuation(continuation);
		public T GetResult() => _operation.GetResultAndDisposeOperationWithoutWaiting();
	}
	
#pragma warning disable CA1001 // "Should be disposable" -- These objects live the lifetime of the application
	sealed class AsyncOperationTrackingData : IAsyncOperationTrackingData {
#pragma warning restore CA1001	
		readonly Lock _lock = new();
		readonly ManualResetEventSlim _completionIndicator = new(false);
		IPrimaryThreadDispatcher? _primaryThreadDispatcher = null;
		Action? _continuations = null;
		ulong _version;
		T _result = default!;
		Exception? _exception = null;

		public ulong Version {
			get {
				lock (_lock) return _version;
			}
		}

		public AsyncOperationTrackingData() {
			lock (_lock) Clear();
		}

		void ThrowIfIncorrectVersion(ulong version) {
			Debug.Assert(_lock.IsHeldByCurrentThread);
			if (version != _version) {
				throw new InvalidOperationException($"Operation versions do not match: this {nameof(TinyFfrAsyncOperation)} has already been disposed or recycled.");
			}
		}

		void Clear() {
			Debug.Assert(_lock.IsHeldByCurrentThread);
			_result = default!;
			_exception = null;
			_primaryThreadDispatcher = null;
			_continuations = null;
			_completionIndicator.Reset();
			++_version;
		}

		static void ScheduleOrInvokeContinuations(IPrimaryThreadDispatcher? dispatcher, Action continuations) {
			foreach (var invocationListEntry in continuations.GetInvocationList()) {
				var continuation = (Action) invocationListEntry;
				if (dispatcher != null) {
					dispatcher.SchedulePrimaryThreadContinuation(continuation);
					continue;
				}

				try {
					continuation();
				}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- One continuation must not prevent the rest being invoked
				catch (Exception e) {
#pragma warning restore CA1031
					Console.WriteLine($"{nameof(TinyFfrAsyncOperation)} continuation failed with unhandled exception '{e}' | {e.GetAllMessages()}.");
					Console.WriteLine(e.StackTrace);
				}
			}
		}

		public void Reset(IPrimaryThreadDispatcher primaryThreadDispatcher) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			lock (_lock) {
				Clear();
				_primaryThreadDispatcher = primaryThreadDispatcher;
			}
		}

		public bool GetIsCompleted(ulong version) {
			lock (_lock) {
				ThrowIfIncorrectVersion(version);
				return _completionIndicator.IsSet;
			}
		}

		public void SetResult(ulong version, T result) {
			IPrimaryThreadDispatcher? dispatcher;
			Action? continuationsLocal;
			lock (_lock) {
				ThrowIfIncorrectVersion(version);
				if (_completionIndicator.IsSet) return;
				_result = result;
				_completionIndicator.Set();
				dispatcher = _primaryThreadDispatcher;
				continuationsLocal = _continuations;
				_continuations = null;
			}
			dispatcher?.NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
			if (continuationsLocal != null) ScheduleOrInvokeContinuations(dispatcher, continuationsLocal);
		}

		public void SetException(ulong version, Exception error) {
			IPrimaryThreadDispatcher? dispatcher;
			Action? continuationsLocal;
			lock (_lock) {
				ThrowIfIncorrectVersion(version);
				if (_completionIndicator.IsSet) return;
				_exception = error;
				_completionIndicator.Set();
				dispatcher = _primaryThreadDispatcher;
				continuationsLocal = _continuations;
				_continuations = null;
			}
			dispatcher?.NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
			if (continuationsLocal != null) ScheduleOrInvokeContinuations(dispatcher, continuationsLocal);
		}

		public Action? FaultIfIncompleteAndExtractContinuations(Exception error) {
			lock (_lock) {
				if (_completionIndicator.IsSet) return null;
				_exception = error;
				_completionIndicator.Set();
				var continuationsLocal = _continuations;
				_continuations = null;
				return continuationsLocal;
			}
		}

		public void ScheduleContinuation(ulong version, Action continuation) {
			ArgumentNullException.ThrowIfNull(continuation);

			IPrimaryThreadDispatcher? dispatcher;
			lock (_lock) {
				ThrowIfIncorrectVersion(version);
				dispatcher = _primaryThreadDispatcher;
				if (!_completionIndicator.IsSet) {
					_continuations += continuation;
					return;
				}
			}
			ScheduleOrInvokeContinuations(dispatcher, continuation);
		}

		public bool WaitForCompletion(ulong version, TimeSpan timeout, CancellationToken cancellationToken) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();

			IPrimaryThreadDispatcher dispatcher;
			lock (_lock) {
				ThrowIfIncorrectVersion(version);
				dispatcher = _primaryThreadDispatcher!;
			}
			return dispatcher.BlockPrimaryThreadUntilConditionSatisfied(_completionIndicator, this, version, timeout, cancellationToken);
		}

		public bool TryGetResultAndClear(ulong version, out T result, TimeSpan timeout, CancellationToken cancellationToken, out bool canBeReturnedToPool) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			canBeReturnedToPool = false;

			if (!WaitForCompletion(version, timeout, cancellationToken)) {
				result = default!;
				return false;
			}

			result = GetResultAndClearWithoutWaiting(version, out canBeReturnedToPool);
			return true;
		}

		public T GetResultAndClearWithoutWaiting(ulong version, out bool canBeReturnedToPool) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			canBeReturnedToPool = false;

			Exception? exceptionLocal;
			T result;
			lock (_lock) {
				ThrowIfIncorrectVersion(version);
				if (!_completionIndicator.IsSet) {
					throw new InvalidOperationException($"Can not extract the result of this {nameof(TinyFfrAsyncOperation)}: it has not completed yet.");
				}
				exceptionLocal = _exception;
				result = _result;
				Clear();
				canBeReturnedToPool = true;
			}

			if (exceptionLocal != null) {
				throw new AggregateException($"{nameof(TinyFfrAsyncOperation)} failed due to async exception ({exceptionLocal.GetAllMessages()}).", exceptionLocal);
			}
			return result;
		}

		void IAsyncOperationTrackingData.DiscardResultAndClearWithoutWaiting(ulong version) => _ = new TinyFfrAsyncOperation<T>(this, version).GetResultAndDisposeOperationWithoutWaiting();
	}

	static readonly ArrayPoolBackedObjectPool<AsyncOperationTrackingData> _trackerPool = new(&CreateDataObject);
	static AsyncOperationTrackingData CreateDataObject() => new();

	static AsyncOperationTrackingData RentTracker() {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		var result = _trackerPool.Rent();
		OutstandingAsyncOperationRegistry.Register(result);
		return result;
	}
	static void ReturnTracker(AsyncOperationTrackingData trackingData) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		OutstandingAsyncOperationRegistry.Unregister(trackingData);
		_trackerPool.Return(trackingData);
	}

	readonly AsyncOperationTrackingData _trackingData;
	readonly ulong _version;

	public bool IsCompleted {
		get {
			return IsDisposed || _trackingData.GetIsCompleted(_version);
		}
	}
	
	public bool IsDisposed {
		get {
			return _trackingData == null || _version != _trackingData.Version;
		}
	}
	
	public bool IsResultAvailable {
		get {
			return !IsDisposed && _trackingData.GetIsCompleted(_version);
		}
	}

	internal TinyFfrAsyncOperation(IPrimaryThreadDispatcher primaryThreadDispatcher) {
		ArgumentNullException.ThrowIfNull(primaryThreadDispatcher);
		_trackingData = RentTracker();
		_trackingData.Reset(primaryThreadDispatcher);
		_version = _trackingData.Version;
	}

	TinyFfrAsyncOperation(AsyncOperationTrackingData trackingData, ulong version) {
		_trackingData = trackingData;
		_version = version;
	}

	public Awaiter GetAwaiter() {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
		return new(this);
	}

	internal void ScheduleContinuation(Action continuation) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
		_trackingData.ScheduleContinuation(_version, continuation);
	}

	internal T GetResultAndDisposeOperationWithoutWaiting() {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();

		var canBeReturnedToPool = false;
		try {
			return _trackingData.GetResultAndClearWithoutWaiting(_version, out canBeReturnedToPool);
		}
		finally {
			if (canBeReturnedToPool) ReturnTracker(_trackingData);
		}
	}

	public void WaitForCompletion() => WaitForCompletion(Timeout.InfiniteTimeSpan, default);
	public bool WaitForCompletion(TimeSpan timeout) => WaitForCompletion(timeout, default);
	public void WaitForCompletion(CancellationToken cancellationToken) => WaitForCompletion(Timeout.InfiniteTimeSpan, cancellationToken);
	public bool WaitForCompletion(TimeSpan timeout, CancellationToken cancellationToken) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
		return _trackingData.WaitForCompletion(_version, timeout, cancellationToken);
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
			return _trackingData.TryGetResultAndClear(_version, out result, timeout, cancellationToken, out canBeReturnedToPool);
		}
		finally {
			if (canBeReturnedToPool) ReturnTracker(_trackingData);
		}
	}

	internal void SetResult(T result) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
		_trackingData.SetResult(_version, result);
	}

	internal void SetException(Exception error) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
		_trackingData.SetException(_version, error);
	}

	public static implicit operator TinyFfrAsyncOperation(TinyFfrAsyncOperation<T> operand) => new(operand._trackingData, operand._version);
	public static explicit operator TinyFfrAsyncOperation<T>(TinyFfrAsyncOperation operand) {
		if (operand.TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
		if (operand.TrackingData is not AsyncOperationTrackingData typedTrackingData) {
			throw new InvalidCastException($"Given {nameof(TinyFfrAsyncOperation)} does not represent an operation whose result type is {typeof(T).Name}.");
		}
		return new(typedTrackingData, operand.Version);
	}

	internal static void Smuggle(in TinyFfrAsyncOperation<T> target, Span<byte> dest) {
		var wrapperHandle = GCHandle.Alloc(target._trackingData, GCHandleType.Normal);
		BinaryPrimitives.WriteIntPtrLittleEndian(dest, GCHandle.ToIntPtr(wrapperHandle));
		BinaryPrimitives.WriteUInt64LittleEndian(dest[sizeof(IntPtr)..], target._version);
	}

	internal static TinyFfrAsyncOperation<T> DeSmuggle(ReadOnlySpan<byte> src) {
		var gcHandle = GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(src));
		var trackingData = (AsyncOperationTrackingData) gcHandle.Target!;
		gcHandle.Free();

		return new(trackingData, BinaryPrimitives.ReadUInt64LittleEndian(src[sizeof(IntPtr)..]));
	}
}
