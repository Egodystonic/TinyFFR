using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed unsafe class HeapPool : IDisposable {
	readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
	readonly ArrayPoolBackedMap<nuint, byte[]> _activeSpanLeases = new();
	nuint _prevSpanLeaseId = 0U;

	public PooledHeapMemory<T> BorrowAndCopy<T>(ReadOnlySpan<T> copySource) where T : unmanaged {
		var result = Borrow<T>(copySource.Length);
		copySource.CopyTo(result.Buffer);
		return result;
	}
	public PooledHeapMemory<T> Borrow<T>(int numElements) where T : unmanaged => Borrow<T>(numElements, sizeof(T));
	public PooledHeapMemory<T> Borrow<T>(int numElements, int elementSize) where T : unmanaged => new(_pool, elementSize * numElements);
	public PooledHeapMemory<byte> Borrow(int numBytes) => new(_pool, numBytes);
	
	public ScopedSpanLease<T> CreateSpanLease<T>(int numElements) where T : unmanaged {
		var byteLength = sizeof(T) * numElements;
		var buffer = _pool.Rent(byteLength);
		_activeSpanLeases[++_prevSpanLeaseId] = buffer;
		return new(
			this,
			_prevSpanLeaseId,
			&DisposeSpanLease,
			MemoryMarshal.Cast<byte, T>(buffer.AsSpan(0, byteLength))
		);
	}
	
	public ScopedReadOnlySpanLease<T> CreateReadOnlySpanLease<T>(int numElements) where T : unmanaged {
		var byteLength = sizeof(T) * numElements;
		var buffer = _pool.Rent(byteLength);
		_activeSpanLeases[++_prevSpanLeaseId] = buffer;
		return new(
			this,
			_prevSpanLeaseId,
			&DisposeSpanLease,
			MemoryMarshal.Cast<byte, T>(buffer.AsSpan(0, byteLength))
		);
	}

	public void Dispose() => _activeSpanLeases.Dispose();

	static void DisposeSpanLease(object o, nuint id) {
		var pool = (HeapPool) o;
		if (pool._activeSpanLeases.Remove(id, out var buffer)) pool._pool.Return(buffer);
	}
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
