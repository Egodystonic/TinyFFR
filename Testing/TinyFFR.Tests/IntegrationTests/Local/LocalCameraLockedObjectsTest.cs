// Created on 2026-07-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalCameraLockedObjectsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		// Expected: the left quad always faces the camera exactly; the right quad only spins around the
		// vertical axis to track the camera; the text should always be readable head-on. All three should
		// pivot about their anchor points (visible when orbiting: the anchored corner/centre stays put).
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Camera-Locked Objects Test (fly around; Esc quits)");
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, 5f));
		using var quadMesh = factory.MeshBuilder.CreateQuadMesh(twoSided: true, backSideInvertsTextures: false);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		using var font = factory.AssetLoader.LoadFont(CommonTestAssets.FindAsset("DejaVuSans.ttf"));
		var pen = font.CreatePen(ColorVect.WhiteOpaque, ColorVect.BlackOpaque, 1f);
		var @string = font.CreateString("Camera-Locked Text");

		using var freeLockedQuad = factory.ObjectBuilder.CreateCameraLockedQuadInstance(
			quadMesh, mat,
			position: new Location(-2f, 0f, 0f),
			size: new XYPair<float>(1.5f, 1f),
			positionAnchor: Orientation2D.DownLeft
		);
		using var axisLockedQuad = factory.ObjectBuilder.CreateCameraLockedQuadInstance(
			quadMesh, mat,
			position: new Location(2f, 0f, 0f),
			size: new XYPair<float>(1.5f, 1f),
			lockedAxis: Direction.Up
		);
		using var lockedText = factory.ObjectBuilder.CreateCameraLockedTextInstance(
			pen, @string,
			position: new Location(0f, 2f, 0f),
			textInstanceHeight: 0.3f
		);

		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		scene.Add(freeLockedQuad);
		scene.Add(axisLockedQuad);
		scene.Add(lockedText);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		using var camController = camera.CreateController<FreeFlyingCameraController>();

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camController, dt, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camController, dt);
			DefaultCameraInputHandler.Progress(camController, dt);

			renderer.Render();
		}

		scene.Remove(freeLockedQuad);
		scene.Remove(axisLockedQuad);
		scene.Remove(lockedText);
	}
}
