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