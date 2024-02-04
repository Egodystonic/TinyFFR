// Created on 2024-01-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Interop;

// This is a ref struct to help ensure it isn't boxed or lives outside the scope of the method it's returned to.
public readonly unsafe ref struct UnmanagedResourceIterator<T> {
	public struct Enumerator : IEnumerator<T> {
		readonly int _count;
		readonly object? _instanceParam;
		readonly delegate* managed<object?, int, T> _getItemFunc;
		int _curIndex;

		internal Enumerator(object? instanceParam, int count, delegate*<object?, int, T> getItemFunc) {
			_instanceParam = instanceParam;
			_count = count;
			_getItemFunc = getItemFunc;
			_curIndex = -1;
		}
		
		public T Current => _getItemFunc(_instanceParam, _curIndex);
		object IEnumerator.Current => Current!;

		public bool MoveNext() {
			_curIndex++;
			return _curIndex < _count;
		}
		public void Reset() => _curIndex = -1;
		public void Dispose() { /* no op */ }
	}

	readonly object? _instanceParam;
	// TODO maybe a readonly fixed 8-byte optional arg to allow parameterization of the collection
	readonly delegate* managed<object?, int> _getCountFunc;
	readonly delegate* managed<object?, int, T> _getItemFunc;

	public int Count {
		get {
			ThrowIfInvalid();
			return _getCountFunc(_instanceParam);
		}
	}
	public T this[int index] {
		get {
			ThrowIfInvalid();
			if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and < Count ({Count}).");
			return _getItemFunc(_instanceParam, index);
		}
	}

	internal UnmanagedResourceIterator(object? instanceParam, delegate*<object?, int> getCountFunc, delegate*<object?, int, T> getItemFunc) {
		ArgumentNullException.ThrowIfNull(getCountFunc);
		ArgumentNullException.ThrowIfNull(getItemFunc);
		_instanceParam = instanceParam;
		_getCountFunc = getCountFunc;
		_getItemFunc = getItemFunc;
	}

	public Enumerator GetEnumerator() {
		ThrowIfInvalid();
		return new Enumerator(_instanceParam, Count, _getItemFunc);
	}

	internal void ThrowIfInvalid() {
		if (_getCountFunc == null) throw InvalidObjectException.InvalidDefault(typeof(UnmanagedResourceIterator<T>));
	}

	public bool Equals(UnmanagedResourceIterator<T> other) {
		return _instanceParam == other._instanceParam && _getCountFunc == other._getCountFunc && _getItemFunc == other._getItemFunc;
	}

	public override bool Equals(object? obj) {
		throw new NotSupportedException("Can not invoke Equals(object) on ref-struct.");
	}

	public override int GetHashCode() {
		return HashCode.Combine(_instanceParam, (IntPtr) _getCountFunc, (IntPtr) _getItemFunc);
	}

	public static bool operator ==(UnmanagedResourceIterator<T> left, UnmanagedResourceIterator<T> right) => left.Equals(right);
	public static bool operator !=(UnmanagedResourceIterator<T> left, UnmanagedResourceIterator<T> right) => !left.Equals(right);
}