// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
public readonly partial struct Transform : IMathPrimitive<Transform>, IBoundedRandomizable<Transform>, IDescriptiveStringProvider {
	public Location Position { get; init; }
	public Rotation Rotation { get; init; }
	public Vect Scaling { get; init; }

	public Transform() : this(Location.Origin) { }
	public Transform(Location position) : this(position, Rotation.None) { }
	public Transform(Location position, Rotation rotation) : this(position, rotation, (1f, 1f, 1f)) { }
	public Transform(Location position, Rotation rotation, Vect scaling) {
		Position = position;
		Rotation = rotation;
		Scaling = scaling;
	}

	#region Factories and Conversions
	public Matrix4x4 ToMatrix() {

	}
	public void ToMatrix(ref Matrix4x4 dest) {

	}

	public void Deconstruct(out Location position, out Rotation rotation, out Vect scaling) {
		position = Position;
		rotation = Rotation;
		scaling = Scaling;
	}

	public static implicit operator Transform(Location position) => new(position);
	public static implicit operator Transform((Location Position, Rotation Rotation) tuple) => new(tuple.Position, tuple.Rotation);
	public static implicit operator Transform((Location Position, Rotation Rotation, Vect Scaling) tuple) => new(tuple.Position, tuple.Rotation, tuple.Scaling);
	#endregion

	#region Random
	public static Transform Random() {
		return new(
			Location.Random(),
			Rotation.Random(),
			Vect.Random()
		);
	}

	public static Transform Random(Transform minInclusive, Transform maxExclusive) {
		return new(
			Location.Random(minInclusive.Position, maxExclusive.Position),
			Rotation.Random(minInclusive.Rotation, maxExclusive.Rotation),
			Vect.Random(minInclusive.Scaling, maxExclusive.Scaling)
		);
	}
	#endregion

	#region Span Conversion
	public static int SerializationByteSpanLength { get; } = Location.SerializationByteSpanLength + Rotation.SerializationByteSpanLength + Vect.SerializationByteSpanLength;

	public static void SerializeToBytes(Span<byte> dest, Transform src) {
		Location.SerializeToBytes(dest, src.Position);
		dest = dest[Location.SerializationByteSpanLength..];
		Rotation.SerializeToBytes(dest, src.Rotation);
		dest = dest[Rotation.SerializationByteSpanLength..];
		Vect.SerializeToBytes(dest, src.Scaling);
	}

	public static Transform DeserializeFromBytes(ReadOnlySpan<byte> src) {
		var location = Location.DeserializeFromBytes(src);
		src = src[Location.SerializationByteSpanLength..];
		var rotation = Rotation.DeserializeFromBytes(src);
		src = src[Rotation.SerializationByteSpanLength..];
		var scaling = Vect.DeserializeFromBytes(src);
		return new(location, rotation, scaling);
	}
	#endregion
}