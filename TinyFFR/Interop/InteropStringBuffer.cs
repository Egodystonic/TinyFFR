// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Text;

namespace Egodystonic.TinyFFR.Interop;

sealed unsafe class InteropStringBuffer : IDisposable {
	public InteropStringBuffer(int length, bool addOneForNullTerminator) {
		if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), length, "Buffer length must be positive.");
		if (addOneForNullTerminator) length++;

		checked { // Shouldn't be possible to overflow considering we checked for non-positive values, but just in case
			AsPointer = (byte*) NativeMemory.AllocZeroed((uint) length);
		}
		Length = length;
	}

	public byte* AsPointer { get; }
	public int Length { get; }
	public ref byte AsRef => ref Unsafe.AsRef<byte>(AsPointer);
	public Span<byte> AsSpan => new(AsPointer, Length);

	// Returns number of bytes written including null terminator
	public int ConvertFromUtf16(ReadOnlySpan<char> src) => ConvertFromUtf16(src, false, default);
	public int ConvertFromUtf16OrThrowIfBufferTooSmall(ReadOnlySpan<char> src, ReadOnlySpan<char> exceptionMessage) => ConvertFromUtf16(src, true, exceptionMessage);
	int ConvertFromUtf16(ReadOnlySpan<char> src, bool throwIfTruncated, ReadOnlySpan<char> exceptionMessage) {
		var subStrLength = src.Length;
		var lengthRequired = Encoding.UTF8.GetByteCount(src[..subStrLength]);
		while (lengthRequired > Length) {
			if (throwIfTruncated) throw new InvalidOperationException(exceptionMessage.ToString());
			var diff = lengthRequired - Length;
			if (diff > 0) subStrLength -= diff;
			else subStrLength -= 1;

			if (subStrLength <= 0) {
				AsPointer[0] = 0;
				return 1;
			}

			lengthRequired = Encoding.UTF8.GetByteCount(src[..subStrLength]);
		}
		var numBytesWritten = Encoding.UTF8.GetBytes(src[..subStrLength], AsSpan);
		if (numBytesWritten < Length) {
			AsPointer[numBytesWritten] = 0;
			return numBytesWritten + 1;
		}
		else {
			if (throwIfTruncated) throw new InvalidOperationException(exceptionMessage.ToString());
			AsPointer[Length - 1] = 0;
			return Length;
		}
	}

	public int ConvertToUtf16(Span<char> dest) {
		var firstZero = AsSpan.IndexOf((byte) 0);
		if (firstZero < 0) firstZero = Length;
		var destSizeRequired = Encoding.UTF8.GetCharCount(AsPointer, firstZero);

		while (destSizeRequired > dest.Length) {
			var diff = destSizeRequired - dest.Length;
			if (diff > 0) firstZero -= diff;
			else firstZero -= 1;
			
			if (firstZero <= 0) {
				dest[0] = '\0';
				return 1;
			}

			destSizeRequired = Encoding.UTF8.GetCharCount(AsPointer, firstZero);
		}

		return Encoding.UTF8.GetChars(AsSpan[..firstZero], dest);
	}

	public int GetUtf16Length() {
		var firstZero = AsSpan.IndexOf((byte) 0);
		if (firstZero < 0) firstZero = Length;
		return Encoding.UTF8.GetCharCount(AsPointer, firstZero);
	}

	public override string ToString() {
		var span = new char[Length];
		return new String(span, 0, ConvertToUtf16(span));
	}

	public void Dispose() => NativeMemory.Free(AsPointer);
}