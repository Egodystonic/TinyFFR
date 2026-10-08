---
title: Scene Objects
description: Information on model instances, model instance groups, and the other kinds of object that can be placed in a scene; and on the SceneObject type that wraps them all.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Scenes can be filled with various types of object. The most common is a `ModelInstance`, a single occurrence of a mesh + material placed in the world. :material-arrow-right: [Model Instances](#model-instances)
    * Quads, mutable grids, text, and camera-locked objects are specialized kinds of model instance. :material-arrow-right: [Other Object Types](#other-object-types)
    * It's possible to wrap any of these in a "`SceneObject`"; a unifying lightweight wrapper that can represent any of the above as well as lights or cameras. :material-arrow-right: [SceneObject](#sceneobject)

</div>

## Building Objects

```csharp
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material); // (1)!
instance.Position = new Location(0f, 1f, 3f);
instance.RotateBy(Direction.Up % 45f);
instance.SetScaling(2f);

scene.Add(instance); // (2)!
```

1.	Creates a model instance: One occurrence of `mesh`, rendered with `material`.

2.	Adds the instance to a scene, so that it's rendered.

Meshes and materials describe the *shape* and *surface* of something, but they don't exist anywhere in the world by themselves. To actually place something in a scene, you create an object from them with the `factory.ObjectBuilder`.

This page explains each kind of object that can be placed in a scene, and how to build them.

## Model Instances

A `ModelInstance` is a single instance of a *model*(1) in the world. Each instance has its own position, scaling, and rotation.
{ .annotate }

1.	A `Model` is one `Mesh` and one `Material`.

Model instances are created with `factory.ObjectBuilder.CreateModelInstance()`:

```csharp
using var fromModel = factory.ObjectBuilder.CreateModelInstance(model); // (1)!
using var fromMesh = factory.ObjectBuilder.CreateModelInstance(mesh, material, initialPosition: new Location(1f, 0f, 0f)); // (2)!
using var withDefaultMaterial = factory.ObjectBuilder.CreateModelInstance(mesh); // (3)!
```

1.	Creates an instance from a `Model` (see [Bundled Assets](bundled_assets.md)).

2.	Creates an instance from a mesh and material, starting at (1, 0, 0). This is identical to passing a `Model` with the same mesh and material.

	Every overload shown in this example also accepts an initial rotation, scaling, and name; or alternatively a `Transform`.

3.	Creating an instance without specifying a material uses [the default material](the_default_material.md).

Many instances can share the same mesh and material; each instance has its own transform. The instance [depends on](resource_dependencies.md) its mesh and material, so they can't be disposed while it's still alive.

### Transform (Position, Rotation, Scaling)

Every model instance has a `Position`, `Rotation`, and `Scaling`; these three properties are sometimes referred together as an instance's [`Transform`](transform.md). Each can be read and set directly or manipulated with various helper methods:

```csharp
instance.Transform = Transform.None; // (6)!
instance.Position = new Location(0f, 2f, 0f);
instance.Rotation = Direction.Left % 30f;
instance.Scaling = new Vect(1f, 2f, 1f);

instance.MoveBy(new Vect(0f, 0f, 1f)); // (1)!
instance.RotateBy(Direction.Up % 10f); // (2)!
instance.RotateBy(Direction.Up % 10f, pivotPoint: Location.Origin); // (3)!
instance.ScaleBy(2f); // (4)!
instance.AdjustScaleBy(0.5f); // (5)!
```

1.	Moves the instance 1 unit forward from wherever it currently is.

2.	Rotates the instance by a further 10° around the up axis.

3.	Rotates the instance by 10° around the up axis, pivoting around the origin rather than the instance's own position (so its position changes too).

4.	*Multiplies* the instance's scaling by 2 (so a scaling of `(1, 2, 1)` becomes `(2, 4, 2)`).

5.	*Adds* 0.5 to each component of the instance's scaling (so a scaling of `(2, 4, 2)` becomes `(2.5, 4.5, 2.5)`).

6.	Sets the instance's position/rotation/scaling to their defaults (position at `(0, 0, 0)`, rotation of `None`, scaling of `(1, 1, 1)`).

The same transform members are available on every kind of object on this page that can be positioned, rotated, or scaled.

### Size

`Scaling` is relative to the size of the mesh, so a scaling of `(2, 2, 2)` makes an object twice as big as its mesh, whatever size that happens to be. To make an object a specific size instead, use `SetSize()`:

```csharp
instance.SetSize(new Vect(1f, 2f, 1f)); // (1)!

var treeScaling = treeMesh.CalculateScalingForSize(new Vect(3f, 10f, 3f)); // (2)!
foreach (var tree in trees) {
	tree.Scaling = treeScaling;
}
```

1.	Sets the instance's `Scaling` so that it measures 1m wide, 2m tall, and 1m deep (along its own axes, i.e. before its rotation is applied).

2.	Calculates the scaling that sizes instances of `treeMesh` at 3m × 10m × 3m once, so it can be assigned to many instances without recalculating it for each.

`SetSize()` works out the required scaling from the instance's bounding box, so the following points apply:

* The result is only as accurate as the bounding box is tight. For most meshes the box fits exactly, but a [skeletal mesh](skeletal_meshes.md)'s box covers every pose of its animations, which can make it larger than the mesh appears at rest.
* Bounding boxes are enlarged slightly when a mesh is created (by `MeshCreationConfig.BoundingBoxAdditionalMargin`). `SetSize()` removes this margin before calculating the scaling; if you created the mesh with a non-default margin, pass the same value as `SetSize()`'s `boundingBoxMargin` argument.
* An axis along which the mesh has no thickness (such as the depth of a flat quad) can't be resized, so its scaling is set to `1`.
* The new scaling replaces the old one; calling `SetSize()` repeatedly with the same size always gives the same result.

### Mesh & Material

An instance's mesh and material can be changed at any time with the `Mesh` and `Material` properties (or `SetMesh()` / `SetMaterial()`). You can also set a default shading style by using `SetDefaultMaterialBaseColor()` and `SetDefaultMaterialShadingStyle()` (see [The Default Material](the_default_material.md) for more information).

### Draw Order

Objects are normally drawn in an order chosen by the renderer. Setting an instance's `DrawOrderDeferralAmount` (between `0` and `ModelInstance.MaxDrawOrderDeferralAmount`) forces it to be drawn after other objects; higher values are drawn later. This is useful where two surfaces occupy the same space and the wrong one is drawn on top, or where a transparent object needs to be composited over something specific. Set it back to `null` to return the instance to its natural place in the drawing order.

### More on Model Instances

Model instances have a number of further capabilities, explained on their own pages:

* `instance.MaterialEffects` adjusts per-instance effects (such as texture transforms and blending) without affecting other instances sharing the same material. :material-arrow-right: [Material Effects](material_effects.md)

* `instance.GetAnimationPlayer()` and its overloads play the animations of a [skeletal mesh](skeletal_meshes.md) on an instance. :material-arrow-right: [Playing Skeletal Animations](playing_skeletal_animations.md)

* `instance.BorrowVerticesSpan()` alters an instance's vertices without affecting other instances sharing the same mesh (if the mesh was created with `AllowsPerInstanceVertexMutation`). :material-arrow-right: [Dynamic Meshes & Mutable Grids](dynamic_meshes_and_mutable_grids.md)

* `instance.GetWorldSpaceBoundingBox()` and similar methods describe the volume an instance occupies in the world. :material-arrow-right: [Bounding Boxes](bounding_boxes.md)

* `instance.SetKeyedMaterialColor()` sets the colours an instance is painted in when using a colour-keyed material. :material-arrow-right: [Color Keyed Materials](color_keyed_materials.md)

## Model Instance Groups

A `ModelInstanceGroup` is a set of model instances that can be moved, rotated, and scaled together as though they were one object. They're most commonly created from a [bundled asset](bundled_assets.md), whose models are usually intended to be placed together:

```csharp
using var group = factory.ObjectBuilder.CreateModelInstances(bundle, initialPosition: new Location(0f, 0f, 5f)); // (1)!
scene.Add(group); // (2)!

group.RotateBy(Direction.Up % 90f); // (3)!
foreach (var instance in group) { // (4)!
	Console.WriteLine(instance);
}
```

1.	Creates one instance for every model in the bundle, all starting at (0, 0, 5).

2.	Adds every instance in the group to the scene.

3.	Rotates the whole group by 90° around the up axis. Each instance keeps its position and orientation relative to the others.

4.	Iterates the instances in the group. They can also be accessed with `group.Instances`, `group.Count`, and `group[index]`.

Groups can also be created from a span of `Model`s or `Mesh`es with `CreateModelInstances()`, or from instances you've already created with `factory.ObjectBuilder.GroupModelInstances()`. The instances in a group keep working individually; the group is just an additional handle that transforms them together.

A group has the same transform members as a model instance, plus a few others:

* `group.SetMaterial()` sets the material of every instance in the group at once.
* `group.SetSize()` scales the whole group so that it measures the given size, according to the combined bounding box of every instance in it (see [Size](#size)). To size many groups created from the same bundle, calculate the scaling once with `bundle.CalculateScalingForSize()` and assign it to each group's `Scaling`.
* Groups created from a bundled asset with animations share a single animation table, so the whole group can be animated together with `group.GetAnimationPlayer()` (see [Playing Skeletal Animations](playing_skeletal_animations.md)).
* Disposing a group disposes the instances in it. When grouping existing instances with `GroupModelInstances()`, pass `disposingGroupDisposesInstances: false` if you'd rather dispose them yourself.

## Other Object Types

The following kinds of object are specialized views of a `ModelInstance`, exposed via its `UnderlyingModelInstance` property (or `UnderlyingQuadInstance` / `UnderlyingTextInstance` for the camera-locked types, which themselves have an `UnderlyingModelInstance`). Disposing one disposes its underlying instance.

### Quads

```csharp
using var quadMesh = factory.MeshBuilder.CreateQuad();
using var quad = factory.ObjectBuilder.CreateQuadInstance(quadMesh, material, position: new Location(0f, 1f, 0f), size: new XYPair<float>(2f, 1f));
```

A `QuadInstance` is a single flat rectangle placed in the world, created from a `QuadMesh`. Quads are often used to display 2D information such as signs or labels etc, and/or sometimes for various performance tricks (such as "fake" foliage or other effects). Unlike a model instance, a quad's scaling is two-dimensional (an `XYPair<float>`). 

Quads are explained in full on their own page: [Quads](quads.md).

### Mutable Grids

```csharp
using var gridMesh = factory.MeshBuilder.CreateMutableGrid(new XYPair<int>(32, 32));
using var grid = factory.ObjectBuilder.CreateMutableGridInstance(gridMesh, material);
```

A `MutableGridInstance` is a grid surface (created from a `MutableGridMesh`) whose vertices can be easily displaced and manipulated, via `grid.BorrowVerticesSpan()`. Mutable grids are useful for graphing visualiations or simulating non-rigid surfaces such as water or cloth. 

They're also explained on their own page: [Dynamic Meshes & Mutable Grids](dynamic_meshes_and_mutable_grids.md).

### Text

```csharp
using var pen = font.CreatePen(BuiltInFontPenStyle.Default);
using var str = font.CreateString("Hello, world!");
using var text = factory.ObjectBuilder.CreateTextInstance(pen, str, position: new Location(0f, 2f, 0f));
```

A `TextInstance` is a string of text placed in the world, drawn with a `FontPen` and `FontString` created from a [`Font`](loading_fonts.md). A text instance's `Pen` and `String` can be changed at any time. Like quads, its scaling is two-dimensional. 

Pens, strings, and text instances are explained in full on their own pages: [Text Instances](text_instances.md).

### Camera-Locked Objects

```csharp
using var label = factory.ObjectBuilder.CreateCameraLockedTextInstance(pen, str, position: new Location(0f, 3f, 0f));
using var icon = factory.ObjectBuilder.CreateCameraLockedQuadInstance(quadMesh, material, position: new Location(1f, 3f, 0f));
```

`CameraLockedQuadInstance` and `CameraLockedTextInstance` are quads and text whose rotation is always *locked* so they face the camera (also sometimes called *billboarding*).

Because they always face the camera, camera-locked objects can be positioned and scaled but not rotated. How they turn and how they're sized is set by four properties chosen when they're created:

<span class="def-icon">:material-card-bulleted-outline:</span> `LockedUprightDirection`

:   Which direction the object keeps as its "up" as it turns (or `Direction.None` to let it turn freely on every axis).

<span class="def-icon">:material-card-bulleted-outline:</span> `PositionAnchor`

:   Which point of the object (e.g. its bottom-left corner) is placed at its position (or its centre if `Orientation2D.None`).

<span class="def-icon">:material-card-bulleted-outline:</span> `ScalingMode`

:   Whether the object is sized in world units (shrinking with distance like any other object) or as a fraction of the screen (staying the same size however far away it is).

<span class="def-icon">:material-card-bulleted-outline:</span> `LockStyle`

:   Whether the object turns to point at the camera's position, or turns to sit square-on to the screen.

These are explained in full on their own page: [Camera-Locked Objects](camera-locked_objects.md).

???+ info "Add The Camera-Locked Object Itself"
	Only the camera-locked object turns to face the camera. If you add its *underlying* quad or text instance to a scene instead (e.g. `scene.Add(label.UnderlyingTextInstance)`), it's treated as an ordinary quad or text instance, and won't follow the camera.

### Lights & Cameras

```csharp
using var pointLight = factory.LightBuilder.CreatePointLight(new Location(0f, 3f, 0f));
using var spotLight = factory.LightBuilder.CreateSpotLight(new Location(0f, 3f, 0f), Direction.Down);
using var sun = factory.LightBuilder.CreateDirectionalLight(Direction.Down);
scene.Add(pointLight); // (1)!

using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 1f, -5f)); // (2)!
```

1.	Like objects, lights only illuminate a scene once they've been added to it.

2.	Cameras are never added to a scene; instead, a camera is given to a [`Renderer`](using_renderers.md) along with the scene it should look at.

`Light`s are also added to a scene but are not backed by a `ModelInstance`. Light types are described in more detail in the following pages ([Point Lights](point_lights.md), [Spot Lights](spot_lights.md), [Directional Lights](directional_lights.md)).

`Camera`s are __not__ added to a scene (rather, they are used to *capture* it). For more info see [Camera Settings](camera_settings.md).

???+ warning "One Directional Light Per Scene"
	A scene can contain at most one [directional light](directional_lights.md). If a scene already contains a directional light, adding another has no effect (the second light is not added). Remove the first directional light before adding a different one.

## SceneObject

`SceneObject` is a lightweight wrapper that can represent any of the types on this page. Every one of them converts implicitly to a `SceneObject`, which makes it possible to write code that works with any kind of object (alternatively, see [Trait Interfaces](trait_interfaces.md#scene-objects) for writing generic code over the object types themselves):

```csharp
static void Lift(SceneObject obj, float height) { // (1)!
	obj.MoveBy(Direction.Up * height);
}

Lift(instance, 1f); // (2)!
Lift(group, 1f);
Lift(pointLight, 1f);
Lift(label, 1f);

SceneObject sceneObject = quad;
scene.Add(sceneObject); // (3)!
if (sceneObject.Type == SceneObjectType.QuadInstance) {
	var originalQuad = (QuadInstance) sceneObject; // (4)!
}
```

1.	A method that moves any kind of object up by the given height.

2.	Model instances, groups, lights, text, and every other type on this page convert implicitly to `SceneObject`.

3.	`Scene.Add()` and `Scene.Remove()` accept a `SceneObject`, and behave exactly as though the matching typed overload had been called.

4.	`Type` tells you what's wrapped, and an explicit cast gets the original object back. (Casting to the wrong type throws an `InvalidCastException`.)

A `SceneObject` wraps one of the following types, indicated by its `Type` property (a `SceneObjectType`):

| Type                         | Positioned | Oriented | Scaled | Sizable | Material | Colour |
| :--------------------------- | :--------: | :------: | :----: | :-----: | :------: | :----: |
| `ModelInstance`              | ✓ | ✓ | ✓ | ✓ | ✓ |   |
| `ModelInstanceGroup`         | ✓ | ✓ | ✓ | ✓ | ✓ |   |
| `QuadInstance`               | ✓ | ✓ | ✓ |   | ✓ |   |
| `MutableGridInstance`        | ✓ | ✓ | ✓ |   | ✓ |   |
| `TextInstance`               | ✓ | ✓ | ✓ |   |   |   |
| `CameraLockedQuadInstance`   | ✓ |   | ✓ |   | ✓ |   |
| `CameraLockedTextInstance`   | ✓ |   | ✓ |   |   |   |
| `PointLight`                 | ✓ |   |   |   |   | ✓ |
| `SpotLight`                  | ✓ | ✓ |   |   |   | ✓ |
| `DirectionalLight`           |   | ✓ |   |   |   | ✓ |
| `Camera`                     | ✓ | ✓ |   |   |   |   |
| `None` (a `default` `SceneObject`) |   |   |   |   |   |   |

`SceneObject` exposes the members of every column (e.g. `Position`, `Rotation`, `Scaling`, `SetSize()`, `SetMaterial()`, `ColorHue`), but members that don't apply to the wrapped type simply do nothing. 

For example, setting the "material" of a light is ignored, and reading an unsupported property returns a neutral default, so reading the "rotation" of a point light returns `Rotation.None`. To check whether a member will have an effect, use the extension methods on `SceneObjectType`: `IsPositioned()`, `IsOriented()`, `IsScaled()`, `IsTransformed()` (all three), `IsSizable()`, `IsMaterialReceiving()`, and `IsColored()` (e.g. `var takesMaterial = sceneObj.Type.IsMaterialReceiving()`).

The following details also apply:

* A `SceneObject` doesn't own the object it wraps, and doesn't need to be disposed. It's cheap to create and pass around.
* You *can* use `sceneObject.DisposeUnderlyingObject()` to dispose the wrapped object. Once the wrapped object has been disposed (either via `DisposeUnderlyingObject()` or via its own `Dispose()` method), the `SceneObject` must no longer be used.
* `IsStoredAsModelInstance()` indicates that the wrapped type is backed by a single model instance, in which case the `SceneObject` can also be cast to a plain `ModelInstance` (useful for checking the results of [scene queries](scenes.md#scene-queries)).
* Adding a `SceneObject` wrapping a `Camera` to a scene has no effect (cameras are never part of a scene).
* A `default` `SceneObject` wraps nothing; its `Type` is `SceneObjectType.None`. Every member does nothing (and reads return neutral defaults), and adding it to or removing it from a scene has no effect. This makes it safe to keep a `SceneObject` field unset until you have something to put in it.
