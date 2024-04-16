// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR;

public readonly partial struct XYPair<T> :
	IAdditionOperators<XYPair<T>, XYPair<T>, XYPair<T>>,
	ISubtractionOperators<XYPair<T>, XYPair<T>, XYPair<T>>,
	IMultiplyOperators<XYPair<T>, T, XYPair<T>>,
	IDivisionOperators<XYPair<T>, T, XYPair<T>>,
	IUnaryNegationOperators<XYPair<T>, XYPair<T>>,
	IInterpolatable<XYPair<T>>,
	IBoundedRandomizable<XYPair<T>> {
	internal const int DefaultRandomRange = 100;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator +(XYPair<T> lhs, XYPair<T> rhs) => lhs.Plus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> Plus(XYPair<T> other) => new(X + other.X, Y + other.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator -(XYPair<T> lhs, XYPair<T> rhs) => lhs.Minus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> Minus(XYPair<T> other) => new(X - other.X, Y - other.Y);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator -(XYPair<T> operand) => operand.Reversed;
	public XYPair<T> Reversed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-X, -Y);
	}

	public Angle? PolarAngle { // TODO clarify this is the four-quadrant inverse tangent
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Angle.From2DPolarAngle(this);
	}

	public XYPair<T> Absolute {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(T.Abs(X), T.Abs(Y));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> pairOperand, T scalarOperand) => pairOperand.ScaledBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> pairOperand, float scalarOperand) => pairOperand.ScaledBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(T scalarOperand, XYPair<T> pairOperand) => pairOperand.ScaledBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(float scalarOperand, XYPair<T> pairOperand) => pairOperand.ScaledBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator /(XYPair<T> pairOperand, T divisorOperand) => pairOperand.DividedBy(divisorOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator /(XYPair<T> pairOperand, float divisorOperand) => pairOperand.DividedBy(divisorOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> ScaledBy(T scalar) => new(X * scalar, Y * scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> ScaledBy(float scalar) => new(T.CreateSaturating(Single.CreateSaturating(X) * scalar), T.CreateSaturating((Single.CreateSaturating(Y) * scalar)));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> DividedBy(T divisor) => new(X / divisor, Y / divisor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> DividedBy(float divisor) => new(T.CreateSaturating(Single.CreateSaturating(X) / divisor), T.CreateSaturating((Single.CreateSaturating(Y) / divisor)));

	public static XYPair<T> Interpolate(XYPair<T> start, XYPair<T> end, float distance) {
		return start + (end - start) * distance;
	}

	public static XYPair<T> CreateNewRandom() {
		return new(
			T.CreateChecked(RandomUtils.NextSingleNegOneToOneInclusive() * DefaultRandomRange),
			T.CreateChecked(RandomUtils.NextSingleNegOneToOneInclusive() * DefaultRandomRange)
		);
	}
	public static XYPair<T> CreateNewRandom(T minInclusive, T maxExclusive) => CreateNewRandom((minInclusive, maxExclusive), (minInclusive, maxExclusive));
	public static XYPair<T> CreateNewRandom(XYPair<T> minInclusive, XYPair<T> maxExclusive) {
		var x = (Min: Double.CreateChecked(minInclusive.X), Max: Double.CreateChecked(maxExclusive.X));
		var y = (Min: Double.CreateChecked(minInclusive.Y), Max: Double.CreateChecked(maxExclusive.Y));
		return new(
			T.CreateChecked(RandomUtils.GlobalRng.NextDouble() * (x.Max - x.Min) + x.Min),
			T.CreateChecked(RandomUtils.GlobalRng.NextDouble() * (y.Max - y.Min) + y.Min)
		);
	}
}