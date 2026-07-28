// Created on 2026-07-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalScenePrimitivesTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Scene Primitives Test");
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 1.5f, -7f));
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);

		var lime = new ColorVect(0.55f, 1f, 0.2f, 1f);
		var cyan = new ColorVect(0f, 0.8f, 0.9f, 1f);
		var orange = new ColorVect(1f, 0.55f, 0f, 1f);
		var glass = new ColorVect(0.2f, 0.45f, 0.9f, 0.4f);

		const float Y = 1.2f;

		var primitives = new List<ScenePrimitive>();
		ScenePrimitive Track(ScenePrimitive p) {
			primitives.Add(p);
			return p;
		}
		void Label(float x, string text, ColorVect color) => Track(scene.AddPrimitiveString(new Location(x, 0.2f, 0f), text, new PrimitivePaintbrush(color, ColorVect.BlackOpaque), Magnitude.Small));

		// 1. Point (2D billboarded marker, constant screen size)
		Track(scene.AddPrimitivePoint(new Location(-5.25f, Y, 0f), new PrimitivePaintbrush(ColorVect.RedOpaque, ColorVect.BlackOpaque), Magnitude.VeryLarge));
		Label(-5.25f, "Point", ColorVect.RedOpaque);

		// 2. Cuboid (filled)
		Track(scene.AddPrimitiveShape(new PositionedRotatedCuboid(0.7f, 0.7f, 0.7f, new Location(-3.75f, Y, 0f), 30f % Direction.Up), new PrimitivePaintbrush(ColorVect.GreenOpaque)));
		Label(-3.75f, "Cuboid", ColorVect.GreenOpaque);

		// 3. Cuboid (wireframe)
		Track(scene.AddPrimitiveShape(new PositionedRotatedCuboid(0.7f, 0.7f, 0.7f, new Location(-2.25f, Y, 0f), 30f % Direction.Up), new PrimitivePaintbrush(lime), wireframe: true));
		Label(-2.25f, "Cuboid (wire)", lime);

		// 4. Sphere (filled)
		Track(scene.AddPrimitiveShape(new PositionedSphere(0.4f, new Location(-0.75f, Y, 0f)), new PrimitivePaintbrush(ColorVect.BlueOpaque)));
		Label(-0.75f, "Sphere", ColorVect.BlueOpaque);

		// 5. Sphere (wireframe)
		Track(scene.AddPrimitiveShape(new PositionedSphere(0.4f, new Location(0.75f, Y, 0f)), new PrimitivePaintbrush(cyan), wireframe: true));
		Label(0.75f, "Sphere (wire)", cyan);

		// 6. BoundedRay (finite line segment with drawn endpoints)
		Track(scene.AddPrimitiveShape(new BoundedRay(new Location(2.25f, Y - 0.6f, 0f), new Location(2.25f, Y + 0.6f, 0f)), new PrimitivePaintbrush(ColorVect.YellowOpaque, ColorVect.RedOpaque), Magnitude.Large, includeEndpoints: true));
		Label(2.25f, "BoundedRay", ColorVect.YellowOpaque);

		// 7. Ray (infinite half-line with drawn start point)
		Track(scene.AddPrimitiveShape(new Ray(new Location(3.75f, Y - 0.6f, 0f), Direction.Up), new PrimitivePaintbrush(ColorVect.PinkOpaque, ColorVect.WhiteOpaque), Magnitude.Large, includeStartPoint: true));
		Label(3.75f, "Ray", ColorVect.PinkOpaque);

		// 8. Line (infinite in both directions)
		Track(scene.AddPrimitiveShape(new Line(new Location(5.25f, Y, 0f), Direction.Up), new PrimitivePaintbrush(orange), Magnitude.Large));
		Label(5.25f, "Line", orange);

		// 9. Plane (semi-transparent back wall)
		Track(scene.AddPrimitiveShape(new Plane(Direction.Backward, new Location(0f, 1.5f, 3f)), new PrimitivePaintbrush(glass)));

		// 10. Grid (floor, default red major / white minor / grey background)
		Track(scene.AddPrimitiveGrid(Location.Origin, gridSize: 14f));

		// 11. String (title billboard)
		Track(scene.AddPrimitiveString(new Location(0f, 3f, 0f), "Scene Primitives", new PrimitivePaintbrush(ColorVect.WhiteOpaque, ColorVect.BlackOpaque), Magnitude.VeryLarge));

		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		using var camController = camera.CreateController<InspectorCameraController>();
		camController.AllowUpsideDownFlip = true;
		camController.MaxDistance = 20f;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camController, dt, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camController, dt);
			DefaultCameraInputHandler.Progress(camController, dt);

			renderer.Render();
			window.SetTitle(loop.FramesPerSecondRecentAverage.ToString("N0"));
		}

		foreach (var p in primitives) p.Dispose();
	}
}
