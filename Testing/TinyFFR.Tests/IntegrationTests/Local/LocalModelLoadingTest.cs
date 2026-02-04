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
			"BoxTextured.gltf",	
			"BoxTexturedSelfContained.gltf",
			"BoxTextured.glb",
		};
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Arrows control camera | Press Space");
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 4f, -4f), initialViewDirection: new Direction(0f, -1f, 1f));
		using var light = factory.LightBuilder.CreateSpotLight(position: camera.Position, coneDirection: camera.ViewDirection, castsShadows: true);
		using var scene = factory.SceneBuilder.CreateScene();
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		scene.Add(light);
		
		var curFileIndex = -1;
		var curModelIndex = -1;
		ResourceGroup? group = null; 
		ModelInstance? instance = null;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var deltaTime = (float) loop.IterateOnce().TotalSeconds;
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				if (instance is {} i) {
					scene.Remove(i);
					i.Dispose();
				}
				
				if (group is {} g) {
					curModelIndex++;
					if (curModelIndex >= g.Models.Count) {
						g.Dispose();
					}
					group = null;
				}
				
				if (group == null) {
					curFileIndex++;
					if (curFileIndex >= _filesToLoad.Length) curFileIndex = 0;
					
					Console.WriteLine(_filesToLoad[curFileIndex]);
					group = factory.AssetLoader.LoadModels(CommonTestAssets.FindAsset("models/" + _filesToLoad[curFileIndex]));
					curModelIndex = 0;
				}

				instance = factory.ObjectBuilder.CreateModelInstance(group.Value.Models[curModelIndex]);
				scene.Add(instance.Value);
				window.SetTitle($"Arrows control camera, PgUp/PgDown/Home controls model | '{_filesToLoad[curFileIndex]}' #{curModelIndex}");
			}

			var originToCam = Location.Origin >> camera.Position;
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowRight)) originToCam *= ((-90f * deltaTime) % Direction.Down);
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowLeft)) originToCam *= ((90f * deltaTime) % Direction.Down);
			
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowUp)) originToCam = originToCam.WithLengthDecreasedBy(2f * deltaTime);
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowDown)) originToCam = originToCam.WithLengthIncreasedBy(2f * deltaTime);
			
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.PageUp)) instance?.RotateBy((90f * deltaTime) % Direction.Left);
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.PageDown)) instance?.RotateBy((90f * deltaTime) % Direction.Forward);
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Home)) instance?.SetRotation(Rotation.None);
			
			camera.Position = Location.Origin + originToCam;
			camera.LookAt(Location.Origin, Direction.Up);
			
			light.Position = camera.Position;
			light.ConeDirection = camera.ViewDirection;
			
			renderer.Render();
		}
	}
}