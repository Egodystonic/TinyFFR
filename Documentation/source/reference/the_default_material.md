---
title: The Default Material
description: Information on how to use TinyFFR's built-in default material.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * The default material is a built-in material that requires no textures, and is used by any object created without a material. :material-arrow-right: [The Default Material](#the-default-material)
    * Each object using it can choose its own colour and shading style. :material-arrow-right: [Shading Styles](#shading-styles)

</div>

## The Default Material

```csharp
using var cubeMesh = factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube);

using var cube = factory.ObjectBuilder.CreateModelInstance(cubeMesh); // (1)!
cube.SetDefaultMaterialBaseColor(StandardColor.Red); // (2)!
cube.SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle.Plain); // (3)!
```

1.	Creates a model instance without specifying a material, so it uses the default material.

2.	Sets the colour this object is drawn in.

3.	Sets the style this object is drawn in (see [Shading Styles](#shading-styles) below).

The *default material* is a special built-in material that needs no textures at all. Any object created without a material (e.g. by passing `null`, or simply omitting the material argument, when calling `factory.ObjectBuilder.CreateModelInstance()`) uses the default material. It can also be accessed directly via `factory.MaterialBuilder.DefaultMaterial`.

Rather than describing a realistic surface, the default material draws objects in a single flat colour with one of a handful of simple shading styles. This makes it useful for:

* Prototyping, before you've created or loaded any real materials;
* Debugging and diagnostic visuals (e.g. visualising bounding volumes, collision shapes, or a mesh's triangles);
* Simple, flat-coloured visuals that don't need any textures.

The default material is unlit (it ignores the scene's lighting entirely), and objects drawn with it neither cast nor receive shadows.

The default material is owned by TinyFFR, so you never need to dispose it (doing so has no effect). You can check whether a material is the default material with the `IsDefault` property.

You don't need to set an object's `Material` property to `materialBuilder.DefaultMaterial`; simply invoking `SetDefaultMaterialBaseColor()` or `SetDefaultMaterialShadingStyle()` sets it automatically.

### Per-Object Colour and Style

Every object using the default material has its own colour and shading style, set with the following methods:

<span class="def-icon">:material-card-bulleted-outline:</span> `SetDefaultMaterialBaseColor(ColorVect baseColor)`

:   Sets the colour the object is drawn in. Defaults to opaque white.

	If the colour's alpha is less than 100%, the object is drawn partially transparent (except in the [wireframe](#wireframe-rendering) style, which always draws its lines opaque). Partially-transparent objects invoke the transparency-sorting algorithm, which is not 100% accurate (for the sake of performance) and can result in 'flickering' between translucent overlapping objects.

<span class="def-icon">:material-card-bulleted-outline:</span> `SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle style)`

:   Sets the style the object is drawn in (see below). Defaults to `DefaultMaterialShadingStyle.Plain3D`.

Because each object's colour and style are its own, changing them on one object never affects any other object using the default material.

These methods are available on model instances, as well as on model instance groups (where they apply to every instance in the group). Calling either method on an object that's currently using a different material switches that object to the default material.

## Shading Styles

The `DefaultMaterialShadingStyle` enum has the following values:

<span class="def-icon">:material-card-bulleted-outline:</span> `DefaultMaterialShadingStyle.Plain3D`

:   The object's surfaces appear to be lit by a light shining from the camera itself. Surfaces facing the camera are drawn in the full base colour, whereas surfaces seen at more oblique angles are drawn progressively darker (down to a quarter of the base colour's brightness). This gives the object a sense of shape and depth regardless of the scene's lighting. This is the default.

<span class="def-icon">:material-card-bulleted-outline:</span> `DefaultMaterialShadingStyle.Plain`

:   The object is drawn in a completely flat, uniform colour, ignoring the viewing angle entirely. As the object's shape isn't picked out by any shading, this tends to look like a silhouette; it's most useful for markers, highlights, and other elements that must stay clearly visible no matter how the scene is lit.

<span class="def-icon">:material-card-bulleted-outline:</span> `DefaultMaterialShadingStyle.Wireframe`

:   Only the edges of the object's triangles are drawn, leaving their faces empty. This is mostly useful for debugging, as it shows exactly how an object's mesh is constructed. This style requires the object's mesh to support wireframe rendering (see [Loading Meshes](loading_meshes.md#wireframe-data)).
