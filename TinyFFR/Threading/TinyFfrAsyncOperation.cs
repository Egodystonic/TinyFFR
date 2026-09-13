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

/// <summary>
/// A snapshot summary of how many of a set of <see cref="TinyFfrAsyncOperation"/>s (or <see cref="TinyFfrAsyncOperation{T}"/>s) have completed so far.
/// Returned by <see cref="TinyFfrAsyncOperation.GetCompletionStats"/>.
/// </summary>
/// <param name="OperationCount">The total number of operations in the set.</param>
/// <param name="CompletedCount">The number of operations in the set that had already completed at the time this snapshot was taken.</param>
/// <param name="CompletedFraction">The value of <paramref name="CompletedCount"/> divided by <paramref name="OperationCount"/>, i.e. <c>0f</c> if none have completed and <c>1f</c> if all have.</param>
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

/// <summary>
/// Represents an asynchronous operation being tracked by TinyFFR, exposing the properties common to both <see cref="TinyFfrAsyncOperation"/> and <see cref="TinyFfrAsyncOperation{T}"/>.
/// </summary>
public interface ITinyFfrAsyncOperation {
	/// <summary>
	/// Whether the underlying asynchronous operation has finished, regardless of whether it (or its result) has since been consumed/disposed.
	/// </summary>
	/// <remarks>
	/// This remains <see langword="true"/> even after the operation has been consumed (see <see cref="IsDisposed"/>);
	/// use <see cref="IsResultAvailable"/> if you need to know whether the result can still be retrieved.
	/// </remarks>
	bool IsCompleted { get; }
	/// <summary>
	/// Whether this specific operation value has already been consumed (either by awaiting it or by fetching its result),
	/// and so no longer refers to a live, trackable operation.
	/// </summary>
	bool IsDisposed { get; }
	/// <summary>
	/// Whether the underlying asynchronous operation has finished <i>and</i> has not yet been consumed,
	/// i.e. whether fetching the result right now would return immediately rather than blocking or throwing.
	/// </summary>
	bool IsResultAvailable { get; }
}

