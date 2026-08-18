// Created on 2026-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.Threading;

namespace Egodystonic.TinyFFR.Threading;

readonly unsafe struct ThreadJob {
	const int PointerSize = 8;
	const int MaxSerializedJobArgumentSizeBytes = 32; // Can be increased if necessary
	const int WorkPtrOffset = PointerSize * 0;
	const int CompletionPtrOffset = PointerSize * 1;
	const int ContextOffset = PointerSize * 2;
	const int ResultOffset = PointerSize * 0;
	static ulong _prevJobId;

#if DEBUG
#pragma warning disable CA1065 // "Do not raise exceptions in unexpected locations" -- Failing at type-load is the point: every offset below would be silently wrong
	static ThreadJob() {
		if (sizeof(nint) != PointerSize) throw new PlatformNotSupportedException($"Platform pointer size mismatch.");
	}
#pragma warning restore CA1065
#endif

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
		try {
			if (Work == null || Completion == null) throw new InvalidObjectException($"Given {nameof(ThreadJob)} had null work or completion pointer.");

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

	public void Cancel(JobCompletionRegistrar? completionRegistrar) {
		try {
			if (Completion == null) return;
			Completion(Context, new OperationCanceledException($"{nameof(ThreadJob)} was abandoned before execution because the thread pool was disposed."), default);
		}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- We're already on the teardown path and must keep draining
		catch (Exception e) {
#pragma warning restore CA1031
			Console.WriteLine($"Could not cleanly cancel job {JobId}: {e.GetAllMessages()}.");
		}
		finally {
			completionRegistrar?.NotifyCompletion(JobId);
		}
	}

	public static ThreadJob CreateWithManagedContextManagedResult<TContext, TResult>(TContext context, delegate* managed<TContext, TResult> work, delegate* managed<TContext, Exception?, TResult, void> completion) where TContext : class where TResult : class {
		static SerializedJobData Work(SerializedJobData serializedContext) {
			var workPtr = (delegate* managed<TContext, TResult>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[WorkPtrOffset..]);
			var unwrappedContext = (TContext) GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedContext[ContextOffset..])).Target!;
			var result = workPtr(unwrappedContext);
			var resultHandle = GCHandle.Alloc(result);
			var serializedResult = new SerializedJobData();
			BinaryPrimitives.WriteIntPtrLittleEndian(serializedResult[ResultOffset..], GCHandle.ToIntPtr(resultHandle));
			return serializedResult;
		}

		static void Completion(SerializedJobData serializedContext, Exception? error, SerializedJobData serializedResult) {
			var completionPtr = (delegate* managed<TContext, Exception?, TResult, void>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..]);
			var contextHandle = GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedContext[ContextOffset..]));
			var unwrappedContext = (TContext) contextHandle.Target!;
			contextHandle.Free();

			var unwrappedResult = default(TResult)!;
			if (error == null) {
				var resultHandle = GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedResult[ResultOffset..]));
				unwrappedResult = (TResult) resultHandle.Target!;
				resultHandle.Free();
			}

			completionPtr(unwrappedContext, error, unwrappedResult);
		}

		var serializedContext = new SerializedJobData();
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[WorkPtrOffset..], (UIntPtr) work);
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..], (UIntPtr) completion);
		var contextHandle = GCHandle.Alloc(context);
		BinaryPrimitives.WriteIntPtrLittleEndian(serializedContext[ContextOffset..], GCHandle.ToIntPtr(contextHandle));

		return new ThreadJob(serializedContext, &Work, &Completion);
	}

	public static ThreadJob CreateWithUnmanagedContextUnmanagedResult<TContext, TResult>(TContext context, delegate* managed<TContext, TResult> work, delegate* managed<TContext, Exception?, TResult, void> completion) where TContext : unmanaged where TResult : unmanaged {
		static SerializedJobData Work(SerializedJobData serializedContext) {
			var workPtr = (delegate* managed<TContext, TResult>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[WorkPtrOffset..]);
			var unwrappedContext = MemoryMarshal.Read<TContext>(serializedContext[ContextOffset..]);
			var result = workPtr(unwrappedContext);
			var serializedResult = new SerializedJobData();
			MemoryMarshal.Write(serializedResult[ResultOffset..], result);
			return serializedResult;
		}

		static void Completion(SerializedJobData serializedContext, Exception? error, SerializedJobData serializedResult) {
			var completionPtr = (delegate* managed<TContext, Exception?, TResult, void>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..]);
			var unwrappedContext = MemoryMarshal.Read<TContext>(serializedContext[ContextOffset..]);
			var unwrappedResult = error == null ? MemoryMarshal.Read<TResult>(serializedResult[ResultOffset..]) : default;

			completionPtr(unwrappedContext, error, unwrappedResult);
		}

		if (sizeof(TContext) > MaxSerializedJobArgumentSizeBytes - ContextOffset) {
			throw new InvalidOperationException("Context type too large.");
		}
		if (sizeof(TResult) > MaxSerializedJobArgumentSizeBytes) {
			throw new InvalidOperationException("Result type too large.");
		}

		var serializedContext = new SerializedJobData();
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[WorkPtrOffset..], (UIntPtr) work);
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..], (UIntPtr) completion);
		MemoryMarshal.Write(serializedContext[ContextOffset..], in context);

		return new ThreadJob(serializedContext, &Work, &Completion);
	}

	public static ThreadJob CreateWithManagedContextUnmanagedResult<TContext, TResult>(TContext context, delegate* managed<TContext, TResult> work, delegate* managed<TContext, Exception?, TResult, void> completion) where TContext : class where TResult : unmanaged {
		static SerializedJobData Work(SerializedJobData serializedContext) {
			var workPtr = (delegate* managed<TContext, TResult>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[WorkPtrOffset..]);
			var unwrappedContext = (TContext) GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedContext[ContextOffset..])).Target!;
			var result = workPtr(unwrappedContext);
			var serializedResult = new SerializedJobData();
			MemoryMarshal.Write(serializedResult[ResultOffset..], result);
			return serializedResult;
		}

		static void Completion(SerializedJobData serializedContext, Exception? error, SerializedJobData serializedResult) {
			var completionPtr = (delegate* managed<TContext, Exception?, TResult, void>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..]);
			var contextHandle = GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedContext[ContextOffset..]));
			var unwrappedContext = (TContext) contextHandle.Target!;
			contextHandle.Free();
			var unwrappedResult = error == null ? MemoryMarshal.Read<TResult>(serializedResult[ResultOffset..]) : default;

			completionPtr(unwrappedContext, error, unwrappedResult);
		}

		if (sizeof(TResult) > MaxSerializedJobArgumentSizeBytes) {
			throw new InvalidOperationException("Result type too large.");
		}

		var serializedContext = new SerializedJobData();
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[WorkPtrOffset..], (UIntPtr) work);
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..], (UIntPtr) completion);
		var contextHandle = GCHandle.Alloc(context);
		BinaryPrimitives.WriteIntPtrLittleEndian(serializedContext[ContextOffset..], GCHandle.ToIntPtr(contextHandle));

		return new ThreadJob(serializedContext, &Work, &Completion);
	}

	public static ThreadJob CreateWithUnmanagedContextManagedResult<TContext, TResult>(TContext context, delegate* managed<TContext, TResult> work, delegate* managed<TContext, Exception?, TResult, void> completion) where TContext : unmanaged where TResult : class {
		static SerializedJobData Work(SerializedJobData serializedContext) {
			var workPtr = (delegate* managed<TContext, TResult>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[WorkPtrOffset..]);
			var unwrappedContext = MemoryMarshal.Read<TContext>(serializedContext[ContextOffset..]);
			var result = workPtr(unwrappedContext);
			var resultHandle = GCHandle.Alloc(result);
			var serializedResult = new SerializedJobData();
			BinaryPrimitives.WriteIntPtrLittleEndian(serializedResult[ResultOffset..], GCHandle.ToIntPtr(resultHandle));
			return serializedResult;
		}

		static void Completion(SerializedJobData serializedContext, Exception? error, SerializedJobData serializedResult) {
			var completionPtr = (delegate* managed<TContext, Exception?, TResult, void>) BinaryPrimitives.ReadUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..]);
			var unwrappedContext = MemoryMarshal.Read<TContext>(serializedContext[ContextOffset..]);

			var unwrappedResult = default(TResult)!;
			if (error == null) {
				var resultHandle = GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(serializedResult[ResultOffset..]));
				unwrappedResult = (TResult) resultHandle.Target!;
				resultHandle.Free();
			}

			completionPtr(unwrappedContext, error, unwrappedResult);
		}

		if (sizeof(TContext) > MaxSerializedJobArgumentSizeBytes - ContextOffset) {
			throw new InvalidOperationException("Context type too large.");
		}

		var serializedContext = new SerializedJobData();
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[WorkPtrOffset..], (UIntPtr) work);
		BinaryPrimitives.WriteUIntPtrLittleEndian(serializedContext[CompletionPtrOffset..], (UIntPtr) completion);
		MemoryMarshal.Write(serializedContext[ContextOffset..], in context);

		return new ThreadJob(serializedContext, &Work, &Completion);
	}
}
