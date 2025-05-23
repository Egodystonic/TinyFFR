﻿// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR;

/* We don't implement IEquatable<IVect> because it's hard to argue that there IS a concrete definition of equality here.
 * Does a Location actually equal a Direction even if their XYZ components are identical?
 * If users really just want equality of the components they can use `ToVector3().Equals(other.ToVector3())` which is more explicit.
 */
public interface IVect : IMathPrimitive {
	internal const char VectorStringPrefixChar = '<';
	internal const char VectorStringSuffixChar = '>';

	float X { get; }
	float Y { get; }
	float Z { get; }
	Vector3 ToVector3();

	float this[Axis axis] { get; }
	XYPair<float> this[Axis first, Axis second] { get; }

	string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => VectExtensions.ToString(this, format, formatProvider);
	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => VectExtensions.TryFormat(this, destination, out charsWritten, format, provider);

	void Deconstruct(out float x, out float y, out float z);

	Vect AsVect();
	
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

		if (s.Length <= 2) return false;
		if (s[0] != VectorStringPrefixChar) return false;
		if (s[^1] != VectorStringSuffixChar) return false;
		s = s[1..^1];

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 0) return false;

		if (!Single.TryParse(s[..indexOfSeparator], provider, out var x)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 0) return false;

		if (!Single.TryParse(s[..indexOfSeparator], provider, out var y)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		
		if (!Single.TryParse(s, provider, out var z)) return false;

		result = new(x, y, z);
		return true;
	}
}

public interface IVect<TSelf> : IVect, IMathPrimitive<TSelf>, IInterpolatable<TSelf> where TSelf : IVect<TSelf> {
	static abstract TSelf FromVector3(Vector3 v);
	static abstract implicit operator TSelf((float X, float Y, float Z) tuple);
	TSelf this[Axis first, Axis second, Axis third] { get; }
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