// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using System.Drawing;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalShadowsTest {
	const int GridSize = 3;
	const int HalfGridSize = GridSize / 2;
	const float CubeSize = 0.35f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Recommended!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Shadows Test", size: (1920, 1080), position: (100, 100));
		using var camera = factory.CameraBuilder.CreateCamera();
		using var scene = factory.SceneBuilder.CreateScene(backdropColor: ColorVect.Black);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		using var cubeMesh = factory.MeshBuilder.CreateMesh(new Cuboid(CubeSize));
		using var cubeMat = factory.MaterialBuilder.CreateOpaqueMaterial(
			colorMap: factory.MaterialBuilder.CreateColorMap(StandardColor.White)
		);

		var cubeList = factory.ResourceAllocator.CreateNewArrayPoolBackedList<ModelInstance>();
		
		for (var x = -HalfGridSize; x <= HalfGridSize; ++x) {
			for (var y = -HalfGridSize; y <= HalfGridSize; ++y) {
				for (var z = -HalfGridSize; z <= HalfGridSize; ++z) {
					var cube = factory.ObjectBuilder.CreateModelInstance(cubeMesh, cubeMat, initialPosition: new(x, y, z));
					scene.Add(cube);
					cubeList.Add(cube);
				}
			}
		}

		using var floor = factory.ObjectBuilder.CreateModelInstance(
			cubeMesh, 
			cubeMat, 
			initialPosition: new(0f, -(HalfGridSize + CubeSize), 0f), 
			initialScaling: new Vect((GridSize / CubeSize) * 2f, 1f, (GridSize / CubeSize) * 2f)
		);
		scene.Add(floor);

		
		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);

		ResetCameraAndLoop(loop, camera, floor.Position);
		ExecuteDirectionalLightTest(factory.LightBuilder, scene, loop, renderer, camera);

		scene.Remove(floor);
		foreach (var cube in cubeList) {
			scene.Remove(cube);
			cube.Dispose();
		}
		cubeList.Dispose();
	}

	void ResetCameraAndLoop(ApplicationLoop loop, Camera camera, Location lookAtPos) {
		camera.Position = new Location(HalfGridSize, HalfGridSize * 1.5f, -(GridSize + 1));
		camera.HorizontalFieldOfView = 120f;
		camera.LookAt(lookAtPos);
		loop.ResetTotalIteratedTime();
	}

	void ExecuteDirectionalLightTest(ILightBuilder lightBuilder, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
		using var directionalLight = lightBuilder.CreateDirectionalLight(
			direction: new Location(0f, CubeSize, GridSize).DirectionTo(Location.Origin),
			color: StandardColor.LightingSunRiseSet,
			showSunDisc: true
		);
		directionalLight.SetSunDiscParameters(new() { Scaling = 5f });

		scene.Add(directionalLight);

		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(7d)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;

			directionalLight.RotateBy(4f % Direction.Down * dt);
			directionalLight.Direction = (Location.Origin + directionalLight.Direction * -3f + Direction.Up * dt * 0.02f).DirectionTo(Location.Origin);

			renderer.Render();
		}

		directionalLight.Color = StandardColor.Maroon;
		directionalLight.Direction = new Direction(0.3f, -1f, 0.3f);

		loop.ResetTotalIteratedTime();
		camera.Position = new Location(-HalfGridSize, HalfGridSize, -HalfGridSize);
		camera.LookAt(Location.Origin);
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(11d)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;

			directionalLight.RotateBy(40f % Direction.Forward * dt * (directionalLight.Direction.Y > 0f ? 10f : 1f));
			camera.Position = camera.Position.RotatedAroundOriginBy(-48f % Direction.Down * dt);
			camera.LookAt(Location.Origin, Direction.Up);

			renderer.Render();
		}

		scene.Remove(directionalLight);
	}
}