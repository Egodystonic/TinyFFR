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
using Egodystonic.TinyFFR.Rendering;
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
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Camera-Locked Objects Test");
		using var userControlledCamera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -2f));
		using var staticCamera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -0.4f)); 
		using var font = factory.AssetLoader.LoadFont(BuiltInFont.Monospace);
		using var pen = font.CreatePen(BuiltInFontPenStyle.Default);
		using var @string = font.CreateString("12345678");

		using var lockedText = factory.ObjectBuilder.CreateCameraLockedTextInstance(
			pen, 
			@string,
			lockedUprightDirection: Direction.Up,
			textInstanceHeight: 0.1f
		);

		using var userControlledScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var staticScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		userControlledScene.Add(lockedText);
		staticScene.Add(lockedText);
		using var userControlledSceneRenderer = factory.RendererBuilder.CreateRenderer(userControlledScene, userControlledCamera, window);
		using var staticSceneRenderer = factory.RendererBuilder.CreateRenderer(staticScene, staticCamera, window);
		staticSceneRenderer.SetRenderSubAreaFraction(Orientation2D.DownRight, (0.02f, 0.02f), (0.2f, 0.2f));
		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(userControlledSceneRenderer, RenderCompositionType.Standard);
		compositor.Add(staticSceneRenderer, RenderCompositionType.Standard);
		using var camController = userControlledCamera.CreateController<InspectorCameraController>();

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camController, dt, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camController, dt);
			DefaultCameraInputHandler.Progress(camController, dt);

			compositor.RenderAll();
		}

		staticScene.RemoveAll();
		userControlledScene.RemoveAll();
	}
}
