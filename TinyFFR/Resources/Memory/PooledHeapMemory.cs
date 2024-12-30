using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed unsafe class HeapPool {
	readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

	public PooledHeapMemory<T> Borrow<T>(int numElements) where T : unmanaged => Borrow<T>(numElements, sizeof(T));
	public PooledHeapMemory<T> Borrow<T>(int numElements, int elementSize) where T : unmanaged => new(_pool, elementSize * numElements);
	public PooledHeapMemory<byte> Borrow(int numBytes) => new(_pool, numBytes);
}

readonly record struct PooledHeapMemory<T> : IDisposable where T : unmanaged {
	readonly byte[] _buffer;
	readonly int _requestedSizeBytes;
	readonly ArrayPool<byte> _pool;

	public PooledHeapMemory(ArrayPool<byte> pool, int requestedSizeBytes) {
		_pool = pool;
		_requestedSizeBytes = requestedSizeBytes;
		_buffer = pool.Rent(requestedSizeBytes);
	}

	public Span<T> Buffer => MemoryMarshal.Cast<byte, T>(_buffer.AsSpan(0, _requestedSizeBytes));

	public void Dispose() => _pool.Return(_buffer);
}
