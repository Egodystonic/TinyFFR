// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly struct Sphere : IShape<Sphere> {
	internal const float DefaultRandomMin = 1f;
	internal const float DefaultRandomMax = 3f;
	public static readonly Sphere UnitSphere = new(1f);

	readonly float _radius;

	public float Radius => _radius;

	public Sphere(float radius) => _radius = radius;

	public float Volume => 2f / 3f * MathF.Tau * Radius * Radius * Radius;
	public float SurfaceArea => 2f * MathF.Tau * Radius * Radius;
	public float Circumference => MathF.Tau * Radius;
	public float Diameter => 2f * Radius;

	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(Sphere), (nameof(Radius), Radius));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Sphere), (nameof(Radius), Radius));

	public static Sphere Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Sphere result) => TryParse(s.AsSpan(), provider, out result);

	public static Sphere Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out float radius);
		return new(radius);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Sphere result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out float radius)) return false;
		result = new(radius);
		return true;
	}

	public static ReadOnlySpan<float> ConvertToSpan(in Sphere src) => new(in src._radius);
	public static Sphere ConvertFromSpan(ReadOnlySpan<float> src) => new(src[0]);

	public static Sphere Interpolate(Sphere start, Sphere end, float distance) => new(Single.Lerp(start.Radius, end.Radius, distance));

	public static Sphere CreateNewRandom() => new(RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax));
	public static Sphere CreateNewRandom(Sphere minInclusive, Sphere maxExclusive) => new(RandomUtils.NextSingle(minInclusive.Radius, maxExclusive.Radius));

	#region Equality
	public bool Equals(Sphere other) => Radius.Equals(other.Radius);
	public bool Equals(Sphere other, float tolerance) => MathF.Abs(Radius - other.Radius) <= tolerance;
	public override bool Equals(object? obj) => obj is Sphere other && Equals(other);
	public override int GetHashCode() => Radius.GetHashCode();
	public static bool operator ==(Sphere left, Sphere right) => left.Equals(right);
	public static bool operator !=(Sphere left, Sphere right) => !left.Equals(right);
	#endregion
}