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

using var imgui = factory.SceneBuilder.CreateImGuiScene(factory);
using var imguiRenderer = factory.RendererBuilder.CreateRenderer(imgui, window);

using var compositor = factory.RendererBuilder.CreateCompositor(window);
compositor.Add(sceneRenderer, RenderCompositionType.Standard);
compositor.Add(imguiRenderer, RenderCompositionType.RetainPreviousScenes);

using var loop = factory.ApplicationLoopBuilder.CreateLoop();
loop.EnableInputTextTranscription = true;

var rotationSpeed = 1.5f;
var showDemoWindow = true;
var totalRotation = 0f;
var textBuffer = string.Empty;

while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce();
	var kbm = loop.Input.KeyboardAndMouse;
	if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) break;

	totalRotation += rotationSpeed * (float) deltaTime.TotalSeconds;
	cube.SetRotation(new Rotation(totalRotation * 60f, Direction.Up));

	imgui.BeginFrame(deltaTime, loop.Input, window);

	if (showDemoWindow) ImGui.ShowDemoWindow(ref showDemoWindow);

	ImGui.SetNextWindowPos(new Vector2(20f, 20f), ImGuiCond.FirstUseEver);
	ImGui.SetNextWindowSize(new Vector2(360f, 320f), ImGuiCond.FirstUseEver);
	if (ImGui.Begin("TinyFFR Controls")) {
		ImGui.Text($"FPS: {loop.FramesPerSecondRecentAverage:N0}");
		ImGui.SliderFloat("Rotation Speed", ref rotationSpeed, 0f, 5f);
		ImGui.Checkbox("Show Demo Window", ref showDemoWindow);

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

	imgui.EndFrame();

	compositor.RenderAll();
	
	window.SetTitle($"FPS: {loop.FramesPerSecondRecentAverage:N0}");
}

return 0;
