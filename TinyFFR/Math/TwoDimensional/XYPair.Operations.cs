// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct XYPair<T> :
	IAlgebraicRing<XYPair<T>>,
	IInterpolatable<XYPair<T>>,
	IDistanceMeasurable<XYPair<T>> { // TODO Angle-measurable
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
	public static XYPair<T> operator -(XYPair<T> operand) => operand.Inverted;
	public XYPair<T> Inverted {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-X, -Y);
	}
	public XYPair<T> Reciprocal {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(T.One / X, T.One / Y);
	}

	static XYPair<T> IAdditiveIdentity<XYPair<T>, XYPair<T>>.AdditiveIdentity => new(T.Zero, T.Zero);

	public static XYPair<T> operator *(XYPair<T> left, XYPair<T> right) => left.MultipliedBy(right);
	public static XYPair<T> operator /(XYPair<T> left, XYPair<T> right) => left.DividedBy(right);
	public XYPair<T> MultipliedBy(XYPair<T> other) => new(X * other.X, Y * other.Y);
	public XYPair<T> DividedBy(XYPair<T> other) => new(X / other.X, Y / other.Y);

	static XYPair<T> IMultiplicativeIdentity<XYPair<T>, XYPair<T>>.MultiplicativeIdentity => new(T.One, T.One);

	public Angle? PolarAngle { // TODO clarify this is the four-quadrant inverse tangent
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Angle.From2DPolarAngle(this);
	}

	public XYPair<T> Absolute {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(T.Abs(X), T.Abs(Y));
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> pairOperand, T scalarOperand) => pairOperand.MultipliedBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(T scalarOperand, XYPair<T> pairOperand) => pairOperand.MultipliedBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator /(XYPair<T> pairOperand, T divisorOperand) => pairOperand.DividedBy(divisorOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> DividedBy(T divisor) => new(X / divisor, Y / divisor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> MultipliedBy(T scalar) => new(X * scalar, Y * scalar);

	public float DistanceSquaredFrom(XYPair<T> pair) => Single.CreateSaturating(T.Abs(X - pair.X) + T.Abs(Y - pair.Y));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(XYPair<T> pair) => MathF.Sqrt(DistanceSquaredFrom(pair));

	public static XYPair<T> Interpolate(XYPair<T> start, XYPair<T> end, float distance) {
		return start + (end - start) * T.CreateSaturating(distance);
	}
	public XYPair<T> Clamp(XYPair<T> min, XYPair<T> max) {
		return new(
			T.Clamp(X, min.X, max.X),
			T.Clamp(Y, min.Y, max.Y)
		);
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