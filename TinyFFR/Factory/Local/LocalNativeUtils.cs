// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Text;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Factory.Local;

readonly unsafe record struct TemporaryLoadSpaceBuffer(nuint BufferIdentity, UIntPtr DataPtr, int DataLengthBytes) {
	public Span<T> AsSpan<T>() where T : unmanaged => MemoryMarshal.Cast<byte, T>(new Span<byte>((void*) DataPtr, DataLengthBytes));
}

static unsafe class LocalNativeUtils {
	readonly struct ActiveBufferData {
		public readonly ILocalGpuHoldingBufferAllocator Allocator;
		public readonly FixedByteSlabPool.SlabRental Rental;
		public readonly delegate* managed<nuint, Span<byte>, void> OptionalReadbackFunc;

		public ActiveBufferData(ILocalGpuHoldingBufferAllocator allocator, FixedByteSlabPool.SlabRental rental, delegate*<nuint, Span<byte>, void> optionalReadbackFunc) {
			Allocator = allocator;
			Rental = rental;
			OptionalReadbackFunc = optionalReadbackFunc;
		}
	}
	static readonly ArrayPoolBackedMap<nuint, ActiveBufferData> _activeTemporaryBuffers = new();

	public const string NativeLibName = "TinyFFR.Native";
	const long UnrenderedUploadBytesBeforeFlush = 64L * 1024 * 1024;
	const long MinUnrenderedUploadBytesBeforeBlockingFlush = 256L * 1024 * 1024;
	const int RenderedFramesPerRetentionWindow = 120;
	static long _uploadBytesSinceLastRenderedFrame = 0L;
	static int _renderedFramesInRetentionWindow = 0;
	static bool _nativeLibInitialized = false;
	static bool _sdlInitialized = false;
	static nuint _nextTemporaryBufferId = 0;
	static int _bufferDeallocationDepth = 0;

	[DllImport(NativeLibName, EntryPoint = "get_err_buffer")]
	static extern byte* GetErrorBuffer();

	[DllImport(NativeLibName, EntryPoint = "get_err_buffer_length")]
	static extern int GetErrorBufferLength();

	[DllImport(NativeLibName, EntryPoint = "inject_fake_error")]
	internal static extern InteropResult InjectFakeError(byte* utf8Message);

	public static string GetLastError() {
		var bufferLength = GetErrorBufferLength();
		var asSpan = new ReadOnlySpan<byte>(GetErrorBuffer(), bufferLength);
		var firstZero = asSpan.IndexOf((byte) 0);
		return Encoding.UTF8.GetString(asSpan[..(firstZero >= 0 ? firstZero : bufferLength)]);
	}

	[DllImport(NativeLibName, EntryPoint = "exec_once_only_init_sdl")]
	static extern InteropResult ExecOnceOnlyInitSdl();

	public static void InitializeNativeLibIfNecessary(bool headlessMode) {
		if (!_nativeLibInitialized) {
			if (UIntPtr.Size != 8) throw new InvalidOperationException("TinyFFR local factories are only supported on 64-bit platforms.");

			// This curious invocation tells the NVIDIA drivers (if they exist) to force this application to run
			// on the dedicated GPU in systems where that isn't always the default (e.g. gaming laptops).
			// We don't actually need the loaded library handle, and if the load attempt fails 
			// we can just ignore the failure as it means this system probably isn't an NVIDIA one.
			_ = NativeLibrary.TryLoad("nvapi64.dll", out _);

			SetLogNotifyDelegate(&HandleLogMessage);
			SetBufferDeallocationDelegate(&DeallocateRentedBufferFromNativeCaller).ThrowIfFailure();
			_nativeLibInitialized = true;
		}

		if (headlessMode || _sdlInitialized) return;
		ExecOnceOnlyInitSdl().ThrowIfFailure();
		_sdlInitialized = true;
	}

	[DllImport(NativeLibName, EntryPoint = "set_buffer_deallocation_delegate")]
	static extern InteropResult SetBufferDeallocationDelegate(
		delegate* unmanaged<nuint, void> bufferDeallocDelegate
	);

	[DllImport(NativeLibName, EntryPoint = "set_log_notify_delegate")]
	static extern InteropResult SetLogNotifyDelegate(
		delegate* unmanaged<byte*, void> logNotifyDelegate
	);

	[DllImport(NativeLibName, EntryPoint = "flush_pending_uploads")]
	static extern InteropResult FlushPendingUploads(InteropBool waitForCompletion);

