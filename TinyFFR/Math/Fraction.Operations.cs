// Created on 2023-10-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

partial struct Fraction {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction operator -(Fraction operand) => operand.Negated;
	public Fraction Negated {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromDecimal(-_asDecimal);
	}


	public Fraction Absolute {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromDecimal(MathF.Abs(_asDecimal));
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float operator /(float scalar, Fraction fraction) => scalar / fraction._asDecimal;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction MultipliedBy(float scalar) => FromDecimal(_asDecimal * scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction DividedBy(float scalar) => FromDecimal(_asDecimal / scalar);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction operator *(Fraction lhs, Fraction rhs) => lhs.MultipliedBy(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction operator /(Fraction lhs, Fraction rhs) => lhs.DividedBy(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction MultipliedBy(Fraction other) => FromDecimal(_asDecimal * other._asDecimal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction DividedBy(Fraction other) => FromDecimal(_asDecimal / other._asDecimal);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction operator %(Fraction lhs, Fraction rhs) => FromDecimal(lhs._asDecimal % rhs._asDecimal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float operator %(float lhs, Fraction rhs) => lhs % rhs._asDecimal;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction operator +(Fraction lhs, Fraction rhs) => lhs.Plus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Fraction operator -(Fraction lhs, Fraction rhs) => lhs.Minus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction Plus(Fraction other) => FromDecimal(_asDecimal + other._asDecimal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction Minus(Fraction other) => FromDecimal(_asDecimal + other._asDecimal);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction ClampZeroToFull() => new(Math.Clamp(AsDecimal, Zero.AsDecimal, Full.AsDecimal));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction ClampFullNegativeToFull() => new(Math.Clamp(AsDecimal, FullNegative.AsDecimal, Full.AsDecimal));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction Clamp(Fraction min, Fraction max) => new(Math.Clamp(AsDecimal, min.AsDecimal, max.AsDecimal));




	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(Fraction other) => _asDecimal.CompareTo(other._asDecimal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(Fraction left, Fraction right) => left._asDecimal > right._asDecimal;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(Fraction left, Fraction right) => left._asDecimal >= right._asDecimal;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(Fraction left, Fraction right) => left._asDecimal < right._asDecimal;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(Fraction left, Fraction right) => left._asDecimal <= right._asDecimal;
}