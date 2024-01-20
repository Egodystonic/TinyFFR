// Created on 2024-01-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed class ByteBufferPool {
	public readonly record struct PooledByteBufferHandle : IDisposable {
		readonly ArrayPool<byte> _owningArrayPool;
		internal byte[] Buffer { get; }
		internal int RequestedLength { get; }

		public PooledByteBufferHandle(ArrayPool<byte> owningArrayPool, byte[] buffer, int requestedLength) {
			_owningArrayPool = owningArrayPool;
			Buffer = buffer;
			RequestedLength = requestedLength;
		}

		public Span<byte> GetBufferSpan() => new(Buffer, 0, RequestedLength);
		public void Dispose() => _owningArrayPool.Return(Buffer);
	}

	readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Create();

	public PooledByteBufferHandle Reserve(int numBytes) {
		return new(_bytePool, _bytePool.Rent(numBytes), numBytes);
	}
}