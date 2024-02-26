// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial struct XYPair<T> : IMathPrimitive<XYPair<T>, T> where T : unmanaged, INumber<T> {
	public static readonly XYPair<T> Zero = new(T.Zero, T.Zero);

	public T X { get; init; }
	public T Y { get; init; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair(T x, T y) {
		X = x;
		Y = y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 ToVector2() => new(Single.CreateSaturating(X), Single.CreateSaturating(Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> FromVector2(Vector2 v) => new(T.CreateSaturating(v.X), T.CreateSaturating(v.Y));

	public static XYPair<T> FromOrientationAndLength(Orientation2D orientation, T length) {
		var angle = orientation.GetPolarAngle();
		if (angle == null) return Zero;
		else return FromAngleAndLength(angle.Value, length);
	}

	public static XYPair<T> FromAngleAndLength(Angle angle, T length) => new XYPair<T>(T.CreateSaturating(MathF.Cos(angle.Radians)), T.CreateSaturating(MathF.Sin(angle.Radians))) * length;

	public void Deconstruct(out T x, out T y) {
		x = X;
		y = Y;
	}
	public static implicit operator XYPair<T>((T X, T Y) tuple) => new(tuple.X, tuple.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<T> ConvertToSpan(in XYPair<T> src) => MemoryMarshal.Cast<XYPair<T>, T>(new ReadOnlySpan<XYPair<T>>(in src));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> ConvertFromSpan(ReadOnlySpan<T> src) => new(src[0], src[1]);

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => $"{IVect.VectorStringPrefixChar}{X.ToString(format, formatProvider)}{NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator} {Y.ToString(format, formatProvider)}{IVect.VectorStringSuffixChar}";

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
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
		writeSuccess = X.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// Y
		writeSuccess = Y.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// >
		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringSuffixChar;
		charsWritten++;
		return true;
	}

	public static XYPair<T> Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out XYPair<T> result) => TryParse(s.AsSpan(), provider, out result);

	public static XYPair<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		s = s[1..]; // Assume starts with VectorStringPrefixChar

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var x = T.Parse(s[..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		var y = T.Parse(s[..^1], provider); // Assume ends with VectorStringSuffixChar

		return new(x, y);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out XYPair<T> result) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		result = default;

		if (s.Length <= 2) return false;
		if (s[0] != IVect.VectorStringPrefixChar) return false;
		if (s[^1] != IVect.VectorStringSuffixChar) return false;
		s = s[1..^1];

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 0) return false;

		if (!T.TryParse(s[..indexOfSeparator], provider, out var x)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		
		if (!T.TryParse(s, provider, out var y)) return false;

		result = new(x, y);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(XYPair<T> other) => X.Equals(other.X) && Y.Equals(other.Y);
	public bool Equals(XYPair<T> other, float tolerance) {
		return Single.CreateSaturating(T.Abs(X - other.X)) <= tolerance
			&& Single.CreateSaturating(T.Abs(Y - other.Y)) <= tolerance;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(XYPair<T> left, XYPair<T> right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(XYPair<T> left, XYPair<T> right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is XYPair<T> other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => HashCode.Combine(X, Y);
}