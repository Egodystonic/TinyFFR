// Created on 2023-10-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

namespace Egodystonic.TinyFFR;

public static class PercentageUtils {
	public const string StringSuffix = "%";
	
	public static string ConvertFractionToPercentageString(XYPair<float> fraction, string? format = null, IFormatProvider? formatProvider = null) {
		var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		return $"{IVect.VectorStringPrefixChar}" +
			   $"{ConvertFractionToPercentageString(fraction.X, format, formatProvider)}{separator} " +
			   $"{ConvertFractionToPercentageString(fraction.Y, format, formatProvider)}" +
			   $"{IVect.VectorStringSuffixChar}";
	}
	public static string ConvertFractionToPercentageString<TVect>(TVect fraction, string? format = null, IFormatProvider? formatProvider = null) where TVect : IVect {
		var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		return $"{IVect.VectorStringPrefixChar}" +
			   $"{ConvertFractionToPercentageString(fraction.X, format, formatProvider)}{separator} " +
			   $"{ConvertFractionToPercentageString(fraction.Y, format, formatProvider)}{separator} " +
			   $"{ConvertFractionToPercentageString(fraction.Z, format, formatProvider)}" +
			   $"{IVect.VectorStringSuffixChar}";
	}

	public static string ConvertFractionToPercentageString(float fraction, string? format = null, IFormatProvider? formatProvider = null) {
		var result = $"{(fraction * 100f).ToString(format, formatProvider)}{StringSuffix}";
		var nfi = NumberFormatInfo.GetInstance(formatProvider);
		if (!result.StartsWith(nfi.NegativeSign, StringComparison.Ordinal)) return result;
		var valueWithoutNegativeSign = result[nfi.NegativeSign.Length..];
		for (var i = 0; i < (valueWithoutNegativeSign.Length - StringSuffix.Length); ++i) {
			if (valueWithoutNegativeSign[i] == '0') continue;
			
			if (valueWithoutNegativeSign[i..].StartsWith(nfi.NumberDecimalSeparator, StringComparison.Ordinal)) {
				i += nfi.NumberDecimalSeparator.Length - 1;
			}
			else if (valueWithoutNegativeSign[i..].StartsWith(nfi.NumberGroupSeparator, StringComparison.Ordinal)) {
				i += nfi.NumberGroupSeparator.Length - 1;
			}
			else return result;
		}
		return valueWithoutNegativeSign;
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

	public static bool TryFormatFractionToPercentageString(XYPair<float> fraction, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		charsWritten = 0;
		int tryWriteCharsWrittenOutVar;
		bool writeSuccess;

		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringPrefixChar;
		charsWritten++;
		destination = destination[1..];

		writeSuccess = TryFormatFractionToPercentageString(fraction.X, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = TryFormatFractionToPercentageString(fraction.Y, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringSuffixChar;
		charsWritten++;
		return true;
	}

	public static bool TryFormatFractionToPercentageString<TVect>(TVect fraction, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) where TVect : IVect {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		charsWritten = 0;
		int tryWriteCharsWrittenOutVar;
		bool writeSuccess;

		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringPrefixChar;
		charsWritten++;
		destination = destination[1..];

		writeSuccess = TryFormatFractionToPercentageString(fraction.X, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = TryFormatFractionToPercentageString(fraction.Y, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = TryFormatFractionToPercentageString(fraction.Z, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringSuffixChar;
		charsWritten++;
		return true;
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

	static float ParseNextComponent(ref ReadOnlySpan<char> s, NumberFormatInfo numberFormatter, IFormatProvider? provider) {
		var indexOfSeparator = IndexOfComponentSeparator(s, numberFormatter);
		var result = ParsePercentageStringToFraction(s[..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		return result;
	}

	static bool TryParseNextComponent(ref ReadOnlySpan<char> s, NumberFormatInfo numberFormatter, IFormatProvider? provider, out float fraction) {
		var indexOfSeparator = IndexOfComponentSeparator(s, numberFormatter);
		if (indexOfSeparator < 0) {
			fraction = default;
			return false;
		}

		if (!TryParsePercentageStringToFraction(s[..indexOfSeparator], provider, out fraction)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		return true;
	}

	static bool TryParseFinalComponent(ReadOnlySpan<char> s, NumberFormatInfo numberFormatter, IFormatProvider? provider, out float fraction) {
		if (IndexOfComponentSeparator(s, numberFormatter) >= 0) {
			fraction = default;
			return false;
		}

		return TryParsePercentageStringToFraction(s, provider, out fraction);
	}

	static int IndexOfComponentSeparator(ReadOnlySpan<char> s, NumberFormatInfo numberFormatter) {
		var searchStartIndex = s.IndexOf(StringSuffix);
		if (searchStartIndex < 0) searchStartIndex = 0;

		var indexOfSeparator = s[searchStartIndex..].IndexOf(numberFormatter.NumberGroupSeparator);
		return indexOfSeparator < 0 ? -1 : indexOfSeparator + searchStartIndex;
	}

	public static XYPair<float> ParsePercentageStringToFractionXYPair(string s, IFormatProvider? provider = null) => ParsePercentageStringToFractionXYPair(s.AsSpan(), provider);
	public static XYPair<float> ParsePercentageStringToFractionXYPair(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		s = s[1..];

		var x = ParseNextComponent(ref s, numberFormatter, provider);
		var y = ParsePercentageStringToFraction(s[..^1], provider);

		return new(x, y);
	}

	public static TVect ParsePercentageStringToFractionVect<TVect>(string s, IFormatProvider? provider = null) where TVect : IVect<TVect> => ParsePercentageStringToFractionVect<TVect>(s.AsSpan(), provider);
	public static TVect ParsePercentageStringToFractionVect<TVect>(ReadOnlySpan<char> s, IFormatProvider? provider = null) where TVect : IVect<TVect> {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		s = s[1..];

		var x = ParseNextComponent(ref s, numberFormatter, provider);
		var y = ParseNextComponent(ref s, numberFormatter, provider);
		var z = ParsePercentageStringToFraction(s[..^1], provider);

		return TVect.FromVector3(new Vector3(x, y, z));
	}

	public static bool TryParsePercentageStringToFractionXYPair(string? s, IFormatProvider? provider, out XYPair<float> fraction) => TryParsePercentageStringToFractionXYPair(s != null ? s.AsSpan() : ReadOnlySpan<char>.Empty, provider, out fraction);
	public static bool TryParsePercentageStringToFractionXYPair(ReadOnlySpan<char> s, IFormatProvider? provider, out XYPair<float> fraction) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		fraction = default;

		if (s.Length <= 2) return false;
		if (s[0] != IVect.VectorStringPrefixChar) return false;
		if (s[^1] != IVect.VectorStringSuffixChar) return false;
		s = s[1..^1];

		if (!TryParseNextComponent(ref s, numberFormatter, provider, out var x)) return false;
		if (!TryParseFinalComponent(s, numberFormatter, provider, out var y)) return false;

		fraction = new(x, y);
		return true;
	}

	public static bool TryParsePercentageStringToFractionVect<TVect>(string? s, IFormatProvider? provider, out TVect fraction) where TVect : IVect<TVect> => TryParsePercentageStringToFractionVect(s != null ? s.AsSpan() : ReadOnlySpan<char>.Empty, provider, out fraction);
	public static bool TryParsePercentageStringToFractionVect<TVect>(ReadOnlySpan<char> s, IFormatProvider? provider, out TVect fraction) where TVect : IVect<TVect> {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		fraction = default!;

		if (s.Length <= 2) return false;
		if (s[0] != IVect.VectorStringPrefixChar) return false;
		if (s[^1] != IVect.VectorStringSuffixChar) return false;
		s = s[1..^1];

		if (!TryParseNextComponent(ref s, numberFormatter, provider, out var x)) return false;
		if (!TryParseNextComponent(ref s, numberFormatter, provider, out var y)) return false;
		if (!TryParseFinalComponent(s, numberFormatter, provider, out var z)) return false;

		fraction = TVect.FromVector3(new Vector3(x, y, z));
		return true;
	}
}