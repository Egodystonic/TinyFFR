// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Numerics;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.DearImGui;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;
using Hexa.NET.ImGui;

CommonTestSupportFunctions.ResolveNativeAssembliesFromBuildOutputDir();

if (args.Length > 0 && args[0] == "--headless-dpi-check") return HeadlessDpiCheck.Run(args.Length > 1 ? args[1] : null);
if (args.Length > 2 && args[0] == "--headless-ui-dump") return HeadlessDpiCheck.RunUiDump(args[1], Single.Parse(args[2]));
if (args.Length > 0 && args[0] == "--headless-dynamic-buffer-check") return HeadlessDpiCheck.RunDynamicBufferCheck();
if (args.Length > 0 && args[0] == "--headless-texture-churn") return HeadlessDpiCheck.RunTextureChurn(args.Length > 1 ? Int32.Parse(args[1]) : 60);
if (args.Length > 0 && args[0] == "--headless-gamepad-check") return HeadlessDpiCheck.RunGamepadMappingCheck();
if (args.Length > 0 && args[0] == "--headless-subarea-check") return HeadlessDpiCheck.RunSubAreaCheck();
if (args.Length > 0 && args[0] == "--headless-imgui-texture-check") return HeadlessDpiCheck.RunImGuiTextureCheck();
if (args.Length > 0 && args[0] == "--headless-subarea-compositor-check") return HeadlessDpiCheck.RunSubAreaCompositorCheck();

using var factory = new LocalTinyFfrFactory();
var display = factory.DisplayDiscoverer.Primary!.Value;
using var window = factory.WindowBuilder.CreateWindow(display, title: "TinyFFR ImGui Integration Test");

using var cubeMesh = factory.MeshBuilder.CreateMesh(new Cuboid(1f));
using var cubeMaterial = factory.MaterialBuilder.CreateTestMaterial();
using var cube = factory.ObjectBuilder.CreateModelInstance(cubeMesh, cubeMaterial);
using var light = factory.LightBuilder.CreatePointLight(new Location(2f, 2f, -2f));
using var scene = factory.SceneBuilder.CreateScene();
scene.Add(cube);
scene.Add(light);

using var camera = factory.CameraBuilder.CreateCamera(initialPosition: new Location(0f, 0f, -3f));
using var sceneRenderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

using var viewportBuffer = factory.RendererBuilder.CreateRenderOutputBuffer((640, 480));

using var imgui = factory.SceneBuilder.CreateImGuiScene(factory, new ImGuiSceneCreationConfig { EnableGamepadNavigation = true });
using var imguiRenderer = factory.RendererBuilder.CreateRenderer(imgui, window);

var useCompositor = !args.Contains("--no-compositor");
var compositor = useCompositor ? factory.RendererBuilder.CreateCompositor(window) : default;
if (useCompositor) {
	compositor.Add(sceneRenderer, RenderCompositionType.Standard);
	compositor.Add(imguiRenderer, RenderCompositionType.RetainPreviousScenes);
}
Console.WriteLine(useCompositor ? "Rendering VIA COMPOSITOR (pass --no-compositor to render renderers directly)" : "Rendering renderers DIRECTLY (no compositor)");

using var viewportCube = factory.ObjectBuilder.CreateModelInstance(cubeMesh, cubeMaterial);
using var viewportCamera = factory.CameraBuilder.CreateCamera(initialPosition: new Location(0f, 0.6f, -2.5f));
using var viewportScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
viewportScene.Add(viewportCube);
using var viewportRenderer = factory.RendererBuilder.CreateRenderer(viewportScene, viewportCamera, viewportBuffer);
var viewportTextureId = imgui.RegisterTexture(viewportBuffer.CreateDynamicTexture());

using var loop = factory.ApplicationLoopBuilder.CreateLoop();
loop.EnableInputTextTranscription = true;

