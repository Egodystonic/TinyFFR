// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Text;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Factory.Local;

readonly unsafe record struct TemporaryLoadSpaceBuffer(nuint BufferIdentity, UIntPtr DataPtr, int DataLengthBytes) {
	public Span<T> AsSpan<T>() where T : unmanaged => MemoryMarshal.Cast<byte, T>(new Span<byte>((void*) DataPtr, DataLengthBytes));
}

static unsafe class LocalNativeUtils {
	readonly struct ActiveBufferData {
		public readonly ILocalGpuHoldingBufferAllocator Allocator;
		public readonly FixedByteBufferPool.FixedByteBuffer Buffer;
		public readonly delegate* managed<nuint, Span<byte>, void> OptionalReadbackFunc;

		public ActiveBufferData(ILocalGpuHoldingBufferAllocator allocator, FixedByteBufferPool.FixedByteBuffer buffer, delegate*<nuint, Span<byte>, void> optionalReadbackFunc) {
			Allocator = allocator;
			Buffer = buffer;
			OptionalReadbackFunc = optionalReadbackFunc;
		}
	}
	static readonly ArrayPoolBackedMap<nuint, ActiveBufferData> _activeTemporaryBuffers = new();

	public const string NativeLibName = "TinyFFR.Native";
	static bool _nativeLibInitialized = false;
	static nuint _nextTemporaryBufferId = 0;

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

	[DllImport(NativeLibName, EntryPoint = "exec_once_only_initialization")]
	static extern InteropResult ExecOnceOnlyInitialization();

	public static void InitializeNativeLibIfNecessary() {
		if (_nativeLibInitialized) return;

		if (UIntPtr.Size != 8) throw new InvalidOperationException("TinyFFR local factories are only supported on 64-bit platforms.");

		// This curious invocation tells the NVIDIA drivers (if they exist) to force this application to run
		// on the dedicated GPU in systems where that isn't always the default (e.g. gaming laptops).
		// We don't actually need the loaded library handle, and if the load attempt fails 
		// we can just ignore the failure as it means this system probably isn't an NVIDIA one.
		_ = NativeLibrary.TryLoad("nvapi64.dll", out _);

		SetLogNotifyDelegate(&HandleLogMessage);
		ExecOnceOnlyInitialization().ThrowIfFailure();
		SetBufferDeallocationDelegate(&DeallocateRentedBufferFromNativeCaller).ThrowIfFailure();
		TextureCompressor.AscertainCompressionSupport();
		_nativeLibInitialized = true;
	}

	[DllImport(NativeLibName, EntryPoint = "set_buffer_deallocation_delegate")]
	static extern InteropResult SetBufferDeallocationDelegate(
		delegate* unmanaged<nuint, void> bufferDeallocDelegate
	);

	[DllImport(NativeLibName, EntryPoint = "set_log_notify_delegate")]
	static extern InteropResult SetLogNotifyDelegate(
		delegate* unmanaged<byte*, void> logNotifyDelegate
	);

	[UnmanagedCallersOnly]
	static void DeallocateRentedBufferFromNativeCaller(nuint bufferId) => DeallocateRentedBuffer(bufferId);

	public static void DeallocateRentedBuffer(nuint bufferId) {
		if (!_activeTemporaryBuffers.Remove(bufferId, out var tuple)) {
			throw new InvalidOperationException($"Buffer '{bufferId}' has already been deallocated.");
		}
		if (tuple.OptionalReadbackFunc != null) tuple.OptionalReadbackFunc(bufferId, tuple.Buffer.AsByteSpan);
		var allocator = tuple.Allocator;
		allocator.GpuHoldingBufferPool.Return(tuple.Buffer);
		if (allocator.IsDisposed) DisposeTemporaryCpuBufferPoolIfSafe(allocator);
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
		if (sizeBytes > allocator.GpuHoldingBufferPool.MaxBufferSizeBytes) {
			throw new InvalidOperationException($"Can not load asset because its in-memory size is {sizeBytes} bytes (" +
												$"the maximum asset size configured is {allocator.GpuHoldingBufferPool.MaxBufferSizeBytes} bytes; " +
												$"the limit can be raised by setting the {nameof(LocalTinyFfrFactoryConfig.MaxCpuToGpuAssetTransferSizeBytes)} value in " +
												$"the {nameof(LocalTinyFfrFactoryConfig)} passed to the {nameof(LocalTinyFfrFactory)} constructor).");
		}
		var bufferId = _nextTemporaryBufferId++;
		var buffer = allocator.GpuHoldingBufferPool.Rent(sizeBytes);
		_activeTemporaryBuffers.Add(bufferId, new(allocator, buffer, optionalReadbackFunc));
		return new(bufferId, buffer.StartPtr, sizeBytes);
	}
}