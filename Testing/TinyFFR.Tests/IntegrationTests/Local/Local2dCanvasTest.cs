// Created on 2026-08-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class Local2dCanvasTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		const int CornerInset = 16;
		const int TextPixelHeight = 28;
		var cornerQuadSize = new XYPair<int>(128, 64);
		var centreQuadSize = new XYPair<int>(200, 200);

		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local 2D Canvas Test (key: T to toggle text input, V to toggle visibility)");

		using var worldScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var worldCamera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var cuboidMesh = factory.MeshBuilder.CreateMesh(new Cuboid(1f));
		using var worldMat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		using var cuboidInstance = factory.ObjectBuilder.CreateModelInstance(cuboidMesh, worldMat, new Location(0f, 0f, 3f));
		worldScene.Add(cuboidInstance);
		using var light = factory.LightBuilder.CreatePointLight(new Location(1f, 1f, 1f), brightness: 5f);
		worldScene.Add(light);
		using var worldRenderer = factory.RendererBuilder.CreateRenderer(worldScene, worldCamera, window);

		using var canvasScene = factory.SceneBuilder.CreateCanvasScene();
		using var canvasRenderer = factory.RendererBuilder.CreateRenderer(canvasScene, window);

		using var whiteTex = factory.TextureBuilder.CreateColorMap(ColorVect.WhiteOpaque, includeAlpha: false);
		using var redTex = factory.TextureBuilder.CreateColorMap(ColorVect.RedOpaque, includeAlpha: false);
		using var greenTex = factory.TextureBuilder.CreateColorMap(ColorVect.GreenOpaque, includeAlpha: false);
		using var blueTex = factory.TextureBuilder.CreateColorMap(ColorVect.BlueOpaque, includeAlpha: false);

		var cornerAnchors = new[] { Orientation2D.UpLeft, Orientation2D.UpRight, Orientation2D.DownLeft, Orientation2D.DownRight };
		foreach (var anchor in cornerAnchors) {
			var corner = canvasScene.Add(whiteTex);
			corner.CanvasAnchor = anchor;
			corner.PositionPixels = (CornerInset, CornerInset);
			corner.SizePixels = cornerQuadSize;
		}

		var lowLayerQuad = canvasScene.Add(redTex);
		lowLayerQuad.CanvasAnchor = Orientation2D.None;
		lowLayerQuad.SizePixels = centreQuadSize;
		lowLayerQuad.Layer = 1;

		var highLayerQuad = canvasScene.Add(greenTex);
		highLayerQuad.CanvasAnchor = Orientation2D.None;
		highLayerQuad.PositionPixels = (60, 60);
		highLayerQuad.SizePixels = centreQuadSize;
		highLayerQuad.Layer = 2;

		var halfWidthBar = canvasScene.Add(blueTex);
		halfWidthBar.CanvasAnchor = Orientation2D.Down;
		halfWidthBar.PositionPixels = (0, CornerInset);
		halfWidthBar.SizeFraction = (0.5f, 0f);
		halfWidthBar.SizePixels = (0, 32);

		using var font = factory.AssetLoader.LoadFont();
		using var titleString = font.CreateString("2D Canvas -- pixel space");
		var title = canvasScene.Add(titleString, font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline));
		title.CanvasAnchor = Orientation2D.Up;
		title.PositionPixels = (0, CornerInset);
		title.SizePixels = (0, TextPixelHeight);

		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(worldRenderer, RenderCompositionType.Standard);
		compositor.Add(canvasRenderer, RenderCompositionType.RetainPreviousScenes);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			var kbm = loop.Input.KeyboardAndMouse;

			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.T)) loop.EnableInputTextTranscription = !loop.EnableInputTextTranscription;
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.V)) highLayerQuad.IsVisible = !highLayerQuad.IsVisible;

			cuboidInstance.RotateBy(new Rotation(30f * dt, Direction.Up));
			lowLayerQuad.RotateBy(20f * dt);

			window.SetTitle(
				$"text input {(loop.EnableInputTextTranscription ? "ON" : "off")} | typed: '{kbm.TranscribedText.ToString()}'"
			);

			compositor.RenderAll();
		}
	}
}
