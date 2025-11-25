using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.WinForms;
using Egodystonic.TinyFFR.World;

namespace TinyFFR.Tests.Integrations.WinForms {
	public partial class MainForm : Form {
		List<IDisposable>? _disposables;
		ModelInstance _instance;
		PointLight _light;

		public MainForm() {
			InitializeComponent();
		}

		private void toggleRenderingButton_Click(object sender, EventArgs e) {
			ToggleRendering();
		}

		private void changeLightColourButton_Click(object sender, EventArgs e) {
			ChangeLightColour();
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
			var camera = factory.CameraBuilder.CreateCamera(Egodystonic.TinyFFR.Location.Origin);
			var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
			var tex = factory.AssetLoader.LoadColorTexture(factory.AssetLoader.BuiltInTexturePaths.White);
			var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(tex);
			_instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 2.2f);
			_light = factory.LightBuilder.CreatePointLight(camera.Position, ColorVect.FromHueSaturationLightness(0f, 0.8f, 0.75f));
			var scene = factory.SceneBuilder.CreateScene(backdropColor: StandardColor.LightingSunMidday);
			sceneView.Renderer = factory.RendererBuilder.CreateBindableRenderer(scene, camera, factory.ResourceAllocator);

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
			_disposables.Add(sceneView.Renderer);
			_disposables.Add(factory.ApplicationLoopBuilder.StartWinFormsUiLoop(Tick));

			sceneView.Renderer.Value.Render();
		}

		void StopRendering() {
			sceneView.Renderer = null;
			foreach (var d in Enumerable.Reverse(_disposables!)) {
				d.Dispose();
			}
			_disposables = null;
		}

		void Tick(TimeSpan deltaTime) {
			sceneView.Renderer!.Value.Render();

			_instance.RotateBy((float) deltaTime.TotalSeconds * 130f % Direction.Up);
			_instance.RotateBy((float) deltaTime.TotalSeconds * 80f % Direction.Right);
		}
	}
}
