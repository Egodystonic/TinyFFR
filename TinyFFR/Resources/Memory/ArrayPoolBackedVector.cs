// Created on 2024-02-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed class ArrayPoolBackedVector<T> : IArrayPoolBackedList<T> {
	public struct Enumerator : IEnumerator<T> {
		readonly ArrayPoolBackedVector<T> _owner;
		readonly int _version;
		int _curIndex;

		public T Current { get; private set; } = default!;
		object IEnumerator.Current => Current!;

		public Enumerator(ArrayPoolBackedVector<T> owner) {
			_owner = owner;
			_version = owner.Version;
			Reset();
		}

		public bool MoveNext() {
			if (_version != _owner.Version) throw new InvalidOperationException("Collection was modified.");
			if (++_curIndex < _owner.Count) {
				Current = _owner[_curIndex];
				return true;
			}
			
			Current = default!;
			return false;
		}

		public void Reset() {
			_curIndex = -1;
			Current = default!;
		}

		public void Dispose() { /* no op */ }
	}

	public const int DefaultInitialCapacity = 4;
	T[] _backingArray;

	public int Version { get; private set; } = 0;
	
	public Span<T> AsSpan {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _backingArray.AsSpan(0, Count);
	}

	public int Count { get; private set; } = 0;
	bool ICollection<T>.IsReadOnly { get; } = false;

	public T this[int index] {
		get {
			if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and < Count.");
			return _backingArray[index];
		}
		set {
			if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and < Count.");
			_backingArray[index] = value;
		}
	}

	public ArrayPoolBackedVector(int initialCapacity = DefaultInitialCapacity) {
		_backingArray = ArrayPool<T>.Shared.Rent(initialCapacity);
	}

	public void Add(T item) {
		IncreaseBackingArraySizeIfFull();
		_backingArray[Count++] = item;
		++Version;
	}

	public void Clear() {
		Array.Clear(_backingArray);
		Count = 0;
		++Version;
	}

	public ref T GetValueByRef(int index) {
		if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and < Count.");
		return ref _backingArray[index];
	}

	public void ClearWithoutZeroingMemory() {
		Count = 0;
		++Version;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(T item) => IndexOf(item) >= 0;

	public bool Remove(T item) {
		var itemIndex = IndexOf(item);
		if (itemIndex < 0) return false;

		RemoveAt(itemIndex);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int IndexOf(T item) => Array.IndexOf(_backingArray, item, 0, Count);

	public void Insert(int index, T item) {
		IncreaseBackingArraySizeIfFull();
		if (index > Count || index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and <= Count.");

		Array.Copy(_backingArray, index, _backingArray, index + 1, Count - index);
		_backingArray[index] = item;
		Count++;
		++Version;
	}

	public void RemoveAt(int index) {
		if (index >= Count || index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and < Count.");

		Count--;
		Array.Copy(_backingArray, index + 1, _backingArray, index, Count - index);
		_backingArray[Count] = default!;
		++Version;
	}

	public T RemoveLast() {
		if (Count == 0) throw new InvalidOperationException("Vector is empty.");

		Count--;
		var result = _backingArray[Count];
		_backingArray[Count] = default!;
		++Version;
		return result;
	}

	public bool TryRemoveLast(out T result) {
		if (Count == 0) {
			result = default!;
			return false;
		}

		Count--;
		result = _backingArray[Count];
		_backingArray[Count] = default!;
		++Version;
		return true;
	}

	public void CopyTo(T[] array, int arrayIndex) => AsSpan.CopyTo(array.AsSpan(arrayIndex));

	public Enumerator GetEnumerator() => new(this);
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

	public void Dispose() {
		++Version;
		ArrayPool<T>.Shared.Return(_backingArray);
		_backingArray = null!;
	}

	void IncreaseBackingArraySizeIfFull() {
		if (Count < _backingArray.Length) return;

		var newBackingArray = ArrayPool<T>.Shared.Rent(_backingArray.Length * 2);
		_backingArray.CopyTo(newBackingArray, 0);
		ArrayPool<T>.Shared.Return(_backingArray, clearArray: true);
		_backingArray = newBackingArray;
	}
}