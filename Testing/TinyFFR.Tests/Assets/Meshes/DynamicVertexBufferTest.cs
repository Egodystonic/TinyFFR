// Created on 2026-10-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class DynamicVertexBufferTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldNotBeDisposableWhileAnySpanIsBorrowed() {
		using var factory = new LocalTinyFfrFactory();
		var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(16, 48);

		var vertexLease = buffer.BorrowVerticesSpan(false, false);
		Assert.Throws<ResourceDependencyException>(() => buffer.Dispose());
		vertexLease.Dispose();

		var indexLease = buffer.BorrowIndicesSpanReadOnly();
		Assert.Throws<ResourceDependencyException>(() => buffer.Dispose());
		indexLease.Dispose();

		var resizeLease = buffer.BorrowIndicesSpan(false, false);
		Assert.Throws<ResourceDependencyException>(() => buffer.ResizeIndexBuffer(96));
		resizeLease.Dispose();

		buffer.Dispose();
		Assert.Throws<ObjectDisposedException>(() => _ = buffer.VertexBufferSize);
	}

	[Test]
	public void ShouldNotBeResizableOrDisposableWhileMeshesExist() {
		using var factory = new LocalTinyFfrFactory();
		var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(16, 48);
		var mesh = buffer.CreateMesh();

		Assert.Throws<ResourceDependencyException>(() => buffer.ResizeVertexBuffer(32));
		Assert.Throws<ResourceDependencyException>(() => buffer.ResizeIndexBuffer(96));
		Assert.Throws<ResourceDependencyException>(() => buffer.Dispose());

		mesh.Dispose();
		buffer.ResizeVertexBuffer(32);
		buffer.ResizeIndexBuffer(96);
		Assert.AreEqual(32, buffer.VertexBufferSize);
		Assert.AreEqual(96, buffer.IndexBufferSize);
		buffer.Dispose();
	}
}
