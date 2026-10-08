// Created on 2025-02-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Resources.Memory;

/// <summary>
/// Represents an <see cref="IArrayPoolBackedDictionary{TKey, TValue}"/> that can not be disposed.
/// </summary>
/// <typeparam name="TKey">The dictionary key.</typeparam>
/// <typeparam name="TValue">The dictionary value.</typeparam>
public interface INonDisposableArrayPoolBackedDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
	/// <summary>
	/// Returns an enumerator that iterates through the collection without allocating any garbage.
	/// </summary>
	new ArrayPoolBackedDictionaryEnumerator<TKey, TValue> GetEnumerator();
}

/// <summary>
/// A garbage-free enumerator over an <see cref="INonDisposableArrayPoolBackedDictionary{TKey,TValue}"/> or <see cref="IArrayPoolBackedDictionary{TKey,TValue}"/>.
/// </summary>
/// <typeparam name="TKey">The dictionary key.</typeparam>
/// <typeparam name="TValue">The dictionary value.</typeparam>
public struct ArrayPoolBackedDictionaryEnumerator<TKey, TValue> : IEnumerator<KeyValuePair<TKey, TValue>> {
	readonly ArrayPoolBackedMap<TKey, TValue> _owner;
	readonly int _version;
	int _bucketIndex;
	int _indexInBucket;

	/// <inheritdoc />
	public KeyValuePair<TKey, TValue> Current { get; private set; } = default;
	object IEnumerator.Current => Current!;

	internal ArrayPoolBackedDictionaryEnumerator(ArrayPoolBackedMap<TKey, TValue> owner) {
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
/// Represents an <see cref="IDictionary{TKey, TValue}"/> whose underlying heap storage is pooled, eliminating GC churn.
/// </summary>
/// <typeparam name="TKey">The dictionary key.</typeparam>
/// <typeparam name="TValue">The dictionary value.</typeparam>
public interface IArrayPoolBackedDictionary<TKey, TValue> : INonDisposableArrayPoolBackedDictionary<TKey, TValue>, IDisposable;
