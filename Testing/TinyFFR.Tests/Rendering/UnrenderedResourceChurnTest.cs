// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Reflection;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering.Local;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class UnrenderedResourceChurnTest {
	const int ChurnCount = 10_000;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void GpuHoldingBufferPoolShouldStayBoundedWhenUploadingWithoutRendering() {
		const int MaxExpectedSpaceCount = 16;
		using var factory = new LocalTinyFfrFactory();
		using var font = factory.AssetLoader.LoadFont();
		var pool = ((ILocalGpuHoldingBufferAllocator) factory).GpuHoldingBufferPool;

		for (var i = 0; i < ChurnCount; ++i) font.CreateString("Unrendered text").Dispose();

		Assert.LessOrEqual(pool.AllocatedSpaceCount, MaxExpectedSpaceCount, $"{ChurnCount} uploads with no frame rendered grew the holding buffer pool to {pool.AllocatedSpaceCount} spaces.");
	}

	[Test]
	public void QueuedResourceDisposalsShouldStayBoundedWhenDisposingWithoutRendering() {
		const int MaxExpectedQueuedDisposals = 4_096;
		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer((64, 64));
		using var scene = factory.SceneBuilder.CreateScene();
		using var camera = factory.CameraBuilder.CreateCamera();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, buffer);

		var maxQueuedDisposals = 0;
		for (var i = 0; i < ChurnCount; ++i) {
			factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube).Dispose();
			maxQueuedDisposals = Int32.Max(maxQueuedDisposals, GetQueuedDisposalCount());
		}

		Assert.Less(maxQueuedDisposals, MaxExpectedQueuedDisposals, $"Disposing {ChurnCount} meshes with no frame rendered queued up to {maxQueuedDisposals} deferred disposals.");
		renderer.RenderAndWaitForGpu();
	}

	[Test]
	public void RenderingCanvasSceneToOutputBufferShouldNotAllocate() {
		const int WarmupFrameCount = 10;
		const int MeasuredFrameCount = 100;
		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer((64, 64));
		using var canvas = factory.SceneBuilder.CreateCanvasScene();
		using var renderer = factory.RendererBuilder.CreateRenderer(canvas, buffer);

		for (var i = 0; i < WarmupFrameCount; ++i) renderer.Render();
		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < MeasuredFrameCount; ++i) renderer.Render();
		var allocatedDuring = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
		renderer.WaitForGpu();

		Assert.AreEqual(0L, allocatedDuring, $"Rendering {MeasuredFrameCount} canvas frames allocated {allocatedDuring} bytes.");
	}

	static int GetQueuedDisposalCount() {
		var queue = typeof(LocalFrameSynchronizationManager).GetField("_unassignedCallbacks", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
		return (int) queue.GetType().GetProperty("Count")!.GetValue(queue)!;
	}
}
