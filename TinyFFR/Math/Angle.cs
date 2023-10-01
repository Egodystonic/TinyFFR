// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float), Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from float
public readonly partial struct Angle : IMathPrimitive<Angle> {
	public const string StringSuffix = "°";
	const float Tau = MathF.Tau;
	const float TauReciprocal = 1f / MathF.Tau;
	const float RadiansToDegreesRatio = 360f / Tau;
	const float DegreesToRadiansRatio = Tau / 360f;
	public static readonly Angle None = FromRadians(0f);
	public static readonly Angle QuarterCircle = FromRadians(Tau * 0.25f);
	public static readonly Angle HalfCircle = FromRadians(Tau * 0.5f);
	public static readonly Angle ThreeQuarterCircle = FromRadians(Tau * 0.75f);
	public static readonly Angle FullCircle = FromRadians(Tau * 1f);

	readonly float _asRadians;

	public float Radians {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _asRadians;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _asRadians = value;
	}
	public float Degrees {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Radians * RadiansToDegreesRatio;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => Radians = value * DegreesToRadiansRatio;
	}
	public float CoefficientOfFullCircle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Radians * TauReciprocal;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => Radians = Tau * value;
	}

	public Angle(float fullCircleCoefficient) => CoefficientOfFullCircle = fullCircleCoefficient;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromRadians(float radians) => new() { Radians = radians };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromDegrees(float degrees) => new() { Degrees = degrees };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromCoefficientOfFullCircle(float fullCircleCoefficient) => new(fullCircleCoefficient);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromCoefficientOfFullCircle(Fraction fullCircleFraction) => FromCoefficientOfFullCircle(fullCircleFraction.AsCoefficient);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Angle(float operand) => FromCoefficientOfFullCircle(operand);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Angle src) => new(src._asRadians);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle ConvertFromSpan(ReadOnlySpan<float> src) => FromRadians(src[0]);

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => $"{Degrees.ToString(format, formatProvider)}{StringSuffix}";

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		writeSuccess = Degrees.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite($"{StringSuffix}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		return writeSuccess;
	}

	public static Angle Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Angle result) => TryParse(s.AsSpan(), provider, out result);

	public static Angle Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		var indexOfSuffix = s.IndexOf(StringSuffix);

		var degrees = indexOfSuffix >= 0
			? Single.Parse(s[..indexOfSuffix], provider)
			: Single.Parse(s, provider);

		return FromDegrees(degrees);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Angle result) {
		var indexOfSuffix = s.IndexOf(StringSuffix);
		if (indexOfSuffix < 0) indexOfSuffix = s.Length;

		if (!Single.TryParse(s[..indexOfSuffix], provider, out var degrees)) {
			result = default;
			return false;
		}

		result = FromDegrees(degrees);
		return true;
	}

	public bool Equals(Angle other, float tolerance) {
		// Using CoefficientOfFullCircle rather than _asRadians because the implicit conversion from float to Angle
		// assumes it's a coefficient and therefore I feel like the tolerance value here should also be a coefficient
		return MathF.Abs(CoefficientOfFullCircle - other.CoefficientOfFullCircle) <= tolerance;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Angle other) => _asRadians.Equals(other._asRadians);
	public override bool Equals(object? obj) => obj is Angle other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => _asRadians.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Angle left, Angle right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Angle left, Angle right) => !left.Equals(right);
}