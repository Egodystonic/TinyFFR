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

		for (var i = 0; i < 100; ++i) {
			rentedBuffers.Add(pool.Rent(maxBufferSize));
		}
	}

	[Test]
	public void ShouldNeverLeakOrCorruptMemory() {
		const int NumIterations = 20_000;

		using var pool = new FixedByteBufferPool(100);
		var maxBufferSize = pool.MaxBufferSizeBytes;
		var rentedBufferArray = new FixedByteBufferPool.FixedByteBuffer[Byte.MaxValue];
		for (var i = 0; i < rentedBufferArray.Length; ++i) {
			rentedBufferArray[i] = pool.Rent(Random.Shared.Next(maxBufferSize) + 1);
			rentedBufferArray[i].AsByteSpan.Fill((byte) i);
		}

		for (var i = 0; i < NumIterations; ++i) {
			var indexToReplace = Random.Shared.Next(rentedBufferArray.Length);
			var curBuf = rentedBufferArray[indexToReplace];
			var curSpan = curBuf.AsReadOnlyByteSpan;
			for (var b = 0; b < curSpan.Length; ++b) Assert.AreEqual(indexToReplace, curSpan[b]);
			pool.Return(curBuf);
			rentedBufferArray[indexToReplace] = pool.Rent(Random.Shared.Next(maxBufferSize) + 1);
			rentedBufferArray[indexToReplace].AsByteSpan.Fill((byte) indexToReplace);
		}

		for (var i = 0; i < rentedBufferArray.Length; ++i) {
			for (var b = 0; b < rentedBufferArray[i].AsReadOnlyByteSpan.Length; ++b) Assert.AreEqual(i, rentedBufferArray[i].AsReadOnlyByteSpan[b]);
		}
	}

	[Test]
	public void ShouldRentBuffersOfAppropriateSize() {
		using var pool = new FixedByteBufferPool(100);

		for (var i = 0; i < 10; ++i) {
			Assert.GreaterOrEqual(pool.Rent<byte>(i).SizeBytes, i);
			Assert.GreaterOrEqual(pool.Rent<short>(i).SizeBytes, i * sizeof(short));
			Assert.GreaterOrEqual(pool.Rent<float>(i).SizeBytes, i * sizeof(float));
			Assert.GreaterOrEqual(pool.Rent<double>(i).SizeBytes, i * sizeof(double));
		}
	}
}