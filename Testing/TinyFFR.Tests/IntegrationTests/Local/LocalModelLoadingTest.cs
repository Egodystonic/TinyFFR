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
			"NegativeScaleTest.glb",
			"CompareIor.glb",
			"AttenuationTest.glb",
			"CompareRoughness.glb",
			"MetalRoughSpheres.glb",
			"CompareMetallic.glb",
			"TransmissionRoughnessTest.glb",
			"EmissiveStrengthTest.glb",
			"CompareTransmission.glb",
			"TransmissionTest.glb",
			"BoxTextured.gltf",	
			"BarramundiFish.glb",
			"CompareAmbientOcclusion.glb",
			"CompareNormal.glb",
			"Avocado.glb",
			"TextureCoordinateTest.glb",
			"NormalTangentMirrorTest.glb",
			"ClearCoatTest.glb",
			"DamagedHelmet.glb",
			"AnisotropyStrengthTest.glb",
			"AnisotropyDiscTest.glb",
			"BoxTexturedSelfContained.gltf",
			"BoxTextured.glb",
			"BoxTexturedNonPowerOfTwo.glb",
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
		var lightBrightnessStage = 3;
		using var light = factory.LightBuilder.CreateSpotLight(position: camera.Position, coneDirection: camera.ViewDirection, castsShadows: true, highQuality: true);
		using var backdrop = factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		using var scene = factory.SceneBuilder.CreateScene();
		scene.SetBackdrop(backdrop);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		scene.Add(light);
		
		var curFileIndex = -1;
		ResourceGroup? group = null; 
		List<ModelInstance> instances = new();

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var deltaTime = (float) loop.IterateOnce().TotalSeconds;
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				foreach (var instance in instances) {
					scene.Remove(instance);
					instance.Dispose();
				}
				instances.Clear();
				
				if (group is {} g) {
					g.Dispose();
				}
				
				curFileIndex++;
				if (curFileIndex >= _filesToLoad.Length) curFileIndex = 0;
				
				Console.WriteLine(_filesToLoad[curFileIndex]);
				group = factory.AssetLoader.LoadModels(CommonTestAssets.FindAsset("models/" + _filesToLoad[curFileIndex]));

				foreach (var model in group.Value.Models) {
					instances.Add(factory.ObjectBuilder.CreateModelInstance(model));
					scene.Add(instances[^1]);
				}
				window.SetTitle($"L controls camera light | X/Y/Z rotates models | '{_filesToLoad[curFileIndex]}' ({group.Value.Models.Count} models / {group.Value.Meshes.Count} meshes / {group.Value.Materials.Count} materials / {group.Value.Textures.Count} textures)");
			}

			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.X)) {
				foreach (var instance in instances) instance.RotateBy((90f * deltaTime) % Direction.Left);
			}
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Y)) {
				foreach (var instance in instances) instance.RotateBy((90f * deltaTime) % Direction.Up);
			}
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Z)) {
				foreach (var instance in instances) instance.RotateBy((90f * deltaTime) % Direction.Forward);
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