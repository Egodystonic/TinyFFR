---
title: Creating Materials
description: Information on what materials are and how to create them in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * A `Material` describes how a surface looks, and is assembled from one or more texture maps. :material-arrow-right: [Materials](#materials)
    * Materials are created via `factory.MaterialBuilder`, which has a method for each type of material. :material-arrow-right: [Material Types](#material-types)

</div>

## Materials

```csharp
using var colorMap = factory.AssetLoader.LoadColorMap(@"Assets/bricks.png"); // (1)!
using var normalMap = factory.AssetLoader.LoadNormalMap(@"Assets/bricks_normal.png");

using var material = factory.MaterialBuilder.CreateStandardMaterial(colorMap, normalMap: normalMap); // (2)!

using var cubeMesh = factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube);
using var cube = factory.ObjectBuilder.CreateModelInstance(cubeMesh, material); // (3)!
scene.Add(cube);
```

1.	Loads the texture maps the material will be made from. Each type of texture map is explained in [Texture Map Types](texture_map_types.md).

2.	Creates a standard material using the color map and normal map.

3.	Creates a model instance combining the shape of a cube mesh with the surface appearance described by the material, ready to be added to a scene.

A `Material` describes the *appearance* of a surface, e.g. its colour, how bumpy, rough, or metallic it is, whether it glows or lets light through, and so on. Materials never describe *shape*; that's the job of a `Mesh`. Every object rendered by TinyFFR combines a mesh (giving its shape) with a material (giving its surface).

Materials are assembled from [texture maps](texture_map_types.md), each of which supplies one aspect of the surface's appearance (e.g. a color map supplies its colour, a normal map supplies its small-scale bumps and grooves, and so on). The maps can be [loaded from files](loading_textures.md) or [created programmatically](creating_textures.md).

A single material can be shared by any number of objects (and doing so is cheaper than creating a separate material for each one).

??? tip "Retrieving a Material's Textures"
	You can retrieve the texture a material uses for a given map with `material.TryGetAssociatedTexture()`, passing in one of the parameter name constants declared on each material type's creation config. For example: 
	
	```csharp
	var colorMap = material.TryGetAssociatedTexture(
		StandardMaterialCreationConfig.ColorMapParameterString
	);
	```

	This returns `null` if the material doesn't use a texture for that map.

### Material Types

There are several types of material, each suited to a different kind of surface and each created with its own method on the `factory.MaterialBuilder`:

| Material Type                                       | Creation Method                  | Required Maps                           | Used For                                                                                       |
| :-------------------------------------------------- | :------------------------------- | :-------------------------------------- | :--------------------------------------------------------------------------------------------- |
| [Standard](standard_materials.md)                   | `CreateStandardMaterial()`       | Color                                   | Almost every opaque (or cut-out/partially transparent) real-world surface.                      |
| [Transmissive](transmissive_materials.md)           | `CreateTransmissiveMaterial()`   | Color, Absorption-Transmission          | Surfaces that light passes *through*, such as glass, water, or ice.                             |
| [Lighting-Ignoring](lighting_ignoring_materials.md) | `CreateLightingIgnoringMaterial()` | Color                                 | Surfaces that should look the same however the scene is lit (e.g. overlays, stylised visuals).  |
| [Color-Keyed](color_keyed_materials.md)             | `CreateColorKeyedMaterial()`     | Key                                     | Surfaces whose colours are chosen individually by each object using them.                       |

If you're unsure which to use, start with a standard material: It's the right choice for the vast majority of surfaces. Each material type, and every map and option it supports, is explained in detail on its own page.

### Creation Configs

```csharp
using var material = factory.MaterialBuilder.CreateStandardMaterial(new StandardMaterialCreationConfig {
	ColorMap = colorMap, // (1)!
	NormalMap = normalMap,
	Name = "Bricks" // (2)!
});
```

1.	The maps that a material type requires are declared as `required` properties on its config, so you can't forget to set them.

2.	Every material config also has the `Name` and `EnablePerInstanceEffects` properties (see below).

Every material creation method generally has two overloads; one that takes each option as a (mostly optional) argument, and one that takes a creation config struct for that material type (`StandardMaterialCreationConfig`, `TransmissiveMaterialCreationConfig`, `LightingIgnoringMaterialCreationConfig`, or `ColorKeyedMaterialCreationConfig`). Both are equivalent, so use whichever you find more readable.

Every material type supports the following common options:

<span class="def-icon">:material-card-bulleted-outline:</span> `Name`

:   The name to give the material. May be left empty.

<span class="def-icon">:material-card-bulleted-outline:</span> `EnablePerInstanceEffects`

:   Whether each object using this material may alter the material's appearance individually at runtime (e.g. moving or blending its textures), via the object's `MaterialEffects` property. Defaults to `false`. See [Material Effects](material_effects.md) for more information.

	???+ warning "Per-Instance Effects Are Costly"
		Enabling per-instance effects makes a material markedly more expensive to render, *whether or not any object actually uses them*. Only enable this option for materials that need it.

		This option can only be set when the material is created. You can check whether an existing material supports per-instance effects with its `SupportsPerInstanceEffects` property.

## Built-in Materials

TinyFFR provides two materials that can be used without creating or loading any textures:

#### The "Default" Material

`factory.MaterialBuilder.DefaultMaterial` is used automatically by any object created without a material (i.e. when passing `null` as the material to `CreateModelInstance()`). Its appearance can be adjusted per object, and it's useful for prototyping, debugging, and simple visuals.

See more here: [The Default Material](the_default_material.md)

#### The "Test" Material

`factory.MaterialBuilder.CreateTestMaterial()` creates a material that displays a UV test pattern, which makes it easy to see how textures will be stretched, rotated, or repeated across a mesh's surface.

See more here: [The Test Material](the_test_material.md)

## Material Lifetimes

Like every other resource in TinyFFR, a material should be disposed when you're done with it. A material keeps track of the textures it was created from (and every object using a material keeps track of that material), so disposal must happen in the following order:

1.	Every object using the material (e.g. every `ModelInstance` and `Model`);
2.	The material itself;
3.	The textures the material uses (if nothing else is using them).

Attempting to dispose a material that's still in use by an object, or a texture that's still in use by a material, throws a `ResourceDependencyException`. Note that disposing a material does *not* dispose its constituent textures.
