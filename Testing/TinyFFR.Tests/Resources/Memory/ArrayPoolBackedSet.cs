// Created on 2026-06-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
class ArrayPoolBackedSetTest {
	const int NumValues = 1000;
	ArrayPoolBackedSet<int> _set = null!;

	[SetUp]
	public void SetUpTest() {
		_set = new();
		for (var i = 0; i < NumValues; ++i) _set.Add(i);
	}

	[TearDown]
	public void TearDownTest() {
		_set.Dispose();
	}

	[Test]
	public void ShouldGrowBucketCountToKeepLookupsConstantTime() {
		const int GrowthTestValueCount = 20_000;

		using var set = new ArrayPoolBackedSet<int>();
		var initialBucketCount = set.NumBuckets;

		for (var i = 0; i < GrowthTestValueCount; ++i) {
			Assert.AreEqual(true, set.Add(i));
			Assert.LessOrEqual(set.Count, set.NumBuckets * 4);
		}

		Assert.Greater(set.NumBuckets, initialBucketCount);
		Assert.AreEqual(GrowthTestValueCount, set.Count);

		for (var i = 0; i < GrowthTestValueCount; ++i) {
			Assert.AreEqual(true, set.Contains(i));
			Assert.AreEqual(false, set.Add(i));
		}

		var enumeratedLedger = new bool[GrowthTestValueCount];
		var enumeratedCount = 0;
		foreach (var item in set) {
			Assert.AreEqual(false, enumeratedLedger[item]);
			enumeratedLedger[item] = true;
			++enumeratedCount;
		}
		Assert.AreEqual(GrowthTestValueCount, enumeratedCount);
		Assert.IsTrue(enumeratedLedger.All(v => v));

		var indexedLedger = new bool[GrowthTestValueCount];
		for (var i = 0; i < GrowthTestValueCount; ++i) {
			var item = set.GetItemAtIndex(i);
			Assert.AreEqual(false, indexedLedger[item]);
			indexedLedger[item] = true;
		}
		Assert.IsTrue(indexedLedger.All(v => v));

		// Non-sequential indexed access must not be confused by the sequential-walk memo
		Assert.AreEqual(set.GetItemAtIndex(0), set.GetItemAtIndex(0));
		Assert.AreEqual(set.GetItemAtIndex(GrowthTestValueCount - 1), set.GetItemAtIndex(GrowthTestValueCount - 1));
		Assert.AreEqual(set.GetItemAtIndex(7), set.GetItemAtIndex(7));
		Assert.Throws<ArgumentOutOfRangeException>(() => set.GetItemAtIndex(GrowthTestValueCount));
		Assert.Throws<ArgumentOutOfRangeException>(() => set.GetItemAtIndex(-1));

		for (var i = 0; i < GrowthTestValueCount; i += 2) Assert.AreEqual(true, set.Remove(i));
		Assert.AreEqual(GrowthTestValueCount / 2, set.Count);
		for (var i = 1; i < GrowthTestValueCount; i += 2) Assert.AreEqual(true, set.Contains(i));
		for (var i = 0; i < GrowthTestValueCount; i += 2) Assert.AreEqual(false, set.Contains(i));
	}

	[Test]
	public void ShouldMaintainCountAcrossIntersectWith() {
		using var set = new ArrayPoolBackedSet<int>();
		for (var i = 0; i < 5_000; ++i) set.Add(i);

		set.IntersectWith(Enumerable.Range(2_500, 5_000).ToArray());

		Assert.AreEqual(2_500, set.Count);
		var enumeratedCount = 0;
		foreach (var _ in set) ++enumeratedCount;
		Assert.AreEqual(2_500, enumeratedCount);
		Assert.AreEqual(true, set.SetEquals(Enumerable.Range(2_500, 2_500).ToArray()));
	}

	[Test]
	public void ShouldCorrectlyEnumerate() {
		var items = new List<int>();

		foreach (var item in _set) {
			items.Add(item);
		}

		Assert.AreEqual(NumValues, items.Count);

		var itemLedger = new bool[NumValues];

		for (var i = 0; i < NumValues; ++i) {
			itemLedger[items[i]] = true;
		}

		Assert.IsTrue(itemLedger.All(v => v));

		itemLedger = new bool[NumValues];

		for (var i = 0; i < NumValues; ++i) {
			var item = _set.GetItemAtIndex(i);
			itemLedger[item] = true;
		}

		Assert.IsTrue(itemLedger.All(v => v));
	}

	[Test]
	public void ShouldCorrectlySetCount() {
		Assert.AreEqual(NumValues, _set.Count);
		_set.Remove(1);
		Assert.AreEqual(NumValues - 1, _set.Count);
		_set.Remove(1);
		Assert.AreEqual(NumValues - 1, _set.Count);
		_set.Remove(-1);
		Assert.AreEqual(NumValues - 1, _set.Count);
		_set.Remove(2);
		Assert.AreEqual(NumValues - 2, _set.Count);
		_set.Add(NumValues);
		Assert.AreEqual(NumValues - 1, _set.Count);
	}

	[Test]
	public void ShouldCorrectlyAddItems() {
		Assert.IsTrue(_set.Add(NumValues));
		Assert.IsTrue(_set.Contains(NumValues));
		Assert.IsFalse(_set.Add(NumValues));
		Assert.IsTrue(_set.Add(NumValues + 1));
		Assert.IsTrue(_set.Contains(NumValues + 1));
	}

