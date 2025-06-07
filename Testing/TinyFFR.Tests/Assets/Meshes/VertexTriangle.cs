// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class VertexTriangleTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public unsafe void ShouldCorrectlyLayOutStruct() {
		AssertStructLayout<VertexTriangle>(12);
		var valSpan = stackalloc VertexTriangle[] { new(11, 22, 33), new(44, 55, 66) };
		var valPtr = (int*) valSpan;
		Assert.AreEqual(11, valPtr[0]);
		Assert.AreEqual(22, valPtr[1]);
		Assert.AreEqual(33, valPtr[2]);
		Assert.AreEqual(44, valPtr[3]);
		Assert.AreEqual(55, valPtr[4]);
		Assert.AreEqual(66, valPtr[5]);
	}

	[Test]
	public void ShouldCorrectlyShiftIndices() {
		Assert.AreEqual(new VertexTriangle(4, 5, 6), new VertexTriangle(1, 2, 3).ShiftedBy(3));
		Assert.AreEqual(new VertexTriangle(-2, -1, 0), new VertexTriangle(1, 2, 3).ShiftedBy(-3));
		Assert.AreEqual(new VertexTriangle(1, 2, 3), new VertexTriangle(1, 2, 3).ShiftedBy(0));
	}
}