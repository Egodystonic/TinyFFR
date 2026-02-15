// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class MeshVertexSkeletalTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public unsafe void ShouldCorrectlyLayOutStruct() {
		AssertStructLayout<MeshVertexSkeletal>(MeshVertexSkeletal.ExpectedSerializedSize);
		var valSpan = stackalloc MeshVertexSkeletal[] {
			new((1f, 2f, 3f), (4f, 5f), new(0.1f, 0.2f, 0.3f, 0.4f), MeshVertexSkeletal.BoneIndexArray.Create(12, 34, 56, 78), MeshVertexSkeletal.BoneWeightArray.Create(1.2f, 3.4f, 5.6f, 7.8f)), 
			new((6f, 7f, 8f), (9f, 10f), new(0.5f, 0.6f, 0.7f, 0.8f), MeshVertexSkeletal.BoneIndexArray.Create(87, 65, 43, 21), MeshVertexSkeletal.BoneWeightArray.Create(8.7f, 6.5f, 4.3f, 2.1f))
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
		Assert.AreEqual(BinaryPrimitives.ReadSingleBigEndian([12, 34, 56, 78]), valPtr[9]);
		Assert.AreEqual(1.2f, valPtr[10]);
		Assert.AreEqual(3.4f, valPtr[11]);
		Assert.AreEqual(5.6f, valPtr[12]);
		Assert.AreEqual(7.8f, valPtr[13]);

		Assert.AreEqual(6f, valPtr[14]);
		Assert.AreEqual(7f, valPtr[15]);
		Assert.AreEqual(8f, valPtr[16]);
		Assert.AreEqual(9f, valPtr[17]);
		Assert.AreEqual(10f, valPtr[18]);
		Assert.AreEqual(0.5f, valPtr[19]);
		Assert.AreEqual(0.6f, valPtr[20]);
		Assert.AreEqual(0.7f, valPtr[21]);
		Assert.AreEqual(0.8f, valPtr[22]);
		Assert.AreEqual(BinaryPrimitives.ReadSingleBigEndian([87, 65, 32, 21]), valPtr[23]);
		Assert.AreEqual(8.7f, valPtr[24]);
		Assert.AreEqual(6.5f, valPtr[25]);
		Assert.AreEqual(4.3f, valPtr[26]);
		Assert.AreEqual(2.1f, valPtr[27]);
	}
}