// Created on 2023-10-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float), Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from float
public readonly partial struct Fraction : IMathPrimitive<Fraction> {
	public const string StringSuffix = " %";
	public static readonly Fraction Full = new(1f);
	public static readonly Fraction FullInverse = new(-1f);
	public static readonly Fraction Zero = new(0f);

	readonly float _asCoefficient;

	public float AsCoefficient {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _asCoefficient;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _asCoefficient = value;
	}
	public float AsPercentage {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsCoefficient * 100f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsCoefficient = value * 0.01f;
	}

	public Fraction(float coefficient) => AsCoefficient = coefficient;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction FromCoefficient(float coefficient) => new() { AsCoefficient = coefficient };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction FromRatio(float numerator, float denominator) => new() { AsCoefficient = MathF.Abs(denominator) >= 0.0000001f ? numerator / denominator : 0f };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction FromPercentage(float percentage) => new() { AsPercentage = percentage };

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Fraction(float operand) => FromCoefficient(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator float(Fraction operand) => operand.AsCoefficient;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Fraction src) => new(src._asCoefficient);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction ConvertFromSpan(ReadOnlySpan<float> src) => FromCoefficient(src[0]);

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => $"{AsPercentage.ToString(format, formatProvider)}{StringSuffix}";

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		writeSuccess = AsPercentage.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite($"{StringSuffix}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		return writeSuccess;
	}

	public static Fraction Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Fraction result) => TryParse(s.AsSpan(), provider, out result);

	public static Fraction Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		var indexOfSuffix = s.IndexOf(StringSuffix);

		var percentage = indexOfSuffix >= 0
			? Single.Parse(s[..indexOfSuffix], provider)
			: Single.Parse(s, provider);

		return FromPercentage(percentage);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Fraction result) {
		var indexOfSuffix = s.IndexOf(StringSuffix);
		if (indexOfSuffix < 0) indexOfSuffix = s.Length;

		if (!Single.TryParse(s[..indexOfSuffix], provider, out var percentage)) {
			result = default;
			return false;
		}

		result = FromPercentage(percentage);
		return true;
	}

	public bool Equals(Fraction other, float tolerance) => MathF.Abs(_asCoefficient - other._asCoefficient) <= tolerance;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Fraction other) => _asCoefficient.Equals(other._asCoefficient);
	public override bool Equals(object? obj) => obj is Fraction other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => _asCoefficient.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Fraction left, Fraction right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Fraction left, Fraction right) => !left.Equals(right);
}