// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial struct XYPair<T> : IMathPrimitive<XYPair<T>> where T : unmanaged, INumber<T> {
	public static readonly XYPair<T> Zero = new(T.Zero, T.Zero);
	static readonly int _marshalledElementSizeBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in Zero._x)).Length;

	readonly T _x;
	readonly T _y;

	public T X {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _x;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _x = value;
	}
	public T Y {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _y;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _y = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair(T x, T y) {
		_x = x;
		_y = y;
	}

	#region Factories and Conversions
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 ToVector2() => new(Single.CreateSaturating(X), Single.CreateSaturating(Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> FromVector2(Vector2 v) => new(T.CreateSaturating(v.X), T.CreateSaturating(v.Y));

	public static XYPair<T> FromOrientationAndLength(Orientation2D orientation, T length) {
		var angle = orientation.ToPolarAngle();
		if (angle == null) return Zero;
		else return FromPolarAngleAndLength(angle.Value, length);
	}

	public static XYPair<T> FromPolarAngleAndLength(Angle angle, T length) => new XYPair<T>(T.CreateSaturating(MathF.Cos(angle.AsRadians)), T.CreateSaturating(MathF.Sin(angle.AsRadians))) * length;

	public void Deconstruct(out T x, out T y) {
		x = X;
		y = Y;
	}
	public static implicit operator XYPair<T>((T X, T Y) tuple) => new(tuple.X, tuple.Y);
	#endregion

	#region Span Conversions
	// ReSharper disable once StaticMemberInGenericType We actually want to specialize the value for type T, so this is correct
	public static int SerializationByteSpanLength { get; } = _marshalledElementSizeBytes * 2;

	public static void SerializeToBytes(Span<byte> dest, XYPair<T> src) {
		MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in src._x)).CopyTo(dest);
		MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in src._y)).CopyTo(dest[_marshalledElementSizeBytes..]);
	}

	public static XYPair<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			MemoryMarshal.AsRef<T>(src),
			MemoryMarshal.AsRef<T>(src[_marshalledElementSizeBytes..])
		);
	}
	#endregion

	#region String Conversions
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
	#endregion

	#region Equality
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
	#endregion
}