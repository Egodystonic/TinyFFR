using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Egodystonic.TinyFFR.Resources.Memory;

interface IHeapPool {
	PooledHeapMemory<T> BorrowAndCopy<T>(ReadOnlySpan<T> copySource) where T : unmanaged;
	PooledHeapMemory<T> Borrow<T>(int numElements) where T : unmanaged;
	PooledHeapMemory<T> Borrow<T>(int numElements, int elementSize) where T : unmanaged;
	PooledHeapMemory<byte> Borrow(int numBytes);
}

sealed unsafe class HeapPool : IHeapPool, IDisposable {
	readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
	readonly ArrayPoolBackedMap<nuint, byte[]> _activeSpanLeases = new();
	nuint _prevSpanLeaseId = 0U;
	
	public ThreadSafeHeapPoolWrapper ThreadSafeWrapper { get; }

	public HeapPool() => ThreadSafeWrapper = new(this);

	public PooledHeapMemory<T> BorrowAndCopy<T>(ReadOnlySpan<T> copySource) where T : unmanaged {
		var result = Borrow<T>(copySource.Length);
		copySource.CopyTo(result.Span);
		return result;
	}
	public PooledHeapMemory<T> Borrow<T>(int numElements) where T : unmanaged => Borrow<T>(numElements, sizeof(T));
	public PooledHeapMemory<T> Borrow<T>(int numElements, int elementSize) where T : unmanaged => new(_pool, elementSize * numElements);
	public PooledHeapMemory<byte> Borrow(int numBytes) => new(_pool, numBytes);
	
	public ScopedSpanLease<T> CreateSpanLease<T>(int numElements) where T : unmanaged {
		var byteLength = sizeof(T) * numElements;
		var buffer = _pool.Rent(byteLength);
		_activeSpanLeases[++_prevSpanLeaseId] = buffer;
		return new(&DisposeSpanLease, this, _prevSpanLeaseId, MemoryMarshal.Cast<byte, T>(buffer.AsSpan(0, byteLength)));
	}
	
	public ScopedReadOnlySpanLease<T> CreateReadOnlySpanLease<T>(int numElements) where T : unmanaged {
		var byteLength = sizeof(T) * numElements;
		var buffer = _pool.Rent(byteLength);
		_activeSpanLeases[++_prevSpanLeaseId] = buffer;
		return new(&DisposeSpanLease, this, _prevSpanLeaseId, MemoryMarshal.Cast<byte, T>(buffer.AsSpan(0, byteLength)));
	}

	public void Dispose() => _activeSpanLeases.Dispose();

	static void DisposeSpanLease(object? o, nuint id) {
		var pool = (HeapPool) o!;
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

	public Span<T> Span => MemoryMarshal.Cast<byte, T>(_buffer.AsSpan(0, _requestedSizeBytes));
	
	public GCHandle FixData() => GCHandle.Alloc(_buffer, GCHandleType.Pinned);

	public void Dispose() => _pool.Return(_buffer);
}

sealed class ThreadSafeHeapPoolWrapper : IHeapPool {
	readonly HeapPool _owner;

	public ThreadSafeHeapPoolWrapper(HeapPool owner) {
		ArgumentNullException.ThrowIfNull(owner);
		_owner = owner;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PooledHeapMemory<T> BorrowAndCopy<T>(ReadOnlySpan<T> copySource) where T : unmanaged => _owner.BorrowAndCopy(copySource);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PooledHeapMemory<T> Borrow<T>(int numElements) where T : unmanaged => _owner.Borrow<T>(numElements);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PooledHeapMemory<T> Borrow<T>(int numElements, int elementSize) where T : unmanaged => _owner.Borrow<T>(numElements, elementSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PooledHeapMemory<byte> Borrow(int numBytes) => _owner.Borrow(numBytes);
}
