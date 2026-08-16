// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Threading;

static class TinyFfrAsyncOperation {
	public const int SmuggleSizeBytes = 12;
}

public readonly unsafe record struct TinyFfrAsyncOperation<T> {
	sealed class AsyncOperationTrackingWrapper {
		ManualResetValueTaskSourceCore<T> _taskImpl = new();
		
		public short CurrentToken => _taskImpl.Version;
		
		public void PrepareForNextUsage() {
			_taskImpl.Reset();
		}
		
		public T BlockAndWaitForOutcome(short token) {
			return _taskImpl.GetResult(token);
		}
		
		public bool GetHasCompleted(short token) {
			return _taskImpl.GetStatus(token) switch {
				ValueTaskSourceStatus.Succeeded => true,
				ValueTaskSourceStatus.Faulted => true,
				ValueTaskSourceStatus.Canceled => true,
				_ => false
			};
		}
		
		public void SetResult(T result) {
			_taskImpl.SetResult(result);
		}
		
		public void SetException(Exception error) {
			_taskImpl.SetException(error);
		}
	}
	
	static readonly ObjectPool<AsyncOperationTrackingWrapper> _trackerPool = new(&CreateWrapper);
	static AsyncOperationTrackingWrapper CreateWrapper() {
		var result = new AsyncOperationTrackingWrapper();
		result.PrepareForNextUsage();
		return result;
	}
	
	readonly AsyncOperationTrackingWrapper _wrapper;
	readonly short _wrapperToken;
	
	public bool IsCompleted => _wrapper.GetHasCompleted(_wrapperToken);

	public TinyFfrAsyncOperation() {
		_wrapper = _trackerPool.Rent();
		_wrapperToken = _wrapper.CurrentToken;
	}
	
	TinyFfrAsyncOperation(AsyncOperationTrackingWrapper wrapper, short wrapperToken) {
		_wrapper = wrapper;
		_wrapperToken = wrapperToken;
	}
	
	public T ExtractResultAndDispose() {
		if (_wrapper == null) throw InvalidObjectException.InvalidDefault<TinyFfrAsyncOperation<T>>();
		try {
			return _wrapper.BlockAndWaitForOutcome(_wrapperToken);
		}
		finally {
			_wrapper.PrepareForNextUsage();
			_trackerPool.Return(_wrapper);
		}
	}
	
	internal void SetResult(T result) {
		_wrapper.SetResult(result);
	}
	
	internal void SetException(Exception error) {
		_wrapper.SetException(error);
	}
	
	internal static void Smuggle(in TinyFfrAsyncOperation<T> target, Span<byte> dest) {
		var wrapperHandle = GCHandle.Alloc(target._wrapper, GCHandleType.Normal);
		BinaryPrimitives.WriteIntPtrLittleEndian(dest, GCHandle.ToIntPtr(wrapperHandle));
		BinaryPrimitives.WriteInt16LittleEndian(dest[sizeof(IntPtr)..], target._wrapperToken);
	}
	
	internal static TinyFfrAsyncOperation<T> DeSmuggle(ReadOnlySpan<byte> src) {
		return new(
			((AsyncOperationTrackingWrapper) GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(src)).Target!),			
			BinaryPrimitives.ReadInt16LittleEndian(src[sizeof(IntPtr)..])
		);
	}
}