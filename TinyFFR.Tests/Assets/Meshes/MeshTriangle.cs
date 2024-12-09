// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class RandomUtilsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public unsafe void ShouldCorrectlyLayOutStruct() {
		AssertStructLayout<MeshTriangle>();
		var valSpan = stackalloc MeshTriangle[] { new(11, 22, 33), new(44, 55, 66) };
		var valPtr = (int*) valSpan;
		Assert.AreEqual(11, valPtr[0]);
		Assert.AreEqual(22, valPtr[1]);
		Assert.AreEqual(33, valPtr[2]);
		Assert.AreEqual(44, valPtr[3]);
		Assert.AreEqual(55, valPtr[4]);
		Assert.AreEqual(66, valPtr[5]);
	}
}