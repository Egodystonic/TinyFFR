// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalCameraRayCreationTest {
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Keys: Mouse1 / Space / Enter");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin + Direction.Backward * 2f);
		
		using var scene = factory.SceneBuilder.CreateScene();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		ScenePrimitive? sceneRay = null;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(null);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var deltaTime = (float) loop.IterateOnce().TotalSeconds;
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Return)) {
				var curProjectionStyle = camera.ProjectionType;
				if (curProjectionStyle == CameraProjectionType.Orthographic) camera.SetProjectionType(CameraProjectionType.Perspective);
				else camera.SetProjectionType(CameraProjectionType.Orthographic);
			}

			foreach (var mc in loop.Input.KeyboardAndMouse.NewMouseClicks) {
				var ray = renderer.CastRayFromRenderSurface(mc.Location);
				ray = ray.MovedBy(ray.Direction * 0.01f);
				sceneRay?.Dispose();
				sceneRay = scene.AddPrimitiveShape(ray, constantScreenSize: false);
			}
			
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space)) {
				camera.Position = Location.Origin + (Location.Origin >> camera.Position) * ((20f * deltaTime) % Direction.Down);
				camera.LookAt(Location.Origin, Direction.Up);
			}
			
			renderer.Render();
		}
	}
}