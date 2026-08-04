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
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local 2D Canvas Test (key: T to toggle text input)");

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

		using var quadMesh = factory.MeshBuilder.CreateQuadMesh();
		using var canvasMat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial(ignoresLighting: true);

		var cornerAnchors = new[] { Orientation2D.UpLeft, Orientation2D.UpRight, Orientation2D.DownLeft, Orientation2D.DownRight };
		var cornerQuads = new QuadInstance[cornerAnchors.Length];
		for (var i = 0; i < cornerAnchors.Length; ++i) {
			cornerQuads[i] = factory.ObjectBuilder.CreateQuadInstance(quadMesh, canvasMat);
			canvasScene.Add(cornerQuads[i], cornerAnchors[i], (CornerInset, CornerInset), cornerQuadSize);
		}

		using var lowLayerQuad = factory.ObjectBuilder.CreateQuadInstance(quadMesh, canvasMat);
		using var highLayerQuad = factory.ObjectBuilder.CreateQuadInstance(quadMesh, canvasMat);
		lowLayerQuad.SetDefaultMaterialBaseColor(ColorVect.RedOpaque);
		highLayerQuad.SetDefaultMaterialBaseColor(ColorVect.GreenOpaque);
		canvasScene.Add(lowLayerQuad, Orientation2D.None, XYPair<int>.Zero, centreQuadSize, layer: 1);
		canvasScene.Add(highLayerQuad, Orientation2D.None, new XYPair<int>(60, 60), centreQuadSize, layer: 2);

		using var halfWidthBar = factory.ObjectBuilder.CreateQuadInstance(quadMesh, canvasMat);
		halfWidthBar.SetDefaultMaterialBaseColor(ColorVect.BlueOpaque);
		canvasScene.Add(halfWidthBar, new CanvasDock {
			CanvasAnchor = Orientation2D.Down,
			PixelOffset = (0, CornerInset),
			FractionalSize = (0.5f, 0f),
			PixelSize = (0, 32)
		});

		using var font = factory.AssetLoader.LoadFont();
		var pen = font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);
		using var titleString = font.CreateString("2D Canvas -- pixel space");
		using var titleInstance = factory.ObjectBuilder.CreateTextInstance(pen, titleString);
		canvasScene.Add(titleInstance, Orientation2D.Up, (0, CornerInset), TextPixelHeight);

		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(worldRenderer, RenderCompositionType.Standard);
		compositor.Add(canvasRenderer, RenderCompositionType.RetainPreviousScenes);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			var kbm = loop.Input.KeyboardAndMouse;

			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.T)) loop.EnableInputTextTranscription = !loop.EnableInputTextTranscription;

			cuboidInstance.RotateBy(new Rotation(30f * dt, Direction.Up));

			var cursorCanvasCoord = canvasScene.GetPixelCoordFromCursor(kbm.MouseCursorPosition);
			window.SetTitle(
				$"Canvas {canvasScene.SizePixels} | cursor {cursorCanvasCoord} (contained: {canvasScene.Contains(cursorCanvasCoord)}) " +
				$"| text input {(loop.EnableInputTextTranscription ? "ON" : "off")} | typed: '{kbm.TranscribedText.ToString()}'"
			);

			compositor.RenderAll();
		}

		foreach (var quad in cornerQuads) quad.Dispose();
	}
}
