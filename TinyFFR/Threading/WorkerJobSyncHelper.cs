// Created on 2026-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Threading;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Threading;

sealed unsafe class WorkerJobSyncHelper<TSelf, TContext, TConfig> : IDisposable where TContext : WorkerJobSyncHelper<TSelf, TContext, TConfig>.WorkerJobSyncHelperContext, new() where TConfig : struct, IConfigStruct<TConfig>, allows ref struct {
	public abstract class WorkerJobSyncHelperContext {
		public TSelf Self { get; set; } = default!;
		public IPrimaryThreadDispatcher PrimaryThreadDispatcher { get; set; } = null!;
		public ThreadSafeHeapPoolWrapper HeapPool { get; set; } = null!;
		public IJobExecutionFacade JobDispatcher { get; set; } = null!;
		public PooledHeapMemory<byte>? HeapPoolSerializedConfig { get; set; } = null;
		public PooledHeapMemory<char>? HeapPoolName { get; set; } = null;
		public ReadOnlySpan<char> Name => HeapPoolName.HasValue ? HeapPoolName.Value.Span : default;
		public abstract void TearDown();
		
		readonly byte[] _generatedResourceData = new byte[IResource.SerializedLengthBytes];
		Exception? _dispatchException = null;
		void* _dispatchWorkPtr = null;
		public TResource GenerateResourceOnPrimaryAndWait<TResource>(delegate* managed<TContext, TResource> work) where TResource : IResource<TResource> {
			Array.Clear(_generatedResourceData);
			_dispatchException = null;
			_dispatchWorkPtr = work;
			
			static Unused Work(TContext? @this) {
				if (@this!._dispatchWorkPtr == null) throw new InvalidOperationException("Dispatch work pointer was null (this is a bug in TinyFFR).");
				var resource = ((delegate* managed<TContext, TResource>) @this._dispatchWorkPtr)(@this);
				resource.AllocateGcHandleAndSerializeResource(@this._generatedResourceData);
				return default;
			}
			
			static void Completion(TContext? @this, Exception? error, Unused _) {
				@this!._dispatchException = error;
			}
			
			JobDispatcher.AddPrimaryThreadJobAndWait(ThreadJob.CreateWithManagedContextUnmanagedResult((TContext) this, &Work, &Completion));
			
			if (_dispatchException is { } ex) throw new AggregateException($"Could not create resource ({ex.GetAllMessages()}).", ex);
			return TResource.CreateFromSerializedAndFreeAllocatedGcHandle(_generatedResourceData);
		}
		
		public void DispatchOnPrimaryAndWait(delegate* managed<TContext, void> work) {
			_dispatchException = null;
			_dispatchWorkPtr = work;
			
			static Unused Work(TContext? @this) {
				if (@this!._dispatchWorkPtr == null) throw new InvalidOperationException("Dispatch work pointer was null (this is a bug in TinyFFR).");
				((delegate* managed<TContext, void>) @this._dispatchWorkPtr)(@this);
				return default;
			}
			
			static void Completion(TContext? @this, Exception? error, Unused _) {
				@this!._dispatchException = error;
			}
			
			JobDispatcher.AddPrimaryThreadJobAndWait(ThreadJob.CreateWithManagedContextUnmanagedResult((TContext) this, &Work, &Completion));
			
			if (_dispatchException is { } ex) throw new AggregateException($"Could not complete async work ({ex.GetAllMessages()}).", ex);
		}
		
		public void SetConfig(in TConfig cfg) {
			if (HeapPoolSerializedConfig.HasValue) throw new InvalidOperationException("Config already set (this is a bug in TinyFFR).");
			HeapPoolSerializedConfig = HeapPool.Borrow(TConfig.GetHeapStorageFormattedLength(in cfg));
			TConfig.AllocateAndConvertToHeapStorage(HeapPoolSerializedConfig.Value.Span, in cfg);
		}
		
		public void SetName(ReadOnlySpan<char> name) {
			if (name.IsEmpty) return;
			if (HeapPoolName.HasValue) throw new InvalidOperationException("Heap pool name already set (this is a bug in TinyFFR).");
			HeapPoolName = HeapPool.BorrowAndCopy(name);
		}
		
