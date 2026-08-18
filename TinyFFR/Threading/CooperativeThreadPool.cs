// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Egodystonic.TinyFFR.Threading;

sealed unsafe class CooperativeThreadPool : IJobExecutionFacade, IPrimaryThreadDispatcher, IDisposable {
	readonly ArrayPoolBackedConcurrentBlockingQueue<ThreadJob> _primaryJobQueue;
	readonly ArrayPoolBackedConcurrentBlockingQueue<ThreadJob> _workerJobQueue;
	readonly CancellationTokenSource _poolDisposalCts = new();
	readonly ThreadingConfig _config;
	readonly Thread _primaryThread;
	readonly Thread[] _workerThreads;
	readonly JobCompletionRegistrar _completionRegistrar;
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable Reference kept to avoid GC collect
	readonly ThreadStart _workerThreadEntryRef;

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
		ObjectDisposedException.ThrowIf(_poolDisposalCts.IsCancellationRequested, this);
		_workerJobQueue.Enqueue(job);
	}
	
	public void AddPrimaryThreadJob(ThreadJob job) {
		ObjectDisposedException.ThrowIf(_poolDisposalCts.IsCancellationRequested, this);
		if (Thread.CurrentThread == _primaryThread) {
			job.Execute(_completionRegistrar);
			return;
		}
		
		_primaryJobQueue.Enqueue(job);
	}
	
	public void AddPrimaryThreadJobAndWait(ThreadJob job) {
		ObjectDisposedException.ThrowIf(_poolDisposalCts.IsCancellationRequested, this);
		if (Thread.CurrentThread == _primaryThread) {
			job.Execute(_completionRegistrar);
			return;
		}
		
		_completionRegistrar.RegisterInterest(job.JobId);
		_primaryJobQueue.Enqueue(job);
		_completionRegistrar.WaitForCompletion(job.JobId);
	}
	
	void IPrimaryThreadDispatcher.ExecutePendingCooperativeJobs() {
		Debug.Assert(Thread.CurrentThread == _primaryThread);
		while (_primaryJobQueue.TryDequeue(out var job)) {
			job.Execute(_completionRegistrar);
		}
	}

	bool IPrimaryThreadDispatcher.BlockPrimaryThreadUntilConditionSatisfied(ManualResetEventSlim mre, TimeSpan timeout, CancellationToken cancellationToken) {
		Debug.Assert(Thread.CurrentThread == _primaryThread);
		var pollTime = TimeSpan.FromMilliseconds(100d);
		while (!mre.IsSet) {
			((IPrimaryThreadDispatcher) this).ExecutePendingCooperativeJobs();
			var success = mre.Wait(timeout < pollTime && timeout >= TimeSpan.Zero ? timeout : pollTime, cancellationToken);
			if (success) break;
			if (timeout >= TimeSpan.Zero) {
				timeout -= pollTime;
				if (timeout < TimeSpan.Zero) return false;
			}
		}
		return true;
	}

	void WorkerThreadEntry() {
		try {
			var disposalToken = _poolDisposalCts.Token;
			while (!disposalToken.IsCancellationRequested) {
				ThreadJob job;
				try {
					job = _workerJobQueue.DequeueBlocking();
				}
				catch (Exception takeException) when (takeException is ObjectDisposedException && disposalToken.IsCancellationRequested) {
					// Do nothing- this is just the thread pool being shut down
					break;
				}
				job.Execute(_completionRegistrar);
			}
		}
		catch (Exception e) {
			Console.WriteLine($"{Thread.CurrentThread} failed with unhandled exception '{e}' | {e.GetAllMessages()}.");
			Console.WriteLine(e.StackTrace);
			throw;
		}
	}

	public void Dispose() {
		_poolDisposalCts.Dispose();
		var startTimestamp = Stopwatch.GetTimestamp();
		var remainingTime = _config.MaxShutdownWaitTime;
		var threadIdx = 0;
		while (remainingTime >= TimeSpan.Zero && threadIdx < _workerThreads.Length) {
			if (!_workerThreads[threadIdx].Join(remainingTime)) {
				Console.WriteLine($"Could not join all worker thread pool threads before {nameof(ThreadingConfig.MaxShutdownWaitTime)} elapsed.");
				return;
			}
			
			++threadIdx;
			remainingTime = Stopwatch.GetElapsedTime(startTimestamp);
		}
		
		_completionRegistrar.Dispose();
		_workerJobQueue.Dispose();
		_primaryJobQueue.Dispose();
	}
}