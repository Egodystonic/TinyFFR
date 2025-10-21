// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalCuboidRenderTest {
	TexturePattern<ColorVect> _colorPattern;
	TexturePattern<UnitSphericalCoordinate> _normalPattern;
	TexturePattern<Real> _occlusionPattern;
	TexturePattern<Real> _roughnessPattern;
	TexturePattern<Real> _metallicPattern;

	[SetUp]
	public void SetUpTest() {
		_colorPattern = TexturePattern.ChequerboardBordered(new ColorVect(1f, 1f, 1f), 2, new ColorVect(1f, 0f, 0f), new ColorVect(0f, 1f, 0f), new ColorVect(0f, 0f, 1f), new ColorVect(0.5f, 0.5f, 0.5f), (4, 4));
		_normalPattern = TexturePattern.Circles(
			IMaterialBuilder.DefaultTexelNormal,
			new UnitSphericalCoordinate(0f, 45f),
			new UnitSphericalCoordinate(90f, 45f),
			new UnitSphericalCoordinate(180f, 45f),
			new UnitSphericalCoordinate(270f, 45f),
			IMaterialBuilder.DefaultTexelNormal
		);
		_occlusionPattern = TexturePattern.Chequerboard<Real>(0.5f, 1f, 0.8f, (27, 27));
		_roughnessPattern = TexturePattern.Chequerboard<Real>(0.8f, 0.4f, 1f, (27, 27));
		_metallicPattern = TexturePattern.Chequerboard<Real>(0.4f, 0f, (27, 27));
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Cuboid Render Test");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(_colorPattern);
		using var normalMap = factory.AssetLoader.MaterialBuilder.CreateNormalMap(_normalPattern);
		using var ormMap = factory.AssetLoader.MaterialBuilder.CreateOrmMap(_occlusionPattern, _roughnessPattern, _metallicPattern);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(colorMap, normalMap, ormMap);
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 2.2f);
		using var light = factory.LightBuilder.CreatePointLight(camera.Position, ColorVect.FromHueSaturationLightness(0f, 0.8f, 0.75f));
		var backdropColor = new ColorVect(0f, 1f, 0.7f);
		using var scene = factory.SceneBuilder.CreateScene(backdropColor: backdropColor);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		scene.Add(instance);
		scene.Add(light);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(8d)) {
			_ = loop.IterateOnce();
			renderer.Render();

			instance.RotateBy(1.3f % Direction.Up);
			instance.RotateBy(0.8f % Direction.Right);

			light.Color = light.Color.WithHueAdjustedBy(1f);
			light.Position = instance.Position + (((instance.Position >> camera.Position) * 0.44f) * ((MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 5f) * 15f) % Direction.Down));
			light.Position += Direction.Up * MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 4f) * 0.5f;

			backdropColor = backdropColor.WithHueAdjustedBy(-1f);
			scene.SetBackdrop(backdropColor);
		}
	}
}