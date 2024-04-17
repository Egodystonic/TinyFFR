// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct OriginCuboid : IFullyInteractableConvexShape<OriginCuboid> {  // TODO IIntersectable<Plane, BoundedPlane or similar>
	internal const float DefaultRandomMin = 0.5f;
	internal const float DefaultRandomMax = 1.5f;
	public static readonly OriginCuboid UnitCube = new(1f, 1f, 1f);

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

	public OriginCuboid(float width, float height, float depth) {
		_halfWidth = width * 0.5f;
		_halfHeight = height * 0.5f;
		_halfDepth = depth * 0.5f;
	}

	public static OriginCuboid FromHalfDimensions(float halfWidth, float halfHeight, float halfDepth) {
		return new() {
			HalfWidth = halfWidth,
			HalfHeight = halfHeight,
			HalfDepth = halfDepth
		};
	}

	public float GetExtent(Axis axis) => axis switch { // TODO rename this and below to get extent? Probably because it's actually an extent (e.g. it's just a measurement, not any actual point)
		Axis.X => Width,
		Axis.Y => Height,
		Axis.Z => Depth,
		_ => throw new ArgumentException($"{nameof(Axis)} can not be {nameof(Axis.None)}.", nameof(axis))
	};
	public float GetHalfExtent(Axis axis) => axis switch {
		Axis.X => HalfWidth,
		Axis.Y => HalfHeight,
		Axis.Z => HalfDepth,
		_ => throw new ArgumentException($"{nameof(Axis)} can not be {nameof(Axis.None)}.", nameof(axis))
	};

	public float GetSideSurfaceArea(CardinalOrientation3D side) {
		return side.GetAxis() switch {
			Axis.X => HalfHeight * HalfDepth * 4f,
			Axis.Y => HalfDepth * HalfWidth * 4f,
			Axis.Z => HalfWidth * HalfHeight * 4f,
			_ => throw new ArgumentException($"{nameof(CardinalOrientation3D)} can not be {nameof(CardinalOrientation3D.None)}.", nameof(side))
		};
	}

	public static OriginCuboid Interpolate(OriginCuboid start, OriginCuboid end, float distance) {
		return FromHalfDimensions(
			Single.Lerp(start.HalfWidth, end.HalfWidth, distance),
			Single.Lerp(start.HalfHeight, end.HalfHeight, distance),
			Single.Lerp(start.HalfDepth, end.HalfDepth, distance)
		);
	}

	public static OriginCuboid CreateNewRandom() {
		return FromHalfDimensions(
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax)
		);
	}
	public static OriginCuboid CreateNewRandom(OriginCuboid minInclusive, OriginCuboid maxExclusive) {
		return FromHalfDimensions(
			RandomUtils.NextSingle(minInclusive.HalfWidth, maxExclusive.HalfWidth),
			RandomUtils.NextSingle(minInclusive.HalfHeight, maxExclusive.HalfHeight),
			RandomUtils.NextSingle(minInclusive.HalfDepth, maxExclusive.HalfDepth)
		);
	}

	public static ReadOnlySpan<float> ConvertToSpan(in OriginCuboid src) => MemoryMarshal.Cast<OriginCuboid, float>(new ReadOnlySpan<OriginCuboid>(in src));
	public static OriginCuboid ConvertFromSpan(ReadOnlySpan<float> src) => MemoryMarshal.Cast<float, OriginCuboid>(src)[0];

	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(OriginCuboid), (nameof(Width), Width), (nameof(Height), Height), (nameof(Depth), Depth));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(OriginCuboid), (nameof(Width), Width), (nameof(Height), Height), (nameof(Depth), Depth));

	public static OriginCuboid Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out OriginCuboid result) => TryParse(s.AsSpan(), provider, out result);

	public static OriginCuboid Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out float width, out float height, out float depth);
		return new(width, height, depth);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out OriginCuboid result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out float width, out float height, out float depth)) return false;
		result = new(width, height, depth);
		return true;
	}

	#region Equality
	public bool Equals(OriginCuboid other) => Width.Equals(other.Width) && Height.Equals(other.Height) && Depth.Equals(other.Depth);
	public bool Equals(OriginCuboid other, float tolerance) {
		return MathF.Abs(Width - other.Width) <= tolerance
			&& MathF.Abs(Height - other.Height) <= tolerance
			&& MathF.Abs(Depth - other.Depth) <= tolerance;
	}
	public override bool Equals(object? obj) => obj is OriginCuboid other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(Width, Height, Depth);
	public static bool operator ==(OriginCuboid left, OriginCuboid right) => left.Equals(right);
	public static bool operator !=(OriginCuboid left, OriginCuboid right) => !left.Equals(right);
	#endregion
}