// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct OriginSphere : IFullyInteractableConvexShape<OriginSphere> { // TODO IIntersectable<Plane, Circle>
	internal const float DefaultRandomMin = 1f;
	internal const float DefaultRandomMax = 3f;
	public static readonly OriginSphere UnitSphere = new(1f);

	readonly float _radius;

	public float Radius => _radius;
	public float RadiusSquared => _radius * _radius;

	public OriginSphere(float radius) {
		_radius = radius;
	}

	public float Volume => 2f / 3f * MathF.Tau * Radius * RadiusSquared;
	public float SurfaceArea => 2f * MathF.Tau * RadiusSquared;
	public float Circumference => MathF.Tau * Radius;
	public float Diameter => 2f * Radius;

	#region Factories and Conversions
	public static OriginSphere FromVolume(float volume) => new(MathF.Cbrt(volume / (2f / 3f * MathF.Tau)));
	public static OriginSphere FromSurfaceArea(float surfaceArea) => new(MathF.Sqrt(surfaceArea / (2f * MathF.Tau)));
	public static OriginSphere FromCircumference(float circumference) => new(circumference / MathF.Tau);
	public static OriginSphere FromDiameter(float diameter) => new(diameter * 0.5f);
	public static OriginSphere FromRadiusSquared(float radiusSquared) => new(MathF.Sqrt(radiusSquared));
	#endregion

	#region Span Conversions
	public static ReadOnlySpan<float> ConvertToSpan(in OriginSphere src) => new(in src._radius);
	public static OriginSphere ConvertFromSpan(ReadOnlySpan<float> src) => new(src[0]);
	#endregion

	#region String Conversions
	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(OriginSphere), (nameof(Radius), Radius));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(OriginSphere), (nameof(Radius), Radius));

	public static OriginSphere Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out OriginSphere result) => TryParse(s.AsSpan(), provider, out result);

	public static OriginSphere Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out float radius);
		return new(radius);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out OriginSphere result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out float radius)) return false;
		result = new(radius);
		return true;
	}
	#endregion

	#region Equality
	public bool Equals(OriginSphere other) => _radius.Equals(other._radius);
	public bool Equals(OriginSphere other, float tolerance) => MathF.Abs(Radius - other.Radius) <= tolerance;
	public override bool Equals(object? obj) => obj is OriginSphere other && Equals(other);
	public override int GetHashCode() => _radius.GetHashCode();
	public static bool operator ==(OriginSphere left, OriginSphere right) => left.Equals(right);
	public static bool operator !=(OriginSphere left, OriginSphere right) => !left.Equals(right);
	#endregion
}