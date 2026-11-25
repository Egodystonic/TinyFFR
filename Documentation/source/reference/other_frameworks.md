---
title: Other Frameworks
description: An overview of how to integrate TinyFFR with UI frameworks, engines, and other hosts that TinyFFR doesn't provide an integration for.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * TinyFFR can be hosted in UI frameworks it doesn't provide an integration for by running in headless mode and rendering off-screen. :material-arrow-right: [Overview](#overview)
    * Frames can be handed to the framework either by copying them via the CPU (simple, works everywhere), or by sharing them on the GPU (fast, but requires native interop). :material-arrow-right: [Getting Frames to Your Framework](#getting-frames-to-your-framework)

</div>

## Overview

TinyFFR provides ready-made integrations for [ImGui](imgui_integration.md), [Avalonia](avalonia_integration.md), [WPF](wpf_integration.md), and [WinForms](winforms_integration.md). Any other UI framework (or game engine, or other host application) can still display TinyFFR's output, by following the same approach those integrations take:

1. 	Create the factory in [headless mode](creating_and_managing_windows.md#the-windowbuilder) (`#!csharp new LocalTinyFfrFactoryConfig { HeadlessMode = true }`), so that TinyFFR leaves windowing, input, and the rest of the OS integration to your framework.
2. 	Render off-screen, in to a render target your framework can display (see below).
3. 	Drive rendering from your framework's own frame/timer callback on its UI thread, rather than from a standalone [application loop](application_loops.md) of TinyFFR's own. TinyFFR and your framework then share the same primary thread, so all interaction with TinyFFR's API should happen on the UI thread, just as it does with the framework itself.
4. 	Use your framework's own input events. In headless mode TinyFFR doesn't gather any input of its own.

## Getting Frames to Your Framework

### CPU Copy

Render in to a [`RenderOutputBuffer`](capturing_render_output.md#render-output-buffers), [read each frame back](capturing_render_output.md#reading-frames) on the CPU, and copy its pixels in to your framework's bitmap/image type for display.

This is the simplest approach, works on every platform and GPU, and requires no native interop; but every frame is copied from the GPU to the CPU and (usually) uploaded back to the GPU again by your framework, which costs CPU time and memory bandwidth, particularly at high resolutions. It's recommended to start here even if you intend to support GPU sharing later becuase you'll probably want a CPU-copy-based fallback anyway.

:material-arrow-right: [Capturing Render Output](capturing_render_output.md)

### GPU Sharing

If your framework (or the graphics API it renders with) can import native GPU images, frames can instead be shared with it directly on the GPU, without ever being copied. Typical examples are frameworks that render with Vulkan (and support external memory), Direct3D 11 or 9Ex (and support shared textures), Metal (and support `IOSurface`s), or OpenGL/ANGLE (and support the relevant memory-sharing extensions).

To do this, render in to an `ExportableRenderSurface` instead of a buffer, import its images in to your framework, and then take, display, and return the latest rendered image each frame. This requires calling your framework's/graphics API's native interop directly and synchronizing access to the images correctly, so it's considerably more involved than the CPU approach.

Because GPU sharing depends on the platform, the GPU, and the framework's rendering mode, it isn't always available. A robust integration uses it where it can and falls back to the CPU approach otherwise (e.g. when `TryCreateExportableRenderSurface()` returns `null`, when the framework can't import the surface's handle type, or when TinyFFR and the framework are using different GPUs). TinyFFR uses this dual/hybrid approach for WPF and Avalonia.

:material-arrow-right: [GPU Render Exporting](gpu_render_exporting.md)
