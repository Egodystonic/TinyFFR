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
class LocalMipmappingTest {
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();

		using var mipmappedWallAlbedo = factory.AssetLoader.LoadTexture(
			CommonTestAssets.FindAsset(KnownTestAsset.BrickAlbedoTex),
			new TextureCreationConfig {
				GenerateMipMaps = true,
				IsLinearColorspace = false
			}
		);
		using var nonMipmappedWallAlbedo = factory.AssetLoader.LoadTexture(
			CommonTestAssets.FindAsset(KnownTestAsset.BrickAlbedoTex),
			new TextureCreationConfig {
				GenerateMipMaps = false,
				IsLinearColorspace = false
			}
		);
		using var wallNormals = factory.AssetLoader.LoadNormalMap(
			CommonTestAssets.FindAsset(KnownTestAsset.BrickNormalTex)
		);
		using var wallOrm = factory.AssetLoader.LoadOcclusionRoughnessMetallicMap(
			CommonTestAssets.FindAsset(KnownTestAsset.BrickOrmTex)
		);

		using var mipmappedWallMat = factory.MaterialBuilder.CreateStandardMaterial(
			mipmappedWallAlbedo,
			wallNormals,
			wallOrm
		);
		using var nonMipmappedWallMat = factory.MaterialBuilder.CreateStandardMaterial(
			nonMipmappedWallAlbedo,
			wallNormals,
			wallOrm
		);

		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Press Space (Mipmap = OFF)");
		window.Size = display.CurrentResolution.ScaledByReal(0.9f);
		using var camera = factory.CameraBuilder.CreateCamera(initialViewDirection: new Direction(0f, -0.3f, 1f));
		using var mesh = factory.MeshBuilder.CreateMesh(new Cuboid(300f, 0.05f, 1000f));

		using var mipmapInstance = factory.ObjectBuilder.CreateModelInstance(mesh, mipmappedWallMat, initialPosition: (0f, -0.8f, 0f));
		using var nonMipmapInstance = factory.ObjectBuilder.CreateModelInstance(mesh, nonMipmappedWallMat, initialPosition: (0f, -0.8f, 0f));
		using var light = factory.LightBuilder.CreateDirectionalLight(Direction.Down);
		using var scene = factory.SceneBuilder.CreateScene();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		scene.Add(light);
		scene.Add(nonMipmapInstance);
		var currentlyMipmapped = false;
		
		using var loop = factory.ApplicationLoopBuilder.CreateLoop(null);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			_ = loop.IterateOnce();
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				if (currentlyMipmapped) {
					scene.Remove(mipmapInstance);
					scene.Add(nonMipmapInstance);
					window.SetTitle("Press Space (Mipmap = OFF)");
				}
				else {
					scene.Remove(nonMipmapInstance);
					scene.Add(mipmapInstance);
					window.SetTitle("Press Space (Mipmap = ON)");
				}
				
				currentlyMipmapped = !currentlyMipmapped;
			}
			
			renderer.Render();
		}
	}
}