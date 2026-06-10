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
class LocalQuadTest {
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Quad Test (key: 1/2/3/4 X/Y)");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var twoSidedInvertQuadMesh = factory.MeshBuilder.CreateQuadMesh(twoSided: true, backSideInvertsTextures: true);
		using var oneSidedInvertQuadMesh = factory.MeshBuilder.CreateQuadMesh(twoSided: false, backSideInvertsTextures: true);
		using var twoSidedNonInvertQuadMesh = factory.MeshBuilder.CreateQuadMesh(twoSided: true, backSideInvertsTextures: false);
		using var oneSidedNonInvertQuadMesh = factory.MeshBuilder.CreateQuadMesh(twoSided: false, backSideInvertsTextures: false);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		using var twoSidedInvertQuadInst = factory.ObjectBuilder.CreateQuadInstance(twoSidedInvertQuadMesh, mat);
		using var oneSidedInvertQuadInst = factory.ObjectBuilder.CreateQuadInstance(oneSidedInvertQuadMesh, mat);
		using var twoSidedNonInvertQuadInst = factory.ObjectBuilder.CreateQuadInstance(twoSidedNonInvertQuadMesh, mat);
		using var oneSidedNonInvertQuadInst = factory.ObjectBuilder.CreateQuadInstance(oneSidedNonInvertQuadMesh, mat);
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		using var camController = camera.CreateController<FreeFlyingCameraController>();
		
		var size = XYPair<float>.One;
		QuadInstance? inst = null;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camController, dt, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camController, dt);
			DefaultCameraInputHandler.Progress(camController, dt);
			
			void UpdateInst(QuadInstance q) {
				inst = q;
				scene.Remove(twoSidedInvertQuadInst);
				scene.Remove(oneSidedInvertQuadInst);
				scene.Remove(twoSidedNonInvertQuadInst);
				scene.Remove(oneSidedNonInvertQuadInst);
				inst?.SetTransform(
					position: camera.Position + camera.ViewDirection * 1f,
					size: size,
					facingDirection: -camera.ViewDirection,
					uprightDirection: Direction.Up
				);
				if (inst == null) return; 
				scene.Add(inst.Value);
				var instName = "";
				if (inst.Value == twoSidedInvertQuadInst) instName = "Two-sided with inverted rear";
				if (inst.Value == oneSidedInvertQuadInst) instName = "One-sided with inverted rear";
				if (inst.Value == twoSidedNonInvertQuadInst) instName = "Two-sided with non-inverted rear";
				if (inst.Value == oneSidedNonInvertQuadInst) instName = "One-sided with non-inverted rear";
				window.SetTitle(instName + " @ " + size);
			}
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow1)) {
				UpdateInst(twoSidedInvertQuadInst);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow2)) {
				UpdateInst(oneSidedInvertQuadInst);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow3)) {
				UpdateInst(twoSidedNonInvertQuadInst);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow4)) {
				UpdateInst(oneSidedNonInvertQuadInst);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.X)) {
				size = size with { X = size.X - 0.1f };
				if (inst.HasValue) UpdateInst(inst.Value);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Y)) {
				size = size with { Y = size.Y - 0.1f };
				if (inst.HasValue) UpdateInst(inst.Value);
			}
			
			renderer.Render();
		}
	}
}