---
title: Math API Optimisations
description: Ways to speed up heavy use of TinyFFR's math and geometry types, including quaternions, Fast and Squared variants, skipping renormalization, and SIMD via System.Numerics.
---

TinyFFR's math types are designed to be safe, easy to use, and highly performant. In some cases the API must make a tradeoff between safety/approachability and performance. For most applications the differences are negligible, but code that processes many thousands of values per frame may benefit from the techniques on this page.

The figures below were measured by processing one million values at a time; how much each technique helps in your application will depend on what your code does, your machine's architecture, your hardware, etc.; so always measure/profile to identify bottlenecks and prove improvements.

???+ tip "Not Useful for Occasional API Use"
	You don't need to start pre-converting every `Rotation` to a `Quaternion`, or using `Fast` variants of the API at every possible call-site, etc. 
	
	Identify areas where you're using hundreds of 'slower' invocations per frame and consider using the faster alternatives instead in these places, backed with benchmarks/profiling to prove the need and the subsequent speedup.
	
	A lot of the "slower" default API provides *much* better ergonomics; i.e. returning a `null` value instead of an undefined value for geometric queries that have no sensible answer, or making it possible to represent a 180° rotation without degenerating the axis. It's not worth giving that up for most of your math code.
	
	Remember, the figures quoted below were tested with *one million* invocations of the pertinent API.

## Quaternion vs Rotation

A `Rotation` stores an axis and an angle, which makes it easy to understand and create. However, actually *applying* a rotation (e.g. rotating a `Direction`) requires converting it to a quaternion (a `System.Numerics.Quaternion`) first, which involves some trigonometry. When the same rotation is applied many times, it can be worth converting it once and using the overloads that take a `Quaternion` directly:

```csharp
var spin = new Rotation(90f, Direction.Up);
var spinQuaternion = spin.ToQuaternion(); // (1)!

for (var i = 0; i < directions.Length; ++i) {
	directions[i] = Rotation.Rotate(directions[i], spinQuaternion); // (2)!
}
```

1.	Converts the rotation to a quaternion once.

2.	Applies the pre-converted quaternion. In testing, this was around 2.4x faster than `directions[i] * spin`, which converts the rotation every time.

Many other methods also accept a `Quaternion` in place of a `Rotation`, including:

* `RotatedBy()` on `Location`, `Vect`, `Direction`, `Line`, `Ray`, `BoundedRay`, and `Plane`;
* `RotateBy()` and `SetRotationQuaternion()` on scene objects such as model instances, cameras, and lights;
* `Rotation.Combine()`, `Rotation.Interpolate()`, and `Transform.WithAdditionalRotation()`.

Similarly, when combining a long chain of rotations, combining them as quaternions and converting back to a `Rotation` once at the end (with `Rotation.FromQuaternion()`) avoids a conversion at every step.

## Skipping Checks & Corrections

Many methods check their inputs or correct their outputs, to keep results valid and consistent. When you already know those checks or corrections aren't needed, some methods offer variants that skip them.

### Fast Variants

