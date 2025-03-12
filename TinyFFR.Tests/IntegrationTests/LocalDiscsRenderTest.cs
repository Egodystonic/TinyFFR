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
class LocalDiscsRenderTest {
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	Polygon CreatePolygon(Direction normal, bool isWoundClockwise, params Location[] vertices) => new(vertices, normal, isWoundClockwise);
	Polygon CreateDisc(Direction normal) => CreatePolygon(normal, false, Enumerable.Range(0, 100).Select(i => normal.AnyOrthogonal().AsVect().RotatedBy((normal % 90f).ScaledBy(i / 25f)).AsLocation()).ToArray());

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Recommended!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Disc Render Test");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var polyGroup = factory.AssetLoader.MeshBuilder.AllocateNewPolygonGroup();
		foreach (var cardinal in OrientationUtils.AllCardinals) {
			polyGroup.Add(
				CreateDisc(cardinal.ToDirection()),
				cardinal.ToDirection().AnyOrthogonal(),
				Direction.FromDualOrthogonalization(cardinal.ToDirection(), cardinal.ToDirection().AnyOrthogonal()),
				Location.Origin
			);
		} 
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(polyGroup, new Transform2D(scaling: new(2f), rotation: 45f));
		using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(
			TexturePattern.Lines(ColorVect.FromStandardColor(StandardColor.White), ColorVect.FromStandardColor(StandardColor.Silver), true, perturbationMagnitude: 0.1f)
		);
		using var normalMap = factory.AssetLoader.MaterialBuilder.CreateNormalMap(
			TexturePattern.Lines(new Direction(0f, 1f, 1f), new Direction(0f, -1f, 1f), false, perturbationMagnitude: 0.1f)
		);
		using var ormMap = factory.AssetLoader.MaterialBuilder.CreateOrmMap();
		using var mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(colorMap, normalMap, ormMap);
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 5.2f);
		using var light = factory.LightBuilder.CreatePointLight(instance.Position, ColorVect.FromHueSaturationLightness(0f, 0.8f, 0.75f));
		using var scene = factory.SceneBuilder.CreateScene(includeBackdrop: false);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		scene.Add(instance);
		scene.Add(light);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(8d)) {
			_ = loop.IterateOnce();
			renderer.Render();

			instance.MoveBy(Direction.Backward * 0.007f);
			instance.RotateBy(1.9f % Direction.Up);
			instance.RotateBy(-1.4f % Direction.Right);
			light.Position = instance.Position + Direction.Backward * 0.6f;

			light.Color = light.Color.WithHueAdjustedBy(1f);
		}
	}
}