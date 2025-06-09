using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Testing.Local.TestSetup;

sealed class TestOptions {
	public Func<ILocalTinyFfrFactory>? CustomFactoryCreationFunc { get; set; } = null;

	public bool CreateWindow { get; set; } = true;
	public Func<ILocalTinyFfrFactory, Window>? CustomWindowCreationFunc { get; set; } = null;

	public bool CreateScene { get; set; } = true;
	public Func<ILocalTinyFfrFactory, Scene>? CustomSceneCreationFunc { get; set; } = null;

	public bool LockCursorToWindow { get; set; } = true;
	public bool AddSkyBackdrop { get; set; } = true;
	public bool AddCube { get; set; } = true;
	public bool AddSunlight { get; set; } = true;
	public bool AddCamera { get; set; } = true;
	public bool AddRenderer { get; set; } = true;
	public bool UseDefaultCameraControls { get; set; } = true;
	public bool UseDefaultLoop { get; set; } = true;
	public Func<ILocalTinyFfrFactory, ApplicationLoop>? CustomLoopCreationFunc { get; set; } = null;
}

sealed class TestObjects {
	public required List<IDisposable> Disposables { get; init; }
	public required ILocalTinyFfrFactory Factory { get; init; }
	public Window? Window { get; init; }
	public Scene? Scene { get; init; }
	public EnvironmentCubemap? SkyBackdrop { get; init; }
	public ModelInstance? Cube { get; init; }
	public DirectionalLight? Sunlight { get; init; }
	public Camera? Camera { get; init; }
	public Renderer? Renderer { get; init; }
	public ApplicationLoop? Loop { get; init; }
}

static class TestScaffold {
	static TestObjects? _testObjects;
	static TestOptions? _options;
	static bool _exitTestCalled = false;

	public static TestObjects TestObjects => _testObjects ?? throw new InvalidOperationException("Test not yet initialized. Can not access test objects.");

	static bool ShouldRunDefaultLoop => _options != null && _testObjects != null && _options.UseDefaultLoop && _testObjects.Loop != null;

	public static void SetUpStandardTestObjects() {
		_options = new TestOptions();
		TestMain.ConfigureTest(_options);

		var disposables = new List<IDisposable>();

		var factory = (_options.CustomFactoryCreationFunc ?? CreateDefaultTestFactory).Invoke();
		disposables.Add(factory);

		Window? window = _options.CreateWindow
			? (_options.CustomWindowCreationFunc ?? CreateDefaultWindow).Invoke(factory)
			: null;
		if (window is { } w) {
			disposables.Add(w);
			if (_options.LockCursorToWindow) w.LockCursor = true;
		}

		Scene? scene = _options.CreateScene
			? (_options.CustomSceneCreationFunc ?? CreateDefaultScene).Invoke(factory)
			: null;
		// We don't add scene to disposables because we add it in CleanUpTestObjects

		EnvironmentCubemap? skyBackdrop = _options.AddSkyBackdrop
			? factory.AssetLoader.LoadEnvironmentCubemap(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr))
			: null;
		if (skyBackdrop is { } b) {
			disposables.Add(b);
			scene?.SetBackdrop(b);
		}

		ModelInstance? cube = _options.AddCube
			? CreateCube(factory, disposables)
			: null;
		if (cube is { } c) {
			disposables.Add(c);
			scene?.Add(c);
		}

		DirectionalLight? sunlight = _options.AddSunlight
			? factory.LightBuilder.CreateDirectionalLight()
			: null;
		if (sunlight is { } s) {
			disposables.Add(s);
			scene?.Add(s);
		}

		Camera? camera = _options.AddCamera
			? factory.CameraBuilder.CreateCamera((0f, 0f, -2f))
			: null;
		if (camera is { } cam) {
			disposables.Add(cam);
		}

		Renderer? renderer = (_options.AddRenderer && scene is { } rendererScene && camera is { } rendererCamera && window is { } rendererWindow)
			? factory.RendererBuilder.CreateRenderer(rendererScene, rendererCamera, rendererWindow)
			: null;
		// We don't add renderer to disposables because we add it in CleanUpTestObjects

