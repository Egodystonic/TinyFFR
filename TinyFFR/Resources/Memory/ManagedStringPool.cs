// Created on 2024-09-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed class ManagedStringPool {
	public readonly record struct RentedStringHandle(char[] BorrowedArray, int Length) {
		public ReadOnlySpan<char> AsSpan => BorrowedArray.AsSpan(0, Length);
		public string AsNewStringObject => new(AsSpan);
	}

	readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;

	public RentedStringHandle RentAndCopy(ReadOnlySpan<char> src) {
		var result = new RentedStringHandle(_charPool.Rent(src.Length), src.Length);
		src.CopyTo(result.BorrowedArray.AsSpan());
		return result;
	}

	public void Return(RentedStringHandle handle) {
		_charPool.Return(handle.BorrowedArray);
	}
}