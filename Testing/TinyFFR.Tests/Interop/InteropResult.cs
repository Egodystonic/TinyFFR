// Created on 2024-12-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Interop;

[TestFixture]
class InteropResultTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<InteropResult>(1);

	[Test]
	public void ShouldCorrectlyTranslate() {
		Assert.AreEqual(true, (bool) InteropResult.Success);
		Assert.AreEqual(false, (bool) InteropResult.Failure);
		Assert.AreEqual(false, (bool) default(InteropResult));
	}
}