// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct Cuboid : IShape<Cuboid> {
	internal const float DefaultRandomMin = 1f;
	internal const float DefaultRandomMax = 3f;
	public static readonly Cuboid UnitCube = new(1f, 1f, 1f);

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

	public float Volume {
		get => HalfWidth * HalfHeight * HalfDepth * 8f;
		init {
			var diffCubeRoot = MathF.Cbrt(value / Volume);
			_halfWidth *= diffCubeRoot;
			_halfHeight *= diffCubeRoot;
			_halfDepth *= diffCubeRoot;
		}
	}

	public float SurfaceArea {
		get => (Width * Height + Height * Depth + Depth * Width) * 2f;
		init {
			var diffSquareRoot = MathF.Sqrt(value / SurfaceArea);
			_halfWidth *= diffSquareRoot;
			_halfHeight *= diffSquareRoot;
			_halfDepth *= diffSquareRoot;
		}
	}

	public Location this[Orientation3D orientationFromCentrePoint] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => GetSurfaceLocation(orientationFromCentrePoint);
	}
	public float this[Axis dimensionAxis] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => GetDimension(dimensionAxis);
	}

	public Cuboid(float width, float height, float depth) {
		_halfWidth = width * 0.5f;
		_halfHeight = height * 0.5f;
		_halfDepth = depth * 0.5f;
	}

	public static Cuboid FromHalfDimensions(float halfWidth, float halfHeight, float halfDepth) {
		return new() {
			HalfWidth = halfWidth,
			HalfHeight = halfHeight,
			HalfDepth = halfDepth
		};
	}

	public float GetDimension(Axis axis) => axis switch {
		Axis.X => Width,
		Axis.Y => Height,
		Axis.Z => Depth,
		_ => throw new ArgumentException($"{nameof(Axis)} can not be {nameof(Axis.None)}.", nameof(axis))
	};

	public Location GetSurfaceLocation(Orientation3D orientationFromCentrePoint) {
		return new(
			_halfWidth * orientationFromCentrePoint.GetAxisSign(Axis.X),
			_halfHeight * orientationFromCentrePoint.GetAxisSign(Axis.Y),
			_halfDepth * orientationFromCentrePoint.GetAxisSign(Axis.Z)
		);
	}
	public float GetSideSurfaceArea(CardinalOrientation3D side) {
		return side.GetAxis() switch {
			Axis.X => HalfHeight * HalfDepth * 4f,
			Axis.Y => HalfDepth * HalfWidth * 4f,
			Axis.Z => HalfWidth * HalfHeight * 4f,
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
	public static Cuboid ConvertFromSpan(ReadOnlySpan<float> src) => MemoryMarshal.Cast<float, Cuboid>(src)[0];

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