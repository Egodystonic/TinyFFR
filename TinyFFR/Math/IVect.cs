// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR;

/* We don't implement IEquatable<IVect> because it's hard to argue that there IS a concrete definition of equality here.
 * Does a Location actually equal a Direction even if their XYZ components are identical?
 * If users really just want equality of the components they can use `ToVector3().Equals(other.ToVector3())` which is more explicit.
 */
/// <summary>
/// Interface that represents any three-float-channel vector (such as <see cref="Location"/>, <see cref="Vect"/>, <see cref="Direction"/>, etc).
/// </summary>
/// <seealso cref="IVect{TSelf}"/>
public interface IVect : IMathPrimitive {
	internal const char VectorStringPrefixChar = '<';
	internal const char VectorStringSuffixChar = '>';

	/// <summary>
	/// The first component of this vector.
	/// </summary>
	float X { get; }
	float Y { get; }
	float Z { get; }

	/// <summary>
	/// Converts this vector to a raw SIMD-ready <see cref="Vector3"/>.
	/// </summary>
	Vector3 ToVector3();

	/// <summary>
	/// Gets the float component specified by the given <paramref name="axis"/>. 
	/// </summary>
	/// <param name="axis">The axis whose component you wish to retrieve (e.g. <see cref="X"/>, <see cref="Y"/>, or <see cref="Z"/>).</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="axis"/> is not <see cref="Axis.X"/>, <see cref="Axis.Y"/>, or <see cref="Axis.Z"/>.</exception>
	float this[Axis axis] { get; }
	XYPair<float> this[Axis first, Axis second] { get; }

	string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => VectExtensions.ToString(this, format, formatProvider);
	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => VectExtensions.TryFormat(this, destination, out charsWritten, format, provider);

	void Deconstruct(out float x, out float y, out float z);

	/// <summary>
	/// Converts this vector to a <see cref="Vect"/> regardless of its actual type.
	/// </summary>
	Vect AsVect();

	/// <summary>
	/// Helper method for parsing any <see cref="IVect"/> from a string.
	/// </summary>
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

	/// <summary>
	/// Helper method for parsing any <see cref="IVect"/> from a string.
	/// </summary>
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

/// <summary>
/// Interface that represents any three-float-channel vector (such as <see cref="Location"/>, <see cref="Vect"/>, <see cref="Direction"/>, etc). Extends <see cref="IVect"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IVect<TSelf> : IVect, IMathPrimitive<TSelf>, IInterpolatable<TSelf> where TSelf : IVect<TSelf> {
	static abstract TSelf FromVector3(Vector3 v);
	static abstract implicit operator TSelf((float X, float Y, float Z) tuple);
	TSelf this[Axis first, Axis second, Axis third] { get; }
}

/// <summary>
/// A static class housing extension methods for <see cref="IVect"/> and related types.
/// </summary>
public static class VectExtensions {
	/// <summary>Returns the string representation of the current instance using the specified format string to format individual elements and the specified format provider to define culture-specific formatting.</summary>
	/// <param name="this">The extended object.</param>
	/// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
	/// <param name="formatProvider">A format provider that supplies culture-specific formatting information.</param>
	/// <returns>The string representation of the current instance.</returns>
	public static string ToString<TSelf>(this TSelf @this, string? format, IFormatProvider? formatProvider) where TSelf : IVect => @this.ToVector3().ToString(format, formatProvider);

	/// <summary>Tries to format the value of the current instance into the provided span of characters.</summary>
	/// <param name="this">The extended object.</param>
	/// <param name="destination">The span in which to write this instance's value formatted as a span of characters.</param>
	/// <param name="charsWritten">When this method returns, contains the number of characters that were written in <paramref name="destination" />.</param>
	/// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination" />.</param>
	/// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination" />.</param>
	/// <returns>
	/// <see langword="true" /> if the formatting was successful; otherwise, <see langword="false" />.</returns>
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