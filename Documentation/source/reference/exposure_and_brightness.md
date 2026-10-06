---
title: Exposure & Brightness
description: How to calibrate a camera's exposure against the lights and backdrops in a scene, so that what you render is neither too dark nor washed out.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * You can control how much light a `Camera` lets in (i.e. its exposure) using real-world values. :material-arrow-right: [Camera Exposure](camera-exposure)
    * It's also possible to set up light sources using real-world units. :material-arrow-right: [Light Sources & Their Units](#light-sources-their-units)
    * The default brightness levels of lights and scene backdrops in TinyFFR are calibrated for the default camera exposure. :material-arrow-right: [Exposure & Brightness](#exposure-brightness)

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

TinyFFR's lights and backdrops use real-world brightnesses, and real-world brightnesses vary enormously. The sun on a clear day lights a scene roughly a thousand times more brightly than a household bulb a metre away, and a full moon roughly a million times less brightly than the sun. A camera (or your eye) copes with this by adjusting its *exposure* which is a measure how much of the light arriving at it ends up in the image.

In short, the final brightness of a rendered scene depends on the brightness of all lights + the backdrop and how the camera is exposed to that light; i.e. its `Exposure`. Getting a good image means choosing an exposure that suits the lighting, which is what the rest of this page explains.

??? question "When should I use this?"
	You don't need to use these controls if you're happy with the defaults and have no need or desire to model real-world lighting or multiple dynamic ranges. 
	
	The default brightness values of every source of illumination in TinyFFR are calibrated for the camera's default exposure of `InsideBrightLighting`. For example, a `PointLight` `Brightness` of `1f` provides a typical lightbulb brightness; a `SpotLight` `Brightness` of `1f` provides a typical flashlight brightness; and a `DirectionalLight` `Brightness` of `1f` provides the typical blanket brightness of a well-let interior.
	
	On the other hand:
	
	* Controlling exposure + brightness values allows you to model larger brightness ranges in a scene than can typically be represented by most consumer displays.
	* Furthermore, real-world units make lighting predictable. A bulb and the sun can share a scene with their true relative brightnesses, materials respond the same way under any lighting, and a scene that looks right in daylight still looks right when you swap the sun for a candle and the camera for a night-time exposure. 
	
	The cost is that every scene needs an exposure that suits it.

## Camera Exposure

A camera's exposure is set via its `camera.Exposure` property. This property takes a `CameraExposureParams` instance which has the following three properties:

<span class="def-icon">:material-card-bulleted-outline:</span> `Aperture`

:   How wide the lens opening is, as an f-number. *Smaller* numbers mean a wider opening and a brighter image. The aperture also controls the strength of the camera's depth-of-field effect (alongside the linear quality control `RenderQualityConfig.DepthOfFieldStrength`).

<span class="def-icon">:material-card-bulleted-outline:</span> `ShutterSpeed`

:   How long the shutter stays open, in seconds. Longer means brighter.

<span class="def-icon">:material-card-bulleted-outline:</span> `Sensitivity`

:   How sensitive the sensor is to light, as an ISO value. Higher means brighter.

### Exposure Presets

The simplest way to choose an exposure is with a `CameraExposurePreset`, named after the lighting it suits:

```csharp
camera.SetExposure(CameraExposurePreset.OutsideOvercast); // (1)!

using var nightCamera = factory.CameraBuilder.CreateCamera( // (2)!
	exposure: CameraExposurePreset.OutsideFullMoon 
); 

CameraExposureParams settings = CameraExposurePreset.InsideMoodLighting; // (3)!
```

1.	Exposes the camera for an overcast day.

2.	Creates a camera exposed for a landscape lit by a full moon.

3.	Presets convert implicitly to `CameraExposureParams` (or you can use `ToExposureParams()`).

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

By default, cameras are set to `CameraExposurePreset.InsideBrightLighting`, which suits a well-lit interior.

??? abstract "Exposure Relationship to Illuminance"
	A scene lit to an illuminance of `E` lux is well exposed at an exposure value of roughly `log2(E / 2.5)`, or equivalently a camera at a given `Ev100` suits a scene lit to roughly `2.5 × 2^Ev100` lux. 
	
	You can use the rule to choose an exposure for any lighting, not just the presets.

### Tweaking Exposure

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

Because multiplying and dividing only ever change the sensitivity, they never alter the strength of a depth-of-field effect; changing the aperture does.

### Interpolating Exposure

`CameraExposureParams.Interpolate()` blends between two exposures, which is useful for transitions such as walking from a dark room in to daylight, or a sunset over time:

```csharp
var progress = 0.25f;
camera.Exposure = CameraExposureParams.Interpolate( // (1)!
	CameraExposurePreset.OutsideMidday, 
	CameraExposurePreset.OutsideTwilight, 
	progress
); 
```

1.	A quarter of the way from a midday exposure to a twilight one.

`Interpolate()` moves each setting evenly *in stops*, so the brightness of the image changes evenly as `progress` does. `InterpolateArithmetically()` interpolates each setting linearly instead, which changes the brightness unevenly; it is rarely what you want.

## Light Sources & Their Units

Every light has a `Brightness`, a relative value where `1f` is the default strength for its kind, and each kind of light also has a physical unit. The relationship between the two differs:

| Source | Physical Unit | `1f` Equals | Preset Name |
| :----- | :------------ | :---------- | :----------- |
| [Point Light](point_lights.md#brightness) | Lumens (output) | 800 lumens (`PointLight.DefaultLumens`); a household bulb | `PointLightBrightnessPreset.BulbTypical` |
| [Spot Light](spot_lights.md#brightness) | Candela (beam intensity) | 3,000 candela (`SpotLight.DefaultCandela`); a handheld flashlight | `SpotLightBrightnessPreset.FlashlightTypical` |
| [Directional Light](directional_lights.md#brightness) | Lux (illuminance) | 600 lux (`DirectionalLight.DefaultLux`); a well-lit interior | `DirectionalLightBrightnessPreset.InsideBrightLighting` |
| [Backdrop](#backdrops) | Lux (illuminance) | The backdrop image as authored (`BackdropTexture.MeasuredLux`) | `SceneBackdropBrightnessPreset.InsideBrightLighting` |


Each light also has a property in its physical unit (`PointLight.BrightnessLumens`, `SpotLight.BrightnessCandela`, and `DirectionalLight.BrightnessLux`), and static methods that convert between the two (e.g. `PointLight.LumensToBrightness()` and `PointLight.BrightnessToLumens()`, or `SpotLight.CandelaToBrightness()` and `SpotLight.BrightnessToCandela()`). A backdrop's conversions belong to the backdrop texture itself, because each image lights a scene differently (see [Measured Brightness](#measured-brightness)).

Every kind of light (and backdrop) also has its own brightness presets, which are the simplest way to choose a brightness. Pass one when creating a light (`brightnessPreset:`), or to `SetBrightness()` / `SetBackdrop()` afterwards.

```csharp
lamp.BrightnessLumens = 1_100f; // (1)!
torch.BrightnessCandela = 8_000f; // (2)!
sun.BrightnessLux = 20_000f; // (3)!
```

1.	Sets a point light's output to 1,100 lumens, roughly that of a 75W incandescent bulb.

2.	Sets a spot light's beam intensity to 8,000 candela, roughly that of a bright handheld flashlight.

3.	Sets a directional light to 20,000 lux, roughly that of full daylight in the shade.

```csharp
light.SetBrightness(PointLightBrightnessPreset.Candle); // (1)!

using var floodlight = factory.LightBuilder.CreatePointLight(brightnessPreset: PointLightBrightnessPreset.Floodlight); // (2)!
var floodlightBrightness = PointLightBrightnessPreset.Floodlight.ToBrightnessValue(); // (3)!
var floodlightLumens = PointLightBrightnessPreset.Floodlight.ToLumens(); // (4)!
```

1.	Makes an existing light as bright as a candle flame.

2.	Creates a light as bright as an outdoor floodlight.

3.	`ToBrightnessValue()` returns the equivalent `Brightness` value of a preset.

4.	`ToLumens()` returns a preset's output in its physical unit (`ToCandela()` for spot light presets, and `ToLux()` for directional light and backdrop presets).

### Point Light Presets

| Preset | Output | Equivalent To |
| :----- | :----- | :------------ |
| `PointLightBrightnessPreset.Candle` | 12 lumens | A candle flame |
| `PointLightBrightnessPreset.BulbDim` | 450 lumens | A 40W incandescent bulb |
| `PointLightBrightnessPreset.BulbTypical` (default) | 800 lumens | A 60W incandescent bulb |
| `PointLightBrightnessPreset.BulbBright` | 1,600 lumens | A 100W incandescent bulb |
| `PointLightBrightnessPreset.BulbVeryBright` | 3,000 lumens | A 200W incandescent bulb |
| `PointLightBrightnessPreset.Floodlight` | 10,000 lumens | An outdoor floodlight |

### Spot Light Presets

| Preset | Beam Intensity | Equivalent To |
| :----- | :------------- | :------------ |
| `SpotLightBrightnessPreset.DeskLamp` | 150 candela | A desk or reading lamp |
| `SpotLightBrightnessPreset.FlashlightDim` | 500 candela | A small flashlight, such as one on a keyring |
| `SpotLightBrightnessPreset.FlashlightTypical` (default) | 3,000 candela | A typical handheld flashlight |
| `SpotLightBrightnessPreset.FlashlightBright` | 20,000 candela | A powerful flashlight |
| `SpotLightBrightnessPreset.CarHeadlight` | 30,000 candela | A car headlight on full beam |
| `SpotLightBrightnessPreset.StageSpotlight` | 100,000 candela | A theatre or stage spotlight |
| `SpotLightBrightnessPreset.Searchlight` | 1,000,000 candela | A searchlight |

### Directional Light & Backdrop Presets

The directional light and backdrop presets are named after lighting conditions (times of day and weather), each matching the name of a camera exposure preset.

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

Each preset's illuminance is the *total* light of its condition, so a backdrop preset includes the light of the sun (or moon) where there is one. The directional `Midday` preset is deliberately a little brighter, because the sun only fully lights the surfaces facing it.

```csharp
using var sun = factory.LightBuilder.CreateDirectionalLight(brightnessPreset: DirectionalLightBrightnessPreset.SunriseSunset); // (1)!
camera.SetExposure(CameraExposurePreset.OutsideSunriseSunset);

scene.SetBackdrop(skyTexture, SceneBackdropBrightnessPreset.Twilight); // (2)!
camera.SetExposure(CameraExposurePreset.OutsideTwilight);
```

1.	A low sun, correctly exposed by the matching camera preset.

2.	A backdrop lit to the brightness of twilight, correctly exposed by the matching camera preset.

???+ info "Matching presets gives similar outputs"
	Matching presets gives you a roughly-identical sensitivity to light in each instance. This means that lighting a scene with only twilight-level light and then using the twilight exposure preset ends up looking as bright as lighting a scene with midday-level light and using the midday exposure preset.
	
	The presets are given so you can correctly set a *baseline* working camera setup for your target real-world brightness, calibrating from there.
	
	If you actually want your night-time scenes to look *darker* for example, you should use a camera exposure calibrated for a slightly brighter scene.
	
??? question "What about matching `PointLight`s and `SpotLight`s?"
	Point and spot lights don't have matching camera presets, because how brightly they light a scene depends on how far away they are. What matters for exposure is the illuminance arriving at the objects you're looking at:

	* A point light's illuminance falls with the square of distance. A light with an output of `L` lumens lights a surface `d` metres away to roughly `L / (4π d²)` lux.
	* A spot light's illuminance at the centre of its beam is roughly its beam intensity in candela (`BrightnessCandela`) divided by `d²`.

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

## Backdrops

A scene's [backdrop](scenes.md#backdrops) both fills the background and lights the scene from every direction (unless you use `SetBackdropWithoutIndirectLighting()`). Its brightness is set by an *intensity* (the `backdropIntensity` or `indirectLightingIntensity` parameter); and an intensity of `1f` shows the backdrop exactly as its image was authored.

```csharp
scene.SetBackdrop(sunset); // (1)!
scene.SetBackdrop(sunset, backdropIntensity: 0.5f); // (2)!
scene.SetBackdrop(sunset, SceneBackdropBrightnessPreset.SunriseSunset); // (3)!
using var newScene = factory.SceneBuilder.CreateScene(sunset, initialBackdropIntensity: 0.5f); // (4)!
```

1.	Shows the backdrop as authored (an intensity of `1f`).

2.	Shows the backdrop half as bright (lighting the scene with a quarter of the light).

3.	Lights the scene to the brightness of sunrise or sunset (2,500 lux), regardless of the authored brightness of the image (see below).

4.	Scenes can also be created with an initial backdrop intensity (or with `SceneCreationConfig.InitialBackdropIntensity`).

An intensity of `1f` uses the authored illumination intensity of the HDR/EXR image. A typical backdrop image therefore *theoretically* looks correctly exposed at `1f` through a camera with the default exposure; however image files rarely record how bright the scene they depict really was, so different images can still light a scene very differently.

For this reason, you should be prepared to tweak the lighting intensity for your chosen backdrop texture according to taste.

### Measured Brightness

To make backdrop brightness predictable, TinyFFR measures how brightly each backdrop's image lights a scene when the backdrop is loaded.

```csharp
var lux = sunset.MeasuredLux; // (1)!
scene.SetBackdrop(sunset, sunset.LuxToIntensity(5_000f)); // (2)!
var currentLux = sunset.IntensityToLux(0.5f); // (3)!
```

1.	The illuminance, in lux, that this backdrop delivers to an upward-facing surface at an intensity of `1f`.

2.	Sets the backdrop to whatever intensity lights the scene to 5,000 lux.

3.	The illuminance this backdrop delivers at an intensity of `0.5f` (a quarter of `MeasuredLux`).

`MeasuredLux` includes all the light in the image, including that of the sun if the image shows one, and is measured with the backdrop unrotated.

???+ info "Light Direction Matters"
	`MeasuredLux` describes the light falling on an *upward-facing* surface. A backdrop whose light comes mostly from one direction (such as an image with the sun in it) lights surfaces facing that direction much more brightly than surfaces facing away. An object viewed with the sun behind it can therefore look darker than the rest of a correctly-exposed scene; rotating the backdrop (`rotation:`) to move its brightest part behind the camera lights the visible side of your objects.

??? note "Colour Backdrops"
	A flat colour backdrop (`SetBackdrop(ColorVect)`) lights the scene as though it were an image of that colour in every direction, so its measured brightness depends on how bright the colour is (a mid-grey lights a scene to roughly 245 lux at `1f`). Presets work with colour backdrops in the same way as with images. A black colour can't light a scene at any intensity, so a preset leaves it at `1f`.

	Unlike an image, the flat colour drawn behind the scene is always shown exactly as specified, whatever the backdrop's intensity or the camera's exposure; only the light it contributes to the scene changes. The default backdrop of a new scene is dark grey (`0x080808`), which supplies only a faint trace of light (roughly 15 lux).

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

???+ note "Backdrops That Contain the Sun"
	Many sky images (including the built-in `BuiltInSceneBackdrop.Clouds`) already contain the sun, and the sun is usually most of their light. Adding a `DirectionalLight` for the sun on top of such a backdrop counts the sun twice. That's fine if you want the directional light's crisp shadows, but expect to lower the exposure (or the directional light's brightness) to compensate.

## Emissive Materials

Emissive (glowing) parts of materials (see [Emissive Maps](texture_map_types.md#emissive-maps)) are calibrated to look as authored through a camera with the default exposure. Like real glowing objects (screens, signs, lamps), they look dimmer through a less sensitive exposure. An emissive surface that glows strongly indoors may barely register in a sunlit scene exposed with `CameraExposurePreset.OutsideMidday`.

## Troubleshooting

!!! note ""
	#### Everything is black (or almost black):
	
	The camera's exposure is far too insensitive for the light in the scene. Check that there's a light or backdrop at all (the default backdrop provides only a trace of light), then pick the camera preset that matches your lighting, or multiply `camera.Exposure` until the scene looks right.

!!! note ""
	#### Everything is white or washed out:
	The camera's exposure is far too sensitive for the light in the scene: a common cause is a sunlit scene (a directional light or backdrop at `Midday`) viewed with the default exposure, which suits indoor lighting. Use `CameraExposurePreset.OutsideMidday`, or divide `camera.Exposure`.

!!! note ""
	#### The objects look right, but the background looks dark (or vice versa):
	The background is the backdrop's own brightness, whereas the objects are lit by every light in the scene. If the objects are also lit by a bright directional light, the camera may be correctly exposed for the objects but not the backdrop. Light the scene with the backdrop alone at a matching preset (see [Matching Presets](#matching-presets)), or raise the backdrop's intensity to suit the exposure.

!!! note ""
	#### The scene is too bright with both a sun and a sky backdrop:
	Each matching preset is correct on its own, so together they're roughly twice as bright. Divide the exposure by two, or dim one of them (see [Combining Light Sources](#combining-light-sources)).

!!! note ""
	#### My objects look dark against a well-exposed backdrop:
	The backdrop's brightest light (often the sun in the image) is probably behind the objects. Rotate the backdrop, add a light in front of the objects, or raise the exposure slightly.

!!! note ""
	#### A new backdrop image is much brighter or darker than the built-in ones:
	Images aren't authored to a common brightness. Use a `SceneBackdropBrightnessPreset` (or `LuxToIntensity()`) rather than a plain intensity to light the scene to a known brightness, whatever the image.
