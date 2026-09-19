// Created on 2026-09-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalModelPickingTest {
	const int GridWidth = 12;
	const int GridHeight = 7;
	const float GridSpacing = 1.2f;
	const float MainCameraDistance = 9f;
	const float PipSizeFraction = 0.35f;
	const int PipRateLimitRatio = 4;
	const float PickPointSize = 0.02f;

	static readonly Location PipCameraPosition = new(7f, 8f, -9f);
	static readonly ColorVect UnpickedColor = new(0.35f, 0.37f, 0.45f);
	static readonly ColorVect MainPickColor = new(1f, 0.95f, 0.1f);
	static readonly ColorVect PipSubAreaPickColor = new(0.1f, 0.9f, 1f);
	static readonly ColorVect PipSurfacePickColor = new(1f, 0.1f, 0.65f);

	enum PickSource {
		MainSurface,
		PipSubArea,
		PipSurface
	}

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Model Picking");
		using var mainCamera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -MainCameraDistance), initialViewDirection: Direction.Forward);
		using var pipCamera = factory.CameraBuilder.CreateCamera(PipCameraPosition, initialViewDirection: PipCameraPosition.DirectionTo(Location.Origin));
		using var sunlight = factory.LightBuilder.CreateDirectionalLight(castsShadows: false);
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		scene.Add(sunlight);

		using var mainRenderer = factory.RendererBuilder.CreateRenderer(scene, mainCamera, window, new RendererCreationConfig { GpuSynchronizationFrameBufferCount = 1, Name = "Main Renderer" });
		using var pipRenderer = factory.RendererBuilder.CreateRenderer(scene, pipCamera, window, new RendererCreationConfig { GpuSynchronizationFrameBufferCount = -1, Name = "PiP Renderer" });
		pipRenderer.SetRenderSubAreaFraction(Orientation2D.DownRight, XYPair<float>.Zero, new XYPair<float>(PipSizeFraction, PipSizeFraction));

		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(mainRenderer, RenderCompositionType.Standard);
		compositor.Add(pipRenderer, RenderCompositionType.Standard);
		Assert.AreEqual(window, compositor.TargetWindow);
		Assert.IsNull(compositor.TargetBuffer);

		using var cuboidMesh = factory.MeshBuilder.CreateMesh(
			new Cuboid(0.8f),
			centreTextureOrigin: false,
			new MeshGenerationConfig { TextureTransform = Transform2D.None },
			new MeshCreationConfig { Name = "Picking Test Cuboid" }
		);
		using var sphereMesh = factory.MeshBuilder.CreateMesh(
			new Sphere(0.45f),
			subdivisionLevel: 4,
			new MeshGenerationConfig { TextureTransform = Transform2D.None },
			new MeshCreationConfig { Name = "Picking Test Sphere" }
		);

		var instances = new List<ModelInstance>();
		for (var x = 0; x < GridWidth; ++x) {
			for (var y = 0; y < GridHeight; ++y) {
				var index = (x * GridHeight) + y;
				var instance = factory.ObjectBuilder.CreateModelInstance(
					(index & 0b1) == 0 ? cuboidMesh : sphereMesh,
					initialPosition: new Location(
						(x - (GridWidth - 1) * 0.5f) * GridSpacing,
						(y - (GridHeight - 1) * 0.5f) * GridSpacing,
						0f
					),
					initialRotation: Rotation.Random(),
					initialScaling: Vect.Random(Vect.One * 0.6f, Vect.One * 1f),
					name: "Picking Test Instance " + index
				);
				instance.SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle.Plain3D);
				scene.Add(instance);
				instances.Add(instance);
			}
		}

		var pickedInstances = new ModelInstance?[3];
		var pickPointPrimitives = new ScenePrimitive?[3];
		var pickColors = new[] { MainPickColor, PipSubAreaPickColor, PipSurfacePickColor };
		var pickPointColors = new[] { MainPickColor.WithLightnessAdjustedBy(-0.3f), PipSubAreaPickColor.WithLightnessAdjustedBy(-0.3f), PipSurfacePickColor.WithLightnessAdjustedBy(-0.3f) };
		var pipRateLimited = false;
		var lastPickDescription = "<none>";

		void RecordPick(PickSource source, PixelPickResult? pickResult) {
			var sourceIndex = (int) source;
			pickedInstances[sourceIndex] = pickResult?.ModelInstance;
			if (pickResult is { } pr) {
				if (pickPointPrimitives[sourceIndex] == null) {
					pickPointPrimitives[sourceIndex] = scene.AddPrimitive();
					pickPointPrimitives[sourceIndex]!.Value.SetPaintbrush(new PrimitivePaintbrush(pickPointColors[sourceIndex], ColorVect.BlackOpaque));
				}
				pickPointPrimitives[sourceIndex]!.Value.SetGeometryPoint(pr.Position + (pr.Position >> mainCamera.Position).WithLength(0.1f), PickPointSize, constantScreenSize: true);
				lastPickDescription = $"{source}: {pr.ModelInstance.GetNameAsNewStringObject()} @ {pr.Position}";
			}
			else {
				pickPointPrimitives[sourceIndex]?.Dispose();
				pickPointPrimitives[sourceIndex] = null;
				lastPickDescription = $"{source}: <nothing>";
			}
		}

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			_ = loop.IterateOnce();

			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.R)) {
				pipRateLimited = !pipRateLimited;
				compositor.SetRendererFrameRateRatio(pipRenderer, pipRateLimited ? PipRateLimitRatio : 1);
			}

			var pipLogicalOffset = new XYPair<int>(
				(int) MathF.Round(window.Size.X * (1f - PipSizeFraction)),
				(int) MathF.Round(window.Size.Y * (1f - PipSizeFraction))
			);

			foreach (var click in loop.Input.KeyboardAndMouse.NewMouseClicks) {
				switch (click.Key) {
					case MouseKey.MouseLeft:
						RecordPick(PickSource.MainSurface, mainRenderer.PickModelInstanceFromRenderSurface(click.Location));
						break;
					case MouseKey.MouseRight:
						RecordPick(PickSource.PipSubArea, pipRenderer.PickModelInstanceFromRenderSubAreaSurface(click.Location - pipLogicalOffset));
						break;
					case MouseKey.MouseMiddle:
						RecordPick(PickSource.PipSurface, pipRenderer.PickModelInstanceFromRenderSurface(click.Location));
						break;
				}
			}

			foreach (var instance in instances) {
				var color = UnpickedColor;
				for (var i = 0; i < pickedInstances.Length; ++i) {
					if (pickedInstances[i] is { } picked && picked == instance) color = pickColors[i];
				}
				instance.SetDefaultMaterialBaseColor(color);
			}

			window.SetTitle(
				$"Model Picking | LMB = main (yellow), RMB = PiP sub-area (cyan), MMB = PiP full-surface (magenta; should match RMB) | " +
				$"R = PiP rate limit ({(pipRateLimited ? $"1/{PipRateLimitRatio}" : "off")}) | last: {lastPickDescription}"
			);

			compositor.RenderAll();
		}

		foreach (var primitive in pickPointPrimitives) primitive?.Dispose();
		foreach (var instance in instances) {
			scene.Remove(instance);
			instance.Dispose();
		}
	}
}
