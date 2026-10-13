---
title: Color-Keyed Materials
description: Information on how to create and use color-keyed (per-object recolourable) materials in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Color-keyed materials let every object using them choose its own colours, using a single "key map" texture to mark where each colour goes. :material-arrow-right: [Color-Keyed Materials](#color-keyed-materials)
    * Each channel of the key map (red, green, blue, and alpha) controls how strongly one of four per-object colours appears on the surface. :material-arrow-right: [The Key Map](#the-key-map)
    * Parts of the surface can be cut out entirely, or (optionally) made translucent. :material-arrow-right: [Transparency](#transparency)

</div>

## Color-Keyed Materials

```csharp
using var keyMap = factory.AssetLoader.LoadTexture(@"Assets/vehicle_key.png", TextureDataType.LinearData); // (1)!
using var vehicleMaterial = factory.MaterialBuilder.CreateColorKeyedMaterial(keyMap); // (2)!

using var redVehicle = factory.ObjectBuilder.CreateModelInstance(vehicleMesh, vehicleMaterial);
redVehicle.SetKeyedMaterialColor(ColorChannel.R, StandardColor.Red); // (3)!
redVehicle.SetKeyedMaterialColor(ColorChannel.G, StandardColor.White);

using var blueVehicle = factory.ObjectBuilder.CreateModelInstance(vehicleMesh, vehicleMaterial);
blueVehicle.SetKeyedMaterialColor(ColorChannel.R, StandardColor.Blue); // (4)!
blueVehicle.SetKeyedMaterialColor(ColorChannel.G, StandardColor.Yellow);
```

1.	Loads the key map. In this example, the red channel of the key map marks the vehicle's body and the green channel marks its trim.

	Note that key maps must be loaded as `LinearData` (see [The Key Map](#the-key-map) below).

2.	Creates a color-keyed material from the key map. Both vehicles below share this one material.

3.	Paints the first vehicle's body (the key map's red channel) red, and its trim (the key map's green channel) white.

4.	Paints the second vehicle's body blue and its trim yellow, without affecting the first vehicle.

A *color-keyed* material lets each object that uses it pick its own colours. Rather than a color map, the material is created from a *key map*: A texture whose red, green, blue, and alpha channels mark *where* each of four colours appears on the surface. The colours themselves are then chosen individually by each object using the material.

This makes color-keyed materials useful for things such as recolourable icons, markers, decals, and debug visuals; and highlighting objects or parts of objects in a chosen colour at runtime (e.g. selection highlights).

Like [lighting-ignoring materials](lighting_ignoring_materials.md), color-keyed materials are *unlit*: They ignore the scene's lighting entirely, so the chosen colours appear the same no matter how the scene is lit. Objects using a color-keyed material also neither cast nor receive shadows. They are cheap to render.

Color-keyed materials are created with `factory.MaterialBuilder.CreateColorKeyedMaterial()`. The basics of creating materials (including creation configs and lifetimes) are explained in [Creating Materials](creating_materials.md).

### The Key Map

Each channel of the key map is a *weighting* for one of the four colours, and the colour of each point on the surface is the sum of the four colours multiplied by their weights at that point:

`surface colour = (R key × R colour) + (G key × G colour) + (B key × B colour) + (A key × A colour)`

(Each colour is an RGBA colour, so the surface's alpha is calculated the same way. The result is clamped to a maximum of 100% in every channel.)

For example, where the key map is pure red `(255, 0, 0)` the surface shows exactly the R colour; where it's half-red, half-green `(128, 128, 0)` the surface shows an even mix of the R and G colours; and where it's black the R, G, and B colours contribute nothing.

Until an object chooses its own colours, each channel has the following default colour:

<span class="def-icon">:material-card-bulleted-outline:</span> Red channel

:   Defaults to red (with 0% alpha).

<span class="def-icon">:material-card-bulleted-outline:</span> Green channel

:   Defaults to green (with 0% alpha).

<span class="def-icon">:material-card-bulleted-outline:</span> Blue channel

:   Defaults to blue (with 0% alpha).

<span class="def-icon">:material-card-bulleted-outline:</span> Alpha channel

:   Defaults to black (with 100% alpha).

This means that, by default, an object simply displays the key map itself (with its alpha channel as the surface's opacity).

If the key map has no alpha channel (i.e. it's a three-channel RGB texture), its alpha key is treated as 100% everywhere. In this case the alpha channel's colour is added across the *entire* surface, so it acts as a "base" colour that the other three colours are added on top of. By default this is opaque black, which simply makes the whole surface opaque.

???+ warning "Load Key Maps as Linear Data"
	A key map's channels are weightings rather than colours, so it must be loaded with the `TextureDataType.LinearData` [data type](loading_textures.md#texture-data-types) (e.g. via `assetLoader.LoadTexture(path, TextureDataType.LinearData)`, as in the example above).

	Loading a key map with `LoadColorMap()` (or any other `ColorSrgb` texture) would apply sRGB decoding to its values, meaning a 50% key would no longer mix two colours evenly.

| Map     | Argument / Config Property | Required | Effect                                                                    |
| :------ | :------------------------- | :------- | :------------------------------------------------------------------------ |
| Key     | `keyMap` / `KeyMap`        | Yes      | Marks where (and how strongly) each of the four per-object colours appears. |

The key map is the only map supported by color-keyed materials. It may be a three-channel (RGB) or four-channel (RGBA) texture.

## Setting Key Colors

```csharp
modelInstance.SetKeyedMaterialColor(ColorChannel.R, new ColorVect(0.1f, 0.6f, 0.2f)); // (1)!
modelInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent); // (2)!
```

1.	Sets the colour shown wherever the key map's red channel is set to a dark green.

2.	Sets the colour of the key map's alpha channel to be fully transparent (see [Transparency](#transparency) below).

An object's key colours are set with `SetKeyedMaterialColor()`, specifying which channel of the key map to set the colour for (`ColorChannel.R`, `.G`, `.B`, or `.A`) and the colour to use. The colours can be changed at any time.

The first time an object's key colours are set, TinyFFR gives that object its own private copy of the material (which is how each object can have different colours while sharing one material). Objects whose colours are never set continue to share the original material. This means every object with its own colours has a small additional memory cost (that of one extra material); but there's no need to manage or dispose the copies yourself.

`SetKeyedMaterialColor()` has no effect on objects whose material isn't a color-keyed material. You can check whether a material is color-keyed with its `SupportsColorKeying` property.

## Transparency

```csharp
using var translucentMaterial = factory.MaterialBuilder.CreateColorKeyedMaterial( // (1)!
	keyMap, 
	blendOutputAlphaWithScene: true
);

using var highlight = factory.ObjectBuilder.CreateModelInstance(mesh, translucentMaterial);
highlight.SetKeyedMaterialColor(ColorChannel.R, new ColorVect(1f, 0.8f, 0f, 0.4f, multiplyAlpha: true)); // (2)!
```

1.	Creates a color-keyed material whose output is blended with the scene behind it.

2.	Sets a 40%-opaque yellow colour for the red channel. Note the `multiplyAlpha: true` argument, which premultiplies the colour's alpha (see below).

The surface's alpha (calculated in the same way as its colour; see [The Key Map](#the-key-map) above) is used according to the `blendOutputAlphaWithScene` argument (or `BlendOutputAlphaWithScene` config property):

<span class="def-icon">:material-card-bulleted-outline:</span> `false` (default)

:   The alpha simply switches each point on the surface on or off: Anywhere with less than 40% alpha isn't drawn at all, and everywhere else is drawn fully opaque.

	This is the cheaper option, and lets you cut holes or shapes out of the surface (e.g. by setting the alpha channel's colour, or any other channel's colour, to `ColorVect.BlackTransparent`).

<span class="def-icon">:material-card-bulleted-outline:</span> `true`

:   The alpha genuinely blends the surface with whatever is behind it, allowing translucent key colours.

	In this case the key colours' alpha must be [premultiplied](texture_map_types.md#alpha-premultiplication). `SetKeyedMaterialColor()` doesn't premultiply colours for you, so create translucent colours with `multiplyAlpha: true` (as in the example above) or `colour.WithPremultipliedAlpha()`.

	This has a slight additional rendering cost, and invokes the transparency-sorting algorithm which is not 100% accurate (for the sake of performance) and can result in 'flickering' between translucent overlapping objects.

## Per-Instance Effects

Color-keyed materials don't support [material effects](material_effects.md).

## ColorKeyedMaterialCreationConfig

```csharp
using var material = factory.MaterialBuilder.CreateColorKeyedMaterial(new ColorKeyedMaterialCreationConfig {
	KeyMap = keyMap,
	BlendOutputAlphaWithScene = false,
	Name = "Vehicle Livery"
});
```

For reference, the following is every property on the `ColorKeyedMaterialCreationConfig`:

<span class="def-icon">:material-card-bulleted-outline:</span> `KeyMap`

:   The key map. This property is required. May be a three-channel (RGB) or four-channel (RGBA) texture.

<span class="def-icon">:material-card-bulleted-outline:</span> `BlendOutputAlphaWithScene`

:   Whether the surface's alpha blends it with the scene behind it (see [Transparency](#transparency)). Defaults to `false`.

<span class="def-icon">:material-card-bulleted-outline:</span> `Name`

:   The name to give the material. May be left empty.
