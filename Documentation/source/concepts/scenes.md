---
title: Scenes
description: This page explains the concept of scenes, cameras, and renderers in TinyFFR.
---

Scenes are essentially "containers" for model instances and lights. You can not add the same model instance or light to a scene more than once, but you can add them to multiple scenes.

## Backdrops

Scenes can include a backdrop; either a flat colour or an HDR image.

Use `SetBackdrop()` to set the backdrop to either a colour or an `EnvironmentCubemap` containing a loaded HDR image. An `EnvironmentCubemap` is a resource and can be created with the factory's `AssetLoader`.

### Indirect Lighting

By default, all objects in the scene are globally lit by the backdrop. 

When using a plain colour backdrop the illumination is the same colour uniformly applied to every surface. When using an environment cubemap the lighting is applied to each surface differently according to the brightness and colour of the sky facing that surface.

The ambient occlusion map used for any material is used to dim indirect lighting.

You can also set a backdrop with indirect lighting disabled, if desired (see below).

### Backdrop Methods

`Scene` has the following functions for controlling backdrops and indirect lighting:

<span class="def-icon">:material-code-block-parentheses:</span> `SetBackdrop(ColorVect color, float indirectLightingIntensity = 1f)`

:   Sets the backdrop of the scene to the given `color`.

	You can also optionally set a value for `indirectLightingIntensity`, where `1f` is the default (meaning 100%). Setting this value will not affect the backdrop color.

<span class="def-icon">:material-code-block-parentheses:</span> `SetBackdrop(EnvironmentCubemap cubemap, float backdropIntensity = 1f)`

:   Sets the backdrop of the scene to the given `cubemap`.

	You can also optionally set a value for `backdropIntensity`, where `1f` is the default (meaning 100%). Setting this value changes the intensity of indirect lighting and also the brightness/intensity of the backdrop.

<span class="def-icon">:material-code-block-parentheses:</span> `SetBackdropWithoutIndirectLighting(ColorVect color)`

:   Sets the backdrop of the scene to the given `color`.

	This method will simply set a backdrop color but disables all indirect lighting. This means objects will appear to be pitch-black unless lit by another light source.

<span class="def-icon">:material-code-block-parentheses:</span> `SetBackdropWithoutIndirectLighting(EnvironmentCubemap cubemap, float backdropIntensity = 1f)`

:   Sets the backdrop of the scene to the given `cubemap`.

	This method will set the environment cubemap as the backdrop/sky, but will not use it to apply any indirect lighting. This means objects will appear to be pitch-black unless lit by another light source.

	You can still set the intensity/brightness of the cubemap using the optional `backdropIntensity` value. This will only adjust the brightness of the sky.

<span class="def-icon">:material-code-block-parentheses:</span> `RemoveBackdrop()`

:   If you prefer no backdrop and no indirect lighting, you can use this method to have a scene with no backdrop at all.

	For most intents and purposes this is the same as setting the backdrop to a solid black colour.

<span class="def-icon">:material-code-block-parentheses:</span> `Scene.LuxToBrightness(float lux)`

:   This static method can convert a real-life value in lux to a brightness/intensity value used in the methods above.

	For example, to set a backdrop with an illuminance of 30000 lux: `#!csharp myScene.SetBackdrop(StandardColor.White, Scene.LuxToBrightness(30_000f));`

<span class="def-icon">:material-code-block-parentheses:</span> `Scene.BrightnessToLux(float brightness)`

:   This static method reverses the conversion made in `LuxToBrightness()`.

## Renderers & Cameras

Scenes are ultimately rendered to a render target (such as a window) by a `Renderer`, using a `Camera`. The `Camera` captures the scene from a specific direction and with specific parameters. The renderer takes that capture and turns it in to a texture/frame.

### Camera Controls

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

	The default value is `CameraCreationConfig.DefaultNearPlaneDistance` (0.1m). This can not be 0 due to [perspective divide](https://stackoverflow.com/questions/17269686/why-do-we-need-perspective-division); the lowest permitted value is `Camera.NearPlaneDistanceMin` (1E-5m).

<span class="def-icon">:material-card-bulleted-outline:</span> `FarPlaneDistance`

:   This sets how far something can be from the camera before it is no longer rendered.

	Setting this higher will let you render things further from the camera, but may cause [Z-fighting](https://en.wikipedia.org/wiki/Z-fighting) for objects further away unless you also increase the `NearPlaneDistance`.

	In general you should try to keep the `FarPlaneDistance` no more than 5 orders of magnitude more than `NearPlaneDistance`, 6 at an absolute max. TinyFFR will automatically adjust the `FarPlaneDistance` to make sure it is never more than 1E6 times higher than `NearPlaneDistance`.

	The default value is `CameraCreationConfig.DefaultFarPlaneDistance` (3000m). This can not be lower than or equal to `NearPlaneDistance`.

<span class="def-icon">:material-code-block-parentheses:</span> `SetViewAndUpDirection(Direction newViewDirection, Direction newUpDirection, bool enforceOrthogonality = true)`

:   Sets the `ViewDirection` and `UpDirection` together. This can be useful if you want to avoid the auto-orthogonalization calculations when setting them separately.

	If `enforceOrthogonality` is `false`, TinyFFR will not orthogonalize the two directions at all. If they are not orthogonal this can lead to some unexpected or even confusing perspective distortions.