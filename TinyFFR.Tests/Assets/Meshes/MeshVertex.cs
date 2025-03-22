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
		AssertStructLayout<MeshVertex>(MeshVertex.ExpectedSerializedSize);
		var valSpan = stackalloc MeshVertex[] {
			new((1f, 2f, 3f), (4f, 5f), new(0.1f, 0.2f, 0.3f, 0.4f)), 
			new((6f, 7f, 8f), (9f, 10f), new(0.5f, 0.6f, 0.7f, 0.8f))
		};
		var valPtr = (float*) valSpan;
		Assert.AreEqual(1f, valPtr[0]);
		Assert.AreEqual(2f, valPtr[1]);
		Assert.AreEqual(3f, valPtr[2]);
		Assert.AreEqual(4f, valPtr[3]);
		Assert.AreEqual(5f, valPtr[4]);
		Assert.AreEqual(0.1f, valPtr[5]);
		Assert.AreEqual(0.2f, valPtr[6]);
		Assert.AreEqual(0.3f, valPtr[7]);
		Assert.AreEqual(0.4f, valPtr[8]);

		Assert.AreEqual(6f, valPtr[9]);
		Assert.AreEqual(7f, valPtr[10]);
		Assert.AreEqual(8f, valPtr[11]);
		Assert.AreEqual(9f, valPtr[12]);
		Assert.AreEqual(10f, valPtr[13]);
		Assert.AreEqual(0.5f, valPtr[14]);
		Assert.AreEqual(0.6f, valPtr[15]);
		Assert.AreEqual(0.7f, valPtr[16]);
		Assert.AreEqual(0.8f, valPtr[17]);
	}
}