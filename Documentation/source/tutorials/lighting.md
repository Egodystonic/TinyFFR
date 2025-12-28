---
title: Lighting
description: This page explains the concept of lighting in TinyFFR.
---

## Light Types

### Point Lights

Point lights illuminate all objects uniformly in a sphere centered on their `Position`.

<span class="def-icon">:material-card-bulleted-outline:</span> `Brightness`

:   Gets or sets the brightness of this light (i.e. how strongly it illuminates objects).

	The default value is `1f`, and the perceived brightness of the light is designed to scale with this value (so `2f` indicates twice as bright, `0.5f` half as bright, etc).

	If you prefer to work with real-life units, you can use the static methods `LumensToBrightness()` and `BrightnessToLumens()` to convert a value in lumens to/from a `Brightness`. The default value (corresponding to a `Brightness` of `1f`) is accessible via the constant `DefaultLumens`.

	See also the `AdjustBrightnessBy()` and `ScaleBrightnessBy()` methods that in-place add to or multiply the `Brightness` of the light respectively.

<span class="def-icon">:material-card-bulleted-outline:</span> `Color`, `ColorHue`, `ColorSaturation`, `ColorLightness`

:   Gets or sets the colour of the light (or its hue/saturation/lightness).

	These properties should be used to set the ratio of red, green, and blue in the output colour of the light, not its brightness (the `Alpha` value of the `Color` property is ignored).
	
	Similarly, note that the colour's *lightness* is not the same as the light's `Brightness` property. The `ColorLightness` is a property of the tone of the light in the standard HSL model. The `ColorLightness` can not be increased past `1f`. To adjust the overall brightness of the light, modify the `Brightness` property.

	See also the `AdjustColorHueBy()`, `AdjustColorSaturationBy()` and `AdjustColorLightnessBy()` methods which allow you to in-place edit these properties of the light's colouration.

<span class="def-icon">:material-card-bulleted-outline:</span> `CastsShadows`

