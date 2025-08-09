// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Text;

namespace Egodystonic.TinyFFR.Interop;

sealed unsafe class InteropStringBuffer : IDisposable {
	public InteropStringBuffer(int bufferLength, bool addOneForNullTerminator) {
		if (bufferLength <= 0) throw new ArgumentOutOfRangeException(nameof(bufferLength), bufferLength, "Buffer length must be positive.");
		if (addOneForNullTerminator) bufferLength++;

		checked { // Shouldn't be possible to overflow considering we checked for non-positive values, but just in case
			BufferPtr = (byte*) NativeMemory.AllocZeroed((uint) bufferLength);
		}
		BufferLength = bufferLength;
	}

	public byte* BufferPtr { get; }
	public int BufferLength { get; }
	public ref byte BufferRef => ref Unsafe.AsRef<byte>(BufferPtr);

	public Span<byte> AsSpan => new(BufferPtr, BufferLength);

	// Returns number of bytes written including null terminator
	public int ConvertFromUtf16(ReadOnlySpan<char> src) => ConvertFromUtf16(src, false, default);
	public int ConvertFromUtf16OrThrowIfBufferTooSmall(ReadOnlySpan<char> src, ReadOnlySpan<char> exceptionMessage) => ConvertFromUtf16(src, true, exceptionMessage);
	int ConvertFromUtf16(ReadOnlySpan<char> src, bool throwIfTruncated, ReadOnlySpan<char> exceptionMessage) {
		var subStrLength = src.Length;
		var lengthRequired = Encoding.UTF8.GetByteCount(src[..subStrLength]);
		while (lengthRequired > BufferLength) {
			if (throwIfTruncated) throw new InvalidOperationException(exceptionMessage.ToString());
			var diff = lengthRequired - BufferLength;
			if (diff > 0) subStrLength -= diff;
			else subStrLength -= 1;

			if (subStrLength <= 0) {
				BufferPtr[0] = 0;
				return 1;
			}

			lengthRequired = Encoding.UTF8.GetByteCount(src[..subStrLength]);
		}
		var numBytesWritten = Encoding.UTF8.GetBytes(src[..subStrLength], AsSpan);
		if (numBytesWritten < BufferLength) {
			BufferPtr[numBytesWritten] = 0;
			return numBytesWritten + 1;
		}
		else {
			if (throwIfTruncated) throw new InvalidOperationException(exceptionMessage.ToString());
			BufferPtr[BufferLength - 1] = 0;
			return BufferLength;
		}
	}

	public int ConvertToUtf16(Span<char> dest) {
		var firstZero = AsSpan.IndexOf((byte) 0);
		if (firstZero < 0) firstZero = BufferLength;
		var destSizeRequired = Encoding.UTF8.GetCharCount(BufferPtr, firstZero);

		while (destSizeRequired > dest.Length) {
			var diff = destSizeRequired - dest.Length;
			if (diff > 0) firstZero -= diff;
			else firstZero -= 1;
			
			if (firstZero <= 0) {
				dest[0] = '\0';
				return 1;
			}

			destSizeRequired = Encoding.UTF8.GetCharCount(BufferPtr, firstZero);
		}

		return Encoding.UTF8.GetChars(AsSpan[..firstZero], dest);
	}

	public int GetUtf16Length() {
		var firstZero = AsSpan.IndexOf((byte) 0);
		if (firstZero < 0) firstZero = BufferLength;
		return Encoding.UTF8.GetCharCount(BufferPtr, firstZero);
	}

	public override string ToString() {
		var span = new char[BufferLength];
		ConvertToUtf16(span);
		return new String(span);
	}

	public void Dispose() => NativeMemory.Free(BufferPtr);
}