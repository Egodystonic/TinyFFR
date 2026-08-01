// Created on 2026-07-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Linq;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class BindableRendererCompositorImplProviderTest {
	FakeRendererBuilder _builder = null!;
	FakeResourceAllocator _allocator = null!;
	Scene _scene;
	Camera _camera;
	FakeCameraImplProvider _cameraImpl = null!;

	[SetUp]
	public void SetUpTest() {
		_builder = new FakeRendererBuilder();
		_allocator = new FakeResourceAllocator();
		_scene = BindableRendererTestScaffold.CreateScene();
		(_camera, _cameraImpl) = BindableRendererTestScaffold.CreateCamera();
	}

	[TearDown]
	public void TearDownTest() {
		_scene.Dispose();
		_camera.Dispose();
	}

	static void NoopFrameHandler(XYPair<int> dimensions, ReadOnlySpan<TexelRgb24> texels) { }

	Renderer CreateBindableRenderer(in BindableRendererCreationConfig config) => _builder.CreateBindableRenderer(_scene, _camera, _allocator, in config);
	Renderer CreateBindableRenderer() => CreateBindableRenderer(new BindableRendererCreationConfig());

	FakeCompositorImplProvider LatestInnerCompositor => _builder.CreatedCompositors[^1];
	FakeRendererImplProvider LatestInnerRenderer => _builder.CreatedRenderers[^1];
	FakeRenderOutputBufferImplProvider LatestBuffer => _builder.CreatedBuffers[^1];

	[Test]
	public void ShouldCreateSharedBufferAndActualCompositorOnConstruction() {
		var compositor = _builder.CreateBindableCompositor(new BindableRendererCompositorCreationConfig { DefaultBufferSize = (320, 160) });

		var sharedBuffer = LatestBuffer;
		var innerCompositor = LatestInnerCompositor;
		var resolvedName = compositor.GetNameAsNewStringObject();

		Assert.AreEqual(new XYPair<int>(320, 160), sharedBuffer.TextureDimensions);
		Assert.AreEqual(false, String.IsNullOrEmpty(resolvedName));
		Assert.AreEqual($"{resolvedName} output buffer", sharedBuffer.Name);
		Assert.AreEqual(resolvedName, innerCompositor.Name);
		Assert.AreEqual(sharedBuffer.BufferInstance, innerCompositor.Target);

		var namedCompositor = _builder.CreateBindableCompositor("Test Compositor");
		Assert.AreEqual("Test Compositor", namedCompositor.GetNameAsNewStringObject());
	}

	[Test]
	public void ShouldRejectNonBindableRenderersInAdd() {
		var compositor = _builder.CreateBindableCompositor();
		var nonBindableRenderer = new FakeRendererImplProvider(9999, _scene, _camera, LatestBuffer.BufferInstance, new RendererCreationConfig()).RendererInstance;

		Assert.Throws<ArgumentException>(() => compositor.Add(nonBindableRenderer, RenderCompositionType.Standard));
	}

	[Test]
	public void ShouldRejectDuplicateAdds() {
		var compositor = _builder.CreateBindableCompositor();
		var renderer = CreateBindableRenderer();
		compositor.Add(renderer, RenderCompositionType.Standard);

		Assert.Throws<ArgumentException>(() => compositor.Add(renderer, RenderCompositionType.Standard));
	}

	[Test]
	public void ShouldRejectRendererAlreadyAttachedToAnotherCompositor() {
		var compositorA = _builder.CreateBindableCompositor();
		var compositorB = _builder.CreateBindableCompositor();
		var renderer = CreateBindableRenderer();
		compositorA.Add(renderer, RenderCompositionType.Standard);

		Assert.Throws<InvalidOperationException>(() => compositorB.Add(renderer, RenderCompositionType.Standard));
	}

	[Test]
	public void ShouldRejectRendererCurrentlySupplyingFramesDirectly() {
		var compositor = _builder.CreateBindableCompositor();
		var renderer = CreateBindableRenderer();
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (100, 50), (100, 50), NoopFrameHandler);

		Assert.Throws<InvalidOperationException>(() => compositor.Add(renderer, RenderCompositionType.Standard));

		BindableRendererImplProvider.StopHandlingFrames(renderer);
		Assert.DoesNotThrow(() => compositor.Add(renderer, RenderCompositionType.Standard));
	}

	[Test]
	public void ShouldRetargetAddedRendererOntoSharedBuffer() {
		var compositor = _builder.CreateBindableCompositor();
		var sharedBuffer = LatestBuffer;
		var innerCompositor = LatestInnerCompositor;

		var renderer = CreateBindableRenderer();
		var standaloneBuffer = LatestBuffer;
		var originalInnerRenderer = LatestInnerRenderer;

		compositor.Add(renderer, RenderCompositionType.RetainPreviousScenes);

		Assert.AreEqual(true, standaloneBuffer.Disposed);
		Assert.AreEqual(true, originalInnerRenderer.Disposed);
		var retargetedInnerRenderer = LatestInnerRenderer;
		Assert.AreEqual(sharedBuffer.BufferInstance, retargetedInnerRenderer.TargetBuffer);
		Assert.AreEqual(1, innerCompositor.AddCalls.Count);
		Assert.AreEqual(retargetedInnerRenderer.RendererInstance, innerCompositor.AddCalls[0].Renderer);
		Assert.AreEqual(RenderCompositionType.RetainPreviousScenes, innerCompositor.AddCalls[0].CompositionType);
	}

	[Test]
	public void ShouldReturnAddedRenderersInOrderAndInvalidateEnumerationOnChange() {
		var compositor = _builder.CreateBindableCompositor();
		var rendererA = CreateBindableRenderer();
		var rendererB = CreateBindableRenderer();
		compositor.Add(rendererA, RenderCompositionType.Standard);

		var preAddEnumerable = compositor.AddedRenderers;
		Assert.AreEqual(1, preAddEnumerable.Count);

		compositor.Add(rendererB, RenderCompositionType.RetainPreviousScenes);

		var postAddEnumerable = compositor.AddedRenderers;
		Assert.AreEqual(2, postAddEnumerable.Count);
		Assert.AreEqual(rendererA, postAddEnumerable[0]);
		Assert.AreEqual(rendererB, postAddEnumerable[1]);

		Assert.Throws<InvalidOperationException>(() => _ = preAddEnumerable.Count);
	}

	[Test]
	public void ShouldForwardAndPersistEnabledState() {
		var compositor = _builder.CreateBindableCompositor();
		var innerCompositor = LatestInnerCompositor;
		var rendererA = CreateBindableRenderer();
		var rendererB = CreateBindableRenderer();
		var unaddedRenderer = CreateBindableRenderer();
		compositor.Add(rendererA, RenderCompositionType.Standard);
		compositor.Add(rendererB, RenderCompositionType.RetainPreviousScenes);
		var innerRendererB = LatestInnerRenderer;

		compositor.SetEnabledState(rendererB, false);

		Assert.AreEqual(1, innerCompositor.SetEnabledStateCalls.Count);
		Assert.AreEqual((innerRendererB.RendererInstance, false), innerCompositor.SetEnabledStateCalls[0]);
		Assert.Throws<ArgumentException>(() => compositor.SetEnabledState(unaddedRenderer, false));

		BindableRendererCompositorImplProvider.StartOrContinueHandlingFrames(compositor, (100, 50), (100, 50), NoopFrameHandler);

		var recreatedInnerCompositor = LatestInnerCompositor;
		Assert.AreNotEqual(innerCompositor, recreatedInnerCompositor);
		Assert.AreEqual(1, recreatedInnerCompositor.SetEnabledStateCalls.Count);
		Assert.AreEqual(false, recreatedInnerCompositor.SetEnabledStateCalls[0].Enabled);
		Assert.AreEqual(recreatedInnerCompositor.AddCalls[1].Renderer, recreatedInnerCompositor.SetEnabledStateCalls[0].Renderer);
	}

	[Test]
	public void ShouldForwardRenderAllAndWaitForGpu() {
		var compositor = _builder.CreateBindableCompositor();
		var innerCompositor = LatestInnerCompositor;

		compositor.RenderAll();
		compositor.RenderAllAndWaitForGpu();

		Assert.AreEqual(2, innerCompositor.RenderAllCount);
		Assert.AreEqual(1, innerCompositor.WaitForGpuCount);
	}

	[Test]
	public void ShouldRecreateEverythingWhenHandlingFramesStarts() {
		var compositor = _builder.CreateBindableCompositor();
		var originalSharedBuffer = LatestBuffer;
		var originalInnerCompositor = LatestInnerCompositor;
		var rendererA = CreateBindableRenderer();
		var rendererB = CreateBindableRenderer();
		compositor.Add(rendererA, RenderCompositionType.Standard);
		var innerRendererA = LatestInnerRenderer;
		compositor.Add(rendererB, RenderCompositionType.RetainPreviousScenes);
		var innerRendererB = LatestInnerRenderer;
		compositor.SetEnabledState(rendererB, false);
		_cameraImpl.AspectRatioCalls.Clear();

		BindableRendererCompositorImplProvider.StartOrContinueHandlingFrames(compositor, (400, 100), (400, 100), NoopFrameHandler);

		Assert.AreEqual(true, originalInnerCompositor.Disposed);
		Assert.AreEqual(true, originalSharedBuffer.Disposed);
		Assert.AreEqual(true, innerRendererA.Disposed);
		Assert.AreEqual(true, innerRendererB.Disposed);

		var newSharedBuffer = LatestBuffer;
		Assert.AreEqual(new XYPair<int>(400, 100), newSharedBuffer.TextureDimensions);
		Assert.AreNotEqual(null, newSharedBuffer.CurrentHandler);
		Assert.AreEqual(true, newSharedBuffer.LastHandlerLowestAddressesRepresentFrameTop);

		var newInnerCompositor = LatestInnerCompositor;
		Assert.AreEqual(newSharedBuffer.BufferInstance, newInnerCompositor.Target);
		Assert.AreEqual(2, newInnerCompositor.AddCalls.Count);
		Assert.AreEqual(RenderCompositionType.Standard, newInnerCompositor.AddCalls[0].CompositionType);
		Assert.AreEqual(RenderCompositionType.RetainPreviousScenes, newInnerCompositor.AddCalls[1].CompositionType);
		Assert.AreEqual(newSharedBuffer.BufferInstance, ((FakeRendererImplProvider) newInnerCompositor.AddCalls[0].Renderer.Implementation).TargetBuffer);
		Assert.AreEqual((newInnerCompositor.AddCalls[1].Renderer, false), newInnerCompositor.SetEnabledStateCalls[0]);

		Assert.AreEqual(true, _cameraImpl.AspectRatioCalls.Contains(4f));

		Assert.AreEqual(rendererA, compositor.AddedRenderers[0]);
		Assert.AreEqual(rendererB, compositor.AddedRenderers[1]);

		BindableRendererCompositorImplProvider.StopHandlingFrames(compositor);
		Assert.AreEqual(null, newSharedBuffer.CurrentHandler);
	}

	[Test]
	public void ShouldPreventDirectFrameHandlingAndDisposalOfAttachedRenderers() {
		var compositor = _builder.CreateBindableCompositor();
		var renderer = CreateBindableRenderer();
		compositor.Add(renderer, RenderCompositionType.Standard);
		var innerRenderer = LatestInnerRenderer;

		Assert.Throws<InvalidOperationException>(() => BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (100, 50), (100, 50), NoopFrameHandler));
		Assert.Throws<InvalidOperationException>(() => BindableRendererImplProvider.StopHandlingFrames(renderer));
		Assert.Throws<InvalidOperationException>(renderer.Dispose);

		Assert.DoesNotThrow(renderer.Render);
		Assert.AreEqual(1, innerRenderer.RenderCount);
	}

	[Test]
	public void ShouldDetachContainedRenderersWhenDisposedWithoutCascade() {
		var compositor = _builder.CreateBindableCompositor();
		var sharedBuffer = LatestBuffer;
		var innerCompositor = LatestInnerCompositor;
		var renderer = CreateBindableRenderer(new BindableRendererCreationConfig { DefaultBufferSize = (222, 111) });
		compositor.Add(renderer, RenderCompositionType.Standard);
		var attachedInnerRenderer = LatestInnerRenderer;

		compositor.Dispose();

		Assert.AreEqual(true, innerCompositor.Disposed);
		Assert.AreEqual(true, sharedBuffer.Disposed);
		Assert.AreEqual(true, attachedInnerRenderer.Disposed);

		Assert.AreEqual(false, renderer.IsDisposed);
		var newStandaloneBuffer = LatestBuffer;
		Assert.AreEqual(new XYPair<int>(222, 111), newStandaloneBuffer.TextureDimensions);
		Assert.AreEqual(newStandaloneBuffer.BufferInstance, LatestInnerRenderer.TargetBuffer);

		Assert.Throws<ObjectDisposedException>(compositor.RenderAll);
		Assert.DoesNotThrow(compositor.Dispose);

		Assert.DoesNotThrow(renderer.Dispose);
		Assert.AreEqual(true, renderer.IsDisposed);
	}

	[Test]
	public void ShouldDisposeContainedRenderersWhenDisposedWithCascade() {
		var compositor = _builder.CreateBindableCompositor();
		var renderer = CreateBindableRenderer();
		compositor.Add(renderer, RenderCompositionType.Standard);
		var attachedInnerRenderer = LatestInnerRenderer;
		var rendererBufferCountBeforeDisposal = _builder.CreatedBuffers.Count;

		compositor.Dispose(disposeContainedRenderers: true);

		Assert.AreEqual(true, attachedInnerRenderer.Disposed);
		Assert.AreEqual(true, renderer.IsDisposed);
		Assert.AreEqual(true, _allocator.CreatedGroups[0].Disposed);
		Assert.AreEqual(rendererBufferCountBeforeDisposal, _builder.CreatedBuffers.Count);
	}

	[Test]
	public void ShouldPropagateRayCastScalingToRenderersAddedAfterFrameHandlingStarts() {
		var compositor = _builder.CreateBindableCompositor();
		BindableRendererCompositorImplProvider.StartOrContinueHandlingFrames(compositor, (1600, 900), (800, 450), NoopFrameHandler);
		var renderer = CreateBindableRenderer();
		compositor.Add(renderer, RenderCompositionType.Standard);

		_ = renderer.CastRayFromRenderSurface((100, 50));

		Assert.AreEqual(new XYPair<int>(200, 100), LatestInnerRenderer.RenderSurfaceRayCalls[0].PixelCoord);
	}

	[Test]
	public void ShouldRetainRayCastScalingAcrossSharedBufferRecreation() {
		var compositor = _builder.CreateBindableCompositor();
		var renderer = CreateBindableRenderer();
		compositor.Add(renderer, RenderCompositionType.Standard);
		BindableRendererCompositorImplProvider.StartOrContinueHandlingFrames(compositor, (1600, 900), (800, 450), NoopFrameHandler);

		BindableRendererCompositorImplProvider.StartOrContinueHandlingFrames(compositor, (600, 300), (300, 150), NoopFrameHandler);
		_ = renderer.CastRayFromRenderSurface((100, 50));

		Assert.AreEqual(new XYPair<int>(200, 100), LatestInnerRenderer.RenderSurfaceRayCalls[0].PixelCoord);
	}
}
