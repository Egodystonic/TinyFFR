// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Egodystonic.TinyFFR.Threading;

sealed unsafe class CooperativeThreadPool : IJobExecutionFacade, IPrimaryThreadDispatcher, IDisposable {
	static readonly TimeSpan ShutdownDrainPollInterval = TimeSpan.FromMilliseconds(10d);
	static readonly SendOrPostCallback HostPumpCallback = static state => ((CooperativeThreadPool) state!).ExecuteHostRequestedPump();

	readonly ArrayPoolBackedConcurrentBlockingQueue<ThreadJob> _primaryJobQueue;
	readonly ArrayPoolBackedConcurrentBlockingQueue<ThreadJob> _workerJobQueue;
	readonly object _primaryWakeMonitor = new();
	readonly ThreadingConfig _config;
	readonly Thread[] _workerThreads;
	readonly JobCompletionRegistrar _completionRegistrar;
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable Reference kept to avoid GC collect
	readonly ThreadStart _workerThreadEntryRef;
	readonly SynchronizationContext? _pumpableHostSyncContext;
	readonly Lock _hostPumpLock = new();
	int _liveWorkerThreadCount;
	bool _hostPumpRequestOutstanding;
	bool _hasLoggedForeignThreadPump;
	bool _hasLoggedFailedPost;
	volatile bool _isDisposing;
	volatile bool _isDisposed;

	public bool HasWorkerThreads { get; }

	public CooperativeThreadPool(ThreadingConfig config) {
		ArgumentNullException.ThrowIfNull(config);

		_config = config;
		if (config.WakeHostMessageLoopForPrimaryThreadWork && SynchronizationContext.Current is { } hostSyncContext and not TinyFfrSynchronizationContext) {
			_pumpableHostSyncContext = hostSyncContext;
		}
		_primaryJobQueue = new ArrayPoolBackedConcurrentBlockingQueue<ThreadJob>();
		_workerJobQueue = new ArrayPoolBackedConcurrentBlockingQueue<ThreadJob>();
		_completionRegistrar = new();

		var threadCount = Int32.Max(0, config.WorkerThreadCount ?? System.Environment.ProcessorCount);
		HasWorkerThreads = threadCount > 0;
		_workerThreads = new Thread[threadCount];
		_workerThreadEntryRef = WorkerThreadEntry;
		_liveWorkerThreadCount = threadCount;

		for (var i = 0; i < threadCount; ++i) {
			_workerThreads[i] = new Thread(_workerThreadEntryRef) {
				IsBackground = true,
				Name = "TinyFFR Worker Background Thread"
			};
			_workerThreads[i].Start();
		}
	}

	void EnqueueOrCancel(ArrayPoolBackedConcurrentBlockingQueue<ThreadJob> queue, ThreadJob job) {
		try {
			queue.Enqueue(job);
		}
		catch {
			job.Cancel(_completionRegistrar);
			throw;
		}
	}

	[DoesNotReturn]
	void CancelJobAndThrowObjectDisposed(ThreadJob job) {
		job.Cancel(_completionRegistrar);
		throw new ObjectDisposedException(ToString());
	}

	public void AddWorkerThreadJob(ThreadJob job) {
		if (_isDisposing) CancelJobAndThrowObjectDisposed(job);
		if (!HasWorkerThreads) {
			job.Execute(_completionRegistrar);
			return;
		}

		EnqueueOrCancel(_workerJobQueue, job);
	}

	public void AddPrimaryThreadJob(ThreadJob job) {
		if (_isDisposed) CancelJobAndThrowObjectDisposed(job);
		if (ThreadSafetyTracker.CurrentThreadIsPrimary()) {
			job.Execute(_completionRegistrar);
			return;
		}

		EnqueueOrCancel(_primaryJobQueue, job);
		NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
		RequestHostPumpIfPossible();
	}

	public void AddPrimaryThreadJobAndWait(ThreadJob job) {
		if (_isDisposed) CancelJobAndThrowObjectDisposed(job);
		if (ThreadSafetyTracker.CurrentThreadIsPrimary()) {
			job.Execute(_completionRegistrar);
			return;
		}

		_completionRegistrar.RegisterInterest(job.JobId);
		EnqueueOrCancel(_primaryJobQueue, job);
		NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
		RequestHostPumpIfPossible();

		if (!_completionRegistrar.WaitForCompletion(job.JobId, Timeout.InfiniteTimeSpan, CancellationToken.None)) {
			throw new ObjectDisposedException(ToString(), $"{nameof(CooperativeThreadPool)} was disposed while waiting for a primary thread job to complete.");
		}
	}

	void IPrimaryThreadDispatcher.ExecutePendingCooperativeJobs(TimeSpan? targetExecutionTimeCap) => ExecutePendingCooperativeJobs(targetExecutionTimeCap);

	void ExecutePendingCooperativeJobs(TimeSpan? targetExecutionTimeCap) {
		if (_isDisposed) return;
		ExecutePendingCooperativeJobsRegardlessOfDisposalState(targetExecutionTimeCap);
	}

