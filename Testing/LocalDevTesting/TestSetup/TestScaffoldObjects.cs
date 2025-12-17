// Created on 2025-12-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Testing.Local.TestSetup;

sealed record TestContext {
	readonly ILocalTinyFfrFactory? _factory;
	readonly Window? _window;
	readonly Scene? _scene;
	readonly Material? _material;
	readonly Mesh? _mesh;
	readonly ModelInstance? _modelInstance;
	readonly EnvironmentCubemap? _backdrop;
	readonly DirectionalLight? _directionLight;
	readonly Camera? _camera;
	readonly Renderer? _renderer;
	readonly ApplicationLoop? _loop;

	public ILocalTinyFfrFactory Factory => _factory ?? throw NoContextObjectException();
	public Window Window => _window ?? throw NoContextObjectException();
	public Scene Scene => _scene ?? throw NoContextObjectException();
	public Material Material => _material ?? throw NoContextObjectException();
	public Mesh Mesh => _mesh ?? throw NoContextObjectException();
	public ModelInstance ModelInstance => _modelInstance ?? throw NoContextObjectException();
	public EnvironmentCubemap Backdrop => _backdrop ?? throw NoContextObjectException();
	public DirectionalLight DirectionalLight => _directionLight ?? throw NoContextObjectException();
	public Camera Camera => _camera ?? throw NoContextObjectException();
	public Renderer Renderer => _renderer ?? throw NoContextObjectException();
	public ApplicationLoop Loop => _loop ?? throw NoContextObjectException();

	public ILatestInputRetriever Input => Loop.Input;

	public TestContext(ILocalTinyFfrFactory? factory, Window? window, Scene? scene, Material? material, Mesh? mesh, ModelInstance? modelInstance, EnvironmentCubemap? backdrop, DirectionalLight? directionLight, Camera? camera, Renderer? renderer, ApplicationLoop? loop) {
		_factory = factory;
		_window = window;
		_scene = scene;
		_material = material;
		_mesh = mesh;
		_modelInstance = modelInstance;
		_backdrop = backdrop;
		_directionLight = directionLight;
		_camera = camera;
		_renderer = renderer;
		_loop = loop;
	}

	public void DisposeObjects() {
		_loop?.Dispose();
		_renderer?.Dispose();
		_camera?.Dispose();
		_scene?.Dispose();
		_directionLight?.Dispose();
		_backdrop?.Dispose();
		_modelInstance?.Dispose();
		_mesh?.Dispose();
		_material?.Dispose();
		_window?.Dispose();
		_factory?.Dispose();
	}

	static InvalidOperationException NoContextObjectException() {
		return new InvalidOperationException($"Context object (or one of its dependent objects) was deliberately set to null in test setup, and is therefore unavailable inside the test.");
	}
}

interface ITestContextBuilder {
	public ILocalTinyFfrFactory? Factory { get; set; }
	public Window? Window { get; set; }
	public Scene? Scene { get; set; }
	public Material? Material { get; set; }
	public Mesh? Mesh { get; set; }
	public ModelInstance? ModelInstance { get; set; }
	public EnvironmentCubemap? Backdrop { get; set; }
	public DirectionalLight? DirectionalLight { get; set; }
	public Camera? Camera { get; set; }
	public Renderer? Renderer { get; set; }
	public ApplicationLoop? Loop { get; set; }
}

sealed record TestContextBuilder : ITestContextBuilder {
	readonly record struct RefOption<T>(T? Value, bool IsSet) where T : class;
	readonly record struct ValOption<T>(T? Value, bool IsSet) where T : struct;

	RefOption<ILocalTinyFfrFactory> _factory = default;
	ValOption<Window> _window = default;
	ValOption<Scene> _scene = default;
	ValOption<Material> _material = default;
	ValOption<Mesh> _mesh = default;
	ValOption<ModelInstance> _modelInstance = default;
	ValOption<EnvironmentCubemap> _backdrop = default;
	ValOption<DirectionalLight> _directionalLight = default;
	ValOption<Camera> _camera = default;
	ValOption<Renderer> _renderer = default;
	ValOption<ApplicationLoop> _loop = default;

	public ILocalTinyFfrFactory? Factory {
		get => Materialize(ref _factory, CreateDefaultFactory);
		set => SetOrThrowIfAlreadySet(ref _factory, value);
	}

	public Window? Window {
		get => Materialize(ref _window, CreateDefaultWindow);
		set => SetOrThrowIfAlreadySet(ref _window, value);
	}

	public Scene? Scene {
		get => Materialize(ref _scene, CreateDefaultScene);
		set => SetOrThrowIfAlreadySet(ref _scene, value);
	}

	public Material? Material {
		get => Materialize(ref _material, CreateDefaultMaterial);
		set => SetOrThrowIfAlreadySet(ref _material, value);
	}

	public Mesh? Mesh {
		get => Materialize(ref _mesh, CreateDefaultMesh);
		set => SetOrThrowIfAlreadySet(ref _mesh, value);
	}

	public ModelInstance? ModelInstance {
		get => Materialize(ref _modelInstance, CreateDefaultModelInstance);
		set => SetOrThrowIfAlreadySet(ref _modelInstance, value);
	}

	public EnvironmentCubemap? Backdrop {
		get => Materialize(ref _backdrop, CreateDefaultBackdrop);
		set => SetOrThrowIfAlreadySet(ref _backdrop, value);
	}

	public DirectionalLight? DirectionalLight {
		get => Materialize(ref _directionalLight, CreateDefaultDirectionalLight);
		set => SetOrThrowIfAlreadySet(ref _directionalLight, value);
	}

