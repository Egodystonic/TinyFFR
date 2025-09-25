using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Egodystonic.TinyFFR.Avalonia;

namespace TinyFFR.Tests.Integrations.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase {
	List<IDisposable>? _disposables;
	CancellationTokenSource? _tickStopCts;
	ModelInstance _instance;
	ApplicationLoop _loop;
	PointLight _light;

	[ObservableProperty]
	public partial RelayCommand ToggleRenderingButtonPressed { get; set; }

	[ObservableProperty]
	public partial RelayCommand ChangeLightColourButtonPressed { get; set; }

	[ObservableProperty]
	public partial Renderer? Renderer { get; set; }

	public MainViewModel() {
		ToggleRenderingButtonPressed = new(ToggleRendering);
		ChangeLightColourButtonPressed = new(ChangeLightColour);
	}

	void ToggleRendering() {
		if (_disposables == null) StartRendering();
		else StopRendering();
	}

	void ChangeLightColour() {
		if (_disposables == null) return;
		_light.AdjustColorHueBy(30f);
	}

	void StartRendering() {
		_disposables = new List<IDisposable>();

		var factory = new LocalTinyFfrFactory();
		var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		var mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial();
		_instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 2.2f);
		_light = factory.LightBuilder.CreatePointLight(camera.Position, ColorVect.FromHueSaturationLightness(0f, 0.8f, 0.75f));
		var scene = factory.SceneBuilder.CreateScene(backdropColor: StandardColor.LightingSunMidday);
		Renderer = factory.RendererBuilder.CreateBindableRenderer(scene, camera, factory.ResourceAllocator);

		scene.Add(_instance);
		scene.Add(_light);

		_loop = factory.ApplicationLoopBuilder.CreateLoop(60);

		_tickStopCts = new CancellationTokenSource();

		_loop.BeginIteratingOnUiThread(Tick, _tickStopCts.Token);

		_disposables.Add(factory);
		_disposables.Add(camera);
		_disposables.Add(mesh);
		_disposables.Add(mat);
		_disposables.Add(_instance);
		_disposables.Add(_light);
		_disposables.Add(scene);
		_disposables.Add(Renderer);
		_disposables.Add(_loop);

		Renderer.Value.Render();
	}

	void StopRendering() {
		Renderer = null;
		_tickStopCts!.Cancel();
		_tickStopCts = null;
		foreach (var d in Enumerable.Reverse(_disposables!)) {
			d.Dispose();
		}
		_disposables = null;
	}

	void Tick(TimeSpan deltaTime) {
		Renderer!.Value.Render();

		_instance.RotateBy(1.3f % Direction.Up);
		_instance.RotateBy(0.8f % Direction.Right);

		foreach (var newEvent in _loop.Input.GameControllersCombined.NewButtonEvents) {
			Debug.WriteLine(newEvent);
		}
	}
}
