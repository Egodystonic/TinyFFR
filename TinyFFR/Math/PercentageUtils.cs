// Created on 2023-10-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

public static class PercentageUtils {
	public const string StringSuffix = "%";

	public static string ConvertFractionToPercentageString(float fraction, string? format = null, IFormatProvider? formatProvider = null) {
		return $"{(fraction * 100f).ToString(format, formatProvider)}{StringSuffix}";
	}

	public static bool TryFormatFractionToPercentageString(float fraction, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		writeSuccess = (fraction * 100f).TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite($"{StringSuffix}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		return writeSuccess;
	}

	public static float ParsePercentageStringToFraction(string s, IFormatProvider? provider = null) => ParsePercentageStringToFraction(s.AsSpan(), provider);
	public static float ParsePercentageStringToFraction(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var indexOfSuffix = s.IndexOf(StringSuffix);

		var percentage = indexOfSuffix >= 0
			? Single.Parse(s[..indexOfSuffix], provider)
			: Single.Parse(s, provider);

		return percentage * 0.01f;
	}

	public static bool TryParsePercentageStringToFraction(string? s, IFormatProvider? provider, out float fraction) => TryParsePercentageStringToFraction(s != null ? s.AsSpan() : ReadOnlySpan<char>.Empty, provider, out fraction);
	public static bool TryParsePercentageStringToFraction(ReadOnlySpan<char> s, IFormatProvider? provider, out float fraction) {
		var indexOfSuffix = s.IndexOf(StringSuffix);
		if (indexOfSuffix < 0) indexOfSuffix = s.Length;

		if (!Single.TryParse(s[..indexOfSuffix], provider, out var percentage)) {
			fraction = default;
			return false;
		}

		fraction = percentage * 0.01f;
		return true;
	}
}