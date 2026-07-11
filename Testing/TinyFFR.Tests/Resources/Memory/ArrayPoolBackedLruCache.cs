// Created on 2026-07-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
class ArrayPoolBackedLruCacheTest {
	[Test]
	public void ShouldThrowForNonPositiveCapacity() {
		Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayPoolBackedLruCache<int, int>(0));
		Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayPoolBackedLruCache<int, int>(-1));
	}

	[Test]
	public void ShouldStoreAndRetrieveValues() {
		using var cache = new ArrayPoolBackedLruCache<int, int>(3);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);

		Assert.IsTrue(cache.TryGet(1, out var v));
		Assert.AreEqual(10, v);
		Assert.IsTrue(cache.TryGet(2, out v));
		Assert.AreEqual(20, v);
		Assert.IsFalse(cache.TryGet(3, out _));
	}

	[Test]
	public void ShouldUpdateValueForExistingKey() {
		using var cache = new ArrayPoolBackedLruCache<int, int>(3);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);
		cache.AddOrSet(3, 30);
		cache.AddOrSet(2, 21);

		Assert.IsTrue(cache.TryGet(1, out var v));
		Assert.AreEqual(10, v);
		Assert.IsTrue(cache.TryGet(2, out v));
		Assert.AreEqual(21, v);
		Assert.IsTrue(cache.TryGet(3, out v));
		Assert.AreEqual(30, v);
	}

	[Test]
	public void ShouldEvictLeastRecentlyUsedItem() {
		using var cache = new ArrayPoolBackedLruCache<int, int>(3);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);
		cache.AddOrSet(3, 30);

		Assert.IsTrue(cache.TryGet(2, out _));

		cache.AddOrSet(4, 40);

		Assert.IsFalse(cache.TryGet(1, out _));

		cache.AddOrSet(5, 50);

		Assert.IsFalse(cache.TryGet(3, out _));
		Assert.IsTrue(cache.TryGet(2, out var v));
		Assert.AreEqual(20, v);
		Assert.IsTrue(cache.TryGet(4, out v));
		Assert.AreEqual(40, v);
		Assert.IsTrue(cache.TryGet(5, out v));
		Assert.AreEqual(50, v);
	}

	[Test]
	public void ShouldHandleRepeatedGetsOfMostRecentKey() {
		using var cache = new ArrayPoolBackedLruCache<int, int>(3);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);
		cache.AddOrSet(3, 30);

		for (var i = 0; i < 3; ++i) {
			Assert.IsTrue(cache.TryGet(3, out var repeatedGetValue));
			Assert.AreEqual(30, repeatedGetValue);
		}

		cache.AddOrSet(4, 40);
		cache.AddOrSet(5, 50);

		Assert.IsFalse(cache.TryGet(1, out _));
		Assert.IsFalse(cache.TryGet(2, out _));
		Assert.IsTrue(cache.TryGet(3, out var v));
		Assert.AreEqual(30, v);
		Assert.IsTrue(cache.TryGet(4, out v));
		Assert.AreEqual(40, v);
		Assert.IsTrue(cache.TryGet(5, out v));
		Assert.AreEqual(50, v);
	}

	[Test]
	public void ShouldWorkWithCapacityOne() {
		using var cache = new ArrayPoolBackedLruCache<int, int>(1);

		cache.AddOrSet(1, 10);
		Assert.IsTrue(cache.TryGet(1, out var v));
		Assert.AreEqual(10, v);

		cache.AddOrSet(2, 20);
		Assert.IsFalse(cache.TryGet(1, out _));
		Assert.IsTrue(cache.TryGet(2, out v));
		Assert.AreEqual(20, v);

		cache.AddOrSet(2, 21);
		Assert.IsTrue(cache.TryGet(2, out v));
		Assert.AreEqual(21, v);

		cache.AddOrSet(3, 30);
		Assert.IsFalse(cache.TryGet(2, out _));
		Assert.IsTrue(cache.TryGet(3, out v));
		Assert.AreEqual(30, v);
	}

	[Test]
	public void ShouldHandleDefaultKeysAndValues() {
		using var capacityOneCache = new ArrayPoolBackedLruCache<int, int>(1);
		capacityOneCache.AddOrSet(0, 0);
		capacityOneCache.AddOrSet(5, 50);
		Assert.IsFalse(capacityOneCache.TryGet(0, out _));
		Assert.IsTrue(capacityOneCache.TryGet(5, out var v));
		Assert.AreEqual(50, v);

		using var cache = new ArrayPoolBackedLruCache<int, int>(2);
		cache.AddOrSet(0, 0);
		cache.AddOrSet(1, 10);
		Assert.IsTrue(cache.TryGet(0, out v));
		Assert.AreEqual(0, v);

		cache.AddOrSet(2, 20);
		Assert.IsFalse(cache.TryGet(1, out _));
		Assert.IsTrue(cache.TryGet(0, out v));
		Assert.AreEqual(0, v);
		Assert.IsTrue(cache.TryGet(2, out v));
		Assert.AreEqual(20, v);
	}

	[Test]
	public void ShouldMaintainRecencyOrderWhenPromotingDuringFill() {
		using var cache = new ArrayPoolBackedLruCache<int, int>(3);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);
		Assert.IsTrue(cache.TryGet(1, out _));
		cache.AddOrSet(3, 30);

		cache.AddOrSet(4, 40);

		Assert.IsFalse(cache.TryGet(2, out _));
		Assert.IsTrue(cache.TryGet(1, out var v));
		Assert.AreEqual(10, v);
		Assert.IsTrue(cache.TryGet(3, out v));
		Assert.AreEqual(30, v);
		Assert.IsTrue(cache.TryGet(4, out v));
		Assert.AreEqual(40, v);
	}

	static void RecordEviction(object? arg, int key, int value) => ((List<(int Key, int Value)>) arg!).Add((key, value));

	[Test]
	public unsafe void ShouldInvokeEvictionCallbackWhenEvicting() {
		var records = new List<(int Key, int Value)>();
		var cache = new ArrayPoolBackedLruCache<int, int>(3, &RecordEviction, records);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);
		cache.AddOrSet(3, 30);
		Assert.AreEqual(0, records.Count);

		cache.AddOrSet(1, 11);
		Assert.AreEqual(0, records.Count);

		Assert.IsTrue(cache.TryGet(2, out _));

		cache.AddOrSet(4, 40);
		Assert.AreEqual(1, records.Count);
		Assert.AreEqual((3, 30), records[0]);

		cache.AddOrSet(5, 50);
		Assert.AreEqual(2, records.Count);
		Assert.AreEqual((1, 11), records[1]);

		cache.Dispose(invokeCacheEvictionCallbackOnAllContainedValues: false);
		Assert.AreEqual(2, records.Count);
	}

	[Test]
	public unsafe void ShouldReturnPreviousValueFromAddOrSet() {
		var records = new List<(int Key, int Value)>();
		var cache = new ArrayPoolBackedLruCache<int, int>(2, &RecordEviction, records);

		Assert.IsFalse(cache.AddOrSet(1, 10, out var previousValue));
		Assert.AreEqual(default(int), previousValue);

		Assert.IsTrue(cache.AddOrSet(1, 11, out previousValue));
		Assert.AreEqual(10, previousValue);
		Assert.AreEqual(0, records.Count);

		Assert.IsTrue(cache.TryGet(1, out var v));
		Assert.AreEqual(11, v);

		cache.AddOrSet(2, 20);
		Assert.IsFalse(cache.AddOrSet(3, 30, out previousValue));
		Assert.AreEqual(default(int), previousValue);
		Assert.AreEqual(1, records.Count);
		Assert.AreEqual((1, 11), records[0]);

		Assert.IsFalse(cache.TryGet(1, out _));
		Assert.IsTrue(cache.TryGet(2, out v));
		Assert.AreEqual(20, v);
		Assert.IsTrue(cache.TryGet(3, out v));
		Assert.AreEqual(30, v);

		cache.Dispose(invokeCacheEvictionCallbackOnAllContainedValues: false);
	}

	[Test]
	public void ShouldPromoteKeyWhenSettingViaAddOrSetWithPreviousValue() {
		using var cache = new ArrayPoolBackedLruCache<int, int>(3);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);
		cache.AddOrSet(3, 30);

		Assert.IsTrue(cache.AddOrSet(1, 11, out _));

		cache.AddOrSet(4, 40);

		Assert.IsFalse(cache.TryGet(2, out _));
		Assert.IsTrue(cache.TryGet(1, out var v));
		Assert.AreEqual(11, v);
		Assert.IsTrue(cache.TryGet(3, out v));
		Assert.AreEqual(30, v);
		Assert.IsTrue(cache.TryGet(4, out v));
		Assert.AreEqual(40, v);
	}

	[Test]
	public unsafe void ShouldInvokeEvictionCallbackOnClearWhenRequested() {
		var records = new List<(int Key, int Value)>();
		var cache = new ArrayPoolBackedLruCache<int, int>(3, &RecordEviction, records);

		cache.Clear(invokeCacheEvictionCallbackOnAllContainedValues: true);
		Assert.AreEqual(0, records.Count);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);
		cache.AddOrSet(3, 30);

		cache.Clear(invokeCacheEvictionCallbackOnAllContainedValues: true);
		Assert.AreEqual(3, records.Count);
		CollectionAssert.AreEquivalent(new[] { (1, 10), (2, 20), (3, 30) }, records);
		Assert.IsFalse(cache.TryGet(1, out _));
		Assert.IsFalse(cache.TryGet(2, out _));
		Assert.IsFalse(cache.TryGet(3, out _));

		cache.Dispose(invokeCacheEvictionCallbackOnAllContainedValues: false);
	}

	[Test]
	public unsafe void ShouldNotInvokeEvictionCallbackOnClearWhenNotRequested() {
		var records = new List<(int Key, int Value)>();
		var cache = new ArrayPoolBackedLruCache<int, int>(3, &RecordEviction, records);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);

		cache.Clear(invokeCacheEvictionCallbackOnAllContainedValues: false);
		Assert.AreEqual(0, records.Count);
		Assert.IsFalse(cache.TryGet(1, out _));
		Assert.IsFalse(cache.TryGet(2, out _));

		cache.Dispose(invokeCacheEvictionCallbackOnAllContainedValues: false);
	}

	[Test]
	public unsafe void ShouldBeReusableAfterClear() {
		var records = new List<(int Key, int Value)>();
		var cache = new ArrayPoolBackedLruCache<int, int>(3, &RecordEviction, records);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);
		cache.AddOrSet(3, 30);
		cache.Clear(invokeCacheEvictionCallbackOnAllContainedValues: false);

		cache.AddOrSet(4, 40);
		cache.AddOrSet(5, 50);
		cache.AddOrSet(6, 60);
		Assert.AreEqual(0, records.Count);

		Assert.IsTrue(cache.TryGet(4, out var v));
		Assert.AreEqual(40, v);

		cache.AddOrSet(7, 70);
		Assert.AreEqual(1, records.Count);
		Assert.AreEqual((5, 50), records[0]);

		Assert.IsFalse(cache.TryGet(5, out _));
		Assert.IsTrue(cache.TryGet(4, out v));
		Assert.AreEqual(40, v);
		Assert.IsTrue(cache.TryGet(6, out v));
		Assert.AreEqual(60, v);
		Assert.IsTrue(cache.TryGet(7, out v));
		Assert.AreEqual(70, v);

		cache.Dispose(invokeCacheEvictionCallbackOnAllContainedValues: false);
	}

	[Test]
	public unsafe void ShouldInvokeEvictionCallbackOnDisposeWhenRequested() {
		var records = new List<(int Key, int Value)>();
		var cache = new ArrayPoolBackedLruCache<int, int>(3, &RecordEviction, records);

		cache.AddOrSet(1, 10);
		cache.AddOrSet(2, 20);

		cache.Dispose(invokeCacheEvictionCallbackOnAllContainedValues: true);
		Assert.AreEqual(2, records.Count);
		CollectionAssert.AreEquivalent(new[] { (1, 10), (2, 20) }, records);
	}

	[Test]
	public unsafe void ShouldInvokeEvictionCallbackOnIDisposableDispose() {
		var records = new List<(int Key, int Value)>();
		using (var cache = new ArrayPoolBackedLruCache<int, int>(3, &RecordEviction, records)) {
			cache.AddOrSet(1, 10);
			cache.AddOrSet(2, 20);
		}

		Assert.AreEqual(2, records.Count);
		CollectionAssert.AreEquivalent(new[] { (1, 10), (2, 20) }, records);
	}

	[Test]
	public void ShouldMatchReferenceImplementation() {
		const int Capacity = 8;
		const int KeyRange = 20;
		const int NumOperations = 10_000;

		using var cache = new ArrayPoolBackedLruCache<int, int>(Capacity);
		var reference = new List<KeyValuePair<int, int>>();
		var rng = new Random(12345);

		for (var opIndex = 0; opIndex < NumOperations; ++opIndex) {
			var key = rng.Next(KeyRange);
			if (rng.Next(2) == 0) {
				var val = rng.Next(1000);
				cache.AddOrSet(key, val);
				var existingIndex = reference.FindIndex(kvp => kvp.Key == key);
				if (existingIndex >= 0) reference.RemoveAt(existingIndex);
				else if (reference.Count == Capacity) reference.RemoveAt(0);
				reference.Add(new(key, val));
			}
			else {
				var expectedIndex = reference.FindIndex(kvp => kvp.Key == key);
				var wasPresent = cache.TryGet(key, out var actualValue);
				if (expectedIndex < 0) {
					Assert.IsFalse(wasPresent);
				}
				else {
					Assert.IsTrue(wasPresent);
					var expectedPair = reference[expectedIndex];
					Assert.AreEqual(expectedPair.Value, actualValue);
					reference.RemoveAt(expectedIndex);
					reference.Add(expectedPair);
				}
			}
		}

		foreach (var kvp in reference) {
			Assert.IsTrue(cache.TryGet(kvp.Key, out var v));
			Assert.AreEqual(kvp.Value, v);
		}
	}
}
