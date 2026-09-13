// Created on 2024-08-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

#pragma warning disable CA1815 // "Should implement IEquatable" -- It's not recommended to compare function pointers, so there's no real way to provide equality for this type (plus it's not particularly useful anyway)
#pragma warning disable CA1710 // "Should end in collection-like suffix" -- I don't really want this used like a collection, I only implement IROL<> because we get it for free due to Count and indexer property already existing, so why not
// Represents an enumerator that takes a copy of TIn and uses a pointer to a static indexer and count method to avoid accidental garbage generation
/// <summary>
/// An instance of an <see cref="IndirectEnumerable{TIn,TOut}"/> lets you enumerate, count, and copy all of the <typeparamref name="TOut"/>s in a <typeparamref name="TIn"/> according to some property/function.
/// In most cases you can use this type like a pseudo-collection; it implements <see cref="IReadOnlyList{TOut}"/>
/// </summary>
/// <remarks>
/// <para>
/// This type is designed to take an input type <typeparamref name="TIn"/> and allow garbage-free iteration over some property or facet of that type,
/// yielding a sequence of <typeparamref name="TOut"/> values, without exposing the memory or mechanism of generation for those values
/// (which in some cases may be unmanaged or ad-hoc).
/// </para>
/// <para>
/// This has some important restrictions -- because instances of <see cref="IndirectEnumerable{TIn,TOut}"/> do not themselves "contain" or "own" any actual memory, the
/// <typeparamref name="TOut"/> collection must be enumerated before the owning <typeparamref name="TIn"/> instance is modified or disposed. Attempting to enumerate this
/// instance after its 'parent' <typeparamref name="TIn"/> has been modified will usually result in an <see cref="InvalidOperationException"/> being thrown.
/// </para>
/// <para>
/// In cases where you need a copy of the items beyond the lifetime of this <see cref="IndirectEnumerable{TIn,TOut}"/>, use <see cref="CopyTo"/>/<see cref="TryCopyTo"/>. 
/// </para>
/// </remarks>
/// <typeparam name="TIn">The type of object that provides the enumerable items.</typeparam>
/// <typeparam name="TOut">The item type to enumerate over.</typeparam>
public readonly unsafe struct IndirectEnumerable<TIn, TOut> : IReadOnlyList<TOut> {
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

		/// <inheritdoc />
		public TOut Current {
			get {
				ThrowIfInvalid();
				return _getItemFunc(_input, _curIndex);
			}
		}
		object IEnumerator.Current => Current!;

		/// <inheritdoc />
		public bool MoveNext() {
			_curIndex++;
			return _curIndex < _count;
		}
		/// <inheritdoc />
		public void Reset() => _curIndex = -1;
		/// <inheritdoc />
		public void Dispose() { /* no op */ }

		void ThrowIfInvalid() {
			if (_getVersionFunc == null || _getItemFunc == null) throw InvalidObjectException.InvalidDefault<Enumerator>();
			if (_getVersionFunc(_input) != _inputVersion) throw new InvalidOperationException($"{_input} was modified, this {nameof(IndirectEnumerable<TIn, TOut>)} is no longer valid.");
		}
	}

	readonly TIn _input;
	readonly int _inputVersion;
	readonly delegate* managed<TIn, int> _getCountFunc;
	readonly delegate* managed<TIn, int> _getVersionFunc;
	readonly delegate* managed<TIn, int, TOut> _getItemFunc;
	
	public static IndirectEnumerable<TIn, TOut> Empty {
		get {
			return new IndirectEnumerable<TIn, TOut>(
				default!,
				-1,
				&GetZeroCount,
				&GetNegOneVer,
				&ThrowIfAccessed
			);
		}
	}
	static int GetZeroCount(TIn _) => 0;
	static int GetNegOneVer(TIn _) => -1;
	static TOut ThrowIfAccessed(TIn _, int __) => throw new InvalidOperationException("This enumerable is empty.");

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

	public IndirectEnumerable(TIn input, int inputVersion, delegate*<TIn, int> getCountFunc, delegate*<TIn, int> getVersionFunc, delegate*<TIn, int, TOut> getItemFunc) {
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
		if (_getCountFunc == null || _getItemFunc == null) throw InvalidObjectException.InvalidDefault<IndirectEnumerable<TIn, TOut>>();
		if (_getVersionFunc(_input) != _inputVersion) throw new InvalidOperationException($"{_input} was modified, this {nameof(IndirectEnumerable<TIn, TOut>)} is no longer valid.");
	}
}