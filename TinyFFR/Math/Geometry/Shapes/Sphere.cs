// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Trait interface used to mark a type as behaving like a sphere shape.
/// </summary>
public interface ISphere : IConvexShape {
	/// <summary>
	/// The radius of the sphere.
	/// </summary>
	float Radius { get; }
	/// <summary>
	/// The square of <see cref="Radius"/>.
	/// </summary>
	float RadiusSquared { get; }
	/// <summary>
	/// The volume enclosed by the sphere.
	/// </summary>
	float Volume { get; }
	/// <summary>
	/// The total surface area of the sphere.
	/// </summary>
	float SurfaceArea { get; }
	/// <summary>
	/// The circumference of the sphere (the length of any great circle around it).
	/// </summary>
	float Circumference { get; }
	/// <summary>
	/// The diameter of the sphere; equal to <c>2f * <see cref="Radius"/></c>.
	/// </summary>
	float Diameter { get; }
}
/// <summary>
/// Extension of <see cref="ISphere"/> that includes a self type parameter allowing for more functional definitions.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface ISphere<TSelf> : ISphere, IConvexShape<TSelf> where TSelf : ISphere<TSelf> {
	/// <summary>
	/// Attempts to find the circle formed where <paramref name="plane"/> slices through this sphere.
	/// </summary>
	/// <param name="plane">The plane to slice with.</param>
	/// <param name="circleCentrePoint">Set to the centre of the resultant circle, if it exists.</param>
	/// <param name="circleRadius">Set to the radius of the resultant circle, if it exists.</param>
	/// <returns><see langword="true"/> if <paramref name="plane"/> intersects this sphere (in which case <paramref name="circleCentrePoint"/> and <paramref name="circleRadius"/> are populated); <see langword="false"/> otherwise.</returns>
	bool TrySplit(Plane plane, out Location circleCentrePoint, out float circleRadius);
}

/// <summary>
/// Represents a sphere shape, centred on its own local origin.
/// </summary>
public readonly partial struct Sphere : ISphere<Sphere> {
	internal const float DefaultRandomMin = 1f;
	internal const float DefaultRandomMax = 3f;
	/// <summary>
	/// A sphere with <see cref="Radius"/> equal to <c>1f</c>.
	/// </summary>
	public static readonly Sphere UnitSphere = new(1f);
	/// <summary>
	/// A sphere with <see cref="Diameter"/> equal to <c>1f</c>.
	/// </summary>
	public static readonly Sphere OneMeterDiameterSphere = FromDiameter(1f);
	/// <summary>
	/// A sphere with <see cref="Volume"/> equal to <c>1f</c>.
	/// </summary>
	public static readonly Sphere OneMeterCubedVolumeSphere = FromVolume(1f);

	readonly float _radius;

	/// <inheritdoc />
	public float Radius => _radius;
	/// <inheritdoc />
	public float RadiusSquared => _radius * _radius;

	/// <summary>
	/// Constructs a new <see cref="Sphere"/> with the given <paramref name="radius"/>.
	/// </summary>
	/// <param name="radius">The radius of the sphere.</param>
	public Sphere(float radius) {
		_radius = radius;
	}

	/// <inheritdoc />
	public float Volume => 2f / 3f * MathF.Tau * Radius * RadiusSquared;
	/// <inheritdoc />
	public float SurfaceArea => 2f * MathF.Tau * RadiusSquared;
	/// <inheritdoc />
	public float Circumference => MathF.Tau * Radius;
	/// <inheritdoc />
	public float Diameter => 2f * Radius;

	#region Factories and Conversions
	/// <summary>
	/// Constructs the <see cref="Sphere"/> with the given <see cref="Volume"/>.
	/// </summary>
	/// <param name="volume">The desired volume. Must be non-negative.</param>
	public static Sphere FromVolume(float volume) => new(MathF.Cbrt(volume / (2f / 3f * MathF.Tau)));
	/// <summary>
	/// Constructs the <see cref="Sphere"/> with the given <see cref="SurfaceArea"/>.
	/// </summary>
	/// <param name="surfaceArea">The desired surface area. Must be non-negative.</param>
	public static Sphere FromSurfaceArea(float surfaceArea) => new(MathF.Sqrt(surfaceArea / (2f * MathF.Tau)));
	/// <summary>
	/// Constructs the <see cref="Sphere"/> with the given <see cref="Circumference"/>.
	/// </summary>
	/// <param name="circumference">The desired circumference. Must be non-negative.</param>
	public static Sphere FromCircumference(float circumference) => new(circumference / MathF.Tau);
	/// <summary>
	/// Constructs the <see cref="Sphere"/> with the given <see cref="Diameter"/>.
	/// </summary>
	/// <param name="diameter">The desired diameter. Must be non-negative.</param>
	public static Sphere FromDiameter(float diameter) => new(diameter * 0.5f);
	/// <summary>
	/// Constructs the <see cref="Sphere"/> with the given <see cref="RadiusSquared"/>.
	/// </summary>
	/// <param name="radiusSquared">The desired square of the radius. Must be non-negative.</param>
	public static Sphere FromRadiusSquared(float radiusSquared) => new(MathF.Sqrt(radiusSquared));

	/// <summary>
	/// Positions this sphere at <paramref name="position"/>; equivalent to <see cref="ToPositionedSphere(Location)"/>.
	/// </summary>
	/// <param name="position">The position for the resultant shape.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedSphere WithPosition(Location position) => ToPositionedSphere(position);
	/// <summary>
	/// Converts this sphere to a <see cref="PositionedSphere"/> centred at <paramref name="position"/>.
	/// </summary>
	/// <param name="position">The position for the resultant shape.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedSphere ToPositionedSphere(Location position) => new(this, position);
	#endregion

	#region Random
	/// <inheritdoc/>
	public static Sphere Random() => new(RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax));
	/// <inheritdoc/>
	public static Sphere Random(Sphere minInclusive, Sphere maxExclusive) => new(RandomUtils.NextSingle(minInclusive.Radius, maxExclusive.Radius));
	#endregion

	#region Span Conversions
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = sizeof(float);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, Sphere src) => BinaryPrimitives.WriteSingleLittleEndian(dest, src._radius);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Sphere DeserializeFromBytes(ReadOnlySpan<byte> src) => new(BinaryPrimitives.ReadSingleLittleEndian(src));
	#endregion

	#region String Conversions
	/// <inheritdoc />
	public override string ToString() => ToString(null, null);
	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Sphere), (nameof(Radius), Radius));
	/// <inheritdoc />
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Sphere), (nameof(Radius), Radius));

	/// <inheritdoc />
	public static Sphere Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Sphere result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static Sphere Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out float radius);
		return new(radius);
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Sphere result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out float radius)) return false;
		result = new(radius);
		return true;
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	public bool Equals(Sphere other) => _radius.Equals(other._radius);
	/// <inheritdoc />
	public bool Equals(Sphere other, float tolerance) => MathF.Abs(Radius - other.Radius) <= tolerance;
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Sphere other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => _radius.GetHashCode();
	/// <inheritdoc />
	public static bool operator ==(Sphere left, Sphere right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(Sphere left, Sphere right) => !left.Equals(right);
	#endregion
}