// Created on 2026-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Threading;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Threading;

sealed unsafe class WorkerResourceCreationSyncHelper<TContext, TResult> : IDisposable where TContext : class, WorkerResourceCreationSyncHelper<TContext, TResult>.IPooledSyncObject, new() where TResult : IResource<TResult> {
	public interface IPooledSyncObject {
		TResult NewResource { get; }
		Exception? Error { get; set; }
		void CreateResource();
		void Recycle();
	}
	readonly ThreadLocal<ArrayPoolBackedObjectPool<TContext>> _threadLocalObjectPool = new(CreatePool, trackAllValues: true);
		
	static ArrayPoolBackedObjectPool<TContext> CreatePool() => new(&CreateInstance);
	static TContext CreateInstance() => new();
	
	public TContext GetThreadLocalSyncObject() {
		return _threadLocalObjectPool.Value!.Rent();
	}
	
	public TResult GenerateResultOnPrimaryThread(TContext syncObject, IJobExecutionFacade facade) {
		static Unused Work(TContext context) {
			context.CreateResource();
			return default;
		}
		static void Completion(TContext context, Exception? error, Unused _) {
			context.Error = error;
		}
		
		facade.AddPrimaryThreadJobAndWait(ThreadJob.CreateWithManagedContextUnmanagedResult(syncObject, &Work, &Completion));

		var result = syncObject.NewResource;
		var error = syncObject.Error;
		syncObject.Recycle();
		_threadLocalObjectPool.Value!.Return(syncObject);

		if (error != null) throw new AggregateException($"Could not create resource on primary thread ({error.GetAllMessages()}).", error);

		return result;
	}

	public void Dispose() {
		foreach (var value in _threadLocalObjectPool.Values) {
			value.Dispose(invokeDisposeOnEachItemBeforeRelease: false);
		}
		_threadLocalObjectPool.Dispose();
	}
}