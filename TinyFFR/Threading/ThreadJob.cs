using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Channels;

namespace Egodystonic.TinyFFR.Threading;

readonly unsafe struct ThreadJob {
	const int PointerSize = 8;
	const int MaxSerializedJobArgumentSizeBytes = 32; // Can be increased if necessary
	static ulong _prevJobId;
	[InlineArray(MaxSerializedJobArgumentSizeBytes)] internal struct SerializedJobData { byte _; }
	public readonly ulong JobId;
	public readonly SerializedJobData Context;
	public readonly delegate* managed<SerializedJobData, SerializedJobData> Work;
	public readonly delegate* managed<SerializedJobData, Exception?, SerializedJobData, void> Completion;

	public ThreadJob(SerializedJobData context, delegate*<SerializedJobData, SerializedJobData> work, delegate*<SerializedJobData, Exception?, SerializedJobData, void> completion) {
		JobId = Interlocked.Increment(ref _prevJobId);
		Context = context;
		Work = work;
		Completion = completion;
	}
	
	public void Execute(JobCompletionRegistrar? completionRegistrar) {
		if (Work == null || Completion == null) throw new ArgumentException("Given job had null work or completion pointer.");

		try {
			SerializedJobData result;
			try {
				result = Work(Context);
			}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- We're passing it on to the continuation
			catch (Exception e) {
#pragma warning restore CA1031
				Completion(Context, e, default);
				return;
			}
		
			Completion(Context, null, result);
		}
		finally {
			completionRegistrar?.NotifyCompletion(JobId);
		}
	}

	public static ThreadJob CreateWithManagedContextManagedResult<TContext, TResult>(TContext context, delegate* managed<TContext, TResult> work, delegate* managed<TContext, Exception?, TResult, void> completion) where TContext : class where TResult : class {
		const int WorkPtrOffset = PointerSize * 0;
		const int CompletionPtrOffset = PointerSize * 1;
		const int ContextHandleOffset = PointerSize * 2;
		const int ResultHandleOffset = PointerSize * 0;
		
		static SerializedJobData Work(SerializedJobData serializedContext) {
			var workPtr = (delegate* managed<TContext, TResult>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[WorkPtrOffset..]);
			var unwrappedContext = (TContext) GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedContext[ContextHandleOffset..])).Target!;
			var result = workPtr(unwrappedContext);
			var resultHandle = GCHandle.Alloc(result);
			var serializedResult = new SerializedJobData();
			BinaryPrimitives.WriteIntPtrLittleEndian(serializedResult[ResultHandleOffset..], GCHandle.ToIntPtr(resultHandle));
			return serializedResult;
		}
		
		static void Completion(SerializedJobData serializedContext, Exception? error, SerializedJobData serializedResult) {
			var completionPtr = (delegate* managed<TContext, Exception?, TResult, void>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..]);
			var contextHandle = GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedContext[ContextHandleOffset..]));
			var unwrappedContext = (TContext) contextHandle.Target!;
			contextHandle.Free();
			var resultHandle = GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedResult[ResultHandleOffset..]));
			var unwrappedResult = (TResult) resultHandle.Target!;
			resultHandle.Free();
			
			completionPtr(unwrappedContext, error, unwrappedResult);
		}
		
		var serializedContext = new SerializedJobData();
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[WorkPtrOffset..], (UIntPtr) work);
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..], (UIntPtr) completion);
		var contextHandle = GCHandle.Alloc(context);
		BinaryPrimitives.WriteIntPtrLittleEndian(serializedContext[ContextHandleOffset..], GCHandle.ToIntPtr(contextHandle));
		
		return new ThreadJob(serializedContext, &Work, &Completion);
	}
	
	public static ThreadJob CreateWithUnmanagedContextUnmanagedResult<TContext, TResult>(TContext context, delegate* managed<TContext, TResult> work, delegate* managed<TContext, Exception?, TResult, void> completion) where TContext : unmanaged where TResult : unmanaged {
		const int WorkPtrOffset = PointerSize * 0;
		const int CompletionPtrOffset = PointerSize * 1;
		const int ContextDataOffset = PointerSize * 2;
		const int ResultDataOffset = PointerSize * 0;
		
		static SerializedJobData Work(SerializedJobData serializedContext) {
			var workPtr = (delegate* managed<TContext, TResult>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[WorkPtrOffset..]);
			var unwrappedContext = MemoryMarshal.Read<TContext>(serializedContext[ContextDataOffset..]);
			var result = workPtr(unwrappedContext);
			var serializedResult = new SerializedJobData();
			MemoryMarshal.Write(serializedResult[ResultDataOffset..], result);
			return serializedResult;
		}
		
		static void Completion(SerializedJobData serializedContext, Exception? error, SerializedJobData serializedResult) {
			var completionPtr = (delegate* managed<TContext, Exception?, TResult, void>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..]);
			var unwrappedContext = MemoryMarshal.Read<TContext>(serializedContext[ContextDataOffset..]);
			var unwrappedResult = MemoryMarshal.Read<TResult>(serializedResult[ResultDataOffset..]);
			
			completionPtr(unwrappedContext, error, unwrappedResult);
		}
		
		if (sizeof(TContext) > MaxSerializedJobArgumentSizeBytes - ContextDataOffset) {
			throw new InvalidOperationException("Context type too large.");
		}
		if (sizeof(TResult) > MaxSerializedJobArgumentSizeBytes) {
			throw new InvalidOperationException("Result type too large.");
		}
		
		var serializedContext = new SerializedJobData();
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[WorkPtrOffset..], (UIntPtr) work);
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..], (UIntPtr) completion);
		MemoryMarshal.Write(serializedContext[ContextDataOffset..], in context);
		
		return new ThreadJob(serializedContext, &Work, &Completion);
	}
	
	public static ThreadJob CreateWithManagedContextUnmanagedResult<TContext, TResult>(TContext context, delegate* managed<TContext, TResult> work, delegate* managed<TContext, Exception?, TResult, void> completion) where TContext : class where TResult : unmanaged {
		const int WorkPtrOffset = PointerSize * 0;
		const int CompletionPtrOffset = PointerSize * 1;
		const int ContextHandleOffset = PointerSize * 2;
		const int ResultDataOffset = PointerSize * 0;
		
		static SerializedJobData Work(SerializedJobData serializedContext) {
			var workPtr = (delegate* managed<TContext, TResult>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[WorkPtrOffset..]);
			var unwrappedContext = (TContext) GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedContext[ContextHandleOffset..])).Target!;
			var result = workPtr(unwrappedContext);
			var serializedResult = new SerializedJobData();
			MemoryMarshal.Write(serializedResult[ResultDataOffset..], result);
			return serializedResult;
		}
		
		static void Completion(SerializedJobData serializedContext, Exception? error, SerializedJobData serializedResult) {
			var completionPtr = (delegate* managed<TContext, Exception?, TResult, void>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..]);
			var contextHandle = GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedContext[ContextHandleOffset..]));
			var unwrappedContext = (TContext) contextHandle.Target!;
			contextHandle.Free();
			var unwrappedResult = MemoryMarshal.Read<TResult>(serializedResult[ResultDataOffset..]);
			
			completionPtr(unwrappedContext, error, unwrappedResult);
		}
		
		if (sizeof(TResult) > MaxSerializedJobArgumentSizeBytes) {
			throw new InvalidOperationException("Result type too large.");
		}
		
		var serializedContext = new SerializedJobData();
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[WorkPtrOffset..], (UIntPtr) work);
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..], (UIntPtr) completion);
		var contextHandle = GCHandle.Alloc(context);
		BinaryPrimitives.WriteIntPtrLittleEndian(serializedContext[ContextHandleOffset..], GCHandle.ToIntPtr(contextHandle));
		
		return new ThreadJob(serializedContext, &Work, &Completion);
	}
	
	public static ThreadJob CreateWithUnmanagedContextManagedResult<TContext, TResult>(TContext context, delegate* managed<TContext, TResult> work, delegate* managed<TContext, Exception?, TResult, void> completion) where TContext : unmanaged where TResult : class {
		const int WorkPtrOffset = PointerSize * 0;
		const int CompletionPtrOffset = PointerSize * 1;
		const int ContextDataOffset = PointerSize * 2;
		const int ResultHandleOffset = PointerSize * 0;
		
		static SerializedJobData Work(SerializedJobData serializedContext) {
			var workPtr = (delegate* managed<TContext, TResult>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[WorkPtrOffset..]);
			var unwrappedContext = MemoryMarshal.Read<TContext>(serializedContext[ContextDataOffset..]);
			var result = workPtr(unwrappedContext);
			var resultHandle = GCHandle.Alloc(result);
			var serializedResult = new SerializedJobData();
			BinaryPrimitives.WriteIntPtrLittleEndian(serializedResult[ResultHandleOffset..], GCHandle.ToIntPtr(resultHandle));
			return serializedResult;
		}
		
		static void Completion(SerializedJobData serializedContext, Exception? error, SerializedJobData serializedResult) {
			var completionPtr = (delegate* managed<TContext, Exception?, TResult, void>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..]);
			var unwrappedContext = MemoryMarshal.Read<TContext>(serializedContext[ContextDataOffset..]);
			var resultHandle = GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedResult[ResultHandleOffset..]));
			var unwrappedResult = (TResult) resultHandle.Target!;
			resultHandle.Free();
			
			completionPtr(unwrappedContext, error, unwrappedResult);
		}
		
		if (sizeof(TContext) > MaxSerializedJobArgumentSizeBytes - ContextDataOffset) {
			throw new InvalidOperationException("Context type too large.");
		}
		
		var serializedContext = new SerializedJobData();
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[WorkPtrOffset..], (UIntPtr) work);
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..], (UIntPtr) completion);
		MemoryMarshal.Write(serializedContext[ContextDataOffset..], in context);
		
		return new ThreadJob(serializedContext, &Work, &Completion);
	}
}