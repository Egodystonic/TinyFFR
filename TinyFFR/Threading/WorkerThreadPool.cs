// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Egodystonic.TinyFFR.Threading;

readonly unsafe struct WorkerThreadJob<TContext> where TContext : unmanaged {
	public readonly TContext Context;
	public readonly delegate* managed<ref TContext, void> Execution;
	public readonly delegate* managed<in TContext, Exception?, void> Continuation;

	public WorkerThreadJob(TContext context, delegate*<ref TContext, void> execution, delegate*<in TContext, Exception?, void> continuation) {
		Context = context;
		Execution = execution;
		Continuation = continuation;
	}
	
	public static WorkerThreadPool.JobDescriptor CreateJobDescriptor(in WorkerThreadJob<TContext> job) {
		var serializedContext = new WorkerThreadPool.SerializedJobContext();
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext, (UIntPtr) job.Execution);
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[sizeof(UIntPtr)..], (UIntPtr) job.Continuation);
		MemoryMarshal.AsBytes(new ReadOnlySpan<TContext>(in job.Context)).CopyTo(serializedContext[(sizeof(UIntPtr) * 2)..]);
		return new WorkerThreadPool.JobDescriptor(serializedContext, &DispatchSerializedExecution, &DispatchSerializedContinuation); 
	}
	
	static void DispatchSerializedExecution(ref WorkerThreadPool.SerializedJobContext ctx) {
		var executionPtr = (delegate* managed<ref TContext, void>) BinaryPrimitives.ReadUIntPtrLittleEndian(ctx);
		ref var context = ref MemoryMarshal.AsRef<TContext>(ctx[(sizeof(UIntPtr) * 2)..]);
		if (executionPtr == null) return;
		executionPtr(ref context);
	}
	
	static void DispatchSerializedContinuation(in WorkerThreadPool.SerializedJobContext ctx, Exception? exception) {
		var continuationPtr = (delegate* managed<in TContext, Exception?, void>) BinaryPrimitives.ReadUIntPtrLittleEndian(ctx[sizeof(UIntPtr)..]);
		ref readonly var context = ref MemoryMarshal.AsRef<TContext>(ctx[(sizeof(UIntPtr) * 2)..]);
		if (continuationPtr == null) return;
		continuationPtr(in context, exception);
	}
}

sealed unsafe class WorkerThreadPool : IDisposable {
	internal const int MaxJobContextSerializedSizeBytes = 128;
	internal const int JobContextSize = sizeof(ulong) * 2 + MaxJobContextSerializedSizeBytes;
	[InlineArray(MaxJobContextSerializedSizeBytes)] internal struct SerializedJobContext { byte _; }
	internal struct JobDescriptor {
		public SerializedJobContext Context;
		public readonly delegate* managed<ref SerializedJobContext, void> SerializedExecution;
		public readonly delegate* managed<in SerializedJobContext, Exception?, void> SerializedContinuation;

		public JobDescriptor(SerializedJobContext context, delegate*<ref SerializedJobContext, void> serializedExecution, delegate*<in SerializedJobContext, Exception?, void> serializedContinuation) {
			Context = context;
			SerializedExecution = serializedExecution;
			SerializedContinuation = serializedContinuation;
		}
	}  
	
	readonly BlockingCollection<JobDescriptor> _jobChannel;
	readonly CancellationTokenSource _poolDisposalCts = new();
	readonly ThreadingConfig _config;
	readonly Thread[] _threads;
	readonly bool _allJobsShouldBeSynchronous;
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable Reference kept to avoid GC collect
	readonly ThreadStart _workerThreadEntryRef;

	public WorkerThreadPool(ThreadingConfig config) {
		_config = config;
		_jobChannel = new BlockingCollection<JobDescriptor>(new ConcurrentQueue<JobDescriptor>());
		
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
	
	public void AddJob(JobDescriptor job) {
		ObjectDisposedException.ThrowIf(_poolDisposalCts.IsCancellationRequested, this);
		
		if (_allJobsShouldBeSynchronous) {
			ExecuteJob(ref job);
			return;
		}
		
		_jobChannel.Add(job);
	}
	
	void WorkerThreadEntry() {
		try {
			var disposalToken = _poolDisposalCts.Token;
			while (!disposalToken.IsCancellationRequested) {
				JobDescriptor job;
				try {
					job = _jobChannel.Take(disposalToken);
				}
				catch (Exception takeException) when (takeException is OperationCanceledException or ObjectDisposedException && disposalToken.IsCancellationRequested) {
					// Do nothing- this is just the thread pool being shut down
					break;
				}
				ExecuteJob(ref job);
			}
		}
		catch (Exception e) {
			Console.WriteLine($"{Thread.CurrentThread} failed with unhandled exception '{e}' | {e.GetAllMessages()}.");
			Console.WriteLine(e.StackTrace);
			throw;
		}
	}
	
	static void ExecuteJob(ref JobDescriptor job) {
		if (job.SerializedExecution == null || job.SerializedContinuation == null) throw new InvalidOperationException("Job execution or serialization pointer was null (this is a bug in TinyFFR).");
		try {
			job.SerializedExecution(ref job.Context);
		}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- We're passing it on to the continuation
		catch (Exception executionException) {
#pragma warning restore CA1031
			job.SerializedContinuation(in job.Context, executionException);
		}
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
		
		_jobChannel.Dispose();
	}
}