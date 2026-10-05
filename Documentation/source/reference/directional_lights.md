---
title: Directional Lights
description: Information on how to create and adjust directional lights, such as the sun.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * A directional light lights the whole scene from one direction, like the sun. :material-arrow-right: [Directional Lights](#directional-lights)
    * Its brightness is measured in lux, and (unlike other lights) scales linearly. Presets match times of day and weather. :material-arrow-right: [Brightness](#brightness)
    * It can also draw a visible sun disc in the sky. :material-arrow-right: [Sun Disc](#sun-disc)
    * A scene can only contain one directional light. :material-arrow-right: [One Per Scene](#one-per-scene)

</div>

## Directional Lights

```csharp
using var sun = factory.LightBuilder.CreateDirectionalLight( // (1)!
	direction: new Direction(0f, -1f, 0.5f),
	color: StandardColor.LightingSunMidday,
	brightnessPreset: DirectionalLightBrightnessPreset.Midday,
	castsShadows: true,
	showSunDisc: true
);
scene.Add(sun); // (2)!
camera.SetExposure(CameraExposurePreset.OutsideMidday); // (3)!
```

1.	Creates a directional light shining down and forward, with the colour and brightness of midday sunlight, casting shadows, and drawing a sun disc in the sky.

2.	Adds the light to a [scene](scenes.md). A light only illuminates the scenes it has been added to.

3.	Sunlight is far brighter than indoor lighting, so the camera viewing the scene needs an exposure to match (see [Exposure & Brightness](exposure_and_brightness.md)).

A `DirectionalLight` lights the entire scene from a single direction. It has no position and no falloff: it's treated as being infinitely far away, so every object in the scene is lit from the same angle with the same intensity. This makes it the right way to model the sun or the moon.

Directional lights share the [colour](point_lights.md#colour) and [shadow](point_lights.md#shadows) properties common to every kind of light, which are explained on the [Point Lights](point_lights.md) page.

### Creating Directional Lights

Directional lights are created with `factory.LightBuilder.CreateDirectionalLight()`, which accepts the following optional parameters (or alternatively a `DirectionalLightCreationConfig` with equivalent properties):

<span class="def-icon">:material-card-bulleted-outline:</span> `direction`

:   The direction the light travels in. Defaults to mostly downward and slightly forward, approximating afternoon sunlight. See [Direction](#direction).

<span class="def-icon">:material-card-bulleted-outline:</span> `color`, `castsShadows`

:   The light's colour (default white) and whether it casts shadows (default `false`). See [Point Lights](point_lights.md#colour).

<span class="def-icon">:material-card-bulleted-outline:</span> `brightness`

:   How much light the light casts. Defaults to `1f`. See [Brightness](#brightness).

<span class="def-icon">:material-card-bulleted-outline:</span> `showSunDisc`

:   Whether the light draws a visible disc in the sky. Defaults to `false`. Can only be set at creation. See [Sun Disc](#sun-disc).

<span class="def-icon">:material-card-bulleted-outline:</span> `name`

:   An optional name for the light.

## Direction

A directional light's `Direction` is the direction its light *travels*; i.e. the direction its rays move, not the direction pointing towards the light source. So for a sun directly overhead, the direction points downward.

Because a directional light has no position, its direction is the only thing that determines how objects are lit by it, and which way their shadows fall. A directional light's `RotateBy()` methods rotate its direction.

## Brightness

A directional light's `Brightness` is a relative value like that of [every other light](point_lights.md#brightness), where `1f` corresponds to `DirectionalLight.DefaultLux` (600 lux; that of a well-lit interior). Daylight is far brighter: direct midday sunlight is roughly 125,000 lux.

Unlike point and spot lights, the relationship between a directional light's brightness and its physical unit is *linear*: a brightness of `2f` casts twice as much light as `1f` (1,200 lux). `DirectionalLight.LuxToBrightness()` and `DirectionalLight.BrightnessToLux()` convert between the two:

```csharp
sun.Brightness = DirectionalLight.LuxToBrightness(20_000f); // (1)!
```

1.	Sets the light to 20,000 lux (roughly that of full daylight in the shade).

The simplest way to choose a brightness, though, is to pick the time of day or weather the light represents with a `DirectionalLightBrightnessPreset` preset:

```csharp
sun.SetBrightness(DirectionalLightBrightnessPreset.SunriseSunset); // (1)!
camera.SetExposure(CameraExposurePreset.OutsideSunriseSunset); // (2)!

using var moon = factory.LightBuilder.CreateDirectionalLight( // (3)!
	brightness: DirectionalLightBrightnessPreset.FullMoon.ToBrightnessValue()
);
```

1.	Makes the light as bright as the light shortly after sunrise or before sunset.

2.	Sets the camera to the matching [exposure](exposure_and_brightness.md#matching-presets).

3.	`ToBrightnessValue()` returns the equivalent `Brightness` value of a preset, which is useful when creating a light. (`ToLux()` returns its illuminance.)

| Preset | Illuminance | Pairs With |
| :----- | :---------- | :--------- |
| `DirectionalLightBrightnessPreset.InsideBrightLighting` (default) | 600 lux | `CameraExposurePreset.InsideBrightLighting` (the default exposure) |
| `DirectionalLightBrightnessPreset.Midday` | 125,000 lux | `CameraExposurePreset.OutsideMidday` |
| `DirectionalLightBrightnessPreset.Overcast` | 10,000 lux | `CameraExposurePreset.OutsideOvercast` |
| `DirectionalLightBrightnessPreset.SunriseSunset` | 2,500 lux | `CameraExposurePreset.OutsideSunriseSunset` |
| `DirectionalLightBrightnessPreset.Twilight` | 40 lux | `CameraExposurePreset.OutsideTwilight` |
| `DirectionalLightBrightnessPreset.FullMoon` | 0.25 lux | `CameraExposurePreset.OutsideFullMoon` |
| `DirectionalLightBrightnessPreset.Starlight` | 0.005 lux | `CameraExposurePreset.OutsideStarlight` |

Each preset lights a scene completely on its own: a directional light at a preset, viewed through a camera with the `CameraExposurePreset` of the same name, is correctly exposed without any other light. See [Exposure & Brightness](exposure_and_brightness.md) for how to calibrate exposure and lighting in general.

??? tip "Directional Lights & Backdrops"
	A scene's [backdrop](scenes.md#backdrops) also lights the scene (as ambient light from every direction), and the `SceneBackdropBrightnessPreset` of the same name lights a scene correctly for the same camera exposure on its own too. Using both together adds their light, so the scene will be brighter than either alone: lower the camera's exposure (e.g. `camera.Exposure /= 2f`) or the brightness of one of them to compensate. This is especially worth knowing for backdrop images that already contain the sun, whose light then counts twice.

## Sun Disc

A directional light created with `showSunDisc: true` draws a visible disc in the sky where its light comes from, in the way the sun appears as a bright disc rather than merely lighting the scene. The disc is drawn against the scene's [backdrop](scenes.md#backdrops), so it's only visible where the backdrop is.

The disc's appearance can be adjusted with `SetSunDiscParameters()`:

```csharp
sun.SetSunDiscParameters(new SunDiscConfig {
	Scaling = 2f, // (1)!
	FringingScaling = 0.5f, // (2)!
	FringingOuterRadiusScaling = 1.5f // (3)!
});
```

1.	Doubles the size of the disc itself.

2.	Halves the brightness of the halo of light around the disc (the bloom that surrounds a bright light seen through an atmosphere). `0f` leaves a hard-edged disc with no glow.

3.	Widens how far the halo spreads outward.

Each property defaults to `1f` (the disc's natural appearance).

## One Per Scene

???+ warning "One Directional Light Per Scene"
	A scene can contain at most one directional light. If a scene already contains a directional light, adding another has no effect (the second light is not added). Remove the first directional light before adding a different one.

	A single directional light can still be added to several different scenes.
