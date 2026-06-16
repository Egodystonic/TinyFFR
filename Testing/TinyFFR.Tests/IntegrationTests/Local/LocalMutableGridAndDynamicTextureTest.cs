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
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalMutableGridAndDynamicTextureTest {
	static readonly XYPair<int> Dimensions = (1024, 1024);
	
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Mutable Grid & Dynamic Texture Test");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		
		var texels = new TexelRgb24[Dimensions.Area];
		texels.AsSpan().Fill(new TexelRgb24(255, 255, 255));
		
		using var texture = factory.TextureBuilder.CreateTexture(texels, new TextureGenerationConfig { Dimensions = Dimensions }, new TextureCreationConfig { AllowsDynamicWrites = true, IsLinearColorspace = false });
		using var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(texture);
		
		using var mesh = factory.MeshBuilder.CreateMutableGridMesh(Dimensions);
		using var instance = factory.ObjectBuilder.CreateMutableGridInstance(mesh, mat);
		
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		using var camController = camera.CreateController<FreeFlyingCameraController>();
		scene.Add(instance);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camController, dt, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camController, dt);
			DefaultCameraInputHandler.Progress(camController, dt);
			
			renderer.Render();
		}
		
		scene.Remove(instance);
	}
}