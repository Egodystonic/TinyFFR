// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using System.Threading;

namespace Egodystonic.TinyFFR.Threading;

sealed unsafe class CooperativeThreadPool : IJobExecutionFacade, IPrimaryThreadDispatcher, IDisposable {
	const int MaxCooperativeJobHydrationStackDepth = 8;
	int _currentCooperativeJobHydrationStackDepth = 0;

	readonly ArrayPoolBackedConcurrentBlockingQueue<ThreadJob> _primaryJobQueue;
	readonly ArrayPoolBackedConcurrentBlockingQueue<ThreadJob> _workerJobQueue;
	readonly object _primaryWakeMonitor = new();
	readonly ThreadingConfig _config;
	readonly Thread _primaryThread;
	readonly Thread[] _workerThreads;
	readonly JobCompletionRegistrar _completionRegistrar;
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable Reference kept to avoid GC collect
	readonly ThreadStart _workerThreadEntryRef;
	volatile bool _isDisposed;

	public bool HasWorkerThreads { get; }

	public CooperativeThreadPool(ThreadingConfig config, Thread primaryThread) {
		ArgumentNullException.ThrowIfNull(config);
		ArgumentNullException.ThrowIfNull(primaryThread);

		_config = config;
		_primaryThread = primaryThread;
		_primaryJobQueue = new ArrayPoolBackedConcurrentBlockingQueue<ThreadJob>();
		_workerJobQueue = new ArrayPoolBackedConcurrentBlockingQueue<ThreadJob>();
		_completionRegistrar = new();

		var threadCount = Int32.Max(0, config.WorkerThreadCount ?? System.Environment.ProcessorCount);
		HasWorkerThreads = threadCount > 0;
		_workerThreads = new Thread[threadCount];
		_workerThreadEntryRef = WorkerThreadEntry;

		for (var i = 0; i < threadCount; ++i) {
			_workerThreads[i] = new Thread(_workerThreadEntryRef) {
				IsBackground = true,
				Name = "TinyFFR Worker Background Thread"
			};
			_workerThreads[i].Start();
		}
	}

	public void AddWorkerThreadJob(ThreadJob job) {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (!HasWorkerThreads) {
			job.Execute(_completionRegistrar);
			return;
		}

		_workerJobQueue.Enqueue(job);
	}

	public void AddPrimaryThreadJob(ThreadJob job) {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (Thread.CurrentThread == _primaryThread) {
			job.Execute(_completionRegistrar);
			return;
		}

		_primaryJobQueue.Enqueue(job);
		NotifyPrimaryThreadOfEventIfCurrentlyBlocked();
	}

	public void AddPrimaryThreadJobAndWait(ThreadJob job) {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (Thread.CurrentThread == _primaryThread) {
			job.Execute(_completionRegistrar);
			return;
		}

		_completionRegistrar.RegisterInterest(job.JobId);
		try {
			_primaryJobQueue.Enqueue(job);
		}
		catch {
			_completionRegistrar.NotifyCompletion(job.JobId);
			throw;
		}

		NotifyPrimaryThreadOfEventIfCurrentlyBlocked();

		if (!_completionRegistrar.WaitForCompletion(job.JobId, Timeout.InfiniteTimeSpan, CancellationToken.None)) {
			throw new ObjectDisposedException(ToString(), $"{nameof(CooperativeThreadPool)} was disposed while waiting for a primary thread job to complete.");
		}
	}

	void IPrimaryThreadDispatcher.ExecutePendingCooperativeJobs() => ExecutePendingCooperativeJobs();

	void ExecutePendingCooperativeJobs() {
		Debug.Assert(Thread.CurrentThread == _primaryThread);
		if (_isDisposed) return;
		if (_currentCooperativeJobHydrationStackDepth >= MaxCooperativeJobHydrationStackDepth) return;

		++_currentCooperativeJobHydrationStackDepth;
		try {
			while (!_isDisposed && _primaryJobQueue.TryDequeue(out var job)) {
				job.Execute(_completionRegistrar);
			}
		}
		finally {
			--_currentCooperativeJobHydrationStackDepth;
		}
	}

	public void NotifyPrimaryThreadOfEventIfCurrentlyBlocked() {
		lock (_primaryWakeMonitor) {
			Monitor.PulseAll(_primaryWakeMonitor);
		}
	}

	bool IPrimaryThreadDispatcher.BlockPrimaryThreadUntilConditionSatisfied(ManualResetEventSlim mre, TimeSpan timeout, CancellationToken cancellationToken) {
		ArgumentNullException.ThrowIfNull(mre);
		Debug.Assert(Thread.CurrentThread == _primaryThread);

		var startTimestamp = Stopwatch.GetTimestamp();
		var hasTimeout = timeout >= TimeSpan.Zero;
		
		var cancellationRegistration = cancellationToken.CanBeCanceled
			? cancellationToken.UnsafeRegister(static state => ((CooperativeThreadPool) state!).NotifyPrimaryThreadOfEventIfCurrentlyBlocked(), this)
			: default;

		try {
			while (true) {
				ExecutePendingCooperativeJobs();
				if (mre.IsSet) return true;
				cancellationToken.ThrowIfCancellationRequested();
				if (_isDisposed) return mre.IsSet;

				var remainingTime = Timeout.InfiniteTimeSpan;
				if (hasTimeout) {
					remainingTime = timeout - Stopwatch.GetElapsedTime(startTimestamp);
					if (remainingTime <= TimeSpan.Zero) return false;
				}

				lock (_primaryWakeMonitor) {
					if (mre.IsSet || _primaryJobQueue.HasPendingItems || cancellationToken.IsCancellationRequested) continue;
					Monitor.Wait(_primaryWakeMonitor, remainingTime);
				}
			}
		}
		finally {
			cancellationRegistration.Dispose();
		}
	}

	void WorkerThreadEntry() {
		while (!_isDisposed) {
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

	public void Dispose() {
		if (_isDisposed) return;
		_isDisposed = true;

		_workerJobQueue.Seal();
		_primaryJobQueue.Seal();
		NotifyPrimaryThreadOfEventIfCurrentlyBlocked();

		var startTimestamp = Stopwatch.GetTimestamp();
		foreach (var workerThread in _workerThreads) {
			var remainingTime = _config.MaxShutdownWaitTime - Stopwatch.GetElapsedTime(startTimestamp);
			if (remainingTime < TimeSpan.Zero) remainingTime = TimeSpan.Zero;

			if (workerThread.Join(remainingTime)) continue;
			Console.WriteLine($"Could not join all worker thread pool threads before {nameof(ThreadingConfig.MaxShutdownWaitTime)} elapsed.");
			break;
		}

		AbandonRemainingJobs(_workerJobQueue);
		AbandonRemainingJobs(_primaryJobQueue);

		_completionRegistrar.Dispose();
		_workerJobQueue.Dispose();
		_primaryJobQueue.Dispose();
	}

	void AbandonRemainingJobs(ArrayPoolBackedConcurrentBlockingQueue<ThreadJob> queue) {
		while (queue.TryDequeue(out var job)) {
			job.Cancel(_completionRegistrar);
		}
	}
}
