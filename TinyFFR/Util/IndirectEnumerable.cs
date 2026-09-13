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
	/// <summary>
	/// The enumerator type returned by <see cref="IndirectEnumerable{TIn,TOut}.GetEnumerator"/>
	/// (i.e. the type used via duck-typing in a <c>foreach</c> statement).
	/// </summary>
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
	
	/// <summary>
	/// An <see cref="IndirectEnumerable{TIn,TOut}"/> with a <see cref="Count"/> of <c>0</c>.
	/// </summary>
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

	/// <summary>
	/// The number of <typeparamref name="TOut"/> values exposed by this instance.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the underlying <typeparamref name="TIn"/> instance has been modified or disposed since this <see cref="IndirectEnumerable{TIn,TOut}"/> was created.
	/// </exception>
	public int Count {
		get {
			ThrowIfInvalid();
			return _getCountFunc(_input);
		}
	}
	/// <summary>
	/// Gets the <typeparamref name="TOut"/> at the given <paramref name="index"/>; equivalent to <see cref="ElementAt"/>.
	/// </summary>
	/// <param name="index">The index of the item to retrieve. Must be <c>&gt;= 0</c> and <c>&lt; <see cref="Count"/></c>.</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the underlying <typeparamref name="TIn"/> instance has been modified or disposed since this <see cref="IndirectEnumerable{TIn,TOut}"/> was created.
	/// </exception>
	public TOut this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ElementAt(index);
	}

	/// <summary>
	/// Constructs a new <see cref="IndirectEnumerable{TIn,TOut}"/> that exposes the <typeparamref name="TOut"/> values yielded by <paramref name="input"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This constructor is intended for types that expose one of their own facets as an <see cref="IndirectEnumerable{TIn,TOut}"/> (for example, a shape exposing its corners or edges). It's left public
	/// as a convenience for advanced API users that want to construct their own indirect enumerable; but this is not intended to be used in typical API usage scenarios.
	/// </para>
	/// <para>
	/// The three function pointers must all be static/unmanaged-callable methods, and are invoked by this instance (and any <see cref="Enumerator"/>s created from it) instead of allocating a delegate or capturing state.
	/// </para>
	/// </remarks>
	/// <param name="input">The value passed to each of <paramref name="getCountFunc"/>, <paramref name="getVersionFunc"/> and <paramref name="getItemFunc"/> to provide the count/version/items respectively.</param>
	/// <param name="inputVersion">A version number for <paramref name="input"/> at the time of construction, used to detect subsequent modification of <paramref name="input"/> (see the type-level remarks).</param>
	/// <param name="getCountFunc">A function that returns the current number of items in <paramref name="input"/>. Must not be <see langword="null"/>.</param>
	/// <param name="getVersionFunc">A function that returns the current version of <paramref name="input"/>, matched against <paramref name="inputVersion"/> to detect modification. Must not be <see langword="null"/>.</param>
	/// <param name="getItemFunc">A function that returns the item at a given index in <paramref name="input"/>. Must not be <see langword="null"/>.</param>
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

	/// <summary>
	/// Gets the <typeparamref name="TOut"/> at the given <paramref name="index"/>.
	/// </summary>
	/// <param name="index">The index of the item to retrieve. Must be <c>&gt;= 0</c> and <c>&lt; <see cref="Count"/></c>.</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the underlying <typeparamref name="TIn"/> instance has been modified or disposed since this <see cref="IndirectEnumerable{TIn,TOut}"/> was created.
	/// </exception>
	/// <seealso cref="this[]"/>
	public TOut ElementAt(int index) {
		ThrowIfInvalid();
		if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be >= 0 and < Count ({Count}).");
		return _getItemFunc(_input, index);
	}

	/// <summary>
	/// Copies every <typeparamref name="TOut"/> exposed by this instance into <paramref name="dest"/>, in order.
	/// </summary>
	/// <param name="dest">The destination span to copy into. Must be at least <see cref="Count"/> elements long.</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the underlying <typeparamref name="TIn"/> instance has been modified or disposed since this <see cref="IndirectEnumerable{TIn,TOut}"/> was created.
	/// </exception>
	public void CopyTo(Span<TOut> dest) {
		ThrowIfInvalid();
		for (var i = 0; i < Count; ++i) {
			dest[i] = this[i];
		}
	}
	/// <summary>
	/// Attempts to copy every <typeparamref name="TOut"/> exposed by this instance into <paramref name="dest"/>, in order.
	/// </summary>
	/// <param name="dest">The destination span to copy into.</param>
	/// <returns><see langword="false"/> if <paramref name="dest"/> is shorter than <see cref="Count"/> (in which case nothing is copied); <see langword="true"/> otherwise.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the underlying <typeparamref name="TIn"/> instance has been modified or disposed since this <see cref="IndirectEnumerable{TIn,TOut}"/> was created.
	/// </exception>
	public bool TryCopyTo(Span<TOut> dest) {
		ThrowIfInvalid();
		if (dest.Length < Count) return false;
		CopyTo(dest);
		return true;
	}

	/// <summary>
	/// Returns an enumerator that iterates over every <typeparamref name="TOut"/> exposed by this instance.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the underlying <typeparamref name="TIn"/> instance has been modified or disposed since this <see cref="IndirectEnumerable{TIn,TOut}"/> was created.
	/// </exception>
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