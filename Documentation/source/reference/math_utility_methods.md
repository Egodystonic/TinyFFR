---
title: Math Utility Methods
description: The static helper methods in CameraUtils, OrientationUtils, MathUtils, and PercentageUtils.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * `CameraUtils` calculates camera matrices, rays, projections, and how much of the world a camera sees, without needing a `Camera`. :material-arrow-right: [CameraUtils](#camerautils)
    * `OrientationUtils` lists every orientation value and builds orientations from per-axis values. :material-arrow-right: [OrientationUtils](#orientationutils)
    * `MathUtils` has general numeric and matrix helpers, and 2D anchoring calculations. :material-arrow-right: [MathUtils](#mathutils)
    * `PercentageUtils` converts fractions to and from percentage strings. :material-arrow-right: [PercentageUtils](#percentageutils)

</div>

TinyFFR's static utility classes hold calculations that don't belong to any one type. Many of their methods are described on the pages for the types they work with; this page covers the rest, and links to the others.

## CameraUtils

`CameraUtils` (in the `Egodystonic.TinyFFR.World` namespace) performs the same calculations a `Camera` does, but from plain values. That's useful when you want camera math without creating a camera resource, e.g. on another thread, or for a "virtual" camera that's never rendered.

```csharp
var visibleArea = CameraUtils.CalculatePerspectiveViewportWorldSizeAtDistance( // (1)!
	camera.HorizontalFieldOfView,
	camera.VerticalFieldOfView,
	10f
);
var cameraLeft = CameraUtils.CalculateCameraRelativeOrientationDirection( // (2)!
	Orientation.Left,
	viewDirection,
	upDirection
);
```

1.	The width and height (in metres) of the area the camera sees 10m in front of it. For example, a camera with a 90° horizontal and 60° vertical field of view sees an area 20m wide and about 11.5m tall at that distance. This is useful for sizing something so that it exactly fills the view at a given distance.

2.	The world direction that's to the *left* of a camera looking along `viewDirection`, with `upDirection` as its up. This is the calculation behind `camera.GetRelativeOrientationDirection()`.

<span class="def-icon">:material-code-block-parentheses:</span> `CalculateOrthographicViewportWorldSize(orthographicHeight, aspectRatio)`

:   The width and height (in metres) of the area an orthographic camera sees. This is the same at every distance, as an orthographic camera's view doesn't spread out.

<span class="def-icon">:material-code-block-parentheses:</span> `CalculatePerspectiveViewportWorldSizeAtDistanceFromFovTangents(...)`

:   The same as `CalculatePerspectiveViewportWorldSizeAtDistance()`, but taking the tangents of half of each field of view. If you calculate this every frame, precalculating the tangents saves some trigonometry.

The rest of `CameraUtils` is described elsewhere:

* `CalculateModelMatrix()`, `CalculateViewMatrix()`, `CalculatePerspectiveProjectionMatrix()`, and `CalculateOrthographicProjectionMatrix()` calculate a camera's matrices (see [Matrices](conventions.md#matrices)).
* `CreateRayFrom...CameraParameters()` and `ProjectOnTo...CameraNearPlane[Clamped]()` cast rays from, and project locations on to, a camera's near plane (see [Ray Casting, Pixel-Picking, & Projecting](ray_casting_pixel_picking_projecting.md)).

## OrientationUtils

`OrientationUtils` holds lists of every value of each [orientation enum](angle_rotation_orientation.md#orientation), and methods for building orientations:

<span class="def-icon">:material-card-bulleted-outline:</span> `All3DOrientations` / `AllCardinals` / `AllIntercardinals` / `AllDiagonals` / `AllAxes`

:   Every 3D orientation (26), cardinal (6), intercardinal (12), and diagonal (8) orientation, and every axis (3), as `ReadOnlySpan`s (so looping over them allocates no memory). `None` isn't included.

<span class="def-icon">:material-card-bulleted-outline:</span> `All2DOrientations` / `All2DDiagonals` / `AllHorizontals` / `AllVerticals`

:   The same for the [2D orientations](2d_types.md#2d-orientations): every `Orientation2D` (8), `DiagonalOrientation2D` (4), `HorizontalOrientation2D` (`Right` and `Left`), and `VerticalOrientation2D` (`Up` and `Down`).

<span class="def-icon">:material-code-block-parentheses:</span> `CreateOrientation(x, y, z)` / `CreateOrientationFromValueSigns(x, y, z)`

:   Builds an `Orientation` from three per-axis orientations, or from the signs of three numbers (see [Orientation](angle_rotation_orientation.md#orientation)).

<span class="def-icon">:material-code-block-parentheses:</span> `CreateXAxisOrientationFromValueSign(value)` / `CreateYAxisOrientationFromValueSign(value)` / `CreateZAxisOrientationFromValueSign(value)`

:   The orientation along a single axis matching the sign of `value` (e.g. a negative X value gives `XAxisOrientation.Right`, as `+X` is left), or `None` for zero.

## MathUtils

### Numbers

```csharp
var wrapped = MathUtils.TrueModulus(-1, 360); // (1)!
var smallest = MathUtils.Min(a, b, c); // (2)!
var remapped = ((Real) 5f).RemapRange((0f, 10f), (0f, 100f)); // (3)!
var valid = brightness.IsPositiveAndFinite(); // (4)!
```

1.	`359`. Unlike the `%` operator (which gives `-1` here), the result always has the same sign as the divisor, which is usually what you want for wrapping values around (e.g. indices around a circular list, or degrees around a circle).

2.	The smallest of three values (of any comparable type). `MathUtils.Max()` gives the largest, and both also accept any number of values (e.g. `MathUtils.Min(a, b, c, d)`).

3.	`50`: 5 is halfway between 0 and 10, so it becomes the value halfway between 0 and 100. Values outside the input range are extrapolated beyond the output range, rather than clamped. `RemapRange()` works with [ordinal](trait_interfaces.md#arithmetic-traits) types (`Real` and `Angle`), so a `float` must be cast to a `Real` first.

4.	`true` if `brightness` is a finite number greater than zero. `IsNonNegativeAndFinite()` also accepts zero. Both are useful for validating input.

<span class="def-icon">:material-code-block-parentheses:</span> `SafeAbs(value)`

:   The absolute value of an integer, without overflowing for the smallest possible value (`Math.Abs(int.MinValue)` throws an exception, whereas `MathUtils.SafeAbs(int.MinValue)` returns `int.MaxValue`).

<span class="def-icon">:material-card-bulleted-outline:</span> `GoldenRatio` / `SquareRootOfTwo` / `SquareRootOfThree` / `SquareRootOfTwoReciprocal` / `SquareRootOfThreeReciprocal`

:   Commonly needed constants.

<span class="def-icon">:material-code-block-parentheses:</span> `NormalizeOrZero(vector4)` / `NormalizeOrIdentity(quaternion)`

:   Normalizes a `System.Numerics` `Vector4` or `Quaternion`, returning zero (or the identity quaternion) instead of `NaN`s when given a zero-length value.

### Matrices

These help when working with `System.Numerics` matrices directly (e.g. when interoperating with other libraries):

<span class="def-icon">:material-code-block-parentheses:</span> `GetBestGuessTransformFromMatrix(matrix)`

:   Converts a `Matrix4x4` to a [`Transform`](transform.md) (or a `Matrix3x2` to a `Transform2D`), with a separate scaling, rotation, and translation. Unlike `new Transform(matrix)`, the result is stored as components rather than as a matrix. Even matrices that can't be decomposed cleanly (e.g. ones that scale an axis to zero) give a result, although it may only be approximate. `GetTranslationFromMatrix()`, `GetBestGuessScalingFromMatrix()`, and `GetBestGuessRotationFromMatrix()` extract just one part.

<span class="def-icon">:material-code-block-parentheses:</span> `ForceInvertMatrix(matrix)`

:   Inverts a `Matrix4x4` or `Matrix3x2`, always returning a usable result. A matrix that can't be inverted normally (e.g. one that scales an axis to zero) is adjusted until it can be, so the result is only approximate in that case.

<span class="def-icon">:material-code-block-parentheses:</span> `matrix.Equals(other, tolerance)`

:   Compares two matrices within a tolerance (see [Equality](equality.md#tolerance-equality)).

<span class="def-icon">:material-code-block-parentheses:</span> `matrix.ToStringDescriptive()`

:   A readable description of a `Matrix4x4`, recognising common cases (e.g. `Identity`, `Translation[1.00/2.00/3.00]`, or `Rotation[90° around Up]`) and listing every value otherwise.

<span class="def-icon">:material-code-block-parentheses:</span> `matrix.GetRow(index)` / `matrix.GetColumn(index)`

:   One row or column of a `Matrix4x4`, as a `Vector4`.

### 2D Anchoring

`FindAnchoredPointIn2DCoordinateSystem()`, `FindAnchoredAreaIn2DCoordinateSystem()`, and `FindAnchorInNormalized2DCoordinateSystem()` position things relative to the corners and edges of a 2D grid, such as a window. See [Anchoring Helpers](2d_types.md#anchoring-helpers).

## PercentageUtils

`PercentageUtils` converts between fractions (where `1f` is 100%) and percentage strings:

```csharp
var text = PercentageUtils.ConvertFractionToPercentageString(0.5f); // (1)!
var precise = PercentageUtils.ConvertFractionToPercentageString(0.12345f, "N1"); // (2)!
var pair = PercentageUtils.ConvertFractionToPercentageString(new XYPair<float>(0.5f, 1f)); // (3)!
var fraction = PercentageUtils.ParsePercentageStringToFraction("75%"); // (4)!
```

1.	`"50%"`.

2.	`"12.3%"`, using a standard .NET number format (`N1` gives one decimal place).

3.	`"<50%, 100%>"`. Three-component values such as `Vect` work in the same way (e.g. `"<50%, 100%, 0%>"`).

4.	`0.75f`. `ParsePercentageStringToFractionXYPair()` and `ParsePercentageStringToFractionVect<T>()` parse the multi-component forms back, and `TryParse...()` versions return `false` instead of throwing for invalid strings. The `%` sign is optional when parsing.

`TryFormatFractionToPercentageString()` writes in to a `Span<char>` instead of creating a new string.