/// <summary>
/// Represents an ongoing or completed asynchronous operation with the return type erased; useful for grouping multiple async operations of differing return type together.
/// </summary>
/// <remarks>
/// You can implicitly convert a <see cref="TinyFfrAsyncOperation{T}"/> to a <see cref="TinyFfrAsyncOperation"/>. To convert back requires an explicit cast.
/// </remarks>
/// <seealso cref="TinyFfrAsyncOperation{T}"/>
public readonly record struct TinyFfrAsyncOperation : ITinyFfrAsyncOperation {
#pragma warning disable CA1034 // "Don't nest public classes" -- I'll do what I want
	/// <summary>
	/// The awaiter type returned by <see cref="GetAwaiter"/>, enabling a <see cref="TinyFfrAsyncOperation"/> to be used directly with the <see langword="await"/> keyword.
	/// </summary>
	/// <remarks>
	/// You will not typically need to use this type directly; it exists to satisfy the compiler's awaitable pattern.
	/// </remarks>
	public readonly record struct Awaiter : ICriticalNotifyCompletion {
#pragma warning restore CA1034
		readonly TinyFfrAsyncOperation _operation;

		internal Awaiter(TinyFfrAsyncOperation operation) => _operation = operation;

		/// <summary>
		/// Whether the awaited operation has finished and this awaiter is on the primary thread, and so is ready for the compiler-generated <see langword="await"/> code to call <see cref="GetResult"/>.
		/// </summary>
		public bool IsCompleted => _operation.IsCompleted && ThreadSafetyTracker.CurrentThreadIsPrimary();
		/// <inheritdoc/>
		public void OnCompleted(Action continuation) => _operation.ScheduleContinuation(continuation);
		/// <inheritdoc/>
		public void UnsafeOnCompleted(Action continuation) => _operation.ScheduleContinuation(continuation);
		/// <summary>
		/// Consumes this operation, marking it as disposed. Called automatically by the compiler-generated <see langword="await"/> code once <see cref="IsCompleted"/> is <see langword="true"/>.
		/// </summary>
		public void GetResult() => _operation.DiscardResultAndDisposeOperationWithoutWaiting();
	}
	
	internal const int SmuggleSizeBytes = 16;
	internal IAsyncOperationTrackingData TrackingData { get; }
	internal ulong Version { get; }

	/// <inheritdoc/>
	public bool IsCompleted {
		get {
			return IsDisposed || TrackingData.GetIsCompleted(Version);
		}
	}

	/// <inheritdoc/>
	public bool IsDisposed {
		get {
			if (TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
			return Version != TrackingData.Version;
		}
	}

	/// <inheritdoc/>
	public bool IsResultAvailable {
		get {
			return !IsDisposed && TrackingData.GetIsCompleted(Version);
		}
	}

	internal TinyFfrAsyncOperation(IAsyncOperationTrackingData trackingData, ulong version) {
		TrackingData = trackingData;
		Version = version;
	}

	/// <summary>
	/// Returns the awaiter used to make this operation directly awaitable via the <see langword="await"/> keyword.
	/// </summary>
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
	
	/// <summary>
	/// Returns a snapshot of how many of the given <paramref name="operations"/> have completed so far.
	/// </summary>
	/// <remarks>
	/// This does not consume any of <paramref name="operations"/>: it is safe to call on operations you still intend to await or wait on afterwards.
	/// </remarks>
	/// <param name="operations">The operations to check. Consuming this method does not consume the operations themselves.</param>
	public static OngoingAsynchronousOperationStatistics GetCompletionStats(params ReadOnlySpan<TinyFfrAsyncOperation> operations) {
		var completed = 0;
		for (var i = 0; i < operations.Length; ++i) {
			if (operations[i].IsCompleted) ++completed;
		}
		return new OngoingAsynchronousOperationStatistics(operations.Length, completed, (float) completed / (float) operations.Length);
	}
	/// <inheritdoc cref="GetCompletionStats(ReadOnlySpan{TinyFfrAsyncOperation})"/>
	/// <typeparam name="TCollection">The type of the list of operations.</typeparam>
	public static OngoingAsynchronousOperationStatistics GetCompletionStats<TCollection>(TCollection operations) where TCollection : IReadOnlyList<TinyFfrAsyncOperation> {
		var completed = 0;
		var count = operations.Count;
		for (var i = 0; i < count; ++i) {
			if (operations[i].IsCompleted) ++completed;
		}
		return new OngoingAsynchronousOperationStatistics(count, completed, (float) completed / (float) count);
	}

	/// <summary>
	/// Blocks the calling thread until this operation completes.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume the operation: you must still separately await it or call <see cref="TinyFfrAsyncOperation{T}.GetResultAndDisposeOperation()">GetResultAndDisposeOperation()</see> on its typed counterpart afterwards.
	/// </remarks>
	public void WaitForCompletion() => WaitForCompletion(Timeout.InfiniteTimeSpan, default);
	/// <summary>
	/// Blocks the calling thread until this operation completes, or until <paramref name="timeout"/> elapses.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume the operation: you must still separately await it or call <see cref="TinyFfrAsyncOperation{T}.GetResultAndDisposeOperation()">GetResultAndDisposeOperation()</see> on its typed counterpart afterwards.
	/// </remarks>
	/// <param name="timeout">The maximum amount of time to wait. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
	/// <returns><see langword="true"/> if the operation completed before <paramref name="timeout"/> elapsed; <see langword="false"/> otherwise.</returns>
	public bool WaitForCompletion(TimeSpan timeout) => WaitForCompletion(timeout, default);
	/// <summary>
	/// Blocks the calling thread until this operation completes, or until <paramref name="cancellationToken"/> is cancelled.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume the operation: you must still separately await it or call <see cref="TinyFfrAsyncOperation{T}.GetResultAndDisposeOperation()">GetResultAndDisposeOperation()</see> on its typed counterpart afterwards.
	/// </remarks>
	/// <param name="cancellationToken">A token that can be used to stop waiting early.</param>
	public void WaitForCompletion(CancellationToken cancellationToken) => WaitForCompletion(Timeout.InfiniteTimeSpan, cancellationToken);
	/// <summary>
	/// Blocks the calling thread until this operation completes, or until <paramref name="timeout"/> elapses, or until <paramref name="cancellationToken"/> is cancelled.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume the operation: you must still separately await it or call <see cref="TinyFfrAsyncOperation{T}.GetResultAndDisposeOperation()">GetResultAndDisposeOperation()</see> on its typed counterpart afterwards.
	/// </remarks>
	/// <param name="timeout">The maximum amount of time to wait. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
	/// <param name="cancellationToken">A token that can be used to stop waiting early.</param>
	/// <returns><see langword="true"/> if the operation completed before <paramref name="timeout"/> elapsed; <see langword="false"/> otherwise.</returns>
	public bool WaitForCompletion(TimeSpan timeout, CancellationToken cancellationToken) {
		if (TrackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation>();
		return TrackingData.WaitForCompletion(Version, timeout, cancellationToken);
	}

	/// <summary>
	/// Blocks the calling thread until every one of <paramref name="operations"/> has completed.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume any of <paramref name="operations"/>.
	/// </remarks>
	/// <param name="operations">The operations to wait for.</param>
	public static void WaitForAllToComplete(params ReadOnlySpan<TinyFfrAsyncOperation> operations) => WaitForAllToComplete(Timeout.InfiniteTimeSpan, default, operations);
	/// <summary>
	/// Blocks the calling thread until every one of <paramref name="operations"/> has completed, or until <paramref name="timeout"/> elapses.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume any of <paramref name="operations"/>. <paramref name="timeout"/> is a single deadline for the whole call, shared across all of <paramref name="operations"/>, not a per-operation timeout.
	/// </remarks>
	/// <param name="timeout">The maximum amount of total time to wait across all of <paramref name="operations"/>. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
	/// <param name="operations">The operations to wait for.</param>
	/// <returns><see langword="true"/> if every operation completed before <paramref name="timeout"/> elapsed; <see langword="false"/> otherwise.</returns>
	public static bool WaitForAllToComplete(TimeSpan timeout, params ReadOnlySpan<TinyFfrAsyncOperation> operations) => WaitForAllToComplete(timeout, default, operations);
	/// <summary>
	/// Blocks the calling thread until every one of <paramref name="operations"/> has completed, or until <paramref name="cancellationToken"/> is cancelled.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume any of <paramref name="operations"/>.
	/// </remarks>
	/// <param name="cancellationToken">A token that can be used to stop waiting early.</param>
	/// <param name="operations">The operations to wait for.</param>
	public static void WaitForAllToComplete(CancellationToken cancellationToken, params ReadOnlySpan<TinyFfrAsyncOperation> operations) => WaitForAllToComplete(Timeout.InfiniteTimeSpan, cancellationToken, operations);
	/// <summary>
	/// Blocks the calling thread until every one of <paramref name="operations"/> has completed, or until <paramref name="timeout"/> elapses, or until <paramref name="cancellationToken"/> is cancelled.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume any of <paramref name="operations"/>. <paramref name="timeout"/> is a single deadline for the whole call, shared across all of <paramref name="operations"/>, not a per-operation timeout.
	/// </remarks>
	/// <param name="timeout">The maximum amount of total time to wait across all of <paramref name="operations"/>. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
	/// <param name="cancellationToken">A token that can be used to stop waiting early.</param>
	/// <param name="operations">The operations to wait for.</param>
	/// <returns><see langword="true"/> if every operation completed before <paramref name="timeout"/> elapsed; <see langword="false"/> otherwise.</returns>
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
	
	/// <inheritdoc cref="WaitForAllToComplete(ReadOnlySpan{TinyFfrAsyncOperation})"/>
	/// <typeparam name="TCollection">The type of the list of operations.</typeparam>
	public static void WaitForAllToComplete<TCollection>(TCollection operations) where TCollection : IReadOnlyList<TinyFfrAsyncOperation> => WaitForAllToComplete(Timeout.InfiniteTimeSpan, default, operations);
	/// <inheritdoc cref="WaitForAllToComplete(TimeSpan,ReadOnlySpan{TinyFfrAsyncOperation})"/>
	/// <typeparam name="TCollection">The type of the list of operations.</typeparam>
	public static bool WaitForAllToComplete<TCollection>(TimeSpan timeout, TCollection operations) where TCollection : IReadOnlyList<TinyFfrAsyncOperation> => WaitForAllToComplete(timeout, default, operations);
#pragma warning disable CA1068 // "CancellationToken should be last parameter" -- Not wrong, but done this way for consistency with params overloads above
	/// <inheritdoc cref="WaitForAllToComplete(CancellationToken,ReadOnlySpan{TinyFfrAsyncOperation})"/>
	/// <typeparam name="TCollection">The type of the list of operations.</typeparam>
	public static void WaitForAllToComplete<TCollection>(CancellationToken cancellationToken, TCollection operations) where TCollection : IReadOnlyList<TinyFfrAsyncOperation> => WaitForAllToComplete(Timeout.InfiniteTimeSpan, cancellationToken, operations);
	/// <inheritdoc cref="WaitForAllToComplete(TimeSpan,CancellationToken,ReadOnlySpan{TinyFfrAsyncOperation})"/>
	/// <typeparam name="TCollection">The type of the list of operations.</typeparam>
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

/// <summary>
/// Represents an ongoing or completed asynchronous operation that will eventually produce a <typeparamref name="T"/> result.
/// </summary>
/// <remarks>
/// <para>
/// Every <see cref="TinyFfrAsyncOperation{T}"/> returned by the library must be consumed exactly once, either by awaiting it or by calling an overload of <see cref="GetResultAndDisposeOperation()"/>.
/// Consuming an operation is what releases its internal tracking data back to a shared pool; an operation that is never consumed retains that tracking data (and its wait handle) for the
/// lifetime of the process. An operation whose wait timed out has not been consumed, and must still be consumed once it completes.
/// </para>
/// <para>
/// Note that the <c>await</c> pathway introduces garbage that the GC must collect and therefore may cause frame stuttering. Thus, the <c>await</c> pathway should be avoided in performance-sensitive
/// workloads, prefer <see cref="GetResultAndDisposeOperation()"/> and its overloads alongside <see cref="IsCompleted"/> and <see cref="IsResultAvailable"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the operation's eventual result.</typeparam>
public readonly unsafe record struct TinyFfrAsyncOperation<T> : ITinyFfrAsyncOperation {
#pragma warning disable CA1034 // "Don't nest public classes" -- I'll do what I want
	/// <summary>
	/// The awaiter type returned by <see cref="GetAwaiter"/>, enabling a <see cref="TinyFfrAsyncOperation{T}"/> to be used directly with the <see langword="await"/> keyword.
	/// </summary>
	/// <remarks>
	/// You will not typically need to use this type directly; it exists to satisfy the compiler's awaitable pattern.
	/// </remarks>
	public readonly record struct Awaiter : ICriticalNotifyCompletion {
#pragma warning restore CA1034
		readonly TinyFfrAsyncOperation<T> _operation;

		internal Awaiter(TinyFfrAsyncOperation<T> operation) => _operation = operation;

		/// <summary>
		/// Whether the awaited operation has finished and this awaiter is on the primary thread, and so is ready for the compiler-generated <see langword="await"/> code to call <see cref="GetResult"/>.
		/// </summary>
		public bool IsCompleted => _operation.IsCompleted && ThreadSafetyTracker.CurrentThreadIsPrimary();
		/// <inheritdoc/>
		public void OnCompleted(Action continuation) => _operation.ScheduleContinuation(continuation);
		/// <inheritdoc/>
		public void UnsafeOnCompleted(Action continuation) => _operation.ScheduleContinuation(continuation);
		/// <summary>
		/// Consumes this operation, marking it as disposed, and returns its result. Called automatically by the compiler-generated <see langword="await"/> code once <see cref="IsCompleted"/> is <see langword="true"/>.
		/// </summary>
		public T GetResult() => _operation.GetResultAndDisposeOperationWithoutWaiting();
	}
	
#pragma warning disable CA1001 // "Should be disposable" -- These objects live the lifetime of the application
	sealed class AsyncOperationTrackingData : IAsyncOperationTrackingData {
#pragma warning restore CA1001	
		readonly Lock _lock = new();
		readonly ManualResetEventSlim _completionIndicator = new(false);
		IPrimaryThreadDispatcher? _primaryThreadDispatcher = null;
		Action? _continuations = null;
		ulong _version; // This should only ever increase, otherwise owning TinyFfrAsyncOperation structs could "resurrect" if their version suddenly matches again
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

	/// <inheritdoc/>
	public bool IsCompleted {
		get {
			return IsDisposed || _trackingData.GetIsCompleted(_version);
		}
	}

	/// <inheritdoc/>
	public bool IsDisposed {
		get {
			if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
			return _version != _trackingData.Version;
		}
	}

	/// <inheritdoc/>
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

	/// <summary>
	/// Returns the awaiter used to make this operation directly awaitable via the <see langword="await"/> keyword.
	/// </summary>
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

	/// <summary>
	/// Blocks the calling thread until this operation completes. If <see cref="IsCompleted"/> is <c>true</c> this method returns immediately.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume the operation: you must still separately await it or call <see cref="GetResultAndDisposeOperation()"/> afterwards.
	/// </remarks>
	/// <exception cref="InvalidOperationException">Thrown if this operation has already been disposed/consumed (i.e. <see cref="IsDisposed"/> is <c>true</c>).</exception>
	public void WaitForCompletion() => WaitForCompletion(Timeout.InfiniteTimeSpan, default);
	/// <summary>
	/// Blocks the calling thread until this operation completes, or until <paramref name="timeout"/> elapses.
	/// If <see cref="IsCompleted"/> is <c>true</c> this method returns immediately.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume the operation: you must still separately await it or call <see cref="GetResultAndDisposeOperation()"/> afterwards.
	/// </remarks>
	/// <param name="timeout">The maximum amount of time to wait. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
	/// <returns><see langword="true"/> if the operation completed before <paramref name="timeout"/> elapsed; <see langword="false"/> otherwise.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this operation has already been disposed/consumed (i.e. <see cref="IsDisposed"/> is <c>true</c>).</exception>
	public bool WaitForCompletion(TimeSpan timeout) => WaitForCompletion(timeout, default);
	/// <summary>
	/// Blocks the calling thread until this operation completes, or until <paramref name="cancellationToken"/> is cancelled.
	/// If <see cref="IsCompleted"/> is <c>true</c> this method returns immediately.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume the operation: you must still separately await it or call <see cref="GetResultAndDisposeOperation()"/> afterwards.
	/// </remarks>
	/// <param name="cancellationToken">A token that can be used to stop waiting early.</param>
	/// <exception cref="InvalidOperationException">Thrown if this operation has already been disposed/consumed (i.e. <see cref="IsDisposed"/> is <c>true</c>).</exception>
	public void WaitForCompletion(CancellationToken cancellationToken) => WaitForCompletion(Timeout.InfiniteTimeSpan, cancellationToken);
	/// <summary>
	/// Blocks the calling thread until this operation completes, or until <paramref name="timeout"/> elapses, or until <paramref name="cancellationToken"/> is cancelled.
	/// If <see cref="IsCompleted"/> is <c>true</c> this method returns immediately.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This does not consume the operation: you must still separately await it or call <see cref="GetResultAndDisposeOperation()"/> afterwards.
	/// </remarks>
	/// <param name="timeout">The maximum amount of time to wait. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
	/// <param name="cancellationToken">A token that can be used to stop waiting early.</param>
	/// <returns><see langword="true"/> if the operation completed before <paramref name="timeout"/> elapsed; <see langword="false"/> otherwise.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this operation has already been disposed/consumed (i.e. <see cref="IsDisposed"/> is <c>true</c>).</exception>
	public bool WaitForCompletion(TimeSpan timeout, CancellationToken cancellationToken) {
		if (_trackingData == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
		return _trackingData.WaitForCompletion(_version, timeout, cancellationToken);
	}

	/// <summary>
	/// Blocks the calling thread until this operation completes, then consumes it and returns its result.
	/// If <see cref="IsCompleted"/> is <c>true</c> this method returns immediately.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This is one of the two valid ways to consume this operation (the other being to <see langword="await"/> it) — see the type-level remarks.
	/// </remarks>
	/// <exception cref="InvalidOperationException">Thrown if this operation has already been disposed/consumed (i.e. <see cref="IsDisposed"/> is <c>true</c>).</exception>
	public T GetResultAndDisposeOperation() {
		_ = GetResultAndDisposeOperation(Timeout.InfiniteTimeSpan, default, out var result);
		return result;
	}
	/// <summary>
	/// Blocks the calling thread until this operation completes or <paramref name="timeout"/> elapses; if it completed in time, consumes it and outputs its result.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. If this returns <see langword="true"/>, the operation has been consumed — see the type-level remarks. If it returns <see langword="false"/>, the operation has <i>not</i> been consumed and must still be consumed later, once it completes.
	/// </remarks>
	/// <param name="timeout">The maximum amount of time to wait. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
	/// <param name="result">Set to the operation's result if this method returns <see langword="true"/>; otherwise set to <see langword="default"/>.</param>
	/// <returns><see langword="true"/> if the operation completed (and was consumed) before <paramref name="timeout"/> elapsed; <see langword="false"/> otherwise.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this operation has already been disposed/consumed (i.e. <see cref="IsDisposed"/> is <c>true</c>).</exception>
	public bool GetResultAndDisposeOperation(TimeSpan timeout, out T result) => GetResultAndDisposeOperation(timeout, default, out result);
	/// <summary>
	/// Blocks the calling thread until this operation completes, then consumes it and returns its result.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. This is one of the two valid ways to consume this operation (the other being to <see langword="await"/> it) — see the type-level remarks.
	/// </remarks>
	/// <param name="cancellationToken">A token that can be used to stop waiting early. If cancelled before the operation completes, the operation is <i>not</i> consumed and must still be consumed later, once it completes.</param>
	/// <exception cref="InvalidOperationException">Thrown if this operation has already been disposed/consumed (i.e. <see cref="IsDisposed"/> is <c>true</c>).</exception>
	public T GetResultAndDisposeOperation(CancellationToken cancellationToken) {
		_ = GetResultAndDisposeOperation(Timeout.InfiniteTimeSpan, cancellationToken, out var result);
		return result;
	}
	/// <summary>
	/// Blocks the calling thread until this operation completes, until <paramref name="timeout"/> elapses, or until <paramref name="cancellationToken"/> is cancelled; if it completed in time, consumes it and outputs its result.
	/// </summary>
	/// <remarks>
	/// Must be called from the primary thread. If this returns <see langword="true"/>, the operation has been consumed — see the type-level remarks. If it returns <see langword="false"/>, the operation has <i>not</i> been consumed and must still be consumed later, once it completes.
	/// </remarks>
	/// <param name="timeout">The maximum amount of time to wait. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
	/// <param name="cancellationToken">A token that can be used to stop waiting early.</param>
	/// <param name="result">Set to the operation's result if this method returns <see langword="true"/>; otherwise set to <see langword="default"/>.</param>
	/// <returns><see langword="true"/> if the operation completed (and was consumed) before <paramref name="timeout"/> elapsed and before <paramref name="cancellationToken"/> was cancelled; <see langword="false"/> otherwise.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this operation has already been disposed/consumed (i.e. <see cref="IsDisposed"/> is <c>true</c>).</exception>
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

	/// <summary>
	/// Converts <paramref name="operand"/> to an untyped <see cref="TinyFfrAsyncOperation"/> referring to the same underlying operation.
	/// </summary>
	/// <remarks>
	/// This is useful when you want to track or wait on a mix of operations with different result types using the untyped APIs (e.g. <see cref="TinyFfrAsyncOperation.WaitForAllToComplete(ReadOnlySpan{TinyFfrAsyncOperation})"/>). It does not consume <paramref name="operand"/>: the result is a second handle to the same operation, and either handle can be used to consume it.
	/// </remarks>
	/// <param name="operand">The typed operation to convert.</param>
	public static implicit operator TinyFfrAsyncOperation(TinyFfrAsyncOperation<T> operand) => new(operand._trackingData, operand._version);
	/// <summary>
	/// Converts <paramref name="operand"/> back to a <see cref="TinyFfrAsyncOperation{T}"/> referring to the same underlying operation.
	/// </summary>
	/// <remarks>
	/// This is the inverse of the implicit conversion to <see cref="TinyFfrAsyncOperation"/>, and does not consume <paramref name="operand"/>.
	/// </remarks>
	/// <param name="operand">The untyped operation to convert.</param>
	/// <exception cref="InvalidCastException">Thrown if <paramref name="operand"/> does not represent an operation whose result type is <typeparamref name="T"/>.</exception>
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
