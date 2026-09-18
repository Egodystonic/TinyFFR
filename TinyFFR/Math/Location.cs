// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a single point in 3D space.
/// </summary>
/// <remarks>
/// In contrast to a <see cref="Vect"/>, this type represents a singular point, not a direction + magnitude.
/// In some contexts a Location can be thought of as a displacement vector from the co-ordinate origin: For these cases <see cref="AsVect"/> is provided.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)]
public readonly partial struct Location : IVect<Location> {
	internal const float WValue = 1f;
	internal const float DefaultRandomRange = 100f;
	/// <summary>
	/// The location at <c>(0f, 0f, 0f)</c>.
	/// </summary>
	public static readonly Location Origin = new(0f, 0f, 0f);

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
	public Location this[Axis first, Axis second, Axis third] => new(this[first], this[second], this[third]);

	/// <summary>
	/// Constructs a new <see cref="Location"/> equal to <see cref="Origin"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location() : this(0f, 0f, 0f) { }
	/// <summary>
	/// Constructs a new <see cref="Location"/> at the given <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/> coordinates.
	/// </summary>
	/// <param name="x">The X coordinate.</param>
	/// <param name="y">The Y coordinate.</param>
	/// <param name="z">The Z coordinate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location(float x, float y, float z) : this(new Vector4(x, y, z, WValue)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Location(Vector4 v) { AsVector4 = v; }

	#region Factories and Conversions
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location FromVector3(Vector3 v) => new(new Vector4(v, WValue));

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
	public static implicit operator Location((float X, float Y, float Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);

	/// <summary>
	/// Converts <paramref name="directionOperand"/> to a <see cref="Location"/> by treating its components as coordinates.
	/// </summary>
	/// <param name="directionOperand">The direction to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Location(Direction directionOperand) => new(directionOperand.AsVector4 with { W = WValue });
	/// <summary>
	/// Converts <paramref name="vectOperand"/> to a <see cref="Location"/> by treating its components as coordinates.
	/// </summary>
	/// <param name="vectOperand">The vector to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Location(Vect vectOperand) => new(vectOperand.AsVector4 with { W = WValue });
	#endregion

	#region Random
	/// <summary>
	/// Produces a random location, with each axis independently in the range <c>-100f</c> to <c>100f</c>.
	/// </summary>
	/// <returns>A new <see cref="Location"/> with each of <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/> independently in the range <c>[-100, 100]</c>.</returns>
	public static Location Random() {
		return FromVector3(new Vector3(
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive()
		) * DefaultRandomRange);
	}
	/// <summary>
	/// Produces a random location that lies somewhere on the straight line between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// </summary>
	/// <remarks>
	/// This is not a volumetric function, the resultant value always lies on the line segment between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// For a volumetric random point (i.e. treating <paramref name="minInclusive"/> and <paramref name="maxExclusive"/> as opposite corners of a cuboid),
	/// use <see cref="PositionedCuboid.FromOppositeCorners">PositionedCuboid.FromOppositeCorners(min, max)</see>.<see cref="PositionedCuboid.Random()">Random()</see> instead.
	/// </remarks>
	/// <param name="minInclusive">One end of the line segment to pick from. This value itself can be returned.</param>
	/// <param name="maxExclusive">The other end of the line segment to pick from. This exact value will not be returned.</param>
	public static Location Random(Location minInclusive, Location maxExclusive) {
		return minInclusive + ((minInclusive >> maxExclusive) * RandomUtils.NextSingle());
	}
	/// <summary>
	/// Produces a random location, uniformly distributed within the interior of <paramref name="shape"/>.
	/// </summary>
	/// <param name="shape">The convex shape to pick a random interior point from.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location Random<TShape>(TShape shape) where TShape : IConvexShape => shape.GetRandomInternalLocation();
	#endregion

	#region Span Conversion
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 3;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, Location src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.X);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.Y);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Z);
	}

	/// <inheritdoc />
	public static Location DeserializeFromBytes(ReadOnlySpan<byte> src) {
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location Parse(string s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Location result) {
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
	public static Location Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) => FromVector3(IVect.ParseVector3String(s, provider));

	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Location result) {
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
	public bool Equals(Location other) => AsVector4.Equals(other.AsVector4);
	/// <summary>
	/// Determines whether this location is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares the <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/> coordinates independently, each within
	/// <paramref name="tolerance"/>; it is not the same as checking whether the two locations are within a given
	/// distance of each other (for that, use <see cref="IsWithinDistanceOf"/> instead).
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(Location other, float tolerance) {
		return MathF.Abs(X - other.X) <= tolerance
			&& MathF.Abs(Y - other.Y) <= tolerance
			&& MathF.Abs(Z - other.Z) <= tolerance;
	}
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Location left, Location right) => left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Location left, Location right) => !left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Location other && Equals(other);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsVector4.GetHashCode();
	#endregion
}