	[Test]
	public void ShouldCorrectlyClear() {
		_set.Clear();
		Assert.AreEqual(0, _set.Count);
		var result = 0;
		foreach (var item in _set) result += 1;
		Assert.AreEqual(0, result);

		_set.Add(1);
		_set.Add(2);
		_set.Add(3);
		_set.ClearWithoutZeroingMemory();
		Assert.AreEqual(0, _set.Count);
		foreach (var _ in _set) result += 1;
		Assert.AreEqual(0, result);
	}

	[Test]
	public void ShouldCorrectlyRemove() {
		void AssertItemMissing(int item) {
			Assert.IsFalse(_set.Contains(item));
			Assert.IsTrue(_set.All(i => i != item));
		}

		Assert.IsTrue(_set.Remove(3));
		Assert.AreEqual(NumValues - 1, _set.Count);
		AssertItemMissing(3);
		Assert.IsFalse(_set.Remove(3));
		Assert.AreEqual(NumValues - 1, _set.Count);
		Assert.IsTrue(_set.Remove(4));
		Assert.AreEqual(NumValues - 2, _set.Count);
		AssertItemMissing(4);
		Assert.IsFalse(_set.Remove(4));
		Assert.IsTrue(_set.Contains(5));
	}

	[Test]
	public void ShouldCorrectlyCopy() {
		var tooSmall = new int[NumValues - 1];
		var bigEnough = new int[NumValues + 1];

		Assert.Catch(() => _set.CopyTo(tooSmall, 0));
		Assert.Catch(() => _set.CopyTo(bigEnough, 2));
		Assert.Catch(() => _set.CopyTo(bigEnough, -1));
		_set.CopyTo(bigEnough, 1);
		Assert.AreEqual(default(int), bigEnough[0]);

		var itemLedger = new bool[NumValues];

		for (var i = 0; i < NumValues; ++i) {
			itemLedger[bigEnough[i + 1]] = true;
		}
		Assert.IsTrue(itemLedger.All(v => v));
	}

	[Test]
	public void ShouldCorrectlyDetermineContains() {
		Assert.AreEqual(true, _set.Contains(0));
		Assert.AreEqual(true, _set.Contains(NumValues - 1));
		Assert.AreEqual(false, _set.Contains(-1));
		Assert.AreEqual(false, _set.Contains(NumValues));
	}

	[Test]
	public void ShouldCorrectlyUnion() {
		_set.UnionWith(new[] { 0, 1, NumValues, NumValues + 1 });
		Assert.AreEqual(NumValues + 2, _set.Count);
		Assert.IsTrue(_set.Contains(NumValues));
		Assert.IsTrue(_set.Contains(NumValues + 1));
	}

	[Test]
	public void ShouldCorrectlyIntersect() {
		_set.IntersectWith(new[] { 1, 2, 3, NumValues + 100 });
		Assert.AreEqual(3, _set.Count);
		Assert.IsTrue(_set.Contains(1));
		Assert.IsTrue(_set.Contains(2));
		Assert.IsTrue(_set.Contains(3));
		Assert.IsFalse(_set.Contains(0));
		Assert.IsFalse(_set.Contains(NumValues + 100));
	}

	[Test]
	public void ShouldCorrectlyExcept() {
		_set.ExceptWith(new[] { 0, 1, 2, NumValues });
		Assert.AreEqual(NumValues - 3, _set.Count);
		Assert.IsFalse(_set.Contains(0));
		Assert.IsFalse(_set.Contains(1));
		Assert.IsFalse(_set.Contains(2));
		Assert.IsTrue(_set.Contains(3));
	}

	[Test]
	public void ShouldCorrectlySymmetricExcept() {
		_set.SymmetricExceptWith(new[] { 0, NumValues });
		Assert.AreEqual(NumValues, _set.Count);
		Assert.IsFalse(_set.Contains(0));
		Assert.IsTrue(_set.Contains(NumValues));
		Assert.IsTrue(_set.Contains(1));
	}

	[Test]
	public void ShouldCorrectlyDetermineSubsetAndSuperset() {
		Assert.IsTrue(_set.IsSubsetOf(Enumerable.Range(0, NumValues)));
		Assert.IsTrue(_set.IsSubsetOf(Enumerable.Range(0, NumValues + 10)));
		Assert.IsFalse(_set.IsSubsetOf(Enumerable.Range(1, NumValues)));

		Assert.IsFalse(_set.IsProperSubsetOf(Enumerable.Range(0, NumValues)));
		Assert.IsTrue(_set.IsProperSubsetOf(Enumerable.Range(0, NumValues + 10)));

		Assert.IsTrue(_set.IsSupersetOf(new[] { 1, 2, 3 }));
		Assert.IsFalse(_set.IsSupersetOf(new[] { 1, NumValues }));

		Assert.IsTrue(_set.IsProperSupersetOf(new[] { 1, 2, 3 }));
		Assert.IsFalse(_set.IsProperSupersetOf(Enumerable.Range(0, NumValues)));
	}

	[Test]
	public void ShouldCorrectlyDetermineOverlapsAndSetEquals() {
		Assert.IsTrue(_set.Overlaps(new[] { -5, 3, -1 }));
		Assert.IsFalse(_set.Overlaps(new[] { -5, -1, NumValues }));

		Assert.IsTrue(_set.SetEquals(Enumerable.Range(0, NumValues)));
		Assert.IsFalse(_set.SetEquals(Enumerable.Range(0, NumValues - 1)));
		Assert.IsFalse(_set.SetEquals(Enumerable.Range(1, NumValues)));
	}
}
