// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Egodystonic.TinyFFR.Threading;

readonly unsafe struct WorkerThreadJob<TContext, TResult> where TContext : unmanaged {
	public readonly TinyFfrAsyncOperation<TResult> SharedOperation;
	public readonly TContext Context;
	public readonly delegate* managed<TContext, TResult> WorkerThreadWork;

	public WorkerThreadJob(TinyFfrAsyncOperation<TResult> sharedOperation, TContext context, delegate* managed<TContext, TResult> workerThreadWork) {
		InvalidObjectException.ThrowIfDefault(sharedOperation);
		ArgumentNullException.ThrowIfNull(workerThreadWork);
		SharedOperation = sharedOperation;
		Context = context;
		WorkerThreadWork = workerThreadWork;
	}
}

sealed unsafe class WorkerThreadPool : IWorkSchedulerFacade, IDisposable {
	public const int MaxJobContextSerializedSizeBytes = 96;
	const int QueuedJobSerializedContextSizeBytes = sizeof(ulong) + TinyFfrAsyncOperation.SmuggleSizeBytes + MaxJobContextSerializedSizeBytes;
	[InlineArray(QueuedJobSerializedContextSizeBytes)] internal struct SerializedJobContext { byte _; }
	struct QueuedJob {
		public readonly SerializedJobContext Context;
		public readonly delegate* managed<in SerializedJobContext, void> WorkerThreadWork;

		public QueuedJob(SerializedJobContext context, delegate*<in SerializedJobContext, void> workerThreadWork) {
			Context = context;
			WorkerThreadWork = workerThreadWork;
		}
	}  
	
	readonly BlockingCollection<QueuedJob> _jobQueue;
	readonly CancellationTokenSource _poolDisposalCts = new();
	readonly ThreadingConfig _config;
	readonly Thread[] _threads;
	readonly bool _allJobsShouldBeSynchronous;
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable Reference kept to avoid GC collect
	readonly ThreadStart _workerThreadEntryRef;

	public WorkerThreadPool(ThreadingConfig config) {
		_config = config;
		_jobQueue = new BlockingCollection<QueuedJob>(new ConcurrentQueue<QueuedJob>());
		
		var threadCount = Int32.Max(0, config.WorkerThreadCount ?? System.Environment.ProcessorCount);
		_allJobsShouldBeSynchronous = threadCount > 0;
		_threads = new Thread[threadCount];
		_workerThreadEntryRef = WorkerThreadEntry;
		
		for (var i = 0; i < threadCount; ++i) {
			_threads[i] = new Thread(_workerThreadEntryRef) {
				IsBackground = true,
				Name = "TinyFFR Worker Background Thread"
			};
			_threads[i].Start();
		}
	}
	
	public void AddJob<TContext, TResult>(WorkerThreadJob<TContext, TResult> job) where TContext : unmanaged {
		ObjectDisposedException.ThrowIf(_poolDisposalCts.IsCancellationRequested, this);
		if (job.WorkerThreadWork == null) throw new ArgumentException("Given job had null worker thread work pointer.", nameof(job));
		InvalidObjectException.ThrowIfDefault(job.SharedOperation);
		
		static void DispatchSerializedExecution(in SerializedJobContext ctx) {
			var executionPtr = (delegate* managed<TContext, TResult>) BinaryPrimitives.ReadUIntPtrLittleEndian(ctx);
			var sharedOperation = TinyFfrAsyncOperation<TResult>.DeSmuggle(ctx[sizeof(UIntPtr)..]);
			var context = MemoryMarshal.AsRef<TContext>(ctx[(sizeof(UIntPtr) + TinyFfrAsyncOperation.SmuggleSizeBytes)..]);
			
			try {
				if (executionPtr == null) throw new InvalidOperationException("Deserialized execution pointer was null (this is a bug in TinyFFR).");
				var result = executionPtr(context);
				sharedOperation.SetResult(result);
			}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- We're passing it on to the continuation
			catch (Exception e) {
#pragma warning restore CA1031
				sharedOperation.SetException(e);
			}
		}
		
		static QueuedJob TranslateToInternalJobRepresentation(WorkerThreadJob<TContext, TResult> externalRepresentation) {
			var serializedContext = new SerializedJobContext();
			BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext, (UIntPtr) externalRepresentation.WorkerThreadWork);
			Span<byte> smuggledSharedOperation = stackalloc byte[TinyFfrAsyncOperation.SmuggleSizeBytes];
			TinyFfrAsyncOperation<TResult>.Smuggle(in externalRepresentation.SharedOperation, smuggledSharedOperation);
			smuggledSharedOperation.CopyTo(serializedContext[sizeof(UIntPtr)..]);
			MemoryMarshal.AsBytes(new ReadOnlySpan<TContext>(in externalRepresentation.Context)).CopyTo(serializedContext[(sizeof(UIntPtr) + TinyFfrAsyncOperation.SmuggleSizeBytes)..]);
			return new QueuedJob(serializedContext, &DispatchSerializedExecution); 
		}
		
		var internalRepresentation = TranslateToInternalJobRepresentation(job);
		
		if (_allJobsShouldBeSynchronous) ExecuteJob(in internalRepresentation);
		else _jobQueue.Add(internalRepresentation);
	}
	
	void WorkerThreadEntry() {
		try {
			var disposalToken = _poolDisposalCts.Token;
			while (!disposalToken.IsCancellationRequested) {
				QueuedJob job;
				try {
					job = _jobQueue.Take(disposalToken);
				}
				catch (Exception takeException) when (takeException is OperationCanceledException or ObjectDisposedException && disposalToken.IsCancellationRequested) {
					// Do nothing- this is just the thread pool being shut down
					break;
				}
				ExecuteJob(in job);
			}
		}
		catch (Exception e) {
			Console.WriteLine($"{Thread.CurrentThread} failed with unhandled exception '{e}' | {e.GetAllMessages()}.");
			Console.WriteLine(e.StackTrace);
			throw;
		}
	}
	
	static void ExecuteJob(in QueuedJob job) {
		if (job.WorkerThreadWork == null) throw new InvalidOperationException("Job work pointer was null (this is a bug in TinyFFR).");
		job.WorkerThreadWork(in job.Context);
	}

	public void Dispose() {
		_poolDisposalCts.Dispose();
		var startTimestamp = Stopwatch.GetTimestamp();
		var remainingTime = _config.MaxShutdownWaitTime;
		var threadIdx = 0;
		while (remainingTime >= TimeSpan.Zero && threadIdx < _threads.Length) {
			if (!_threads[threadIdx].Join(remainingTime)) {
				Console.WriteLine($"Could not join all worker thread pool threads before {nameof(ThreadingConfig.MaxShutdownWaitTime)} elapsed.");
				return;
			}
			
			++threadIdx;
			remainingTime = Stopwatch.GetElapsedTime(startTimestamp);
		}
		
		_jobQueue.Dispose();
	}
}