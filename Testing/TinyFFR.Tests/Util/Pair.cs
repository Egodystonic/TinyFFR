namespace Egodystonic.TinyFFR;

[TestFixture]
class PairTest {
	[Test]
	public void ToStringShouldPrintOnlyFirstAndSecond() {
		Assert.AreEqual("Pair { First = 1, Second = abc }", new Pair<int, string>(1, "abc").ToString());
		Assert.AreEqual("Pair { First = 3, Second = 3 }", new Pair<int, int>(3, 3).ToString());
	}
}
