// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalColorspaceTest {
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();

		using var linearWallAlbedo = factory.AssetLoader.LoadTexture(
			CommonTestAssets.FindAsset(KnownTestAsset.BrickAlbedoTex),
			isLinearColorspace: true,
			name: "linearWallTex"
		);
		using var srgbWallAlbedo = factory.AssetLoader.LoadTexture(
			CommonTestAssets.FindAsset(KnownTestAsset.BrickAlbedoTex),
			isLinearColorspace: false,
			name: "srgbWallTex"
		);
		using var wallNormals = factory.AssetLoader.LoadNormalMap(
			CommonTestAssets.FindAsset(KnownTestAsset.BrickNormalTex)
		);
		using var wallOrm = factory.AssetLoader.LoadOcclusionRoughnessMetallicMap(
			CommonTestAssets.FindAsset(KnownTestAsset.BrickOrmTex)
		);

		using var linearWallMat = factory.MaterialBuilder.CreateStandardMaterial(
			linearWallAlbedo,
			wallNormals,
			wallOrm,
			name: "linearwall"
		);
		using var srgbWallMat = factory.MaterialBuilder.CreateStandardMaterial(
			srgbWallAlbedo,
			wallNormals,
			wallOrm,
			name: "srgbwall"
		);

		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Colorspace Test");
		using var camera = factory.CameraBuilder.CreateCamera((0f, 2f, -2f));
		using var mesh = factory.MeshBuilder.CreateMesh(new Cuboid(1f));
		using var cubemap = factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));

		using var linearInstance = factory.ObjectBuilder.CreateModelInstance(mesh, linearWallMat, initialPosition: (0.8f, 0f, 0f));
		using var srgbInstance = factory.ObjectBuilder.CreateModelInstance(mesh, srgbWallMat, initialPosition: (-0.8f, 0f, 0f));
		using var scene = factory.SceneBuilder.CreateScene();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		var lights = factory.ResourceAllocator.CreatePooledMemoryBuffer<PointLight>(6);
		for (var i = 0; i < 6; ++i) {
			lights.Span[i] = factory.LightBuilder.CreatePointLight(
				Location.Origin + (new Vect(0f, 3f, 3f) * (Direction.Down % (60f * i))),
				ColorVect.FromHueSaturationLightness(60f * i, 1f, 0.5f),
				brightness: 2f,
				castsShadows: true
			);
			scene.Add(lights.Span[i]);
		}

		scene.Add(linearInstance);
		scene.Add(srgbInstance);
		scene.SetBackdrop(cubemap, 1f);

		var originToCam = camera.Position << Location.Origin;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(null);
		while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(10d)) {
			_ = loop.IterateOnce();
			renderer.Render();

			camera.Position = Location.Origin + (originToCam * (Direction.Down % 18f * ((float) loop.TotalIteratedTime.TotalSeconds - 5f)));
			camera.SetViewAndUpDirection((camera.Position >> Location.Origin).Direction, Direction.Up);

			for (var i = 0; i < 6; ++i) {
				lights.Span[i].Position = Location.Origin + (new Vect(0f, 3f, 3f) * (Direction.Down % (((float) loop.TotalIteratedTime.TotalSeconds * 120f) + 60f * i)));
			}
		}

		foreach (var light in lights.Span) {
			scene.Remove(light);
			light.Dispose();
		}
		factory.ResourceAllocator.ReturnPooledMemoryBuffer(lights);
	}
}