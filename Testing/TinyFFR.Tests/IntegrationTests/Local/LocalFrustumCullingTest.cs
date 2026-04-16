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
class LocalFrustumCullingTest {
	const int NumMeshes = 100;
	const int NumModelInstances = 30_000;
	
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory(rendererBuilderConfig: new() { EnableVSync = false });
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Frustum Culling ");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		
		var meshes = new Mesh[NumMeshes];
		for (var i = 0; i < NumMeshes; ++i) {
			if ((i & 0b1) == 1) meshes[i] = factory.MeshBuilder.CreateMesh(Cuboid.Random());
			else meshes[i] = factory.MeshBuilder.CreateMesh(Sphere.Random(new(0.5f), new(1f)));
		}

		var instances = new ModelInstance[NumModelInstances];
		for (var i = 0; i < NumModelInstances; ++i) {
			instances[i] = factory.ObjectBuilder.CreateModelInstance(
				meshes[i % NumMeshes],
				mat,
				initialPosition: Location.Origin + (Direction.Random() * RandomUtils.NextSingle(30f, 70f)),
				initialRotation: Rotation.Random(),
				initialScaling: Vect.Random(Vect.One * 0.5f, Vect.One * 1.5f)
			);
			scene.Add(instances[i]);
		}

		var cullingEnabled = true;
		var rotateCamera = true;
		using var loop = factory.ApplicationLoopBuilder.CreateLoop(null);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				renderer.SetFrustumCullingEnabled(cullingEnabled = !cullingEnabled);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.C)) {
				rotateCamera = !rotateCamera;
			}
			
			if (rotateCamera) camera.RotateBy(dt * 120f % Direction.Up);
			window.SetTitle($"Frustum Culling {(cullingEnabled ? "Enabled" : "Disabled")} | Space, C | {loop.FramesPerSecondRecentAverage:N0} FPS average"); 

			renderer.Render();
		}
		
		foreach (var instance in instances) {
			scene.Remove(instance);
			instance.Dispose();
		}
		foreach (var mesh in meshes) mesh.Dispose();
	}
}