---
title: Resource Dependencies
description: Information on how TinyFFR tracks dependencies between resources, and how that affects disposing them.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * When one resource uses another resource, TinyFFR records a *dependency* between them. :material-arrow-right: [Resource Dependencies](#resource-dependencies)
    * A resource can't be disposed while anything still depends on it; attempting to do so throws a `ResourceDependencyException`. :material-arrow-right: [Disposing Resources](#disposing-resources)
    * Dependencies are released when the dependent resource is disposed, or stops using the resource it depended on. :material-arrow-right: [Releasing Dependencies](#releasing-dependencies)

</div>

## Resource Dependencies

```csharp
var colorMap = factory.TextureBuilder.CreateColorMap(StandardColor.Red, includeAlpha: false, name: "Bricks Color");
var material = factory.MaterialBuilder.CreateStandardMaterial(colorMap, name: "Bricks"); // (1)!

try {
	colorMap.Dispose(); // (2)!
}
catch (ResourceDependencyException e) {
	Console.WriteLine("Oops! I tried to dispose something in use: " + e.Message);
}

material.Dispose(); // (3)!
colorMap.Dispose();
```

1.	The material uses `colorMap`, so a dependency is recorded: the material *depends on* the texture.

2.	This throws a `ResourceDependencyException`, because the texture is still in use by the material. The texture is left untouched (i.e. it is not disposed).

3.	Disposing the material first releases its dependency on the texture, which can then be disposed.

Many resources in TinyFFR are built from other resources: a `Material` uses `Texture`s, a `Model` uses a `Mesh` and a `Material`, a `Scene` contains `ModelInstance`s and lights, a `Renderer` uses a `Scene`, a `Camera`, and a `Window`, and so on.

Every time one resource starts using another, TinyFFR records a *dependency* between them. The resource doing the using is the *dependent*, and the resource being used is its *target*. While a target has at least one dependent, it can't be disposed.

??? question "Why Track Dependencies?"
	Most resources in TinyFFR represent data held in unmanaged (native/GPU) memory. Disposing a resource that's still in use by another (e.g. disposing a texture that a material is still sampling from) would leave the dependent resource reading from freed memory, which would result in rendering corruption or an application crash *at best*.

	Tracking dependencies turns that class of bug in to an immediate, descriptive exception at the point the mistake is made.

### Tracked Dependencies

The following dependencies are tracked:

| Dependent                       | Depends On                                                                                         |
| :------------------------------ | :------------------------------------------------------------------------------------------------- |
| `Material`                      | Every `Texture` it uses (color map, normal map, ORM map, etc).                                     |
| `Model`                         | Its `Mesh` and `Material`.                                                                         |
| `ModelInstance`                 | Its `Mesh` and `Material` (the [default material](the_default_material.md) excepted).              |
| `Scene`                         | Every `ModelInstance` and light added to it, and its `BackdropTexture` (if one is set).            |
| `Renderer`                      | Its `Scene`, `Camera`, and render target (`Window` or `RenderOutputBuffer`).                       |
| `RendererCompositor`            | Its render target, and every `Renderer` added to it.                                               |
| `MeshGroupAnimationTable`       | The `Mesh`es it animates.                                                                          |
| Mesh views of a `DynamicVertexBuffer` | The `DynamicVertexBuffer` they were created from.                                            |
| `ResourceGroup`                 | Every resource added to it (see [Resource Groups](resource_groups.md)).                            |
| `TextInstance`                  | The `FontPen` and `FontString` it uses.                                                            |
| `FontPen`, `FontString`         | The `Font` they were created from. The font *owns* them (see below).                               |

## Disposing Resources

When you attempt to dispose a resource that still has dependents, a `ResourceDependencyException` is thrown and the resource is *not* disposed. The exception message names the resource and (up to three of) its dependents, for example:

```
Can not execute this action (i.e. dispose or mutation) for Texture 'Bricks Color' because it is still in use by 1 other
resource(s) ('Bricks'). Dispose or otherwise relinquish the dependency on those resources first before executing this
action on 'Bricks Color'.
```

!!! tip "Name Your Resources"
	The exception message identifies resources by their names. Unnamed resources are given generated names (e.g. `Unnamed Texture 645C6936EC80`), which are much harder to track down; so giving your resources names (via the `name` parameter that every creation method accepts) makes dependency errors far easier to diagnose.

The simplest way to avoid dependency errors is to dispose resources in the *reverse* order to the order you created them in. Because a resource can only depend on resources that already exist, disposing in reverse order always disposes dependents before their targets:

```csharp
using var colorMap = factory.TextureBuilder.CreateColorMap(StandardColor.Red, includeAlpha: false);
using var material = factory.MaterialBuilder.CreateStandardMaterial(colorMap);
using var mesh = factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube);
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);
using var scene = factory.SceneBuilder.CreateScene();
scene.Add(instance); // (1)!
```

1.	`using` declarations are disposed in reverse order at the end of their scope, so here the scene is disposed first, then the instance, then the mesh, material, and finally the texture.

The same principle applies to [resource groups](resource_groups.md), which dispose their contents in the reverse order they were added.

### Mutation

Dependencies can also prevent *modifying* a resource, where the modification would invalidate its dependents. Currently, this only applies to `DynamicVertexBuffer`s when resizing a buffer's vertex or index storage throws a `ResourceDependencyException` while any mesh views created from it are still alive.

### Owned Resources

Some resources *own* other resources that they create, and dispose them when they're disposed themselves:

* A `Font` owns its atlas texture, and every `FontPen` and `FontString` created from it.
* A `RenderOutputBuffer` owns the texture it renders in to.

Owned resources don't stop their owner being disposed. However, anything *outside* the owner that still uses something it owns *does* stop it. For example, a font can't be disposed while a `TextInstance` is still drawing with one of its pens. In that case the exception names the pen (or string) that's still in use, and the instance using it:

```csharp
var font = factory.AssetLoader.LoadFont();
var pen = font.CreatePen(StandardColor.White);
var str = font.CreateString("Hello");
var text = factory.ObjectBuilder.CreateTextInstance(pen, str);

font.Dispose(); // (1)!
text.Dispose();
font.Dispose(); // (2)!
```

1.	Throws a `ResourceDependencyException`, because `text` is still using `pen` and `str`.

2.	Succeeds, and disposes `pen` and `str` along with the font.

## Releasing Dependencies

A dependent's dependencies are released when:

* The dependent is disposed. For example, disposing a `Material` releases its dependency on every `Texture` it uses.
* The dependent stops using the target. For example, `modelInstance.SetMaterial(otherMaterial)` releases the instance's dependency on its previous material, and `scene.Remove(modelInstance)` releases the scene's dependency on the instance.

```csharp
var oldMaterial = factory.MaterialBuilder.CreateStandardMaterial(colorMap);
var newMaterial = factory.MaterialBuilder.CreateStandardMaterial(otherColorMap);
var instance = factory.ObjectBuilder.CreateModelInstance(mesh, oldMaterial);

instance.SetMaterial(newMaterial); // (1)!
oldMaterial.Dispose(); // (2)!
```

1.	The instance now depends on `newMaterial`, and no longer on `oldMaterial`.

2.	`oldMaterial` can now be disposed, as nothing depends on it.
