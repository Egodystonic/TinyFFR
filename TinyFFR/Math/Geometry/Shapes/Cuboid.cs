// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Trait interface used to mark a type as behaving like a rectangular cuboid ("box") shape.
/// </summary>
public interface ICuboid : IConvexShape {
	/// <summary>
	/// Half of <see cref="Width"/>, i.e. the distance from the centre of the cuboid to either of its X-axis-facing sides.
	/// </summary>
	float HalfWidth { get; init; }
	/// <summary>
	/// Half of <see cref="Height"/>, i.e. the distance from the centre of the cuboid to either of its Y-axis-facing sides.
	/// </summary>
	float HalfHeight { get; init; }
	/// <summary>
	/// Half of <see cref="Depth"/>, i.e. the distance from the centre of the cuboid to either of its Z-axis-facing sides.
	/// </summary>
	float HalfDepth { get; init; }
	/// <summary>
	/// The size of the cuboid along the X axis.
	/// </summary>
	float Width { get; init; }
	/// <summary>
	/// The size of the cuboid along the Y axis.
	/// </summary>
	float Height { get; init; }
	/// <summary>
	/// The size of the cuboid along the Z axis.
	/// </summary>
	float Depth { get; init; }
	/// <summary>
	/// The volume enclosed by the cuboid.
	/// </summary>
	float Volume { get; }
	/// <summary>
	/// The total surface area of the cuboid (the sum of the areas of all six sides).
	/// </summary>
	float SurfaceArea { get; }
	/// <summary>
	/// The smallest of <see cref="HalfWidth"/>, <see cref="HalfHeight"/> and <see cref="HalfDepth"/>.
	/// </summary>
	float SmallestHalfExtent{ get; }
	/// <summary>
	/// The smallest of <see cref="Width"/>, <see cref="Height"/> and <see cref="Depth"/>.
	/// </summary>
	float SmallestExtent { get; }
	/// <summary>
	/// The largest of <see cref="HalfWidth"/>, <see cref="HalfHeight"/> and <see cref="HalfDepth"/>.
	/// </summary>
	float LargestHalfExtent{ get; }
	/// <summary>
	/// The largest of <see cref="Width"/>, <see cref="Height"/> and <see cref="Depth"/>.
	/// </summary>
	float LargestExtent { get; }

	/// <summary>
	/// Returns the centre point of the given <paramref name="side"/> of the cuboid.
	/// </summary>
	/// <param name="side">The side to find the centre point of. Must not be <see cref="CardinalOrientation.None"/>.</param>
	Location CentroidAt(CardinalOrientation side);
	/// <summary>
	/// Returns the given <paramref name="corner"/> of the cuboid.
	/// </summary>
	/// <param name="corner">The corner to find. Must not be <see cref="DiagonalOrientation.None"/>.</param>
	Location CornerAt(DiagonalOrientation corner);
	/// <summary>
	/// Returns the plane that the given <paramref name="side"/> of the cuboid lies within.
	/// </summary>
	/// <remarks>
	/// The plane's normal points away from the cuboid's centre (i.e. it is equal to <c>side.ToDirection()</c>).
	/// </remarks>
	/// <param name="side">The side to find. Must not be <see cref="CardinalOrientation.None"/>.</param>
	Plane SideAt(CardinalOrientation side);
	/// <summary>
	/// Returns the given <paramref name="edge"/> of the cuboid.
	/// </summary>
	/// <param name="edge">The edge to find. Must not be <see cref="IntercardinalOrientation.None"/>.</param>
	BoundedRay EdgeAt(IntercardinalOrientation edge);
}
/// <summary>
/// Extension of <see cref="ICuboid"/> that includes a self type parameter allowing for more functional definitions.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface ICuboid<TSelf> : ICuboid, IConvexShape<TSelf> where TSelf : ICuboid<TSelf> {
	/// <summary>
	/// All eight corners of this cuboid.
	/// </summary>
	IndirectEnumerable<TSelf, Location> Corners { get; }
	/// <summary>
	/// All twelve edges of this cuboid.
	/// </summary>
	IndirectEnumerable<TSelf, BoundedRay> Edges { get; }
	/// <summary>
	/// All six sides of this cuboid, as planes.
	/// </summary>
	IndirectEnumerable<TSelf, Plane> Sides { get; }
	/// <summary>
	/// The centre points of all six sides of this cuboid.
	/// </summary>
	IndirectEnumerable<TSelf, Location> Centroids { get; }
	/// <summary>
	/// Returns this cuboid scaled uniformly on all three axes so that its <see cref="ICuboid.Volume"/> becomes <paramref name="newVolume"/>.
	/// </summary>
	/// <param name="newVolume">The desired volume. Must be non-negative.</param>
	TSelf WithVolume(float newVolume);
	/// <summary>
	/// Returns this cuboid scaled uniformly on all three axes so that its <see cref="ICuboid.SurfaceArea"/> becomes <paramref name="newSurfaceArea"/>.
	/// </summary>
	/// <param name="newSurfaceArea">The desired surface area. Must be non-negative.</param>
	TSelf WithSurfaceArea(float newSurfaceArea);
	/// <summary>
	/// Returns this cuboid with <paramref name="adjustment"/> added to each of <see cref="ICuboid.HalfWidth"/>, <see cref="ICuboid.HalfHeight"/> and <see cref="ICuboid.HalfDepth"/>.
	/// </summary>
	/// <param name="adjustment">The amount to add to each half-extent. Can be negative to shrink the cuboid.</param>
	TSelf WithAllExtentsAdjustedBy(float adjustment);
}

