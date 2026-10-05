---
title: Exposure & Brightness
description: How to calibrate a camera's exposure against the lights and backdrops in a scene, so that what you render is neither too dark nor washed out.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Lights and backdrops are measured in real-world units, and a camera's exposure decides how bright the result looks. :material-arrow-right: [Exposure & Brightness](#exposure-brightness)
    * A camera's exposure is an aperture, a shutter speed, and a sensitivity, usually chosen with a preset. :material-arrow-right: [Camera Exposure](#camera-exposure)
    * Pick the camera preset with the same name as your light or backdrop preset and the scene is correctly exposed. :material-arrow-right: [Matching Presets](#matching-presets)
    * Several light sources add their light together, so need a less sensitive exposure. :material-arrow-right: [Combining Light Sources](#combining-light-sources)
    * Backdrop images are measured when they're loaded, so presets light any backdrop to the same real-world brightness. :material-arrow-right: [Backdrops](#backdrops)

</div>

## Exposure & Brightness

```csharp
using var scene = factory.SceneBuilder.CreateScene();
scene.SetBackdrop(BuiltInSceneBackdrop.Clouds, SceneBackdropBrightnessPreset.Midday); // (1)!

using var camera = factory.CameraBuilder.CreateCamera(exposure: CameraExposurePreset.OutsideMidday); // (2)!

using var lamp = factory.LightBuilder.CreatePointLight(brightnessPreset: PointLightBrightnessPreset.BulbTypical); // (3)!
using var indoorCamera = factory.CameraBuilder.CreateCamera(); // (4)!
```

1.	Lights the scene with the built-in clouds backdrop, at the brightness of a clear day around midday (100,000 lux).

2.	Creates a camera exposed for a sunny day. Because its preset has the same name as the backdrop's, the scene is correctly exposed.

3.	Creates a point light as bright as a typical household light bulb (800 lumens).

4.	Creates a camera with the default exposure, which suits a brightly-lit interior such as a room lit by a nearby bulb.

TinyFFR's lights and backdrops use real-world brightnesses, and real-world brightnesses vary enormously: the sun on a clear day lights a scene roughly a thousand times more brightly than a household bulb a metre away, and a full moon roughly a million times less brightly than the sun. A real camera (or your eye) copes with this by adjusting its *exposure*: how much of the light arriving at it ends up in the image. A TinyFFR `Camera` does the same.

So the brightness of the final image depends on two things:

* How much light is in the scene: the [lights](point_lights.md) and the scene's [backdrop](scenes.md#backdrops).
* How the camera is exposed to that light: its `Exposure`.

Neither means anything on its own. A scene lit by a single bulb looks almost black through a camera exposed for sunlight; a sunlit scene looks completely white through a camera exposed for a dim room. Getting a good image means choosing an exposure that suits the lighting, which is what the rest of this page explains.

??? info "Why Not Just Use Arbitrary Brightness Values?"
	Real-world units make lighting predictable. A bulb and the sun can share a scene with their true relative brightnesses, materials respond the same way under any lighting, and a scene that looks right in daylight still looks right when you swap the sun for a candle and the camera for a night-time exposure. The cost is that every scene needs an exposure that suits it; the presets described below make that a one-line choice in almost every case.

## Camera Exposure

A camera's exposure is the same three settings a photographer balances against each other:

<span class="def-icon">:material-card-bulleted-outline:</span> `Aperture`

:   How wide the lens opening is, as an f-number. *Smaller* numbers mean a wider opening and a brighter image. The aperture also controls the strength of the camera's depth-of-field effect (see `RenderQualityConfig.DepthOfFieldStrength`).

<span class="def-icon">:material-card-bulleted-outline:</span> `ShutterSpeed`

:   How long the shutter stays open, in seconds. Longer means brighter.

<span class="def-icon">:material-card-bulleted-outline:</span> `Sensitivity`

:   How sensitive the sensor is to light, as an ISO value. Higher means brighter.

Together these form a `CameraExposureParams`, which is the type of `camera.Exposure`. Its `Ev100` property gives the *exposure value* the three settings add up to: the photographer's single-number measure of how much light the camera needs, in *stops*. Each increase of `1` in `Ev100` halves the brightness of the image (it needs twice as much light for the same result).

### Exposure Presets

The simplest way to choose an exposure is with a `CameraExposurePreset`, named after the lighting it suits:

```csharp
camera.SetExposure(CameraExposurePreset.OutsideOvercast); // (1)!

using var nightCamera = factory.CameraBuilder.CreateCamera(exposure: CameraExposurePreset.OutsideFullMoon); // (2)!

CameraExposureParams settings = CameraExposurePreset.InsideMoodLighting; // (3)!
```

1.	Exposes the camera for an overcast day.

2.	Creates a camera exposed for a landscape lit by a full moon. (`CameraCreationConfig.InitialExposure` does the same when creating a camera from a config.)

3.	Presets convert implicitly to `CameraExposureParams`; `ToExposureParams()` does the same explicitly.

| Preset | Settings | `Ev100` | Suits Scenes Lit To Roughly |
| :----- | :------- | ------: | --------------------------: |
| `CameraExposurePreset.OutsideVeryBright` | f/16, 1/250s, ISO 100 | 16 | 160,000 lux (sunlight reflecting off snow or sand) |
| `CameraExposurePreset.OutsideMidday` | f/16, 1/125s, ISO 100 | 15 | 100,000 lux |
| `CameraExposurePreset.OutsideOvercast` | f/5.6, 1/125s, ISO 100 | 12 | 10,000 lux |
| `CameraExposurePreset.OutsideSunriseSunset` | f/4, 1/60s, ISO 100 | 10 | 2,500 lux |
| `CameraExposurePreset.InsideBrightLighting` (default) | f/2.8, 1/60s, ISO 200 | 7.9 | 600 lux |
| `CameraExposurePreset.InsideMoodLighting` | f/2, 1/30s, ISO 400 | 4.9 | 80 lux |
| `CameraExposurePreset.OutsideTwilight` | f/2, 1/30s, ISO 800 | 3.9 | 40 lux |
| `CameraExposurePreset.InsideCandleLighting` | f/2, 1/15s, ISO 1,600 | 1.9 | 10 lux |
| `CameraExposurePreset.OutsideNightStreet` | f/2, 1/8s, ISO 3,200 | 0 | 2.5 lux |
| `CameraExposurePreset.OutsideFullMoon` | f/2, 1/2s, ISO 6,400 | -3 | 0.25 lux |
| `CameraExposurePreset.OutsideStarlight` | f/2, 4s, ISO 51,200 | -9 | 0.005 lux |

New cameras use `CameraExposurePreset.InsideBrightLighting`, which suits a well-lit interior.

??? info "How Exposure Relates to Illuminance"
	Every preset follows the same photographic rule of thumb: a scene lit to an illuminance of `E` lux is well exposed at an exposure value of roughly `log2(E / 2.5)`, or equivalently a camera at a given `Ev100` suits a scene lit to roughly `2.5 × 2^Ev100` lux. You can use the rule to choose an exposure for any lighting, not just the presets' (see [Point & Spot Lights](#point-spot-lights)).

### Adjusting Exposure

Multiplying or dividing a camera's `Exposure` by a number scales its sensitivity, which makes the image exactly that many times brighter or dimmer:

```csharp
camera.SetExposure(CameraExposurePreset.InsideBrightLighting);
camera.Exposure *= 2f; // (1)!
camera.Exposure /= 4f; // (2)!
camera.Exposure = new CameraExposureParams(8f, 1f / 250f, 400f); // (3)!
```

1.	Doubles the sensitivity, making the image twice as bright (one stop brighter).

2.	Divides the sensitivity by four, making the image a quarter as bright (two stops dimmer).

3.	Sets the three settings directly.

Because multiplying and dividing only ever change the sensitivity, they never alter the strength of a depth-of-field effect; changing the aperture does. Each setting is clamped to a permitted range when assigned to a camera (`CameraExposureParams.ApertureMin`/`ApertureMax`, `ShutterSpeedMin`/`ShutterSpeedMax`, and `SensitivityMin`/`SensitivityMax`), and a non-finite setting is replaced with that of the default exposure.

`CameraExposureParams.Interpolate()` blends between two exposures, which is useful for transitions such as walking from a dark room in to daylight, or a sunset over time:

```csharp
var progress = 0.25f;
camera.Exposure = CameraExposureParams.Interpolate(CameraExposurePreset.OutsideMidday, CameraExposurePreset.OutsideTwilight, progress); // (1)!
```

1.	A quarter of the way from a midday exposure to a twilight one.

`Interpolate()` moves each setting evenly *in stops*, so the brightness of the image changes evenly as `progress` does. `InterpolateArithmetically()` interpolates each setting linearly instead, which changes the brightness unevenly; it is rarely what you want.

## Light Sources & Their Units

Every light has a `Brightness`, a relative value where `1f` is the default strength for its kind, and each kind of light also has a physical unit. The relationship between the two differs:

| Source | Physical Unit | `1f` Equals | Relationship |
| :----- | :------------ | :---------- | :----------- |
| [Point Light](point_lights.md#brightness) | Lumens (output) | 800 lumens (`PointLight.DefaultLumens`); a household bulb | Quadratic |
| [Spot Light](spot_lights.md#brightness) | Lumens (equivalent output) | Roughly 37,700 lumens (`SpotLight.DefaultLumens`); a handheld flashlight | Quadratic |
| [Directional Light](directional_lights.md#brightness) | Lux (illuminance) | 600 lux (`DirectionalLight.DefaultLux`); a well-lit interior | Linear |
| [Backdrop](#backdrops) | Lux (illuminance) | The backdrop image as authored (`BackdropTexture.MeasuredLux`) | Quadratic |

*Quadratic* means the light emitted is proportional to the *square* of the value: a point light at a brightness of `2f` emits four times the light of `1f` (3,200 lumens), which looks roughly twice as bright. *Linear* means the light is proportional to the value: a directional light at `2f` casts exactly twice the light of `1f` (1,200 lux).

Each light also has a property in its physical unit (`PointLight.BrightnessLumens`, `SpotLight.BrightnessLumens`, and `DirectionalLight.BrightnessLux`), and static methods that convert between the two (e.g. `PointLight.LumensToBrightness()` and `PointLight.BrightnessToLumens()`). A backdrop's conversions belong to the backdrop texture itself, because each image lights a scene differently (see [Measured Brightness](#measured-brightness)).

```csharp
lamp.BrightnessLumens = 1_100f; // (1)!
sun.BrightnessLux = 20_000f; // (2)!
```

1.	Sets a point light's output to 1,100 lumens, roughly that of a 75W incandescent bulb.

2.	Sets a directional light to 20,000 lux, roughly that of full daylight in the shade.

Every kind of light (and backdrop) also has its own brightness presets, which are the simplest way to choose a brightness. Pass one when creating a light (`brightnessPreset:`), or to `SetBrightness()` / `SetBackdrop()` afterwards.

## Matching Presets

The directional light and backdrop presets are named after the same lighting conditions as the camera presets, and each is calibrated to work with the camera preset of the same name:

!!! tip "The Matching Rule"
	A scene lit by **either** a directional light **or** a backdrop at a given preset, viewed through a camera at the `CameraExposurePreset` of the same name, is correctly exposed with no other lights needed.

| Condition | Camera Preset | Directional Light Preset | Backdrop Preset |
| :-------- | :------------ | :----------------------- | :-------------- |
| A well-lit interior | `InsideBrightLighting` (default) | `InsideBrightLighting` (default): 600 lux | `InsideBrightLighting`: 600 lux |
| A clear day around midday | `OutsideMidday` | `Midday`: 125,000 lux | `Midday`: 100,000 lux |
| An overcast day | `OutsideOvercast` | `Overcast`: 10,000 lux | `Overcast`: 10,000 lux |
| Shortly after sunrise or before sunset | `OutsideSunriseSunset` | `SunriseSunset`: 2,500 lux | `SunriseSunset`: 2,500 lux |
| Just after sunset | `OutsideTwilight` | `Twilight`: 40 lux | `Twilight`: 40 lux |
| A full moon | `OutsideFullMoon` | `FullMoon`: 0.25 lux | `FullMoon`: 0.25 lux |
| A moonless, starlit night | `OutsideStarlight` | `Starlight`: 0.005 lux | `Starlight`: 0.005 lux |

Each preset's illuminance is the *total* light of its condition, so a backdrop preset includes the light of the sun (or moon) where there is one. Every pairing is within a fraction of a stop of the [exposure rule of thumb](#exposure-presets); the directional `Midday` preset is deliberately a little brighter, because the sun only fully lights the surfaces facing it.

```csharp
using var sun = factory.LightBuilder.CreateDirectionalLight(brightnessPreset: DirectionalLightBrightnessPreset.SunriseSunset); // (1)!
camera.SetExposure(CameraExposurePreset.OutsideSunriseSunset);

scene.SetBackdrop(skyTexture, SceneBackdropBrightnessPreset.Twilight); // (2)!
camera.SetExposure(CameraExposurePreset.OutsideTwilight);
```

1.	A low sun, correctly exposed by the matching camera preset.

2.	A backdrop lit to the brightness of twilight, correctly exposed by the matching camera preset.

??? question "Why Do Night Scenes Look As Bright As Day?"
	Because the matching camera preset is exposed *for* night-time light, in the same way a long-exposure photograph of a moonlit landscape can look like daytime. If you want a night scene to look dark, use a less sensitive exposure than its matching preset: for example, a moonlit scene viewed with `CameraExposurePreset.OutsideTwilight` (or with the matching exposure divided by 8 or 16) reads as night-time.

## Combining Light Sources

Light adds up. Each matched pairing is correctly exposed *on its own*, so a scene with both a directional light and a backdrop at matching presets receives roughly twice the light either would alone, and looks about one stop too bright:

```csharp
scene.SetBackdrop(BuiltInSceneBackdrop.Clouds, SceneBackdropBrightnessPreset.Midday);
using var sun = factory.LightBuilder.CreateDirectionalLight(brightnessPreset: DirectionalLightBrightnessPreset.Midday);
scene.Add(sun);

camera.SetExposure(CameraExposurePreset.OutsideMidday);
camera.Exposure /= 2f; // (1)!
```

1.	Compensates for the second source by making the camera half as sensitive (one stop).

In general, when sources are combined, add up their illuminances and divide the exposure by how many times brighter the total is than the condition the camera preset suits. Alternatively, keep the exposure and dim one of the sources (e.g. a directional light acting only as a subtle key light at a fraction of its preset's brightness).

!!! warning "Backdrops That Contain the Sun"
	Many sky images (including the built-in `BuiltInSceneBackdrop.Clouds`) already contain the sun, and the sun is usually most of their light. Adding a `DirectionalLight` for the sun on top of such a backdrop counts the sun twice. That's fine if you want the directional light's crisp shadows, but expect to lower the exposure (or the directional light's brightness) to compensate.

## Point & Spot Lights

Point and spot lights don't have matching camera presets, because how brightly they light a scene depends on how far away they are. What matters for exposure is the illuminance arriving at the objects you're looking at:

* A point light's illuminance falls with the square of distance: a light with an output of `L` lumens lights a surface `d` metres away to roughly `L / (4π d²)` lux.
* A spot light's illuminance at the centre of its beam is roughly its beam intensity in candela (the value its presets are defined by) divided by `d²`.

| Light | Distance | Illuminance | Suitable Exposure |
| :---- | -------: | ----------: | :---------------- |
| `PointLightBrightnessPreset.BulbTypical` | 0.3m | 710 lux | `CameraExposurePreset.InsideBrightLighting` |
| `PointLightBrightnessPreset.BulbTypical` | 1m | 64 lux | `CameraExposurePreset.InsideMoodLighting` |
| `PointLightBrightnessPreset.Candle` | 0.3m | 11 lux | `CameraExposurePreset.InsideCandleLighting` |
| `SpotLightBrightnessPreset.DeskLamp` | 0.5m | 600 lux | `CameraExposurePreset.InsideBrightLighting` |
| `SpotLightBrightnessPreset.FlashlightTypical` | 2m | 750 lux | `CameraExposurePreset.InsideBrightLighting` |
| `SpotLightBrightnessPreset.StageSpotlight` | 5m | 4,000 lux | Between `OutsideSunriseSunset` and `OutsideOvercast` |

For any other combination, work out the illuminance and pick the closest exposure preset, then scale it by the ratio:

```csharp
var illuminance = PointLightBrightnessPreset.BulbBright.ToLumens() / (4f * MathF.PI * 1.5f * 1.5f); // (1)!
camera.SetExposure(CameraExposurePreset.InsideMoodLighting);
camera.Exposure *= 80f / illuminance; // (2)!
```

1.	A 1,600 lumen bulb 1.5m away lights a surface to roughly 57 lux.

2.	`InsideMoodLighting` suits 80 lux, so the camera needs to be 80 / 57 (roughly 1.4) times more sensitive.

??? info "High-Quality Spot Lights"
	The spot light presets assume a standard spot light, which spreads its output as though it were a point light masked to a cone. A spot light created with `highQuality: true` concentrates its whole output in to its cone, so appears brighter the narrower the cone is. See [Spot Lights](spot_lights.md#cone-brightness).

## Backdrops

A scene's [backdrop](scenes.md#backdrops) both fills the background and lights the scene from every direction. Its brightness is set by an *intensity* (the `backdropIntensity` or `indirectLightingIntensity` parameter), which works like the brightness of a point light:

* An intensity of `1f` shows the backdrop exactly as its image was authored.
* The relationship is quadratic: an intensity of `2f` looks twice as bright as `1f`, and takes four times the light.
* Intensities are capped at `Scene.MaxBrightness`.

```csharp
scene.SetBackdrop(sunset); // (1)!
scene.SetBackdrop(sunset, backdropIntensity: 0.5f); // (2)!
scene.SetBackdrop(sunset, SceneBackdropBrightnessPreset.SunriseSunset); // (3)!
using var newScene = factory.SceneBuilder.CreateScene(sunset, initialBackdropIntensity: 0.5f); // (4)!
```

1.	Shows the backdrop as authored (an intensity of `1f`).

2.	Shows the backdrop half as bright (lighting the scene with a quarter of the light).

3.	Lights the scene to the brightness of sunrise or sunset (2,500 lux), whatever the brightness of the image (see below).

4.	Scenes can also be created with an initial backdrop intensity (or with `SceneCreationConfig.InitialBackdropIntensity`).

An intensity of `1f` follows the usual convention for image-based lighting (the one used by Filament, the rendering engine underneath TinyFFR), adjusted to TinyFFR's default exposure. A typical backdrop image therefore looks correctly exposed at `1f` through a camera with the default exposure. That makes `1f` a good starting point when you just want a backdrop to look as its author intended, but image files rarely record how bright the scene they depict really was, so different images can still light a scene very differently.

### Measured Brightness

To make backdrop brightness predictable, TinyFFR measures how brightly each backdrop's image lights a scene when the backdrop is loaded:

```csharp
var lux = sunset.MeasuredLux; // (1)!
scene.SetBackdrop(sunset, sunset.LuxToIntensity(5_000f)); // (2)!
var currentLux = sunset.IntensityToLux(0.5f); // (3)!
```

1.	The illuminance, in lux, that this backdrop delivers to an upward-facing surface at an intensity of `1f`.

2.	Sets the backdrop to whatever intensity lights the scene to 5,000 lux.

3.	The illuminance this backdrop delivers at an intensity of `0.5f` (a quarter of `MeasuredLux`).

`MeasuredLux` includes all the light in the image, including that of the sun if the image shows one, and is measured with the backdrop unrotated. For reference, the built-in clouds backdrop measures roughly 716 lux, and the built-in starfield roughly 14 lux.

??? info "How Backdrops Are Measured"
	Every backdrop is ultimately stored as two images: the sky that's drawn, and a *lighting* image from which its ambient light is derived (see [Backdrop Textures](backdrop_textures.md)). TinyFFR measures the lighting image, in order of preference:

	1. From the compact lighting summary (*spherical harmonics*) that TinyFFR's backdrop preprocessing tool embeds in every lighting image it produces.
	2. By sampling the lighting image's pixels directly, if it has no such summary (for example, a lighting image produced by other tools).
	3. If neither is possible, by assuming the brightness of a typical backdrop.

	Measurement never fails, and takes a negligible amount of time. Baked backdrops are measured when they're loaded, in the same way, so they always agree with their original images.

The `SceneBackdropBrightnessPreset` overloads of `SetBackdrop()` (and of `SetBackdropWithoutIndirectLighting()`) use this measurement to convert the preset's illuminance to an intensity for that particular backdrop. A backdrop preset therefore lights the scene to the same illuminance whichever image it's used with, which is what makes the [matching presets](#matching-presets) work for any backdrop.

??? info "Colour Backdrops"
	A flat colour backdrop (`SetBackdrop(ColorVect)`) lights the scene as though it were an image of that colour in every direction, so its measured brightness depends on how bright the colour is: a mid-grey lights a scene to roughly 245 lux at `1f`. Presets work with colour backdrops in the same way as with images. A black colour can't light a scene at any intensity, so a preset leaves it at `1f`.

	Unlike an image, the flat colour drawn behind the scene is always shown exactly as specified, whatever the backdrop's intensity or the camera's exposure; only the light it contributes to the scene changes. The default backdrop of a new scene is a very dark grey (`0x080808`), which supplies only a faint trace of light (roughly 15 lux), so a new scene needs lights or a brighter backdrop.

??? tip "Light Direction Matters"
	`MeasuredLux` describes the light falling on an *upward-facing* surface. A backdrop whose light comes mostly from one direction (such as an image with the sun in it) lights surfaces facing that direction much more brightly than surfaces facing away. An object viewed with the sun behind it can therefore look darker than the rest of a correctly-exposed scene; rotating the backdrop (`rotation:`) to move its brightest part behind the camera lights the visible side of your objects.

## Emissive Materials

Emissive (glowing) parts of materials (see [Emissive Maps](texture_map_types.md#emissive-maps)) are calibrated to look as authored through a camera with the default exposure. Like real glowing objects (screens, signs, lamps), they look dimmer through a less sensitive exposure: an emissive surface that glows strongly indoors may barely register in a sunlit scene exposed with `CameraExposurePreset.OutsideMidday`.

## Troubleshooting

??? question "Everything is black (or almost black)"
	The camera's exposure is far too insensitive for the light in the scene. Check that there's a light or backdrop at all (the default backdrop provides only a trace of light), then pick the camera preset that matches your lighting, or multiply `camera.Exposure` until the scene looks right.

??? question "Everything is white or washed out"
	The camera's exposure is far too sensitive for the light in the scene: a common cause is a sunlit scene (a directional light or backdrop at `Midday`) viewed with the default exposure, which suits indoor lighting. Use `CameraExposurePreset.OutsideMidday`, or divide `camera.Exposure`.

??? question "The objects look right, but the background looks dark (or vice versa)"
	The background is the backdrop's own brightness, whereas the objects are lit by every light in the scene. If the objects are also lit by a bright directional light, the camera may be correctly exposed for the objects but not the backdrop. Light the scene with the backdrop alone at a matching preset (see [Matching Presets](#matching-presets)), or raise the backdrop's intensity to suit the exposure.

??? question "The scene is too bright with both a sun and a sky backdrop"
	Each matching preset is correct on its own, so together they're roughly twice as bright. Divide the exposure by two, or dim one of them (see [Combining Light Sources](#combining-light-sources)).

??? question "My objects look dark against a well-exposed backdrop"
	The backdrop's brightest light (often the sun in the image) is probably behind the objects. Rotate the backdrop, add a light in front of the objects, or raise the exposure slightly.

??? question "A new backdrop image is much brighter or darker than the built-in ones"
	Images aren't authored to a common brightness. Use a `SceneBackdropBrightnessPreset` (or `LuxToIntensity()`) rather than a plain intensity to light the scene to a known brightness, whatever the image.
