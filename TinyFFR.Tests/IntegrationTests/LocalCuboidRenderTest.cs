// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalCuboidRenderTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Recommended!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Cuboid Render Test");
		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(CuboidDescriptor.UnitCube);
		var colorPattern = TexturePattern.ChequerboardBordered(new ColorVect(1f, 1f, 1f), 2, new ColorVect(1f, 0f, 0f), new ColorVect(0f, 1f, 0f), new ColorVect(0f, 0f, 1f), new ColorVect(0.5f, 0.5f, 0.5f), (4, 4));
		var normalPattern = TexturePattern.Rectangles(
			interiorSize: (96, 96),
			borderSize: (15, 15),
			paddingSize: (20, 20),
			interiorValue: Direction.Forward,
			borderRightValue: new Direction(1f, 0f, 1f),
			borderTopValue: new Direction(0f, 1f, 1f),
			borderLeftValue: new Direction(-1f, 0f, 1f),
			borderBottomValue: new Direction(0f, -1f, 1f),
			paddingValue: Direction.Forward,
			repetitions: (4, 4)
		);
		var occlusionPattern = TexturePattern.Chequerboard(0.5f, 1f, 0.8f, (27, 27));
		var roughnessPattern = TexturePattern.Chequerboard(0.8f, 0.4f, 1f, (27, 27));
		var metallicPattern = TexturePattern.Chequerboard(1f, 0f, (27, 27));
		using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(colorPattern);
		using var normalMap = factory.AssetLoader.MaterialBuilder.CreateNormalMap(normalPattern);
		using var ormMap = factory.AssetLoader.MaterialBuilder.CreateOrmMap(occlusionPattern, roughnessPattern, metallicPattern);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(colorMap, normalMap, ormMap: ormMap);
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 2.2f);
		using var light = factory.LightBuilder.CreatePointLight(camera.Position, ColorVect.FromHueSaturationLightness(0f, 0.8f, 0.75f), falloffRange: 10f, brightness: 5000000f);
		using var scene = factory.SceneBuilder.CreateScene();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		scene.Add(instance);
		scene.Add(light);

		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(10.3d)) {
			_ = loop.IterateOnce();
			renderer.Render();

			instance.RotateBy(1.3f % Direction.Up);
			instance.RotateBy(0.8f % Direction.Right);

			light.Color = light.Color.WithHueAdjustedBy(1f);
			light.Position = instance.Position + (((instance.Position >> camera.Position) * 1.2f) * ((MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 5f) * 15f) % Direction.Down));
			light.Position += Direction.Up * MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 8f) * 0.5f;
		}
	}
}