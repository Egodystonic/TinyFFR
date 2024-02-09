// Created on 2024-01-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Interop;

// This needs to be a ref struct so that we can safely convert ArgData to a Span without needing to fix it (as ref structs can only live on the stack, but normal structs could be boxed or allocated as fields heap objects)
// Name "LazyReadOnlySpan" isn't *really* what this is exactly, but it hopefully makes more sense to consumers of the API (should make it easy to understand what it is at first glance)
// TODO is this actually useful or should we just pre-iterate everything and store the stuff in ArrayPoolBackedVectors etc?
public readonly unsafe ref struct LazyReadOnlySpan<T> {
	static readonly ArgData NullArgData = default;

	public struct Enumerator : IEnumerator<T> {
		readonly int _count;
		readonly object? _instanceParam;
		readonly ArgData _argData;
		readonly delegate* managed<object?, ReadOnlySpan<byte>, int, T> _getItemFunc;
		int _curIndex;

		internal Enumerator(object? instanceParam, ArgData argData, int count, delegate*<object?, ReadOnlySpan<byte>, int, T> getItemFunc) {
			_instanceParam = instanceParam;
			_argData = argData;
			_count = count;
			_getItemFunc = getItemFunc;
			_curIndex = -1;
		}
		
		public T Current => _getItemFunc(_instanceParam, _argData, _curIndex);
		object IEnumerator.Current => Current!;

		public bool MoveNext() {
			_curIndex++;
			return _curIndex < _count;
		}
		public void Reset() => _curIndex = -1;
		public void Dispose() { /* no op */ }
	}

	[InlineArray(DataLengthBytes)]
	internal struct ArgData {
		public const int DataLengthBytes = 8;
		byte _;
	}

	readonly object? _instanceParam;
	readonly ArgData _argData;
	readonly delegate* managed<object?, ReadOnlySpan<byte>, int> _getCountFunc;
	readonly delegate* managed<object?, ReadOnlySpan<byte>, int, T> _getItemFunc;

	public int Count {
		get {
			ThrowIfInvalid();
			return _getCountFunc(_instanceParam, _argData);
		}
	}
	public T this[int index] {
		get {
			ThrowIfInvalid();
			if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and < Count ({Count}).");
			return _getItemFunc(_instanceParam, _argData, index);
		}
	}

	internal LazyReadOnlySpan(object? instanceParam, delegate*<object?, ReadOnlySpan<byte>, int> getCountFunc, delegate*<object?, ReadOnlySpan<byte>, int, T> getItemFunc) : this(instanceParam, NullArgData, getCountFunc, getItemFunc) { }
	internal LazyReadOnlySpan(object? instanceParam, scoped ReadOnlySpan<byte> argData, delegate*<object?, ReadOnlySpan<byte>, int> getCountFunc, delegate*<object?, ReadOnlySpan<byte>, int, T> getItemFunc) {
		ArgumentNullException.ThrowIfNull(getCountFunc);
		ArgumentNullException.ThrowIfNull(getItemFunc);
		if (argData.Length > ArgData.DataLengthBytes) throw new ArgumentException("Argument data must be no more than 8 bytes in length.");
		_instanceParam = instanceParam;
		_getCountFunc = getCountFunc;
		_getItemFunc = getItemFunc;
		_argData = default;
		argData.CopyTo(_argData);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ReadOnlySpan<byte> CreateArgData<TArgData>(ref readonly TArgData data) where TArgData : unmanaged {
		var result = MemoryMarshal.AsBytes(new ReadOnlySpan<TArgData>(in data));
		if (result.Length > ArgData.DataLengthBytes) throw new ArgumentException("Argument data must be no more than 8 bytes in length.");
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ref readonly TArgData ConvertArgData<TArgData>(ReadOnlySpan<byte> data) where TArgData : unmanaged {
		return ref MemoryMarshal.AsRef<TArgData>(data[..sizeof(TArgData)]);
	}

	public Enumerator GetEnumerator() {
		ThrowIfInvalid();
		return new Enumerator(_instanceParam, _argData, Count, _getItemFunc);
	}

	internal void ThrowIfInvalid() {
		if (_getCountFunc == null) throw InvalidObjectException.InvalidDefault(typeof(LazyReadOnlySpan<T>));
	}

	public bool Equals(LazyReadOnlySpan<T> other) {
		return _instanceParam == other._instanceParam && _getCountFunc == other._getCountFunc && _getItemFunc == other._getItemFunc;
	}

	public override bool Equals(object? obj) {
		throw new NotSupportedException("Can not invoke Equals(object) on ref-struct.");
	}

	public override int GetHashCode() {
		return HashCode.Combine(_instanceParam, (IntPtr) _getCountFunc, (IntPtr) _getItemFunc);
	}

	public static bool operator ==(LazyReadOnlySpan<T> left, LazyReadOnlySpan<T> right) => left.Equals(right);
	public static bool operator !=(LazyReadOnlySpan<T> left, LazyReadOnlySpan<T> right) => !left.Equals(right);
}