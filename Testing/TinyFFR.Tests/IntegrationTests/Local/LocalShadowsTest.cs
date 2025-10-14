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
using Egodystonic.TinyFFR.Testing;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalShadowsTest {
	const int GridSize = 3;
	const int HalfGridSize = GridSize / 2;
	const float CubeSize = 0.35f;
	static readonly Location FloorPosition = new(0f, -(HalfGridSize + CubeSize), 0f);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Shadows Test", size: (1920, 1080), position: (100, 100));
		using var camera = factory.CameraBuilder.CreateCamera();
		using var cubemap = factory.AssetLoader.LoadEnvironmentCubemap(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		using var scene = factory.SceneBuilder.CreateScene();
		scene.SetBackdrop(cubemap, backdropIntensity: 1f, rotation: 180f % Direction.Forward);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		using var cubeMesh = factory.MeshBuilder.CreateMesh(new Cuboid(CubeSize));
		using var cubeMat = factory.MaterialBuilder.CreateAlphaAwareMaterial(
			colorMap: factory.MaterialBuilder.CreateColorMapWithAlpha(TexturePattern.Chequerboard(
					new ColorVect(1f, 1f, 1f, 1f), 
					new ColorVect(0f, 0f, 0f, 0f)
				)
			),
			type: AlphaMaterialType.ShadowMask
		);
		using var floorMat = factory.MaterialBuilder.CreateAlphaAwareMaterial(factory.MaterialBuilder.CreateColorMapWithAlpha(new ColorVect(0.5f, 0.5f, 0.5f, 0.5f)));

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
			floorMat, 
			initialPosition: new(0f, -(HalfGridSize + CubeSize), 0f), 
			initialScaling: new Vect((GridSize / CubeSize) * 4f, 1f, (GridSize / CubeSize) * 4f)
		);
		scene.Add(floor);

		
		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);

		void ExecSubTest(Action<ILocalTinyFfrFactory, Scene, ApplicationLoop, Renderer, Camera> subTest) {
			try {
				camera.Position = new Location(HalfGridSize, HalfGridSize * 1.5f, -(GridSize + 1));
				camera.HorizontalFieldOfView = 120f;
				camera.LookAt(FloorPosition, Direction.Up);
				loop.ResetTotalIteratedTime();
				subTest(factory, scene, loop, renderer, camera);
			}
			catch (Exception e) {
				Console.WriteLine(e);
				throw;
			}
		}

		ExecSubTest(PointsInBoxes);
		ExecSubTest(SpotlightRotating);
		ExecSubTest(SpotlightsMoving);
		ExecSubTest(DirectionalLongShadows);
		ExecSubTest(DirectionalDynamic);
		ExecSubTest(DoubleScene);

		scene.Remove(floor);
		foreach (var cube in cubeList) {
			scene.Remove(cube);
			cube.Dispose();
		}
		cubeList.Dispose();
	}

	bool PassedTimeFence(float deltaTime, TimeSpan totalTime, TimeSpan timeFence) {
		return totalTime.TotalSeconds >= timeFence.TotalSeconds && totalTime.TotalSeconds - deltaTime < timeFence.TotalSeconds;
	}

	void DirectionalLongShadows(ILocalTinyFfrFactory factory, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
		var lightBuilder = factory.LightBuilder;
		using var directionalLight = lightBuilder.CreateDirectionalLight(
			direction: new Location(0f, CubeSize, GridSize).DirectionTo(Location.Origin),
			color: StandardColor.LightingSunRiseSet,
			showSunDisc: true,
			castsShadows: true
		);
		directionalLight.SetSunDiscParameters(new() { Scaling = 5f });

		scene.Add(directionalLight);
		renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(9d)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;

			directionalLight.RotateBy(4f % Direction.Down * dt);
			directionalLight.Direction = (Location.Origin + directionalLight.Direction * -3f + Direction.Up * dt * 0.02f).DirectionTo(Location.Origin);

			if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(1.5d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.Low });
				directionalLight.AdjustColorHueBy(30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(3d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.Standard });
				directionalLight.AdjustColorHueBy(-30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4.5d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.High });
				directionalLight.AdjustColorHueBy(30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });
				directionalLight.AdjustColorHueBy(-30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(7.5d))) {
				directionalLight.CastsShadows = false;
			}

			renderer.Render();
		}

		scene.Remove(directionalLight);
	}

	void DirectionalDynamic(ILocalTinyFfrFactory factory, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
		var lightBuilder = factory.LightBuilder;
		using var directionalLight = lightBuilder.CreateDirectionalLight(
			direction: new Direction(0.3f, -1f, 0.3f),
			color: StandardColor.Maroon,
			showSunDisc: true,
			castsShadows: true
		);
		directionalLight.SetSunDiscParameters(new() { Scaling = 5f });

		scene.Add(directionalLight);
		renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

		camera.Position = new Location(-HalfGridSize, HalfGridSize, -HalfGridSize);
		camera.LookAt(Location.Origin);
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(13d)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;

			directionalLight.RotateBy(50f % Direction.Forward * dt * (directionalLight.Direction.Y > 0f ? 10f : 1f));
			camera.Position = camera.Position.RotatedAroundOriginBy(-42f % Direction.Down * dt);
			camera.LookAt(Location.Origin, Direction.Up);

			if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(2d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.Low });
				directionalLight.AdjustColorHueBy(30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.Standard });
				directionalLight.AdjustColorHueBy(-30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.High });
				directionalLight.AdjustColorHueBy(30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(8d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });
				directionalLight.AdjustColorHueBy(-30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(10d))) {
				directionalLight.CastsShadows = false;
			}

			renderer.Render();
		}

		scene.Remove(directionalLight);
	}

	void PointsInBoxes(ILocalTinyFfrFactory factory, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
		var lightBuilder = factory.LightBuilder;
		using var pointLightUpper = lightBuilder.CreatePointLight(
			color: StandardColor.LightingSunRiseSet,
			castsShadows: true,
			position: new Location(0f, (HalfGridSize + 2) * CubeSize, HalfGridSize / 2f)
		);
		using var pointLightLower = lightBuilder.CreatePointLight(
			color: StandardColor.White,
			castsShadows: true,
			position: new Location(0f, -(HalfGridSize + 2) * CubeSize, HalfGridSize / -2f)
		);

		scene.Add(pointLightUpper);
		scene.Add(pointLightLower);
		renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(10d)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;

			pointLightUpper.Position = pointLightUpper.Position.RotatedAroundOriginBy(90f % Direction.Down * dt);

			camera.Position = camera.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);
			camera.LookAt(FloorPosition, Direction.Up);

			if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(1.5d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.Low });
				pointLightUpper.AdjustColorHueBy(30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(3d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.Standard });
				pointLightUpper.AdjustColorHueBy(-30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4.5d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.High });
				pointLightUpper.AdjustColorHueBy(30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });
				pointLightUpper.AdjustColorHueBy(-30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(7.5d))) {
				pointLightUpper.CastsShadows = false;
			}

			renderer.Render();
		}

		scene.Remove(pointLightUpper);
		scene.Remove(pointLightLower);
	}

	void SpotlightRotating(ILocalTinyFfrFactory factory, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
		var lightBuilder = factory.LightBuilder;
		using var spotlight = lightBuilder.CreateSpotLight(
			color: StandardColor.LightingSunRiseSet,
			castsShadows: true,
			position: new Location(0f, (HalfGridSize + 2) * CubeSize, 0f),
			coneDirection: new(0.3f, -1f, 0.3f),
			coneAngle: 90f
		);
		using var overhead = lightBuilder.CreateSpotLight(
			color: StandardColor.White,
			castsShadows: false,
			position: new Location(0f, (HalfGridSize + 2) * CubeSize, 0f),
			coneDirection: Direction.Down,
			coneAngle: 160f
		);

		scene.Add(spotlight);
		scene.Add(overhead);
		renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(10d)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;

			spotlight.RotateBy(90f % Direction.Down * dt);

			camera.Position = camera.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);
			camera.LookAt(FloorPosition, Direction.Up);

			if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(1.5d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.Low });
				spotlight.AdjustColorHueBy(30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(3d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.Standard });
				spotlight.AdjustColorHueBy(-30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4.5d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.High });
				spotlight.AdjustColorHueBy(30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });
				spotlight.AdjustColorHueBy(-30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(7.5d))) {
				spotlight.CastsShadows = false;
			}

			renderer.Render();
		}

		scene.Remove(overhead);
		scene.Remove(spotlight);
	}

	void SpotlightsMoving(ILocalTinyFfrFactory factory, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
		var lightBuilder = factory.LightBuilder;
		using var spotlightA = lightBuilder.CreateSpotLight(
			color: StandardColor.LightingSunRiseSet,
			castsShadows: true,
			position: new Location(HalfGridSize * CubeSize, (GridSize + 2) * CubeSize, 0f),
			coneDirection: Direction.Down,
			coneAngle: 110f
		);
		using var spotlightB = lightBuilder.CreateSpotLight(
			color: StandardColor.LightingSunRiseSet,
			castsShadows: true,
			position: new Location(HalfGridSize * -CubeSize, (GridSize + 2) * CubeSize, 0f),
			coneDirection: Direction.Down,
			coneAngle: 110f
		);

		scene.Add(spotlightA);
		scene.Add(spotlightB);
		renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(10d)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;

			spotlightA.Position = spotlightA.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);
			spotlightB.Position = spotlightB.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);

			camera.Position = camera.Position.RotatedAroundOriginBy(45f % Direction.Down * dt);
			camera.LookAt(FloorPosition, Direction.Up);

			if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(1.5d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.Low });
				spotlightA.AdjustColorHueBy(30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(3d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.Standard });
				spotlightA.AdjustColorHueBy(-30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(4.5d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.High });
				spotlightA.AdjustColorHueBy(30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(6d))) {
				renderer.SetQuality(new() { ShadowQuality = Quality.VeryHigh });
				spotlightA.AdjustColorHueBy(-30f);
			}
			else if (PassedTimeFence(dt, loop.TotalIteratedTime, TimeSpan.FromSeconds(7.5d))) {
				spotlightA.CastsShadows = false;
			}

			renderer.Render();
		}

		scene.Remove(spotlightA);
		scene.Remove(spotlightB);
	}

	void DoubleScene(ILocalTinyFfrFactory factory, Scene scene, ApplicationLoop loop, Renderer renderer, Camera camera) {
		var lightBuilder = factory.LightBuilder;
		using var directionalLight = lightBuilder.CreateDirectionalLight(
			direction: new Location(0f, CubeSize, GridSize).DirectionTo(Location.Origin),
			color: StandardColor.LightingSunRiseSet,
			showSunDisc: true,
			castsShadows: true
		);
		directionalLight.SetSunDiscParameters(new() { Scaling = 5f });

		scene.Add(directionalLight);
		renderer.SetQuality(new() { ShadowQuality = Quality.VeryLow });

		using var window2 = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value, size: (200, 200));
		using var scene2 = factory.SceneBuilder.CreateScene(backdropColor: ColorVect.Black);
		using var renderer2 = factory.RendererBuilder.CreateRenderer(scene2, camera, window2, new RendererCreationConfig { AutoUpdateCameraAspectRatio = false, GpuSynchronizationFrameBufferCount = -1 });
		renderer2.SetQuality(new() { ShadowQuality = Quality.VeryHigh });

		using var directionalLight2 = lightBuilder.CreateDirectionalLight(
			direction: new Location(0f, CubeSize, GridSize).DirectionTo(Location.Origin),
			color: StandardColor.Maroon,
			showSunDisc: true,
			castsShadows: true
		);
		directionalLight2.SetSunDiscParameters(new() { Scaling = 5f, FringingScaling = 5f });
		scene2.Add(directionalLight2);

		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(4d)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;

			directionalLight.RotateBy(8f % Direction.Down * dt);
			directionalLight.Direction = (Location.Origin + directionalLight.Direction * -3f + Direction.Up * dt * 0.04f).DirectionTo(Location.Origin);

			directionalLight2.RotateBy(8f % Direction.Down * dt);
			directionalLight2.Direction = (Location.Origin + directionalLight.Direction * -3f + Direction.Up * dt * 0.04f).DirectionTo(Location.Origin);

			camera.RotateBy(5f % Direction.Right * dt);

			renderer.Render();
			renderer2.Render();
		}

		scene.Remove(directionalLight);
		scene2.Remove(directionalLight2);
	}
}