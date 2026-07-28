using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.WinForms;
using Egodystonic.TinyFFR.World;

namespace TinyFFR.Tests.Integrations.WinForms {
	public partial class MainForm : Form {
		List<IDisposable>? _disposables;
		ModelInstance _instance;
		ModelInstance _overlayInstance;
		PointLight _light;
		Renderer _overlayRenderer;
		bool _overlayEnabled;

		public MainForm() {
			InitializeComponent();
		}

		private void toggleRenderingButton_Click(object sender, EventArgs e) {
			ToggleRendering();
		}

		private void changeLightColourButton_Click(object sender, EventArgs e) {
			ChangeLightColour();
		}

		private void toggleOverlayButton_Click(object sender, EventArgs e) {
			ToggleOverlay();
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
			if (sceneView.Compositor is not { } compositor) return;
			_overlayEnabled = !_overlayEnabled;
			compositor.SetEnabledState(_overlayRenderer, _overlayEnabled);
		}

		void StartRendering() {
			_disposables = new List<IDisposable>();

			var factory = new LocalTinyFfrFactory();
			var camera = factory.CameraBuilder.CreateCamera(Egodystonic.TinyFFR.Location.Origin);
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

			if (compositeModeCheckBox.Checked) {
				var overlayCamera = factory.CameraBuilder.CreateCamera(Egodystonic.TinyFFR.Location.Origin);
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

				sceneView.Compositor = compositor;
			}
			else {
				sceneView.Renderer = renderer;
			}

			_disposables.Add(factory.ApplicationLoopBuilder.StartWinFormsUiLoop(Tick));

			RenderCurrentSource();
		}

		void StopRendering() {
			sceneView.Renderer = null;
			sceneView.Compositor = null;
			foreach (var d in Enumerable.Reverse(_disposables!)) {
				d.Dispose();
			}
			_disposables = null;
		}

		void RenderCurrentSource() {
			if (sceneView.Compositor is { } compositor) compositor.RenderAll();
			else sceneView.Renderer?.Render();
		}

		void Tick(TimeSpan deltaTime) {
			RenderCurrentSource();

			_instance.RotateBy((float) deltaTime.TotalSeconds * 130f % Direction.Up);
			_instance.RotateBy((float) deltaTime.TotalSeconds * 80f % Direction.Right);
			if (sceneView.Compositor != null) {
				_overlayInstance.RotateBy((float) deltaTime.TotalSeconds * -130f % Direction.Up);
				_overlayInstance.RotateBy((float) deltaTime.TotalSeconds * -80f % Direction.Right);
			}
		}
	}
}
