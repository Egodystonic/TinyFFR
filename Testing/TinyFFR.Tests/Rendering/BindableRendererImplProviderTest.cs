// Created on 2026-07-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class BindableRendererImplProviderTest {
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

	static void NoopFrameHandler(XYPair<int> dimensions, ReadOnlySpan<TexelRgba32> texels) { }

	Renderer CreateRenderer(in BindableRendererCreationConfig config) => _builder.CreateBindableRenderer(_scene, _camera, _allocator, in config);
	Renderer CreateRenderer() => CreateRenderer(new BindableRendererCreationConfig { Quality = RenderQualityConfig.Default });

	[Test]
	public void ShouldPersistFrustumCullingSettingAcrossBufferRecreation() {
		var renderer = CreateRenderer();
		renderer.SetFrustumCullingEnabled(false);
		Assert.AreEqual(new[] { false }, _builder.CreatedRenderers[0].FrustumCullingCalls);

		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (100, 50), (100, 50), NoopFrameHandler);

		Assert.AreEqual(2, _builder.CreatedRenderers.Count);
		Assert.AreEqual(new[] { false }, _builder.CreatedRenderers[1].FrustumCullingCalls);
	}

	[Test]
	public void ShouldNotSetFrustumCullingWhenNeverConfigured() {
		var renderer = CreateRenderer();
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (100, 50), (100, 50), NoopFrameHandler);

		Assert.AreEqual(0, _builder.CreatedRenderers[0].FrustumCullingCalls.Count);
		Assert.AreEqual(0, _builder.CreatedRenderers[1].FrustumCullingCalls.Count);
	}

	[Test]
	public void ShouldPersistViewportSubAreaAcrossBufferRecreation() {
		var renderer = CreateRenderer();
		renderer.SetRenderSubAreaFraction(Orientation2D.UpLeft, (0.1f, 0.2f), (0.5f, 0.6f));

		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (100, 50), (100, 50), NoopFrameHandler);

		var secondRenderer = _builder.CreatedRenderers[1];
		Assert.AreEqual(1, secondRenderer.SubAreaFractionCalls.Count);
		Assert.AreEqual((Orientation2D.UpLeft, new XYPair<float>(0.1f, 0.2f), new XYPair<float>(0.5f, 0.6f)), secondRenderer.SubAreaFractionCalls[0]);

		renderer.SetRenderSubAreaPixels(Orientation2D.DownRight, (10, 20), (50, 60));
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (200, 100), (200, 100), NoopFrameHandler);

		var thirdRenderer = _builder.CreatedRenderers[2];
		Assert.AreEqual(0, thirdRenderer.SubAreaFractionCalls.Count);
		Assert.AreEqual(1, thirdRenderer.SubAreaPixelCalls.Count);
		Assert.AreEqual((Orientation2D.DownRight, new XYPair<int>(10, 20), new XYPair<int>(50, 60)), thirdRenderer.SubAreaPixelCalls[0]);
	}

	[Test]
	public void ShouldForwardResolvedNameToActualRendererAndBuffer() {
		var unnamedRenderer = CreateRenderer();
		var generatedName = unnamedRenderer.GetNameAsNewStringObject();
		Assert.AreEqual(false, String.IsNullOrEmpty(generatedName));
		Assert.AreEqual(generatedName, _builder.CreatedRenderers[0].Name);
		Assert.AreEqual($"{generatedName} output buffer", _builder.CreatedBuffers[0].Name);

		_ = CreateRenderer(new BindableRendererCreationConfig { Name = "Test Renderer", Quality = RenderQualityConfig.Default });
		Assert.AreEqual("Test Renderer", _builder.CreatedRenderers[1].Name);
		Assert.AreEqual("Test Renderer output buffer", _builder.CreatedBuffers[1].Name);
	}

	[Test]
	public void ShouldPersistQualityConfigAcrossBufferRecreation() {
		var renderer = CreateRenderer();
		renderer.SetQuality(new RenderQualityConfig { ShadowQuality = Quality.VeryHigh });

		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (100, 50), (100, 50), NoopFrameHandler);

		Assert.AreEqual(Quality.VeryHigh, _builder.CreatedRenderers[1].Quality.ShadowQuality);
	}

	[Test]
	public void ShouldRegisterFrameHandlerOnRecreatedBuffer() {
		var renderer = CreateRenderer();
		Assert.AreEqual(null, _builder.CreatedBuffers[0].CurrentHandler);

		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (100, 50), (100, 50), NoopFrameHandler);

		var secondBuffer = _builder.CreatedBuffers[1];
		Assert.AreEqual(new XYPair<int>(100, 50), secondBuffer.TextureDimensions);
		Assert.AreNotEqual(null, secondBuffer.CurrentHandler);
		Assert.AreEqual(true, secondBuffer.LastHandlerLowestAddressesRepresentFrameTop);
		Assert.AreEqual(true, _builder.CreatedBuffers[0].Disposed);
		Assert.AreEqual(true, _builder.CreatedRenderers[0].Disposed);

		BindableRendererImplProvider.StopHandlingFrames(renderer);
		Assert.AreEqual(null, secondBuffer.CurrentHandler);
		Assert.AreEqual(1, secondBuffer.ClearHandlersCallCount);
	}

	[Test]
	public void ShouldUpdateCameraAspectRatioWhenConfigured() {
		_ = CreateRenderer(new BindableRendererCreationConfig { DefaultBufferSize = (200, 100), Quality = RenderQualityConfig.Default });
		Assert.AreEqual(new[] { 2f }, _cameraImpl.AspectRatioCalls);

		_cameraImpl.AspectRatioCalls.Clear();
		var renderer = CreateRenderer(new BindableRendererCreationConfig { AutoUpdateCameraAspectRatio = false, Quality = RenderQualityConfig.Default });
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (100, 50), (100, 50), NoopFrameHandler);
		Assert.AreEqual(0, _cameraImpl.AspectRatioCalls.Count);
	}

	[Test]
	public void ShouldDisposeOwnedResourcesOnDisposal() {
		var renderer = CreateRenderer();
		renderer.Dispose();

		Assert.AreEqual(true, _builder.CreatedRenderers[0].Disposed);
		Assert.AreEqual(true, _builder.CreatedBuffers[0].Disposed);
		Assert.AreEqual(true, _allocator.CreatedGroups[0].Disposed);
		Assert.AreEqual(true, renderer.IsDisposed);
		Assert.DoesNotThrow(renderer.Dispose);
	}

	[Test]
	public void ShouldScaleRayCastCoordsUpToBufferSpace() {
		var renderer = CreateRenderer();
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (1600, 900), (800, 450), NoopFrameHandler);

		_ = renderer.CastRayFromRenderSurface((100, 50));

		var actualRenderer = _builder.CreatedRenderers[^1];
		Assert.AreEqual(1, actualRenderer.RenderSurfaceRayCalls.Count);
		Assert.AreEqual(new XYPair<int>(200, 100), actualRenderer.RenderSurfaceRayCalls[0].PixelCoord);
		Assert.AreEqual(true, actualRenderer.RenderSurfaceRayCalls[0].DisableDpiScalingAdjustment);
	}

	[Test]
	public void ShouldScaleRayCastCoordsDownWhenBufferIsSmallerThanControl() {
		var renderer = CreateRenderer();
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (300, 150), (900, 450), NoopFrameHandler);

		_ = renderer.CastRayFromRenderSurface((300, 150));

		Assert.AreEqual(new XYPair<int>(100, 50), _builder.CreatedRenderers[^1].RenderSurfaceRayCalls[0].PixelCoord);
	}

	[Test]
	public void ShouldNotScaleRayCastCoordsWhenAdjustmentDisabled() {
		var renderer = CreateRenderer();
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (1600, 900), (800, 450), NoopFrameHandler);

		_ = renderer.CastRayFromRenderSurface((100, 50), DiagonalOrientation2D.UpLeft, true);

		Assert.AreEqual(new XYPair<int>(100, 50), _builder.CreatedRenderers[^1].RenderSurfaceRayCalls[0].PixelCoord);
	}

	[Test]
	public void ShouldScaleSubAreaRayCastCoords() {
		var renderer = CreateRenderer();
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (1600, 900), (800, 450), NoopFrameHandler);

		_ = renderer.CastRayFromRenderSubAreaSurface((100, 50));

		var actualRenderer = _builder.CreatedRenderers[^1];
		Assert.AreEqual(1, actualRenderer.ViewportSurfaceRayCalls.Count);
		Assert.AreEqual(new XYPair<int>(200, 100), actualRenderer.ViewportSurfaceRayCalls[0].PixelCoord);
	}

	[Test]
	public void ShouldNotScaleRayCastCoordsBeforeFrameHandlingStarts() {
		var renderer = CreateRenderer();

		_ = renderer.CastRayFromRenderSurface((100, 50));

		Assert.AreEqual(new XYPair<int>(100, 50), _builder.CreatedRenderers[^1].RenderSurfaceRayCalls[0].PixelCoord);
	}

	[Test]
	public void ShouldTreatNonPositiveCursorCoordinateSpaceSizeAsIdentity() {
		var renderer = CreateRenderer();
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (300, 150), (0, 0), NoopFrameHandler);

		_ = renderer.CastRayFromRenderSurface((100, 50));

		Assert.AreEqual(new XYPair<int>(100, 50), _builder.CreatedRenderers[^1].RenderSurfaceRayCalls[0].PixelCoord);
	}

	[Test]
	public void ShouldRoundScaledRayCastCoordsToEven() {
		var renderer = CreateRenderer();
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (150, 150), (100, 100), NoopFrameHandler);

		_ = renderer.CastRayFromRenderSurface((1, 3));

		Assert.AreEqual(new XYPair<int>(2, 4), _builder.CreatedRenderers[^1].RenderSurfaceRayCalls[0].PixelCoord);
	}

	[Test]
	public void ShouldNotRecreateTargetBufferWhenBufferSizeIsUnchanged() {
		var renderer = CreateRenderer();
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (100, 50), (100, 50), NoopFrameHandler);
		var rendererCountAfterFirstStart = _builder.CreatedRenderers.Count;
		var bufferCountAfterFirstStart = _builder.CreatedBuffers.Count;

		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (100, 50), (100, 50), NoopFrameHandler);

		Assert.AreEqual(rendererCountAfterFirstStart, _builder.CreatedRenderers.Count);
		Assert.AreEqual(bufferCountAfterFirstStart, _builder.CreatedBuffers.Count);
		Assert.AreNotEqual(null, _builder.CreatedBuffers[^1].CurrentHandler);
	}

	[Test]
	public void ShouldUpdateRayCastScalingWhenOnlyCursorCoordinateSpaceSizeChanges() {
		var renderer = CreateRenderer();
		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (800, 400), (800, 400), NoopFrameHandler);
		var bufferCountAfterFirstStart = _builder.CreatedBuffers.Count;

		BindableRendererImplProvider.StartOrContinueHandlingFrames(renderer, (800, 400), (400, 200), NoopFrameHandler);
		_ = renderer.CastRayFromRenderSurface((100, 50));

		Assert.AreEqual(bufferCountAfterFirstStart, _builder.CreatedBuffers.Count);
		Assert.AreEqual(new XYPair<int>(200, 100), _builder.CreatedRenderers[^1].RenderSurfaceRayCalls[0].PixelCoord);
	}
}
