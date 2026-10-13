---
title: Lighting-Ignoring Materials
description: Information on how to create and use lighting-ignoring (unlit) materials in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Lighting-ignoring materials display their color map as-is, completely unaffected by the scene's lighting. :material-arrow-right: [Lighting-Ignoring Materials](#lighting-ignoring-materials)
    * A color map is the only map they use. :material-arrow-right: [Supported Maps](#supported-maps)
    * A color map with an alpha channel always blends the surface with whatever is behind it. :material-arrow-right: [Transparency](#transparency)

</div>

## Lighting-Ignoring Materials

```csharp
using var colorMap = factory.AssetLoader.LoadColorMap(@"Assets/warning_sign.png");
using var signMaterial = factory.MaterialBuilder.CreateLightingIgnoringMaterial(colorMap); // (1)!

using var markerColorMap = factory.TextureBuilder.CreateColorMap(new ColorVect(1f, 0.5f, 0f), includeAlpha: false); // (2)!
using var markerMaterial = factory.MaterialBuilder.CreateLightingIgnoringMaterial(markerColorMap);
```

1.	Creates a lighting-ignoring material that shows the color map loaded from `warning_sign.png`.

2.	Creates a 1x1 plain orange color map, and then a lighting-ignoring material from it. Every object using `markerMaterial` will appear as a flat, uniform orange, no matter how it's lit.

A *lighting-ignoring* material (sometimes called an "unlit" material) displays its color map exactly as it is, completely ignoring the scene's lighting. Lights, the scene's ambient lighting, and shadows have no effect on it. A surface using a lighting-ignoring material therefore looks the same no matter how (or whether) the scene is lit.

This makes them useful for things such as:

* Markers, gizmos, highlights, and debug overlays that must always be clearly visible;
* Signs, screens, and labels placed in the 3D world;
* Stylised, cartoon, or "flat" art styles;
* Textures that already have their lighting "baked" in to them.

Lighting-ignoring materials are also among the cheapest materials to render, as their appearance needs no lighting calculations. However, objects using lighting-ignoring materials tend to look "flat" and "2D".

Note that although the scene's lighting has no effect on these materials, they are still affected by the scene's fog (if any has been added with `scene.AddFog()`), and by the renderer's post-processing (e.g. tone mapping and bloom) like everything else in the scene.

Objects using a lighting-ignoring material also neither cast nor receive shadows.

Lighting-ignoring materials are created with `factory.MaterialBuilder.CreateLightingIgnoringMaterial()`. The basics of creating materials (including creation configs and lifetimes) are explained in [Creating Materials](creating_materials.md).

### Supported Maps

| Map                                      | Argument / Config Property | Required | Effect                                                                        |
| :--------------------------------------- | :------------------------- | :------- | :---------------------------------------------------------------------------- |
| [Color](texture_map_types.md#color-maps) | `colorMap` / `ColorMap`    | Yes      | The colour displayed on the surface (and, optionally, its transparency).      |

A color map is the only map supported by lighting-ignoring materials. The other map types (normal, ORM(R), etc.) all describe how a surface responds to light, so they have no use for a material that ignores lighting.

## Transparency

```csharp
using var ghostColorMap = factory.TextureBuilder.CreateColorMap( // (1)!
	new ColorVect(0.6f, 0.8f, 1f, 0.3f), 
	includeAlpha: true
);

using var ghostMaterial = factory.MaterialBuilder.CreateLightingIgnoringMaterial(ghostColorMap);
```

1.	Creates a 30%-opaque pale blue color map. Color maps created by the texture builder with `includeAlpha: true` have their alpha premultiplied automatically.

If the material's color map has an alpha channel (i.e. it's a four-channel RGBA texture), the surface is blended with whatever is behind it according to the alpha. This requires the color map's alpha to be [premultiplied](texture_map_types.md#alpha-premultiplication). If the color map has no alpha channel, the surface is always fully opaque.

Unlike [standard materials](standard_materials.md#transparency), lighting-ignoring materials have no "mask-only" alpha mode: Alpha always genuinely blends the surface with the scene behind it. This means even surfaces whose alpha is only ever fully opaque or fully transparent (e.g. a cut-out shape) are drawn as transparent surfaces, which invokes the transparency-sorting algorithm. This is not 100% accurate (for the sake of performance), and can result in 'flickering' between translucent overlapping objects.

## Per-Instance Effects

If a lighting-ignoring material is created with `enablePerInstanceEffects: true` (or the `EnablePerInstanceEffects` config property), each object using it can alter its appearance individually at runtime via the object's `MaterialEffects` property. See [Material Effects](material_effects.md) for more information on using material effects.

A lighting-ignoring material supports the following effects:

* Transforming (moving, rotating, and scaling) its texture across the surface;
* Blending its color map towards a second color map.

## LightingIgnoringMaterialCreationConfig

```csharp
using var material = factory.MaterialBuilder.CreateLightingIgnoringMaterial(new LightingIgnoringMaterialCreationConfig {
	ColorMap = colorMap,
	EnablePerInstanceEffects = true,
	Name = "Scrolling Sign"
});
```

For reference, the following is every property on the `LightingIgnoringMaterialCreationConfig`:

<span class="def-icon">:material-card-bulleted-outline:</span> `ColorMap`

:   The color map. This property is required. May be a three-channel (RGB) or four-channel (RGBA) texture.

<span class="def-icon">:material-card-bulleted-outline:</span> `Name` / `EnablePerInstanceEffects`

:   The options common to every material type; see [Creating Materials](creating_materials.md#creation-configs).
