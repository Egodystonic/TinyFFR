---
title: Integrating ImGui
description: How to render ImGui interfaces on top of your TinyFFR scenes.
---

# Integrating Dear ImGui

Unlike the WPF, Avalonia and WinForms integrations — where TinyFFR renders *into* a host framework's control — the Dear ImGui integration works the other way around. **TinyFFR is the backend**: it owns the window, feeds ImGui its input, and turns ImGui's per-frame draw data into Filament geometry drawn over your 3D scene.

## Installation

```
dotnet add package Egodystonic.TinyFFR.ImGui
```

This brings in `Egodystonic.TinyFFR` and [Hexa.NET.ImGui](https://www.nuget.org/packages/Hexa.NET.ImGui/) (a .NET binding for Dear ImGui 1.92) transitively.

## The ImGui scene

An ImGui interface is a **separate scene with its own renderer**, composited on top of your 3D pass — exactly like a [canvas](../reference/canvas.md):

```csharp
using var imguiScene = factory.SceneBuilder.CreateImGuiScene(factory);
using var imguiRenderer = factory.RendererBuilder.CreateRenderer(imguiScene, window);

using var compositor = factory.RendererBuilder.CreateCompositor(window);
compositor.Add(tinyFfrSceneRenderer, RenderCompositionType.Standard); // (1)!
compositor.Add(imguiSceneRenderer, RenderCompositionType.RetainPreviousScenes); // (2)!
```

1. This should be a regular renderer for your existing 3D scene.

2. `RenderCompositionType.RetainPreviousScenes` is what lets your 3D scene show through translucent ImGui panels. Add the ImGui renderer **last** so the interface draws on top.

## The frame loop

Bracket your widget code between `BeginFrame` and `EndFrame`:

```csharp
using var loop = factory.ApplicationLoopBuilder.CreateLoop();
loop.EnableInputTextTranscription = true; // (1)!

while (!loop.Input.UserQuitRequested) {
    var deltaTime = loop.IterateOnce();

    imgui.BeginFrame(deltaTime, loop.Input, window); // (2)!

    ImGui.ShowDemoWindow();

    imgui.EndFrame();

    compositor.RenderAll();
}
```

1. ImGui text fields need real typed characters, not raw scancodes. Set `loop.EnableInputTextTranscription = true` — it is **off by default**, and without it text boxes will not accept input.

2. `BeginFrame` feeds `ImGuiIO` with the display size, DPI scale, delta time and all input; `EndFrame` calls `ImGui.Render()` and translates the resulting draw lists into renderable geometry.

There is also a windowless overload for rendering to a `RenderOutputBuffer` (or when hosted inside another UI framework), where you supply the sizes yourself:

```csharp
imgui.BeginFrame(deltaTime, input, logicalSize, framebufferSize);
```
