// Created on 2026-10-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using System.Numerics;
using Egodystonic.TinyFFR.Threading;

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
	public void ShouldZeroInitializeAndPreserveVerticesWhenResizing() {
		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(8, 4);
		using (var lease = buffer.BorrowVerticesSpanReadOnly()) {
			foreach (var vertex in lease.Span) Assert.AreEqual(default(MeshVertex), vertex);
		}
		using (var lease = buffer.BorrowVerticesSpan(false, false)) {
			for (var i = 0; i < lease.Span.Length; ++i) lease.Span[i] = CreateIndexedVertex(i);
		}

		buffer.ResizeVertexBuffer(12);
		using (var lease = buffer.BorrowVerticesSpanReadOnly()) {
			Assert.AreEqual(12, lease.Span.Length);
			for (var i = 0; i < 8; ++i) Assert.AreEqual(CreateIndexedVertex(i), lease.Span[i]);
			for (var i = 8; i < 12; ++i) Assert.AreEqual(default(MeshVertex), lease.Span[i]);
		}

		buffer.ResizeVertexBuffer(3);
		using (var lease = buffer.BorrowVerticesSpanReadOnly()) {
			Assert.AreEqual(3, lease.Span.Length);
			for (var i = 0; i < 3; ++i) Assert.AreEqual(CreateIndexedVertex(i), lease.Span[i]);
		}
	}

	[Test]
	public void ShouldNotAllocateManagedMemoryForLiveBufferMirrors() {
		const int LiveBufferCount = 16;
		const int VertexCount = 8_192;
		const int TriangleCount = 4_096;
		const long MaxExpectedAllocationBytes = 256 * 1024;
		using var factory = new LocalTinyFfrFactory();
		var buffers = new DynamicVertexBuffer[LiveBufferCount];
		factory.MeshBuilder.CreateDynamicVertexBuffer(VertexCount, TriangleCount).Dispose();

		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < buffers.Length; ++i) buffers[i] = factory.MeshBuilder.CreateDynamicVertexBuffer(VertexCount, TriangleCount);
		var allocatedDuring = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
		for (var i = 0; i < buffers.Length; ++i) buffers[i].Dispose();

		Assert.Less(allocatedDuring, MaxExpectedAllocationBytes, $"Holding {LiveBufferCount} buffers of {VertexCount} vertices allocated {allocatedDuring} managed bytes; the CPU-side mirrors are being allocated on the managed heap.");
	}

	static MeshVertex CreateIndexedVertex(int index) => new(new Location(index, index * 2f, index * 3f), new XYPair<float>(index, -index), Direction.Right, Direction.Up, Direction.Backward);

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

	static void WriteTestVertices(Span<MeshVertex> block, int startIndex, int seed) {
		for (var i = 0; i < block.Length; ++i) {
			var index = startIndex + i;
			block[i] = new MeshVertex(new Location(index, index * 0.5f + seed, -index), new XYPair<float>(index % 7, index % 3), Quaternion.Identity);
		}
	}

	static void WriteTestTriangles(Span<VertexTriangle> block, int startIndex, int vertexCount) {
		for (var i = 0; i < block.Length; ++i) {
			var index = startIndex + i;
			block[i] = new VertexTriangle(index % vertexCount, (index * 7 + 1) % vertexCount, (index * 13 + 2) % vertexCount);
		}
	}

	static void CopyVertexBlock(ReadOnlySpan<MeshVertex> block, int startIndex, MeshVertex[] destination) => block.CopyTo(destination.AsSpan(startIndex));
	static void CopyTriangleBlock(ReadOnlySpan<VertexTriangle> block, int startIndex, VertexTriangle[] destination) => block.CopyTo(destination.AsSpan(startIndex));

	[Test]
	public unsafe void ParallelizedReadsAndWritesShouldMatchSerialLeases() {
		const int VertexCount = 70_001;
		const int TriangleCount = 50_003;
		using var factory = new LocalTinyFfrFactory(factoryConfig: new LocalTinyFfrFactoryConfig { ThreadingConfig = new ThreadingConfig { WorkerThreadCount = 5 } });
		using var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(VertexCount, TriangleCount);

		buffer.WriteVerticesParallelized(&WriteTestVertices, 11, recalculateBoundingBoxOnLeaseDispose: true, overwriteChildMeshBoundingBoxes: false);
		buffer.WriteTrianglesParallelized(&WriteTestTriangles, VertexCount, recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false);

		var expectedVertices = new MeshVertex[VertexCount];
		WriteTestVertices(expectedVertices, 0, 11);
		var expectedTriangles = new VertexTriangle[TriangleCount];
		WriteTestTriangles(expectedTriangles, 0, VertexCount);

		using (var lease = buffer.BorrowVerticesSpanReadOnly()) Assert.IsTrue(expectedVertices.AsSpan().SequenceEqual(lease.Span));
		using (var lease = buffer.BorrowTrianglesSpanReadOnly()) Assert.IsTrue(expectedTriangles.AsSpan().SequenceEqual(lease.Span));

		var readVertices = new MeshVertex[VertexCount];
		buffer.ReadVerticesParallelized(&CopyVertexBlock, readVertices);
		Assert.IsTrue(expectedVertices.AsSpan().SequenceEqual(readVertices));
		var readTriangles = new VertexTriangle[TriangleCount];
		buffer.ReadTrianglesParallelized(&CopyTriangleBlock, readTriangles);
		Assert.IsTrue(expectedTriangles.AsSpan().SequenceEqual(readTriangles));

		buffer.WriteVerticesParallelized(&WriteTestVertices, 99, recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false, 100..2100);
		WriteTestVertices(expectedVertices.AsSpan(100, 2000), 0, 99);
		using (var lease = buffer.BorrowVerticesSpanReadOnly()) Assert.IsTrue(expectedVertices.AsSpan().SequenceEqual(lease.Span));
	}

	static void ThrowOnBlock(Span<MeshVertex> block, int startIndex, int throwIndex) {
		if (throwIndex >= startIndex && throwIndex < startIndex + block.Length) throw new InvalidOperationException("Expected test failure.");
	}

	[Test]
	public unsafe void ParallelizedWriteExceptionsShouldPropagateAndReleaseLeases() {
		using var factory = new LocalTinyFfrFactory(factoryConfig: new LocalTinyFfrFactoryConfig { ThreadingConfig = new ThreadingConfig { WorkerThreadCount = 4 } });
		var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(40_000, 16);
		Assert.Throws<InvalidOperationException>(() => buffer.WriteVerticesParallelized(&ThrowOnBlock, 31_337, false, false));
		buffer.Dispose();
	}
}