	void ExecutePendingCooperativeJobsRegardlessOfDisposalState(TimeSpan? targetExecutionTimeCap) {
		Debug.Assert(ThreadSafetyTracker.CurrentThreadIsPrimary());

		var startTimestamp = Stopwatch.GetTimestamp();
		while (_primaryJobQueue.TryDequeue(out var job)) {
			try {
				job.Execute(_completionRegistrar);
			}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- We're logging it, not much else to do
			catch (Exception e) {
#pragma warning restore CA1031
				Console.WriteLine($"{Thread.CurrentThread} failed to execute a primary thread job with unhandled exception '{e}' | {e.GetAllMessages()}.");
				Console.WriteLine(e.StackTrace);
			}
			if (targetExecutionTimeCap is { } maxTime && Stopwatch.GetElapsedTime(startTimestamp) >= maxTime) break;
		}
	}

	public bool SchedulePrimaryThreadContinuation(Action continuation) {
		ArgumentNullException.ThrowIfNull(continuation);

		static Unused InvokeContinuation(Action? continuation) {
			InvokeContinuationAndLogAnyError(continuation!);
			return default;
		}
		static void CompleteContinuation(Action? continuation, Exception? error, Unused result) {
			if (error == null) return;
			InvokeContinuationOnPrimaryThreadOrDiscard(continuation!);
		}

		if (_isDisposed) return InvokeContinuationOnPrimaryThreadOrDiscard(continuation);

		var job = ThreadJob.CreateWithManagedContextUnmanagedResult(continuation, &InvokeContinuation, &CompleteContinuation);
		try {
			_primaryJobQueue.Enqueue(job);
		}
		catch (ObjectDisposedException) {
			job.Cancel(_completionRegistrar);
			return ThreadSafetyTracker.CurrentThreadIsPrimary();
		}
		NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
		RequestHostPumpIfPossible();
		return true;
	}

	void RequestHostPumpIfPossible() {
		if (_pumpableHostSyncContext == null) return;

		lock (_hostPumpLock) { // This ensures we don't accidentally get in to a loop
			if (_hostPumpRequestOutstanding) return;
			_hostPumpRequestOutstanding = true;
		}

		try {
			_pumpableHostSyncContext.Post(HostPumpCallback, this);
		}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- A failed host post must not propagate in to unrelated job scheduling
		catch (Exception e) {
#pragma warning restore CA1031
			lock (_hostPumpLock) {
				_hostPumpRequestOutstanding = false;
				if (_hasLoggedFailedPost) return;
				_hasLoggedFailedPost = true;
			}
			Console.WriteLine(
				$"Could not post primary thread work to the host synchronization context: {e.GetAllMessages()}. " +
				$"Pending primary thread work will only be executed by an application loop iteration. This message is only shown once."
			);
		}
	}

	void ExecuteHostRequestedPump() {
		lock (_hostPumpLock) _hostPumpRequestOutstanding = false;
		if (_isDisposed) return;

		if (!ThreadSafetyTracker.CurrentThreadIsPrimary()) {
			LogForeignThreadPumpOnce();
			return;
		}

		ExecutePendingCooperativeJobs(_config.HostPumpTimeCap);
	}

	void LogForeignThreadPumpOnce() {
		lock (_hostPumpLock) {
			if (_hasLoggedForeignThreadPump) return;
			_hasLoggedForeignThreadPump = true;
		}
		Console.WriteLine(
			$"Host synchronization context did not dispatch a primary thread work pump to the primary thread. " +
			$"Pending primary thread work will only be executed by an application loop iteration. This message is only shown once."
		);
	}

	static bool InvokeContinuationOnPrimaryThreadOrDiscard(Action continuation) {
		if (!ThreadSafetyTracker.CurrentThreadIsPrimary()) {
			Console.WriteLine(
				$"Discarding a primary thread continuation scheduled from {Thread.CurrentThread} after {nameof(CooperativeThreadPool)} disposal: " +
				$"it can not be invoked on the primary thread."
			);
			return false;
		}
		InvokeContinuationAndLogAnyError(continuation);
		return true;
	}

	static void InvokeContinuationAndLogAnyError(Action continuation) {
		try {
			continuation();
		}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- We're logging it, not much else to do
		catch (Exception e) {
#pragma warning restore CA1031
			Console.WriteLine($"Primary thread continuation failed with unhandled exception '{e}' | {e.GetAllMessages()}.");
			Console.WriteLine(e.StackTrace);
		}
	}

	public void NotifyPrimaryThreadOfEventIfCurrentlyBlocked() {
		lock (_primaryWakeMonitor) {
			Monitor.PulseAll(_primaryWakeMonitor);
		}
	}

