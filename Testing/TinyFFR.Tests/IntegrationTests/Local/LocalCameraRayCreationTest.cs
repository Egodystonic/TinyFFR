// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalCameraRayCreationTest {
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Click Mouse and/or Hold Space");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin + Direction.Backward * 2f);
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		
		var activeInstances = new ModelInstance[10];
		for (var i = 0; i < activeInstances.Length; ++i) {
			activeInstances[i] = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialScaling: new Vect(0.04f));
		}
		
		using var light = factory.LightBuilder.CreateSpotLight(camera.Position, camera.ViewDirection, castsShadows: true, highQuality: true);
		using var scene = factory.SceneBuilder.CreateScene();
		scene.SetBackdrop(StandardColor.LightingAmbientOvercast, 0.3f);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		scene.Add(light);
		for (var i = 0; i < activeInstances.Length; ++i) {
			scene.Add(activeInstances[i]);
		}

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(null);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var deltaTime = (float) loop.IterateOnce().TotalSeconds;

			foreach (var mc in loop.Input.KeyboardAndMouse.NewMouseClicks) {
				var ray = renderer.CastRayFromRenderSurface(mc.Location);
				for (var i = 0; i < activeInstances.Length; ++i) {
					activeInstances[i].SetPosition(ray.UnboundedLocationAtDistance(0.2f * (i + 1)));
				}
			}
			
			if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space)) {
				camera.Position = Location.Origin + (Location.Origin >> camera.Position) * ((20f * deltaTime) % Direction.Down);
				camera.LookAt(Location.Origin, Direction.Up);
				light.Position = camera.Position;
				light.ConeDirection = camera.ViewDirection;	
			}
			
			renderer.Render();
		}
		
		for (var i = 0; i < activeInstances.Length; ++i) {
			scene.Remove(activeInstances[i]);
			activeInstances[i].Dispose();
		}
	}
}