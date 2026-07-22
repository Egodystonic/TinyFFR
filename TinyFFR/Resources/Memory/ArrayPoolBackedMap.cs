// Created on 2024-02-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;

namespace Egodystonic.TinyFFR.Resources.Memory;

// Not aiming to be anywhere near as fast or optimised or clever as I'm sure .NET's Dictionary is for now-- can improve perf in future if necessary. Just trying to avoid garbage generation.
sealed class ArrayPoolBackedMap<TKey, TValue> : IArrayPoolBackedDictionary<TKey, TValue> {
	public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
		readonly ArrayPoolBackedMap<TKey, TValue> _owner;
		readonly int _version;
		int _bucketIndex;
		int _indexInBucket;

		public KeyValuePair<TKey, TValue> Current { get; private set; } = default;
		object IEnumerator.Current => Current!;

		public Enumerator(ArrayPoolBackedMap<TKey, TValue> owner) {
			_owner = owner;
			_version = owner.Version;
			Reset();
		}

		public bool MoveNext() {
			if (_version != _owner.Version) throw new InvalidOperationException("Collection was modified.");
			while (_bucketIndex < _owner._numBuckets) {
				var bucket = _owner._buckets[_bucketIndex];
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

		public void Reset() {
			_bucketIndex = 0;
			_indexInBucket = -1;
			Current = default!;
		}

		public void Dispose() { /* no op */ }
	}

	public struct KeyEnumerator : IEnumerator<TKey>, IEnumerable<TKey> {
		readonly ArrayPoolBackedMap<TKey, TValue> _owner;
		readonly int _version;
		int _bucketIndex;
		int _indexInBucket;

		public TKey Current { get; private set; } = default!;
		object IEnumerator.Current => Current!;

		public KeyEnumerator(ArrayPoolBackedMap<TKey, TValue> owner) {
			_owner = owner;
			_version = owner.Version;
			Reset();
		}

		public bool MoveNext() {
			if (_version != _owner.Version) throw new InvalidOperationException("Collection was modified.");
			while (_bucketIndex < _owner._numBuckets) {
				var bucket = _owner._buckets[_bucketIndex];
				if (++_indexInBucket < bucket.Count) {
					Current = bucket[_indexInBucket].Key;
					return true;
				}

				++_bucketIndex;
				_indexInBucket = -1;
			}

			Current = default!;
			return false;
		}

		public void Reset() {
			_bucketIndex = 0;
			_indexInBucket = -1;
			Current = default!;
		}

		public void Dispose() { /* no op */ }

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => this;
		public KeyEnumerator GetEnumerator() => this;
	}

	public struct ValueEnumerator : IEnumerator<TValue>, IEnumerable<TValue> {
		readonly ArrayPoolBackedMap<TKey, TValue> _owner;
		readonly int _version;
		int _bucketIndex;
		int _indexInBucket;

		public TValue Current { get; private set; } = default!;
		object IEnumerator.Current => Current!;

		public ValueEnumerator(ArrayPoolBackedMap<TKey, TValue> owner) {
			_owner = owner;
			_version = owner.Version;
			Reset();
		}

		public bool MoveNext() {
			if (_version != _owner.Version) throw new InvalidOperationException("Collection was modified.");
			while (_bucketIndex < _owner._numBuckets) {
				var bucket = _owner._buckets[_bucketIndex];
				if (++_indexInBucket < bucket.Count) {
					Current = bucket[_indexInBucket].Value;
					return true;
				}

				++_bucketIndex;
				_indexInBucket = -1;
			}

			Current = default!;
			return false;
		}

		public void Reset() {
			_bucketIndex = 0;
			_indexInBucket = -1;
			Current = default!;
		}

		public void Dispose() { /* no op */ }

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => this;
		public ValueEnumerator GetEnumerator() => this;
	}

	const int InitialBucketCount = 4; // Must be power of two
	const int MaxAverageTargetBucketOccupancy = 4;
	ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>[] _buckets;
	int _numBuckets;
	int _hashMask;
	int _count;
	
	// These fields memoise lookup to make conseecutive scan a little better
	int _lastIndexLookupVersion = -1;
	int _lastIndexLookupIndex;
	int _lastIndexLookupBucket;
	int _lastIndexLookupBucketStart;

	public ArrayPoolBackedMap() {
		_numBuckets = InitialBucketCount;
		_hashMask = InitialBucketCount - 1;
		_buckets = ArrayPool<ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>>.Shared.Rent(_numBuckets);
		for (var i = 0; i < _numBuckets; ++i) _buckets[i] = new ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>();
	}

	public int Version { get; private set; } = 0;

	public int Count => _count;

	public TValue this[TKey key] {
		get => (GetKvpFromKey(key) ?? throw new KeyNotFoundException($"Key '{key}' was not found in this map.")).Value;
		set {
			var bucket = GetBucket(key);
			var index = GetIndexFromBucket(bucket, key);

			if (index is { } i) bucket[i] = new(key, value);
			else {
				bucket.Add(new(key, value));
				++_count;
				++Version;
				GrowBucketsIfOverloaded();
			}
		}
	}
	ICollection<TKey> IDictionary<TKey, TValue>.Keys {
		get {
			var result = new TKey[Count];
			CopyKeysTo(result);
			return result;
		}
	}
	ICollection<TValue> IDictionary<TKey, TValue>.Values {
		get {
			var result = new TValue[Count];
			CopyValuesTo(result);
			return result;
		}
	}
	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly { get; } = false;

	public KeyEnumerator Keys => new(this);
	public ValueEnumerator Values => new(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(TKey key, TValue value) => Add(new(key, value));
	public void Add(KeyValuePair<TKey, TValue> item) {
		var bucket = GetBucket(item.Key);
		if (GetIndexFromBucket(bucket, item.Key).HasValue) throw new ArgumentException($"Key '{item.Key}' already exists in this map.");

		bucket.Add(item);
		++_count;
		++Version;
		GrowBucketsIfOverloaded();
	}

	public bool TryAdd(TKey key, TValue value) {
		var bucket = GetBucket(key);
		if (GetIndexFromBucket(bucket, key).HasValue) return false;
		bucket.Add(new(key, value));
		++_count;
		++Version;
		GrowBucketsIfOverloaded();
		return true;
	}

	public void Clear() {
		for (var i = 0; i < _numBuckets; ++i) _buckets[i].Clear();
		_count = 0;
		++Version;
	}
	public void ClearWithoutZeroingMemory() {
		for (var i = 0; i < _numBuckets; ++i) _buckets[i].ClearWithoutZeroingMemory();
		_count = 0;
		++Version;
	}

	public bool ContainsKey(TKey key) => GetIndexFromBucket(GetBucket(key), key).HasValue;

	public KeyValuePair<TKey, TValue> GetPairAtIndex(int index) {
		if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be > 0 and < Count.");

		var bucketIndex = 0;
		var remainder = index;
		if (_lastIndexLookupVersion == Version && index >= _lastIndexLookupIndex) {
			bucketIndex = _lastIndexLookupBucket;
			remainder = index - _lastIndexLookupBucketStart;
		}

		var bucketStart = index - remainder;
		for (var i = bucketIndex; i < _numBuckets; ++i) {
			var bucket = _buckets[i];
			if (remainder < bucket.Count) {
				_lastIndexLookupVersion = Version;
				_lastIndexLookupIndex = index;
				_lastIndexLookupBucket = i;
				_lastIndexLookupBucketStart = bucketStart;
				return bucket[remainder];
			}

			remainder -= bucket.Count;
			bucketStart += bucket.Count;
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
		--_count;
		++Version;
		return true;
	}

	public bool Remove(KeyValuePair<TKey, TValue> item) {
		var bucket = GetBucket(item.Key);
		var index = GetIndexFromBucket(bucket, item.Key);
		if (!index.HasValue) return false;

		var valueComparer = EqualityComparer<TValue>.Default;

		if (!valueComparer.Equals(bucket[index.Value].Value, item.Value)) return false;
		bucket.RemoveAt(index.Value);
		--_count;
		++Version;
		return true;
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
		for (var i = 0; i < _numBuckets; ++i) {
			_buckets[i].CopyTo(array, arrayIndex);
			arrayIndex += _buckets[i].Count;
		}
	}

	public void CopyTo(Span<KeyValuePair<TKey, TValue>> span) {
		for (var i = 0; i < _numBuckets; ++i) {
			_buckets[i].AsSpan.CopyTo(span);
			span = span[_buckets[i].Count..];
		}
	}

	public void CopyKeysTo(Span<TKey> span) {
		for (var i = 0; i < _numBuckets; ++i) {
			for (var j = 0; j < _buckets[i].Count; ++j) {
				span[0] = _buckets[i][j].Key;
				span = span[1..];
			}
		}
	}

	public void CopyValuesTo(Span<TValue> span) {
		for (var i = 0; i < _numBuckets; ++i) {
			for (var j = 0; j < _buckets[i].Count; ++j) {
				span[0] = _buckets[i][j].Value;
				span = span[1..];
			}
		}
	}

	public void Dispose() {
		for (var i = 0; i < _numBuckets; ++i) _buckets[i].Dispose();
		ArrayPool<ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>>.Shared.Return(_buckets, clearArray: true);
		++Version;
	}

	void GrowBucketsIfOverloaded() {
		if (_count <= _numBuckets * MaxAverageTargetBucketOccupancy) return;

		var newNumBuckets = _numBuckets * 2;
		var newHashMask = newNumBuckets - 1;
		var newBuckets = ArrayPool<ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>>.Shared.Rent(newNumBuckets);

		// Maintainer's note:
		// Bucket count is always a power of two, so widening the mask by one bit splits each existing bucket in to
		// itself and exactly one new bucket. That lets us keep the existing bucket instances rather than reallocating all of them.
		for (var i = 0; i < _numBuckets; ++i) newBuckets[i] = _buckets[i];
		for (var i = _numBuckets; i < newNumBuckets; ++i) newBuckets[i] = new ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>();

		for (var i = 0; i < _numBuckets; ++i) {
			var bucket = newBuckets[i];
			for (var j = bucket.Count - 1; j >= 0; --j) {
				var kvp = bucket[j];
				var newBucketIndex = GetBucketIndex(kvp.Key, newHashMask);
				if (newBucketIndex == i) continue;
				newBuckets[newBucketIndex].Add(kvp);
				bucket.RemoveAt(j);
			}
		}

		ArrayPool<ArrayPoolBackedVector<KeyValuePair<TKey, TValue>>>.Shared.Return(_buckets, clearArray: true);
		_buckets = newBuckets;
		_numBuckets = newNumBuckets;
		_hashMask = newHashMask;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int GetBucketIndex(TKey key, int hashMask) => (key?.GetHashCode() & hashMask) ?? 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int GetBucketIndex(TKey key) => GetBucketIndex(key, _hashMask);

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
