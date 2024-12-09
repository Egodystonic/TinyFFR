// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class MeshVertexTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public unsafe void ShouldCorrectlyLayOutStruct() {
		AssertStructLayout<MeshVertex>(20);
		var valSpan = stackalloc MeshVertex[] { new((1f, 2f, 3f), (4f, 5f)), new((6f, 7f, 8f), (9f, 10f)) };
		var valPtr = (float*) valSpan;
		Assert.AreEqual(1f, valPtr[0]);
		Assert.AreEqual(2f, valPtr[1]);
		Assert.AreEqual(3f, valPtr[2]);
		Assert.AreEqual(4f, valPtr[3]);
		Assert.AreEqual(5f, valPtr[4]);
		Assert.AreEqual(6f, valPtr[5]);
		Assert.AreEqual(7f, valPtr[6]);
		Assert.AreEqual(8f, valPtr[7]);
		Assert.AreEqual(9f, valPtr[8]);
		Assert.AreEqual(10f, valPtr[9]);
	}
}