---
title: Spot Lights
description: Information on how to create and adjust spot lights.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * A spot light emits light from a position in a cone, like a torch or a theatre spotlight. :material-arrow-right: [Spot Lights](#spot-lights)
    * The cone has an outer angle, and an inner angle within which the light is at full strength. :material-arrow-right: [Cone](#cone)
    * Spot lights can be created to dim as their cone widens, as real spot lights do. :material-arrow-right: [Cone & Brightness](#cone-brightness)

</div>

## Spot Lights

```csharp
using var light = factory.LightBuilder.CreateSpotLight( // (1)!
	position: new Location(0f, 3f, 0f),
	coneDirection: Direction.Down,
	coneAngle: 40f,
	intenseBeamAngle: 30f
);
scene.Add(light); // (2)!

light.ConeDirection = new Direction(0f, -1f, 1f); // (3)!
```

1.	Creates a spot light 3 units above the origin, pointing down, with a 40° cone that's at full strength for the central 30°.

2.	Adds the light to a [scene](scenes.md). A light only illuminates the scenes it has been added to.

3.	Points the light down and forward.

A `SpotLight` emits light from a position in a cone, like a torch, a desk lamp, or a theatre spotlight. Objects outside the cone receive no light from it.

Spot lights share the [brightness](point_lights.md#brightness), [colour](point_lights.md#colour), and [shadow](point_lights.md#shadows) properties common to every kind of light, which are explained on the [Point Lights](point_lights.md) page.

### Creating Spot Lights

Spot lights are created with `factory.LightBuilder.CreateSpotLight()`, which accepts the following optional parameters (or alternatively a `SpotLightCreationConfig` with equivalent properties):

<span class="def-icon">:material-card-bulleted-outline:</span> `position`

:   Where the light is. Defaults to the origin.

<span class="def-icon">:material-card-bulleted-outline:</span> `coneDirection`

:   Which way the light points. Defaults to `Direction.Down`.

<span class="def-icon">:material-card-bulleted-outline:</span> `coneAngle`, `intenseBeamAngle`

:   The shape of the cone. Default to 70° and 15° respectively. See [Cone](#cone).

<span class="def-icon">:material-card-bulleted-outline:</span> `color`, `brightness`, `castsShadows`

:   The light's colour (default white), brightness (default `1f`), and whether it casts shadows (default `false`). See [Point Lights](point_lights.md#brightness).

<span class="def-icon">:material-card-bulleted-outline:</span> `maxDistance`

:   How far down its cone the light reaches, in metres. Defaults to `15f`. See [Range](#range).

<span class="def-icon">:material-card-bulleted-outline:</span> `highQuality`

:   Whether the light dims as its cone widens. Defaults to `false`. Can only be set at creation. See [Cone & Brightness](#cone-brightness).

<span class="def-icon">:material-card-bulleted-outline:</span> `name`

:   An optional name for the light.

## Cone

A spot light's cone is described by three properties:

<span class="def-icon">:material-card-bulleted-outline:</span> `ConeDirection`

:   Which way the light points; i.e. the direction its cone of light travels along. A spot light's `RotateBy()` methods rotate this direction.

<span class="def-icon">:material-card-bulleted-outline:</span> `ConeAngle`

:   How wide the cone is. Nothing outside the cone receives any light. Clamped to between `SpotLight.MinConeAngle` (1°) and `SpotLight.MaxConeAngle` (180°).

<span class="def-icon">:material-card-bulleted-outline:</span> `IntenseBeamAngle`

:   How wide the fully-lit centre of the cone is. Between this angle and `ConeAngle`, the light fades out to nothing.

Both angles are the cone's *full* width, not the angle from its centre to its edge; so a `ConeAngle` of 90° describes a cone spreading 45° in every direction from `ConeDirection`.

The relationship between the two angles controls how hard or soft the edge of the pool of light looks. Setting `IntenseBeamAngle` close to `ConeAngle` gives a sharply-defined circle of light, like a theatre spotlight; setting it much smaller gives a soft glow that fades gradually outward, like a desk lamp.

The intense beam can't be wider than the cone, so the two angles push each other: setting `IntenseBeamAngle` wider than `ConeAngle` widens the cone to match, and setting `ConeAngle` narrower than `IntenseBeamAngle` narrows the beam to match.

## Range

Like a point light's [range](point_lights.md#range), a spot light's `MaxIlluminationDistance` is the distance (in metres) down its cone beyond which it contributes no light at all. Setting it too small for the light's brightness produces a visible edge where the light abruptly cuts off.

## Brightness

A spot light's brightness works exactly like a [point light's](point_lights.md#brightness): `1f` corresponds to `SpotLight.DefaultLumens` (1,250,000 lumens), and the relationship is quadratic. `SpotLight.LumensToBrightness()` and `SpotLight.BrightnessToLumens()` convert between the two.

## Cone & Brightness

In the real world, a spot light concentrates a fixed amount of light in to its cone. Widening the cone spreads the same light over a larger area, so everything it lights becomes dimmer; narrowing the cone makes it brighter.

This is physically correct, but it can make spot lights awkward to work with, because adjusting the cone also changes how bright the light looks. By default, TinyFFR's spot lights therefore *don't* behave this way: a spot light's brightness is the same whatever its cone angle.

To create a spot light that does behave physically correctly (dimming as its cone widens), pass `highQuality: true` to `CreateSpotLight()` (or set `IsHighQuality` in a `SpotLightCreationConfig`). This can only be chosen when the light is created.
