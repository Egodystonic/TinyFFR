// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

// Is there a better way of implementing these methods? Almost certainly. But for v1 this will do.
// The Parse methods assume T1/T2/T3 would never output any of the tokens in their string forms. Ideally in the future I'd like to not do that.
static class GeometryUtils {
	public const string ParameterStartToken = "[";
	public const string ParameterKeyValueSeparatorToken = " ";
	public const string ParameterSeparatorToken = " | ";
	public const string ParameterEndToken = "]";

	#region ToString
	public static string StandardizedToString<T1>(string? format, IFormatProvider? formatProvider, string typeName, (string Name, T1 Value) paramA) where T1 : IFormattable {
		return $"{typeName}{ParameterStartToken}{paramA.Name}{ParameterKeyValueSeparatorToken}{paramA.Value.ToString(format, formatProvider)}{ParameterEndToken}";
	}
	
	public static string StandardizedToString<T1, T2>(string? format, IFormatProvider? formatProvider, string typeName, (string Name, T1 Value) paramA, (string Name, T2 Value) paramB) where T1 : IFormattable where T2 : IFormattable {
		return $"{typeName}{ParameterStartToken}{paramA.Name}{ParameterKeyValueSeparatorToken}{paramA.Value.ToString(format, formatProvider)}{ParameterSeparatorToken}{paramB.Name}{ParameterKeyValueSeparatorToken}{paramB.Value.ToString(format, formatProvider)}{ParameterEndToken}";
	}
	
	public static string StandardizedToString<T1, T2, T3>(string? format, IFormatProvider? formatProvider, string typeName, (string Name, T1 Value) paramA, (string Name, T2 Value) paramB, (string Name, T3 Value) paramC) where T1 : IFormattable where T2 : IFormattable where T3 : IFormattable {
		return $"{typeName}{ParameterStartToken}{paramA.Name}{ParameterKeyValueSeparatorToken}{paramA.Value.ToString(format, formatProvider)}{ParameterSeparatorToken}{paramB.Name}{ParameterKeyValueSeparatorToken}{paramB.Value.ToString(format, formatProvider)}{ParameterSeparatorToken}{paramC.Name}{ParameterKeyValueSeparatorToken}{paramC.Value.ToString(format, formatProvider)}{ParameterEndToken}";
	}
	#endregion

