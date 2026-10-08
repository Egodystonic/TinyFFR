---
title: Trait Interfaces
description: The interfaces TinyFFR's math, geometry, scene object, and resource types implement, and how to use them to write generic code.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * All of TinyFFR's types implement small *trait interfaces* that each describe one capability, such as being movable (`ITranslatable<T>`) or interpolatable (`IInterpolatable<T>`). :material-arrow-right: [Writing Generic Code](#writing-generic-code)

</div>

## Writing Generic Code

Rather than one large interface per type, TinyFFR's types each implement many small *trait* interfaces, each describing a single capability. Most of the time you won't need to think about them, as you'll call methods on the types directly. They're useful when you want to write one method that works with many different types:

```csharp
static T MoveUp<T>(T value, float height) where T : ITranslatable<T> { // (1)!
	return value.MovedBy(Direction.Up * height);
}

static T Halfway<T>(T start, T end) where T : IInterpolatable<T> { // (2)!
	return T.Interpolate(start, end, 0.5f);
}

var raisedRay = MoveUp(ray, 2f); // (3)!
var raisedBox = MoveUp(box, 2f);
var purple = Halfway(ColorVect.RedOpaque, ColorVect.BlueOpaque); // (4)!
```

1.	Works with any type that can be moved by a `Vect` (`Location`, `Ray`, `Plane`, `PositionedCuboid`, `Transform`, and so on).

2.	Works with any type that can be interpolated. Many trait interfaces have `static abstract` members, which you call on the type parameter itself (here, `T.Interpolate()`).

3.	A `Ray` and a `PositionedCuboid`, each moved 2m up.

4.	A colour halfway between red and blue.

??? warning "Avoiding Boxing"
	TinyFFR's types are structs, so to avoid allocating memory (and creating garbage):

	* Use the interface as a generic *constraint* (`where T : ITranslatable<T>`), as above, rather than as a parameter type (`ITranslatable<Location> value`). Passing a struct as an interface-typed parameter *boxes* it (copies it to the heap).
	* Prefer each trait's main methods (e.g. `MovedBy()`, `ScaledBy()`, `RotatedBy()`) over its convenience members (e.g. `Plus()`, `MultipliedBy()`). Some convenience members are implemented on the interface itself (as *default interface methods*), and .NET boxes the struct to call those even through a generic constraint. This may improve in a future CLR/.NET version.

## Base Interfaces

<span class="def-icon">:material-code-block-parentheses:</span> `IMathPrimitive<T>`

:   Implemented by every math and geometry type. It combines:

	* `IToleranceEquatable<T>` (`Equals(other, tolerance)`) and `==`/`!=` (see [Equality](equality.md));
	* `IFixedLengthByteSpanSerializable<T>` (see [Serialization](serialization.md));
	* .NET's `ISpanFormattable` and `ISpanParsable<T>` (`ToString()`, `Parse()`, and `TryParse()`);
	* `IRandomizable<T>` (`T.Random()`). Most types also implement `IBoundedRandomizable<T>` (`T.Random(min, max)`).

<span class="def-icon">:material-code-block-parentheses:</span> `IVect<T>`

:   Implemented by the three-component vector types (`Location`, `Vect`, `Direction`) and `ColorVect`. See [Vector Types](vector_types.md).

<span class="def-icon">:material-code-block-parentheses:</span> `ILineLike` / `ILineLike<T>`

:   Implemented by `Line`, `Ray`, and `BoundedRay`, and gives them their shared API (`StartPoint`, `Direction`, `Length`, closest points, intersections, and so on). See [Lines](lines.md).

<span class="def-icon">:material-code-block-parentheses:</span> `IShape<T>` / `IConvexShape<T>` / `ISphere<T>` / `ICuboid<T>`

:   Implemented by the shapes. `IConvexShape<T>` provides the shared measuring and intersecting API; `ISphere<T>` and `ICuboid<T>` are implemented by both the plain and [positioned](shapes.md#positioned-shapes) versions of each shape. See [Shapes](shapes.md).

<span class="def-icon">:material-code-block-parentheses:</span> `IPhysicalValidityDeterminable`

:   Provides `IsPhysicallyValid`, which checks that a value is finite and otherwise sensible (e.g. a `Direction` is unit-length, or a shape's dimensions aren't negative).

<span class="def-icon">:material-code-block-parentheses:</span> `IDescriptiveStringProvider`

:   Provides `ToStringDescriptive()`, a more detailed string than `ToString()` that's useful for debugging (e.g. a `Rotation`'s axis is also described as its nearest orientation, such as `(Up)`).

## Arithmetic Traits

These describe a type's arithmetic (and are defined in `ArithmeticTraits.cs`):

| Interface | Provides | Implemented by (e.g.) |
| :-- | :-- | :-- |
| `IInterpolatable<T>` | `T.Interpolate(start, end, distance)` and `Clamp(min, max)` (see [Interpolation](interpolation_algorithms.md)) | Every math type |
| `IPrecomputationInterpolatable<T, TPre>` | Faster repeated interpolation between the same two values | `Direction`, `Rotation`, `Line`, `Ray`, `Plane` |
| `IOrdinal<T>` | Values have a natural order, and `T.GetInterpolationDistance()` finds how far a value lies between two others | `Angle`, `Real` |
| `IAlgebraicGroup<T>` | `+`, `-`, unary `-`, and a zero value | `Vect`, `Angle`, `Rotation`, `XYPair<T>`, `Real` |
| `IAlgebraicRing<T>` | `IAlgebraicGroup<T>` plus `*` and `/` by another value of the same type | `Vect`, `XYPair<T>`, `Real` |
| `IInvertible<T>` | `Inverted` (and unary `-`), e.g. a reversed direction or rotation | `Vect`, `Direction`, `Angle`, `Rotation`, line-likes, `Plane` |
| `IMultiplicativeInvertible<T>` | `Reciprocal` (`1 / value`, or `null` if a component is zero) | `Vect`, `XYPair<T>`, `Real` |
| `INormalizable<T>` | `Normalized`, a canonical form of the value | `Angle`, `Rotation` |
| `IAbsolutizable<T>` | `Absolute`, with signs removed | `Vect`, `Angle`, `XYPair<T>` |
| `IInnerProductSpace<T>` | `Dot(other)` | `Vect`, `Direction`, `XYPair<T>` |
| `IVectorProductSpace<T>` | `Cross(other)` | `Vect`, `Direction` |
| `ITransitionRepresentable<T, TResult>` | `>>`, giving the change from one value to another | `Location` (giving a `Vect`), `Direction` (giving a `Rotation`) |

`IAdditive<T, TOther, TResult>` and `IMultiplicative<T, TOther, TResult>` describe the `+`/`-` and `*`/`/` operators (with `Plus()`/`Minus()` and `MultipliedBy()`/`DividedBy()` equivalents) between a type and another type, and `IBlendable<T>` is a simpler version of `IInterpolatable<T>` that's also implemented by [texel types](creating_textures.md).

## Geometric Traits

These describe what can be done to (or with) a geometric value, and are defined in `GeometricTraits.cs`.

### Transformation

| Interface | Provides |
| :-- | :-- |
| `IScalable<T>` | `ScaledBy(scalar)` |
| `IIndependentAxisScalable<T>` | `ScaledBy(vect)`, scaling each axis separately |
| `IRotatable<T>` | `RotatedBy(rotation)` |
| `ITranslatable<T>` | `MovedBy(vect)` |
| `ITransformable<T>` | `TransformedBy(transform)` and `TransformedByInverseOf(transform)` (and all three of the above) |
| `IPointScalable<T>`, `IPointRotatable<T>`, `IPointTransformable<T>` | The same, around a given pivot point (e.g. `RotatedBy(rotation, pivot)`, `RotatedAroundOriginBy(rotation)`) |
| `ILengthAdjustable<T>` | `WithLength()`, `WithLengthIncreasedBy()`, `WithMaxLength()`, etc. |

Each has a 2D equivalent ending in `2D` (e.g. `ITranslatable2D<T>`, `ITransformable2D<T>`), using `XYPair<float>`, `Angle`, and `Transform2D`.

These interfaces also define the matching operators (`* scalar`, `* rotation`, `+ vect`, and `* transform`), which always work in generic code. However, some types only offer the methods directly, where an operator would be ambiguous (e.g. `boundedRay * 2f` doesn't compile, because it isn't clear which point the ray should be scaled around; use `ScaledFromStartBy()` or one of its siblings instead).

The following table shows which of these the main math types implement:

| Type | Scalable | Rotatable | Translatable | Transformable | Around a pivot | Length-adjustable |
| :-- | :-: | :-: | :-: | :-: | :-: | :-: |
| `Location` | ✓ | ✓ | ✓ | ✓ | ✓ | |
| `Vect` | ✓ | ✓ | | | | ✓ |
| `Direction` | | ✓ | | | | |
| `Angle`, `Rotation` | ✓ | | | | | |
| `Transform` | ✓ | ✓ | ✓ | ✓ | | |
| `XYPair<T>` | ✓ | ✓ (2D) | ✓ (2D) | ✓ (2D) | ✓ (2D) | ✓ |
| `Transform2D` | ✓ | ✓ (2D) | ✓ (2D) | ✓ (2D) | | |
| `ColorVect` | ✓ | | | | | |
| `Line`, `Ray`, `Plane` | | ✓ | ✓ | | ✓ | |
| `BoundedRay` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Sphere`, `Cuboid` | ✓ | | | | | |
| `PositionedSphere`, `PositionedCuboid` | ✓ | | ✓ | | | |
| `PositionedRotatedCuboid` | ✓ | ✓ | ✓ | | | |

### Measurement & Relationships

These take another value (of type `TOther`) to measure against or relate to. Most types implement them for several different `TOther`s (e.g. a `Ray` can measure its distance from a `Location`, another line-like, a `Plane`, or a shape).

| Interface | Provides |
| :-- | :-- |
| `IAngleMeasurable<TOther>` | `AngleTo(other)` (and the `^` operator) |
| `IDistanceMeasurable<TOther>` / `ISignedDistanceMeasurable<TOther>` | `DistanceFrom(other)`, `DistanceSquaredFrom(other)` / `SignedDistanceFrom(other)` |
| `IContainer<TOther>` / `IContainable<TOther>` | `Contains(other)` / `IsContainedWithin(other)` |
| `IClosestEndogenousPointDiscoverable<TOther>` | `PointClosestTo(other)`: the point on *this* value closest to `other` |
| `IClosestExogenousPointDiscoverable<TOther>` | `ClosestPointOn(other)`: the point on *`other`* closest to this value |
| `IIntersectable<TOther>` / `IIntersectionDeterminable<TOther, TIntersection>` | `IsIntersectedBy(other)` / `IntersectionWith(other)` |
| `IRelatable<TOther, TRelationship>` | `RelationshipTo(other)` (e.g. which side of a plane a shape is on) |
| `IParallelDiscernible<TOther>` / `IOrthogonalDiscernible<TOther>` | `IsParallelTo()`, `IsApproximatelyParallelTo()` / `IsOrthogonalTo()`, `IsApproximatelyOrthogonalTo()` |
| `IParallelizable<T, TOther>` / `IOrthogonalizable<T, TOther>` | `ParallelizedWith(other)` / `OrthogonalizedAgainst(other)` |
| `IProjectable<T, TOther>` | `ProjectedOnTo(other)` |
| `IReflectable<TOther, TReflection>` | `ReflectedBy(other)` and `IncidentAngleWith(other)` |

Several of these come in pairs, one being the "mirror image" of the other, named with a `Target` suffix: For example, `Vect` implements `IProjectable<Vect, Plane>` (`vect.ProjectedOnTo(plane)`), and `Plane` implements `IProjectionTarget<Vect>` (`plane.ProjectionOf(vect)`). The same applies to reflection (`IReflectionTarget`), parallelization, and orthogonalization.

Interfaces beginning with `ILine` (e.g. `ILineDistanceMeasurable`) are shorthands for implementing a trait against all three line-like types, and those beginning with `IConvexShape` (e.g. `IConvexShapeIntersectable`) against every convex shape. Most methods that take a `Fast` prefix (e.g. `FastIntersectionWith()`) are also defined by these interfaces (see [API Naming](conventions.md#api-naming)).

## Object & Resource Traits

### Scene Objects

Every object that can be placed in a scene implements `ISceneObject`, along with interfaces describing what it supports. These match the columns of the [`SceneObject` table](scene_objects.md#sceneobject):

| Interface | Provides | `SceneObject` column |
| :-- | :-- | :-- |
| `IMovableSceneObject` / `IPositionedSceneObject` | `MoveBy()` / `Position` | Positioned |
| `IReorientableSceneObject` / `IOrientedSceneObject` | `RotateBy()` / `Rotation` | Oriented |
| `IRescalableSceneObject` / `IScaledSceneObject` | `ScaleBy()`, `AdjustScaleBy()` / `Scaling` | Scaled |
| `ISizableSceneObject` | `SetSize()` | Sizable |
| `ITransformedSceneObject` | All of the above, plus `Transform` and `RotateBy(rotation, pivotPoint)` | |
| `IMaterialReceivingSceneObject` / `IMaterialUsingSceneObject` | `SetMaterial()` and the default material settings / `Material` | Material |
| `IColoredSceneObject` | `ColorHue`, `ColorSaturation`, `ColorLightness`, and adjustments to each | Colour |

The first interface in each pair provides a relative change (e.g. `MoveBy()`), and the second adds reading and setting the value directly (e.g. `Position`). `Camera` also implements `IPositionedSceneObject` and `IOrientedSceneObject`.

[Canvas objects](canvas_scenes.md#canvas-objects) implement the 2D equivalents (`IPositioned2DSceneObject`, `ITransformed2DSceneObject`, and so on, via `ICanvasObject`), and the [lights](point_lights.md) implement `ILight` (`Color`, `Brightness`, `CastsShadows`, and so on), so you can write code that works with any light.

```csharp
static void ResetTransform<T>(T obj) where T : ITransformedSceneObject { // (1)!
	obj.Transform = Transform.None;
}

static void Dim<T>(T light) where T : ILight { // (2)!
	light.ScaleBrightnessBy(0.5f);
}
```

1.	Works with model instances, groups, quads, mutable grids, and text instances.

2.	Works with point lights, spot lights, and directional lights.

### Other Traits

<span class="def-icon">:material-code-block-parentheses:</span> `IDisposableResource<T>` / `IResource<T>`

:   Implemented by every [resource](resource_dependencies.md) type (meshes, materials, textures, model instances, scenes, and so on), providing `Dispose()`, `IsDisposed`, and name access (`GetNameAsNewStringObject()`, `CopyName()`).

<span class="def-icon">:material-code-block-parentheses:</span> `IRenderTarget`

:   Implemented by `Window` and `RenderOutputBuffer`: anything a renderer can draw in to.

<span class="def-icon">:material-code-block-parentheses:</span> `ITexel<T>` (and its variants)

:   Implemented by the texel types (e.g. `TexelRgb24`, `TexelRgba32`) used to create and read [textures](creating_textures.md#creating-textures-from-texel-data).

<span class="def-icon">:material-code-block-parentheses:</span> `ITimeKeyedItem`

:   Implemented by keyframe types (e.g. animation and camera keyframes) that sit at a point along a timeline.
