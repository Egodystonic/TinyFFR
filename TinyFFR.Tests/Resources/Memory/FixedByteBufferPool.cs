// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
class FixedByteBufferPoolTest {
    [SetUp]
    public void SetUpTest() { }

    [TearDown]
    public void TearDownTest() { }

	[Test]
	public void MaxBufferSizeShouldAlwaysBeAtLeastWhatWasRequested() {
		for (var i = 4; i < 120; ++i) {
			using var pool = new FixedByteBufferPool(i);
			Assert.GreaterOrEqual(pool.MaxBufferSizeBytes, i);
			Assert.GreaterOrEqual(pool.GetMaxBufferSize<uint>(), i / 4);
		}
	}

	[Test]
	public void ShouldAlwaysBePossibleToRentUpToMaximumBufferSize() {
		using var pool = new FixedByteBufferPool(100);
		var maxBufferSize = pool.MaxBufferSizeBytes;

		var rentedBuffers = new List<FixedByteBufferPool.FixedByteBuffer>();
		for (var i = 0; i < 100; ++i) {
			rentedBuffers.Add(pool.Rent(maxBufferSize));
		}

		foreach (var buf in rentedBuffers) {
			pool.Return(buf);
		}

		for (var i = 0; i < 10_000; ++i) {
			rentedBuffers.Add(pool.Rent(Random.Shared.Next(maxBufferSize) + 1));
		}
	}
}