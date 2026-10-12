// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
class PooledCapacityNormalizationTest {
	const int GrownLength = 64;

	readonly record struct ConstantHashKey(int Value) {
		public override int GetHashCode() => 0;
	}

	[SetUp]
	public void SetUpTest() => TinyFfrArrayPool.ReleaseAllPooledMemory();

	[TearDown]
	public void TearDownTest() => TinyFfrArrayPool.ReleaseAllPooledMemory();

	[Test]
	public void ShouldReturnGrownVectorBackingArrayToArrayPoolWhenVectorIsReturnedToItsPool() {
		var pool = new VectorPool<int>(zeroMemoryOnReturn: true);
		var vector = pool.Rent();
		for (var i = 0; i < GrownLength; ++i) vector.Add(i);
		pool.Return(vector);

		AssertArrayPoolCanRentWithoutAllocating<int>();
		pool.Dispose();
	}

	[Test]
	public void ShouldReturnGrownBucketArraysToArrayPoolWhenSetIsShrunk() {
		var set = new ArrayPoolBackedSet<ConstantHashKey>();
		for (var i = 0; i < GrownLength; ++i) set.Add(new ConstantHashKey(i));
		set.Clear();
		set.ShrinkToInitialBucketCount();

		AssertArrayPoolCanRentWithoutAllocating<ConstantHashKey>();
		set.Dispose();
	}

	[Test]
	public void ShouldReturnGrownBucketArraysToArrayPoolWhenMapIsShrunk() {
		var map = new ArrayPoolBackedMap<ConstantHashKey, int>();
		for (var i = 0; i < GrownLength; ++i) map.Add(new ConstantHashKey(i), i);
		map.Clear();
		map.ShrinkToInitialBucketCount();

		AssertArrayPoolCanRentWithoutAllocating<KeyValuePair<ConstantHashKey, int>>();
		map.Dispose();
	}

	static void AssertArrayPoolCanRentWithoutAllocating<T>() {
		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		var array = TinyFfrArrayPool<T>.Shared.Rent(GrownLength);
		var allocatedDuring = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
		TinyFfrArrayPool<T>.Shared.Return(array);

		Assert.AreEqual(0L, allocatedDuring, $"A {GrownLength}-element {typeof(T).Name} array had to be freshly allocated, so the grown backing array was kept by the pooled collection instead of being returned.");
	}
}
