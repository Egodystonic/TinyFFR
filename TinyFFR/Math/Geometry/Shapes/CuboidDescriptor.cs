// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR;

public readonly partial struct CuboidDescriptor : IConvexShape<CuboidDescriptor> {
	internal const float DefaultRandomMin = 0.5f;
	internal const float DefaultRandomMax = 1.5f;
	public static readonly CuboidDescriptor UnitCube = new(1f, 1f, 1f);

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

	public unsafe OneToManyEnumerator<CuboidDescriptor, Location> Corners => new(this, &GetCornerCountForEnumerator, &GetCornerForEnumerator);
	static int GetCornerCountForEnumerator(CuboidDescriptor _) => 8;
	static Location GetCornerForEnumerator(CuboidDescriptor @this, int index) => @this.CornerAt(OrientationUtils.AllDiagonals[index]);

	public unsafe OneToManyEnumerator<CuboidDescriptor, BoundedRay> Edges => new(this, &GetEdgeCountForEnumerator, &GetEdgeForEnumerator);
	static int GetEdgeCountForEnumerator(CuboidDescriptor _) => 12;
	static BoundedRay GetEdgeForEnumerator(CuboidDescriptor @this, int index) => @this.EdgeAt(OrientationUtils.AllIntercardinals[index]);

	public unsafe OneToManyEnumerator<CuboidDescriptor, Plane> Sides => new(this, &GetSideCountForEnumerator, &GetSideForEnumerator);
	static int GetSideCountForEnumerator(CuboidDescriptor _) => 6;
	static Plane GetSideForEnumerator(CuboidDescriptor @this, int index) => @this.SideAt(OrientationUtils.AllCardinals[index]);

	public CuboidDescriptor(float width, float height, float depth) {
		_halfWidth = width * 0.5f;
		_halfHeight = height * 0.5f;
		_halfDepth = depth * 0.5f;
	}

	public float GetExtent(Axis axis) => axis switch {
		Axis.X => Width,
		Axis.Y => Height,
		Axis.Z => Depth,
		_ => throw new ArgumentOutOfRangeException($"{nameof(Axis)} can not be {nameof(Axis.None)} or non-defined value.", axis, nameof(axis))
	};
	public float GetHalfExtent(Axis axis) => axis switch {
		Axis.X => HalfWidth,
		Axis.Y => HalfHeight,
		Axis.Z => HalfDepth,
		_ => throw new ArgumentOutOfRangeException($"{nameof(Axis)} can not be {nameof(Axis.None)} or non-defined value.", axis, nameof(axis))
	};

	public float GetSideSurfaceArea(CardinalOrientation3D side) {
		return side.GetAxis() switch {
			Axis.X => HalfHeight * HalfDepth * 4f,
			Axis.Y => HalfDepth * HalfWidth * 4f,
			Axis.Z => HalfWidth * HalfHeight * 4f,
			_ => throw new ArgumentOutOfRangeException($"{nameof(CardinalOrientation3D)} can not be {nameof(CardinalOrientation3D.None)} or non-defined value.", side, nameof(side))
		};
	}

	public Location CornerAt(DiagonalOrientation3D corner) {
		if (corner == DiagonalOrientation3D.None || !Enum.IsDefined(corner)) throw new ArgumentOutOfRangeException(nameof(corner), corner, $"Can not be '{nameof(DiagonalOrientation3D.None)}' or non-defined value.");

		return new(
			corner.GetAxisSign(Axis.X) * HalfWidth,
			corner.GetAxisSign(Axis.Y) * HalfHeight,
			corner.GetAxisSign(Axis.Z) * HalfDepth
		);
	}

	public Plane SideAt(CardinalOrientation3D side) { // TODO xmldoc that the planes' normals point away from the cuboid centre, e.g. side.ToDirection()
		if (side == CardinalOrientation3D.None || !Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side), side, $"Can not be '{nameof(CardinalOrientation3D.None)}' or non-defined value.");

		return new(side.ToDirection(), GetHalfExtent(side.GetAxis()));
	}

	public BoundedRay EdgeAt(IntercardinalOrientation3D edge) {
		if (edge == IntercardinalOrientation3D.None || !Enum.IsDefined(edge)) throw new ArgumentOutOfRangeException(nameof(edge), edge, $"Can not be '{nameof(IntercardinalOrientation3D.None)}' or non-defined value.");

		var unspecifiedAxis = edge.GetUnspecifiedAxis();
		return new(
			CornerAt((DiagonalOrientation3D) edge.AsGeneralOrientation().WithAxisSign(unspecifiedAxis, -1)),
			CornerAt((DiagonalOrientation3D) edge.AsGeneralOrientation().WithAxisSign(unspecifiedAxis, 1))
		);
	}

	#region Factories and Conversions
	public static CuboidDescriptor FromHalfDimensions(float halfWidth, float halfHeight, float halfDepth) {
		return new() {
			HalfWidth = halfWidth,
			HalfHeight = halfHeight,
			HalfDepth = halfDepth
		};
	}
	#endregion

	#region Random
	public static CuboidDescriptor Random() {
		return FromHalfDimensions(
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax)
		);
	}
	public static CuboidDescriptor Random(CuboidDescriptor minInclusive, CuboidDescriptor maxExclusive) {
		return FromHalfDimensions(
			RandomUtils.NextSingle(minInclusive.HalfWidth, maxExclusive.HalfWidth),
			RandomUtils.NextSingle(minInclusive.HalfHeight, maxExclusive.HalfHeight),
			RandomUtils.NextSingle(minInclusive.HalfDepth, maxExclusive.HalfDepth)
		);
	}
	#endregion

	#region Span Conversions
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 3;

	public static void SerializeToBytes(Span<byte> dest, CuboidDescriptor src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.Width);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.Height);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Depth);
	}

	public static CuboidDescriptor DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..])
		);
	}
	#endregion

	#region String Conversions
	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(CuboidDescriptor), (nameof(Width), Width), (nameof(Height), Height), (nameof(Depth), Depth));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(CuboidDescriptor), (nameof(Width), Width), (nameof(Height), Height), (nameof(Depth), Depth));

	public static CuboidDescriptor Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out CuboidDescriptor result) => TryParse(s.AsSpan(), provider, out result);

	public static CuboidDescriptor Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out float width, out float height, out float depth);
		return new(width, height, depth);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out CuboidDescriptor result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out float width, out float height, out float depth)) return false;
		result = new(width, height, depth);
		return true;
	}
	#endregion

	#region Equality
	public bool Equals(CuboidDescriptor other) => _halfWidth.Equals(other._halfWidth) && _halfHeight.Equals(other._halfHeight) && _halfDepth.Equals(other._halfDepth);
	public bool Equals(CuboidDescriptor other, float tolerance) {
		return MathF.Abs(Width - other.Width) <= tolerance
			&& MathF.Abs(Height - other.Height) <= tolerance
			&& MathF.Abs(Depth - other.Depth) <= tolerance;
	}
	public override bool Equals(object? obj) => obj is CuboidDescriptor other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_halfWidth, _halfHeight, _halfDepth);
	public static bool operator ==(CuboidDescriptor left, CuboidDescriptor right) => left.Equals(right);
	public static bool operator !=(CuboidDescriptor left, CuboidDescriptor right) => !left.Equals(right);
	#endregion
}