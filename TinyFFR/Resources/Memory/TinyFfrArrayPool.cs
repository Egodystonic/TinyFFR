// Created on 2026-09-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers;
using System.Threading;

namespace Egodystonic.TinyFFR.Resources.Memory;

interface IReleasablePool {
	int ReleaseOrder { get; }
	void ReleaseAllPooledMemory();
}

static class TinyFfrArrayPool {
	public const int LargeBufferThresholdBytes = 1024 * 1024;

	// Object pools must be released before array pools: releasing a pooled object returns its backing array
	// to an array pool, so doing it the other way round would leave those arrays retained after the valve.
	public const int ObjectPoolReleaseOrder = 0;
	public const int ArrayPoolReleaseOrder = 1;

	static readonly Lock _registryLock = new();
	static IReleasablePool[] _registeredPools = new IReleasablePool[16];
	static int _registeredPoolCount;

	internal static void Register(IReleasablePool pool) {
		lock (_registryLock) {
			if (_registeredPoolCount == _registeredPools.Length) {
				var grown = new IReleasablePool[_registeredPools.Length * 2];
				Array.Copy(_registeredPools, grown, _registeredPoolCount);
				_registeredPools = grown;
			}
			_registeredPools[_registeredPoolCount++] = pool;
		}
	}

	public static void ReleaseAllPooledMemory() {
		lock (_registryLock) {
			for (var order = ObjectPoolReleaseOrder; order <= ArrayPoolReleaseOrder; ++order) {
				for (var i = 0; i < _registeredPoolCount; ++i) {
					if (_registeredPools[i].ReleaseOrder == order) _registeredPools[i].ReleaseAllPooledMemory();
				}
			}
		}
	}
}

// Maintainer's note: This exists because ArrayPool<T>.Shared caches only a bounded number of buffers per
// array-size bucket (s_maxArraysPerPartition, 32, times one partition per core) and silently DISCARDS every
// return beyond that. TinyFFR routinely holds tens of thousands of small buffers rented simultaneously (one
// dependency-tracker set per live resource, each of which rents again for its own buckets), far past that
// ceiling, so the overwhelming majority of returns were being thrown away and re-allocated on the next use
// This pool retains every returned buffer indefinitely instead, freeing them only when ReleaseAllPooledMemory
// is called (which LocalTinyFfrFactory does upon disposal, bounding retention to one factory's lifetime)
// Buffers larger than LargeBufferThresholdBytes are delegated to ArrayPool<T>.Shared, which was measured to
// handle multi-megabyte asset buffers better than we would and whose bounded retention is desirable there.
sealed class TinyFfrArrayPool<T> : ArrayPool<T>, IReleasablePool {
	const int MinimumBucketLength = 16;
	const int MinimumBucketLengthLog2 = 4;
	const int InitialBucketStoreCapacity = 16;

	// ReSharper disable StaticMemberInGenericType This is deliberate specialization over T
	static readonly int ElementSizeBytes = Unsafe.SizeOf<T>();
	static readonly int PooledBucketCount = CalculatePooledBucketCount();
	static readonly int MaxPooledLength = PooledBucketCount > 0 ? MinimumBucketLength << (PooledBucketCount - 1) : 0;
	static readonly object[] BucketLocks = CreateBucketLocks();
	static readonly T[]?[][] BucketStores = CreateBucketStores();
	static readonly int[] BucketStoreCounts = new int[PooledBucketCount];

	public static readonly new TinyFfrArrayPool<T> Shared = new();

	[ThreadStatic] static T[]?[]? _threadLocalBuffers;
	[ThreadStatic] static int _threadLocalEpoch;
	static int _epoch;
	// ReSharper restore StaticMemberInGenericType

#if DEBUG
	static readonly HashSet<T[]> RentedArrays = new(ReferenceComparer.Instance);

	sealed class ReferenceComparer : IEqualityComparer<T[]> {
		public static readonly ReferenceComparer Instance = new();
		public bool Equals(T[]? x, T[]? y) => ReferenceEquals(x, y);
		public int GetHashCode(T[] obj) => RuntimeHelpers.GetHashCode(obj);
	}
#endif

	TinyFfrArrayPool() => TinyFfrArrayPool.Register(this);

	static int CalculatePooledBucketCount() {
		var result = 0;
		var length = (long) MinimumBucketLength;
		while (length * ElementSizeBytes <= TinyFfrArrayPool.LargeBufferThresholdBytes && length <= Int32.MaxValue) {
			++result;
			length <<= 1;
		}
		return result;
	}

	static object[] CreateBucketLocks() {
		var result = new object[PooledBucketCount];
		for (var i = 0; i < result.Length; ++i) result[i] = new object();
		return result;
	}