var rotationSpeed = 1.5f;
var showDemoWindow = true;
var totalRotation = 0f;
var textBuffer = string.Empty;
var subAreaMode = 0;
var appliedSubAreaMode = -1;
var secondsSinceLastReport = 0f;
var controlsRect = (Pos: Vector2.Zero, Size: Vector2.Zero);
var gamepadRect = (Pos: Vector2.Zero, Size: Vector2.Zero);

var subAreaModes = new (string Name, Orientation2D Anchor, XYPair<float> Offset, XYPair<float> Size)[] {
	("0 full window", Orientation2D.None, (0f, 0f), (1f, 1f)),
	("1 right 30%", Orientation2D.Right, (0f, 0f), (0.3f, 1f)),
	("2 right 90%", Orientation2D.Right, (0f, 0f), (0.9f, 1f)),
	("3 left 50%", Orientation2D.Left, (0f, 0f), (0.5f, 1f)),
	("4 explicit full", Orientation2D.None, (0f, 0f), (1f, 1f)),
	("5 bottom half", Orientation2D.Down, (0f, 0f), (1f, 0.5f))
};

Console.WriteLine("[F3] cycle sub-area modes");

while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce();
	var kbm = loop.Input.KeyboardAndMouse;
	if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) break;

	totalRotation += rotationSpeed * (float) deltaTime.TotalSeconds;
	cube.SetRotation(new Rotation(totalRotation * 60f, Direction.Up));
	viewportCube.SetRotation(new Rotation(totalRotation * -45f, Direction.Up));

	var modeChanged = false;
	if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.F3)) subAreaMode = (subAreaMode + 1) % subAreaModes.Length;
	if (subAreaMode != appliedSubAreaMode) {
		var mode = subAreaModes[subAreaMode];
		imguiRenderer.SetRenderSubAreaFraction(mode.Anchor, mode.Offset, mode.Size);
		appliedSubAreaMode = subAreaMode;
		modeChanged = true;
	}

	viewportRenderer.Render();

	imgui.BeginFrame(deltaTime, loop.Input, window, imguiRenderer);

	if (showDemoWindow) ImGui.ShowDemoWindow(ref showDemoWindow);

	ImGui.SetNextWindowPos(new Vector2(20f, 20f), ImGuiCond.FirstUseEver);
	ImGui.SetNextWindowSize(new Vector2(360f, 320f), ImGuiCond.FirstUseEver);
	if (ImGui.Begin("TinyFFR Controls", ImGuiWindowFlags.NoSavedSettings)) {
		controlsRect = (ImGui.GetWindowPos(), ImGui.GetWindowSize());
		ImGui.Text($"FPS: {loop.FramesPerSecondRecentAverage:N0}");
		ImGui.SliderFloat("Rotation Speed", ref rotationSpeed, 0f, 5f);
		ImGui.Checkbox("Show Demo Window", ref showDemoWindow);

		ImGui.Separator();
		ImGui.Text($"[V] sub-area mode: {subAreaModes[subAreaMode].Name}");
		ImGui.Text($"sub-area {imguiRenderer.GetRenderSubAreaPixelDimensions()} at {imguiRenderer.GetRenderSubAreaPixelOffset()}");

		ImGui.Separator();
		ImGui.Text("Type here (tests text input + clipboard):");
		ImGui.InputText("##text", ref textBuffer, 128);

		ImGui.Separator();
		ImGui.Text("Scroll this region (tests clipping):");
		if (ImGui.BeginChild("scrolltest", new Vector2(0f, 120f), ImGuiChildFlags.Borders)) {
			for (var i = 0; i < 40; ++i) ImGui.Text($"Clipped line {i}");
		}
		ImGui.EndChild();
	}
	ImGui.End();

	ImGui.SetNextWindowPos(new Vector2(400f, 20f), ImGuiCond.FirstUseEver);
	ImGui.SetNextWindowSize(new Vector2(380f, 340f), ImGuiCond.FirstUseEver);
	if (ImGui.Begin("Gamepad", ImGuiWindowFlags.NoSavedSettings)) {
		gamepadRect = (ImGui.GetWindowPos(), ImGui.GetWindowSize());
		var controllers = loop.Input.GameControllers;
		ImGui.Text($"Connected: {controllers.Count}");
		for (var i = 0; i < controllers.Count; ++i) ImGui.Text($"  [{i}] {controllers[i].GetNameAsNewStringObject()}");

		var pad = loop.Input.GameControllersCombined;
		ImGui.Separator();
		ImGui.Text($"L stick  X {pad.LeftStickPosition.GetDisplacementHorizontalWithDeadzone(),6:N2}  Y {pad.LeftStickPosition.GetDisplacementVerticalWithDeadzone(),6:N2}");
		ImGui.Text($"R stick  X {pad.RightStickPosition.GetDisplacementHorizontalWithDeadzone(),6:N2}  Y {pad.RightStickPosition.GetDisplacementVerticalWithDeadzone(),6:N2}");
		ImGui.Text($"Triggers L {pad.LeftTriggerPosition.GetDisplacementWithDeadzone(),6:N2}  R {pad.RightTriggerPosition.GetDisplacementWithDeadzone(),6:N2}");

		ImGui.Separator();
		ImGui.Text("TinyFFR reports held:");
		var held = String.Join(", ", pad.CurrentlyPressedButtons);
		ImGui.TextWrapped(held.Length == 0 ? "  (none)" : "  " + held);

		ImGui.Separator();
		ImGui.Text("ImGui received:");
		var received = new List<string>();
		foreach (var key in Enum.GetValues<ImGuiKey>()) {
			if (!(Enum.GetName(key)?.StartsWith("Gamepad", StringComparison.Ordinal) ?? false)) continue;
			if (ImGui.IsKeyDown(key)) received.Add(Enum.GetName(key)!["Gamepad".Length..]);
		}
		ImGui.TextWrapped(received.Count == 0 ? "  (none)" : "  " + String.Join(", ", received));

		ImGui.Separator();
		ImGui.Text("D-pad moves focus, bottom face activates.");
		ImGui.Text("Sticks scroll; L1/R1 change tweak speed.");
	}
	ImGui.End();

	ImGui.SetNextWindowPos(new Vector2(20f, 360f), ImGuiCond.FirstUseEver);
	ImGui.SetNextWindowSize(new Vector2(340f, 300f), ImGuiCond.FirstUseEver);
	if (ImGui.Begin("Scene View", ImGuiWindowFlags.NoSavedSettings)) {
		ImGui.Text("Offscreen scene sampled as an ImGui image:");
		var available = ImGui.GetContentRegionAvail();
		var imageHeight = MathF.Max(available.Y - 4f, 32f);
		TinyFfrImGuiExtensions.Image(viewportTextureId, new Vector2(available.X, imageHeight));
	}
	ImGui.End();

	imgui.EndFrame();

	secondsSinceLastReport += (float) deltaTime.TotalSeconds;
	if (modeChanged || secondsSinceLastReport >= 1f) {
		secondsSinceLastReport = 0f;
		Console.WriteLine(
			$"[{subAreaModes[subAreaMode].Name}] " +
			$"target={((IRenderTarget) window).ViewportDimensions} logical={window.Size} " +
			$"subArea={imgui.LastFrameSubAreaSize}@{imgui.LastFrameSubAreaOffset} | " +
			$"controls={controlsRect.Size.X:N0}x{controlsRect.Size.Y:N0}@{controlsRect.Pos.X:N0},{controlsRect.Pos.Y:N0} " +
			$"gamepad={gamepadRect.Size.X:N0}x{gamepadRect.Size.Y:N0}@{gamepadRect.Pos.X:N0},{gamepadRect.Pos.Y:N0}"
		);
	}

	if (useCompositor) {
		compositor.RenderAll();
	}
	else {
		sceneRenderer.Render();
		imguiRenderer.Render();
	}

	window.SetTitle($"FPS: {loop.FramesPerSecondRecentAverage:N0}");
}

imgui.UnregisterTexture(viewportTextureId);
viewportScene.Remove(viewportCube);

if (useCompositor) compositor.Dispose();

return 0;
