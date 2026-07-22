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
	const int QuadCount = 10_000;
	const float QuadSize = 0.005f;
	
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Camera-Locked Objects Test [Space]");
		using var userControlledCamera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -2f));
		using var automaticCamera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -0.4f)); 
		using var userControlledScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var automaticScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		
		using var font = factory.AssetLoader.LoadFont(BuiltInFont.Monospace);
		using var pen = font.CreatePen(BuiltInFontPenStyle.BlackWithBackground);
		using var @string = font.CreateString("12345678");
		var curLockedTextAnchor = Orientation2D.None;
		var lockedText = factory.ObjectBuilder.CreateCameraLockedTextInstance(
			pen, 
			@string,
			lockedUprightDirection: Direction.Up,
			textInstanceHeight: 0.1f,
			positionAnchor: curLockedTextAnchor
		);
		
		using var quad = factory.MeshBuilder.CreateQuadMesh(twoSided: false);
		using var quadTex = factory.TextureBuilder.CreateColorMap(
			TexturePattern.GradientVertical(ColorVect.WhiteOpaque, ColorVect.BlackTransparent),
			includeAlpha: true,
			config: new TextureCreationConfig { IsLinearColorspace = false, RenderingConfig = new() { DisableTextureRepeat = true }}
		);
		using var quadMat = factory.MaterialBuilder.CreateLightingIgnoringMaterial(quadTex);
		var lockedQuads = Enumerable.Range(0, QuadCount).Select(_ => factory.ObjectBuilder.CreateCameraLockedQuadInstance(quad, quadMat, position: Location.Random(Sphere.UnitSphere), size: XYPair<float>.One * QuadSize)).ToArray();
		foreach (var lq in lockedQuads) {
			userControlledScene.Add(lq, true);
			automaticScene.Add(lq, true);	
		}

		userControlledScene.Add(lockedText);
		automaticScene.Add(lockedText);
		using var userControlledSceneRenderer = factory.RendererBuilder.CreateRenderer(userControlledScene, userControlledCamera, window);
		using var automaticSceneRenderer = factory.RendererBuilder.CreateRenderer(automaticScene, automaticCamera, window);
		automaticSceneRenderer.SetRenderSubAreaFraction(Orientation2D.DownRight, (0.02f, 0.02f), (0.2f, 0.2f));
		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(userControlledSceneRenderer, RenderCompositionType.Standard);
		compositor.Add(automaticSceneRenderer, RenderCompositionType.Standard);
		using var userCamController = userControlledCamera.CreateController<InspectorCameraController>();
		userCamController.AllowUpsideDownFlip = true;
		using var autoCamController = automaticCamera.CreateController<OrbitalCameraController>();

		
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, userCamController, dt, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, userCamController, dt);
			DefaultCameraInputHandler.Progress(userCamController, dt);
			
			autoCamController.Angle += 20f * dt;
			autoCamController.Progress(dt);
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				var anchors = Enum.GetValues<Orientation2D>();
				var newIdx = Array.IndexOf(anchors, curLockedTextAnchor) + 1;
				if (newIdx >= anchors.Length) newIdx = 0;
				curLockedTextAnchor = anchors[newIdx];
				userControlledScene.Remove(lockedText);
				automaticScene.Remove(lockedText);
				lockedText.Dispose();
				lockedText = factory.ObjectBuilder.CreateCameraLockedTextInstance(
					pen, 
					@string,
					lockedUprightDirection: Direction.Up,
					textInstanceHeight: 0.1f,
					positionAnchor: curLockedTextAnchor
				);
				userControlledScene.Add(lockedText);
				automaticScene.Add(lockedText);
				window.SetTitle(curLockedTextAnchor.ToString());
			}

			compositor.RenderAll();
			window.SetTitle(loop.FramesPerSecondRecentAverage.ToString("N0"));
		}

		automaticScene.RemoveAll();
		userControlledScene.RemoveAll();
		lockedText.Dispose();
		foreach (var lq in lockedQuads) {
			lq.Dispose();
		}
	}
}