/// <summary>
/// Represents an axis-aligned rectangular cuboid ("box") shape, centred on its own local origin.
/// </summary>
public readonly partial struct Cuboid : ICuboid<Cuboid> {
	internal const float DefaultRandomMin = 0.5f;
	internal const float DefaultRandomMax = 1.5f;
	/// <summary>
	/// A cuboid with <see cref="Width"/>, <see cref="Height"/> and <see cref="Depth"/> all equal to <c>1f</c>.
	/// </summary>
	public static readonly Cuboid UnitCube = new(1f);
	const int IteratorVersionNumber = 0;

	readonly float _halfWidth;
	readonly float _halfHeight;
	readonly float _halfDepth;

	/// <inheritdoc />
	public float HalfWidth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfWidth;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfWidth = value;
	}
	/// <inheritdoc />
	public float HalfHeight {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfHeight;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfHeight = value;
	}
	/// <inheritdoc />
	public float HalfDepth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfDepth;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfDepth = value;
	}

	/// <inheritdoc />
	public float Width {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfWidth * 2f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfWidth = value * 0.5f;
	}
	/// <inheritdoc />
	public float Height {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfHeight * 2f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfHeight = value * 0.5f;
	}
	/// <inheritdoc />
	public float Depth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfDepth * 2f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfDepth = value * 0.5f;
	}

	/// <inheritdoc />
	public float Volume => HalfWidth * HalfHeight * HalfDepth * 8f;
	/// <inheritdoc />
	public float SurfaceArea => (Width * Height + Height * Depth + Depth * Width) * 2f;
	/// <inheritdoc />
	public float SmallestHalfExtent => MathUtils.Min(HalfWidth, HalfHeight, HalfDepth);
	/// <inheritdoc />
	public float SmallestExtent => SmallestHalfExtent * 2f;
	/// <inheritdoc />
	public float LargestHalfExtent => MathUtils.Max(HalfWidth, HalfHeight, HalfDepth);
	/// <inheritdoc />
	public float LargestExtent => LargestHalfExtent * 2f;

	/// <inheritdoc />
	public unsafe IndirectEnumerable<Cuboid, Location> Corners => new(this, IteratorVersionNumber, &GetCornerCountForEnumerator, &GetIteratorVersion, &GetCornerForEnumerator);
	static int GetCornerCountForEnumerator(Cuboid _) => 8;
	static Location GetCornerForEnumerator(Cuboid @this, int index) => @this.CornerAt(OrientationUtils.AllDiagonals[index]);

	/// <inheritdoc />
	public unsafe IndirectEnumerable<Cuboid, BoundedRay> Edges => new(this, IteratorVersionNumber, &GetEdgeCountForEnumerator, &GetIteratorVersion, &GetEdgeForEnumerator);
	static int GetEdgeCountForEnumerator(Cuboid _) => 12;
	static BoundedRay GetEdgeForEnumerator(Cuboid @this, int index) => @this.EdgeAt(OrientationUtils.AllIntercardinals[index]);

	/// <inheritdoc />
	public unsafe IndirectEnumerable<Cuboid, Plane> Sides => new(this, IteratorVersionNumber, &GetSideCountForEnumerator, &GetIteratorVersion, &GetSideForEnumerator);
	static int GetSideCountForEnumerator(Cuboid _) => 6;
	static Plane GetSideForEnumerator(Cuboid @this, int index) => @this.SideAt(OrientationUtils.AllCardinals[index]);

	/// <inheritdoc />
	public unsafe IndirectEnumerable<Cuboid, Location> Centroids => new(this, IteratorVersionNumber, &GetCentroidCountForEnumerator, &GetIteratorVersion, &GetCentroidForEnumerator);
	static int GetCentroidCountForEnumerator(Cuboid _) => 6;
	static Location GetCentroidForEnumerator(Cuboid @this, int index) => @this.CentroidAt(OrientationUtils.AllCardinals[index]);

	static int GetIteratorVersion(Cuboid _) => IteratorVersionNumber;

	/// <summary>
	/// Constructs a new cube (a cuboid with equal <see cref="Width"/>, <see cref="Height"/> and <see cref="Depth"/>).
	/// </summary>
	/// <param name="widthHeightDepth">The size of the cube on all three axes.</param>
	public Cuboid(float widthHeightDepth) : this(widthHeightDepth, widthHeightDepth, widthHeightDepth) { }
	/// <summary>
	/// Constructs a new <see cref="Cuboid"/> with the given dimensions.
	/// </summary>
	/// <param name="width">The size of the cuboid on the X axis.</param>
	/// <param name="height">The size of the cuboid on the Y axis.</param>
	/// <param name="depth">The size of the cuboid on the Z axis.</param>
	public Cuboid(float width, float height, float depth) {
		_halfWidth = width * 0.5f;
		_halfHeight = height * 0.5f;
		_halfDepth = depth * 0.5f;
	}

	/// <summary>
	/// Returns this cuboid's size along <paramref name="axis"/> (i.e. one of <see cref="Width"/>, <see cref="Height"/> or <see cref="Depth"/>).
	/// </summary>
	/// <param name="axis">The axis to query. Must not be <see cref="Axis.None"/>.</param>
	public float GetExtent(Axis axis) => axis switch {
		Axis.X => Width,
		Axis.Y => Height,
		Axis.Z => Depth,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} can not be {nameof(Axis.None)} or non-defined value.")
	};
	/// <summary>
	/// Returns this cuboid's half-size along <paramref name="axis"/> (i.e. one of <see cref="HalfWidth"/>, <see cref="HalfHeight"/> or <see cref="HalfDepth"/>).
	/// </summary>
	/// <param name="axis">The axis to query. Must not be <see cref="Axis.None"/>.</param>
	public float GetHalfExtent(Axis axis) => axis switch {
		Axis.X => HalfWidth,
		Axis.Y => HalfHeight,
		Axis.Z => HalfDepth,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} can not be {nameof(Axis.None)} or non-defined value.")
	};

	/// <summary>
	/// Returns the surface area of the given <paramref name="side"/> of the cuboid.
	/// </summary>
	/// <param name="side">The side to query. Must not be <see cref="CardinalOrientation.None"/>.</param>
	public float GetSideSurfaceArea(CardinalOrientation side) {
		return side.GetAxis() switch {
			Axis.X => HalfHeight * HalfDepth * 4f,
			Axis.Y => HalfDepth * HalfWidth * 4f,
			Axis.Z => HalfWidth * HalfHeight * 4f,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, $"{nameof(CardinalOrientation)} can not be {nameof(CardinalOrientation.None)} or non-defined value.")
		};
	}

	/// <inheritdoc />
	public Location CentroidAt(CardinalOrientation side) {
		if (side == CardinalOrientation.None || !Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side), side, $"Can not be '{nameof(CardinalOrientation.None)}' or non-defined value.");

		return (GetHalfExtent(side.GetAxis()) * side.ToDirection()).AsLocation();
	}

	/// <inheritdoc />
	public Location CornerAt(DiagonalOrientation corner) {
		if (corner == DiagonalOrientation.None || !Enum.IsDefined(corner)) throw new ArgumentOutOfRangeException(nameof(corner), corner, $"Can not be '{nameof(DiagonalOrientation.None)}' or non-defined value.");

		return new(
			corner.GetAxisSign(Axis.X) * HalfWidth,
			corner.GetAxisSign(Axis.Y) * HalfHeight,
			corner.GetAxisSign(Axis.Z) * HalfDepth
		);
	}

	/// <inheritdoc />
	public Plane SideAt(CardinalOrientation side) {
		if (side == CardinalOrientation.None || !Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side), side, $"Can not be '{nameof(CardinalOrientation.None)}' or non-defined value.");

		return new(side.ToDirection(), GetHalfExtent(side.GetAxis()));
	}

	/// <inheritdoc />
	public BoundedRay EdgeAt(IntercardinalOrientation edge) {
		if (edge == IntercardinalOrientation.None || !Enum.IsDefined(edge)) throw new ArgumentOutOfRangeException(nameof(edge), edge, $"Can not be '{nameof(IntercardinalOrientation.None)}' or non-defined value.");

		var unspecifiedAxis = edge.GetUnspecifiedAxis();
		return new(
			CornerAt((DiagonalOrientation) edge.AsGeneralOrientation().WithAxisSign(unspecifiedAxis, -1)),
			CornerAt((DiagonalOrientation) edge.AsGeneralOrientation().WithAxisSign(unspecifiedAxis, 1))
		);
	}

	#region Factories and Conversions
	/// <summary>
	/// Positions this cuboid at <paramref name="position"/>; equivalent to <see cref="ToPositionedCuboid(Location)"/>.
	/// </summary>
	/// <param name="position">The position for the resultant shape.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedCuboid WithPosition(Location position) => ToPositionedCuboid(position);
	/// <summary>
	/// Converts this cuboid to a <see cref="PositionedCuboid"/> centred at <paramref name="position"/>.
	/// </summary>
	/// <param name="position">The position for the resultant shape.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedCuboid ToPositionedCuboid(Location position) => new(this, position);
	/// <summary>
	/// Positions and rotates this cuboid; equivalent to <see cref="ToPositionedRotatedCuboid(Location,Rotation)"/>.
	/// </summary>
	/// <param name="position">The position for the resultant shape.</param>
	/// <param name="rotation">The rotation for the resultant shape.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedRotatedCuboid WithPositionAndRotation(Location position, Rotation rotation) => ToPositionedRotatedCuboid(position, rotation);
	/// <summary>
	/// Converts this cuboid to a <see cref="PositionedRotatedCuboid"/> centred at <paramref name="position"/> and rotated by <paramref name="rotation"/>.
	/// </summary>
	/// <param name="position">The position for the resultant shape.</param>
	/// <param name="rotation">The rotation for the resultant shape.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedRotatedCuboid ToPositionedRotatedCuboid(Location position, Rotation rotation) => new(this, position, rotation);
	/// <summary>
	/// Returns the smallest axis-aligned <see cref="Cuboid"/> that can fully enclose <paramref name="rotatable"/> in any rotation.
	/// </summary>
	/// <param name="rotatable">The cuboid to find the enclosing axis-aligned cuboid for.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Cuboid FromSmallestEnclosingAxisAligned(Cuboid rotatable) => rotatable.SmallestEnclosingSphere.SmallestEnclosingCube;

	/// <summary>
	/// Constructs a new <see cref="Cuboid"/> from its half-dimensions.
	/// </summary>
	/// <param name="halfWidth">Half of the desired <see cref="Width"/>.</param>
	/// <param name="halfHeight">Half of the desired <see cref="Height"/>.</param>
	/// <param name="halfDepth">Half of the desired <see cref="Depth"/>.</param>
	public static Cuboid FromHalfDimensions(float halfWidth, float halfHeight, float halfDepth) {
		return new() {
			HalfWidth = halfWidth,
			HalfHeight = halfHeight,
			HalfDepth = halfDepth
		};
	}
	#endregion

	#region Random
	/// <inheritdoc/>
	public static Cuboid Random() {
		return FromHalfDimensions(
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax)
		);
	}
	/// <inheritdoc/>
	public static Cuboid Random(Cuboid minInclusive, Cuboid maxExclusive) {
		return FromHalfDimensions(
			RandomUtils.NextSingle(minInclusive.HalfWidth, maxExclusive.HalfWidth),
			RandomUtils.NextSingle(minInclusive.HalfHeight, maxExclusive.HalfHeight),
			RandomUtils.NextSingle(minInclusive.HalfDepth, maxExclusive.HalfDepth)
		);
	}
	#endregion

	#region Span Conversions
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 3;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, Cuboid src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.Width);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.Height);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Depth);
	}

	/// <inheritdoc />
	public static Cuboid DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..])
		);
	}
	#endregion

	#region String Conversions
	/// <inheritdoc />
	public override string ToString() => ToString(null, null);
	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Cuboid), (nameof(Width), Width), (nameof(Height), Height), (nameof(Depth), Depth));
	/// <inheritdoc />
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Cuboid), (nameof(Width), Width), (nameof(Height), Height), (nameof(Depth), Depth));

	/// <inheritdoc />
	public static Cuboid Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Cuboid result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static Cuboid Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out float width, out float height, out float depth);
		return new(width, height, depth);
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Cuboid result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out float width, out float height, out float depth)) return false;
		result = new(width, height, depth);
		return true;
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	public bool Equals(Cuboid other) => _halfWidth.Equals(other._halfWidth) && _halfHeight.Equals(other._halfHeight) && _halfDepth.Equals(other._halfDepth);
	/// <inheritdoc />
	public bool Equals(Cuboid other, float tolerance) {
		return MathF.Abs(Width - other.Width) <= tolerance
			&& MathF.Abs(Height - other.Height) <= tolerance
			&& MathF.Abs(Depth - other.Depth) <= tolerance;
	}
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Cuboid other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_halfWidth, _halfHeight, _halfDepth);
	/// <inheritdoc />
	public static bool operator ==(Cuboid left, Cuboid right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(Cuboid left, Cuboid right) => !left.Equals(right);
	#endregion
}