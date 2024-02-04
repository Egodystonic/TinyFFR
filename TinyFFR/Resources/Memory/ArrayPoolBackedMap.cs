// Created on 2024-02-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;

namespace Egodystonic.TinyFFR.Resources.Memory;

// Not aiming to be anywhere near as fast or optimised or clever as I'm sure .NET's Dictionary is for now-- can improve perf in future if necessary. Just trying to avoid garbage generation.
sealed class ArrayPoolBackedMap<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable {
	public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
		readonly ArrayPoolBackedMap<TKey, TValue> _owner;
		int _curIndex;

		public KeyValuePair<TKey, TValue> Current {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _owner.GetPairAtIndex(_curIndex);
		}
		object IEnumerator.Current => Current!;

		public Enumerator(ArrayPoolBackedMap<TKey, TValue> owner) {
			_owner = owner;
			Reset();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MoveNext() => ++_curIndex < _owner.Count;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset() => _curIndex = -1;

		public void Dispose() { /* no op */ }
	}

	const int HashMask = 0b11_1111;
	const int NumBuckets = HashMask + 1;
	readonly ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>[] _buckets;
	
	public ArrayPoolBackedMap() {
		_buckets = ArrayPool<ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>>.Shared.Rent(HashMask + 1);
		for (var i = 0; i < NumBuckets; ++i) _buckets[i] = new ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>();
	}

	public int Count {
		get {
			var result = 0;
			for (var i = 0; i < NumBuckets; ++i) result += _buckets[i].Count;
			return result;
		}
	}
	public TValue this[TKey key] {
		get => (GetKvpFromKey(key) ?? throw new KeyNotFoundException($"Key '{key}' was not found in this map.")).Value;
		set {
			var bucket = GetBucket(key);
			var index = GetIndexFromBucket(bucket, key);

			if (index is { } i) bucket[i] = new(key, value);
			else bucket.Add(new(key, value));
		}
	}
	ICollection<TKey> IDictionary<TKey, TValue>.Keys => throw new NotSupportedException();
	ICollection<TValue> IDictionary<TKey, TValue>.Values => throw new NotSupportedException();
	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly { get; } = false;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(TKey key, TValue value) => Add(new(key, value));
	public void Add(KeyValuePair<TKey, TValue> item) {
		var bucket = GetBucket(item.Key);
		if (ContainsKey(item.Key)) throw new ArgumentException($"Key '{item.Key}' already exists in this map.");

		bucket.Add(item);
	}

	public void Clear() {
		for (var i = 0; i < NumBuckets; ++i) _buckets[i].Clear();
	}
	public void ClearWithoutZeroingMemory() {
		for (var i = 0; i < NumBuckets; ++i) _buckets[i].ClearWithoutZeroingMemory();
	}

	public bool ContainsKey(TKey key) => GetIndexFromBucket(GetBucket(key), key).HasValue;

	public KeyValuePair<TKey, TValue> GetPairAtIndex(int index) {
		for (var i = 0; i < NumBuckets; ++i) {
			var bucket = _buckets[i];
			if (index < bucket.Count) return bucket[index];

			index -= bucket.Count;
		}

		throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be > 0 and < Count.");
	}

	public Enumerator GetEnumerator() => new(this);
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();

	public bool Contains(KeyValuePair<TKey, TValue> item) {
		var valueComparer = EqualityComparer<TValue>.Default;

		var existingKvp = GetKvpFromKey(item.Key);
		return existingKvp.HasValue && valueComparer.Equals(existingKvp.Value.Value, item.Value);
	}

	public bool TryGetValue(TKey key, out TValue value) {
		var existingKvp = GetKvpFromKey(key);
		if (existingKvp.HasValue) {
			value = existingKvp.Value.Value;
			return true;
		}
		else {
			value = default!;
			return false;
		}
	}

	public bool Remove(TKey key) {
		var bucket = GetBucket(key);
		var index = GetIndexFromBucket(bucket, key);
		if (!index.HasValue) return false;
		bucket.RemoveAt(index.Value);
		return true;
	}

	public bool Remove(KeyValuePair<TKey, TValue> item) {
		var bucket = GetBucket(item.Key);
		var index = GetIndexFromBucket(bucket, item.Key);
		if (!index.HasValue) return false;

		var valueComparer = EqualityComparer<TValue>.Default;

		if (!valueComparer.Equals(bucket[index.Value].Value, item.Value)) return false;
		bucket.RemoveAt(index.Value);
		return true;
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
		for (var i = 0; i < NumBuckets; ++i) {
			_buckets[i].CopyTo(array, arrayIndex);
			arrayIndex += _buckets[i].Count;
		}
	}

	public void CopyTo(Span<KeyValuePair<TKey, TValue>> span) {
		for (var i = 0; i < NumBuckets; ++i) {
			_buckets[i].AsSpan.CopyTo(span);
			span = span[_buckets[i].Count..];
		}
	}

	public void CopyKeysTo(Span<TKey> span) {
		for (var i = 0; i < NumBuckets; ++i) {
			for (var j = 0; j < _buckets[i].Count; ++j) {
				span[0] = _buckets[i][j].Key;
				span = span[1..];
			}
		}
	}

	public void CopyValuesTo(Span<TValue> span) {
		for (var i = 0; i < NumBuckets; ++i) {
			for (var j = 0; j < _buckets[i].Count; ++j) {
				span[0] = _buckets[i][j].Value;
				span = span[1..];
			}
		}
	}

	public void Dispose() {
		for (var i = 0; i < NumBuckets; ++i) _buckets[i].Dispose();
		ArrayPool<ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>>.Shared.Return(_buckets);
	}

	static int GetBucketIndex(TKey key) => (key?.GetHashCode() & HashMask) ?? 0;

	ArrayPoolBackedVector<KeyValuePair<TKey, TValue>> GetBucket(TKey key) => _buckets[GetBucketIndex(key)];

	static KeyValuePair<TKey, TValue>? GetKvpFromBucket(ArrayPoolBackedVector<KeyValuePair<TKey, TValue>> bucket, TKey key) {
		var index = GetIndexFromBucket(bucket, key);
		return index != null ? bucket[index.Value] : null;
	}

	static int? GetIndexFromBucket(ArrayPoolBackedVector<KeyValuePair<TKey, TValue>> bucket, TKey key) {
		var keyComparer = EqualityComparer<TKey>.Default;

		for (var i = 0; i < bucket.Count; ++i) {
			if (keyComparer.Equals(bucket[i].Key, key)) return i;
		}

		return null;
	}

	KeyValuePair<TKey, TValue>? GetKvpFromKey(TKey key) => GetKvpFromBucket(GetBucket(key), key);
}