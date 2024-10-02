// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Text;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Factory.Local;

public readonly unsafe record struct TemporaryLoadSpaceBuffer(nuint BufferIdentity, UIntPtr DataPtr, int DataLengthBytes) {
	public Span<T> AsSpan<T>() where T : unmanaged => MemoryMarshal.Cast<byte, T>(new Span<byte>((void*) DataPtr, DataLengthBytes));
}

static unsafe class LocalNativeUtils {
	static readonly ArrayPoolBackedMap<nuint, (LocalRendererFactory Factory, FixedByteBufferPool.FixedByteBuffer Buffer)> _activeTemporaryBuffers = new();

	public const string NativeLibName = "TinyFFR.Native";
	const int NativeErrorBufferLength = 1001;
	static bool _nativeLibInitialized = false;
	static nuint _nextTemporaryBufferId = 0;

	[DllImport(NativeLibName, EntryPoint = "get_err_buffer")]
	static extern byte* GetErrorBuffer();

	public static string GetLastError() {
		var asSpan = new ReadOnlySpan<byte>(GetErrorBuffer(), NativeErrorBufferLength);
		var firstZero = asSpan.IndexOf((byte) 0);
		return Encoding.UTF8.GetString(asSpan[..(firstZero >= 0 ? firstZero : NativeErrorBufferLength)]);
	}

	[DllImport(NativeLibName, EntryPoint = "initialize_all")]
	static extern InteropResult InitializeAll();

	public static void InitializeNativeLibIfNecessary() {
		if (_nativeLibInitialized) return;
		InitializeAll().ThrowIfFailure();
		SetBufferDeallocationDelegate(&DeallocateRentedBuffer).ThrowIfFailure();
		_nativeLibInitialized = true;
	}

	[DllImport(NativeLibName, EntryPoint = "set_buffer_deallocation_delegate")]
	static extern InteropResult SetBufferDeallocationDelegate(
		delegate* unmanaged<nuint, void> bufferDeallocDelegate
	);

	[UnmanagedCallersOnly]
	static void DeallocateRentedBuffer(nuint bufferId) {
		if (!_activeTemporaryBuffers.ContainsKey(bufferId)) throw new InvalidOperationException($"Buffer '{bufferId}' has already been deallocated.");
		var tuple = _activeTemporaryBuffers[bufferId];
		_activeTemporaryBuffers.Remove(bufferId);
		var factory = tuple.Factory;
		factory.TemporaryCpuBufferPool.Return(tuple.Buffer);

		if (factory.IsDisposed) DisposeTemporaryCpuBufferPoolIfSafe(factory);
	}

	internal static void DisposeTemporaryCpuBufferPoolIfSafe(LocalRendererFactory factory) {
		foreach (var kvp in _activeTemporaryBuffers) {
			if (ReferenceEquals(kvp.Value.Factory, factory)) return;
		}

		factory.TemporaryCpuBufferPool.Dispose();
	}

	internal static TemporaryLoadSpaceBuffer CopySpanToTemporaryCpuBuffer<T>(LocalRendererFactory factory, ReadOnlySpan<T> data) where T : unmanaged {
		factory.ThrowIfThisIsDisposed();
		var sizeBytes = sizeof(T) * data.Length;
		if (sizeBytes > factory.TemporaryCpuBufferPool.MaxBufferSizeBytes) {
			throw new InvalidOperationException($"Can not load asset because its in-memory size is {sizeBytes} bytes (" +
												$"the maximum asset size configured is {factory.TemporaryCpuBufferPool.MaxBufferSizeBytes} bytes; " +
												$"the limit can be raised by setting the {nameof(LocalRendererFactoryConfig.MaxAssetSizeBytes)} value in " +
												$"the {nameof(LocalRendererFactoryConfig)} passed to the {nameof(LocalRendererFactory)} constructor).");
		}
		var bufferId = _nextTemporaryBufferId++;
		var buffer = factory.TemporaryCpuBufferPool.Rent<T>(data.Length);
		_activeTemporaryBuffers.Add(bufferId, (factory, buffer));
		data.CopyTo(buffer.AsSpan<T>(data.Length));
		return new(bufferId, buffer.StartPtr, sizeBytes);
	}
}