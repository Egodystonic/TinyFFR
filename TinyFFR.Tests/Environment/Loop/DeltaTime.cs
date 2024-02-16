// Created on 2024-02-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Loop;

[TestFixture]
class DeltaTimeTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromTimeSpan() {
		Assert.AreEqual(TimeSpan.FromSeconds(1.234f), new DeltaTime(1.234f).ToTimeSpan());
		Assert.AreEqual(new DeltaTime(1.234f).ElapsedSeconds, ((DeltaTime) TimeSpan.FromSeconds(1.234f)).ElapsedSeconds, 0.001f);
	}

	[Test]
	public void ShouldCorrectlyImplementComparisons() {
		var lowerValue = new DeltaTime(1f);
		var higherValue = new DeltaTime(2f);

		Assert.AreEqual(1f.CompareTo(2f), lowerValue.CompareTo(higherValue));
		Assert.AreEqual(1f > 2f, lowerValue > higherValue);
		Assert.AreEqual(1f >= 2f, lowerValue >= higherValue);
		Assert.AreEqual(1f < 2f, lowerValue < higherValue);
		Assert.AreEqual(1f <= 2f, lowerValue <= higherValue);

		Assert.AreEqual(2f.CompareTo(1f), higherValue.CompareTo(lowerValue));
		Assert.AreEqual(2f > 1f, higherValue > lowerValue);
		Assert.AreEqual(2f >= 1f, higherValue >= lowerValue);
		Assert.AreEqual(2f < 1f, higherValue < lowerValue);
		Assert.AreEqual(2f <= 1f, higherValue <= lowerValue);
	}

	[Test]
	public void ShouldCorrectlyImplementArithmetic() {
		var three = new DeltaTime(3f);
		var four = new DeltaTime(4f);

		Assert.AreEqual(new DeltaTime(7f), three + four);
		Assert.AreEqual(new DeltaTime(7f), four + three);

		Assert.AreEqual(new DeltaTime(1f), four - three);
		Assert.AreEqual(new DeltaTime(-1f), three - four);

		Assert.AreEqual(new DeltaTime(6f), three * 2f);
		Assert.AreEqual(new DeltaTime(6f), 2f * three);
		Assert.AreEqual(new DeltaTime(1.5f), three / 2f);
	}
}