Methods that might not have a meaningful answer (e.g. the intersection of a ray with a plane it runs parallel to) return a nullable value. Many have a `Fast...()` variant that skips the check, returning a non-nullable value that's undefined if there's no meaningful answer (see [API Naming](conventions.md#api-naming)):

```csharp
var hit = ray.FastIntersectionWith(groundPlane); // (1)!
```

1.	Only safe if the ray is known to hit the plane (e.g. a ray pointing downwards and a horizontal plane below it). In testing, this was around 13% faster than `IntersectionWith()`.

`Fast...()` variants exist for intersections, reflections, projections, orthogonalization and parallelization, splitting, incident angles, and more. They're typically worth considering in tight loops where the inputs are already known to be valid.

### Squared Variants

`LengthSquared`, `DistanceSquaredFrom()`, `SurfaceDistanceSquaredFrom()`, and `RadiusSquared` skip a square root. Squared distances can be compared just like distances (e.g. to find the nearest of several objects), as long as you compare them with other squared values:

```csharp
var nearest = candidates[0];
var nearestDistanceSquared = Single.MaxValue;
foreach (var candidate in candidates) {
	var distanceSquared = player.DistanceSquaredFrom(candidate); // (1)!
	if (distanceSquared < nearestDistanceSquared) {
		nearest = candidate;
		nearestDistanceSquared = distanceSquared;
	}
}
```

1.	The squared distance from `player` to `candidate`.

Square roots are cheap on modern processors, so the difference may be small (in testing, finding the nearest of a million locations was only around 3% faster); but it does no harm where a squared value works just as well.

### Skipping Renormalization

Some operations correct their results to remove tiny floating-point errors (e.g. keeping a `Direction` at exactly unit length). Variants that skip the correction can be faster, at the cost of those errors accumulating over many repeated operations:

<span class="def-icon">:material-code-block-parentheses:</span> `Rotation.RotateWithoutRenormalizing(direction, quaternion)` / `Rotation.RotateWithoutCorrectingLength(vect, quaternion)`

:   Rotate without correcting the result's length afterwards. In testing, rotating directions this way (with a pre-converted quaternion) was around 4.3x faster than `direction * rotation`.

	If you repeatedly rotate the same values this way (e.g. every frame), occasionally correcting them with `Direction.Renormalize()` may be worthwhile (see [Renormalizing](vector_types.md)).

<span class="def-icon">:material-code-block-parentheses:</span> `Direction.FromVector3PreNormalized()` / `Rotation.FromQuaternionPreNormalized()`

:   Create a value from components you know are already normalized, skipping the normalization the other factory methods perform. Passing components that aren't normalized creates an invalid value.

<span class="def-icon">:material-code-block-parentheses:</span> `PlusWithoutNormalization()` / `MinusWithoutNormalization()` / `ScaledWithoutNormalizationBy()`

:   `ColorVect` operations that skip clamping each channel to the `0` to `1` range (which can also be useful for high-dynamic-range colours).

## Precomputing Interpolation

When interpolating between the same two directions, rotations, lines, rays, or planes many times, `CreateInterpolationPrecomputation()` and `InterpolateUsingPrecomputation()` can do some of the work once up front. See [Interpolation Precomputation](interpolation_algorithms.md).

## SIMD Acceleration

Several of TinyFFR's vector types have exactly the same layout in memory as types from `System.Numerics`:

| TinyFFR type | Same layout as | W component |
| :-- | :-- | :-- |
| `Vect` | `Vector4` | Always `0f` |
| `Location` | `Vector4` | Always `1f` |
| `Direction` | `Vector4` | Always `0f` (and `X`, `Y`, `Z` are unit length) |
| `ColorVect` | `Vector4` | Alpha |
| `XYPair<float>` | `Vector2` | - |
| `Angle` | `float` (radians) | - |

This means a span of these values can be reinterpreted (without copying) as a span of the `System.Numerics` type with `MemoryMarshal.Cast()`, and then processed with `Vector4` operations, or with wider SIMD types such as `Vector256<float>` that process several values at once:

```csharp
var asVector4s = MemoryMarshal.Cast<Location, Vector4>(locations); // (1)!
var offsetVector = new Vector4(offset.X, offset.Y, offset.Z, 0f); // (2)!
for (var i = 0; i < asVector4s.Length; ++i) {
	asVector4s[i] += offsetVector; // (3)!
}
```

1.	`locations` is a `Span<Location>`. This reinterprets the same memory as a `Span<Vector4>`; nothing is copied.

2.	The offset as a `Vector4`, with a `W` of `0f` so that adding it leaves every location's `W` at `1f`.

3.	Moves every location by `offset`; exactly equivalent to `locations[i] += offset`. In testing, this was around 1.45x faster than using `Location` directly.

Wider SIMD types can process two (or more) values per operation. For example, finding the centre of a large number of locations with `Vector256<float>`:

```csharp
var asVector256s = MemoryMarshal.Cast<Location, Vector256<float>>(locations); // (1)!
var sum = Vector256<float>.Zero;
foreach (var pair in asVector256s) sum += pair; // (2)!
var total = (sum.GetLower() + sum.GetUpper()).AsVector4(); // (3)!

var remainder = MemoryMarshal.Cast<Location, Vector4>(locations[(asVector256s.Length * 2)..]); // (4)!
foreach (var leftover in remainder) total += leftover;

var centre = new Location(total.X / locations.Length, total.Y / locations.Length, total.Z / locations.Length); // (5)!
```

1.	Reinterprets the locations as 256-bit vectors, each holding two locations' worth of `float`s. If there's an odd number of locations, the last one isn't included in this span.

2.	Adds two locations at a time.

3.	Combines the two halves (the sums of the even- and odd-numbered locations) in to one `Vector4`.

4.	Adds the leftover location, if there is one.

5.	The average of all the locations. In testing, this whole calculation was around 7x faster than summing `(Vect) location` for each location (though, as with any change to the order of floating-point additions, the result can differ very slightly in the last few digits).

!!! warning "Keeping Values Valid"
	Writing to a reinterpreted span writes directly to the original values, bypassing all of TinyFFR's validation. Any values you write must keep each type's rules shown in the table above: A `Location`'s `W` must be `1f`, a `Vect`'s must be `0f`, and a `Direction` must be unit length with a `W` of `0f`. Otherwise you may get incorrect results from later operations.

	Also, note:
	
	* `Rotation` is stored as a `Vector4` (3-float unit-length axis followed by a 1-float angle in radians), *not* a `Quaternion`. 
	* `Transform`'s interpretation can change according to its internal representation; but [you can coerce it in to Matrix form](transform.md#matrices).
	
!!! info "'Plane' Name Clash"
	`System.Numerics` also contains a type named `Plane`. If a file imports both `System.Numerics` and `Egodystonic.TinyFFR`, references to `Plane` become ambiguous; a using alias (e.g. `using Plane = Egodystonic.TinyFFR.Plane;`) resolves this.
