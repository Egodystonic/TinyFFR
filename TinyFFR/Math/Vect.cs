// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a displacement or magnitude/direction pair in 3D space (e.g. a velocity, a force, or the difference between two <see cref="Location"/>s).
/// </summary>
/// <remarks>
/// Unlike <see cref="Direction"/>, a <see cref="Vect"/> is not constrained to unit length: it carries both a direction
/// and a magnitude (length). Use a <see cref="Vect"/> whenever "how far and which way" matters, and a <see cref="Direction"/>
/// when only "which way" matters.
/// </remarks>
[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)]
public readonly partial struct Vect : IVect<Vect>, IDescriptiveStringProvider {
	internal const float WValue = 0f;
	internal const float DefaultRandomRange = 100f;
	/// <summary>
	/// The vector with all components equal to <c>0f</c>.
	/// </summary>
	public static readonly Vect Zero = new(0f, 0f, 0f);
	/// <summary>
	/// The vector with all components equal to <c>1f</c>.
	/// </summary>
	public static readonly Vect One = new(1f, 1f, 1f);

	internal readonly Vector4 AsVector4;

	/// <inheritdoc />
	public float X {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.X;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.X = value;
	}
	/// <inheritdoc />
	public float Y {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Y;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.Y = value;
	}
	/// <inheritdoc />
	public float Z {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Z;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.Z = value;
	}

	/// <inheritdoc />
	public float this[Axis axis] => axis switch {
		Axis.X => X,
		Axis.Y => Y,
		Axis.Z => Z,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} must not be anything except {nameof(Axis.X)}, {nameof(Axis.Y)} or {nameof(Axis.Z)}.")
	};
	/// <inheritdoc />
	public XYPair<float> this[Axis first, Axis second] => new(this[first], this[second]);
	/// <inheritdoc />
	public Vect this[Axis first, Axis second, Axis third] => new(this[first], this[second], this[third]);

	/// <summary>
	/// Constructs a new <see cref="Vect"/> equal to <see cref="Zero"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect() : this(0f, 0f, 0f) { }
	/// <summary>
	/// Constructs a new <see cref="Vect"/> with all three components set to <paramref name="xyz"/>.
	/// </summary>
	/// <param name="xyz">The value to use for all three of <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect(float xyz) : this(xyz, xyz, xyz) { }
	/// <summary>
	/// Constructs a new <see cref="Vect"/> with the given <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/> components.
	/// </summary>
	/// <param name="x">The X component.</param>
	/// <param name="y">The Y component.</param>
	/// <param name="z">The Z component.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect(float x, float y, float z) : this(new Vector4(x, y, z, WValue)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Vect(Vector4 v) { AsVector4 = v; }

	#region Factories and Conversions
	/// <summary>
	/// Constructs a new <see cref="Vect"/> pointing in <paramref name="direction"/> with length <paramref name="distance"/>; equivalent to <c>direction * distance</c>.
	/// </summary>
	/// <param name="direction">The direction of the resultant vector.</param>
	/// <param name="distance">The length of the resultant vector. Can be negative, in which case the resultant vector points opposite to <paramref name="direction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect FromDirectionAndDistance(Direction direction, float distance) => direction * distance;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect FromVector3(Vector3 v) => new(new Vector4(v, WValue));

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	/// <inheritdoc />
	public void Deconstruct(out float x, out float y, out float z) {
		x = X;
		y = Y;
		z = Z;
	}
	/// <inheritdoc />
	public static implicit operator Vect((float X, float Y, float Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);

	/// <summary>
	/// Converts <paramref name="directionOperand"/> to a unit-length <see cref="Vect"/> by treating its components as vector components.
	/// </summary>
	/// <param name="directionOperand">The direction to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Vect(Direction directionOperand) => new(directionOperand.AsVector4 with { W = WValue });
	/// <summary>
	/// Converts <paramref name="locationOperand"/> to a <see cref="Vect"/> by treating its coordinates as vector components.
	/// </summary>
	/// <param name="locationOperand">The location to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Vect(Location locationOperand) => new(locationOperand.AsVector4 with { W = WValue });

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Vect IVect.AsVect() => this;
	#endregion

	#region Random
	/// <summary>
	/// Produces a random vector, with each axis independently in the range <c>-100f</c> to <c>100f</c>.
	/// </summary>
	/// <returns>A new <see cref="Vect"/> with each of <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/> independently in the range <c>[-100, 100]</c>.</returns>
	public static Vect Random() {
		return new Vect(
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive()
		) * DefaultRandomRange;
	}
	/// <summary>
	/// Produces a random vector, with each axis independently in the range <paramref name="minInclusive"/> to <paramref name="maxExclusive"/>.
	/// </summary>
	/// <param name="minInclusive">The minimum value for each of <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/> independently.</param>
	/// <param name="maxExclusive">The exclusive ceiling for each of <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/> independently.</param>
	/// <returns>A new <see cref="Vect"/> <c>v</c> such that, for each axis independently, <c>minInclusive &lt;= v &lt; maxExclusive</c>.</returns>
	public static Vect Random(Vect minInclusive, Vect maxExclusive) {
		return new(
			RandomUtils.NextSingle(minInclusive.X, maxExclusive.X),
			RandomUtils.NextSingle(minInclusive.Y, maxExclusive.Y),
			RandomUtils.NextSingle(minInclusive.Z, maxExclusive.Z)
		);
	}
	#endregion

	#region Span Conversion
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 3;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, Vect src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.X);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.Y);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Z);
	}

	/// <inheritdoc />
	public static Vect DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..])
		);
	}
	#endregion

	#region String Conversion
	/// <inheritdoc />
	public override string ToString() => this.ToString(null, null);

	/// <inheritdoc />
	public string ToStringDescriptive() {
		return $"{ToString()} (Direction {Direction.ToStringDescriptive()}, Length {Length.ToString(LengthSquared >= 10f ? "N1" : "N3", CultureInfo.InvariantCulture)})";
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect Parse(string s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Vect result) {
		if (!IVect.TryParseVector3String(s, provider, out var vec3)) {
			result = default;
			return false;
		}
		else {
			result = FromVector3(vec3);
			return true;
		}
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Vect result) {
		if (!IVect.TryParseVector3String(s, provider, out var vec3)) {
			result = default;
			return false;
		}
		else {
			result = FromVector3(vec3);
			return true;
		}
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Vect other) => AsVector4.Equals(other.AsVector4);
	/// <summary>
	/// Determines whether this vector is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares the <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/> components independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(Vect other, float tolerance) {
		return MathF.Abs(X - other.X) <= tolerance
			&& MathF.Abs(Y - other.Y) <= tolerance
			&& MathF.Abs(Z - other.Z) <= tolerance;
	}
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Vect left, Vect right) => left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Vect left, Vect right) => !left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Vect other && Equals(other);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsVector4.GetHashCode();
	#endregion
}