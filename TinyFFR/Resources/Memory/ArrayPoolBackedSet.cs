// Created on 2026-06-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers;

namespace Egodystonic.TinyFFR.Resources.Memory;

// Not aiming to be anywhere near as fast or optimised or clever as I'm sure .NET's HashSet is for now-- can improve perf in future if necessary. Just trying to avoid garbage generation.
sealed class ArrayPoolBackedSet<T> : IArrayPoolBackedSet<T> {
	public struct Enumerator : IEnumerator<T> {
		readonly ArrayPoolBackedSet<T> _owner;
		readonly int _version;
		int _bucketIndex;
		int _indexInBucket;

		public T Current { get; private set; } = default!;
		object IEnumerator.Current => Current!;

		public Enumerator(ArrayPoolBackedSet<T> owner) {
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

	const int InitialBucketCount = 4; // Must be power of two
	const int MaxAverageTargetBucketOccupancy = 8;
	ArrayPoolBackedVector<T>[] _buckets;
	int _numBuckets;
	int _hashMask;
	int _count;

	// These fields memoise lookup to make conseecutive scan a little better
	int _lastIndexLookupVersion = -1;
	int _lastIndexLookupIndex;
	int _lastIndexLookupBucket;
	int _lastIndexLookupBucketStart;

	public ArrayPoolBackedSet() {
		_numBuckets = InitialBucketCount;
		_hashMask = InitialBucketCount - 1;
		_buckets = ArrayPool<ArrayPoolBackedVector<T>>.Shared.Rent(_numBuckets);
		for (var i = 0; i < _numBuckets; ++i) _buckets[i] = new ArrayPoolBackedVector<T>();
	}

	public int Version { get; private set; } = 0;

	public int Count => _count;

	bool ICollection<T>.IsReadOnly { get; } = false;

	public bool Add(T item) {
		var bucket = GetBucket(item);
		if (GetIndexFromBucket(bucket, item).HasValue) return false;

		bucket.Add(item);
		++_count;
		++Version;
		GrowBucketsIfOverloaded();
		return true;
	}
	void ICollection<T>.Add(T item) => Add(item);

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

	public bool Contains(T item) => GetIndexFromBucket(GetBucket(item), item).HasValue;

	public bool Remove(T item) {
		var bucket = GetBucket(item);
		var index = GetIndexFromBucket(bucket, item);
		if (!index.HasValue) return false;
		bucket.RemoveAt(index.Value);
		--_count;
		++Version;
		return true;
	}

	public T GetItemAtIndex(int index) {
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
	IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

	public void CopyTo(T[] array, int arrayIndex) {
		for (var i = 0; i < _numBuckets; ++i) {
			_buckets[i].CopyTo(array, arrayIndex);
			arrayIndex += _buckets[i].Count;
		}
	}

	public void CopyTo(Span<T> span) {
		for (var i = 0; i < _numBuckets; ++i) {
			_buckets[i].AsSpan.CopyTo(span);
			span = span[_buckets[i].Count..];
		}
	}

	public void UnionWith(IEnumerable<T> other) {
		ArgumentNullException.ThrowIfNull(other);
		foreach (var item in other) Add(item);
	}

	public void ExceptWith(IEnumerable<T> other) {
		ArgumentNullException.ThrowIfNull(other);
		if (Count == 0) return;
		if (ReferenceEquals(other, this)) {
			Clear();
			return;
		}
		foreach (var item in other) Remove(item);
	}

	public void IntersectWith(IEnumerable<T> other) {
		ArgumentNullException.ThrowIfNull(other);
		if (Count == 0) return;

		using var otherSet = CreateTempSetFrom(other);
		for (var i = 0; i < _numBuckets; ++i) {
			var bucket = _buckets[i];
			for (var j = bucket.Count - 1; j >= 0; --j) {
				if (otherSet.Contains(bucket[j])) continue;
				bucket.RemoveAt(j);
				--_count;
			}
		}
		++Version;
	}

	public void SymmetricExceptWith(IEnumerable<T> other) {
		ArgumentNullException.ThrowIfNull(other);
		if (ReferenceEquals(other, this)) {
			Clear();
			return;
		}

		using var otherSet = CreateTempSetFrom(other);
		foreach (var item in otherSet) {
			if (!Remove(item)) Add(item);
		}
	}

	public bool IsSubsetOf(IEnumerable<T> other) {
		ArgumentNullException.ThrowIfNull(other);
		if (Count == 0) return true;

		using var otherSet = CreateTempSetFrom(other);
		if (Count > otherSet.Count) return false;
		foreach (var item in this) {
			if (!otherSet.Contains(item)) return false;
		}
		return true;
	}

	public bool IsSupersetOf(IEnumerable<T> other) {
		ArgumentNullException.ThrowIfNull(other);
		foreach (var item in other) {
			if (!Contains(item)) return false;
		}
		return true;
	}

	public bool IsProperSubsetOf(IEnumerable<T> other) {
		ArgumentNullException.ThrowIfNull(other);

		using var otherSet = CreateTempSetFrom(other);
		if (Count >= otherSet.Count) return false;
		foreach (var item in this) {
			if (!otherSet.Contains(item)) return false;
		}
		return true;
	}

	public bool IsProperSupersetOf(IEnumerable<T> other) {
		ArgumentNullException.ThrowIfNull(other);
		if (Count == 0) return false;

		using var otherSet = CreateTempSetFrom(other);
		if (Count <= otherSet.Count) return false;
		foreach (var item in otherSet) {
			if (!Contains(item)) return false;
		}
		return true;
	}

	public bool Overlaps(IEnumerable<T> other) {
		ArgumentNullException.ThrowIfNull(other);
		if (Count == 0) return false;
		foreach (var item in other) {
			if (Contains(item)) return true;
		}
		return false;
	}

	public bool SetEquals(IEnumerable<T> other) {
		ArgumentNullException.ThrowIfNull(other);

		using var otherSet = CreateTempSetFrom(other);
		if (Count != otherSet.Count) return false;
		foreach (var item in this) {
			if (!otherSet.Contains(item)) return false;
		}
		return true;
	}

	public void Dispose() {
		for (var i = 0; i < _numBuckets; ++i) _buckets[i].Dispose();
		ArrayPool<ArrayPoolBackedVector<T>>.Shared.Return(_buckets, clearArray: true);
		++Version;
	}

	void GrowBucketsIfOverloaded() {
		if (_count <= _numBuckets * MaxAverageTargetBucketOccupancy) return;

		var newNumBuckets = _numBuckets * 2;
		var newHashMask = newNumBuckets - 1;
		var newBuckets = ArrayPool<ArrayPoolBackedVector<T>>.Shared.Rent(newNumBuckets);

		// Maintainer's note:
		// Bucket count is always a power of two, so widening the mask by one bit splits each existing bucket in to
		// itself and exactly one new bucket. That lets us keep the existing bucket instances rather than reallocating all of them.
		for (var i = 0; i < _numBuckets; ++i) newBuckets[i] = _buckets[i];
		for (var i = _numBuckets; i < newNumBuckets; ++i) newBuckets[i] = new ArrayPoolBackedVector<T>();

		for (var i = 0; i < _numBuckets; ++i) {
			var bucket = newBuckets[i];
			for (var j = bucket.Count - 1; j >= 0; --j) {
				var item = bucket[j];
				var newBucketIndex = GetBucketIndex(item, newHashMask);
				if (newBucketIndex == i) continue;
				newBuckets[newBucketIndex].Add(item);
				bucket.RemoveAt(j);
			}
		}

		ArrayPool<ArrayPoolBackedVector<T>>.Shared.Return(_buckets, clearArray: true);
		_buckets = newBuckets;
		_numBuckets = newNumBuckets;
		_hashMask = newHashMask;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int GetBucketIndex(T item, int hashMask) => (item?.GetHashCode() & hashMask) ?? 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	int GetBucketIndex(T item) => GetBucketIndex(item, _hashMask);

	ArrayPoolBackedVector<T> GetBucket(T item) => _buckets[GetBucketIndex(item)];

	static int? GetIndexFromBucket(ArrayPoolBackedVector<T> bucket, T item) {
		var comparer = EqualityComparer<T>.Default;

		for (var i = 0; i < bucket.Count; ++i) {
			if (comparer.Equals(bucket[i], item)) return i;
		}

		return null;
	}

	static ArrayPoolBackedSet<T> CreateTempSetFrom(IEnumerable<T> other) {
		var result = new ArrayPoolBackedSet<T>();
		foreach (var item in other) result.Add(item);
		return result;
	}
}
