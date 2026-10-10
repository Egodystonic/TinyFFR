// Created on 2025-02-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Resources.Memory;

/// <summary>
/// Represents an <see cref="IArrayPoolBackedList{T}"/> that can not be disposed.
/// </summary>
/// <typeparam name="T">The list element type.</typeparam>
public interface INonDisposableArrayPoolBackedList<T> : IList<T> {
	/// <summary>
	/// Returns an enumerator that iterates through the collection without allocating any garbage.
	/// </summary>
	new ArrayPoolBackedListEnumerator<T> GetEnumerator();

	/// <summary>
	/// The elements currently in this list, as a <see cref="Span{T}"/> over the list's pooled backing storage (<see cref="ICollection{T}.Count"/> elements long).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The returned span is invalidated by any subsequent modification of this list's length (e.g. adding, inserting, removing, or clearing elements),
	/// as those operations may move the elements to new backing storage and/or return the old storage to the pool for re-use elsewhere.
	/// Using the span after such a modification may read or write memory that no longer belongs to this list.
	/// </para>
	/// <para>
	/// Therefore, do not keep the returned span beyond the current operation (e.g. across frames); retrieve it again each time it is needed.
	/// Writing to the span's elements modifies this list's elements directly.
	/// </para>
	/// </remarks>
	Span<T> BackingSpan { get; }

	/// <summary>
	/// Adds every element in <paramref name="items"/> to the end of this list, in order.
	/// </summary>
	/// <remarks>
	/// Enumerating <paramref name="items"/> may generate garbage, depending on its type. Use <see cref="AddRange(ReadOnlySpan{T})"/> to add elements without generating any garbage.
	/// </remarks>
	/// <param name="items">The elements to add. May be this list itself.</param>
	void AddRange(IEnumerable<T> items);

	/// <summary>
	/// Adds every element in <paramref name="items"/> to the end of this list, in order.
	/// </summary>
	/// <param name="items">The elements to add. May be this list's own <see cref="BackingSpan"/> (or a slice of it).</param>
	void AddRange(ReadOnlySpan<T> items);
}

/// <summary>
/// A garbage-free enumerator over an <see cref="INonDisposableArrayPoolBackedList{T}"/> or <see cref="IArrayPoolBackedList{T}"/>.
/// </summary>
/// <typeparam name="T">The list element type.</typeparam>
public struct ArrayPoolBackedListEnumerator<T> : IEnumerator<T> {
	readonly ArrayPoolBackedVector<T> _owner;
	readonly int _version;
	int _curIndex;

	/// <inheritdoc />
	public T Current { get; private set; } = default!;
	object IEnumerator.Current => Current!;

	internal ArrayPoolBackedListEnumerator(ArrayPoolBackedVector<T> owner) {
		_owner = owner;
		_version = owner.Version;
		Reset();
	}

	/// <inheritdoc />
	public bool MoveNext() {
		if (_version != _owner.Version) throw new InvalidOperationException("Collection was modified.");
		if (++_curIndex < _owner.Count) {
			Current = _owner[_curIndex];
			return true;
		}
		
		Current = default!;
		return false;
	}

	/// <inheritdoc />
	public void Reset() {
		_curIndex = -1;
		Current = default!;
	}

	/// <inheritdoc />
	public void Dispose() { /* no op */ }
}

/// <summary>
/// Represents an <see cref="IList{T}"/> whose underlying heap storage is pooled, eliminating GC churn.
/// </summary>
/// <typeparam name="T">The list element type.</typeparam>
public interface IArrayPoolBackedList<T> : INonDisposableArrayPoolBackedList<T>, IDisposable;
