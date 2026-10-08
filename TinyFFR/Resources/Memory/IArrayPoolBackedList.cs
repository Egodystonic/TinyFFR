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
