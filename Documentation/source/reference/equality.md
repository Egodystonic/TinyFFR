---
title: Equality
description: How TinyFFR's math types compare for equality, including floating-point tolerances, equivalence methods, and approximate geometric tests.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * `==` and `Equals()` compare values *exactly*, so tiny floating-point errors make otherwise-equal values unequal. `Equals(other, tolerance)` allows for small differences. :material-arrow-right: [Exact Equality](#exact-equality), [Tolerance Equality](#tolerance-equality)
    * `IsEquivalent...To()` methods compare the *meaning* of an object instead of just its numerical value. :material-arrow-right: [Equivalence](#equivalence)
    * Methods such as `IsWithinDistanceOf()` and `IsApproximatelyParallelTo()` test geometric closeness. :material-arrow-right: [Approximate Geometric Tests](#approximate-geometric-tests)

</div>

## Exact Equality

TinyFFR's math types store their values as `float`s, and `==` (or `Equals(other)`) compares those numbers exactly. That's usually not what you want when values have been calculated, because floating-point math is rarely perfectly precise:

```csharp
var turned = Direction.Forward * (90f % Direction.Up); // (1)!
var isLeft = turned == Direction.Left; // (2)!
var isNearlyLeft = turned.Equals(Direction.Left, 0.0001f); // (3)!
```

1.	Rotating `Forward` by 90° around `Up` gives `Left`... almost. The result is actually `(1, 0, 0.0000000596)`.

2.	`false`, because the `Z` component isn't exactly `0`.

3.	`true`: The components are all within 0.0001 of `Left`'s.

Use `==` when values should be *identical* (e.g. checking whether a value has changed since you last stored it, or comparing against a value you assigned directly), and a tolerance when they've been calculated.

??? info "Zero & NaN"
	Exact comparisons follow .NET's `float.Equals()` rules: `-0` and `0` are equal, and (unlike the `==` operator on two `float`s) `NaN` is considered equal to `NaN`. Use `IsPhysicallyValid` to check whether a value contains `NaN` or infinite components (and, for a `Direction`, whether it's unit-length).

## Tolerance Equality

Every math type implements `IToleranceEquatable<T>`, which adds an `Equals(other, tolerance)` method. It compares each of the value's components separately, and returns `true` if every one is within `tolerance` of the other value's:

```csharp
var closeEnough = location1.Equals(location2, 0.001f); // (1)!
var sameAngle = angle1.Equals(angle2, 0.1f); // (2)!
var sameRotation = rotation1.Equals(rotation2, 0.01f); // (3)!
```

1.	`true` if the X, Y, and Z coordinates each differ by 1mm or less.

2.	For an `Angle`, the tolerance is in degrees.

3.	For types made of several kinds of component, the tolerance applies to each in its own units. Here, the rotations' angles must be within 0.01° and each component of their axes within 0.01.

??? warning "Tolerances Aren't Distances"
	Because each component is compared separately, `Equals(other, tolerance)` isn't the same as "within `tolerance` of each other": Two locations 1m apart along every axis (about 1.73m apart in total) are equal with a tolerance of `1f`. To compare actual distances or angles, see [Approximate Geometric Tests](#approximate-geometric-tests).

??? warning "Tolerance Equality Isn't Transitive"
	If `a` is equal to `b` within a tolerance, and `b` to `c`, `a` isn't necessarily equal to `c`. Values that are equal within a tolerance can also have different hash codes (`GetHashCode()` is always exact), so don't rely on tolerances when using math types as dictionary keys or in sets.

### Choosing a Tolerance

* For values that should be identical but have been through some calculations (e.g. a direction you've rotated and rotated back), a small tolerance such as `0.0001f` is usually enough.
* `float`s have about seven significant digits of precision, so large values carry larger errors. A location 10km from the origin can only be stored to within about 1mm. Use larger tolerances when working far from `0f`.
* When what you really care about is a distance or an angle, use an [approximate geometric test](#approximate-geometric-tests) instead.

## Numerical vs Semantic Equality

Most types are compared purely by their numbers. A few values, however, can be stored in more than one way while meaning exactly the same thing; and whether two of those count as equal depends on the type.

| Type | `==` compares | Notes |
| :-- | :-- | :-- |
| `Location`, `Vect`, `XYPair<T>`, `ColorVect`, `Real` | Each component | |
| `Direction` | Each component | Directions are always unit-length, so `new Direction(1, 1, 0) == new Direction(2, 2, 0)`. |
| `Angle` | The angle | 0° and 360° are *not* equal. See `IsEquivalentWithinCircleTo()`. |
| `Rotation` | The angle and axis | 90° around `Left` and -90° around `Right` are *not* equal (nor are 0° around different axes, or 0° around any axis and `Rotation.None`). See `IsEquivalentForAllDirectionsTo()`. |
| `Transform` / `Transform2D` | The scaling, rotation, and translation | If either transform is [represented by a matrix](transform.md#matrices), the matrices are compared instead. |
| `Line` | **The line itself** | Two lines are equal if they lie along the same infinite line, even if they were created from different points or with opposite directions. |
| `Ray` | Start point and direction | |
| `BoundedRay` | Start and end points | A bounded ray and its `Flipped` version are *not* equal. See `IsEquivalentDisregardingDirection()`. |
| `Plane` | Normal and position | A plane and its `Flipped` version (the same surface, facing the other way) are *not* equal. |
| `Sphere`, `Cuboid`, and the positioned shapes | Their dimensions (and position and rotation) | |

## Equivalence

`IsEquivalent...To()` methods check whether two values *mean* the same thing, even when they're stored differently:

<span class="def-icon">:material-code-block-parentheses:</span> `Angle.IsEquivalentWithinCircleTo(other)`

:   Whether two angles point the same way around a circle (e.g. 0° and 360°, or -90° and 270°).

<span class="def-icon">:material-code-block-parentheses:</span> `Rotation.IsEquivalentForAllDirectionsTo(other)`

:   Whether two rotations have the same effect on everything (e.g. 90° around `Left` and -90° around `Right`).

<span class="def-icon">:material-code-block-parentheses:</span> `Rotation.IsEquivalentForSingleDirectionTo(other, direction)`

:   Whether two rotations have the same effect on one particular direction (e.g. *any* two rotations around `Up` leave `Up` itself unchanged).

<span class="def-icon">:material-code-block-parentheses:</span> `BoundedRay.IsEquivalentDisregardingDirection(other)`

:   Whether two bounded rays cover the same segment, in either direction.

<span class="def-icon">:material-code-block-parentheses:</span> `SphericalTranslation.IsEquivalentWithinSphereTo(other)`

:   Whether two spherical translations end up in the same place, accounting for angles that wrap around.

Each also has an overload taking a `tolerance`, and the same floating-point caveats apply as for `==`. For example, `(360f % Direction.Up).IsEquivalentForAllDirectionsTo(Rotation.None)` is `false` (because the calculation behind it leaves an error of around 0.0000001), whereas `(360f % Direction.Up).IsEquivalentForAllDirectionsTo(Rotation.None, 0.0001f)` is `true`.

## Approximate Geometric Tests

These methods test how close two values are in a geometric sense, rather than comparing their components:

<span class="def-icon">:material-code-block-parentheses:</span> `location.IsWithinDistanceOf(other, distance)`

:   Whether two locations are no more than `distance` apart.

<span class="def-icon">:material-code-block-parentheses:</span> `direction.IsWithinAngleTo(other, angle)`

:   Whether the angle between two directions is no more than `angle`.

<span class="def-icon">:material-code-block-parentheses:</span> `IsApproximatelyParallelTo(other)` / `IsApproximatelyOrthogonalTo(other)`

:   Whether two directions, [lines](lines.md#measuring-intersecting), or [planes](shapes.md#plane) are parallel or at right-angles, to within 0.1° by default (or a `tolerance` you pass). The non-approximate versions, `IsParallelTo()` and `IsOrthogonalTo()`, are near-exact checks (allowing only for the tiniest floating-point error): They're faster, and tell you in advance whether methods such as `ParallelizedWith()` will return `null`, but usually reject values that are "parallel enough" for practical purposes.

<span class="def-icon">:material-code-block-parentheses:</span> `IsApproximatelyColinearWith(other)`

:   Whether two line-likes lie along the same infinite line.

<span class="def-icon">:material-code-block-parentheses:</span> `IsWithinDistanceAndAngleTo(other, distance, angle)`

:   For `Line`s and `Plane`s: Whether two lines or planes are close in both position and direction.

<span class="def-icon">:material-code-block-parentheses:</span> `Contains(location)`

:   For [line-likes](lines.md#measuring-intersecting) and planes: Whether a location lies on the line or plane, treating it as 1cm thick by default (`ILineLike.DefaultLineThickness` and `Plane.DefaultPlaneThickness`), or a thickness you pass.

## Floating-Point Corrections

TinyFFR proactively corrects small floating-point errors where it can do so safely, so that common comparisons work as you'd expect:

* Directions are re-normalized to unit length after being rotated or otherwise modified. (Methods with `WithoutRenormalizing` in their name skip this for speed, at the cost of slowly accumulating error if you chain them.)
* `Angle.FromAngleBetweenDirections()` (and the `^` operator on directions) snaps results that are extremely close to 0°, 90°, or 180° to exactly those values, so checks like `(d1 ^ d2) == Angle.QuarterCircle` work for directions that really are at right-angles.
* Values that must lie in a range (e.g. the input to an inverse trigonometric function) are clamped to that range first, so a tiny error can't produce `NaN`.

Even so, you should still use a tolerance whenever you compare values that have been calculated.
