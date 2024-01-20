// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Resources.Ram;

public readonly record struct RamResourceHandle : IDisposable {
	public ReadOnlySpan<byte> Data => ByteBufferHandle.GetBufferSpan();

	internal ByteBufferPool.PooledByteBufferHandle ByteBufferHandle { get; }

	internal RamResourceHandle(ByteBufferPool.PooledByteBufferHandle byteBufferHandle) {
		ByteBufferHandle = byteBufferHandle;
	}

	public void Dispose() => ByteBufferHandle.Dispose();
}