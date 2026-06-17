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
	static readonly XYPair<int> Dimensions = (256, 256);
	
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		MutableGridMesh? mesh = null;
		MutableGridInstance? instance = null;
		
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Keys: [XYUVO2]");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		
		var texels = new TexelRgb24[Dimensions.Area];
		texels.AsSpan().Fill(new TexelRgb24(255, 255, 255));
		
		using var texture = factory.TextureBuilder.CreateTexture(texels, new TextureGenerationConfig { Dimensions = Dimensions }, new TextureCreationConfig { AllowsDynamicWrites = true, IsLinearColorspace = false });
		using var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(texture);
		using var testMat = factory.MaterialBuilder.CreateTestMaterial();
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		
		var flipX = false;
		var flipY = false;
		var flipUp = false;
		var origin = Orientation2D.None;
		var twoSided = true;
		var useUvMap = false;
		
		void ResetInstance() {
			if (instance != null) {
				scene.Remove(instance.Value);
				instance.Value.Dispose();
				mesh!.Value.Dispose();
			}
			
			mesh = factory.MeshBuilder.CreateMutableGridMesh(
				Dimensions,
				twoSided,
				flipX ? -IMeshBuilder.DefaultMutableGridMeshXDir : IMeshBuilder.DefaultMutableGridMeshXDir,
				flipY ? -IMeshBuilder.DefaultMutableGridMeshYDir : IMeshBuilder.DefaultMutableGridMeshYDir,
				flipUp ? -IMeshBuilder.DefaultMutableGridMeshUpDir : IMeshBuilder.DefaultMutableGridMeshUpDir,
				null,
				origin,
				100f
			);
			instance = factory.ObjectBuilder.CreateMutableGridInstance(mesh.Value, useUvMap ? testMat : mat);
			scene.Add(instance.Value);
			
			window.SetTitle(
				$"Keys: [XYUO2] | " +
				$"{(!flipX ? "X=Default" : "X=Flipped")} " +
				$"{(!flipY ? "Y=Default" : "Y=Flipped")} " +
				$"{(!flipUp ? "Up=Default" : "Up=Flipped")} " +
				$"Origin={origin} " +
				$"{(twoSided ? "Sides=2" : "Sides=1")} " +
				$"{(useUvMap ? "Tex=Dynamic" : "Tex=UV")}"
			);
		}
		
		ResetInstance();
		
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		using var camController = camera.CreateController<FreeFlyingCameraController>();

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.X)) {
				flipX = !flipX;
				ResetInstance();
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Y)) {
				flipY = !flipY;
				ResetInstance();
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.U)) {
				flipUp = !flipUp;
				ResetInstance();
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.V)) {
				useUvMap = !useUvMap;
				ResetInstance();
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.O)) {
				var values = Enum.GetValues<Orientation2D>();
				var newIndex = Array.IndexOf(values, origin) + 1;
				if (newIndex >= values.Length) newIndex = 0;
				origin = values[newIndex];
				ResetInstance();
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow2)) {
				twoSided = !twoSided;
				ResetInstance();
			}
			
			if (instance is { } i) {
				using var lease = i.BorrowVerticesSpan(true);
				for (var y = 0; y < Dimensions.Y; ++y) {
					for (var x = 0; x < Dimensions.X; ++x) {
						var index = Dimensions.Index(x, y);
						var normalizedCoord = i.GetVertexCoordinateNormalized(index);
						texels[index] = new TexelRgb24(new ColorVect(
							(normalizedCoord.X + 1f) * 0.5f,
							(normalizedCoord.Y + 1f) * 0.5f,
							0.5f
						));
					}
				}
				texture.OverwriteTexels(texels);
			}

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camController, dt, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camController, dt);
			DefaultCameraInputHandler.Progress(camController, dt);
			
			renderer.Render();
		}
		
		if (instance != null) {
			scene.Remove(instance.Value);
			instance.Value.Dispose();
			mesh!.Value.Dispose();
		}
	}
}