	[UnmanagedCallersOnly]
	static void DeallocateRentedBufferFromNativeCaller(nuint bufferId) => DeallocateRentedBuffer(bufferId);

	public static void DeallocateRentedBuffer(nuint bufferId) {
		if (!_activeTemporaryBuffers.Remove(bufferId, out var tuple)) {
			throw new InvalidOperationException($"Buffer '{bufferId}' has already been deallocated.");
		}
		++_bufferDeallocationDepth;
		try {
			if (tuple.OptionalReadbackFunc != null) tuple.OptionalReadbackFunc(bufferId, tuple.Rental.AsByteSpan);
		}
		finally {
			--_bufferDeallocationDepth;
		}
		var allocator = tuple.Allocator;
		allocator.GpuHoldingBufferPool.Return(tuple.Rental);
		if (allocator.IsDisposed) DisposeTemporaryCpuBufferPoolIfSafe(allocator);
	}

	static void ReleaseCompletedUploadsIfOverBudget(FixedByteSlabPool pool, int sizeBytes) {
		_uploadBytesSinceLastRenderedFrame += sizeBytes;
		if (Int64.Min(_uploadBytesSinceLastRenderedFrame, pool.CurrentlyLeasedTotalBytes + sizeBytes) <= UnrenderedUploadBytesBeforeFlush) return;
		FlushPendingUploads(false).ThrowIfFailure();
		var blockingFlushThreshold = Int64.Max(MinUnrenderedUploadBytesBeforeBlockingFlush, 2L * sizeBytes);
		if (LocalFrameSynchronizationManager.IsInOpenFrameScope || pool.CurrentlyLeasedTotalBytes + sizeBytes <= blockingFlushThreshold) return;
		FlushPendingUploads(true).ThrowIfFailure();
	}

	internal static void NotifyFrameRendered(ILocalGpuHoldingBufferAllocator allocator) {
		_uploadBytesSinceLastRenderedFrame = 0L;
		if (++_renderedFramesInRetentionWindow < RenderedFramesPerRetentionWindow) return;
		_renderedFramesInRetentionWindow = 0;
		allocator.GpuHoldingBufferPool.EndRetentionWindow();
	}

	[UnmanagedCallersOnly]
	static void HandleLogMessage(byte* message) {
		Console.WriteLine(message == null ? String.Empty : Marshal.PtrToStringUTF8((IntPtr) message) ?? String.Empty);
	}

	internal static void DisposeTemporaryCpuBufferPoolIfSafe(ILocalGpuHoldingBufferAllocator allocator) {
		foreach (var kvp in _activeTemporaryBuffers) {
			if (ReferenceEquals(kvp.Value.Allocator, allocator)) return;
		}

		allocator.GpuHoldingBufferPool.Dispose();
	}

	internal static TemporaryLoadSpaceBuffer CreateGpuHoldingBufferAndCopyData<T>(ILocalGpuHoldingBufferAllocator allocator, ReadOnlySpan<T> data) where T : unmanaged {
		var buffer = CreateGpuHoldingBuffer<T>(allocator, data.Length);
		data.CopyTo(buffer.AsSpan<T>());
		return buffer;
	}

	internal static TemporaryLoadSpaceBuffer CreateGpuHoldingBuffer<T>(ILocalGpuHoldingBufferAllocator allocator, int numElements) where T : unmanaged {
		var sizeBytes = sizeof(T) * numElements;
		return CreateGpuHoldingBuffer(allocator, sizeBytes);
	}

	internal static TemporaryLoadSpaceBuffer CreateGpuHoldingBuffer(ILocalGpuHoldingBufferAllocator allocator, int sizeBytes) {
		return CreateGpuHoldingBuffer(allocator, sizeBytes, null);
	}

	internal static TemporaryLoadSpaceBuffer CreateGpuHoldingBuffer(ILocalGpuHoldingBufferAllocator allocator, int sizeBytes, delegate* managed<nuint, Span<byte>, void> optionalReadbackFunc) {
		ObjectDisposedException.ThrowIf(allocator.IsDisposed, allocator);
		if (optionalReadbackFunc == null && _bufferDeallocationDepth == 0) ReleaseCompletedUploadsIfOverBudget(allocator.GpuHoldingBufferPool, sizeBytes);
		var bufferId = _nextTemporaryBufferId++;
		var rental = allocator.GpuHoldingBufferPool.Rent(sizeBytes);
		_activeTemporaryBuffers.Add(bufferId, new(allocator, rental, optionalReadbackFunc));
		return new(bufferId, rental.SlotPtr, sizeBytes);
	}
}