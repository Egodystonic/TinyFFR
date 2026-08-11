// Created on 2026-08-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Numerics;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

// ReSharper disable AccessToDisposedClosure
[TestFixture, Explicit]
class LocalSceneCompositingTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory(rendererBuilderConfig: new RendererBuilderConfig { EnableVSync = false });
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Compositor Frame Rate Limit Test");

		using var backdropScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var backdropCamera = factory.CameraBuilder.CreateCamera();
		using var backdropRenderer = factory.RendererBuilder.CreateRenderer(backdropScene, backdropCamera, window);

		using var cubeScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
		using var cubeCamera = factory.CameraBuilder.CreateCamera();
		using var cubeRenderer = factory.RendererBuilder.CreateRenderer(cubeScene, cubeCamera, window);
		using var cubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var cubeTex = factory.TextureBuilder.CreateColorMap(StandardColor.Red, false);
		using var cubeMat = factory.MaterialBuilder.CreateStandardMaterial(cubeTex);
		using var cubeInstances = factory.ObjectBuilder.GroupModelInstances(
			Enumerable.Range(0, 6).Select(_ => factory.ObjectBuilder.CreateModelInstance(cubeMesh, cubeMat, Location.Origin + Direction.Random(new Plane(Direction.Up)) * 3f)).ToArray()
		);
		using var cubeLight = factory.LightBuilder.CreatePointLight(maxIlluminationRadius: 20f);
		cubeScene.Add(cubeInstances);
		cubeScene.Add(cubeLight);

		using var pipScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Starfield);
		using var pipCamera = factory.CameraBuilder.CreateCamera();
		using var pipRenderer = factory.RendererBuilder.CreateRenderer(pipScene, pipCamera, window);
		using var pipMesh = factory.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere);
		using var pipTex = factory.TextureBuilder.CreateColorMap(StandardColor.Green, false);
		using var pipMat = factory.MaterialBuilder.CreateStandardMaterial(pipTex);
		using var pipInstances = factory.ObjectBuilder.GroupModelInstances(
			Enumerable.Range(0, 6).Select(_ => factory.ObjectBuilder.CreateModelInstance(pipMesh, pipMat, Location.Origin + Direction.Random(new Plane(Direction.Up)) * 3f)).ToArray()
		);
		using var pipLight = factory.LightBuilder.CreatePointLight(maxIlluminationRadius: 20f);
		pipScene.Add(pipInstances);
		pipScene.Add(pipLight);
		pipRenderer.SetRenderSubAreaFraction(Orientation2D.Right, (0.05f, 0.0f), (0.25f, 0.25f));

		using var hudScene = factory.SceneBuilder.CreateCanvasScene();
		using var hudRenderer = factory.RendererBuilder.CreateRenderer(hudScene, window);
		using var font = factory.AssetLoader.LoadFont();
		using var fontPen = font.CreatePen(BuiltInFontPenStyle.Default);

		var hudUpdateCount = 0;
		using var hudCounterText = hudScene.Add("HUD 0", fontPen);
		hudCounterText.SetPlacementFraction(Orientation2D.UpLeft, (0.02f, 0.02f), 0.05f);
		using var hudInstructionsText = hudScene.Add("[1] Backdrop  [2] Cubes  [3] PiP  [4] HUD  -  cycle rate limit  |  [Esc] Quit", fontPen);
		hudInstructionsText.SetPlacementFraction(Orientation2D.Down, (0f, 0.02f), 0.022f);
		using var hudPickingText = hudScene.Add("Cursor: -", fontPen);
		hudPickingText.SetPlacementFraction(Orientation2D.UpLeft, (0.02f, 0.09f), 0.03f);

		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(backdropRenderer, RenderCompositionType.Standard);
		compositor.Add(cubeRenderer, RenderCompositionType.RetainPreviousScenes);
		compositor.Add(pipRenderer, RenderCompositionType.Standard);
		compositor.Add(hudRenderer, RenderCompositionType.RetainPreviousScenes);
		backdropRenderer.SetQuality(BuiltInQualityConfiguration.Ultra);
		cubeRenderer.SetQuality(BuiltInQualityConfiguration.Ultra);
		pipRenderer.SetQuality(BuiltInQualityConfiguration.Ultra);

		var rateLimitCycle = new (int? Cap, int Ratio)[] {
			(null, 1), (60, 1), (10, 1), (null, 4), (null, 10), (60, 4), (60, 10), (10, 4), (10, 10)
		};
		var cycledRenderers = new[] { backdropRenderer, cubeRenderer, pipRenderer, hudRenderer };
		var cycledRendererNames = new[] { "Backdrop", "Cubes", "PiP", "HUD" };
		var cycledRendererKeys = new[] {
			KeyboardOrMouseKey.NumberRow1, KeyboardOrMouseKey.NumberRow2,
			KeyboardOrMouseKey.NumberRow3, KeyboardOrMouseKey.NumberRow4
		};
		var rateLimitCycleIndices = new int[cycledRenderers.Length];

		void ApplyRateLimitCycleState(int rendererIndex) {
			var (cap, ratio) = rateLimitCycle[rateLimitCycleIndices[rendererIndex]];
			compositor.SetRendererFrameRateCap(cycledRenderers[rendererIndex], cap);
			compositor.SetRendererFrameRateRatio(cycledRenderers[rendererIndex], ratio);
		}

		static string DescribeRateLimit(int? cap, int ratio) {
			if (cap == null) return ratio <= 1 ? "unlimited" : $"1/{ratio}";
			return ratio <= 1 ? $"{cap}Hz" : $"{cap}Hz 1/{ratio}";
		}

		foreach (var (cap, ratio) in rateLimitCycle) {
			compositor.SetRendererFrameRateCap(hudRenderer, cap);
			compositor.SetRendererFrameRateRatio(hudRenderer, ratio);
			Assert.AreEqual(cap, compositor.GetRendererFrameRateCap(hudRenderer));
			Assert.AreEqual(ratio, compositor.GetRendererFrameRateRatio(hudRenderer));
		}
		for (var i = 0; i < cycledRenderers.Length; ++i) ApplyRateLimitCycleState(i);

		Assert.AreEqual(null, compositor.GetRendererFrameRateCap(hudRenderer));
		Assert.AreEqual(1, compositor.GetRendererFrameRateRatio(hudRenderer));
		Assert.AreEqual(null, compositor.GetRendererFrameRateCap(cubeRenderer));
		Assert.Throws<ArgumentOutOfRangeException>(() => compositor.SetRendererFrameRateCap(hudRenderer, 0));
		Assert.Throws<ArgumentOutOfRangeException>(() => compositor.SetRendererFrameRateRatio(hudRenderer, 0));

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			var kbm = loop.Input.KeyboardAndMouse;

			for (var i = 0; i < cycledRenderers.Length; ++i) {
				if (!kbm.KeyWasPressedThisIteration(cycledRendererKeys[i])) continue;
				rateLimitCycleIndices[i] = (rateLimitCycleIndices[i] + 1) % rateLimitCycle.Length;
				ApplyRateLimitCycleState(i);
			}

			cubeCamera.RotateBy(36f % Direction.Up * dt);
			pipCamera.RotateBy(36f % Direction.Up * -dt);

			hudUpdateCount++;
			hudCounterText.SetText($"HUD {hudUpdateCount}");

			var cursorLocalCoord = hudScene.ConvertRenderTargetCoordToLocal(kbm.MouseCursorPosition);
			hudPickingText.SetText($"Cursor: {cursorLocalCoord} | Over counter: {hudCounterText.Contains(cursorLocalCoord)}");

			compositor.RenderAll();

			var rateLimitSummary = String.Join(" | ", cycledRenderers.Select((r, i) =>
				$"{cycledRendererNames[i]}: {DescribeRateLimit(compositor.GetRendererFrameRateCap(r), compositor.GetRendererFrameRateRatio(r))}"));

			window.SetTitle(
				$"FPS: {loop.FramesPerSecondRecentAverage:0000} avg, {loop.FramesPerSecondRecentMin:0000} min, {loop.FramesPerSecondRecentMax:0000} max | " +
				rateLimitSummary
			);
		}

		pipScene.Remove(pipLight);
		pipScene.Remove(pipInstances);
		cubeScene.Remove(cubeLight);
		cubeScene.Remove(cubeInstances);
	}
}
