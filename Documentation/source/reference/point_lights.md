---
title: Point Lights
description: Information on how to create and adjust point lights, and the properties shared by every kind of light.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * A point light emits light from a single position in every direction, like a light bulb. :material-arrow-right: [Point Lights](#point-lights)
    * Point lights have a colour, a brightness, and a maximum radius. :material-arrow-right: [Brightness](#brightness), [Colour](#colour), [Range](#range)
    * You can optionally enable shadows at increased performance cost. :material-arrow-right: [Shadows](#shadows)
    * Lights use real-world brightnesses, so a camera's exposure must suit the lighting in its scene. :material-arrow-right: [Brightness & Exposure](#brightness-exposure)

</div>

## Point Lights

```csharp
using var light = factory.LightBuilder.CreatePointLight( // (1)!
	new Location(0f, 2f, 0f), 
	StandardColor.LightingIncandescentBulb
); 
scene.Add(light); // (2)!

light.MoveBy(new Vect(1f, 0f, 0f)); // (3)!
light.Brightness = 0.5f; // (4)!
```

1.	Creates a point light at (0, 2, 0), with the warm colour of an incandescent light bulb.

2.	Adds the light to a scene. A light only illuminates the scenes it has been added to.

3.	Moves the light 1 unit to the right.

4.	Reduces the light's brightness.

A `PointLight` emits light from a single position equally in every direction, like a light bulb or a candle flame. Objects close to the light are lit more strongly than those further away.

Like objects, lights must be [added to a scene](scenes.md#adding-removing-contents) before they have any effect, and a single light can be added to several scenes. Lights are resources, so must be disposed when no longer needed.

### Creating Point Lights

Point lights are created with `factory.LightBuilder.CreatePointLight()`, which accepts the following optional parameters (or alternatively a `PointLightCreationConfig` with equivalent properties):

<span class="def-icon">:material-code-json:</span> `position`

:   Where the light is. Defaults to the origin.

<span class="def-icon">:material-code-json:</span> `color`

:   The colour of the light. Defaults to white. See [Colour](#colour).

<span class="def-icon">:material-code-json:</span> `brightness`

:   How much light the light emits. Defaults to `1f` (a typical household light bulb). See [Brightness](#brightness).

<span class="def-icon">:material-code-json:</span> `maxIlluminationRadius`

:   How far the light reaches, in metres. Defaults to `15f`. See [Range](#range).

<span class="def-icon">:material-code-json:</span> `castsShadows`

:   Whether objects lit by the light cast shadows. Defaults to `false`. See [Shadows](#shadows).

<span class="def-icon">:material-code-json:</span> `name`

:   An optional name for the light.

Each of these can also be changed at any time after creation via the light's properties (e.g. `light.Position`, `light.Color`).

## Brightness

Every light's `Brightness` is a *relative* value, where `1f` is the default strength for that kind of light. This makes it easy to adjust lights of different kinds in the same terms ("half as bright", "twice as bright"). Brightness can't be negative (negative values are treated as `0f`).

```csharp
light.Brightness = 2f;
light.ScaleBrightnessBy(1.5f); // (1)!
light.AdjustBrightnessBy(-1f); // (2)!
```

1.	*Multiplies* the brightness by 1.5 (so `2f` becomes `3f`).

2.	*Adds* -1 to the brightness (so `3f` becomes `2f`). The result never goes below `0f`.

For a point light, a brightness of `1f` corresponds to an output of `PointLight.DefaultLumens` (800 lumens; that of a typical household light bulb). Note that the relationship is *quadratic*: the light emitted is proportional to the *square* of the brightness, so a brightness of `2f` emits four times as much light as `1f` (3,200 lumens), not twice as much.

### Brightness Presets

The simplest way to choose a brightness is to pick the real-world light source the light represents:

```csharp
light.SetBrightness(PointLightBrightnessPreset.Candle); // (1)!

using var floodlight = factory.LightBuilder.CreatePointLight( // (2)!
	brightness: PointLightBrightnessPreset.Floodlight.ToBrightnessValue()
);
```

1.	Makes the light as bright as a candle flame.

2.	`ToBrightnessValue()` returns the equivalent `Brightness` value of a preset, which is useful when creating a light. (`ToLumens()` returns its output in lumens.)

| Preset | Output | Equivalent To |
| :----- | :----- | :------------ |
| `PointLightBrightnessPreset.Candle` | 12 lumens | A candle flame |
| `PointLightBrightnessPreset.BulbDim` | 450 lumens | A 40W incandescent bulb |
| `PointLightBrightnessPreset.BulbTypical` (default) | 800 lumens | A 60W incandescent bulb |
| `PointLightBrightnessPreset.BulbBright` | 1,600 lumens | A 100W incandescent bulb |
| `PointLightBrightnessPreset.BulbVeryBright` | 3,000 lumens | A 200W incandescent bulb |
| `PointLightBrightnessPreset.Floodlight` | 10,000 lumens | An outdoor floodlight |

If you'd like to work with physical units directly, `PointLight.LumensToBrightness()` and `PointLight.BrightnessToLumens()` convert between brightness and lumens:

```csharp
light.Brightness = PointLight.LumensToBrightness(1_100f); // (1)!
```

1.	Sets the light's output to 1,100 lumens (roughly that of a 75W incandescent bulb).

??? info "Brightness Units"
	Each kind of light uses its own physical unit, and its own relationship between that unit and brightness:

	| Light | `1f` Brightness Equals | Relationship |
	| :---- | :--------------------- | :----------- |
	| [Point Light](point_lights.md) | 800 lumens (`PointLight.DefaultLumens`); a household light bulb | Quadratic |
	| [Spot Light](spot_lights.md) | Roughly 37,700 lumens (`SpotLight.DefaultLumens`); the equivalent of a handheld flashlight | Quadratic |
	| [Directional Light](directional_lights.md) | 600 lux (`DirectionalLight.DefaultLux`); a well-lit interior | Linear |
	| [Backdrop](exposure_and_brightness.md#backdrops) | The backdrop image as authored (`BackdropTexture.MeasuredLux` gives its illuminance) | Quadratic |

	Each light type has its own static conversion methods for its unit, and every type has its own presets.

## Brightness & Exposure

TinyFFR's lights use real-world brightnesses, and real-world lighting varies enormously: direct sunlight is roughly a thousand times brighter than the light of a single bulb a metre away. A real camera (or the human eye) copes with this by adjusting its *exposure*, and so must a TinyFFR `Camera`. A scene lit by a few light bulbs looks almost black through a camera set up for sunlight, and a sunlit scene looks completely washed out through a camera set up for a dim room.

New cameras use an exposure suited to a brightly-lit room (`CameraExposurePreset.InsideBrightLighting`). Set a camera's exposure to the preset that matches the lighting in your scene (see [Exposure & Brightness](exposure_and_brightness.md) for a full guide to calibrating exposure and lighting):

```csharp
camera.SetExposure(CameraExposurePreset.OutsideMidday); // (1)!
camera.Exposure *= 1.5f; // (2)!

using var nightCamera = factory.CameraBuilder.CreateCamera(exposure: CameraExposurePreset.OutsideFullMoon); // (3)!
```

1.	Sets the camera up for a scene lit by the sun on a clear day.

2.	Multiplying `Exposure` fine-tunes the result: this makes the image exactly one and a half times as bright as the preset alone. Dividing it makes the image dimmer.

3.	Cameras can also be created with an exposure preset.

| Exposure Preset | Suits Scenes Lit To Roughly | Pairs With |
| :-------------- | :-------------------------- | :--------- |
| `CameraExposurePreset.OutsideVeryBright` | Sunlight reflecting off snow or sand | |
| `CameraExposurePreset.OutsideMidday` | 100,000 lux | `DirectionalLightBrightnessPreset.Midday` or `SceneBackdropBrightnessPreset.Midday` |
| `CameraExposurePreset.OutsideOvercast` | 10,000 lux | `DirectionalLightBrightnessPreset.Overcast` or `SceneBackdropBrightnessPreset.Overcast` |
| `CameraExposurePreset.OutsideSunriseSunset` | 2,500 lux | `DirectionalLightBrightnessPreset.SunriseSunset` or `SceneBackdropBrightnessPreset.SunriseSunset` |
| `CameraExposurePreset.InsideBrightLighting` (default) | 600 lux | A `PointLightBrightnessPreset.BulbTypical` around 30cm away; several bulbs nearby |
| `CameraExposurePreset.InsideMoodLighting` | 80 lux | A `PointLightBrightnessPreset.BulbTypical` around 1m away |
| `CameraExposurePreset.OutsideTwilight` | 40 lux | `DirectionalLightBrightnessPreset.Twilight` or `SceneBackdropBrightnessPreset.Twilight` |
| `CameraExposurePreset.InsideCandleLighting` | 10 lux | A `PointLightBrightnessPreset.Candle` nearby |
| `CameraExposurePreset.OutsideNightStreet` | 2.5 lux | |
| `CameraExposurePreset.OutsideFullMoon` | 0.25 lux | `DirectionalLightBrightnessPreset.FullMoon` or `SceneBackdropBrightnessPreset.FullMoon` |
| `CameraExposurePreset.OutsideStarlight` | A few thousandths of a lux | `DirectionalLightBrightnessPreset.Starlight` or `SceneBackdropBrightnessPreset.Starlight` |

???+ tip "Choosing an Exposure"
	* A [backdrop](scenes.md#backdrops) at an intensity of `1f` typically suits the default exposure (`CameraExposurePreset.InsideBrightLighting`).
	* A scene lit by a [directional light](directional_lights.md) at `DirectionalLightBrightnessPreset.Midday` is lit by direct sunlight, so needs `CameraExposurePreset.OutsideMidday`.
	* Each `DirectionalLightBrightnessPreset` and `SceneBackdropBrightnessPreset` preset pairs with the `CameraExposurePreset` of the same name: either one on its own is correctly exposed. Using both together adds their light, so needs a lower exposure.
	* For scenes lit by your own point and spot lights, start from `CameraExposurePreset.InsideBrightLighting` or `CameraExposurePreset.InsideMoodLighting` and multiply or divide `Exposure` until the scene looks right.

A camera's `Exposure` is a `CameraExposureParams`: the combination of `Aperture`, `ShutterSpeed`, and `Sensitivity` (ISO) a photographer might use, plus the `Ev100` (exposure value) they add up to. `ToExposureParams()` returns the settings each preset applies (e.g. `CameraExposurePreset.OutsideTwilight.ToExposureParams()`), and you can set the settings directly instead (e.g. `camera.Exposure = new CameraExposureParams(8f, 1f / 125f, 100f)`). Multiplying or dividing `Exposure` only ever adjusts the sensitivity, so (unlike the aperture) it never changes the strength of a depth-of-field effect. Presets also convert implicitly to `CameraExposureParams`, and `CameraExposureParams.Interpolate()` blends smoothly between two exposures (e.g. to fade a camera from `CameraExposurePreset.OutsideMidday` to `CameraExposurePreset.OutsideTwilight` over time), changing the image's brightness evenly.

## Colour

A light's `Color` tints the light itself, not the objects it falls on. A red light leaves a white surface looking red; but a surface that reflects no red at all stays dark no matter how bright the light is.

`StandardColor` has a number of presets for common real-world light sources (`StandardColor.LightingCandle`, `LightingIncandescentBulb`, `LightingSunRiseSet`, `LightingSunMidday`, `LightingAmbientDaylight`, `LightingAmbientOvercast`, and `LightingAmbientShaded`).

A light's colour can also be adjusted in terms of hue, saturation, and lightness via its `ColorHue`, `ColorSaturation`, and `ColorLightness` properties (and the corresponding `AdjustColor[...]By()` methods).

## Range

A real light never quite stops illuminating things however far away they are; but calculating a contribution too faint to see is wasted work. A point light's `MaxIlluminationRadius` is therefore the distance (in metres) beyond which it contributes no light at all.

Setting this too small for the light's brightness produces a visible edge where the light abruptly cuts off. Setting it unnecessarily large wastes rendering time on objects the light barely affects.

## Shadows

Setting a light's `CastsShadows` to `true` makes the objects it lights cast shadows from it:

```csharp
light.CastsShadows = true;
```

Shadows are calculated separately for each light that casts them, and are one of the more expensive things a scene can ask for. It's therefore common to enable shadows only for the one or two lights that most define a scene's look, and leave the rest without shadows.

The quality of shadows (and whether they're rendered at all) is set per renderer, via the `ShadowQuality` and `ShadowsEnabled` properties of its `RenderQualityConfig`; see [Render Quality](render_quality.md).
