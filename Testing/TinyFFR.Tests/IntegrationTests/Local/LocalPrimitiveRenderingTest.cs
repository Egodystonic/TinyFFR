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
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalPrimitiveRenderingTest {
	const int NumInstances = 1000;
	const float MinDistanceForRandomObjects = 2f;
	const float MaxDistanceForRandomObjects = 100f;
	
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Primitive Rendering Test");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var sphereMesh = factory.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere, subdivisionLevel: 3, config: new MeshCreationConfig { GenerateWireframeData = true }, generationConfig: new());
		using var cubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, centreTextureOrigin: false, config: new MeshCreationConfig { GenerateWireframeData = true }, generationConfig: new());
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Starfield);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		using var camController = camera.CreateController<InspectorCameraController>();
		camController.MinDistance = 1f;
		camController.MaxDistance = 4f;
		camController.Distance = 1.5f;
		
		var instances = new ModelInstance[NumInstances];
		instances[0] = factory.ObjectBuilder.CreateModelInstance(sphereMesh);
		instances[0].SetNullMaterialBaseColor(ColorVect.RandomOpaque());
		scene.Add(instances[0]);
		for (var i = 1; i < NumInstances; ++i) {
			instances[i] = factory.ObjectBuilder.CreateModelInstance(sphereMesh, initialScaling: new(0.3f), initialPosition: Location.Origin + Direction.Random() * RandomUtils.NextSingle(MinDistanceForRandomObjects, MaxDistanceForRandomObjects));
			if (RandomUtils.GlobalRng.Next(2) == 1) instances[i].SetNullMaterialBaseColor(ColorVect.Random().WithPremultipliedAlpha());
			else instances[i].SetNullMaterialBaseColor(ColorVect.RandomOpaque());
			scene.Add(instances[i]);
		}
		
		var usingSphereMesh = true;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camController, dt, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camController, dt);
			DefaultCameraInputHandler.Progress(camController, dt);
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				for (var i = 0; i < NumInstances; ++i) {
					if (RandomUtils.GlobalRng.Next(2) == 1) instances[i].SetNullMaterialBaseColor(ColorVect.Random().WithPremultipliedAlpha());
					else instances[i].SetNullMaterialBaseColor(ColorVect.RandomOpaque());
				}
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.S)) {
				usingSphereMesh = !usingSphereMesh;
				for (var i = 0; i < NumInstances; ++i) {
					instances[i].SetMesh(usingSphereMesh ? sphereMesh : cubeMesh);
				}
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow0)) {
				for (var i = 0; i < NumInstances; ++i) {
					instances[i].SetNullMaterialShadingStyle(NullMaterialShadingStyle.Plain);
				}
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow1)) {
				for (var i = 0; i < NumInstances; ++i) {
					instances[i].SetNullMaterialShadingStyle(NullMaterialShadingStyle.Plain3D);
				}
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow2)) {
				for (var i = 0; i < NumInstances; ++i) {
					instances[i].SetNullMaterialShadingStyle(NullMaterialShadingStyle.Wireframe);
				}
			}
			
			window.SetTitle(loop.FramesPerSecondRecentAverage.ToString("N0"));
			renderer.Render();
		}

		for (var i = 0; i < NumInstances; ++i) {
			scene.Remove(instances[i]);
			instances[i].Dispose();
		}
	}
}