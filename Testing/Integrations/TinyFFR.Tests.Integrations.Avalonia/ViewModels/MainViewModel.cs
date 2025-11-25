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
using Avalonia.Threading;
using Egodystonic.TinyFFR.Avalonia;

namespace TinyFFR.Tests.Integrations.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase {
	List<IDisposable>? _disposables;
	ModelInstance _instance;
	SpotLight _light;

	[ObservableProperty]
	public partial RelayCommand ToggleRenderingButtonPressed { get; set; }

	[ObservableProperty]
	public partial RelayCommand ChangeLightColourButtonPressed { get; set; }

	[ObservableProperty]
	public partial bool Animate { get; set; } = true;

	[ObservableProperty]
	public partial RelayCommand RenderOnce { get; set; }

	[ObservableProperty]
	public partial Renderer? Renderer { get; set; }

	public MainViewModel() {
		ToggleRenderingButtonPressed = new(ToggleRendering);
		ChangeLightColourButtonPressed = new(ChangeLightColour);
		RenderOnce = new(() => Renderer?.Render());
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
		var tex = factory.AssetLoader.LoadColorTexture(factory.AssetLoader.BuiltInTexturePaths.White);
		var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex);
		_instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 2.2f);
		_light = factory.LightBuilder.CreateSpotLight(_instance.Position + Direction.Up * 3f, Direction.Down, 60f, 20f, ColorVect.FromHueSaturationLightness(0f, 0.8f, 0.75f));
		var scene = factory.SceneBuilder.CreateScene(backdropColor: StandardColor.LightingSunMidday);
		scene.SetBackdrop(StandardColor.LightingSunMidday, indirectLightingIntensity: 0f);
		Renderer = factory.RendererBuilder.CreateBindableRenderer(scene, camera, factory.ResourceAllocator);

		scene.Add(_instance);
		scene.Add(_light);

		_disposables.Add(factory);
		_disposables.Add(camera);
		_disposables.Add(mesh);
		_disposables.Add(tex);
		_disposables.Add(mat);
		_disposables.Add(_instance);
		_disposables.Add(_light);
		_disposables.Add(scene);
		_disposables.Add(Renderer);
		_disposables.Add(factory.ApplicationLoopBuilder.StartAvaloniaUiLoop(Tick));

		Renderer.Value.Render();
	}

	void StopRendering() {
		Renderer = null;
		foreach (var d in Enumerable.Reverse(_disposables!)) {
			d.Dispose();
		}
		_disposables = null;
	}

	void Tick(TimeSpan deltaTime) {
		if (Animate) Renderer!.Value.Render();

		_instance.RotateBy((float) deltaTime.TotalSeconds * 130f % Direction.Up);
		_instance.RotateBy((float) deltaTime.TotalSeconds * 80f % Direction.Right);
	}
}
