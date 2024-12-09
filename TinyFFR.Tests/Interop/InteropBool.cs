// Created on 2024-12-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Interop;

[TestFixture]
class InteropBoolTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<InteropBool>(1);

	[Test]
	public void ShouldCorrectlyTranslate() {
		Assert.AreEqual(true, (bool) InteropBool.True);
		Assert.AreEqual(false, (bool) InteropBool.False);
		Assert.AreEqual(false, (bool) default(InteropBool));
	}
}