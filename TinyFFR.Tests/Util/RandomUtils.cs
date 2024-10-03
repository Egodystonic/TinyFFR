// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

[TestFixture]
class RandomUtilsTest {
	const int NumTestIterations = 100_000;
	const float TestTolerance = Single.Epsilon;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCreateSingleValues() {
		for (var i = 0; i < NumTestIterations; ++i) {
			var val = RandomUtils.NextSingle();
			Assert.GreaterOrEqual(val, 0f);
			Assert.Less(val, 1f + TestTolerance);

			val = RandomUtils.NextSingle(-100f, 100f);
			Assert.GreaterOrEqual(val, -100f);
			Assert.Less(val, 100f + TestTolerance);

			val = RandomUtils.NextSingleInclusive(-100f, 100f);
			Assert.GreaterOrEqual(val, -100f);
			Assert.LessOrEqual(val, 100f + TestTolerance);

			val = RandomUtils.NextSingleZeroToOneInclusive();
			Assert.GreaterOrEqual(val, 0f);
			Assert.LessOrEqual(val, 1f);

			val = RandomUtils.NextSingleNegOneToOneInclusive();
			Assert.GreaterOrEqual(val, -1f);
			Assert.LessOrEqual(val, 1f);
		}
	}

	[Test]
	public void NextSingleInclusiveAlgorithmShouldWorkAsExpected() {
		const double IntegerScalar = 1d / (Int32.MaxValue - 1);
		
		Assert.AreEqual(0f, (float) (0 * IntegerScalar));
		Assert.AreEqual(1f, (float) ((Int32.MaxValue - 1) * IntegerScalar));

		Assert.AreEqual(-1f, (float) (0 * IntegerScalar * 2d - 1d));
		Assert.AreEqual(1f, (float) ((Int32.MaxValue - 1) * IntegerScalar * 2d - 1d));
	}
}