// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

[TestFixture]
class ManagedStringPoolTest {
	ManagedStringPool _pool = null!;

    [SetUp]
    public void SetUpTest() {
		_pool = new ManagedStringPool();
	}

    [TearDown]
    public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyRent() {
		const int NumIterations = 1000;

		for (var i = 0; i < NumIterations; ++i) {
			var size = Random.Shared.Next(1, 20);
			var handle = _pool.RentAndCopy(new String('a', size));
			Assert.AreEqual(size, handle.Length);
			Assert.AreEqual(size, handle.AsSpan.Length);
			Assert.LessOrEqual(size, handle.BorrowedArray.Length);
			Assert.AreEqual(new String('a', size), handle.AsNewStringObject);
			Assert.IsTrue(new String('a', size).AsSpan().SequenceEqual(handle.AsSpan));
			_pool.Return(handle);
		}
	}
}