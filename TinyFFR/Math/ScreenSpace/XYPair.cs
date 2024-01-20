// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 2, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Vector2
public readonly partial struct XYPair : IMathPrimitive<XYPair> {
	public static readonly XYPair Zero = new(0f, 0f);

	internal readonly Vector2 AsVector2;

	public float X {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector2.X;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector2.X = value;
	}
	public float Y {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector2.Y;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector2.Y = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair() { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair(float x, float y) : this(new Vector2(x, y)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal XYPair(Vector2 v) => AsVector2 = v;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 ToVector2() => AsVector2;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair FromVector2(Vector2 v) => new(v);

	public void Deconstruct(out float x, out float y) {
		x = X;
		y = Y;
	}
	public static implicit operator XYPair((float X, float Y) tuple) => new(tuple.X, tuple.Y);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in XYPair src) => MemoryMarshal.Cast<XYPair, float>(new ReadOnlySpan<XYPair>(in src));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair ConvertFromSpan(ReadOnlySpan<float> src) => FromVector2(new Vector2(src));

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => $"{X.ToString(format, formatProvider)}{NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator} {Y.ToString(format, formatProvider)}";

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

	public static XYPair Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out XYPair result) => TryParse(s.AsSpan(), provider, out result);

	public static XYPair Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		s = s[1..]; // Assume starts with VectorStringPrefixChar

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var x = Single.Parse(s[..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		var y = Single.Parse(s[..^1], provider); // Assume ends with VectorStringSuffixChar

		return new(x, y);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out XYPair result) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		result = default;

		if (s.Length <= 2) return false;
		if (s[0] != IVect.VectorStringPrefixChar) return false;
		if (s[^1] != IVect.VectorStringSuffixChar) return false;
		s = s[1..^1];

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 0) return false;

		if (!Single.TryParse(s[..indexOfSeparator], provider, out var x)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		
		if (!Single.TryParse(s[..indexOfSeparator], provider, out var y)) return false;

		result = new(x, y);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(XYPair other) => AsVector2.Equals(other.AsVector2);
	public bool Equals(XYPair other, float tolerance) {
		return MathF.Abs(X - other.X) <= tolerance
			&& MathF.Abs(Y - other.Y) <= tolerance;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(XYPair left, XYPair right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(XYPair left, XYPair right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is XYPair other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsVector2.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool EqualsWithinDistance(XYPair other, float distance) => (this - other).LengthSquared <= distance * distance;
}