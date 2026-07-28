using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using Egodystonic.TinyFFR.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyFFR.Tests.Integrations.Wpf {
	public partial class MainWindowViewModel : ObservableObject {
		List<IDisposable>? _disposables;
		ModelInstance _instance;
		ModelInstance _overlayInstance;
		PointLight _light;
		Renderer _overlayRenderer;
		bool _overlayEnabled;

		[ObservableProperty]
		public partial RelayCommand ToggleRenderingButtonPressed { get; set; }

		[ObservableProperty]
		public partial RelayCommand ChangeLightColourButtonPressed { get; set; }

		[ObservableProperty]
		public partial RelayCommand ToggleOverlayButtonPressed { get; set; }

		[ObservableProperty]
		public partial bool UseCompositor { get; set; }

		[ObservableProperty]
		public partial Renderer? Renderer { get; set; }

		[ObservableProperty]
		public partial RendererCompositor? Compositor { get; set; }

		public MainWindowViewModel() {
			ToggleRenderingButtonPressed = new(ToggleRendering);
			ChangeLightColourButtonPressed = new(ChangeLightColour);
			ToggleOverlayButtonPressed = new(ToggleOverlay);
		}

		void ToggleRendering() {
			if (_disposables == null) StartRendering();
			else StopRendering();
		}

		void ChangeLightColour() {
			if (_disposables == null) return;
			_light.AdjustColorHueBy(30f);
		}

		void ToggleOverlay() {
			if (Compositor is not { } compositor) return;
			_overlayEnabled = !_overlayEnabled;
			compositor.SetEnabledState(_overlayRenderer, _overlayEnabled);
		}

		void StartRendering() {
			_disposables = new List<IDisposable>();

			var factory = new LocalTinyFfrFactory();
			var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
			var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
			var tex = factory.AssetLoader.LoadColorMap(factory.AssetLoader.BuiltInTexturePaths.White);
			var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex);
			_instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 2.2f);
			_light = factory.LightBuilder.CreatePointLight(camera.Position, ColorVect.FromHueSaturationLightness(0f, 0.8f, 0.75f));
			var scene = factory.SceneBuilder.CreateScene(backdropColor: StandardColor.LightingSunMidday);
			var renderer = factory.RendererBuilder.CreateBindableRenderer(scene, camera, factory.ResourceAllocator);

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
			_disposables.Add(renderer);

			if (UseCompositor) {
				var overlayCamera = factory.CameraBuilder.CreateCamera(Location.Origin);
				var overlayScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
				_overlayInstance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: overlayCamera.Position + Direction.Forward * 3f + Direction.Left * 0.9f + Direction.Up * 0.9f);
				var overlayLight = factory.LightBuilder.CreatePointLight(overlayCamera.Position, ColorVect.FromHueSaturationLightness(120f, 0.8f, 0.75f));
				_overlayRenderer = factory.RendererBuilder.CreateBindableRenderer(overlayScene, overlayCamera, factory.ResourceAllocator);
				_overlayEnabled = true;

				overlayScene.Add(_overlayInstance);
				overlayScene.Add(overlayLight);

				var compositor = factory.RendererBuilder.CreateBindableCompositor();
				compositor.Add(renderer, RenderCompositionType.Standard);
				compositor.Add(_overlayRenderer, RenderCompositionType.RetainPreviousScenes);

				_disposables.Add(overlayCamera);
				_disposables.Add(_overlayInstance);
				_disposables.Add(overlayLight);
				_disposables.Add(overlayScene);
				_disposables.Add(_overlayRenderer);
				// Reverse-order disposal disposes the compositor before the renderers; its parameterless
				// Dispose detaches the contained renderers so they can then be disposed as standalones
				_disposables.Add(compositor);

				Compositor = compositor;
			}
			else {
				Renderer = renderer;
			}

			_disposables.Add(factory.ApplicationLoopBuilder.StartWpfUiLoop(Tick));

			RenderCurrentSource();
		}

		void StopRendering() {
			Renderer = null;
			Compositor = null;
			foreach (var d in Enumerable.Reverse(_disposables!)) {
				d.Dispose();
			}
			_disposables = null;
		}

		void RenderCurrentSource() {
			if (Compositor is { } compositor) compositor.RenderAll();
			else Renderer?.Render();
		}

		void Tick(TimeSpan deltaTime) {
			RenderCurrentSource();

			_instance.RotateBy((float) deltaTime.TotalSeconds * 130f % Direction.Up);
			_instance.RotateBy((float) deltaTime.TotalSeconds * 80f % Direction.Right);
			if (Compositor != null) {
				_overlayInstance.RotateBy((float) deltaTime.TotalSeconds * -130f % Direction.Up);
				_overlayInstance.RotateBy((float) deltaTime.TotalSeconds * -80f % Direction.Right);
			}
		}
	}
}
