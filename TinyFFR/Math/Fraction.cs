// Created on 2023-10-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float), Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from float
public readonly partial struct Fraction : IMathPrimitive<Fraction> {
	public const string StringSuffix = " %";
	public static readonly Fraction Full = new(1f);
	public static readonly Fraction FullNegative = new(-1f);
	public static readonly Fraction Zero = new(0f);

	readonly float _asDecimal;

	public float AsDecimal {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _asDecimal;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _asDecimal = value;
	}
	public float AsPercentage {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsDecimal * 100f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsDecimal = value * 0.01f;
	}

	public Fraction(float @decimal) => AsDecimal = @decimal;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction FromDecimal(float @decimal) => new() { AsDecimal = @decimal };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction FromRatio(float numerator, float denominator) => new() { AsDecimal = MathF.Abs(denominator) >= 0.0000001f ? numerator / denominator : 0f };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction FromPercentage(float percentage) => new() { AsPercentage = percentage };

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Fraction(float operand) => FromDecimal(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator float(Fraction operand) => operand.AsDecimal;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Fraction src) => new(src._asDecimal);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction ConvertFromSpan(ReadOnlySpan<float> src) => FromDecimal(src[0]);

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

	public static Fraction Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Fraction result) => TryParse(s.AsSpan(), provider, out result);

	public static Fraction Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
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

	public bool Equals(Fraction other, float tolerance) => MathF.Abs(_asDecimal - other._asDecimal) <= tolerance;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Fraction other) => _asDecimal.Equals(other._asDecimal);
	public override bool Equals(object? obj) => obj is Fraction other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => _asDecimal.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Fraction left, Fraction right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Fraction left, Fraction right) => !left.Equals(right);
}