	bool IPrimaryThreadDispatcher.BlockPrimaryThreadUntilConditionSatisfied(ManualResetEventSlim mre, IAsyncOperationTrackingData? versionOwner, ulong expectedVersion, TimeSpan timeout, CancellationToken cancellationToken) {
		ArgumentNullException.ThrowIfNull(mre);
		Debug.Assert(ThreadSafetyTracker.CurrentThreadIsPrimary());

		var startTimestamp = Stopwatch.GetTimestamp();
		var hasTimeout = timeout >= TimeSpan.Zero;

		static bool ConditionIsSatisfied(ManualResetEventSlim mre, IAsyncOperationTrackingData? versionOwner, ulong expectedVersion) {
			return mre.IsSet || (versionOwner != null && versionOwner.Version != expectedVersion);
		}

		var cancellationRegistration = cancellationToken.CanBeCanceled
			? cancellationToken.UnsafeRegister(static state => ((CooperativeThreadPool) state!).NotifyPrimaryThreadOfEventIfCurrentlyBlocked(), this)
			: default;

		try {
			while (true) {
				if (ConditionIsSatisfied(mre, versionOwner, expectedVersion)) return true;

				var remainingTime = Timeout.InfiniteTimeSpan;
				if (hasTimeout) {
					remainingTime = timeout - Stopwatch.GetElapsedTime(startTimestamp);
					if (remainingTime <= TimeSpan.Zero) return false;
				}

				ExecutePendingCooperativeJobs(hasTimeout ? remainingTime : null);

				if (ConditionIsSatisfied(mre, versionOwner, expectedVersion)) return true;
				cancellationToken.ThrowIfCancellationRequested();
				if (_isDisposed) return ConditionIsSatisfied(mre, versionOwner, expectedVersion);

				if (hasTimeout) {
					remainingTime = timeout - Stopwatch.GetElapsedTime(startTimestamp);
					if (remainingTime <= TimeSpan.Zero) return false;
				}

				lock (_primaryWakeMonitor) {
					if (_isDisposed || ConditionIsSatisfied(mre, versionOwner, expectedVersion) || _primaryJobQueue.HasPendingItems || cancellationToken.IsCancellationRequested) continue;
					Monitor.Wait(_primaryWakeMonitor, remainingTime);
				}
			}
		}
		finally {
			cancellationRegistration.Dispose();
		}
	}

	void WorkerThreadEntry() {
		try {
			while (true) {
				ThreadJob job;
				try {
					if (!_workerJobQueue.TryDequeueBlocking(out job)) break;
				}
				catch (ObjectDisposedException) {
					break;
				}

				try {
					job.Execute(_completionRegistrar);
				}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- We're logging it, not much else to do
				catch (Exception e) {
#pragma warning restore CA1031
					Console.WriteLine($"{Thread.CurrentThread} failed with unhandled exception '{e}' | {e.GetAllMessages()}.");
					Console.WriteLine(e.StackTrace);
				}
			}
		}
		finally {
			Interlocked.Decrement(ref _liveWorkerThreadCount);
			NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
		}
	}

	public void Dispose() {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		if (_isDisposed) return;
		_isDisposing = true;

		_workerJobQueue.Seal();

		var startTimestamp = Stopwatch.GetTimestamp();
		while (true) {
			ExecutePendingCooperativeJobsRegardlessOfDisposalState(null);
			if (Volatile.Read(ref _liveWorkerThreadCount) <= 0 && !_primaryJobQueue.HasPendingItems) break;
			if (Stopwatch.GetElapsedTime(startTimestamp) >= _config.MaxShutdownWaitTime) break;

			lock (_primaryWakeMonitor) {
				if (_primaryJobQueue.HasPendingItems || Volatile.Read(ref _liveWorkerThreadCount) <= 0) continue;
				Monitor.Wait(_primaryWakeMonitor, ShutdownDrainPollInterval);
			}
		}

		_isDisposed = true;
		_primaryJobQueue.Seal();

		foreach (var workerThread in _workerThreads) {
			var remainingTime = _config.MaxShutdownWaitTime - Stopwatch.GetElapsedTime(startTimestamp);
			if (remainingTime < TimeSpan.Zero) remainingTime = TimeSpan.Zero;

			if (workerThread.Join(remainingTime)) continue;
			Console.WriteLine(
				$"Could not join all worker thread pool threads before {nameof(ThreadingConfig.MaxShutdownWaitTime)} elapsed. " +
				$"Resources may be torn down while worker threads are still executing."
			);
			break;
		}

		AbandonRemainingJobs(_workerJobQueue);
		AbandonRemainingJobs(_primaryJobQueue);

		try {
			OutstandingAsyncOperationRegistry.FaultAllOutstanding(new ObjectDisposedException(ToString(), $"{nameof(CooperativeThreadPool)} was disposed before this operation completed."));
		}
		finally {
			Debug.Assert(!_workerJobQueue.HasPendingItems);
			Debug.Assert(!_primaryJobQueue.HasPendingItems);
			_completionRegistrar.Dispose();
			_workerJobQueue.Dispose();
			_primaryJobQueue.Dispose();
		}
	}

	void AbandonRemainingJobs(ArrayPoolBackedConcurrentBlockingQueue<ThreadJob> queue) {
		while (queue.TryDequeue(out var job)) {
			job.Cancel(_completionRegistrar);
		}
	}
}
