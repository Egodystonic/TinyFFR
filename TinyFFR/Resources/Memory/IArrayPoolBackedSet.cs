// Created on 2026-06-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources.Memory;

/// <summary>
/// Represents an <see cref="IArrayPoolBackedSet{T}"/> that can not be disposed.
/// </summary>
/// <typeparam name="T">The set element type.</typeparam>
public interface INonDisposableArrayPoolBackedSet<T> : ISet<T> {
	/// <summary>
	/// Returns an enumerator that iterates through the collection without allocating any garbage.
	/// </summary>
	new ArrayPoolBackedSetEnumerator<T> GetEnumerator();

	/// <summary>
	/// Adds every element in <paramref name="items"/> to this set. Elements that are already in this set are ignored.
	/// </summary>
	/// <remarks>
	/// Enumerating <paramref name="items"/> may generate garbage, depending on its type. Use <see cref="AddRange(ReadOnlySpan{T})"/> to add elements without generating any garbage.
	/// </remarks>
	/// <param name="items">The elements to add.</param>
	void AddRange(IEnumerable<T> items);

	/// <summary>
	/// Adds every element in <paramref name="items"/> to this set. Elements that are already in this set are ignored.
	/// </summary>
	/// <param name="items">The elements to add.</param>
	void AddRange(ReadOnlySpan<T> items);
}

/// <summary>
/// A garbage-free enumerator over an <see cref="INonDisposableArrayPoolBackedSet{T}"/> or <see cref="IArrayPoolBackedSet{T}"/>.
/// </summary>
/// <typeparam name="T">The set element type.</typeparam>
public struct ArrayPoolBackedSetEnumerator<T> : IEnumerator<T> {
	readonly ArrayPoolBackedSet<T> _owner;
	readonly int _version;
	int _bucketIndex;
	int _indexInBucket;

	/// <inheritdoc />
	public T Current { get; private set; } = default!;
	object IEnumerator.Current => Current!;

	internal ArrayPoolBackedSetEnumerator(ArrayPoolBackedSet<T> owner) {
		_owner = owner;
		_version = owner.Version;
		Reset();
	}

	/// <inheritdoc />
	public bool MoveNext() {
		if (_version != _owner.Version) throw new InvalidOperationException("Collection was modified.");
		while (_bucketIndex < _owner.BucketCount) {
			var bucket = _owner.GetBucketAtIndex(_bucketIndex);
			if (++_indexInBucket < bucket.Count) {
				Current = bucket[_indexInBucket];
				return true;
			}

			++_bucketIndex;
			_indexInBucket = -1;
		}

		Current = default!;
		return false;
	}

	/// <inheritdoc />
	public void Reset() {
		_bucketIndex = 0;
		_indexInBucket = -1;
		Current = default!;
	}

	/// <inheritdoc />
	public void Dispose() { /* no op */ }
}

/// <summary>
/// Represents an <see cref="ISet{T}"/> whose underlying heap storage is pooled, eliminating GC churn.
/// </summary>
/// <typeparam name="T">The set element type.</typeparam>
public interface IArrayPoolBackedSet<T> : INonDisposableArrayPoolBackedSet<T>, IDisposable;