	static T[]?[][] CreateBucketStores() {
		var result = new T[PooledBucketCount][][];
		for (var i = 0; i < result.Length; ++i) result[i] = new T[InitialBucketStoreCapacity][];
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int GetBucketIndex(int length) => BitOperations.Log2((uint) (length - 1) | (MinimumBucketLength - 1)) - MinimumBucketLengthLog2 + 1;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int GetBucketLength(int bucketIndex) => MinimumBucketLength << bucketIndex;

	static T[]?[] GetThreadLocalBuffers() {
		var currentEpoch = Volatile.Read(ref _epoch);
		if (_threadLocalBuffers is not { } buffers || _threadLocalEpoch != currentEpoch) {
			buffers = new T[PooledBucketCount][];
			_threadLocalBuffers = buffers;
			_threadLocalEpoch = currentEpoch;
		}
		return buffers;
	}

	public override T[] Rent(int minimumLength) {
		ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);
		if (minimumLength == 0) return Array.Empty<T>();
		if (minimumLength > MaxPooledLength) return ArrayPool<T>.Shared.Rent(minimumLength);

		var bucketIndex = GetBucketIndex(minimumLength);
		var result = RentFromThreadLocal(bucketIndex) ?? RentFromBucketStore(bucketIndex) ?? new T[GetBucketLength(bucketIndex)];

#if DEBUG
		lock (RentedArrays) {
			if (!RentedArrays.Add(result)) {
				throw new InvalidOperationException($"{nameof(TinyFfrArrayPool<T>)} handed out an array it believes is already rented (this is a bug in TinyFFR).");
			}
		}
#endif

		return result;
	}

	static T[]? RentFromThreadLocal(int bucketIndex) {
		var buffers = GetThreadLocalBuffers();
		var result = buffers[bucketIndex];
		if (result != null) buffers[bucketIndex] = null;
		return result;
	}

	static T[]? RentFromBucketStore(int bucketIndex) {
		lock (BucketLocks[bucketIndex]) {
			var count = BucketStoreCounts[bucketIndex];
			if (count == 0) return null;

			var store = BucketStores[bucketIndex];
			var result = store[--count];
			store[count] = null;
			BucketStoreCounts[bucketIndex] = count;
			return result;
		}
	}

	public override void Return(T[] array, bool clearArray = false) {
		ArgumentNullException.ThrowIfNull(array);
		if (array.Length == 0) return;

		if (array.Length > MaxPooledLength) {
			ArrayPool<T>.Shared.Return(array, clearArray);
			return;
		}

		var bucketIndex = GetBucketIndex(array.Length);
		if (array.Length != GetBucketLength(bucketIndex)) {
#if DEBUG
			throw new ArgumentException($"Array of length {array.Length} can not have been rented from {nameof(TinyFfrArrayPool<T>)}.", nameof(array));
#else
			return;
#endif
		}

#if DEBUG
		lock (RentedArrays) {
			if (!RentedArrays.Remove(array)) {
				throw new ArgumentException($"Array was returned to {nameof(TinyFfrArrayPool<T>)} but was not rented from it, or has already been returned.", nameof(array));
			}
		}
#endif

		if (clearArray) Array.Clear(array);
		if (ReturnToThreadLocal(bucketIndex, array)) return;
		ReturnToBucketStore(bucketIndex, array);
	}

	static bool ReturnToThreadLocal(int bucketIndex, T[] array) {
		var buffers = GetThreadLocalBuffers();
		if (buffers[bucketIndex] != null) return false;
		buffers[bucketIndex] = array;
		return true;
	}

	static void ReturnToBucketStore(int bucketIndex, T[] array) {
		lock (BucketLocks[bucketIndex]) {
			var store = BucketStores[bucketIndex];
			var count = BucketStoreCounts[bucketIndex];

			if (count == store.Length) {
				var grown = new T[store.Length * 2][];
				Array.Copy(store, grown, count);
				store = grown;
				BucketStores[bucketIndex] = store;
			}

			store[count] = array;
			BucketStoreCounts[bucketIndex] = count + 1;
		}
	}

	public int ReleaseOrder => TinyFfrArrayPool.ArrayPoolReleaseOrder;

	public void ReleaseAllPooledMemory() {
		for (var i = 0; i < PooledBucketCount; ++i) {
			lock (BucketLocks[i]) {
				BucketStores[i] = new T[InitialBucketStoreCapacity][];
				BucketStoreCounts[i] = 0;
			}
		}
		Interlocked.Increment(ref _epoch);
	}

	public override string ToString() => $"TinyFFR Array Pool <{typeof(T).Name}>";
}