	public Camera? Camera {
		get => Materialize(ref _camera, CreateDefaultCamera);
		set => SetOrThrowIfAlreadySet(ref _camera, value);
	}

	public Renderer? Renderer {
		get => Materialize(ref _renderer, CreateDefaultRenderer);
		set => SetOrThrowIfAlreadySet(ref _renderer, value);
	}

	public ApplicationLoop? Loop {
		get => Materialize(ref _loop, CreateDefaultLoop);
		set => SetOrThrowIfAlreadySet(ref _loop, value);
	}

	public TestContext Materialize() {
		return new TestContext(
			Factory,
			Window,
			Scene,
			Material,
			Mesh,
			ModelInstance,
			Backdrop,
			DirectionalLight,
			Camera,
			Renderer,
			Loop
		);
	}

	T? Materialize<T>(ref RefOption<T> opt, Func<T?> defaultMaterializationFunc) where T : class {
		if (!opt.IsSet) opt = new(defaultMaterializationFunc(), true);
		return opt.Value;
	}

	T? Materialize<T>(ref ValOption<T> opt, Func<T?> defaultMaterializationFunc) where T : struct {
		if (!opt.IsSet) opt = new(defaultMaterializationFunc(), true);
		return opt.Value;
	}

	void SetOrThrowIfAlreadySet<T>(ref RefOption<T> opt, T? value) where T : class {
		if (opt.IsSet) throw new InvalidOperationException("The value of this context object has already been set, either directly or indirectly (e.g. materialized when getting it or another object that depends on it).");
		opt = new(value, true);
	}

	void SetOrThrowIfAlreadySet<T>(ref ValOption<T> opt, T? value) where T : struct {
		if (opt.IsSet) throw new InvalidOperationException("The value of this context object has already been set, either directly or indirectly (e.g. materialized when getting it or another object that depends on it).");
		opt = new(value, true);
	}

	ILocalTinyFfrFactory CreateDefaultFactory() => new LocalTinyFfrFactory(rendererBuilderConfig: new RendererBuilderConfig { EnableVSync = false });
	Window? CreateDefaultWindow() {
		if (Factory == null) return null;
		var primaryDisplay = Factory.DisplayDiscoverer.Primary;
		if (primaryDisplay == null) throw new InvalidOperationException($"Can not create default window as no display was discovered. Manually set {nameof(Window)} property in test builder to fix this.");
		return Factory.WindowBuilder.CreateWindow(primaryDisplay.Value, title: "Local Dev Testing Window");
	}
	Scene? CreateDefaultScene() {
		if (Factory == null) return null;
		return Factory.SceneBuilder.CreateScene(name: "Default Test Scene");
	}
	Material? CreateDefaultMaterial() => Factory?.MaterialBuilder.CreateTestMaterial();
	Mesh? CreateDefaultMesh() => Factory?.MeshBuilder.CreateMesh(new Cuboid(1f));
	ModelInstance? CreateDefaultModelInstance() {
		if (Factory == null || Material is not { } material || Mesh is not { } mesh) return null;
		var result = Factory.ObjectBuilder.CreateModelInstance(mesh, material, initialPosition: Location.Origin + Direction.Forward * 1.35f, initialRotation: 45f % Direction.Down, name: "Default Test Model Instance");
		Scene?.Add(result);
		return result;
	}
	EnvironmentCubemap? CreateDefaultBackdrop() {
		if (Factory == null) return null;
		var result = Factory.AssetLoader.LoadEnvironmentCubemap(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr), name: "Default Test Backdrop");
		Scene?.SetBackdrop(result);
		return result;
	}
	DirectionalLight? CreateDefaultDirectionalLight() {
		if (Factory == null) return null;
		var result = Factory.LightBuilder.CreateDirectionalLight(castsShadows: false, showSunDisc: true, direction: new(0f, -1f, -0.3f), name: "Default Test Directional Light");
		Scene?.Add(result);
		return result;
	}
	Camera? CreateDefaultCamera() {
		if (Factory == null) return null;
		var initialPos = (Direction.Up * 0.7f).AsLocation();
		var result = Factory.CameraBuilder.CreateCamera(initialPosition: initialPos, initialViewDirection: (initialPos >> (Direction.Forward * 1.35f).AsLocation()).Direction, name: "Default Test Camera");
		return result;
	}
	Renderer? CreateDefaultRenderer() {
		if (Factory == null || Window is not { } window || Scene is not { } scene || Camera is not { } camera) return null;
		var result = Factory.RendererBuilder.CreateRenderer(scene, camera, window, new RendererCreationConfig { // TODO this should include the window/camera/scene
			Name = "Default Test Renderer",
			Quality = new RenderQualityConfig { ShadowQuality = Quality.VeryHigh }
		});
		return result;
	}
	ApplicationLoop? CreateDefaultLoop() {
		if (Factory == null) return null;
		return Factory.ApplicationLoopBuilder.CreateLoop(frameRateCapHz: null, name: "Default Test Loop");
	}
}

sealed class TestBuilder {
	public ITestContextBuilder Context { get; set; } = new TestContextBuilder();

	public bool AutoDisposeContextObjectsOnTestEnd { get; set; } = true;
	public bool DefaultLoopFpsReportingEnable { get; set; } = true;
	public bool DefaultLoopSlowFrameReportingEnable { get; set; } = true;
	public TimeSpan DefaultLoopFpsReportingPeriod { get; set; } = TimeSpan.FromSeconds(10d);
	public TimeSpan DefaultLoopSlowFrameTime { get; set; } = TimeSpan.FromMilliseconds(20d);
}