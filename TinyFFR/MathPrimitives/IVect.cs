// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

namespace Egodystonic.TinyFFR;

/* We don't implement IEquatable<IVect> because it's hard to argue that there IS a concrete definition of equality here.
 * Does a Location actually equal a Direction even if their XYZ components are identical?
 * If users really just want equality of the components they can use `ToVector3().Equals(other.ToVector3())` which is more explicit.
 */
public interface IVect : ILinearAlgebraComposite {
	internal const char VectorStringPrefixChar = '<';
	internal const char VectorStringSuffixChar = '>';

	float X { get; init; }
	float Y { get; init; }
	float Z { get; init; }
	Vector3 ToVector3();

	string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => VectExtensions.ToString(this, format, formatProvider);
	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => VectExtensions.TryFormat(this, destination, out charsWritten, format, provider);

	protected static Vector3 ParseVector3String(ReadOnlySpan<char> s, IFormatProvider? provider) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		s = s[1..]; // Assume starts with VectorStringPrefixChar

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var x = Single.Parse(s[..indexOfSeparator], provider); 
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var y = Single.Parse(s[..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		var z = Single.Parse(s[..^1], provider); // Assume ends with VectorStringSuffixChar

		return new(x, y, z);
	}

	protected static bool TryParseVector3String(ReadOnlySpan<char> s, IFormatProvider? provider, out Vector3 result) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		result = default;

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 0) return false;

		if (!Single.TryParse(s[..indexOfSeparator], provider, out var x)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		if (!Single.TryParse(s[..indexOfSeparator], provider, out var y)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		if (!Single.TryParse(s[..indexOfSeparator], provider, out var z)) return false;

		result = new(x, y, z);
		return true;
	}
}

public interface IVect<TSelf> : IVect, ILinearAlgebraComposite<TSelf>, IToleranceEquatable<TSelf>, IEqualityOperators<TSelf, TSelf, bool> where TSelf : IVect<TSelf> {
	static abstract TSelf FromVector3(Vector3 v);
}

public static class VectExtensions {
	public static string ToString<TSelf>(this TSelf @this, string? format, IFormatProvider? formatProvider) where TSelf : IVect => @this.ToVector3().ToString(format, formatProvider);
	public static bool TryFormat<TSelf>(this TSelf @this, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null) where TSelf : IVect {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		// <
		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringPrefixChar;
		charsWritten++;
		destination = destination[1..];

		// X
		writeSuccess = @this.X.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// Y
		writeSuccess = @this.Y.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// Z
		writeSuccess = @this.Z.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// >
		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringSuffixChar;
		charsWritten++;
		return true;
	}
}