		ApplicationLoop? loop = _options.UseDefaultLoop
			? (_options.CustomLoopCreationFunc ?? CreateDefaultLoop).Invoke(factory)
			: null;
		if (loop is { } l) {
			disposables.Add(l);
		}

		_testObjects = new TestObjects {
			Disposables = disposables,
			Factory = factory,
			Scene = scene,
			Window = window,
			SkyBackdrop = skyBackdrop,
			Cube = cube,
			Sunlight = sunlight,
			Camera = camera,
			Renderer = renderer,
			Loop = loop
		};
	}

	static ILocalTinyFfrFactory CreateDefaultTestFactory() {
		return new LocalTinyFfrFactory();
	}

	static Window CreateDefaultWindow(ILocalTinyFfrFactory factory) {
		return factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
	}

	static Scene CreateDefaultScene(ILocalTinyFfrFactory factory) {
		return factory.SceneBuilder.CreateScene();
	}

	static ModelInstance CreateCube(ILocalTinyFfrFactory factory, List<IDisposable> disposables) {
		var mesh = factory.MeshBuilder.CreateMesh(new Cuboid(1f));
		disposables.Add(mesh);
		return factory.ObjectBuilder.CreateModelInstance(mesh, factory.MaterialBuilder.TestMaterial);
	}

	static ApplicationLoop CreateDefaultLoop(ILocalTinyFfrFactory factory) {
		return factory.ApplicationLoopBuilder.CreateLoop(frameRateCapHz: 60);
	}

	public static void RunTestLoop() {
		if (_options == null || _testObjects == null) throw new InvalidOperationException($"Must invoke {nameof(SetUpStandardTestObjects)} first.");
		if (!ShouldRunDefaultLoop) return;

		var loop = _testObjects.Loop!.Value;
		var frameCount = 0;
		var startTime = Stopwatch.StartNew();
		while (!loop.Input.UserQuitRequested && !_exitTestCalled) {
			var sw = Stopwatch.StartNew();
			var deltaTime = loop.IterateOnce();
			var dtSecs = (float) deltaTime.TotalSeconds;

			if (_options.UseDefaultCameraControls && _testObjects.Camera is { } camera) {
				DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camera, dtSecs);
				DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camera, dtSecs);
			}

			TestMain.Tick(dtSecs, loop.Input);
			_testObjects.Renderer?.Render();

			if (sw.ElapsedMilliseconds > 20) Console.WriteLine("Slow frame! Measured: " + sw.ElapsedMilliseconds + "ms / DeltaTime: " + deltaTime.TotalMilliseconds + "ms");
			++frameCount;
		}

		try {
			CleanUpTestObjects();
		}
		finally {
			Console.WriteLine("Avg FPS: " + (frameCount / startTime.Elapsed.TotalSeconds).ToString("N0") + " (" + frameCount + " frames over " + startTime.Elapsed.TotalSeconds.ToString("N1") + " seconds)");
		}
	}

	public static void ExitTest() {
		_exitTestCalled = true;
		if (!ShouldRunDefaultLoop) CleanUpTestObjects();
	}

	static void CleanUpTestObjects() {
		if (_testObjects == null) return;

		var disposalExceptions = new List<Exception>();
		if (_testObjects.Scene is { } s) _testObjects.Disposables.Add(s);
		if (_testObjects.Renderer is { } r) _testObjects.Disposables.Add(r);
		for (var i = _testObjects.Disposables.Count - 1; i >= 0; --i) {
			try {
				Console.WriteLine($"Disposing {_testObjects.Disposables[i]}...");
				_testObjects.Disposables[i].Dispose();
			}
			catch (Exception e) {
				Console.WriteLine($"Error when disposing '{_testObjects.Disposables[i]}': {e.Message}");
				disposalExceptions.Add(e);
			}
		}

		if (disposalExceptions.Any()) throw new AggregateException("One or more resources threw an exception when being disposed.", disposalExceptions);
	}
}