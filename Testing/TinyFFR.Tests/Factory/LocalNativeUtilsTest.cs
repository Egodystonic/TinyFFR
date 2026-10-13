// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Reflection;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Factory;

[TestFixture]
class LocalNativeUtilsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }
	
	static int GetOutstandingUploadCount() {
		var map = typeof(LocalNativeUtils).GetField("_activeTemporaryBuffers", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
		return (int) map.GetType().GetProperty("Count")!.GetValue(map)!;
	}

	[Test]
	public void OutstandingUploadBytesShouldStayBoundedWhenUploadingLargeBuffersWithoutRendering() {
		const int LargeUploadCount = 2_000;
		const int VertexCount = 16_384;
		const long FlushThresholdBytes = 32L * 1024 * 1024;
		const long StallThresholdBytes = 64L * 1024 * 1024;
		const long MaxExpectedOutstandingBytes = StallThresholdBytes + 2L * 1024 * 1024;
		using var factory = new LocalTinyFfrFactory(new LocalTinyFfrFactoryConfig {
			UploadMemoryBufferFlushThresholdBytes = FlushThresholdBytes,
			UploadMemoryBufferStallThresholdBytes = StallThresholdBytes
		});
		var pool = ((ILocalGpuHoldingBufferAllocator) factory).GpuHoldingBufferPool;

		var maxOutstandingBytes = 0L;
		for (var i = 0; i < LargeUploadCount; ++i) {
			factory.MeshBuilder.CreateDynamicVertexBuffer(VertexCount, 1).Dispose();
			maxOutstandingBytes = Int64.Max(maxOutstandingBytes, pool.CurrentlyLeasedTotalBytes);
		}

		Assert.LessOrEqual(maxOutstandingBytes, MaxExpectedOutstandingBytes, $"{LargeUploadCount} large uploads with no frame rendered reached {maxOutstandingBytes} outstanding bytes.");
	}

	[Test]
	public void UploadsShouldBeReleasedByAFrame() {
		const int NumIterations = 10_000;
		
		using var factory = new LocalTinyFfrFactory();
		using var font = factory.AssetLoader.LoadFont();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer((64, 64));
		using var scene = factory.SceneBuilder.CreateScene();
		using var camera = factory.CameraBuilder.CreateCamera();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, buffer);
		var pool = ((ILocalGpuHoldingBufferAllocator) factory).GpuHoldingBufferPool;
		renderer.RenderAndWaitForGpu();

		for (var i = 0; i < NumIterations; ++i) font.CreateString("Unrendered text").Dispose();
		Assert.Greater(GetOutstandingUploadCount(), 0);
		Assert.Greater(pool.CurrentlyLeasedTotalBytes, 0L);

		renderer.RenderAndWaitForGpu();
		Assert.AreEqual(0, GetOutstandingUploadCount());
		Assert.AreEqual(0L, pool.CurrentlyLeasedTotalBytes);
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
}
