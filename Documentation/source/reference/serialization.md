---
title: Serialization
description: How to convert TinyFFR's math and geometry types to and from bytes, and the binary format they use.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Every math and geometry type can be written to bytes with `SerializeToBytes()` and read back with `DeserializeFromBytes()`. :material-arrow-right: [Serializing Values](#serializing-values)

</div>

## Serializing Values

Every math and geometry type has three static members for converting to and from bytes:

```csharp
Span<byte> buffer = stackalloc byte[Location.SerializationByteSpanLength]; // (1)!
Location.SerializeToBytes(buffer, location); // (2)!
var restored = Location.DeserializeFromBytes(buffer); // (3)!
```

1.	`SerializationByteSpanLength` is the number of bytes a value of the type takes up (12 for a `Location`). Every value of a type takes the same number of bytes.

2.	Writes `location` to the start of `buffer`. The destination must be at least `SerializationByteSpanLength` bytes long.

3.	Reads a `Location` back from the start of `buffer`. `restored` is exactly equal to `location`.

Because every value has a fixed length, several values can be packed one after another and read back in the same order:

```csharp
var transformLength = Transform.SerializationByteSpanLength;
var colorLength = ColorVect.SerializationByteSpanLength;
Span<byte> buffer = stackalloc byte[transformLength + colorLength]; // (1)!

Transform.SerializeToBytes(buffer, instance.Transform); // (2)!
ColorVect.SerializeToBytes(buffer[transformLength..], color); // (3)!
File.WriteAllBytes("saved_object.bin", buffer); // (4)!

ReadOnlySpan<byte> loaded = File.ReadAllBytes("saved_object.bin"); // (5)!
instance.Transform = Transform.DeserializeFromBytes(loaded);
color = ColorVect.DeserializeFromBytes(loaded[transformLength..]);
```

1.	A buffer big enough for a `Transform` followed by a `ColorVect`. (`stackalloc` creates the buffer without allocating any memory on the heap, which is best for small buffers. For larger ones, borrow a buffer with `factory.ResourceAllocator.BorrowSpan<byte>(length)` rather than creating a new array, to avoid creating garbage.)

2.	Writes a model instance's transform to the start of the buffer.

3.	Writes a colour straight after it, by slicing the buffer from the end of the transform's bytes.

4.	Saves the bytes to a file (any stream or other byte storage works just as well).

5.	Reads the file back and restores both values, using the same offsets.

??? tip "Generic Serialization"
	Every math type implements `IFixedLengthByteSpanSerializable<T>`, whose members are `static abstract`, so you can write generic code that serializes any of them:

	```csharp
	static void Save<T>(Stream stream, T value) where T : IFixedLengthByteSpanSerializable<T> {
		Span<byte> buffer = stackalloc byte[T.SerializationByteSpanLength];
		T.SerializeToBytes(buffer, value);
		stream.Write(buffer);
	}
	```

## Format

The binary format is designed to be stable: Data written by one version of TinyFFR, or on one platform, can be read by another.

* All `float` values are stored as 4-byte IEEE 754 values in **little-endian** byte order.
* Values are stored exactly, so deserializing always gives a value that is `==` to the original (see [Equality](equality.md)).
* Composite types are stored as their parts, one after another, in the order listed below.

| Type | Bytes | Layout |
| :-- | :-: | :-- |
| `Real`, `Angle` | 4 | The value (an `Angle` is stored in **radians**) |
| `Location`, `Vect`, `Direction` | 12 | `X`, `Y`, `Z` |
| `ColorVect` | 16 | `Red`, `Green`, `Blue`, `Alpha` |
| `XYPair<T>` | 2 × size of `T` | `X`, `Y` (e.g. 8 bytes for `XYPair<int>` or `XYPair<float>`), each in the machine's native byte order (which is little-endian on every platform TinyFFR supports) |
| `Rotation` | 16 | Axis `X`, `Y`, `Z`, then the angle in radians |
| `SphericalTranslation` | 8 | Azimuthal offset, polar offset (both `Angle`s) |
| `Transform` | 64 | See [Transforms](#transforms) below |
| `Transform2D` | 25 | See [Transforms](#transforms) below |
| `Line` | 24 | `PointOnLine` (a `Location`), `Direction` |
| `Ray` | 24 | `StartPoint` (a `Location`), `Direction` |
| `BoundedRay` | 24 | `StartPoint`, `EndPoint` (both `Location`s) |
| `Plane` | 24 | `Normal` (a `Direction`), `PointClosestToOrigin` (a `Location`) |
| `Sphere` | 4 | `Radius` |
| `Cuboid` | 12 | `Width`, `Height`, `Depth` |
| `PositionedSphere` | 16 | The `Sphere`, then its position (`X`, `Y`, `Z`) |
| `PositionedCuboid` | 24 | The `Cuboid`, then its position (`X`, `Y`, `Z`) |
| `PositionedRotatedCuboid` | 40 | The `Cuboid`, then its position (`X`, `Y`, `Z`), then its `Rotation` |

### Transforms

A [`Transform`](transform.md) can be stored either as separate scaling, rotation, and translation components, or as a matrix (see [Matrix-Represented Transforms](transform.md#matrices)). Serialization preserves whichever form the transform uses, so a deserialized transform is identical to the original in every way:

* A transform stored as components is written as 16 `float`s: its translation (`X`, `Y`, `Z`, and an unused `0`), its rotation as a quaternion (`X`, `Y`, `Z`, `W`), its scaling (`X`, `Y`, `Z`, and an unused `0`), and finally four `0`s.
* A transform stored as a matrix is written as the matrix's 16 values, row by row (`M11`, `M12`, `M13`, `M14`, `M21`, ..., `M44`). The final row of a transform matrix is never all zeros, which is how the two forms are told apart.

A `Transform2D` works in the same way, with 6 `float`s followed by a single byte flag (`0` for components, `255` for a matrix): as components, its translation (`X`, `Y`), rotation in radians, scaling (`X`, `Y`), and an unused `0`; as a matrix, the `Matrix3x2`'s values row by row (`M11`, `M12`, `M21`, `M22`, `M31`, `M32`).

## Text

The math types can also be converted to and from human-readable strings, via `ToString()` and `Parse()`/`TryParse()` (they implement .NET's `ISpanFormattable` and `ISpanParsable<T>`). Binary serialization is smaller, faster, and exact, whereas text is useful for logging, debugging, and hand-edited files.
