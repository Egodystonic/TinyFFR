// Created on 2024-02-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed unsafe class UnmanagedBuffer<T> : IDisposable, IEnumerable<T> where T : unmanaged {
	public struct Enumerator : IEnumerator<T> {
		readonly UnmanagedBuffer<T> _owner;
		int _curIndex;
		
		public T Current {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _owner[_curIndex];
		}
		object IEnumerator.Current => Current;

		public Enumerator(UnmanagedBuffer<T> owner) {
			_owner = owner;
			Reset();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MoveNext() => ++_curIndex < _owner.Length;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset() => _curIndex = -1;

		public void Dispose() { /* no op */ }
	}

	readonly int? _alignment;

	public T* BufferPointer { get; private set; }
	public Span<T> AsSpan {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(BufferPointer, Length);
	}

	public ref T BufferStartRef {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.AsRef<T>(BufferPointer);
	}
	public int Length { get; private set; }
	bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => BufferPointer == null;
	}

	public T this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => GetAtIndex(index);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => SetAtIndex(index, value);
	}

	public UnmanagedBuffer(int initialLength, int? alignment = null) {
		Length = initialLength;
		_alignment = alignment;

		var lengthBytes = (uint) (initialLength * sizeof(T));

		if (_alignment is { } a) {
			BufferPointer = (T*) NativeMemory.AlignedAlloc(lengthBytes, (uint) a);
		}
		else {
			BufferPointer = (T*) NativeMemory.Alloc(lengthBytes);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T GetAtIndex(int index) {
		if (IsDisposed) ObjectDisposedException.ThrowIf(IsDisposed, this);
		if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and < Length.");
		return BufferPointer[index];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetAtIndex(int index, T value) {
		if (IsDisposed) ObjectDisposedException.ThrowIf(IsDisposed, this);
		if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and < Length.");
		BufferPointer[index] = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DoubleSize() => Resize(Length * 2);

	public void Resize(int numElements) {
		if (numElements < 0) throw new ArgumentOutOfRangeException(nameof(numElements), numElements, $"Must be positive or zero.");
		if (IsDisposed) ObjectDisposedException.ThrowIf(IsDisposed, this);

		Length = numElements;
		var newLengthBytes = (uint) (numElements * sizeof(T));

		if (_alignment is { } alignment) {
			BufferPointer = (T*) NativeMemory.AlignedRealloc(BufferPointer, newLengthBytes, (uint) alignment);
		}
		else {
			BufferPointer = (T*) NativeMemory.Realloc(BufferPointer, newLengthBytes);
		}
	}

	public void Dispose() {
		if (IsDisposed) return;

		if (_alignment.HasValue) {
			NativeMemory.AlignedFree(BufferPointer);
		}
		else {
			NativeMemory.Free(BufferPointer);
		}

		BufferPointer = null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Enumerator GetEnumerator() => new(this);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
}