// Created on 2024-08-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

#pragma warning disable CA1815 // "Should implement IEquatable" -- It's not recommended to compare function pointers, so there's no real way to provide equality for this type (plus it's not particularly useful anyway)
// Represents an enumerator that takes a copy of TIn and uses a pointer to a static indexer and count method to avoid accidental garbage generation
public readonly unsafe struct TypedReferentIterator<TIn, TOut> : IEnumerable<TOut> {
	public struct Enumerator : IEnumerator<TOut> {
		readonly TIn _input;
		readonly int _count;
		readonly int _inputVersion;
		readonly delegate*<TIn, int> _getVersionFunc;
		readonly delegate* managed<TIn, int, TOut> _getItemFunc;
		int _curIndex;

		internal Enumerator(TIn input, int inputVersion, int count, delegate* managed<TIn, int, TOut> getItemFunc, delegate*<TIn, int> getVersionFunc) {
			_input = input;
			_inputVersion = inputVersion;
			_count = count;
			_getItemFunc = getItemFunc;
			_getVersionFunc = getVersionFunc;
			Reset();
		}

		public TOut Current {
			get {
				ThrowIfInvalid();
				return _getItemFunc(_input, _curIndex);
			}
		}
		object IEnumerator.Current => Current!;

		public bool MoveNext() {
			_curIndex++;
			return _curIndex < _count;
		}
		public void Reset() => _curIndex = -1;
		public void Dispose() { /* no op */ }

		void ThrowIfInvalid() {
			if (_getVersionFunc == null || _getItemFunc == null) throw InvalidObjectException.InvalidDefault<Enumerator>();
			if (_getVersionFunc(_input) != _inputVersion) throw new InvalidOperationException($"{_input} was modified, this {nameof(TypedReferentIterator<TIn, TOut>)} is no longer valid.");
		}
	}

	readonly TIn _input;
	readonly int _inputVersion;
	readonly delegate* managed<TIn, int> _getCountFunc;
	readonly delegate* managed<TIn, int> _getVersionFunc;
	readonly delegate* managed<TIn, int, TOut> _getItemFunc;

	public int Count {
		get {
			ThrowIfInvalid();
			return _getCountFunc(_input);
		}
	}
	public TOut this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ElementAt(index);
	}

	public TypedReferentIterator(TIn input, int inputVersion, delegate*<TIn, int> getCountFunc, delegate*<TIn, int> getVersionFunc, delegate*<TIn, int, TOut> getItemFunc) {
		ArgumentNullException.ThrowIfNull(getCountFunc);
		ArgumentNullException.ThrowIfNull(getVersionFunc);
		ArgumentNullException.ThrowIfNull(getItemFunc);

		_input = input;
		_inputVersion = inputVersion;
		_getVersionFunc = getVersionFunc;
		_getCountFunc = getCountFunc;
		_getItemFunc = getItemFunc;
	}

	public TOut ElementAt(int index) {
		ThrowIfInvalid();
		if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and < Count ({Count}).");
		return _getItemFunc(_input, index);
	}

	public void CopyTo(Span<TOut> dest) {
		ThrowIfInvalid();
		for (var i = 0; i < Count; ++i) {
			dest[i] = this[i];
		}
	}
	public bool TryCopyTo(Span<TOut> dest) {
		ThrowIfInvalid();
		if (dest.Length < Count) return false;
		CopyTo(dest);
		return true;
	}

	public Enumerator GetEnumerator() {
		ThrowIfInvalid();
		return new Enumerator(_input, _inputVersion, Count, _getItemFunc, _getVersionFunc);
	}
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<TOut> IEnumerable<TOut>.GetEnumerator() => GetEnumerator();

	internal void ThrowIfInvalid() {
		if (_getCountFunc == null || _getItemFunc == null) throw InvalidObjectException.InvalidDefault<TypedReferentIterator<TIn, TOut>>();
		if (_getVersionFunc(_input) != _inputVersion) throw new InvalidOperationException($"{_input} was modified, this {nameof(TypedReferentIterator<TIn, TOut>)} is no longer valid.");
	}
}