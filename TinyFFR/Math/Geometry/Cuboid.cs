// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly struct Cuboid : IShape<Cuboid> {
	const float DefaultRandomMin = 1f;
	const float DefaultRandomMax = 3f;
	public static readonly Cuboid UnitCube = new(1f, 1f, 1f);

	readonly float _width;
	readonly float _height;
	readonly float _depth;

	public float Width => _width;
	public float Height => _height;
	public float Depth => _depth;

	public float Volume {
		get => Width * Height * Depth;
	}

	public float SurfaceArea {
		get => (Width * Height + Height * Depth + Depth * Width) * 2f;
	}

	public Location this[DiagonalOrientation3D diagonal] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => GetCornerLocation(diagonal);
	}
	public float this[Axis axis] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => GetDimension(axis);
	}

	public Cuboid(float width, float height, float depth) {
		_width = width;
		_height = height;
		_depth = depth;
	}

	public float GetDimension(Axis axis) => axis switch {
		Axis.X => Width,
		Axis.Y => Height,
		Axis.Z => Depth,
		_ => throw new ArgumentException($"{nameof(Axis)} can not be {nameof(Axis.None)}.", nameof(axis))
	};

	public Location GetCornerLocation(DiagonalOrientation3D diagonal) {
		return new(
			this[Axis.X] * 0.5f * diagonal.AsGeneralOrientation().GetAxisSign(Axis.X),
			this[Axis.Y] * 0.5f * diagonal.AsGeneralOrientation().GetAxisSign(Axis.Y),
			this[Axis.Z] * 0.5f * diagonal.AsGeneralOrientation().GetAxisSign(Axis.Z)
		);
	}
	public float GetSideSurfaceArea(CardinalOrientation3D side) {
		return side.GetAxis() switch {
			Axis.X => Height * Depth,
			Axis.Y => Depth * Width,
			Axis.Z => Width * Height,
			_ => throw new ArgumentException($"{nameof(CardinalOrientation3D)} can not be {nameof(CardinalOrientation3D.None)}.", nameof(side))
		};
	}

	public static Cuboid Interpolate(Cuboid start, Cuboid end, float distance) {
		return new(
			Single.Lerp(start.Width, end.Width, distance),
			Single.Lerp(start.Height, end.Height, distance),
			Single.Lerp(start.Depth, end.Depth, distance)
		);
	}

	public static Cuboid CreateNewRandom() {
		return new(
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax)
		);
	}
	public static Cuboid CreateNewRandom(Cuboid minInclusive, Cuboid maxExclusive) {
		return new(
			RandomUtils.NextSingle(minInclusive.Width, maxExclusive.Width),
			RandomUtils.NextSingle(minInclusive.Height, maxExclusive.Height),
			RandomUtils.NextSingle(minInclusive.Depth, maxExclusive.Depth)
		);
	}

	public static ReadOnlySpan<float> ConvertToSpan(in Cuboid src) => MemoryMarshal.Cast<Cuboid, float>(new ReadOnlySpan<Cuboid>(in src));
	public static Cuboid ConvertFromSpan(ReadOnlySpan<float> src) => new(src[0], src[1], src[2]);

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

	#region Equality
	public bool Equals(Cuboid other) => Width.Equals(other.Width) && Height.Equals(other.Height) && Depth.Equals(other.Depth);
	public bool Equals(Cuboid other, float tolerance) {
		return MathF.Abs(Width - other.Width) <= tolerance
			&& MathF.Abs(Height - other.Height) <= tolerance
			&& MathF.Abs(Depth - other.Depth) <= tolerance;
	}
	public override bool Equals(object? obj) => obj is Cuboid other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(Width, Height, Depth);
	public static bool operator ==(Cuboid left, Cuboid right) => left.Equals(right);
	public static bool operator !=(Cuboid left, Cuboid right) => !left.Equals(right);
	#endregion
}