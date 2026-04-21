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
class LocalCameraControllerTest {
	abstract class CameraControllerScenario {
		protected readonly ILocalTinyFfrFactory Factory;
		protected readonly Camera Camera;
		protected readonly Mesh TestMesh;
		protected readonly Material TestMat;
		protected readonly Scene Scene;
		protected Strength Smoothing;

		protected CameraControllerScenario(ILocalTinyFfrFactory factory, Camera camera, Mesh testMesh, Material testMat, Scene scene) {
			Factory = factory;
			Camera = camera;
			TestMesh = testMesh;
			TestMat = testMat;
			Scene = scene;
		}

		public abstract void Start();
		public abstract void Stop();
		public abstract void Iterate(float dt, ILatestInputRetriever input);
		public abstract string GetWindowTitleString();
		
		protected ModelInstance AddTestModelToScene() {
			var result = Factory.ObjectBuilder.CreateModelInstance(TestMesh, TestMat);
			result.Scaling = Vect.One * 0.3f;
			Scene.Add(result);
			return result;
		}
		protected void RemoveAndDispose(ModelInstance i) {
			Scene.Remove(i);
			i.Dispose();
		}
		// protected ModelInstanceGroup AddTestModelsToScene(int count) {
		// 	var group = Factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		// 	for (var i = 0; i < count; ++i) {
		// 		group.Add(Factory.ObjectBuilder.CreateModelInstance(TestMesh, TestMat));
		// 	}
		// 	var result = Factory.ObjectBuilder.CreateModelInstanceGroup(group);
		// 	result.Scaling = Vect.One * 0.3f;
		// 	Scene.Add(result);
		// 	return result;
		// }
		// protected void RemoveAndDispose(ModelInstanceGroup g) {
		// 	Scene.Remove(g);
		// 	g.Dispose();
		// }
		protected void CycleValue<T>(ref T? val, params T?[] options) where T : struct, IToleranceEquatable<T> {
			for (var i = 0; i < options.Length; ++i) {
				if ((val == null && options[i] == null) || (val != null && options[i] != null && val.Value.Equals(options[i]!.Value, 0.001f))) {
					val = options[(i + 1) % options.Length];
					return;
				}
			}
			
			val = options[0];
		}
		protected void CycleSmoothing() {
			Smoothing = (Strength) ((int) Smoothing + 1);
			if (!Enum.IsDefined(Smoothing)) Smoothing = 0;
		}
	}

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "press space | Space to change scenario, num keys to change params");
		window.LockCursor = true;
		using var camera = factory.CameraBuilder.CreateCamera();
		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		
		var scenarios = new CameraControllerScenario[] {
			new OrbitalScenario(factory, camera, mesh, mat, scene)	
		};
		var scenarioIndex = -1;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop(null);
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				scenarioIndex++;
				if (scenarioIndex != 0) {
					scenarios[scenarioIndex - 1].Stop();
				}
				if (scenarioIndex == scenarios.Length) scenarioIndex = 0;
				scenarios[scenarioIndex].Start();
			}
			
			if (scenarioIndex >= 0) {
				scenarios[scenarioIndex].Iterate(dt, loop.Input);
				window.SetTitle(scenarios[scenarioIndex].GetType().Name + " | " + scenarios[scenarioIndex].GetWindowTitleString());				
			}
			
			
			renderer.Render();
		}
		
		if (scenarioIndex >= 0) scenarios[scenarioIndex].Stop();
	}
	
	sealed class OrbitalScenario : CameraControllerScenario {
		OrbitalCameraController _controller = null!;
		ModelInstance[] _staticInstances = null!;
		ModelInstance _targetInstance;
		
		Angle? _maxAngle;
		Real? _minHeight;
		Real? _maxHeight;
		Real? _minDistance;
		Real? _maxDistance;
		
		public OrbitalScenario(ILocalTinyFfrFactory factory, Camera camera, Mesh testMesh, Material testMat, Scene scene) : base(factory, camera, testMesh, testMat, scene) { }

		public override void Start() {
			Smoothing = Strength.Standard;
			_staticInstances = Enumerable.Range(0, 4).Select(_ => AddTestModelToScene()).ToArray();
			_targetInstance = AddTestModelToScene();
			_controller = Camera.CreateController<OrbitalCameraController>();
			
			_maxAngle = null;
			_minHeight = OrbitalCameraController.DefaultHeightMin;
			_maxHeight = OrbitalCameraController.DefaultHeightMax;
			_minDistance = OrbitalCameraController.DefaultDistanceMin;
			_maxDistance = OrbitalCameraController.DefaultDistanceMax;
			
			_staticInstances[0].SetPosition(new Location(-0.5f, 0f, 0f));
			_staticInstances[1].SetPosition(new Location(0.5f, 0f, 0f));
			_staticInstances[2].SetPosition(new Location(0, 0f, -0.5f));
			_staticInstances[3].SetPosition(new Location(0, 0f, 0.5f));
			_targetInstance.SetPosition(Location.Origin + Direction.Forward * 1f);
		}
		public override void Stop() {
			_controller.Dispose();
			foreach (var si in _staticInstances) RemoveAndDispose(si);
			RemoveAndDispose(_targetInstance);
		}
		public override string GetWindowTitleString() {
			return 
				$"[1] Angle {_controller.Angle:N0} (max {_maxAngle?.ToString() ?? "<none>"}) " +
				$"[2] Height {_controller.Height:N2} (min {_minHeight?.ToString("N2") ?? "<none>"} max {_maxHeight?.ToString("N2") ?? "<none>"}) " +
				$"[3] Distance {_controller.Distance:N2} (min {_minDistance?.ToString("N2") ?? "<none>"} max {_maxDistance?.ToString("N2") ?? "<none>"}) " +
				$"[0] Smoothing {Smoothing}";
		}

		public override void Iterate(float dt, ILatestInputRetriever input) {
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow1)) {
				CycleValue(ref _maxAngle, null, 180f, 90f, 20f);
			}
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow2)) {
				CycleValue(ref _minHeight, OrbitalCameraController.DefaultHeightMin, OrbitalCameraController.DefaultHeightMin * 2f, OrbitalCameraController.DefaultHeightMin * 0.2f, null);
				CycleValue(ref _maxHeight, OrbitalCameraController.DefaultHeightMax, OrbitalCameraController.DefaultHeightMax * 2f, OrbitalCameraController.DefaultHeightMax * 0.2f, null);
			}
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow3)) {
				CycleValue(ref _minDistance, OrbitalCameraController.DefaultDistanceMin, OrbitalCameraController.DefaultDistanceMin * 2f, OrbitalCameraController.DefaultDistanceMin * 0.2f, null);
				CycleValue(ref _maxDistance, OrbitalCameraController.DefaultDistanceMax, OrbitalCameraController.DefaultDistanceMax * 2f, OrbitalCameraController.DefaultDistanceMax * 0.2f, null);
			}
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow0)) {
				CycleSmoothing();
				_controller.SetGlobalSmoothing(Smoothing);
			}
			
			_targetInstance.RotateBy(90f % Direction.Up * dt, Location.Origin);

			_controller.AdjustAngleViaMouseCursor(input.KeyboardAndMouse.MouseCursorDelta, 0.02f, maxAngleDiffFromZero: _maxAngle);
			_controller.AdjustHeightViaMouseCursor(input.KeyboardAndMouse.MouseCursorDelta, 0.0001f, minHeight: _minHeight, maxHeight: _maxHeight);
			_controller.AdjustDistanceViaMouseWheel(input.KeyboardAndMouse.MouseScrollWheelDelta, 0.015f, minDistance: _minDistance, maxDistance: _maxDistance);
			_controller.AdjustAngleViaControllerStick(input.GameControllersCombined.LeftStickPosition, 120f, dt, maxAngleDiffFromZero: _maxAngle);
			_controller.AdjustHeightViaControllerStick(input.GameControllersCombined.LeftStickPosition, 0.5f, dt, minHeight: _minHeight, maxHeight: _maxHeight);
			_controller.AdjustDistanceViaControllerTriggers(input.GameControllersCombined.LeftTriggerPosition, input.GameControllersCombined.RightTriggerPosition, 0.5f, dt, minDistance: _minDistance, maxDistance: _maxDistance);
			
			_controller.Target = _targetInstance.Position;
			_controller.Progress(dt);
		}
	}
}