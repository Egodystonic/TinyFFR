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
		protected T? CycleValue<T>(T? val, params T?[] options) where T : struct, IToleranceEquatable<T> {
			for (var i = 0; i < options.Length; ++i) {
				if ((val == null && options[i] == null) || (val != null && options[i] != null && val.Value.Equals(options[i]!.Value, 0.001f))) {
					return options[(i + 1) % options.Length];
				}
			}
			
			return options[0];
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
			new PtzScenario(factory, camera, mesh, mat, scene),	
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
		
		public OrbitalScenario(ILocalTinyFfrFactory factory, Camera camera, Mesh testMesh, Material testMat, Scene scene) : base(factory, camera, testMesh, testMat, scene) { }

		public override void Start() {
			Smoothing = Strength.Standard;
			_staticInstances = Enumerable.Range(0, 4).Select(_ => AddTestModelToScene()).ToArray();
			_targetInstance = AddTestModelToScene();
			_controller = Camera.CreateController<OrbitalCameraController>();
			
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
				$"[1] Angle {_controller.Angle:N0} (max {_controller.MaxAngleDiffFromZero?.ToString() ?? "<none>"}) " +
				$"[2] Height {_controller.Height:N2} (min {_controller.MinHeight?.ToString("N2") ?? "<none>"} max {_controller.MaxHeight?.ToString("N2") ?? "<none>"}) " +
				$"[3] Distance {_controller.Distance:N2} (min {_controller.MinDistance?.ToString("N2") ?? "<none>"} max {_controller.MaxDistance?.ToString("N2") ?? "<none>"}) " +
				$"[0] Smoothing {Smoothing}";
		}

		public override void Iterate(float dt, ILatestInputRetriever input) {
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow1)) {
				_controller.MaxAngleDiffFromZero = CycleValue(_controller.MaxAngleDiffFromZero, null, 180f, 90f, 20f);
			}
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow2)) {
				_controller.MinHeight = CycleValue<Real>(_controller.MinHeight, OrbitalCameraController.DefaultHeightMin, OrbitalCameraController.DefaultHeightMin * 2f, OrbitalCameraController.DefaultHeightMin * 0.2f, null);
				_controller.MaxHeight = CycleValue<Real>(_controller.MaxHeight, OrbitalCameraController.DefaultHeightMax, OrbitalCameraController.DefaultHeightMax * 2f, OrbitalCameraController.DefaultHeightMax * 0.2f, null);
			}
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow3)) {
				_controller.MinDistance = CycleValue<Real>(_controller.MinDistance, OrbitalCameraController.DefaultDistanceMin, OrbitalCameraController.DefaultDistanceMin * 2f, OrbitalCameraController.DefaultDistanceMin * 0.2f, null);
				_controller.MaxDistance = CycleValue<Real>(_controller.MaxDistance, OrbitalCameraController.DefaultDistanceMax, OrbitalCameraController.DefaultDistanceMax * 2f, OrbitalCameraController.DefaultDistanceMax * 0.2f, null);
			}
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow0)) {
				CycleSmoothing();
				_controller.SetGlobalSmoothing(Smoothing);
			}
			
			_targetInstance.RotateBy(90f % Direction.Up * dt, Location.Origin);

			_controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, dt);
			_controller.AdjustAllViaDefaultControls(input.GameControllersCombined, dt);
			
			_controller.Target = _targetInstance.Position;
			_controller.Progress(dt);
		}
	}
	
	sealed class PtzScenario : CameraControllerScenario {
		PanTiltZoomCameraController _controller = null!;
		ModelInstance _modelInstance;
		
		public PtzScenario(ILocalTinyFfrFactory factory, Camera camera, Mesh testMesh, Material testMat, Scene scene) : base(factory, camera, testMesh, testMat, scene) { }

		public override void Start() {
			Smoothing = Strength.Standard;
			_modelInstance = AddTestModelToScene();
			_controller = Camera.CreateController<PanTiltZoomCameraController>();
			_controller.Position = (0f, 1f, -2f);
			_controller.ZeroPanTiltDirection = _controller.Position.DirectionTo(Location.Origin);
			_controller.UpDirection = Direction.Up;
			_modelInstance.SetPosition(Location.Origin);
		}
		public override void Stop() {
			_controller.Dispose();
			RemoveAndDispose(_modelInstance);
		}
		public override string GetWindowTitleString() {
			return 
				$"[1] Pan {_controller.Pan:N0} (max {_controller.PanRange?.ToString() ?? "<none>"}) " +
				$"[2] Tilt {_controller.Tilt:N2} (min {_controller.MaxTiltUp.ToString("N2", null)} max {_controller.MaxTiltDown.ToString("N2", null)}) " +
				$"[3] Zoom {PercentageUtils.ConvertFractionToPercentageString(_controller.Zoom)} (min {_controller.HighestZoomFov.ToString("N2", null)} max {_controller.LowestZoomFov.ToString("N2", null)} " +
				$"[0] Smoothing {Smoothing}";
		}

		public override void Iterate(float dt, ILatestInputRetriever input) {
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow1)) {
				_controller.PanRange = CycleValue(_controller.PanRange, PanTiltZoomCameraController.DefaultPanRangeDegrees, 90f, 20f, null);
			}
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow2)) {
				_controller.MaxTiltDown = CycleValue<Angle>(_controller.MaxTiltDown, PanTiltZoomCameraController.DefaultMaxTiltDownDegrees, PanTiltZoomCameraController.DefaultMaxTiltDownDegrees * 0.5f, PanTiltZoomCameraController.DefaultMaxTiltDownDegrees * 2f)!.Value;
				_controller.MaxTiltUp = CycleValue<Angle>(_controller.MaxTiltUp, PanTiltZoomCameraController.DefaultMaxTiltUpDegrees, PanTiltZoomCameraController.DefaultMaxTiltUpDegrees * 0.5f, PanTiltZoomCameraController.DefaultMaxTiltUpDegrees * 2f)!.Value;
			}
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow3)) {
				_controller.HighestZoomFov = CycleValue<Angle>(_controller.HighestZoomFov, PanTiltZoomCameraController.DefaultHighestZoomFov, PanTiltZoomCameraController.DefaultHighestZoomFov * 0.5f, PanTiltZoomCameraController.DefaultHighestZoomFov * 1.3f)!.Value;
				_controller.LowestZoomFov = CycleValue<Angle>(_controller.LowestZoomFov, PanTiltZoomCameraController.DefaultLowestZoomFov, PanTiltZoomCameraController.DefaultLowestZoomFov * 0.5f, PanTiltZoomCameraController.DefaultLowestZoomFov * 1.3f)!.Value;
			}
			if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow0)) {
				CycleSmoothing();
				_controller.SetGlobalSmoothing(Smoothing);
			}
			_controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, dt);
			_controller.AdjustAllViaDefaultControls(input.GameControllersCombined, dt);
			
			_controller.Progress(dt);
		}
	}
}