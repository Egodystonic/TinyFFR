// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources.Memory;

readonly unsafe record struct NativeHeapMemory<T> : IDisposable where T : unmanaged {
	readonly nint _buffer;
	readonly int _length;

	public Span<T> Span => new((T*) _buffer, _length);

	NativeHeapMemory(void* buffer, int length) {
		_buffer = (nint) buffer;
		_length = length;
	}

	public static NativeHeapMemory<T> Allocate(int numElements) {
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numElements);
		return new(NativeMemory.Alloc((nuint) numElements, (nuint) sizeof(T)), numElements);
	}

	public static NativeHeapMemory<T> AllocateZeroed(int numElements) {
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numElements);
		return new(NativeMemory.AllocZeroed((nuint) numElements, (nuint) sizeof(T)), numElements);
	}

	public void Dispose() => NativeMemory.Free((void*) _buffer);
}
