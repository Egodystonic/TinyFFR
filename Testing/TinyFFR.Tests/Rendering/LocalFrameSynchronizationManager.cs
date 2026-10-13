// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Reflection;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering.Local;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class LocalFrameSynchronizationManagerTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static int GetQueuedDisposalCount() {
		var queue = typeof(LocalFrameSynchronizationManager).GetField("_unassignedCallbacks", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
		return (int) queue.GetType().GetProperty("Count")!.GetValue(queue)!;
	}

	[Test]
	public void QueuedResourceDisposalsShouldStayBoundedWhenDisposingWithoutRendering() {
		const int NumIterations = 10_000;
		const int MaxExpectedQueuedDisposals = 4_096;
		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer((64, 64));
		using var scene = factory.SceneBuilder.CreateScene();
		using var camera = factory.CameraBuilder.CreateCamera();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, buffer);

		var maxQueuedDisposals = 0;
		for (var i = 0; i < NumIterations; ++i) {
			factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube).Dispose();
			maxQueuedDisposals = Int32.Max(maxQueuedDisposals, GetQueuedDisposalCount());
		}

		Assert.Less(maxQueuedDisposals, MaxExpectedQueuedDisposals, $"Disposing {NumIterations} meshes with no frame rendered queued up to {maxQueuedDisposals} deferred disposals.");
		renderer.RenderAndWaitForGpu();
	}
}