:   Whether or not shadows should be cast from this light. Defaults to `false` as shadow casting is performance-intensive.

	See the [Shadows](#shadows) section below for more information.

<span class="def-icon">:material-card-bulleted-outline:</span> `Position`

:   Gets or sets the position of this point light in the world/scene.

	This sets the centre point of the 'sphere' of illumination cast by this light.

<span class="def-icon">:material-card-bulleted-outline:</span> `MaxIlluminationRadius`

:   Gets or sets the size of the 'sphere' of light cast by this point light.

	Note that although this property does not directly affect the brightness of the light, it does affect how quickly the brightness falls off as objects move away from the centre `Position`, meaning it can have an impact on perceived brightness.

	Additionally, this property has a performance implication- a smaller radius will have a lesser performance impact than a larger one. Therefore, consider setting this value as small as possible for your scene.

### Spot Lights

Spot lights illuminate all objects within a cone pointing in a given `ConeDirection` from their `Position`.

<span class="def-icon">:material-card-bulleted-outline:</span> `Brightness`

:   Gets or sets the brightness of this light (i.e. how strongly it illuminates objects).

	The default value is `1f`, and the perceived brightness of the light is designed to scale with this value (so `2f` indicates twice as bright, `0.5f` half as bright, etc).

	If you prefer to work with real-life units, you can use the static methods `LumensToBrightness()` and `BrightnessToLumens()` to convert a value in lumens to/from a `Brightness`. The default value (corresponding to a `Brightness` of `1f`) is accessible via the constant `DefaultLumens`.

	See also the `AdjustBrightnessBy()` and `ScaleBrightnessBy()` methods that in-place add to or multiply the `Brightness` of the light respectively.

<span class="def-icon">:material-card-bulleted-outline:</span> `Color`, `ColorHue`, `ColorSaturation`, `ColorLightness`

:   Gets or sets the colour of the light (or its hue/saturation/lightness).

	These properties should be used to set the ratio of red, green, and blue in the output colour of the light, not its brightness (the `Alpha` value of the `Color` property is ignored).
	
	Similarly, note that the colour's *lightness* is not the same as the light's `Brightness` property. The `ColorLightness` is a property of the tone of the light in the standard HSL model. The `ColorLightness` can not be increased past `1f`. To adjust the overall brightness of the light, modify the `Brightness` property.

	See also the `AdjustColorHueBy()`, `AdjustColorSaturationBy()` and `AdjustColorLightnessBy()` methods which allow you to in-place edit these properties of the light's colouration.

<span class="def-icon">:material-card-bulleted-outline:</span> `CastsShadows`

:   Whether or not shadows should be cast from this light. Defaults to `false` as shadow casting is performance-intensive.

	See the [Shadows](#shadows) section below for more information.

<span class="def-icon">:material-card-bulleted-outline:</span> `Position`

:   Gets or sets the position of the cone's "starting point", e.g. where the spot light is being "shone" from.

<span class="def-icon">:material-card-bulleted-outline:</span> `ConeDirection`

:   Gets or sets the direction the cone is pointing towards.

	Together with the `Position`, this property indicates which objects may be within the spotlight's "cone of illumination".

<span class="def-icon">:material-card-bulleted-outline:</span> `MaxIlluminationDistance`

:   Gets or sets the "length" of the spotlight cone.

	Objects that are further than the `MaxIlluminationDistance` from the `Position` of this light will not be affected by it.

	Note that although this property does not directly affect the brightness of the light, it does affect how quickly the brightness falls off as objects move away from the starting `Position`, meaning it can have an impact on perceived brightness.

	Additionally, this property has a performance implication- a smaller value will have a lesser performance impact than a larger one. Therefore, consider setting this value as small as possible for your scene.

<span class="def-icon">:material-card-bulleted-outline:</span> `ConeAngle`

:   Controls the width of the spotlight "cone"/beam. Must be between `1°` and `180°`.

<span class="def-icon">:material-card-bulleted-outline:</span> `IntenseBeamAngle`

:   Controls the width of a more intense "inner" beam/cone of increased brightness. Must be between `1°` and `180°`. This inner beam is required to make spotlights look more natural. 
	
	By definition, the value of this property must be lower than or equal to `ConeAngle`. Setting `IntenseBeamAngle` to a value higher than `ConeAngle` will increase both to the new `IntenseBeamAngle`. Setting `ConeAngle` to a value lower than `IntenseBeamAngle` will decrease both to the new `ConeAngle` value.

#### High-Quality Spotlights

When creating a `SpotLight` via the `LightBuilder`, you can set optionally set an `IsHighQuality` boolean value. By default this is `false`; setting it to `true` increases the quality of the spotlight illumination render at a slight cost to performance.

### Directional Lights

Directional lights illuminate everything in the scene via a source "shining" in one specific `Direction`. The light source can be considered as being "infinitely" far away for all practical purposes (e.g. like a sun or some other celestial body).

???+ warning "Max One Directional Light per Scene"
	Each scene can only have one `DirectionalLight`.

	Attempting to `Add()` a second `DirectionalLight` to a `Scene` that already contains a first `DirectionalLight` will have no effect, and the light will not be added to scene.

<span class="def-icon">:material-card-bulleted-outline:</span> `Brightness`

:   Gets or sets the brightness of this light (i.e. how strongly it illuminates objects).

	The default value is `1f`, and the perceived brightness of the light is designed to scale with this value (so `2f` indicates twice as bright, `0.5f` half as bright, etc).

	If you prefer to work with real-life units, you can use the static methods `LuxToBrightness()` and `BrightnessToLux()` to convert a value in lux to/from a `Brightness`. The default value (corresponding to a `Brightness` of `1f`) is accessible via the constant `DefaultLux`.

	See also the `AdjustBrightnessBy()` and `ScaleBrightnessBy()` methods that in-place add to or multiply the `Brightness` of the light respectively.

<span class="def-icon">:material-card-bulleted-outline:</span> `Color`, `ColorHue`, `ColorSaturation`, `ColorLightness`

:   Gets or sets the colour of the light (or its hue/saturation/lightness).

	These properties should be used to set the ratio of red, green, and blue in the output colour of the light, not its brightness (the `Alpha` value of the `Color` property is ignored).
	
	Similarly, note that the colour's *lightness* is not the same as the light's `Brightness` property. The `ColorLightness` is a property of the tone of the light in the standard HSL model. The `ColorLightness` can not be increased past `1f`. To adjust the overall brightness of the light, modify the `Brightness` property.

	See also the `AdjustColorHueBy()`, `AdjustColorSaturationBy()` and `AdjustColorLightnessBy()` methods which allow you to in-place edit these properties of the light's colouration.

<span class="def-icon">:material-card-bulleted-outline:</span> `CastsShadows`

:   Whether or not shadows should be cast from this light. Defaults to `false` as shadow casting is performance-intensive.

	See the [Shadows](#shadows) section below for more information.

<span class="def-icon">:material-card-bulleted-outline:</span> `Direction`

:   Gets or sets the direction the light is "emanating" or pointing *towards*. E.g. if this is set to `Direction.Down`, this light will illuminate the upward-facing surfaces of all objects in the scene.

	Another way to think of this property is as being the *opposite* of the direction towards the light source itself (e.g. if this property is set to `Direction.Down`, you could consider the "sun" as being `Direction.Up` in the scene).

	Together with the `Position`, this property indicates which objects may be within the spotlight's "cone of illumination".

#### Sun Discs

When creating a `DirectionalLight` with the `LightBuilder`, you can set an optional boolean value "`showSunDisc`". By default this value is `false`, but if set to `true` a "sun" will be drawn in the sky indicating the "directional light source" of the `DirectionalLight`.

The sun disc's colour will be set according to the `Color` property of the `DirectionalLight`.

???+ warning "Requires a Backdrop"
	The sun disc will only be shown on scenes with a backdrop set.

	If necessary, you can set a backdrop of colour `ColorVect.Black` to have a backdrop with no actual background image or colour.

<span class="def-icon">:material-code-block-parentheses:</span> `SetSunDiscParameters(SunDiscConfig config)`

:   When the sun disc is enabled, you can control its appearance with this method.

	The `config` parameter is a `SunDiscConfig` object that has the following properties:

	* `Scaling`: The size of the sun disc. The default (`1f`) gives a result comparable with the real sun on Earth.
	* `FringingScaling`: Sets the amount of fringing ("haloing") on the sun disc. The default (`1f`) gives a result comparable with the real sun on Earth.
	* `FringingOuterRadiusScaling`: Sets how far around the inner sun disc the fringing ("haloing") expands by. The default (`1f`) gives a result comparable with the real sun on Earth.

	By default, the sun will be shown with size and fringing comparable to the real sun on Earth.

## Shadows

When creating any light using the `LightBuilder`, the factory method (or creation config object) will accept a `bool` indicating whether the light should `castShadows`.

Additionally, you can turn shadow casting on/off for any light source with the `CastsShadows` property.

By default, lights do not cast shadows as there is a significant performance cost. You should only enable shadow casting on a handful of lights at most to maintain a decent framerate.

### Shadow Quality

You can set the quality of rendered shadows using the `SetQuality()` method of the `Renderer` you're using to render the scene with shadow-casting lights. 

## Light "Base" Type

All light instances can be cast to `Light` (e.g. `#!csharp var light = (Light) myPointLight;`). A `Light` instance lets you get/set any property common to all light types (`Color[Hue/Saturation/Lightness]`, `Brightness`, and `CastsShadows`).

You can cast a `Light` instance back to its "real" type (e.g. `#!csharp var pointLight = (PointLight) myLight;`); note that this cast will throw an exception if you attempt to cast to the wrong type (e.g. you try to cast to `PointLight` when it is in fact a `SpotLight`). You can check a `Light`'s type with its `Type` property.

Note that the cast from a specific light type to `Light` is implicit (meaning you don't need to actually cast your specific light instances to e.g. pass a light instance to a method that takes a `Light`).