// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

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
		var display = factory.DisplayDiscoverer.Primary!.Value;
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
		using var colorMap = factory.AssetLoader.TextureBuilder.CreateColorMap(
			TexturePattern.Lines(ColorVect.FromStandardColor(StandardColor.White), ColorVect.FromStandardColor(StandardColor.Silver), true, perturbationMagnitude: 0.1f), includeAlpha: false
		);
		using var normalMap = factory.AssetLoader.TextureBuilder.CreateNormalMap(TexturePattern.PlainFill(SphericalTranslation.ZeroZero));
		using var ormMap = factory.AssetLoader.TextureBuilder.CreateOcclusionRoughnessMetallicMap(
			TexturePattern.Lines<Real>(0f, 1f, false, perturbationMagnitude: 0.1f),
			TexturePattern.Lines<Real>(1f, 0f, false, perturbationMagnitude: 0.1f),
			TexturePattern.PlainFill<Real>(0f)
		);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(colorMap, normalMap, ormMap);
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 5.2f);
		using var light = factory.LightBuilder.CreateSpotLight(camera.Position, camera.ViewDirection, color: ColorVect.FromHueSaturationLightness(0f, 0.8f, 0.75f), coneAngle: 25f, intenseBeamAngle: 10f, brightness: 0.4f, highQuality: true);
		using var sunlight = factory.LightBuilder.CreateDirectionalLight(new Direction(0f, 0f, -1f), StandardColor.LightingSunRiseSet, showSunDisc: true, brightness: 1f);
		using var scene = factory.SceneBuilder.CreateScene();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		scene.SetBackdrop(StandardColor.Black);
		scene.Add(instance);
		scene.Add(light);
		scene.Add(sunlight);
		sunlight.SetSunDiscParameters(new() { Scaling = 10f, FringingScaling = 1.5f });

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(8.6d)) {
			var dt = (float) loop.IterateOnce().TotalSeconds;
			renderer.Render();

			instance.MoveBy(Direction.Backward * 0.01f);
			instance.RotateBy(2f * 0.19f % Direction.Up);
			instance.RotateBy(2f * -0.14f % Direction.Right);
			light.Position = camera.Position;
			light.ConeDirection = (light.Position >> instance.Position).Direction * new Rotation(10f * MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 2f), Direction.Down);
			sunlight.RotateBy(new Rotation(200f * dt, Direction.Down));

			light.Color = light.Color.WithHueAdjustedBy(1f);

			if (loop.TotalIteratedTime > TimeSpan.FromSeconds(5d) && loop.TotalIteratedTime - TimeSpan.FromSeconds(dt) < TimeSpan.FromSeconds(5d)) {
				window.Size = window.Size.ScaledByReal(1.2f);
			}
		}
	}
}