		public TConfig Config {
			get {
				if (!HeapPoolSerializedConfig.HasValue) throw new InvalidOperationException("Config not set (this is a bug in TinyFFR).");
				return TConfig.ConvertFromAllocatedHeapStorage(HeapPoolSerializedConfig.Value.Span);
			}
		}
	}
	
	public sealed class WorkerJobSyncContextWrapper {
		public TContext Context { get; }
		WorkerJobSyncHelper<TSelf, TContext, TConfig> OwningHelper { get; } 
		void* WorkPtr { get; set; } = null;

		public WorkerJobSyncContextWrapper(TContext context, WorkerJobSyncHelper<TSelf, TContext, TConfig> owningHelper) {
			Context = context;
			OwningHelper = owningHelper;
			Context.Self = owningHelper._self;
		}
		
		static Unused TearDownContextOnPrimaryThread(WorkerJobSyncContextWrapper? wrapper) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			ArgumentNullException.ThrowIfNull(wrapper);
			wrapper.WorkPtr = null;
			wrapper.Context.TearDown();
			if (wrapper.Context.HeapPoolSerializedConfig is { } hpsc) {
				TConfig.DisposeAllocatedHeapStorage(hpsc.Span);
				hpsc.Dispose();
			}
			wrapper.Context.HeapPoolName?.Dispose();
			wrapper.Context.HeapPoolName = null!;
			wrapper.Context.HeapPool = null!;
			wrapper.Context.JobDispatcher = null!;
			wrapper.Context.PrimaryThreadDispatcher = null!;
			wrapper.Context.Self = default!;
			return default;
		}
		
		static void ReturnContextToPoolIfNoErrorOnPrimaryThread(WorkerJobSyncContextWrapper? wrapper, Exception? error, Unused result) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			
			// If there's an error the teardown could have left the context object in a half-torn-down state
			// which we should just release to the GC rather than putting back in to the pool
			if (error != null) {
				Console.WriteLine($"Error in teardown for asynchronous context (of type {typeof(TContext)}): {error.GetAllMessages()}");
				Console.WriteLine(error.StackTrace);
				return;
			} 
			if (wrapper == null) {
				Console.WriteLine($"Error in teardown for asynchronous context (of type {typeof(TContext)}): Null wrapper");
				return;
			}
			
