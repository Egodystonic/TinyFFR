namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
unsafe class HeapPoolTest {
	HeapPool _pool = null!;

	[SetUp]
	public void SetUpTest() {
		_pool = new HeapPool();
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBorrowRequestedSize() {
		var sizes = new[] { 0, 1, 7, 16, 100, 1024, 4096 };
		foreach (var size in sizes) {
			Assert.AreEqual(size, _pool.Borrow(size).Buffer.Length);
			Assert.AreEqual(size, _pool.Borrow<int>(size).Buffer.Length);
			Assert.AreEqual(size, _pool.Borrow<float>(size).Buffer.Length);
			Assert.AreEqual(size, _pool.Borrow<long>(size).Buffer.Length);
			Assert.AreEqual(size, _pool.Borrow<Vect>(size).Buffer.Length);
		}
		
		Assert.Catch(() => _pool.Borrow(-1));
		Assert.Catch(() => _pool.Borrow<int>(-1));
		Assert.Catch(() => _pool.Borrow<int>(1, -4));
	}

	[Test]
	public void ShouldCorrectlyBorrowWithCustomElementSize() {
		Assert.AreEqual(8, _pool.Borrow<int>(4, 8).Buffer.Length);
		Assert.AreEqual(48, _pool.Borrow<byte>(3, 16).Buffer.Length);
		Assert.AreEqual(6, _pool.Borrow<long>(2, 24).Buffer.Length);
	}

	[Test]
	public void ShouldCorrectlyBorrowAndCopyData() {
		var input = new Vect[] { new(1f, 2f, 3f), new(4f, 5f, 6f), new(7f, 8f, 9f) };
		var output = _pool.BorrowAndCopy(input).Buffer;
		Assert.IsTrue(input.SequenceEqual(output));
	}

	[Test]
	public void ShouldCorrectlyMaintainDataIntegrityAcrossMultipleTypes() {
		using var byteMem = _pool.Borrow<byte>(10);
		using var intMem = _pool.Borrow<int>(10);
		using var floatMem = _pool.Borrow<float>(10);
		using var longMem = _pool.Borrow<long>(10);
		using var vectMem = _pool.Borrow<Vect>(10);

		for (var i = 0; i < 10; ++i) {
			byteMem.Buffer[i] = (byte) i;
			intMem.Buffer[i] = i * 1000;
			floatMem.Buffer[i] = i * 1.5f;
			longMem.Buffer[i] = i * 100_000L;
			vectMem.Buffer[i] = new Vect(i, i * 2f, i * 3f);
		}

		for (var i = 0; i < 10; ++i) {
			Assert.AreEqual((byte) i, byteMem.Buffer[i]);
			Assert.AreEqual(i * 1000, intMem.Buffer[i]);
			Assert.AreEqual(i * 1.5f, floatMem.Buffer[i]);
			Assert.AreEqual(i * 100_000L, longMem.Buffer[i]);
			Assert.AreEqual(new Vect(i, i * 2f, i * 3f), vectMem.Buffer[i]);
		}
	}

	[Test]
	public void ShouldNeverLeakOrCorruptMemory() {
		const int NumIterations = 20_000;

		var rentedBufferArray = new PooledHeapMemory<byte>[Byte.MaxValue];

		for (var i = 0; i < Byte.MaxValue; ++i) {
			rentedBufferArray[i] = _pool.Borrow<byte>(Random.Shared.Next(1, 100));
			rentedBufferArray[i].Buffer.Fill((byte) i);
		}

		for (var i = 0; i < NumIterations; ++i) {
			var indexToReplace = Random.Shared.Next(Byte.MaxValue);
			var curSpan = rentedBufferArray[indexToReplace].Buffer;
			for (var b = 0; b < curSpan.Length; ++b) Assert.AreEqual((byte) indexToReplace, curSpan[b]);
			rentedBufferArray[indexToReplace].Dispose();
			rentedBufferArray[indexToReplace] = _pool.Borrow<byte>(Random.Shared.Next(1, 100));
			rentedBufferArray[indexToReplace].Buffer.Fill((byte) indexToReplace);
		}

		for (var i = 0; i < Byte.MaxValue; ++i) {
			for (var b = 0; b < rentedBufferArray[i].Buffer.Length; ++b) Assert.AreEqual((byte) i, rentedBufferArray[i].Buffer[b]);
			rentedBufferArray[i].Dispose();
		}
	}
}
