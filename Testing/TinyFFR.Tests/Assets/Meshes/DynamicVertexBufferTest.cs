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
		var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(16, 16);

		var vertexLease = buffer.BorrowVerticesSpan(false, false);
		Assert.Throws<ResourceDependencyException>(() => buffer.Dispose());
		vertexLease.Dispose();

		var triangleLease = buffer.BorrowTrianglesSpanReadOnly();
		Assert.Throws<ResourceDependencyException>(() => buffer.Dispose());
		triangleLease.Dispose();

		var resizeLease = buffer.BorrowTrianglesSpan(false, false);
		Assert.Throws<ResourceDependencyException>(() => buffer.ResizeTriangleBuffer(32));
		resizeLease.Dispose();

		buffer.Dispose();
		Assert.Throws<ObjectDisposedException>(() => _ = buffer.VertexBufferSize);
	}

	[Test]
	public void ShouldNotBeResizableOrDisposableWhileMeshesExist() {
		using var factory = new LocalTinyFfrFactory();
		var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(16, 16);
		var mesh = buffer.CreateMesh();

		Assert.Throws<ResourceDependencyException>(() => buffer.ResizeVertexBuffer(32));
		Assert.Throws<ResourceDependencyException>(() => buffer.ResizeTriangleBuffer(32));
		Assert.Throws<ResourceDependencyException>(() => buffer.Dispose());

		mesh.Dispose();
		buffer.ResizeVertexBuffer(32);
		buffer.ResizeTriangleBuffer(32);
		Assert.AreEqual(32, buffer.VertexBufferSize);
		Assert.AreEqual(32, buffer.TriangleBufferSize);
		buffer.Dispose();
	}

	[Test]
	public void ShouldPreserveTrianglesWhenResizing() {
		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(8, 4);
		using (var lease = buffer.BorrowTrianglesSpan(false, false)) {
			for (var i = 0; i < lease.Span.Length; ++i) lease.Span[i] = new VertexTriangle(i, i + 1, i + 2);
		}

		buffer.ResizeTriangleBuffer(6);
		using (var lease = buffer.BorrowTrianglesSpanReadOnly()) {
			Assert.AreEqual(6, lease.Span.Length);
			for (var i = 0; i < 4; ++i) Assert.AreEqual(new VertexTriangle(i, i + 1, i + 2), lease.Span[i]);
			Assert.AreEqual(default(VertexTriangle), lease.Span[4]);
			Assert.AreEqual(default(VertexTriangle), lease.Span[5]);
		}

		buffer.ResizeTriangleBuffer(2);
		using (var lease = buffer.BorrowTrianglesSpanReadOnly()) {
			Assert.AreEqual(2, lease.Span.Length);
			for (var i = 0; i < 2; ++i) Assert.AreEqual(new VertexTriangle(i, i + 1, i + 2), lease.Span[i]);
		}
	}

	[Test]
	public void ShouldSupportVerticesBeyondSixteenBitIndexRange() {
		const int VertexCount = 100_000;
		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(VertexCount, 2);
		using (var lease = buffer.BorrowVerticesSpan(false, false)) {
			lease.Span[99_997] = new MeshVertex(new Location(0f, 0f, 0f), XYPair<float>.Zero, Direction.Right, Direction.Up, Direction.Backward);
			lease.Span[99_998] = new MeshVertex(new Location(1f, 0f, 0f), XYPair<float>.Zero, Direction.Right, Direction.Up, Direction.Backward);
			lease.Span[99_999] = new MeshVertex(new Location(0f, 1f, 0f), XYPair<float>.Zero, Direction.Right, Direction.Up, Direction.Backward);
		}
		using (var lease = buffer.BorrowTrianglesSpan(false, false, 1..2)) {
			lease.Span[0] = new VertexTriangle(99_997, 99_998, 99_999);
		}
		using (var lease = buffer.BorrowTrianglesSpanReadOnly()) {
			Assert.AreEqual(new VertexTriangle(99_997, 99_998, 99_999), lease.Span[1]);
		}

		using var mesh = buffer.CreateMesh(1..2);
		Assert.AreEqual(2, buffer.TriangleBufferSize);
		Assert.Throws<ArgumentOutOfRangeException>(() => buffer.CreateMesh(1..3));
	}
}
