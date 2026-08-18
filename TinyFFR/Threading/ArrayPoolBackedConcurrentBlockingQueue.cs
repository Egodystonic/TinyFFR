// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers;
using System.Diagnostics;
using System.Threading;

namespace Egodystonic.TinyFFR.Threading;

sealed class ArrayPoolBackedConcurrentBlockingQueue<T> : IDisposable {
	const int InitialCapacity = 4; // Must be power of 2
	readonly object _monitorObject = new();
	T[] _buffer = ArrayPool<T>.Shared.Rent(InitialCapacity);
	ulong _headBitmask = (ulong) (InitialCapacity - 1);
	bool _isDisposed;
	ulong _writeCounter;
	ulong _readCounter;
	
	int WriteHead => (int) (_writeCounter & _headBitmask); 
	int ReadHead => (int) (_readCounter & _headBitmask);
	int Count => (int) (_writeCounter - _readCounter);
	int Capacity => (int) (_headBitmask + 1UL);

	public void Enqueue(T item) {
		lock (_monitorObject) {
			ObjectDisposedException.ThrowIf(_isDisposed, this);
			if (Count == Capacity) GrowBuffer();
			_buffer[WriteHead] = item;
			++_writeCounter;
			Monitor.Pulse(_monitorObject);
		}
	}
	
	public bool TryDequeue(out T outItem) {
		lock (_monitorObject) {
			ObjectDisposedException.ThrowIf(_isDisposed, this);
			if (Count == 0) {
				outItem = default!;
				return false;
			}
			else {
				outItem = TakeNextItem();
				return true;
			}
		}
	}
	
	public T DequeueBlocking() {
		lock (_monitorObject) {
			while (!_isDisposed) {
				if (Count > 0) return TakeNextItem();
				Monitor.Wait(_monitorObject);
			}
			throw new ObjectDisposedException(ToString());
		}
	}
	
	T TakeNextItem() {
		Debug.Assert(Monitor.IsEntered(_monitorObject));
		Debug.Assert(Count > 0);
		var result = _buffer[ReadHead];
		_buffer[ReadHead] = default!;
		++_readCounter;
		return result;
	}
	
	void GrowBuffer() {
		Debug.Assert(Monitor.IsEntered(_monitorObject));
		Debug.Assert(WriteHead == ReadHead);
		var newBuffer = ArrayPool<T>.Shared.Rent(Capacity * 2);
		Array.Copy(_buffer, WriteHead, newBuffer, 0, Capacity - WriteHead);
		Array.Copy(_buffer, 0, newBuffer, Capacity - WriteHead, WriteHead);
		ArrayPool<T>.Shared.Return(_buffer);
		_writeCounter = (ulong) Capacity;
		_readCounter = 0UL;
		_buffer = newBuffer;
		_headBitmask = (ulong) (Capacity * 2) - 1UL;
	}

	public void Dispose() {
		lock (_monitorObject) {
			if (_isDisposed) return;
			try {
				ArrayPool<T>.Shared.Return(_buffer);
			}
			finally {
				_isDisposed = true;
				Monitor.PulseAll(_monitorObject);
			}
		}
	}
}