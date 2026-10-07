// Created on 2026-10-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalWorldProjectionTest {
	const int ReticleSizePixels = 72;
	const int ClampedReticleSizePixels = 40;
	const int PipReticleSizePixels = 32;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "World Projection Test");

		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var groundTex = factory.TextureBuilder.CreateColorMap(TexturePattern.Chequerboard(ColorVect.FromRgb24(0x8A919C), ColorVect.FromRgb24(0x5E6570), new XYPair<int>(24, 24)), includeAlpha: false);
		using var groundMat = factory.MaterialBuilder.CreateStandardMaterial(groundTex);
		using var quadMesh = factory.MeshBuilder.CreateQuad();
		using var ground = factory.ObjectBuilder.CreateQuadInstance(quadMesh, groundMat, position: Location.Origin, size: new XYPair<float>(80f, 80f), facingDirection: Direction.Up, uprightDirection: Direction.Forward);
		scene.Add(ground);
		using var sun = factory.LightBuilder.CreateDirectionalLight(new Vect(-0.6f, -1f, 0.75f).Direction, castsShadows: true);
		scene.Add(sun);

		using var sphereMesh = factory.MeshBuilder.CreateSphere(new Sphere(0.4f));
		using var targetTex = factory.TextureBuilder.CreateColorMap(StandardColor.Red, includeAlpha: false);
		using var targetMat = factory.MaterialBuilder.CreateStandardMaterial(targetTex);
		using var target = factory.ObjectBuilder.CreateModelInstance(sphereMesh, targetMat);
		scene.Add(target);

		using var cubeMesh = factory.MeshBuilder.CreateCuboid(new Cuboid(0.8f));
		using var beaconTex = factory.TextureBuilder.CreateColorMap(StandardColor.Blue, includeAlpha: false);
		using var beaconMat = factory.MaterialBuilder.CreateStandardMaterial(beaconTex);
		using var beacon = factory.ObjectBuilder.CreateModelInstance(cubeMesh, beaconMat, initialPosition: new Location(3f, 0.4f, 8f));
		scene.Add(beacon);

		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 1.6f, 0f), Direction.Forward);
		camera.OrthographicHeight = 12f;
		using var mainRenderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		using var pipCamera = factory.CameraBuilder.CreateCamera(new Location(0f, 22f, 2f), new Direction(0f, -1f, 0.05f));
		using var pipRenderer = factory.RendererBuilder.CreateRenderer(scene, pipCamera, window);
		pipRenderer.SetRenderSubAreaFraction(Orientation2D.DownRight, (0.02f, 0.02f), (0.25f, 0.25f));

		var transparent = new ColorVect(0f, 0f, 0f, 0f);
		TexturePattern<ColorVect> RingPattern(ColorVect color) => TexturePattern.Circles(transparent, color, transparent, interiorRadius: 48, borderSize: 10, paddingSize: XYPair<int>.Zero, repetitions: XYPair<int>.One);
		using var onScreenRingTex = factory.TextureBuilder.CreateCanvasTexture(RingPattern(StandardColor.Green), includeAlpha: true);
		using var clampedRingTex = factory.TextureBuilder.CreateCanvasTexture(RingPattern(StandardColor.Red), includeAlpha: true);
		using var pipRingTex = factory.TextureBuilder.CreateCanvasTexture(RingPattern(StandardColor.Yellow), includeAlpha: true);

		using var font = factory.AssetLoader.LoadFont();
		using var pen = font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);

		using var hud = factory.SceneBuilder.CreateCanvasScene();
		using var hudRenderer = factory.RendererBuilder.CreateRenderer(hud, window);
		var targetReticle = CreateReticle(hud, onScreenRingTex);
		var beaconReticle = CreateReticle(hud, onScreenRingTex);
		var targetLabel = CreateLabel(hud, pen, targetReticle);
		var beaconLabel = CreateLabel(hud, pen, beaconReticle);
		var instructions = hud.Add("[Arrows] Turn camera  [P] Toggle perspective/orthographic  [Space] Pause orbit  [Esc] Quit", pen);
		instructions.SetPlacementFraction(Orientation2D.UpLeft, (0.01f, 0.01f), 0.025f);

		using var pipHud = factory.SceneBuilder.CreateCanvasScene();
		using var pipHudRenderer = factory.RendererBuilder.CreateRenderer(pipHud, window);
		pipHudRenderer.SetRenderSubAreaFraction(Orientation2D.DownRight, (0.02f, 0.02f), (0.25f, 0.25f));
		var pipTargetReticle = pipHud.Add(pipRingTex);
		pipTargetReticle.CanvasAnchor = Orientation2D.UpLeft;
		pipTargetReticle.ObjectAnchor = Orientation2D.None;
		pipTargetReticle.WidthPixels = PipReticleSizePixels;
		pipTargetReticle.HeightPixels = PipReticleSizePixels;

		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(mainRenderer, RenderCompositionType.Standard);
		compositor.Add(hudRenderer, RenderCompositionType.RetainPreviousScenes);
		compositor.Add(pipRenderer, RenderCompositionType.Standard);
		compositor.Add(pipHudRenderer, RenderCompositionType.RetainPreviousScenes);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		var orbitAngle = 0f;
		var orbitPaused = false;
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			var kbm = loop.Input.KeyboardAndMouse;

			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.P)) {
				camera.ProjectionType = camera.ProjectionType == CameraProjectionType.Perspective ? CameraProjectionType.Orthographic : CameraProjectionType.Perspective;
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) orbitPaused = !orbitPaused;

			var yawInput = (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowLeft) ? 1f : 0f) - (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowRight) ? 1f : 0f);
			var pitchInput = (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowUp) ? 1f : 0f) - (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowDown) ? 1f : 0f);
			if (yawInput != 0f) camera.RotateBy((90f * yawInput * dt) % Direction.Up);
			if (pitchInput != 0f) camera.RotateBy((60f * pitchInput * dt) % camera.GetRelativeOrientationDirection(Orientation.Left));

			if (!orbitPaused) orbitAngle += 30f * dt;
			var orbitOffset = (Direction.Forward * 6f) * (orbitAngle % Direction.Up);
			target.SetPosition(camera.Position + orbitOffset + Direction.Down * 0.6f);

			UpdateReticle(mainRenderer, target.Position, targetReticle, targetLabel, onScreenRingTex, clampedRingTex);
			UpdateReticle(mainRenderer, beacon.Position, beaconReticle, beaconLabel, onScreenRingTex, clampedRingTex);

			if (pipRenderer.ProjectOnToRenderSubAreaSurfaceFraction(target.Position) is { } pipFraction) {
				pipTargetReticle.IsVisible = true;
				pipTargetReticle.PositionFraction = pipFraction;
			}
			else {
				pipTargetReticle.IsVisible = false;
			}

			compositor.RenderAll();

			var nearPlaneCoord = camera.ProjectOnToNearPlane(target.Position);
			window.SetTitle(
				$"World Projection Test ({camera.ProjectionType}) | Target near-plane coord: {(nearPlaneCoord is { } npc ? $"{npc.X:N2}, {npc.Y:N2}" : "off-screen")} | " +
				$"FPS: {loop.FramesPerSecondRecentAverage:N0}"
			);
		}

		scene.Remove(beacon);
		scene.Remove(target);
		scene.Remove(sun);
		scene.Remove(ground);
	}

	static CanvasImage CreateReticle(CanvasScene canvas, Texture texture) {
		var result = canvas.Add(texture);
		result.CanvasAnchor = Orientation2D.UpLeft;
		result.ObjectAnchor = Orientation2D.None;
		result.WidthPixels = ReticleSizePixels;
		result.HeightPixels = ReticleSizePixels;
		return result;
	}

	static CanvasText CreateLabel(CanvasScene canvas, FontPen pen, CanvasImage reticle) {
		var result = canvas.Add("-", pen);
		result.SetDockParent<CanvasImage>(reticle);
		result.SetPlacementPixels(Orientation2D.Down, (0, -4), 18, objectAnchor: Orientation2D.Up);
		return result;
	}

	static void UpdateReticle(Renderer renderer, Location location, CanvasImage reticle, CanvasText label, Texture onScreenTexture, Texture clampedTexture) {
		const float ClampedIndicatorInset = 0.94f;
		var fraction = renderer.ProjectOnToRenderSurfaceFractionClamped(location, out var wasClamped);
		if (wasClamped) fraction = new XYPair<float>(0.5f + (fraction.X - 0.5f) * ClampedIndicatorInset, 0.5f + (fraction.Y - 0.5f) * ClampedIndicatorInset);
		reticle.PositionFraction = fraction;
		reticle.SetTexture(wasClamped ? clampedTexture : onScreenTexture);
		reticle.WidthPixels = wasClamped ? ClampedReticleSizePixels : ReticleSizePixels;
		reticle.HeightPixels = wasClamped ? ClampedReticleSizePixels : ReticleSizePixels;

		var pixels = renderer.ProjectOnToRenderSurfacePixels(location);
		label.SetText(pixels is { } p ? $"{p.X}, {p.Y}" : "off-screen");
	}
}
