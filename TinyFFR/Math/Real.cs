// Created on 2025-02-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

// This is mostly a wrapper for float that implements some interfaces, meaning we can use floats in some APIs that work with those interfaces.
// This could go away with a 'shapes' or 'extension everything' implementation in C#
/// <summary>
/// Represents a floating-point 32-bit value (same as <see cref="Single"/>) but with some additional methods exposed that are occasionally
/// useful within TinyFFR. Real is implicitly convertible to and from <see cref="float"/>.
/// </summary>
public readonly struct Real : IMathPrimitive<Real>, IAlgebraicRing<Real>, IOrdinal<Real> {
	/// <summary>
	/// A <see cref="Real"/> with the value <c>0f</c>.
	/// </summary>
	public static readonly Real Zero = 0f;
	/// <summary>
	/// A <see cref="Real"/> with the value <c>1f</c>.
	/// </summary>
	public static readonly Real One = 1f;

	/// <summary>
	/// The underlying <see cref="float"/> value represented by this instance.
	/// </summary>
	public float AsFloat { get; }
	/// <summary>
	/// Constructs a new <see cref="Real"/> wrapping the given <paramref name="asFloat"/> value.
	/// </summary>
	/// <param name="asFloat">The <see cref="float"/> value this instance should represent.</param>
	public Real(float asFloat) { AsFloat = asFloat; }

	/// <summary>
	/// Implicitly converts a <see cref="float"/> to a <see cref="Real"/>.
	/// </summary>
	/// <param name="f">The value to convert.</param>
	public static implicit operator Real(float f) => new(f);
	/// <summary>
	/// Implicitly converts a <see cref="Real"/> to a <see cref="float"/>.
	/// </summary>
	/// <param name="r">The value to convert.</param>
	public static implicit operator float(Real r) => r.AsFloat;
	/// <inheritdoc />
	public bool Equals(Real other) => AsFloat.Equals(other.AsFloat);
	/// <inheritdoc />
	public bool Equals(Real other, float tolerance) => MathF.Abs(AsFloat - other.AsFloat) <= tolerance;
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Real other && Equals(other);
	/// <inheritdoc />
	public static bool operator ==(Real left, Real right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(Real left, Real right) => !left.Equals(right);
	/// <inheritdoc />
	public override int GetHashCode() => AsFloat.GetHashCode();

	#region Parsing / Formatting / ToString
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override string ToString() => ToString(null, null);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider = null) => AsFloat.ToString(format, formatProvider);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider = null) => AsFloat.TryFormat(destination, out charsWritten, format, provider);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real Parse(string s, IFormatProvider? provider) => Single.Parse(s, provider);

	/// <inheritdoc />
	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Real result) {
		var success = Single.TryParse(s, provider, out var f);
		result = f;
		return success;
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Single.Parse(s, provider);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Real result) {
		var success = Single.TryParse(s, provider, out var f);
		result = f;
		return success;
	}
	#endregion

	#region Serialization
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = sizeof(float);

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, Real src) => BinaryPrimitives.WriteSingleLittleEndian(dest, src);
	/// <inheritdoc />
	public static Real DeserializeFromBytes(ReadOnlySpan<byte> src) => BinaryPrimitives.ReadSingleLittleEndian(src);
	#endregion
	
	#region Random / Interpolate / Clamp
	/// <summary>
	/// Produces a random <see cref="Real"/> in the range <c>0f</c> (inclusive) to <c>1f</c> (exclusive).
	/// </summary>
	/// <returns>A new <see cref="Real"/> <c>n</c> such that <c>0f &lt;= n &lt; 1f</c>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real Random() => RandomZeroToOne();

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real Random(Real minInclusive, Real maxExclusive) => RandomUtils.NextSingle(minInclusive, maxExclusive);

	/// <summary>
	/// Produces a random <see cref="Real"/> in the range <paramref name="minInclusive"/> to <paramref name="maxInclusive"/>, with both bounds inclusive.
	/// </summary>
	/// <param name="minInclusive">The minimum value that can be produced.</param>
	/// <param name="maxInclusive">The maximum value that can be produced.</param>
	/// <returns>A new <see cref="Real"/> <c>n</c> such that <c>minInclusive &lt;= n &lt;= maxInclusive</c>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real RandomInclusive(Real minInclusive, Real maxInclusive) => RandomUtils.NextSingleInclusive(minInclusive, maxInclusive);

	/// <summary>
	/// Produces a random <see cref="Real"/> in the range <c>0f</c> (inclusive) to <c>1f</c> (exclusive).
	/// </summary>
	/// <returns>A new <see cref="Real"/> <c>n</c> such that <c>0f &lt;= n &lt; 1f</c>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real RandomZeroToOne() => RandomUtils.NextSingle();

	/// <summary>
	/// Produces a random <see cref="Real"/> in the range <c>0f</c> to <c>1f</c>, with both bounds inclusive.
	/// </summary>
	/// <returns>A new <see cref="Real"/> <c>n</c> such that <c>0f &lt;= n &lt;= 1f</c>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real RandomZeroToOneInclusive() => RandomUtils.NextSingleZeroToOneInclusive();

	/// <summary>
	/// Produces a random <see cref="Real"/> in the range <c>-1f</c> to <c>1f</c>, with both bounds inclusive.
	/// </summary>
	/// <returns>A new <see cref="Real"/> <c>n</c> such that <c>-1f &lt;= n &lt;= 1f</c>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real RandomNegOneToOneInclusive() => RandomUtils.NextSingleNegOneToOneInclusive();

	/// <inheritdoc />
	public static Real Interpolate(Real start, Real end, float distance) => start + (end - start) * distance;
	/// <inheritdoc />
	/// <remarks>
	/// If <paramref name="start"/> and <paramref name="end"/> are equal, this method always returns <c>0f</c> rather than an infinite or <see cref="Single.NaN"/> result.
	/// </remarks>
	public static float GetInterpolationDistance(Real start, Real end, Real input) {
		var result = ((input - start) / (end - start)).AsFloat;
		if (Single.IsInfinity(result) || Single.IsNaN(result)) return 0f;
		else return result;
	}

	/// <inheritdoc />
	public Real Clamp(Real min, Real max) => max < min ? Single.Clamp(this, max, min) : Single.Clamp(this, min, max);
	#endregion

	#region Arithmetic
	/// <summary>
	/// Negates <paramref name="value"/>; equivalent to reading <see cref="Negated"/>.
	/// </summary>
	/// <param name="value">The value to negate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real operator -(Real value) => new(-value.AsFloat);
	Real IInvertible<Real>.Inverted => Negated;
	/// <summary>
	/// Returns the negated (additive inverse) value of this number.
	/// </summary>
	public Real Negated => -this;

	/// <summary>
	/// Adds <paramref name="right"/> to <paramref name="left"/>; equivalent to <c>left.Plus(right)</c>.
	/// </summary>
	/// <param name="left">The left-hand operand.</param>
	/// <param name="right">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real operator +(Real left, Real right) => left.AsFloat + right.AsFloat;

	/// <summary>
	/// Subtracts <paramref name="right"/> from <paramref name="left"/>; equivalent to <c>left.Minus(right)</c>.
	/// </summary>
	/// <param name="left">The left-hand operand.</param>
	/// <param name="right">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real operator -(Real left, Real right) => left.AsFloat - right.AsFloat;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Real Plus(Real other) => this + other;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Real Minus(Real other) => this - other;

	/// <summary>
	/// The additive identity for this type, i.e. the value <c>x</c> such that <c>y + x == y</c> for any <see cref="Real"/> <c>y</c>: <c>0f</c>.
	/// </summary>
	public static Real AdditiveIdentity { get; } = 0f;

	/// <summary>
	/// Multiplies <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.MultipliedBy(right)</c>.
	/// </summary>
	/// <param name="left">The left-hand operand.</param>
	/// <param name="right">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real operator *(Real left, Real right) => left.AsFloat * right.AsFloat;
	/// <summary>
	/// Divides <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.DividedBy(right)</c>.
	/// </summary>
	/// <param name="left">The left-hand operand.</param>
	/// <param name="right">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real operator /(Real left, Real right) => left.AsFloat / right.AsFloat;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Real MultipliedBy(Real other) => this * other;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Real DividedBy(Real other) => this / other;
	/// <summary>
	/// The multiplicative identity for this type, i.e. the value <c>x</c> such that <c>y * x == y</c> for any <see cref="Real"/> <c>y</c>: <c>1f</c>.
	/// </summary>
	public static Real MultiplicativeIdentity { get; } = 1f;
	/// <inheritdoc />
	public Real? Reciprocal => this != 0f ? 1f / this : null;
	#endregion

	#region Comparison
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(Real other) => AsFloat.CompareTo(other.AsFloat);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(Real left, Real right) => left.AsFloat > right.AsFloat;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(Real left, Real right) => left.AsFloat >= right.AsFloat;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(Real left, Real right) => left.AsFloat < right.AsFloat;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(Real left, Real right) => left.AsFloat <= right.AsFloat;
	#endregion
}
/// <summary>
/// A static class housing extension methods for <see cref="float"/> related to <see cref="Real"/>.
/// </summary>
public static class RealExtensions {
	/// <summary>
	/// Converts this <see cref="float"/> to a <see cref="Real"/>.
	/// </summary>
	/// <param name="this">The extended value.</param>
	public static Real AsReal(this float @this) => new(@this);
}