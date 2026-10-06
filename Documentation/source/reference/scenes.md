---
title: Scenes
description: Information on how to create scenes, populate them, give them a backdrop, and query what's in them.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * A `Scene` is a kind of virtual container of objects, lights, etc. that should rendered together. :material-arrow-right: [Scenes](#scenes)
    * Scenes can have backdrop textures and/or fog added to them. :material-arrow-right: [Backdrops](#backdrops), [Fog](#fog)
    * It's possible to run ray test queries on objects in a scene. :material-arrow-right: [Scene Queries](#scene-queries)

</div>

## Scenes

```csharp
using var scene = factory.SceneBuilder.CreateScene(); // (1)!
scene.SetBackdrop(BuiltInSceneBackdrop.Clouds, SceneBackdropBrightnessPreset.Midday);

using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);
scene.Add(instance); // (2)!

using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -3f));
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window); // (3)!
```

1.	Creates a new scene, and sets its backdrop to one of TinyFFR's built-in backdrops. The backdrop lights the scene as well as filling its background.

2.	Adds a model instance to the scene. Nothing appears in (or lights) a scene until it has been added.

3.	Creates a renderer that draws the scene (as seen by the camera) to the window.

A `Scene` is a collection of everything that should be rendered together, including the [objects](scene_objects.md) in a world, the [lights](point_lights.md) that illuminate them, the [backdrop](#backdrops) behind them, and any [fog](#fog) between. Scenes are created with `factory.SceneBuilder`, and drawn by renderering them through a `Camera`.

Objects and lights must be explicitly added to a scene before they appear in it. Any given object or light can belong to several scenes at once.

A scene [depends on](resource_dependencies.md) everything added to it, so an object or light can't be disposed while it's still in a (non-disposed) scene. Remove it from the scene first, or dispose the scene before its contents.

TinyFFR also has `CanvasScene`s for drawing 2D content (such as user interfaces) over the top of a 3D scene; these are explained on their own page: [Canvas Scenes](canvas_scenes.md).

### Creating Scenes

`factory.SceneBuilder.CreateScene()` has overloads that set the new scene's initial backdrop:

```csharp
using var defaultScene = factory.SceneBuilder.CreateScene(); // (1)!
using var colorScene = factory.SceneBuilder.CreateScene(new ColorVect(0.5f, 0.5f, 0.5f)); // (2)!
using var cloudsScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds); // (3)!
using var textureScene = factory.SceneBuilder.CreateScene(backdropTexture); // (4)!
```

1.	A scene with the default backdrop: a very dark grey (`0x080808`). This is deliberately near-black rather than pure black, so that the scene supplies a trace of ambient light and objects in it aren't completely unlit.

2.	A scene with a flat colour backdrop. A mid-grey like this lights the scene evenly from every direction. Passing `null` creates a scene with no backdrop at all.

3.	A scene with one of the [built-in backdrops](#built-in-backdrops).

4.	A scene with a [backdrop texture](backdrop_textures.md) as its backdrop.

Alternatively, a `SceneCreationConfig` can be passed, with the following properties:

<span class="def-icon">:material-card-bulleted-outline:</span> `Name`

:   A name for the scene.

<span class="def-icon">:material-card-bulleted-outline:</span> `InitialBackdropColor`

:   A flat colour backdrop, or `null` for none. Defaults to `SceneCreationConfig.DefaultInitialBackdropColor` (the very dark grey described above).

<span class="def-icon">:material-card-bulleted-outline:</span> `InitialBackdrop`

:   One of the [built-in backdrops](#built-in-backdrops), or `null` for none. Defaults to `null`.

<span class="def-icon">:material-card-bulleted-outline:</span> `InitialBackdropTexture`

:   A [backdrop texture](backdrop_textures.md), or `null` for none. Defaults to `null`.

A scene only has one backdrop, so `InitialBackdropColor` is ignored if either `InitialBackdrop` or `InitialBackdropTexture` is set.

## Adding & Removing Contents

Every kind of object that can be part of a scene has an `Add()` and `Remove()` overload on `Scene`:

| Object                                        | Explained In                                             |
| :-------------------------------------------- | :------------------------------------------------------- |
| `ModelInstance`, `ModelInstanceGroup`          | [Scene Objects](scene_objects.md)                        |
| `PointLight`, `SpotLight`, `DirectionalLight`  | [Point Lights](point_lights.md), [Spot Lights](spot_lights.md), [Directional Lights](directional_lights.md) |
| `QuadInstance`                                | [Quads](quads.md)                                        |
| `MutableGridInstance`                         | [Dynamic Meshes & Mutable Grids](dynamic_meshes_and_mutable_grids.md) |
| `TextInstance`                                | [Text Instances](text_instances.md)                      |
| `CameraLockedQuadInstance`, `CameraLockedTextInstance` | [Camera-Locked Objects](camera-locked_objects.md) |

The types of contents you can add/remove to/from scenes are described in more detail on the next page ([Scene Objects](scene_objects.md)).

`Add()` and `Remove()` are __idempotent__: Adding something that's already in the scene, or removing something that isn't, has no effect.

`scene.RemoveAll()` empties the scene. Its `includeModelInstances`, `includeLights`, and `includePrimitives` parameters can be used to keep some categories of content (e.g. `scene.RemoveAll(includeLights: false)` removes every object but leaves the lights in place).

The current contents of a scene can be enumerated with `scene.ContainedModelInstances` and `scene.ContainedLights`. Note that `ContainedModelInstances` also includes the model instances backing other kinds of object, such as quads, text, and [scene primitives](#scene-primitives).

## Backdrops

```csharp
scene.SetBackdrop(BuiltInSceneBackdrop.Starfield); // (1)!
scene.SetBackdrop(backdropTexture, backdropIntensity: 0.5f, rotation: Direction.Up % 90f); // (2)!
scene.SetBackdrop(new ColorVect(0.6f, 0.6f, 0.6f)); // (3)!
scene.SetBackdropWithoutIndirectLighting(backdropTexture); // (4)!
scene.RemoveBackdrop(); // (5)!
```

1.	Sets the backdrop to one of the built-in backdrops.

2.	Sets the backdrop to a [backdrop texture](backdrop_textures.md), at half intensity, and turned 90° around the up axis.

3.	Sets the backdrop to a flat colour, which also lights the scene in that colour.

4.	Sets the backdrop to a backdrop texture that is drawn behind the scene, but contributes no light to it.

5.	Removes the backdrop entirely, leaving the background empty and removing the ambient light the backdrop was contributing.

A scene's backdrop does two things at once:

* It fills every part of the rendered image that no object covers (e.g. the sky).
* It supplies the *ambient* (or *indirect*) light that objects pick up from their surroundings.

That second part is what makes a scene with a backdrop look markedly more natural than one lit only by its own lights. If you'd rather light a scene entirely with your own lights (or the backdrop image isn't a plausible environment), use `SetBackdropWithoutIndirectLighting()`, which draws the backdrop without it contributing any light.

The `SetBackdrop()` overloads that take a built-in backdrop or backdrop texture also accept a `rotation`, which turns the backdrop around the scene. This is how you choose where the sun (or any other landmark in the backdrop) sits relative to your scene.

???+ info "Backdrops & Compositing"
	When a scene is rendered over the top of another using `RenderCompositionType.RetainPreviousScenes`, it should usually have no backdrop; otherwise the backdrop will cover whatever was rendered before it.
	For information on this, see [Compositing](compositing.md).

### Built-in Backdrops

TinyFFR comes with backdrops that can be used without loading any image of your own, selected with the `BuiltInSceneBackdrop` enum:

<span class="def-icon">:material-card-bulleted-outline:</span> `BuiltInSceneBackdrop.Clouds`

:   A daytime sky with clouds.

<span class="def-icon">:material-card-bulleted-outline:</span> `BuiltInSceneBackdrop.Starfield`

:   A night sky filled with stars.

<span class="def-icon">:material-card-bulleted-outline:</span> `BuiltInSceneBackdrop.None`

:   No backdrop.

??? note "Built-In Backdrop Quality"
	To keep TinyFFR's package size down, the built-in backdrops are somewhat low-resolution, and are mostly intended to help you get something going quickly. You'll probably want to use your own [backdrop textures](backdrop_textures.md) eventually.

## Fog

```csharp
scene.AddFog(FogDensity.Thin); // (1)!
scene.AddFog(FogDensity.Thick, new ColorVect(0.45f, 0.35f, 0.25f)); // (2)!
scene.RemoveFog(); // (3)!
```

1.	Adds a light haze to the scene.

2.	Replaces the fog with a thick, brown fog.

3.	Removes the fog.

A scene can have at most one live fog setup; adding fog to a scene that already has some replaces it. Fog can also be configured in full by passing a `FogDescriptor` to `AddFog()`; this is explained on its own page: [Fog](fog.md).

## Scene Queries

```csharp
var queryProvider = scene.QueryProvider;

var ray = new Ray(camera.Position, camera.ViewDirection);
var hit = queryProvider.GetFirstIntersection(ray); // (1)!
if (hit is { } hitInstance) Console.WriteLine($"Looking at {hitInstance}");

using var resultsLease = factory.ResourceAllocator.BorrowSpan<ModelInstance>(64); // (2)!
var numResults = queryProvider.FindIntersections(new PositionedSphere(3f, Location.Origin), resultsLease.Span);
foreach (var instance in resultsLease.Span[..numResults]) { // (3)!
	Console.WriteLine($"{instance} is within 3m of the origin");
}
```

1.	Finds the nearest object along the ray from the camera, or `null` if the ray hits nothing.

2.	Borrows a buffer for up to 64 results (see [Large Collections of Resources](resource_groups.md#large-collections-of-resources)).

3.	Iterates every object that overlaps a sphere of radius 3 around the origin.

`scene.QueryProvider` answers questions about where the objects in a scene are:

#### Ray Queries

`GetFirstIntersection()` returns the nearest object a `Ray` or `BoundedRay` passes through (or `null` if none). `FindIntersections()` writes every object the ray passes through in to a span, nearest first.

"Nearest" is measured from the start of the ray, and an object containing the ray's start point counts as nearest of all. A `BoundedRay` only reports objects up to its end point, whereas a `Ray` continues indefinitely.

Ray queries also accept a `rayThickness`, which sweeps the ray in to a cylinder of that radius so that objects the ray passes *near* are also reported.

#### Shape Queries

`GetAnyIntersection()` returns an object overlapping a `PositionedCuboid`, `PositionedRotatedCuboid`, or `PositionedSphere` (or `null` if none). `FindIntersections()` writes every object overlapping the shape in to a span.

Shape query results are not ordered, so if several objects overlap the shape there's no meaningful "first"; `GetAnyIntersection()` is useful when you only want to know whether *anything* is in a region. 

### Scene Query Caveats

The following details apply to all scene queries:

* Objects are tested by their [bounding volumes](bounding_boxes.md), not their actual triangles. A hit means "the ray/shape intersects the box enclosing this object", which for irregularly-shaped objects is a looser answer than it may appear.
* `FindIntersections()` returns the number of results *written* to the span (which is capped at the span's length), not the total number of objects hit; so size the span generously if you need every result.
* `GetFirstIntersection()` still has to test every object in the scene to establish which is nearest, so it's no cheaper than `FindIntersections()`.
* Queries return `ModelInstance`s. For objects backed by a model instance (such as quads and text), the result is the underlying `ModelInstance`.
* [Scene primitives](#scene-primitives) are excluded from every query (so a line drawn to visualise a ray doesn't itself register as a hit along that ray).
* If an object's geometry has been modified since it was loaded (see [Dynamic Meshes & Mutable Grids](dynamic_meshes_and_mutable_grids.md)), its cached bounding box may be out of date. Pass `disallowCachedBoundingBoxes: true` to make the query slower but accurate for such objects.

??? tip "Pixel-Perfect Picking"
	If you need to know exactly which object is under a given pixel (e.g. under the mouse cursor), rather than which bounding volumes a ray passes through, use the renderer's pixel-picking instead: see [Ray Casting / Pixel-Picking](ray_casting_and_pixel-picking.md).

## Scene Primitives

```csharp
using var marker = scene.AddPrimitivePoint(new Location(1f, 0f, 0f)); // (1)!
using var arrow = scene.AddPrimitiveArrow(Location.Origin, Direction.Up); // (2)!
```

1.	Draws a point marker at (1, 0, 0).

2.	Draws an arrow from the origin pointing upwards.

Scene primitives are simple shapes/objects (points, text labels, boxes, spheres, lines, rays, planes, arrows, and grids) that can be drawn in a scene to help you see what your code is doing, or to add diagnostic details. Each `AddPrimitive[...]()` method returns a `ScenePrimitive`; dispose it to remove it from the scene.

Primitives aren't considered scene content. They're excluded from [scene queries](#scene-queries), and can be removed en masse with `scene.RemoveAll()`. They're explained in full on their own page: [Scene Primitives](scene_primitives.md).
