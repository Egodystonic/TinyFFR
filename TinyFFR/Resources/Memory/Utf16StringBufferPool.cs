// Created on 2024-09-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;

namespace Egodystonic.TinyFFR.Resources.Memory;

static class Utf16StringBufferPool {
	public readonly record struct Utf16PoolStringHandle(char[] BorrowedArray, int Length) {
		public Span<char> AsSpan => BorrowedArray.AsSpan(0, Length);
		public string AsNewStringObject => new(AsSpan);
	}

	static readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;

	public static Utf16PoolStringHandle RentAndCopy(ReadOnlySpan<char> src) {
		var result = Rent(src.Length);
		src.CopyTo(result.AsSpan);
		return result;
	}
	public static Utf16PoolStringHandle Rent(int stringLength) {
		return new(_charPool.Rent(stringLength), stringLength);
	}

	public static void Return(Utf16PoolStringHandle handle) {
		_charPool.Return(handle.BorrowedArray);
	}
}