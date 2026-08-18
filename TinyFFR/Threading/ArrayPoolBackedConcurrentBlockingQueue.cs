// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers;
using System.Diagnostics;
using System.Threading;

namespace Egodystonic.TinyFFR.Threading;

sealed class ArrayPoolBackedConcurrentBlockingQueue<T> : IDisposable {
	const int InitialCapacity = 4; // Must be power of 2
	readonly object _monitorObject = new();
	T[] _buffer;
	ulong _headBitmask;
	bool _isDisposed;
	bool _queueIsSealed;
	ulong _writeCounter;
	ulong _readCounter;

	public ArrayPoolBackedConcurrentBlockingQueue() {
		_buffer = ArrayPool<T>.Shared.Rent(InitialCapacity);
		_headBitmask = GetUsableCapacityBitmask(_buffer);
	}

	int WriteHead => (int) (_writeCounter & _headBitmask);
	int ReadHead => (int) (_readCounter & _headBitmask);
	int Count => (int) (_writeCounter - _readCounter);
	int Capacity => (int) (_headBitmask + 1UL);

	public bool HasPendingItems {
		get {
			lock (_monitorObject) {
				return !_isDisposed && Count > 0;
			}
		}
	}

	static ulong GetUsableCapacityBitmask(T[] buffer) {
		var usableCapacity = (uint) buffer.Length;
		if (!BitOperations.IsPow2(usableCapacity)) usableCapacity = BitOperations.RoundUpToPowerOf2(usableCapacity) >> 1;
		return usableCapacity - 1UL;
	}

	public void Enqueue(T item) {
		lock (_monitorObject) {
			ObjectDisposedException.ThrowIf(_isDisposed, this);
			if (_queueIsSealed) throw new InvalidOperationException($"Can not enqueue to this {nameof(ArrayPoolBackedConcurrentBlockingQueue<T>)} after {nameof(Seal)} has been invoked.");
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

	public bool TryDequeueBlocking(out T outItem) {
		lock (_monitorObject) {
			while (!_isDisposed) {
				if (Count > 0) {
					outItem = TakeNextItem();
					return true;
				}
				if (_queueIsSealed) break;
				Monitor.Wait(_monitorObject);
			}
			outItem = default!;
			return false;
		}
	}

	public void Seal() {
		lock (_monitorObject) {
			if (_queueIsSealed) return;
			_queueIsSealed = true;
			Monitor.PulseAll(_monitorObject);
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
		var oldCapacity = Capacity;
		var oldReadHead = ReadHead;
		var newBuffer = ArrayPool<T>.Shared.Rent(oldCapacity * 2);
		Array.Copy(_buffer, oldReadHead, newBuffer, 0, oldCapacity - oldReadHead);
		Array.Copy(_buffer, 0, newBuffer, oldCapacity - oldReadHead, oldReadHead);
		ArrayPool<T>.Shared.Return(_buffer, true);
		_buffer = newBuffer;
		_headBitmask = GetUsableCapacityBitmask(newBuffer);
		_writeCounter = (ulong) oldCapacity;
		_readCounter = 0UL;
	}

	public void Dispose() {
		lock (_monitorObject) {
			if (_isDisposed) return;
			try {
				ArrayPool<T>.Shared.Return(_buffer, true);
			}
			finally {
				_buffer = Array.Empty<T>();
				_headBitmask = 0UL;
				_writeCounter = 0UL;
				_readCounter = 0UL;
				_isDisposed = true;
				_queueIsSealed = true;
				Monitor.PulseAll(_monitorObject);
			}
		}
	}
}
