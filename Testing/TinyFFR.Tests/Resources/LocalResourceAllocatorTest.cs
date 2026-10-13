// Created on 2026-10-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Reflection;
using System.Threading;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Resources;

[TestFixture]
class LocalResourceAllocatorTest {
	const int LargeUploadVertexCount = 16_384;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void TrimPooledMemoryShouldReleaseRetainedUploadMemory() {
		using var factory = new LocalTinyFfrFactory();
		var pool = ((ILocalGpuHoldingBufferAllocator) factory).GpuHoldingBufferPool;
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer((64, 64));
		using var scene = factory.SceneBuilder.CreateScene();
		using var camera = factory.CameraBuilder.CreateCamera();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, buffer);

		for (var i = 0; i < 4; ++i) factory.MeshBuilder.CreateDynamicVertexBuffer(LargeUploadVertexCount, 1).Dispose();
		renderer.RenderAndWaitForGpu();
		Assert.AreEqual(0L, pool.CurrentlyLeasedTotalBytes);
		Assert.Greater(CountLiveSlabs(pool), 0);

		factory.ResourceAllocator.TrimPooledMemory();
		Assert.AreEqual(0, CountLiveSlabs(pool));
	}

	[Test]
	public void TrimPooledMemoryShouldDoNothingWhileAsyncOperationsAreOutstanding() {
		using var factory = new LocalTinyFfrFactory();
		var pool = ((ILocalGpuHoldingBufferAllocator) factory).GpuHoldingBufferPool;
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer((64, 64));
		using var scene = factory.SceneBuilder.CreateScene();
		using var camera = factory.CameraBuilder.CreateCamera();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, buffer);

		factory.MeshBuilder.CreateDynamicVertexBuffer(LargeUploadVertexCount, 1).Dispose();
		renderer.RenderAndWaitForGpu();
		var liveSlabsBeforeTrim = CountLiveSlabs(pool);
		Assert.Greater(liveSlabsBeforeTrim, 0);

		using var gate = new ManualResetEventSlim(false);
		var operation = factory.ResourceAllocator.DispatchWorkerThreadJob<ManualResetEventSlim, object>(gate, static g => {
			g!.Wait();
			return new object();
		});
		try {
			Assert.Greater(OutstandingAsyncOperationRegistry.OutstandingCount, 0);
			factory.ResourceAllocator.TrimPooledMemory();
			Assert.AreEqual(liveSlabsBeforeTrim, CountLiveSlabs(pool));
		}
		finally {
			gate.Set();
			_ = operation.GetResultAndDisposeOperation();
		}
		Assert.AreEqual(0, OutstandingAsyncOperationRegistry.OutstandingCount);

		factory.ResourceAllocator.TrimPooledMemory();
		Assert.AreEqual(0, CountLiveSlabs(pool));
	}

	static int CountLiveSlabs(FixedByteSlabPool pool) {
		var recordCount = (int) typeof(FixedByteSlabPool).GetField("_slabRecordMaxConcurrentUsageCount", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(pool)!;
		var freeRecordCount = (int) typeof(FixedByteSlabPool).GetField("_returnedUnusedSlabRecordCounter", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(pool)!;
		return recordCount - freeRecordCount;
	}
}
