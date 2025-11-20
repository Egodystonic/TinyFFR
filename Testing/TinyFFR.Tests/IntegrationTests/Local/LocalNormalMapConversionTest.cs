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
class LocalNormalMapConversionTest {
	TexturePattern<UnitSphericalCoordinate> _normalPattern;

	[SetUp]
	public void SetUpTest() {
		_normalPattern = TexturePattern.Circles(
			UnitSphericalCoordinate.ZeroZero,
			new UnitSphericalCoordinate(0f, 45f),
			new UnitSphericalCoordinate(90f, 45f),
			new UnitSphericalCoordinate(180f, 45f),
			new UnitSphericalCoordinate(270f, 45f),
			UnitSphericalCoordinate.ZeroZero,
			interiorRadius: 96, borderSize: 24, paddingSize: TexturePatternDefaultValues.CirclesDefaultPaddingSize / 3
		);
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Expectation: Outdent Circles");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var colorMap = factory.AssetLoader.TextureBuilder.CreateColorMap(TexturePattern.PlainFill(ColorVect.White), includeAlpha: false);
		using var normalMap = factory.AssetLoader.TextureBuilder.CreateNormalMap(_normalPattern);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(colorMap, normalMap);
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 2.2f);
		using var light = factory.LightBuilder.CreatePointLight(color: ColorVect.White, maxIlluminationRadius: 2f);
		using var scene = factory.SceneBuilder.CreateScene();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		scene.Add(instance);
		scene.Add(light);
		scene.RemoveBackdrop();

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(8d)) {
			_ = loop.IterateOnce();

			light.Color = light.Color.WithHueAdjustedBy(1f);
			light.Position = instance.Position + Direction.Backward * 1.1f + (Direction.Right * 1f * ((90f * (float) loop.TotalIteratedTime.TotalSeconds) % Direction.Forward));

			renderer.Render();
		}
	}
}