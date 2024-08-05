// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;

namespace Egodystonic.TinyFFR;

public readonly partial struct SphereDescriptor : IConvexShape<SphereDescriptor> {
	internal const float DefaultRandomMin = 1f;
	internal const float DefaultRandomMax = 3f;
	public static readonly SphereDescriptor UnitSphere = new(1f);

	readonly float _radius;

	public float Radius => _radius;
	public float RadiusSquared => _radius * _radius;

	public SphereDescriptor(float radius) {
		_radius = radius;
	}

	public float Volume => 2f / 3f * MathF.Tau * Radius * RadiusSquared;
	public float SurfaceArea => 2f * MathF.Tau * RadiusSquared;
	public float Circumference => MathF.Tau * Radius;
	public float Diameter => 2f * Radius;

	#region Factories and Conversions
	public static SphereDescriptor FromVolume(float volume) => new(MathF.Cbrt(volume / (2f / 3f * MathF.Tau)));
	public static SphereDescriptor FromSurfaceArea(float surfaceArea) => new(MathF.Sqrt(surfaceArea / (2f * MathF.Tau)));
	public static SphereDescriptor FromCircumference(float circumference) => new(circumference / MathF.Tau);
	public static SphereDescriptor FromDiameter(float diameter) => new(diameter * 0.5f);
	public static SphereDescriptor FromRadiusSquared(float radiusSquared) => new(MathF.Sqrt(radiusSquared));
	#endregion

	#region Random
	public static SphereDescriptor Random() => new(RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax));
	public static SphereDescriptor Random(SphereDescriptor minInclusive, SphereDescriptor maxExclusive) => new(RandomUtils.NextSingle(minInclusive.Radius, maxExclusive.Radius));
	#endregion

	#region Span Conversions
	public static int SerializationByteSpanLength { get; } = sizeof(float);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, SphereDescriptor src) => BinaryPrimitives.WriteSingleLittleEndian(dest, src._radius);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SphereDescriptor DeserializeFromBytes(ReadOnlySpan<byte> src) => new(BinaryPrimitives.ReadSingleLittleEndian(src));
	#endregion

	#region String Conversions
	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(SphereDescriptor), (nameof(Radius), Radius));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(SphereDescriptor), (nameof(Radius), Radius));

	public static SphereDescriptor Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out SphereDescriptor result) => TryParse(s.AsSpan(), provider, out result);

	public static SphereDescriptor Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out float radius);
		return new(radius);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out SphereDescriptor result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out float radius)) return false;
		result = new(radius);
		return true;
	}
	#endregion

	#region Equality
	public bool Equals(SphereDescriptor other) => _radius.Equals(other._radius);
	public bool Equals(SphereDescriptor other, float tolerance) => MathF.Abs(Radius - other.Radius) <= tolerance;
	public override bool Equals(object? obj) => obj is SphereDescriptor other && Equals(other);
	public override int GetHashCode() => _radius.GetHashCode();
	public static bool operator ==(SphereDescriptor left, SphereDescriptor right) => left.Equals(right);
	public static bool operator !=(SphereDescriptor left, SphereDescriptor right) => !left.Equals(right);
	#endregion
}