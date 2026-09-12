// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a pair of numbers that can be interpreted as a vector, location, direction,
/// or simply as a pair of related values. 
/// </summary>
/// <typeparam name="T">The numeric type for the paired numbers (usually <see cref="float"/> or <see cref="int"/>).</typeparam>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial struct XYPair<T> : IMathPrimitive<XYPair<T>> where T : unmanaged, INumber<T> {
	/// <summary>
	/// An <see cref="XYPair{T}"/> with both <see cref="X"/> and <see cref="Y"/> set to zero.
	/// </summary>
	public static readonly XYPair<T> Zero = new(T.Zero, T.Zero);
	/// <summary>
	/// An <see cref="XYPair{T}"/> with both <see cref="X"/> and <see cref="Y"/> set to one.
	/// </summary>
	public static readonly XYPair<T> One = new(T.One, T.One);
	internal const int DefaultRandomRange = 100;
	static readonly int _marshalledElementSizeBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in Zero._x)).Length;

	readonly T _x;
	readonly T _y;

	/// <summary>
	/// The first value in the pair.
	/// </summary>
	public T X {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _x;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _x = value;
	}
	/// <summary>
	/// The second value in the pair.
	/// </summary>
	public T Y {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _y;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _y = value;
	}

	/// <summary>
	/// Returns <see cref="X"/> or <see cref="Y"/> depending on <paramref name="axis"/>.
	/// </summary>
	/// <param name="axis">The axis to read. Must be <see cref="Axis2D.X"/> or <see cref="Axis2D.Y"/>.</param>
	public T this[Axis2D axis] => axis switch {
		Axis2D.X => X,
		Axis2D.Y => Y,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} must not be anything except {nameof(Axis2D.X)} or {nameof(Axis2D.Y)}.")
	};
	/// <summary>
	/// Returns a new pair built from this pair's values at <paramref name="first"/> and <paramref name="second"/>, in that order.
	/// </summary>
	/// <remarks>
	/// This can be used to swap the two values (by passing <see cref="Axis2D.Y"/> then <see cref="Axis2D.X"/>) or to duplicate one value into both slots.
	/// </remarks>
	/// <param name="first">The axis to read for the new pair's <see cref="X"/>.</param>
	/// <param name="second">The axis to read for the new pair's <see cref="Y"/>.</param>
	public XYPair<T> this[Axis2D first, Axis2D second] => new(this[first], this[second]);

	internal static bool IsFloatingPoint { get; } = typeof(T).GetInterface("IFloatingPoint`1") != null;

	/// <summary>
	/// Constructs a new <see cref="XYPair{T}"/> with both <see cref="X"/> and <see cref="Y"/> set to <paramref name="xy"/>.
	/// </summary>
	/// <param name="xy">The value for both <see cref="X"/> and <see cref="Y"/>.</param>
	public XYPair(T xy) : this(xy, xy) { }

	/// <summary>
	/// Constructs a new <see cref="XYPair{T}"/> with the given values.
	/// </summary>
	/// <param name="x">The value for <see cref="X"/>.</param>
	/// <param name="y">The value for <see cref="Y"/>.</param>
	public XYPair(T x, T y) {
		_x = x;
		_y = y;
	}

	#region Factories and Conversions
	/// <summary>
	/// Converts this pair to a <see cref="Vector2"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 ToVector2() => new(Single.CreateSaturating(X), Single.CreateSaturating(Y));
	/// <summary>
	/// Converts <paramref name="v"/> to an <see cref="XYPair{T}"/>.
	/// </summary>
	/// <param name="v">The vector to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> FromVector2(Vector2 v) => new(T.CreateSaturating(v.X), T.CreateSaturating(v.Y));

	/// <summary>
	/// Constructs the <see cref="XYPair{T}"/> that points in the direction of <paramref name="orientation"/> and has the given <paramref name="length"/>.
	/// </summary>
	/// <param name="orientation">The desired orientation.</param>
	/// <param name="length">The desired length (see <see cref="XYPairExtensions.Length"/>).</param>
	/// <returns><see cref="Zero"/> if <paramref name="orientation"/> is <see cref="Orientation2D.None"/>; the constructed pair otherwise.</returns>
	public static XYPair<T> FromOrientationAndLength(Orientation2D orientation, float length) {
		var angle = orientation.ToPolarAngle();
		if (angle == null) return Zero;
		else return FromPolarAngleAndLength(angle.Value, length);
	}

	/// <summary>
	/// Constructs the unit-length <see cref="XYPair{T}"/> at the given <paramref name="angle"/> around the circle.
	/// </summary>
	/// <remarks>
	/// This follows the same convention as <see cref="Angle.From2DPolarAngle(Orientation2D)"/>: the angle "starts" at 0° pointing in the <see cref="Orientation2D.Right"/> direction (i.e. <c>(1, 0)</c>) and increases anticlockwise.
	/// </remarks>
	/// <param name="angle">The angle around the circle.</param>
	public static XYPair<T> FromPolarAngle(Angle angle) => new(T.CreateSaturating(MathF.Cos(angle.Radians)), T.CreateSaturating(MathF.Sin(angle.Radians)));
	/// <summary>
	/// Constructs the <see cref="XYPair{T}"/> at the given <paramref name="angle"/> around the circle (see <see cref="FromPolarAngle"/>), scaled to <paramref name="length"/>.
	/// </summary>
	/// <param name="angle">The angle around the circle.</param>
	/// <param name="length">The desired length (see <see cref="XYPairExtensions.Length"/>).</param>
	public static XYPair<T> FromPolarAngleAndLength(Angle angle, float length) => FromPolarAngle(angle).WithLength(length);

	/// <summary>
	/// Deconstructs this pair into its two values.
	/// </summary>
	/// <param name="x">Receives <see cref="X"/>.</param>
	/// <param name="y">Receives <see cref="Y"/>.</param>
	public void Deconstruct(out T x, out T y) {
		x = X;
		y = Y;
	}
	/// <summary>
	/// Converts <paramref name="tuple"/> to an <see cref="XYPair{T}"/>.
	/// </summary>
	/// <param name="tuple">The tuple to convert.</param>
	public static implicit operator XYPair<T>((T X, T Y) tuple) => new(tuple.X, tuple.Y);
	#endregion

	#region Random
	/// <summary>
	/// Produces a random pair, with both <see cref="X"/> and <see cref="Y"/> independently randomized.
	/// </summary>
	public static XYPair<T> Random() {
		return new(
			T.CreateChecked(RandomUtils.NextSingleNegOneToOneInclusive() * DefaultRandomRange),
			T.CreateChecked(RandomUtils.NextSingleNegOneToOneInclusive() * DefaultRandomRange)
		);
	}
	/// <summary>
	/// Produces a random pair, with both <see cref="X"/> and <see cref="Y"/> independently randomized between <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// </summary>
	/// <param name="minInclusive">The lower (inclusive) bound for <see cref="X"/> and <see cref="Y"/>.</param>
	/// <param name="maxExclusive">The upper (exclusive) bound for <see cref="X"/> and <see cref="Y"/>.</param>
	public static XYPair<T> Random(T minInclusive, T maxExclusive) => Random((minInclusive, maxExclusive), (minInclusive, maxExclusive));
	/// <summary>
	/// Produces a random pair, with <see cref="X"/> and <see cref="Y"/> each independently randomized between the corresponding values of <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// </summary>
	/// <param name="minInclusive">The lower (inclusive) bound for <see cref="X"/> and <see cref="Y"/>.</param>
	/// <param name="maxExclusive">The upper (exclusive) bound for <see cref="X"/> and <see cref="Y"/>.</param>
	public static XYPair<T> Random(XYPair<T> minInclusive, XYPair<T> maxExclusive) {
		var x = (Min: Double.CreateChecked(minInclusive.X), Max: Double.CreateChecked(maxExclusive.X));
		var y = (Min: Double.CreateChecked(minInclusive.Y), Max: Double.CreateChecked(maxExclusive.Y));
		return new(
			T.CreateChecked(RandomUtils.GlobalRng.NextDouble() * (x.Max - x.Min) + x.Min),
			T.CreateChecked(RandomUtils.GlobalRng.NextDouble() * (y.Max - y.Min) + y.Min)
		);
	}
	#endregion

	#region Span Conversions
	/// <inheritdoc />
	// ReSharper disable once StaticMemberInGenericType We actually want to specialize the value for type T, so this is correct
	public static int SerializationByteSpanLength { get; } = _marshalledElementSizeBytes * 2;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, XYPair<T> src) {
		MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in src._x)).CopyTo(dest);
		MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in src._y)).CopyTo(dest[_marshalledElementSizeBytes..]);
	}

	/// <inheritdoc />
	public static XYPair<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			MemoryMarshal.Read<T>(src),
			MemoryMarshal.Read<T>(src[_marshalledElementSizeBytes..])
		);
	}
	#endregion

	#region String Conversions
	/// <inheritdoc />
	public override string ToString() => ToString(null, null);

	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) => $"{IVect.VectorStringPrefixChar}{X.ToString(format, formatProvider)}{NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator} {Y.ToString(format, formatProvider)}{IVect.VectorStringSuffixChar}";

	/// <inheritdoc />
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

	/// <inheritdoc />
	public static XYPair<T> Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out XYPair<T> result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static XYPair<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		s = s[1..]; // Assume starts with VectorStringPrefixChar

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var x = T.Parse(s[..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		var y = T.Parse(s[..^1], provider); // Assume ends with VectorStringSuffixChar

		return new(x, y);
	}

	/// <inheritdoc />
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
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(XYPair<T> other) => X.Equals(other.X) && Y.Equals(other.Y);
	/// <summary>
	/// Determines whether this pair is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares <see cref="X"/> and <see cref="Y"/> independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	public bool Equals(XYPair<T> other, float tolerance) {
		return Single.CreateSaturating(T.Abs(X - other.X)) <= tolerance
			&& Single.CreateSaturating(T.Abs(Y - other.Y)) <= tolerance;
	}
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(XYPair<T> left, XYPair<T> right) => left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(XYPair<T> left, XYPair<T> right) => !left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is XYPair<T> other && Equals(other);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => HashCode.Combine(X, Y);
	#endregion
}