			wrapper.OwningHelper._contextWrapperPool.Return(wrapper);
		}
		
		void SetUpContextOnPrimaryThread(bool synchronous) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			Context.Self = OwningHelper._self;
			Context.PrimaryThreadDispatcher = OwningHelper._primaryThreadDispatcher;
			Context.JobDispatcher = synchronous ? OwningHelper._synchronousJobDispatcher : OwningHelper._asynchronousJobDispatcher;
		}
		
		public TResult? DispatchArbitrarySynchronousOperation<TResult>(delegate* managed<TContext, TResult?> work) where TResult : class {
			try {
				SetUpContextOnPrimaryThread(true);
				return work(Context);
			}
			finally {
				var exceptionCaught = false;
				try {
					TearDownContextOnPrimaryThread(this);
				}
#pragma warning disable CA1031 // Exception is propagated to logging method
				catch (Exception e) {
#pragma warning restore CA1031
					exceptionCaught = true;
					ReturnContextToPoolIfNoErrorOnPrimaryThread(this, e, default);
				}
				if (!exceptionCaught) ReturnContextToPoolIfNoErrorOnPrimaryThread(this, null, default); 
			}
		}
		
		public TResult DispatchResourceReturningSynchronousOperation<TResult>(delegate* managed<TContext, in TConfig, TResult> work, in TConfig config) where TResult : struct, IResource<TResult> {
			try {
				SetUpContextOnPrimaryThread(true);
				return work(Context, in config);
			}
			finally {
				var exceptionCaught = false;
				try {
					TearDownContextOnPrimaryThread(this);
				}
#pragma warning disable CA1031 // Exception is propagated to logging method
				catch (Exception e) {
#pragma warning restore CA1031
					exceptionCaught = true;
					ReturnContextToPoolIfNoErrorOnPrimaryThread(this, e, default);
				}
				if (!exceptionCaught) ReturnContextToPoolIfNoErrorOnPrimaryThread(this, null, default);
			}
		}

		public TinyFfrAsyncOperation<TResult?> DispatchArbitraryAsynchronousOperation<TResult>(delegate* managed<TContext, TResult?> work) where TResult : class {
			static TResult? Work(WorkerJobSyncContextWrapper? contextWrapper) {
				if (contextWrapper == null || contextWrapper.WorkPtr == null) {
					throw new InvalidOperationException("Context wrapper or its work pointer was null (this is a bug in TinyFFR).");
				}

				try {
					return ((delegate* managed<TContext, TResult?>) contextWrapper.WorkPtr)(contextWrapper.Context);
				}
				finally {
					contextWrapper.Context.JobDispatcher.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(contextWrapper, &TearDownContextOnPrimaryThread, &ReturnContextToPoolIfNoErrorOnPrimaryThread));
				}
			}
			
			SetUpContextOnPrimaryThread(false);
			WorkPtr = work;
			var result = new TinyFfrAsyncOperation<TResult?>(OwningHelper._primaryThreadDispatcher);
			var job = ThreadJob.CreateWithAsyncOpArbitraryResult(this, &Work, result);
			OwningHelper._asynchronousJobDispatcher.AddWorkerThreadJob(job);
			return result;
		}
		
		public TinyFfrAsyncOperation<TResult> DispatchResourceReturningAsynchronousOperation<TResult>(delegate* managed<TContext, in TConfig, TResult> work, in TConfig config) where TResult : struct, IResource<TResult> {
			static TResult Work(WorkerJobSyncContextWrapper? contextWrapper) {
				if (contextWrapper == null || contextWrapper.WorkPtr == null) {
					throw new InvalidOperationException("Context wrapper or its work pointer was null (this is a bug in TinyFFR).");
				}

				try {
					return ((delegate* managed<TContext, in TConfig, TResult>) contextWrapper.WorkPtr)(contextWrapper.Context, contextWrapper.Context.Config);
				}
				finally {
					contextWrapper.Context.JobDispatcher.AddPrimaryThreadJob(ThreadJob.CreateWithManagedContextUnmanagedResult(contextWrapper, &TearDownContextOnPrimaryThread, &ReturnContextToPoolIfNoErrorOnPrimaryThread));
				}
			}
			
			SetUpContextOnPrimaryThread(false);
			Context.SetConfig(in config);
			WorkPtr = work;
			var result = new TinyFfrAsyncOperation<TResult>(OwningHelper._primaryThreadDispatcher);
			var job = ThreadJob.CreateWithAsyncOpResourceResult(this, &Work, result);
			OwningHelper._asynchronousJobDispatcher.AddWorkerThreadJob(job);
			return result;
		}
	}
	readonly ArrayPoolBackedObjectPool<WorkerJobSyncContextWrapper, WorkerJobSyncHelper<TSelf, TContext, TConfig>> _contextWrapperPool;
	readonly TSelf _self;
	readonly ThreadSafeHeapPoolWrapper _heapPool;
	readonly IPrimaryThreadDispatcher _primaryThreadDispatcher;
	readonly IJobExecutionFacade _synchronousJobDispatcher;
	readonly IJobExecutionFacade _asynchronousJobDispatcher;

	public WorkerJobSyncHelper(TSelf self, ThreadSafeHeapPoolWrapper heapPool, IPrimaryThreadDispatcher primaryThreadDispatcher, IJobExecutionFacade synchronousJobDispatcher, IJobExecutionFacade asynchronousJobDispatcher) {
		_contextWrapperPool = new(&CreateInstance, this);
		_self = self;
		_heapPool = heapPool;
		_primaryThreadDispatcher = primaryThreadDispatcher;
		_synchronousJobDispatcher = synchronousJobDispatcher;
		_asynchronousJobDispatcher = asynchronousJobDispatcher;
	}

	static WorkerJobSyncContextWrapper CreateInstance(WorkerJobSyncHelper<TSelf, TContext, TConfig> @this) {
		return new WorkerJobSyncContextWrapper(new(), @this);
	}

	public WorkerJobSyncContextWrapper CreateContextWrapper() {
		var result = _contextWrapperPool.Rent();
		result.Context.HeapPool = _heapPool;
		return result;
	}

	public void Dispose() => _contextWrapperPool.Dispose(invokeDisposeOnEachItemBeforeRelease: true);
}