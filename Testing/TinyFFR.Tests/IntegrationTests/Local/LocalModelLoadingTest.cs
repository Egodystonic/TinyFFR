// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Numerics;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalModelLoadingTest {
	string[] _filesToLoad;
	
	[SetUp]
	public void SetUpTest() {
		_filesToLoad = new[] {
			// Color / texturing / basic import tests
			"BoxTextured.gltf",	
			"BoxTextured.glb",
			"BoxTexturedSelfContained.gltf",
			"BoxTexturedNonPowerOfTwo.glb",
			"Box With Spaces.gltf",
			
			// Mesh + normals / tangents / bitangents + transform node walk tests
			"NormalTangentMirrorTest.glb",
			"NegativeScaleTest.glb",
			"TextureCoordinateTest.glb",
			"CompareNormal.glb",
			
			// ORM
			"CompareRoughness.glb",
			"CompareMetallic.glb",
			"MetalRoughSpheres.glb",
			"CompareAmbientOcclusion.glb",
			
			// Aniso
			"AnisotropyStrengthTest.glb",
			"AnisotropyDiscTest.glb",
			
			// Emissive
			"EmissiveStrengthTest.glb",
			
			// AT
			"TransmissionTest.glb",
			"CompareTransmission.glb",
			"TransmissionRoughnessTest.glb",
			"AttenuationTest.glb",
			"CompareIor.glb",
			
			// CC
			"ClearCoatTest.glb",
			
			// Showcase
			"BarramundiFish.glb",
			"Avocado.glb",
			"DamagedHelmet.glb",
			"showcase_ABeautifulGame.glb",
			"showcase_GlassHurricaneCandleHolder.glb",
			"showcase_MaterialsVariantsShoe.glb",
			"showcase_MosquitoInAmber.glb",
			"showcase_PotOfCoals.glb",
			"showcase_ToyCar.glb",
			"showcase_AnisotropyBarnLamp.glb",
			"showcase_CarConcept.glb",
			"showcase_ChronographWatch.glb",
			"showcase_CommercialRefrigerator.glb",
			
			// Stress test
			"NodePerformanceTest.glb",
		};
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "L controls camera light | X/Y/Z rotates models | Press Space");
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -1f));
		camera.NearPlaneDistance = 0.001f;
		var lightBrightnessStage = 3;
		using var light = factory.LightBuilder.CreateSpotLight(position: camera.Position, coneDirection: camera.ViewDirection, highQuality: true);
		using var sunlight = factory.LightBuilder.CreateDirectionalLight(castsShadows: true);
		using var backdrop = factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		using var scene = factory.SceneBuilder.CreateScene();
		scene.SetBackdrop(backdrop);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		renderer.SetQuality(new(Quality.VeryHigh));

		scene.Add(light);
		scene.Add(sunlight);
		
		var curFileIndex = -1;
		ResourceGroup? loadedResources = null; 
		ModelInstanceGroup? modelInstances = null;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var deltaTime = (float) loop.IterateOnce().TotalSeconds;
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				if (modelInstances is {} i) {
					scene.Remove(i);
					i.Dispose();
				}
				
				if (loadedResources is {} g) {
					g.Dispose();
				}
				
				curFileIndex++;
				if (curFileIndex >= _filesToLoad.Length) curFileIndex = 0;
				
				loadedResources = factory.AssetLoader.LoadAll(CommonTestAssets.FindAsset("models/" + _filesToLoad[curFileIndex]), new ModelCreationConfig(), new ModelReadConfig() { HandleUriEscapedStrings = true });

				modelInstances = factory.ObjectBuilder.CreateModelInstanceGroup(loadedResources.Value);
				scene.Add(modelInstances.Value);
				window.SetTitle($"L controls camera light | X/Y/Z rotates models | '{_filesToLoad[curFileIndex]}' ({loadedResources.Value.Models.Count} models / {loadedResources.Value.Meshes.Count} meshes / {loadedResources.Value.Materials.Count} materials / {loadedResources.Value.Textures.Count} textures)");
			}

			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.X)) {
				modelInstances?.RotateBy((90f * deltaTime) % Direction.Left);
			}
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Y)) {
				modelInstances?.RotateBy((90f * deltaTime) % Direction.Up);
			}
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Z)) {
				modelInstances?.RotateBy((90f * deltaTime) % Direction.Forward);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.L)) {
				lightBrightnessStage++;
				if (lightBrightnessStage > 3) lightBrightnessStage = 0;
				light.SetBrightness(lightBrightnessStage switch {
					0 => 0f,
					1 => 0.33f,
					2 => 0.66f,
					_ => 1f
				});
			}
			
			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camera, deltaTime, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camera, deltaTime);
			
			light.Position = camera.Position;
			light.ConeDirection = camera.ViewDirection;
			
			renderer.Render();
		}
	}
}