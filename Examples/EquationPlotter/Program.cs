using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.DearImGui;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using EquationPlotter;
using Hexa.NET.ImGui;

using var factory = new LocalTinyFfrFactory();
var primaryDisplay = factory.DisplayDiscoverer.Primary ?? throw new InvalidOperationException("No display connected!");
using var window = factory.WindowBuilder.CreateWindow(primaryDisplay, title: "TinyFFR Equation Plotter", size: (1600, 900));
using var camera = factory.CameraBuilder.CreateCamera();
using var cameraController = camera.CreateController<OrbitalCameraController>();
cameraController.SetConstraints(Location.Origin, Direction.Up, Direction.Forward, angleRange: null, minHeight: -10f, maxHeight: 10f, minDistance: 3f, maxDistance: 25f);
cameraController.Angle = Angle.FromDegrees(-35f);
cameraController.Height = 3.5f;
cameraController.Distance = 8f;

using var scene = factory.SceneBuilder.CreateScene();
using var sun = factory.LightBuilder.CreateDirectionalLight(direction: new Direction(-0.3f, -1f, -0.5f), castsShadows: true);
scene.Add(sun);

using var labelScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
using var sceneRenderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
using var labelRenderer = factory.RendererBuilder.CreateRenderer(labelScene, camera, window);
using var imguiScene = factory.SceneBuilder.CreateImGuiScene(factory);
using var imguiRenderer = factory.RendererBuilder.CreateRenderer(imguiScene, window);
using var compositor = factory.RendererBuilder.CreateCompositor(window);
sceneRenderer.SetQuality(new RenderQualityConfig(BuiltInQualityConfiguration.Medium) { AntiAliasingMode = AntiAliasingMode.None });
labelRenderer.SetQuality(BuiltInQualityConfiguration.DebugAndDiagnostic);
compositor.Add(sceneRenderer, RenderCompositionType.Standard);
compositor.Add(labelRenderer, RenderCompositionType.RetainPreviousScenes);
compositor.Add(imguiRenderer, RenderCompositionType.RetainPreviousScenes);
using var loop = factory.ApplicationLoopBuilder.CreateLoop();

using var plots = new PlotController(factory, scene);
var settings = new SceneSettings();
using var axes = new AxesGizmo(scene, labelScene, settings);
var clock = new PlotClock();
var panel = new ControlPanel(plots, clock, axes, settings, scene);

while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();
	var input = loop.Input.KeyboardAndMouse;

	imguiScene.BeginFrame(deltaTime, loop, window, imguiRenderer);
	panel.Draw(loop);

	var io = ImGui.GetIO();
	if (!io.WantCaptureKeyboard && input.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) break;
	if (!io.WantCaptureMouse) {
		if (input.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseLeft) || input.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseRight)) {
			cameraController.AdjustAngleViaMouseCursor(input, Angle.FromDegrees(0.25f * settings.OrbitSensitivity), settings.InvertHorizontal);
			cameraController.AdjustHeightViaMouseCursor(input, 0.02f * settings.OrbitSensitivity, settings.InvertVertical);
		}
		cameraController.AdjustDistanceViaMouseWheel(input, 0.6f * settings.ZoomSensitivity);
	}
	cameraController.Progress(deltaTime);

	clock.Advance(deltaTime);
	plots.Update(clock.Time);

	imguiScene.EndFrame();
	compositor.RenderAll();
}

scene.Remove(sun);
