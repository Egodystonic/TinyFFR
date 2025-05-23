﻿// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR;

public readonly partial struct Cuboid : IConvexShape<Cuboid> {
	internal const float DefaultRandomMin = 0.5f;
	internal const float DefaultRandomMax = 1.5f;
	public static readonly Cuboid UnitCube = new(1f);
	const int IteratorVersionNumber = 0;

	readonly float _halfWidth;
	readonly float _halfHeight;
	readonly float _halfDepth;

	public float HalfWidth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfWidth;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfWidth = value;
	}
	public float HalfHeight {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfHeight;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfHeight = value;
	}
	public float HalfDepth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfDepth;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfDepth = value;
	}

	public float Width {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfWidth * 2f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfWidth = value * 0.5f;
	}
	public float Height {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfHeight * 2f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfHeight = value * 0.5f;
	}
	public float Depth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _halfDepth * 2f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _halfDepth = value * 0.5f;
	}

	public float Volume => HalfWidth * HalfHeight * HalfDepth * 8f;
	public float SurfaceArea => (Width * Height + Height * Depth + Depth * Width) * 2f;

	public unsafe TypedReferentIterator<Cuboid, Location> Corners => new(this, IteratorVersionNumber, &GetCornerCountForEnumerator, &GetIteratorVersion, &GetCornerForEnumerator);
	static int GetCornerCountForEnumerator(Cuboid _) => 8;
	static Location GetCornerForEnumerator(Cuboid @this, int index) => @this.CornerAt(OrientationUtils.AllDiagonals[index]);

	public unsafe TypedReferentIterator<Cuboid, BoundedRay> Edges => new(this, IteratorVersionNumber, &GetEdgeCountForEnumerator, &GetIteratorVersion, &GetEdgeForEnumerator);
	static int GetEdgeCountForEnumerator(Cuboid _) => 12;
	static BoundedRay GetEdgeForEnumerator(Cuboid @this, int index) => @this.EdgeAt(OrientationUtils.AllIntercardinals[index]);

	public unsafe TypedReferentIterator<Cuboid, Plane> Sides => new(this, IteratorVersionNumber, &GetSideCountForEnumerator, &GetIteratorVersion, &GetSideForEnumerator);
	static int GetSideCountForEnumerator(Cuboid _) => 6;
	static Plane GetSideForEnumerator(Cuboid @this, int index) => @this.SideAt(OrientationUtils.AllCardinals[index]);

	public unsafe TypedReferentIterator<Cuboid, Location> Centroids => new(this, IteratorVersionNumber, &GetCentroidCountForEnumerator, &GetIteratorVersion, &GetCentroidForEnumerator);
	static int GetCentroidCountForEnumerator(Cuboid _) => 6;
	static Location GetCentroidForEnumerator(Cuboid @this, int index) => @this.CentroidAt(OrientationUtils.AllCardinals[index]);

	static int GetIteratorVersion(Cuboid _) => IteratorVersionNumber;

	public Cuboid(float widthHeightDepth) : this(widthHeightDepth, widthHeightDepth, widthHeightDepth) { }
	public Cuboid(float width, float height, float depth) {
		_halfWidth = width * 0.5f;
		_halfHeight = height * 0.5f;
		_halfDepth = depth * 0.5f;
	}

	public float GetExtent(Axis axis) => axis switch {
		Axis.X => Width,
		Axis.Y => Height,
		Axis.Z => Depth,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} can not be {nameof(Axis.None)} or non-defined value.")
	};
	public float GetHalfExtent(Axis axis) => axis switch {
		Axis.X => HalfWidth,
		Axis.Y => HalfHeight,
		Axis.Z => HalfDepth,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} can not be {nameof(Axis.None)} or non-defined value.")
	};

	public float GetSideSurfaceArea(CardinalOrientation side) {
		return side.GetAxis() switch {
			Axis.X => HalfHeight * HalfDepth * 4f,
			Axis.Y => HalfDepth * HalfWidth * 4f,
			Axis.Z => HalfWidth * HalfHeight * 4f,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, $"{nameof(CardinalOrientation)} can not be {nameof(CardinalOrientation.None)} or non-defined value.")
		};
	}

	public Location CentroidAt(CardinalOrientation side) {
		if (side == CardinalOrientation.None || !Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side), side, $"Can not be '{nameof(CardinalOrientation.None)}' or non-defined value.");

		return (GetHalfExtent(side.GetAxis()) * side.ToDirection()).AsLocation();
	}

	public Location CornerAt(DiagonalOrientation corner) {
		if (corner == DiagonalOrientation.None || !Enum.IsDefined(corner)) throw new ArgumentOutOfRangeException(nameof(corner), corner, $"Can not be '{nameof(DiagonalOrientation.None)}' or non-defined value.");

		return new(
			corner.GetAxisSign(Axis.X) * HalfWidth,
			corner.GetAxisSign(Axis.Y) * HalfHeight,
			corner.GetAxisSign(Axis.Z) * HalfDepth
		);
	}

	public Plane SideAt(CardinalOrientation side) { // TODO xmldoc that the planes' normals point away from the cuboid centre, e.g. side.ToDirection()
		if (side == CardinalOrientation.None || !Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side), side, $"Can not be '{nameof(CardinalOrientation.None)}' or non-defined value.");

		return new(side.ToDirection(), GetHalfExtent(side.GetAxis()));
	}

	public BoundedRay EdgeAt(IntercardinalOrientation edge) {
		if (edge == IntercardinalOrientation.None || !Enum.IsDefined(edge)) throw new ArgumentOutOfRangeException(nameof(edge), edge, $"Can not be '{nameof(IntercardinalOrientation.None)}' or non-defined value.");

		var unspecifiedAxis = edge.GetUnspecifiedAxis();
		return new(
			CornerAt((DiagonalOrientation) edge.AsGeneralOrientation().WithAxisSign(unspecifiedAxis, -1)),
			CornerAt((DiagonalOrientation) edge.AsGeneralOrientation().WithAxisSign(unspecifiedAxis, 1))
		);
	}

	#region Factories and Conversions
	public static Cuboid FromHalfDimensions(float halfWidth, float halfHeight, float halfDepth) {
		return new() {
			HalfWidth = halfWidth,
			HalfHeight = halfHeight,
			HalfDepth = halfDepth
		};
	}
	#endregion

	#region Random
	public static Cuboid Random() {
		return FromHalfDimensions(
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax)
		);
	}
	public static Cuboid Random(Cuboid minInclusive, Cuboid maxExclusive) {
		return FromHalfDimensions(
			RandomUtils.NextSingle(minInclusive.HalfWidth, maxExclusive.HalfWidth),
			RandomUtils.NextSingle(minInclusive.HalfHeight, maxExclusive.HalfHeight),
			RandomUtils.NextSingle(minInclusive.HalfDepth, maxExclusive.HalfDepth)
		);
	}
	#endregion

	#region Span Conversions
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 3;

	public static void SerializeToBytes(Span<byte> dest, Cuboid src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.Width);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.Height);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Depth);
	}

	public static Cuboid DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..])
		);
	}
	#endregion

	#region String Conversions
	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Cuboid), (nameof(Width), Width), (nameof(Height), Height), (nameof(Depth), Depth));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Cuboid), (nameof(Width), Width), (nameof(Height), Height), (nameof(Depth), Depth));

	public static Cuboid Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Cuboid result) => TryParse(s.AsSpan(), provider, out result);

	public static Cuboid Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out float width, out float height, out float depth);
		return new(width, height, depth);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Cuboid result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out float width, out float height, out float depth)) return false;
		result = new(width, height, depth);
		return true;
	}
	#endregion

	#region Equality
	public bool Equals(Cuboid other) => _halfWidth.Equals(other._halfWidth) && _halfHeight.Equals(other._halfHeight) && _halfDepth.Equals(other._halfDepth);
	public bool Equals(Cuboid other, float tolerance) {
		return MathF.Abs(Width - other.Width) <= tolerance
			&& MathF.Abs(Height - other.Height) <= tolerance
			&& MathF.Abs(Depth - other.Depth) <= tolerance;
	}
	public override bool Equals(object? obj) => obj is Cuboid other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_halfWidth, _halfHeight, _halfDepth);
	public static bool operator ==(Cuboid left, Cuboid right) => left.Equals(right);
	public static bool operator !=(Cuboid left, Cuboid right) => !left.Equals(right);
	#endregion
}