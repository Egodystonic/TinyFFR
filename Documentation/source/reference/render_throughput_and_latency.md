---
title: Render Throughput & Latency
description: Information on how to configure frame rate, vsync, GPU synchronization and input latency in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Each frame passes through your application loop, the renderer, the GPU, and finally the display; each stage can make the next wait. :material-arrow-right: [The Frame Pipeline](#the-frame-pipeline)
    * `GpuSynchronizationFrameBufferCount` controls how many frames can be "in flight" on the GPU at once, trading throughput against input latency. :material-arrow-right: [GPU Synchronization](#gpu-synchronization)
    * `WaitForGpu()` blocks until every queued frame has been rendered; useful for captures and resource cleanup, but not for every frame. :material-arrow-right: [Waiting for the GPU](#waiting-for-the-gpu)

</div>

This page explains the settings that control how quickly frames are produced (*throughput*, i.e. frame rate) and how quickly the user's input shows up on screen (*latency*). 

Note that this is a separate concern to *optimisation*. Optimisation is concerned with making frames as cheap/quick to render as possible; throughput + latency are about how TinyFFR paces and queues the frames you ask it to render. All three (optimisation, throughput, latency) contribute to the overall output framerate but are separate concerns.

## The Frame Pipeline

A typical application loop looks like this:

```csharp
using var loop = factory.ApplicationLoopBuilder.CreateLoop();
while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime(); // (1)!
	
	myObject.MoveBy(velocityDirection * deltaTime); // (2)!
	
	renderer.Render(); // (3)!
}
```

1.	Waits until the next iteration is due (if a frame rate cap is set), then reads the latest user input.

2.	Your per-frame logic, using the input that was just read.

3.	Captures the current state of the scene and queues it to be rendered by the GPU, then returns. The GPU renders the frame in the background while your loop continues with the next iteration.

Each frame goes through four stages:

1.	**Loop iteration**: `IterateOnce()` reads the user's input. If a [frame rate cap](#frame-rate) is set, it first waits until the next iteration is due.
2.	**Update**: Your code moves objects, the camera, etc. according to that input.
3.	**Render**: `Render()` captures the scene and queues a frame for the GPU. It usually returns before the GPU has drawn anything.
4.	**GPU & display**: The GPU draws the frame, and it's shown on the display at its next refresh (if [vsync](#vsync) is enabled) or immediately (if not).

Because the GPU works in the background, the CPU can begin preparing the next frame while the GPU is still drawing the previous one(s). This overlap is what gives good throughput. But it can't go on indefinitely: `Render()` blocks (waits) when:

* Too many frames are already queued on the GPU (see [GPU Synchronization](#gpu-synchronization)). This is a sign that the GPU is the bottleneck, and is normal.
* VSync is enabled and the window isn't yet ready to receive a new frame (see [VSync](#vsync)).

Those waits are what limit your loop's frame rate when no cap is set.

## Frame Rate

<span class="def-icon">:material-card-bulleted-outline:</span> `ApplicationLoop.TargetFrameRate`

:   The maximum number of iterations per second the loop will run at, or `null` (the default) for no cap. It can also be set when the loop is created, e.g. `factory.ApplicationLoopBuilder.CreateLoop(frameRateCapHz: 60)`.

	This is a cap, not a guarantee. If your frames take longer than the cap allows for, the loop simply runs slower.

With [vsync](#vsync) enabled (the default), you'll usually want to leave `TargetFrameRate` as `null`: Rendering is then paced by the display's refresh rate, so the loop runs at the display's refresh rate (or below it, if your frames take longer than one refresh).

Setting a cap *below* the display's refresh rate is still useful. For example, capping a mostly-static tool or menu at 30 frames per second roughly halves the CPU and GPU work on a 60Hz display, saving power (and battery on laptops). Also, some users prefer a stable framerate rather than one that varies depending on scene complexity.

With vsync disabled, nothing paces the loop except the GPU, so an uncapped loop renders as many frames as it can. Set a cap if you don't want that.

!!! info "Measuring Framerate"
	For frame rate statistics such as `FramesPerSecondRecentAverage`, see [Measuring Framerate](measuring_framerate.md).

## VSync

"VSync", short for [Vertical Synchronization](https://en.wikipedia.org/wiki/Screen_tearing#Vertical_synchronization), is a configuration setting that controls whether or not your rendered frames must wait for the monitor's refresh rate.

```csharp
using var factory = new LocalTinyFfrFactory(
	rendererBuilderConfig: new RendererBuilderConfig { EnableVSync = false } // (1)!
);
```

1.	Disables vsync for every renderer this factory creates. It can't be changed after the factory is created.

<span class="def-icon">:material-card-bulleted-outline:</span> `RendererBuilderConfig.EnableVSync`

:   Whether frames rendered to windows wait for the display's refresh. Defaults to `true`.

When VSync is enabled, each frame you render to a `Window` will not actually be displayed until the parent `Display`'s next screen update(1). 
{ .annotate }

1. Most monitors refresh at 60Hz, some gaming monitors can be higher, TV screens may be lower.

For most applications this is desirable for two reasons:

* 	"Rendering" frames faster than the display can actually update is a waste of resources/energy. If your display has a 60Hz refresh rate but you're rendering 240 frames per second, 75% of those frames will never be seen.

* 	Updating the display's data buffer mid-refresh usually results in [screen tearing](https://en.wikipedia.org/wiki/Screen_tearing). Keeping VSync enabled eliminates this problem.

#### When to disable VSync

Because VSync blocks the renderer until the monitor cycles, it can reduce throughput in your application. This also means your application loop's maximum frequency will be capped by the target monitor's refresh rate(1).
{ .annotate }

1. Assuming you are rendering to a `Window` at least once per loop.

Relatedly, VSync introduces additional delay between a frame being rendered and it actually being displayed (e.g. "frames" must wait for the next display update refresh cycle). In applications(1) that demand minimal input latency, this can be problematic.
{ .annotate }

1. Such as video games.

If VSync is disabled, TinyFFR will write each rendered frame to the display's pixel buffer as soon as it's ready, with no delay. This will introduce screen tearing, but reduce input latency and increase throughput.

A display's current refresh rate is given by `display.CurrentRefreshRateHz` (the display a window is on is `window.Display`); see [Display Discovery](display_discovery.md).

VSync only applies to windows. Rendering to a [`RenderOutputBuffer`](capturing_render_output.md) (for example, with the [WPF](wpf_integration.md), [Avalonia](avalonia_integration.md), or [WinForms](winforms_integration.md) integrations, or in headless mode) is never paced by a display.

### Minimized Windows

Nothing is rendered to a window while it's minimized (or has no area), so `Render()` returns straight away. With nothing to wait for, an uncapped loop would otherwise run as fast as it possibly can while the window is minimized, wasting CPU time.

To prevent this, TinyFFR makes each skipped render wait as though the frame had been shown at the display's refresh rate, much like vsync would. This happens whether or not vsync is enabled. A loop that's already capped at or below the display's refresh rate isn't slowed down any further.

<span class="def-icon">:material-card-bulleted-outline:</span> `RendererBuilderConfig.EnableMinimizedWindowFramePacing`

:   Whether renders to minimized windows are paced at the display's refresh rate. Defaults to `true`.

	Set this to `false` if your loop must keep running at full speed while its window is minimized.

## GPU Synchronization

<span class="def-icon">:material-card-bulleted-outline:</span> `RendererCreationConfig.GpuSynchronizationFrameBufferCount`

:   How many frames can be queued or "in flight" on the GPU before `Render()` waits for the oldest one to finish. Defaults to `3`.

```csharp
using var renderer = factory.RendererBuilder.CreateRenderer(
	scene, 
	camera, 
	window, 
	new RendererCreationConfig { GpuSynchronizationFrameBufferCount = 1 } // (1)!
);
```

1.	`Render()` will wait whenever the previous frame is still being rendered.

A higher value lets the CPU get further ahead of the GPU, which smooths over frames that take longer than usual and generally increases throughput. But every queued frame is a frame of input the user hasn't seen yet, so a higher value also increases input latency.

| Value | Behaviour | Throughput | Input latency |
| :---: | :-------- | :--------- | :------------ |
| `1` to `5` | `Render()` waits only when this many frames are already queued. | Higher with larger values | Higher with larger values |
| `3` | The default. | Good | Moderate |
| `0` | `Render()` always waits until the frame is fully rendered. The CPU and GPU never work at the same time. | Lowest | Lowest |
| `-1` | No synchronization at all: `Render()` never waits for the GPU. | See below | See below |

* Values between `1` and `5` set a maximum number of frames that can be "queued" or "in progress" before the call to `Render()` will block the calling thread. A higher value generally increases your average throughput/FPS, but can also increase input latency.

* A value of `0` completely stops all asynchronous rendering. This means every call to `Render()` will __always__ block the calling thread until the frame is fully rendered and displayed on the target/Window. Setting this value can drastically lower average throughput/FPS; a value of at least `1` is recommended in most scenarios. Some competitive video games may wish to use `0`.

* A value of `-1` disables synchronization entirely. This means `Render()` will __never__ block the calling thread; but over time commands submitted to the GPU may exceed the GPU's capability to keep up, resulting in stuttering or even errors. Setting this value is only recommended when using a multi-renderer setup (set all renderers except your last/"primary" renderer to `-1`).

Defaults to `3`.

??? danger "Setting -1 also disables resource disposal protection"
	Another reason to never set this value to `-1` for *all* your renderers is that resource disposal is no longer synchronized.

	Behind the scenes, TinyFFR ensures that your resources are not deleted from GPU memory until scenes using them are fully rendered. This may be *after* you call `.Dispose()` on that resource; TinyFFR uses GPU synchronization [fences](https://en.wikipedia.org/wiki/Memory_barrier) to protect against use-after-dispose race conditions. When you have __no__ renderers with non-negative values for `GpuSynchronizationFrameBufferCount`, there is no longer any fence to synchronize on.

	The only reason to set this value to `-1` is for additional `Renderer`s: It's okay (and even encouraged for performance) to disable synchronization on secondary/tertiary/etc `Renderer`s as long as at least one is still synchronizing commands on the GPU. 
	
	Make sure that one `Renderer` in your application (usually the "primary" one, i.e. the last one in the loop that renders __every__ frame) always has a non-negative value for `GpuSynchronizationFrameBufferCount`.

	If you have no `Renderer` that is guaranteed to render every frame/iteration, you should not set `GpuSynchronizationFrameBufferCount` to `-1` on any `Renderer`.
	
	This does not apply when using a compositor:

	* When rendering several renderers through a [compositor](compositing.md), the compositor synchronizes with just one of them (see [Compositing: Synchronization](compositing.md#synchronization)). You only need to make sure at least one has a value of `0` or higher.
	* When calling `Render()` on several __renderers__ yourself (e.g. one per window), every renderer with a value of `0` or higher synchronizes separately, meaning your loop may wait more than once per frame. Instead, set all but one renderer to `-1`, and leave the one that's guaranteed to render every frame (usually your main renderer) at a non-negative value.

### Input Latency

*Input latency* is the time between the user pressing a key or moving the mouse, and the result of that input appearing on screen. In TinyFFR it's made up of:

* The time until the next loop iteration reads the input (input is only read in `IterateOnce()`).
* The time your code takes to update the scene and call `Render()`.
* The time the frame spends queued behind other frames on the GPU (see [GPU Synchronization](#gpu-synchronization)).
* The time the GPU takes to draw the frame.
* The time the frame waits for the display to refresh (if [vsync](#vsync) is enabled).

The defaults suit most applications. If your application needs to respond as quickly as possible (e.g. a fast-paced competitive game), reduce the frames in flight and consider disabling vsync. Some common configurations are:

=== "Smooth (Default)"

	```csharp
	using var factory = new LocalTinyFfrFactory(); // (1)!
	using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window); // (2)!
	using var loop = factory.ApplicationLoopBuilder.CreateLoop(); // (3)!
	```

	1.	VSync is enabled by default: No tearing, and frames are paced by the display.

	2.	Up to 3 frames in flight (the default).

	3.	No frame rate cap; the display's refresh rate paces the loop.

=== "Low Latency"

	```csharp
	using var factory = new LocalTinyFfrFactory(
		rendererBuilderConfig: new RendererBuilderConfig { EnableVSync = false } // (1)!
	);
	using var renderer = factory.RendererBuilder.CreateRenderer(
		scene, 
		camera, 
		window, 
		new RendererCreationConfig { GpuSynchronizationFrameBufferCount = 1 } // (2)!
	);
	using var loop = factory.ApplicationLoopBuilder.CreateLoop(); // (3)!
	```

	1.	Frames are shown as soon as they're ready (with screen tearing).

	2.	At most one frame in flight. Use `0` to remove all CPU/GPU overlap, for the lowest latency at the cost of frame rate.

	3.	Uncapped. You may wish to set a cap anyway (e.g. `frameRateCapHz: 240`) to stop the application using all available CPU and GPU time.

=== "Power Saving"

	```csharp
	using var factory = new LocalTinyFfrFactory(); // (1)!
	using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
	using var loop = factory.ApplicationLoopBuilder.CreateLoop(frameRateCapHz: 30); // (2)!
	```

	1.	VSync is enabled, preventing tearing.

	2.	The loop never runs more than 30 times per second, so roughly half as many frames are rendered as on a 60Hz display.

## Waiting for the GPU

<span class="def-icon">:material-code-block-parentheses:</span> `WaitForGpu()`

:   Blocks until the GPU has finished rendering every frame previously queued with `Render()`.

<span class="def-icon">:material-code-block-parentheses:</span> `RenderAndWaitForGpu()`

:   Calls `Render()` and then `WaitForGpu()`. When it returns, the frame has been fully rendered to the target window or buffer.

Compositors offer the same methods for all of their renderers at once: `WaitForGpu()` and `RenderAllAndWaitForGpu()` (see [Compositing](compositing.md)).

Waiting for the GPU means the CPU and GPU can no longer work at the same time, which severely reduces throughput; so these methods generally shouldn't be called every frame in a realtime loop. They're useful when:

* **You need the frame to be finished before continuing**: For example when rendering frames one at a time to a [`RenderOutputBuffer`](capturing_render_output.md) for offline processing, or before timing how long a frame took to render.
* **You've just disposed lots of resources**: The GPU memory used by disposed resources (meshes, textures, etc.) is only released once the GPU has finished with every frame that used them, which otherwise happens gradually over the following frames. Calling `WaitForGpu()` after disposing them (e.g. when unloading a level) releases that memory straight away.

`CaptureScreenshot()` and [model picking](ray_casting_pixel_picking_projecting.md#pixel-picking) (`PickModelInstanceFromRenderSurface()`) already do their own separate render and wait for it to finish, so you don't need to call `WaitForGpu()` before using them.
