// Created on 2025-02-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

// This is mostly a wrapper for float that implements some interfaces, meaning we can use floats in some APIs that work with those interfaces.
public readonly record struct Real(float AsFloat) : IMathPrimitive<Real>, IAlgebraicRing<Real>, IOrdinal<Real> {
	public static implicit operator Real(float f) => new(f);
	public static implicit operator float(Real r) => r.AsFloat;
	public bool Equals(Real other, float tolerance) => MathF.Abs(AsFloat - other.AsFloat) <= tolerance;

	#region Parsing / Formatting / ToString
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider) => AsFloat.ToString(format, formatProvider);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => AsFloat.TryFormat(destination, out charsWritten, format, provider);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real Parse(string s, IFormatProvider? provider) => Single.Parse(s, provider);
	
	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Real result) {
		var success = Single.TryParse(s, provider, out var f);
		result = f;
		return success;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Single.Parse(s, provider);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Real result) {
		var success = Single.TryParse(s, provider, out var f);
		result = f;
		return success;
	}
	#endregion

	#region Serialization
	public static int SerializationByteSpanLength { get; } = sizeof(float);

	public static void SerializeToBytes(Span<byte> dest, Real src) => BinaryPrimitives.WriteSingleLittleEndian(dest, src);
	public static Real DeserializeFromBytes(ReadOnlySpan<byte> src) => BinaryPrimitives.ReadSingleLittleEndian(src);
	#endregion
	
	#region Random / Interpolate / Clamp
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real Random() => RandomUtils.NextSingle();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real Random(Real minInclusive, Real maxExclusive) => RandomUtils.NextSingle(minInclusive, maxExclusive);

	public static Real Interpolate(Real start, Real end, float distance) => start + (end - start) * distance;

	public Real Clamp(Real min, Real max) => max < min ? Single.Clamp(this, max, min) : Single.Clamp(this, min, max);
	#endregion

	#region Arithmetic
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real operator -(Real value) => new(-value.AsFloat);
	public Real Inverted => -this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real operator +(Real left, Real right) => left.AsFloat + right.AsFloat;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real operator -(Real left, Real right) => left.AsFloat - right.AsFloat;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Real Plus(Real other) => this + other;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Real Minus(Real other) => this - other;
	
	public static Real AdditiveIdentity { get; } = 0f;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real operator *(Real left, Real right) => left.AsFloat * right.AsFloat;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Real operator /(Real left, Real right) => left.AsFloat / right.AsFloat;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Real MultipliedBy(Real other) => this * other;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Real DividedBy(Real other) => this / other;
	public static Real MultiplicativeIdentity { get; } = 1f;
	public Real? Reciprocal => this != 0f ? 1f / this : null;
	#endregion

	#region Comparison
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(Real other) => AsFloat.CompareTo(other.AsFloat);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(Real left, Real right) => left.AsFloat > right.AsFloat;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(Real left, Real right) => left.AsFloat >= right.AsFloat;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(Real left, Real right) => left.AsFloat < right.AsFloat;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(Real left, Real right) => left.AsFloat <= right.AsFloat;
	#endregion
}

public static class RealExtensions {
	public static Real AsReal(this float f) => f;
}