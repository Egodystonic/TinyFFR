// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct XYPair<T> :
	IAlgebraicRing<XYPair<T>>,
	IInterpolatable<XYPair<T>>,
	IDistanceMeasurable<XYPair<T>>,
	IScalable<XYPair<T>>,
	IAngleMeasurable<XYPair<T>, XYPair<T>> {

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator -(XYPair<T> operand) => operand.Negated;
	public XYPair<T> Negated {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-X, -Y);
	}
	XYPair<T> IInvertible<XYPair<T>>.Inverted => Negated;

	public XYPair<T> Absolute {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(T.Abs(X), T.Abs(Y));
	}

	public XYPair<T>? Reciprocal {
		get {
			if (X == T.Zero || Y == T.Zero) return null;
			return new XYPair<T>(T.One / X, T.One / Y);
		}
	}

	public float Length {
		get => ToVector2().Length();
	}
	public float LengthSquared {
		get => ToVector2().LengthSquared();
	}

	static XYPair<T> IAdditiveIdentity<XYPair<T>, XYPair<T>>.AdditiveIdentity => new(T.Zero, T.Zero);
	static XYPair<T> IMultiplicativeIdentity<XYPair<T>, XYPair<T>>.MultiplicativeIdentity => new(T.One, T.One);

	public XYPair<TNew> Cast<TNew>() where TNew : unmanaged, INumber<TNew> => new(TNew.CreateTruncating(X), TNew.CreateTruncating(Y));

	#region Scaling and Addition/Subtraction
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator +(XYPair<T> lhs, XYPair<T> rhs) => lhs.Plus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> Plus(XYPair<T> other) => new(X + other.X, Y + other.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator -(XYPair<T> lhs, XYPair<T> rhs) => lhs.Minus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> Minus(XYPair<T> other) => new(X - other.X, Y - other.Y);

	public static XYPair<T> operator *(XYPair<T> left, XYPair<T> right) => left.MultipliedBy(right);
	public static XYPair<T> operator /(XYPair<T> left, XYPair<T> right) => left.DividedBy(right);
	public XYPair<T> MultipliedBy(XYPair<T> other) => new(X * other.X, Y * other.Y);
	public XYPair<T> DividedBy(XYPair<T> other) => new(X / other.X, Y / other.Y);

	public static XYPair<T> operator *(XYPair<T> pair, float scalar) => pair.ScaledBy(scalar);
	public static XYPair<T> operator /(XYPair<T> pair, float scalar) => pair.ScaledBy(1f / scalar);
	public static XYPair<T> operator *(float scalar, XYPair<T> pair) => pair.ScaledBy(scalar);
	public XYPair<T> ScaledBy(float scalar) => Cast<float>().MultipliedBy(scalar).Cast<T>();

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
	#endregion

	#region Trigonometry
	public Angle? PolarAngle { // TODO clarify this is the four-quadrant inverse tangent. In this co-ordinate system, positive X is considered to move to the right (unlike our 3D system)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Angle.From2DPolarAngle(this);
	}

	// TODO xmldoc explain this always gives a positive value between 0 and 180, and describes the absolute difference in angle between the two pairs
	public Angle AngleTo(XYPair<T> other) {
		var otherAngle = other.PolarAngle;
		var thisAngle = PolarAngle;
		if (otherAngle == null || thisAngle == null) return Angle.Zero;
		return thisAngle.Value.AbsoluteDifferenceTo(otherAngle.Value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(XYPair<T> lhs, XYPair<T> rhs) => lhs.AngleTo(rhs);
	#endregion

	#region Interactions w/ XYPair
	public float DistanceSquaredFrom(XYPair<T> pair) => Vector2.DistanceSquared(ToVector2(), pair.ToVector2());
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(XYPair<T> pair) => Vector2.Distance(ToVector2(), pair.ToVector2());
	#endregion

	#region Clamping and Interpolation
	public static XYPair<T> Interpolate(XYPair<T> start, XYPair<T> end, float distance) {
		return start + (end - start).ScaledBy(distance);
	}
	public XYPair<T> Clamp(XYPair<T> min, XYPair<T> max) {
		var minX = min.X;
		var maxX = max.X;
		var minY = min.Y;
		var maxY = max.Y;

		if (minX > maxX) (minX, maxX) = (maxX, minX);
		if (minY > maxY) (minY, maxY) = (maxY, minY);

		return new(
			T.Clamp(X, minX, maxX),
			T.Clamp(Y, minY, maxY)
		);
	}
	#endregion
}