	#region TryFormat
	public static bool StandardizedTryFormat<T1>(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider, string typeName, (string Name, T1 Value) paramA) where T1 : ISpanFormattable {
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		writeSuccess = destination.TryWrite(provider, $"{typeName}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite(provider, $"{ParameterStartToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		writeSuccess = destination.TryWrite(provider, $"{paramA.Name}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = destination.TryWrite(provider, $"{ParameterKeyValueSeparatorToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = paramA.Value.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		writeSuccess = destination.TryWrite(provider, $"{ParameterEndToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		return writeSuccess;
	}

	public static bool StandardizedTryFormat<T1, T2>(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider, string typeName, (string Name, T1 Value) paramA, (string Name, T2 Value) paramB) where T1 : ISpanFormattable where T2 : ISpanFormattable {
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		writeSuccess = destination.TryWrite(provider, $"{typeName}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite(provider, $"{ParameterStartToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		writeSuccess = destination.TryWrite(provider, $"{paramA.Name}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = destination.TryWrite(provider, $"{ParameterKeyValueSeparatorToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = paramA.Value.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite(provider, $"{ParameterSeparatorToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite(provider, $"{paramB.Name}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = destination.TryWrite(provider, $"{ParameterKeyValueSeparatorToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = paramB.Value.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		writeSuccess = destination.TryWrite(provider, $"{ParameterEndToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		return writeSuccess;
	}

	public static bool StandardizedTryFormat<T1, T2, T3>(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider, string typeName, (string Name, T1 Value) paramA, (string Name, T2 Value) paramB, (string Name, T3 Value) paramC) where T1 : ISpanFormattable where T2 : ISpanFormattable where T3 : ISpanFormattable {
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		writeSuccess = destination.TryWrite(provider, $"{typeName}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite(provider, $"{ParameterStartToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		writeSuccess = destination.TryWrite(provider, $"{paramA.Name}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = destination.TryWrite(provider, $"{ParameterKeyValueSeparatorToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = paramA.Value.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite(provider, $"{ParameterSeparatorToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite(provider, $"{paramB.Name}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = destination.TryWrite(provider, $"{ParameterKeyValueSeparatorToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = paramB.Value.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite(provider, $"{ParameterSeparatorToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite(provider, $"{paramC.Name}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = destination.TryWrite(provider, $"{ParameterKeyValueSeparatorToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];
		writeSuccess = paramC.Value.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		writeSuccess = destination.TryWrite(provider, $"{ParameterEndToken}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		return writeSuccess;
	}
	#endregion

	#region Parse
	public static void StandardizedParse<T1>(ReadOnlySpan<char> s, IFormatProvider? provider, out T1 outParamA) where T1 : ISpanParsable<T1> {
		s = s[(s.IndexOf(ParameterKeyValueSeparatorToken) + ParameterKeyValueSeparatorToken.Length)..];
		outParamA = T1.Parse(s[..^ParameterEndToken.Length], provider);
	}

	public static void StandardizedParse<T1, T2>(ReadOnlySpan<char> s, IFormatProvider? provider, out T1 outParamA, out T2 outParamB) where T1 : ISpanParsable<T1> where T2 : ISpanParsable<T2> {
		s = s[(s.IndexOf(ParameterKeyValueSeparatorToken) + ParameterKeyValueSeparatorToken.Length)..];
		var separatorTokenIndex = s.IndexOf(ParameterSeparatorToken);
		outParamA = T1.Parse(s[..separatorTokenIndex], provider);

		s = s[(separatorTokenIndex + ParameterSeparatorToken.Length)..];
		s = s[(s.IndexOf(ParameterKeyValueSeparatorToken) + ParameterKeyValueSeparatorToken.Length)..];
		outParamB = T2.Parse(s[..^ParameterEndToken.Length], provider);
	}

	public static void StandardizedParse<T1, T2, T3>(ReadOnlySpan<char> s, IFormatProvider? provider, out T1 outParamA, out T2 outParamB, out T3 outParamC) where T1 : ISpanParsable<T1> where T2 : ISpanParsable<T2> where T3 : ISpanParsable<T3> {
		s = s[(s.IndexOf(ParameterKeyValueSeparatorToken) + ParameterKeyValueSeparatorToken.Length)..];
		var separatorTokenIndex = s.IndexOf(ParameterSeparatorToken);
		outParamA = T1.Parse(s[..separatorTokenIndex], provider);

		s = s[(separatorTokenIndex + ParameterSeparatorToken.Length)..];
		s = s[(s.IndexOf(ParameterKeyValueSeparatorToken) + ParameterKeyValueSeparatorToken.Length)..];
		separatorTokenIndex = s.IndexOf(ParameterSeparatorToken);
		outParamB = T2.Parse(s[..separatorTokenIndex], provider);

		s = s[(separatorTokenIndex + ParameterSeparatorToken.Length)..];
		s = s[(s.IndexOf(ParameterKeyValueSeparatorToken) + ParameterKeyValueSeparatorToken.Length)..];
		outParamC = T3.Parse(s[..^ParameterEndToken.Length], provider);
	}
	#endregion

	#region Parse
	public static bool StandardizedTryParse<T1>(ReadOnlySpan<char> s, IFormatProvider? provider, out T1 outParamA) where T1 : ISpanParsable<T1> {
		outParamA = default!;

		var kvSeparatorTokenIndex = s.IndexOf(ParameterKeyValueSeparatorToken);
		if (kvSeparatorTokenIndex < 0) return false;

		s = s[(kvSeparatorTokenIndex + ParameterKeyValueSeparatorToken.Length)..];
		var paramsEndTokenIndex = s.IndexOf(ParameterEndToken);
		if (paramsEndTokenIndex < 0) return false;

		s = s[..paramsEndTokenIndex];

		if (!T1.TryParse(s, provider, out outParamA!)) return false;
		return true;
	}

	public static bool StandardizedTryParse<T1, T2>(ReadOnlySpan<char> s, IFormatProvider? provider, out T1 outParamA, out T2 outParamB) where T1 : ISpanParsable<T1> where T2 : ISpanParsable<T2> {
		outParamA = default!;
		outParamB = default!;

		var kvSeparatorTokenIndex = s.IndexOf(ParameterKeyValueSeparatorToken);
		if (kvSeparatorTokenIndex < 0) return false;

		s = s[(kvSeparatorTokenIndex + ParameterKeyValueSeparatorToken.Length)..];
		var paramSeparatorTokenIndex = s.IndexOf(ParameterSeparatorToken);
		if (paramSeparatorTokenIndex < 0) return false;
		if (!T1.TryParse(s[..paramSeparatorTokenIndex], provider, out outParamA!)) return false;
		s = s[(paramSeparatorTokenIndex + ParameterSeparatorToken.Length)..];

		kvSeparatorTokenIndex = s.IndexOf(ParameterKeyValueSeparatorToken);
		if (kvSeparatorTokenIndex < 0) return false;
		var paramsEndTokenIndex = s.IndexOf(ParameterEndToken);
		if (paramsEndTokenIndex < 0) return false;

		s = s[(kvSeparatorTokenIndex + ParameterKeyValueSeparatorToken.Length)..paramsEndTokenIndex];

		if (!T2.TryParse(s, provider, out outParamB!)) return false;
		return true;
	}

	public static bool StandardizedTryParse<T1, T2, T3>(ReadOnlySpan<char> s, IFormatProvider? provider, out T1 outParamA, out T2 outParamB, out T3 outParamC) where T1 : ISpanParsable<T1> where T2 : ISpanParsable<T2> where T3 : ISpanParsable<T3> {
		outParamA = default!;
		outParamB = default!;
		outParamC = default!;

		var kvSeparatorTokenIndex = s.IndexOf(ParameterKeyValueSeparatorToken);
		if (kvSeparatorTokenIndex < 0) return false;

		s = s[(kvSeparatorTokenIndex + ParameterKeyValueSeparatorToken.Length)..];
		var paramSeparatorTokenIndex = s.IndexOf(ParameterSeparatorToken);
		if (paramSeparatorTokenIndex < 0) return false;
		if (!T1.TryParse(s[..paramSeparatorTokenIndex], provider, out outParamA!)) return false;
		s = s[(paramSeparatorTokenIndex + ParameterSeparatorToken.Length)..];

		kvSeparatorTokenIndex = s.IndexOf(ParameterKeyValueSeparatorToken);
		if (kvSeparatorTokenIndex < 0) return false;
		s = s[(kvSeparatorTokenIndex + ParameterKeyValueSeparatorToken.Length)..];
		paramSeparatorTokenIndex = s.IndexOf(ParameterSeparatorToken);
		if (paramSeparatorTokenIndex < 0) return false;
		if (!T2.TryParse(s[..paramSeparatorTokenIndex], provider, out outParamB!)) return false;
		s = s[(paramSeparatorTokenIndex + ParameterSeparatorToken.Length)..];

		kvSeparatorTokenIndex = s.IndexOf(ParameterKeyValueSeparatorToken);
		if (kvSeparatorTokenIndex < 0) return false;
		var paramsEndTokenIndex = s.IndexOf(ParameterEndToken);
		if (paramsEndTokenIndex < 0) return false;

		s = s[(kvSeparatorTokenIndex + ParameterKeyValueSeparatorToken.Length)..paramsEndTokenIndex];

		if (!T3.TryParse(s, provider, out outParamC!)) return false;
		return true;
	}
	#endregion
}