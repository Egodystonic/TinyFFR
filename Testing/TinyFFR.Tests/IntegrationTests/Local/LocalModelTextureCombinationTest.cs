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
class LocalModelTextureCombinationTest {
	string[] _filesToLoad;
	
	[SetUp]
	public void SetUpTest() {
		_filesToLoad = new[] {
			"CompareRoughness.glb",
			"CompareMetallic.glb"
		};
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		var curStrategy = TextureCombinationScalingStrategy.RepeatingTile;
		
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display);
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -1f));
		camera.NearPlaneDistance = 0.001f;
		using var light = factory.LightBuilder.CreateSpotLight(position: camera.Position, brightness: 0.3f, coneDirection: camera.ViewDirection, highQuality: true);
		using var sunlight = factory.LightBuilder.CreateDirectionalLight(castsShadows: false);
		using var backdrop = factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		using var scene = factory.SceneBuilder.CreateScene();
		scene.SetBackdrop(backdrop);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		renderer.SetQuality(new(Quality.VeryHigh));

		scene.Add(light);
		scene.Add(sunlight);
		
		var allResourcesLoaded = new List<ResourceGroup>();
		
		void LoadNextExample() {
			foreach (var g in ((IEnumerable<ResourceGroup>) allResourcesLoaded).Reverse()) {
				foreach (var modelInstance in g.ModelInstances) scene.Remove(modelInstance);
				g.Dispose();
			}
			allResourcesLoaded.Clear();
			
			var offset = Location.Origin;
			foreach (var file in _filesToLoad) {
				var models = factory.AssetLoader.LoadAll(CommonTestAssets.FindAsset("models/" + file), new ModelCreationConfig { }, new ModelReadConfig { EmbeddedTextureMapScalingStrategy = curStrategy });
				var instances = factory.ObjectBuilder.CreateModelInstanceGroup(models, initialPosition: offset);
				scene.Add(instances);
				offset += Direction.Down * 1f;
				
				allResourcesLoaded.Add(models);
				allResourcesLoaded.Add(instances.UnderlyingResourceGroup);
			}
			
			window.SetTitle("Space to switch strategy. Current: " + curStrategy);
			curStrategy = (TextureCombinationScalingStrategy) (((int) curStrategy) + 1);
			if (!Enum.IsDefined(curStrategy)) curStrategy = 0;
		}
		
		LoadNextExample();

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var deltaTime = (float) loop.IterateOnce().TotalSeconds;
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				LoadNextExample();
			}
			
			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camera, deltaTime, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camera, deltaTime);
			
			light.Position = camera.Position;
			light.ConeDirection = camera.ViewDirection;
			
			renderer.Render();
		}
	}
}