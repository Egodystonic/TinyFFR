---
title: Scenes & Rendering
description: This page explains the concept of scenes, cameras, and renderers in TinyFFR.
---

`Scenes` are essentially "containers" for model instances and lights. 

`Renderers` take a `Scene` and a `Camera` and *render* them to a target (e.g. a `Window`).

## Scenes

You can add the same objects to multiple scenes simultaneously and render them at different times according to some logic.

Attempting to add an object already in a scene to that same scene again has no effect; it is an [idempotent](https://en.wikipedia.org/wiki/Idempotence) operation. The same applies to removing objects, even if they were never added to the scene in the first place.

### Backdrops

Scenes can include a backdrop; either a flat colour or an HDR image.

Use `SetBackdrop()` to set the backdrop to either a colour or an `EnvironmentCubemap` containing a loaded HDR image. An `EnvironmentCubemap` is a resource and can be created with the factory's `AssetLoader`.

#### Indirect Lighting

By default, all objects in the scene are globally lit by the backdrop. 

When using a plain colour backdrop the illumination is the same colour uniformly applied to every surface. When using an environment cubemap the lighting is applied to each surface differently according to the brightness and colour of the sky facing that surface.

The ambient occlusion map used for any material is used to dim indirect lighting.

You can also set a backdrop with indirect lighting disabled, if desired (see below).

#### Backdrop Methods

`Scene` has the following functions for controlling backdrops and indirect lighting:

<span class="def-icon">:material-code-block-parentheses:</span> `SetBackdrop(ColorVect color, float indirectLightingIntensity = 1f)`

:   Sets the backdrop of the scene to the given `color`.

	You can also optionally set a value for `indirectLightingIntensity`, where `1f` is the default (meaning 100%). Setting this value will not affect the backdrop color.

<span class="def-icon">:material-code-block-parentheses:</span> `SetBackdrop(EnvironmentCubemap cubemap, float backdropIntensity = 1f, Rotation? rotation = null)`

:   Sets the backdrop of the scene to the given `cubemap`.

	You can also optionally set a value for `backdropIntensity`, where `1f` is the default (meaning 100%). Setting this value changes the intensity of indirect lighting and also the brightness/intensity of the backdrop.

	Finally, you can also set an optional `rotation` value which can be used to rotate the skybox texture/cubemap.

<span class="def-icon">:material-code-block-parentheses:</span> `SetBackdropWithoutIndirectLighting(ColorVect color)`

:   Sets the backdrop of the scene to the given `color`.

	This method will simply set a backdrop color but disables all indirect lighting. This means objects will appear to be pitch-black unless lit by another light source.

<span class="def-icon">:material-code-block-parentheses:</span> `SetBackdropWithoutIndirectLighting(EnvironmentCubemap cubemap, float backdropIntensity = 1f, Rotation? rotation = null)`

:   Sets the backdrop of the scene to the given `cubemap`.

	This method will set the environment cubemap as the backdrop/sky, but will not use it to apply any indirect lighting. This means objects will appear to be pitch-black unless lit by another light source.

	You can still set the intensity/brightness of the cubemap using the optional `backdropIntensity` value. This will only adjust the brightness of the sky.

	Finally, you can also set an optional `rotation` value which can be used to rotate the skybox texture/cubemap.

<span class="def-icon">:material-code-block-parentheses:</span> `RemoveBackdrop()`

:   If you prefer no backdrop and no indirect lighting, you can use this method to have a scene with no backdrop at all.

	For most intents and purposes this is the same as setting the backdrop to a solid black colour.

<span class="def-icon">:material-code-block-parentheses:</span> `Scene.LuxToBrightness(float lux)`

:   This static method can convert a real-life value in lux to a brightness/intensity value used in the methods above.

	For example, to set a backdrop with an illuminance of 30000 lux: `#!csharp myScene.SetBackdrop(StandardColor.White, Scene.LuxToBrightness(30_000f));`

<span class="def-icon">:material-code-block-parentheses:</span> `Scene.BrightnessToLux(float brightness)`

:   This static method reverses the conversion made in `LuxToBrightness()`.

## Cameras

Scenes are ultimately rendered to a render target (such as a window) by a `Renderer`, using a `Camera`. The `Camera` captures the scene from a specific direction and with specific parameters. The renderer takes that capture and turns it in to a texture/frame.

Cameras offer the following controls:

<span class="def-icon">:material-card-bulleted-outline:</span> `Position`

:   Where in the scene/world the camera should capture its next frame from.

<span class="def-icon">:material-card-bulleted-outline:</span> `ViewDirection`

:   Where the camera should be looking.

	Note that TinyFFR keeps this value auto-orthogonalized with the `UpDirection`. Changing one of either `ViewDirection` or `UpDirection` may automatically change the other.

<span class="def-icon">:material-card-bulleted-outline:</span> `UpDirection`

:   Which way "up" the camera should be rotated.

	Note that TinyFFR keeps this value auto-orthogonalized with the `ViewDirection`. Changing one of either `ViewDirection` or `UpDirection` may automatically change the other.

<span class="def-icon">:material-card-bulleted-outline:</span> `HorizontalFieldOfView`

:   Represents how wide the viewing angle is as captured by the camera lens.

	This property lets you get/set the viewing angle as specified in the horizontal field (i.e. this property sets the amount of scene seen from left-to-right on the rendered frame).
	
	Changing this will automatically change `VerticalFieldOfView` according to the currently-set `AspectRatio`.

	Must be between `Camera.FieldOfViewMin` (0°) and `Camera.FieldOfViewMax` (360°).

<span class="def-icon">:material-card-bulleted-outline:</span> `VerticalFieldOfView`

:   Represents how wide the viewing angle is as captured by the camera lens.

	This property lets you get/set the viewing angle as specified in the vertical field (i.e. this property sets the amount of scene seen from top-to-bottom on the rendered frame).
	
	Changing this will automatically change `HorizontalFieldOfView` according to the currently-set `AspectRatio`.

	Must be between `Camera.FieldOfViewMin` (0°) and `Camera.FieldOfViewMax` (360°).

<span class="def-icon">:material-card-bulleted-outline:</span> `AspectRatio`

:   Defines the ratio between the output frame's width and height. For example, for a 1920 x 1080 output, this value should be `1920f/1080f`.

	By default, the `Renderer` will automatically update this value on the camera as appropriate for the render target/window.

	If you wish to control this value manually, specify a `RendererCreationConfig` and set `AutoUpdateCameraAspectRatio` to `false` when calling `CreateRenderer()` on the `RendererBuilder`.

<span class="def-icon">:material-card-bulleted-outline:</span> `NearPlaneDistance`

:   This sets how close something has to be to the camera before it is no longer rendered.

	Setting this lower will let you render things closer to the camera, but may cause [Z-fighting](https://en.wikipedia.org/wiki/Z-fighting) for objects further away unless you also reduce the `FarPlaneDistance`.

	In general you should try to keep the `FarPlaneDistance` no more than 5 orders of magnitude more than `NearPlaneDistance`, 6 at an absolute max. TinyFFR will automatically adjust the `FarPlaneDistance` to make sure it is never more than 1E6 times higher than `NearPlaneDistance`.

	The default value is `CameraCreationConfig.DefaultNearPlaneDistance` (0.15m). This can not be 0 due to [perspective divide](https://stackoverflow.com/questions/17269686/why-do-we-need-perspective-division); the lowest permitted value is `Camera.NearPlaneDistanceMin` (1E-5m).

<span class="def-icon">:material-card-bulleted-outline:</span> `FarPlaneDistance`

:   This sets how far something can be from the camera before it is no longer rendered.

	Setting this higher will let you render things further from the camera, but may cause [Z-fighting](https://en.wikipedia.org/wiki/Z-fighting) for objects further away unless you also increase the `NearPlaneDistance`.

	In general you should try to keep the `FarPlaneDistance` no more than 5 orders of magnitude more than `NearPlaneDistance`, 6 at an absolute max. TinyFFR will automatically adjust the `FarPlaneDistance` to make sure it is never more than 1E6 times higher than `NearPlaneDistance`.

	The default value is `CameraCreationConfig.DefaultFarPlaneDistance` (5000m). This can not be lower than or equal to `NearPlaneDistance`.

<span class="def-icon">:material-code-block-parentheses:</span> `SetViewAndUpDirection(Direction newViewDirection, Direction newUpDirection, bool enforceOrthogonality = true)`

:   Sets the `ViewDirection` and `UpDirection` together. This can be useful if you want to avoid the auto-orthogonalization calculations when setting them separately.

	If `enforceOrthogonality` is `false`, TinyFFR will not orthogonalize the two directions at all. If they are not orthogonal this can lead to some unexpected or even confusing perspective distortions.

<span class="def-icon">:material-code-block-parentheses:</span> `LookAt(Location target)`
<span class="def-icon">:material-code-block-parentheses:</span> `LookAt(Location target, Direction upDirection)`

:   Rotates the camera to look at the specified `target`. 

	If you specify an `upDirection`, the camera will maintain that direction as its `UpDirection`, auto-orthogonalizing with the resultant `ViewDirection`. If you do *not* specify an `upDirection`, the camera will simply rotate its `ViewDirection` to face the `target` and auto-orthogonalize the existing `UpDirection` according to that rotation.

	In most cases, you will probably want to specify an `upDirection`. Without an `upDirection` the camera will likely spin around over time.


## Renderers

Renderers must be constructed with a scene to render, a camera to capture the scene with, and a render target to output to (e.g. a [Window](displays_and_windows.md)).

When creating a `Renderer` you can supply an optional `RendererCreationConfig` that has the following options:

<span class="def-icon">:material-card-bulleted-outline:</span> `AutoUpdateCameraAspectRatio`

:   If `true`, when the target surface (e.g. the `Window`)'s aspect ratio changes (i.e. its dimensions change), the renderer will automatically update the `AspectRatio` property of the `Camera` it was built with.

	This is useful if the renderer is associated with one camera and one target/Window only; but may not be what you want in a multi-renderer setup. If you set this to `false` you will be responsible for setting the `AspectRatio` of the camera manually.

	Defaults to `true`.

<span class="def-icon">:material-card-bulleted-outline:</span> `GpuSynchronizationFrameBufferCount`

:   This is an advanced option that controls how this renderer synchronizes the CPU with the GPU; it controls how many frames can be "in progress" on the GPU side before the CPU waits in order to not get too far ahead.

	* Values between `1` and `5` set a maximum number of frames that can be "queued" or "in progress" before the call to `Render()` will block the calling thread. A higher value generally increases your average throughput/FPS, but can also increase input latency.

	* A value of `0` completely stops all asynchronous rendering. This means every call to `Render()` will __always__ block the calling thread until the frame is fully rendered and displayed on the target/Window. Setting this value can drastically lower average throughput/FPS; a value of at least `1` is recommended in most scenarios.

	* A value of `-1` disables synchronization entirely. This means `Render()` will __never__ block the calling thread; but over time commands submitted to the GPU may exceed the GPU's capability to keep up, resulting in stuttering or even errors. Setting this value is only recommended when using a multi-renderer setup (set all renderers except your last/"primary" renderer to `-1`).

	Defaults to `3`.

	???+ warning "Setting -1 also disables resource disposal protection"
		Another reason to never set this value to `-1` for *all* your renderers is that resource disposal is no longer synchronized.

		Behind the scenes, TinyFFR ensures that your resources are not deleted from GPU memory until scenes using them are fully rendered. This may be *after* you call `.Dispose()` on that resource; TinyFFR uses GPU synchronization [fences](https://en.wikipedia.org/wiki/Memory_barrier) to protect against use-after-dispose race conditions. When you have __no__ renderers with non-negative values for `GpuSynchronizationFrameBufferCount`, there is no longer any fence to synchronize on.

		The only reason to set this value to `-1` is for additional `Renderer`s: It's okay (and even encouraged for performance) to disable synchronization on secondary/tertiary/etc `Renderer`s as long as at least one is still synchronizing commands on the GPU. 
		
		Make sure that one `Renderer` in your application (usually the "primary" one, i.e. the last one in the loop that renders __every__ frame) always has a non-negative value for `GpuSynchronizationFrameBufferCount`.

		If you have no `Renderer` that is guaranteed to render every frame/iteration, you should not set `GpuSynchronizationFrameBufferCount` to `-1` on any `Renderer`.

### Renderer Members

Every `Renderer` has the following members:

<span class="def-icon">:material-code-block-parentheses:</span> `Render()`

:   Renders the configured scene, captured using the configured camera, to the configured target output (e.g. a `Window`).

<span class="def-icon">:material-code-block-parentheses:</span> `WaitForGpu()`

:   As described above in the documentation for `GpuSynchronizationFrameBufferCount`, invoking `Render()` actually enqueues a command list on the GPU which will then be completed at a later time.

	In some circumstances you may wish to pause the current application on the CPU side until the GPU has completed its execution and outputted all remaining frames. `WaitForGpu()` will stall the calling thread until this time.

	In other words, by the time `WaitForGpu()` returns, all previous frames queued by a call to `Render()` will have completed. Any handlers attached to `RenderOutputBuffer`s (described below) will have executed.
	
	When running a render loop, `WaitForGpu()` causes a stall in the render pipeline, meaning your application will appear to stutter momentarily. Invoking `WaitForGpu()` frequently may drastically lower your maximum framerate.

<span class="def-icon">:material-code-block-parentheses:</span> `RenderAndWaitForGpu()`

:   A convenience method that invokes `Render()` and then immediately invokes `WaitForGpu()`.

	You can use this function in place of `Render()` when you want your scene render (and correlated callbacks) to be completed by the time the render function returns.

	Just like `WaitForGpu()` this function has performance implications (see `WaitForGpu()` documentation above).

<span class="def-icon">:material-code-block-parentheses:</span> `SetQuality()`

:   Changes the render quality settings of this `Renderer`. All subsequent calls to `Render()` will use the new quality settings.

<span class="def-icon">:material-code-block-parentheses:</span> `CaptureScreenshot(...)`

:   This function can either take an argument specifying the file path of a BMP file to be written with the captured screenshot, or a handler can be specified for more manual processing of captured frame data.

	* When specifying a bitmap file path, be aware that this function may throw an `IOException` if the file path could not be written to.

	* When specifying a handler, see the documentation below for `RenderOutputBuffer.ReadNextFrame()` for more information on the arguments.

	You may also optionally specify the capture resolution for the screenshot (`captureResolution`). Leaving this as `null` will mean the captured screenshot will use the same resolution as the configured render target (e.g. the `Window`).

	The `presentFrameTopToBottom` parameter (only present when specifying a `handler`) is also optional (`false` by default). If `true`, the data passed to the `handler` will be arranged such that the first row in the data represents the top of the texture. If `false` (the default), the data will be arranged from bottom-to-top.

	Note that this function re-renders the configured scene with the configured camera and then stalls the GPU pipeline until the output is received before immediately executing the bitmap file write or handler callback. This has two implications:
	
	* `CaptureScreenshot()` takes a significant toll on performance, similar to `WaitForGpu()` (but worse). If you want to continuously capture frames, it is preferable to use a `RenderOutputBuffer` (see below).
	* The current setup of the configured scene and camera is used at the time `CaptureScreenshot()` is invoked, meaning if the camera or scene have changed since the last `Render()` call, the captured screenshot will not match the last rendered output.

## RenderOutputBuffers

Instead of rendering directly to a window, you can also render to an internal texture buffer; optionally re-displaying this texture elsewhere on an in-scene surface or copying its data to memory/file. In TinyFFR, these buffers are referred to as `RenderOutputBuffer`s.

Creating a `RenderOutputBuffer` is done via the renderer builder:

```csharp
using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(textureDimensions: (1024, 1024)); // (1)!
```

1. The texture dimensions of the buffer is the only required parameter. In this example we're creating a 1024x1024 texture.

You can then pass a `RenderOutputBuffer` to `CreateRenderer()` (instead of a `Window`):

```csharp
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, buffer);
```

### RenderOutputBuffer Members

When invoking `Render()` on the `renderer`, the scene captured with the camera will be rendered on to the buffer. You can use the following various members to access or use the buffer data:

<span class="def-icon">:material-card-bulleted-outline:</span> `TextureDimensions`

:   Returns the X/Y dimensions of the buffer.

<span class="def-icon">:material-code-block-parentheses:</span> `StartReadingFrames(handler, presentFrameTopToBottom)`
<span class="def-icon">:material-code-block-parentheses:</span> `ReadNextFrame(handler, presentFrameTopToBottom)`

:   `StartReadingFrames` instructs TinyFFR that you wish to begin continuously reading all frames rendered to this buffer from this point onwards, until stopped.

	`ReadNextFrame` instructs TinyFFR that you wish to read back the *next* rendered scene from this buffer. All frames rendered after the next one will not be handled.

	The `handler` should be either an `Action<XYPair<int>, ReadOnlySpan<TexelRgb24>>` or its [function pointer equivalent](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers). 
	
	* The first parameter to the handler is the texture dimensions of the output buffer (`X` columns and `Y` rows). The total `Length` of the given span will be equal to the `Area` property of this `XYPair`.
		
	* The second is a span of texels containing the row-major(1) frame data arranged without gaps(2). The texel data is only valid for as long as the handler is executing.
		{ .annotate }

		1. 	The first `X` texels in the span will constitute the first row of texel data; the second `X` texels will constitute the second, etc; for a total of `Y` rows.

			By default, the first row is considered to be the *bottom* of the texture data (as this matches the 2D texture [convention](/concepts/conventions.md#textures-materials) TinyFFR uses). However, this can be reversed with the "`presentFrameTopToBottom`" parameter described below.

		2.	There is no "stride" or blank data at the end of rows; each row is packed without padding and the beginning of one row starts immediately after the end of the previous one in the data.

	The `presentFrameTopToBottom` parameter is optional (`false` by default). If `true`, the data passed to the `handler` will be arranged such that the first row in the data represents the top of the texture. If `false` (the default), the data will be arranged from bottom-to-top.

<span class="def-icon">:material-code-block-parentheses:</span> `StopReadingFrames(cancelQueuedFrames)`

:   This instructs TinyFFR that you wish to stop a previously-started continuous frame reading operation.

	If `cancelQueuedFrames` is true, any previously-rendered frames that are currently still queued in the GPU command pipeline will not be passed to your `handler`. Otherwise, those remaining frames will still be passed to the `handler`. Any frames `Render()`ed after `StopReadingFrames()` is invoked will not be passed to the previously-set `handler` either way.

???+ note "Single Handler Restriction"
	Note that each `RenderOutputBuffer` can only have one readback handler set at any given time. 
	
	That means that *only* the most-recent handler passed to either `StartReadingFrames` or `ReadNextFrame` will be invoked. Calling `StartReadingFrames` or `ReadNextFrame` again will "erase" the previously-set handler.

	If you wish to execute multiple actions for a `RenderOutputBuffer` frame, you should use a single `handler` and pass the received data to multiple further sub-functions.

???+ warning "Asynchrony in GPU Command Pipeline"
	When calling `Render()` for a `Renderer` created with a `RenderOutputBuffer` target with a handler set, the handler may not be invoked immediately or at all until more frames are rendered later on. This is because (by default) frames are queued to be rendered on the GPU, and the GPU asynchronously executes the commandlist. TinyFFR checks for previously-completed frames when `Render()` is called for subsequent frames.

	If you require immediate readback for set handlers when calling `Render()`, consider using `RenderAndWaitForGpu()` instead. Note however that this will quite adversely affect framerate in a render loop. 
	
	You can also call `WaitForGpu()` at specific points after dispatching multiple `Render()` calls in order to force all dispatched frames to be handled at that point. This also adversely affects framerate in a render loop.

	See also the documentation for `GpuSynchronizationFrameBufferCount` and `WaitForGpu()` above.

<span class="def-icon">:material-code-block-parentheses:</span> `CreateDynamicTexture()`

:   This method can be used to create a `Texture` resource that always contains the last-rendered frame to this `RenderOutputBuffer` as its data.

	You can pass this `Texture` to other parts of the library just as you would any other `Texture`; including setting it as the data for a color, normal, or ORM map on a `Material`.

	Note that the lifetime of this `Texture` is forever intrinsically tied to its "parent" `RenderOutputBuffer`. You must dispose all `Texture`s created this way before disposing the parent `RenderOutputBuffer`.