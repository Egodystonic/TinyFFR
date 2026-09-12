// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Numerics;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Describes the winding direction from one <see cref="XYPair{T}"/> to another, as seen in the standard 2D orientation (X to the right, Y up).
/// </summary>
public enum XyPairClockOrientation {
	/// <summary>
	/// The two pairs point in the same or exactly opposite directions.
	/// </summary>
	Colinear = 0,
	/// <summary>
	/// Reaching the second pair from the first by the shortest route turns clockwise.
	/// </summary>
	Clockwise = -1,
	/// <summary>
	/// Reaching the second pair from the first by the shortest route turns anticlockwise.
	/// </summary>
	Anticlockwise = 1,
}

partial struct XYPair<T> :
	IAbsolutizable<XYPair<T>>,
	IAlgebraicRing<XYPair<T>>,
	IInterpolatable<XYPair<T>>,
	IDistanceMeasurable<XYPair<T>>,
	IAngleMeasurable<XYPair<T>, XYPair<T>>,
	IPointTransformable2D<XYPair<T>>,
	ILengthAdjustable<XYPair<T>>,
	IInnerProductSpace<XYPair<T>>,
	IRelatable<XYPair<T>, XYPair<T>, XyPairClockOrientation> {

	/// <summary>
	/// Negates <paramref name="operand"/>; equivalent to reading <see cref="Negated"/>.
	/// </summary>
	/// <param name="operand">The pair to negate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator -(XYPair<T> operand) => operand.Negated;
	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> both negated.
	/// </summary>
	public XYPair<T> Negated {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-X, -Y);
	}
	XYPair<T> IInvertible<XYPair<T>>.Inverted => Negated;

	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> both made non-negative.
	/// </summary>
	public XYPair<T> Absolute {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(T.Abs(X), T.Abs(Y));
	}

	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> both inverted (i.e. <c>1 / X</c> and <c>1 / Y</c>).
	/// </summary>
	/// <returns><see langword="null"/> if either <see cref="XYPair{T}.X"/> or <see cref="XYPair{T}.Y"/> is zero; the inverted pair otherwise.</returns>
	public XYPair<T>? Reciprocal {
		get {
			if (X == T.Zero || Y == T.Zero) return null;
			return new XYPair<T>(T.One / X, T.One / Y);
		}
	}

	/// <summary>
	/// Returns the length from <see cref="XYPair{T}.Zero"/> when considering this XYPair as a 2D vector.
	/// </summary>
	public float Length {
		get => ToVector2().Length();
	}
	/// <summary>
	/// The square of <see cref="Length"/>. Cheaper to calculate than <see cref="Length"/>, and sufficient when only comparing lengths rather than needing the exact value.
	/// </summary>
	public float LengthSquared {
		get => ToVector2().LengthSquared();
	}

	/// <summary>
	/// Returns the area of the rectangle when considering this XYPair as lengths/dimensions (i.e. this returns <c>X * Y</c>).
	/// </summary>
	public T Area {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => T.Abs(X * Y);
	}
	/// <summary>
	/// Returns the ratio of X by Y (e.g. this returns <c>X / Y</c>); or <c>null</c> if Y is zero.
	/// </summary>
	public float? Ratio {
		get {
			var v2 = ToVector2();
			if (v2.Y == 0f) return null;
			return v2.X / v2.Y;
		}
	}

	static XYPair<T> IAdditiveIdentity<XYPair<T>, XYPair<T>>.AdditiveIdentity => new(T.Zero, T.Zero);
	static XYPair<T> IMultiplicativeIdentity<XYPair<T>, XYPair<T>>.MultiplicativeIdentity => new(T.One, T.One);

	/// <summary>
	/// Converts this pair to an <see cref="XYPair{T}"/> of a different numeric type <typeparamref name="TNew"/>, truncating (rather than rounding) if converting from a floating-point type to an integral one.
	/// </summary>
	/// <typeparam name="TNew">The desired numeric type.</typeparam>
	public XYPair<TNew> Cast<TNew>() where TNew : unmanaged, INumber<TNew> {
		if (typeof(TNew) == typeof(T)) return Unsafe.As<XYPair<T>, XYPair<TNew>>(ref Unsafe.AsRef(in this));
		return new XYPair<TNew>(TNew.CreateTruncating(X), TNew.CreateTruncating(Y));
	}

	#region Length Modifiers
	XYPair<T> ILengthAdjustable<XYPair<T>>.WithLength(float newLength) => WithLength(newLength);
	XYPair<T> ILengthAdjustable<XYPair<T>>.WithLengthDecreasedBy(float lengthDecrease) => WithLengthDecreasedBy(lengthDecrease);
	XYPair<T> ILengthAdjustable<XYPair<T>>.WithLengthIncreasedBy(float lengthIncrease) => WithLengthIncreasedBy(lengthIncrease);
	XYPair<T> ILengthAdjustable<XYPair<T>>.WithMaxLength(float maxLength) => WithMaxLength(maxLength);
	XYPair<T> ILengthAdjustable<XYPair<T>>.WithMinLength(float minLength) => WithMinLength(minLength);

	/// <summary>
	/// Returns this pair (considered as a 2D vector) scaled uniformly so that its <see cref="Length"/> becomes <paramref name="newLength"/>.
	/// </summary>
	/// <param name="newLength">The new length. A negative value flips the pair's direction.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public XYPair<T> WithLength(float newLength, MidpointRounding midpointRounding = MidpointRounding.ToEven) => Cast<float>().WithLengthOne().ScaledBy(newLength).CastWithRoundingIfNecessary<float, T>(midpointRounding);
	/// <summary>
	/// Returns this pair with its <see cref="Length"/> reduced by <paramref name="lengthDecrease"/>.
	/// </summary>
	/// <param name="lengthDecrease">The amount to subtract from <see cref="Length"/>. Can be negative to increase the length instead.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public XYPair<T> WithLengthDecreasedBy(float lengthDecrease, MidpointRounding midpointRounding = MidpointRounding.ToEven) => WithLength(Length - lengthDecrease, midpointRounding);
	/// <summary>
	/// Returns this pair with its <see cref="Length"/> increased by <paramref name="lengthIncrease"/>.
	/// </summary>
	/// <param name="lengthIncrease">The amount to add to <see cref="Length"/>. Can be negative to decrease the length instead.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public XYPair<T> WithLengthIncreasedBy(float lengthIncrease, MidpointRounding midpointRounding = MidpointRounding.ToEven) => WithLength(Length + lengthIncrease, midpointRounding);
	/// <summary>
	/// Returns this pair, shortened to <paramref name="maxLength"/> if it is currently longer than that; otherwise returns this pair unchanged.
	/// </summary>
	/// <param name="maxLength">The maximum permitted length. Must be non-negative.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> was negative.</exception>
	public XYPair<T> WithMaxLength(float maxLength, MidpointRounding midpointRounding = MidpointRounding.ToEven) => WithLength(MathF.Min(Length, maxLength >= 0f ? maxLength : throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be non-negative.")), midpointRounding);
	/// <summary>
	/// Returns this pair, lengthened to <paramref name="minLength"/> if it is currently shorter than that; otherwise returns this pair unchanged.
	/// </summary>
	/// <param name="minLength">The minimum permitted length. Must be non-negative.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> was negative.</exception>
	public XYPair<T> WithMinLength(float minLength, MidpointRounding midpointRounding = MidpointRounding.ToEven) => WithLength(MathF.Max(Length, minLength >= 0f ? minLength : throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Must be non-negative.")), midpointRounding);
	#endregion

	#region Trigonometry
	/// <summary>
	/// The angle this pair (considered as a 2D vector) forms around the circle, using the four-quadrant inverse tangent of <see cref="XYPair{T}.Y"/> over <see cref="XYPair{T}.X"/>.
	/// </summary>
	/// <remarks>
	/// This follows the same convention as <see cref="Angle.From2DPolarAngle(Orientation2D)"/>: the angle "starts" at 0° for a pair pointing in the <see cref="Orientation2D.Right"/> direction (i.e. positive <see cref="XYPair{T}.X"/>, zero <see cref="XYPair{T}.Y"/>) and increases anticlockwise.
	/// </remarks>
	/// <returns><see langword="null"/> if this pair is <see cref="Zero"/> (in which case no angle is meaningful); the polar angle otherwise.</returns>
	public Angle? PolarAngle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Angle.From2DPolarAngle(this);
	}

	/// <summary>
	/// Calculates the (unsigned) angle between this pair and <paramref name="other"/>, each considered as a 2D vector.
	/// </summary>
	/// <remarks>
	/// This is always in the range <c>0° &lt;= n &lt;= 180°</c>, describing the absolute difference in angle between the two pairs regardless of winding direction. For a signed version that distinguishes clockwise from anticlockwise, see <see cref="SignedAngleTo"/>.
	/// </remarks>
	/// <param name="other">The other pair to measure against.</param>
	/// <returns><see cref="Angle.Zero"/> if either this pair or <paramref name="other"/> is <see cref="Zero"/> (in which case no angle is meaningful); the angle between them otherwise.</returns>
	public Angle AngleTo(XYPair<T> other) {
		var otherAngle = other.PolarAngle;
		var thisAngle = PolarAngle;
		if (otherAngle == null || thisAngle == null) return Angle.Zero;
		return thisAngle.Value.ShortestDifferenceTo(otherAngle.Value);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(XYPair<T> lhs, XYPair<T> rhs) => lhs.AngleTo(rhs);
	/// <summary>
	/// Calculates the angle formed between this pair and <paramref name="other"/>, additionally attributing a sign (+ or -) to the result according to <see cref="AngleOrientationTo"/>.
	/// </summary>
	/// <remarks>
	/// The result is positive when <paramref name="other"/> is reached from this pair by turning anticlockwise (see <see cref="XyPairClockOrientation.Anticlockwise"/>), and negative when reached by turning clockwise.
	/// </remarks>
	/// <param name="other">The other pair to measure against.</param>
	public Angle SignedAngleTo(XYPair<T> other) => AngleTo(other) * ((int) AngleOrientationTo(other) | 0b1);

	/// <summary>
	/// Calculates the dot product of this pair and <paramref name="other"/>, each considered as a 2D vector.
	/// </summary>
	/// <param name="other">The other pair.</param>
	public float Dot(XYPair<T> other) => Vector2.Dot(ToVector2(), other.ToVector2());

	/// <summary>
	/// Calculates the (2D scalar) cross product of this pair and <paramref name="other"/>, each considered as a 2D vector.
	/// </summary>
	/// <remarks>
	/// The sign of the result matches <see cref="AngleOrientationTo"/>: positive when <paramref name="other"/> is anticlockwise from this pair, negative when clockwise, and zero when the two are colinear.
	/// </remarks>
	/// <param name="other">The other pair.</param>
	public float Cross(XYPair<T> other) {
		var v = ToVector2();
		var w = other.ToVector2();
		return v.X * w.Y - v.Y * w.X;
	}

	/// <summary>
	/// Determines the winding direction from this pair to <paramref name="target"/>, each considered as a 2D vector, as seen in the standard 2D orientation (X to the right, Y up).
	/// </summary>
	/// <param name="target">The target pair.</param>
	public XyPairClockOrientation AngleOrientationTo(XYPair<T> target) => (XyPairClockOrientation) MathF.Sign(Cross(target));
	XyPairClockOrientation IRelatable<XYPair<T>, XyPairClockOrientation>.RelationshipTo(XYPair<T> other) => AngleOrientationTo(other);
	#endregion

	#region Interactions w/ XYPair
	/// <inheritdoc/>
	public float DistanceSquaredFrom(XYPair<T> pair) => Vector2.DistanceSquared(ToVector2(), pair.ToVector2());
	/// <summary>
	/// Returns the distance from <paramref name="pair"/> when considering both XYPairs as 2D points.
	/// </summary>
	/// <param name="pair">The XYPair to measure the 2D euclidean distance to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(XYPair<T> pair) => Vector2.Distance(ToVector2(), pair.ToVector2());
	#endregion

	#region Scaling
	/// <summary>
	/// Scales <paramref name="pair"/> by <paramref name="scalar"/>; equivalent to <see cref="ScaledBy(T)"/>.
	/// </summary>
	/// <param name="pair">The pair to scale.</param>
	/// <param name="scalar">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> pair, T scalar) => pair.ScaledBy(scalar);
	/// <summary>
	/// Divides <paramref name="pair"/>'s <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> both by <paramref name="divisor"/>.
	/// </summary>
	/// <param name="pair">The pair to divide.</param>
	/// <param name="divisor">The divisor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator /(XYPair<T> pair, T divisor) => new(pair.X / divisor, pair.Y / divisor);
	/// <inheritdoc cref="operator *(XYPair{T},T)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(T scalar, XYPair<T> pair) => pair.ScaledBy(scalar);
	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> both multiplied by <paramref name="scalar"/>.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	public XYPair<T> ScaledBy(T scalar) => new(X * scalar, Y * scalar);
	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> both multiplied by the real (floating-point) value <paramref name="scalar"/>.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public XYPair<T> ScaledByReal(float scalar, MidpointRounding midpointRounding = MidpointRounding.ToEven) => Cast<float>().ScaledBy(scalar).CastWithRoundingIfNecessary<float, T>(midpointRounding);
	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> scaled independently by <paramref name="pair"/>'s corresponding real (floating-point) component.
	/// </summary>
	/// <param name="pair">The per-axis scale factors.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public XYPair<T> ScaledByReal(XYPair<float> pair, MidpointRounding midpointRounding = MidpointRounding.ToEven) => Cast<float>().ScaledBy(pair).CastWithRoundingIfNecessary<float, T>(midpointRounding);
	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> scaled independently by <paramref name="pair"/>'s corresponding real (floating-point) component, around <paramref name="scalingOrigin"/>.
	/// </summary>
	/// <param name="pair">The per-axis scale factors.</param>
	/// <param name="scalingOrigin">The point to scale around. Does not need to be this pair.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public XYPair<T> ScaledByReal(XYPair<float> pair, XYPair<float> scalingOrigin, MidpointRounding midpointRounding = MidpointRounding.ToEven) => Cast<float>().ScaledBy(pair, scalingOrigin).CastWithRoundingIfNecessary<float, T>(midpointRounding);
	static XYPair<T> IMultiplyOperators<XYPair<T>, float, XYPair<T>>.operator *(XYPair<T> pair, float scalar) => ((IScalable<XYPair<T>>) pair).ScaledBy(scalar);
	static XYPair<T> IDivisionOperators<XYPair<T>, float, XYPair<T>>.operator /(XYPair<T> pair, float scalar) => ((IScalable<XYPair<T>>) pair).ScaledBy(1f / scalar);
	static XYPair<T> IMultiplicative<XYPair<T>, float, XYPair<T>>.operator *(float scalar, XYPair<T> pair) => ((IScalable<XYPair<T>>) pair).ScaledBy(scalar);
	XYPair<T> IScalable<XYPair<T>>.ScaledBy(float scalar) => ScaledByReal(scalar);

	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> multiplied independently by <paramref name="other"/>'s corresponding component.
	/// </summary>
	/// <param name="other">The per-axis scale factors.</param>
	public XYPair<T> MultipliedBy(XYPair<T> other) => new(X * other.X, Y * other.Y);
	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> divided independently by <paramref name="other"/>'s corresponding component.
	/// </summary>
	/// <param name="other">The per-axis divisors.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> DividedBy(XYPair<T> other) => new(X / other.X, Y / other.Y);
	/// <summary>
	/// Multiplies <paramref name="left"/> and <paramref name="right"/> component-wise; equivalent to <see cref="ScaledBy(XYPair{T})"/>.
	/// </summary>
	/// <param name="left">The pair to scale.</param>
	/// <param name="right">The per-axis scale factors.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> left, XYPair<T> right) => left.ScaledBy(right);
	/// <summary>
	/// Divides <paramref name="left"/> by <paramref name="right"/> component-wise; equivalent to <see cref="DividedBy"/>.
	/// </summary>
	/// <param name="left">The pair to divide.</param>
	/// <param name="right">The per-axis divisors.</param>
	public static XYPair<T> operator /(XYPair<T> left, XYPair<T> right) => new(left.X / right.X, left.Y / right.Y);

	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> scaled independently by <paramref name="vect"/>'s corresponding component, around the origin (<see cref="Zero"/>).
	/// </summary>
	/// <param name="vect">The per-axis scale factors.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> ScaledBy(XYPair<T> vect) => ScaledFromOriginBy(vect);
	/// <inheritdoc cref="ScaledBy(XYPair{T})" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> ScaledFromOriginBy(XYPair<T> vect) => MultipliedBy(vect);
	/// <summary>
	/// Returns this pair with <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> scaled independently by <paramref name="vect"/>'s corresponding component, around <paramref name="scalingOrigin"/>.
	/// </summary>
	/// <param name="vect">The per-axis scale factors.</param>
	/// <param name="scalingOrigin">The point to scale around. Does not need to be this pair.</param>
	public XYPair<T> ScaledBy(XYPair<T> vect, XYPair<T> scalingOrigin) => scalingOrigin + ((this - scalingOrigin) * vect);
	XYPair<T> IIndependentAxisScalable2D<XYPair<T>>.ScaledBy(XYPair<float> vect) => ScaledByReal(vect);
	XYPair<T> IPointIndependentAxisScalable2D<XYPair<T>>.ScaledFromOriginBy(XYPair<float> vect) => ScaledByReal(vect);
	XYPair<T> IPointIndependentAxisScalable2D<XYPair<T>>.ScaledBy(XYPair<float> vect, XYPair<float> scalingOrigin) => ScaledByReal(vect, scalingOrigin);
	#endregion

	#region Rotation
	/* Maintainer's note: I do not specify the multiply operator here for Angle rotations (e.g. XYPair<T> * Angle)
	 * because it's too easy to do something like (myXyPairOfInts * someFloat) expecting a scaling operation and instead
	 * getting the implicit conversion to Angle. For the 3D vector types the rotation operand is Rotation, not Angle,
	 * and they have no type parameterization; both of these facts make it much harder to make such a mistake.
	 */
	XYPair<T> IRotatable2D<XYPair<T>>.RotatedBy(Angle rot) => RotatedAroundOriginBy(rot);
	/// <summary>
	/// Returns this pair (considered as a 2D vector) rotated by <paramref name="rot"/> around the origin (<see cref="Zero"/>).
	/// </summary>
	/// <remarks>
	/// A positive <paramref name="rot"/> turns anticlockwise, matching <see cref="PolarAngle"/>'s convention.
	/// </remarks>
	/// <param name="rot">The angle to rotate by.</param>
	public XYPair<T> RotatedAroundOriginBy(Angle rot) => PolarAngle is { } a ? FromPolarAngleAndLength(a + rot, Length) : Zero;
	/// <summary>
	/// Returns this pair rotated by <paramref name="rot"/> around <paramref name="pivot"/>.
	/// </summary>
	/// <remarks>
	/// A positive <paramref name="rot"/> turns anticlockwise, matching <see cref="PolarAngle"/>'s convention.
	/// </remarks>
	/// <param name="rot">The angle to rotate by.</param>
	/// <param name="pivot">The point to rotate around. Does not need to be this pair.</param>
	public XYPair<T> RotatedBy(Angle rot, XYPair<T> pivot) => pivot + (this - pivot).RotatedAroundOriginBy(rot);
	XYPair<T> IPointRotatable2D<XYPair<T>>.RotatedBy(Angle rot, XYPair<float> pivot) => Cast<float>().RotatedBy(rot, pivot).CastWithRoundingIfNecessary<float, T>();
	#endregion

	#region Translation
	/// <summary>
	/// Adds <paramref name="lhs"/> and <paramref name="rhs"/>; equivalent to <see cref="Plus"/>.
	/// </summary>
	/// <param name="lhs">The first pair.</param>
	/// <param name="rhs">The second pair.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator +(XYPair<T> lhs, XYPair<T> rhs) => lhs.Plus(rhs);
	/// <summary>
	/// Returns this pair with <paramref name="other"/>'s <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> added to this pair's own.
	/// </summary>
	/// <param name="other">The pair to add.</param>
	public XYPair<T> Plus(XYPair<T> other) => new(X + other.X, Y + other.Y);
	/// <summary>
	/// Subtracts <paramref name="rhs"/> from <paramref name="lhs"/>; equivalent to <see cref="Minus"/>.
	/// </summary>
	/// <param name="lhs">The pair to subtract from.</param>
	/// <param name="rhs">The pair to subtract.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator -(XYPair<T> lhs, XYPair<T> rhs) => lhs.Minus(rhs);
	/// <summary>
	/// Returns this pair with <paramref name="other"/>'s <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> subtracted from this pair's own.
	/// </summary>
	/// <param name="other">The pair to subtract.</param>
	public XYPair<T> Minus(XYPair<T> other) => new(X - other.X, Y - other.Y);
	/// <summary>
	/// Returns this pair moved by <paramref name="other"/>; equivalent to <see cref="Plus"/>.
	/// </summary>
	/// <param name="other">The offset to move by.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> MovedBy(XYPair<T> other) => Plus(other);
	XYPair<T> ITranslatable2D<XYPair<T>>.MovedBy(XYPair<float> v) => Cast<float>().MovedBy(v).CastWithRoundingIfNecessary<float, T>();
	#endregion

	#region Transformation
	/// <summary>
	/// Applies <paramref name="right"/> to <paramref name="left"/>; equivalent to <see cref="TransformedBy(Transform2D,MidpointRounding)"/>.
	/// </summary>
	/// <param name="left">The pair to transform.</param>
	/// <param name="right">The transform to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> left, Transform2D right) => left.TransformedBy(right);
	/// <inheritdoc cref="operator *(XYPair{T},Transform2D)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(Transform2D left, XYPair<T> right) => right.TransformedBy(left);

	XYPair<T> ITransformable2D<XYPair<T>>.TransformedBy(Transform2D transform) => TransformedBy(transform);
	XYPair<T> ITransformable2D<XYPair<T>>.TransformedByInverseOf(Transform2D transform) => TransformedByInverseOf(transform);
	XYPair<T> IPointTransformable2D<XYPair<T>>.TransformedBy(Transform2D transform, XYPair<float> transformationOrigin) => TransformedBy(transform, transformationOrigin);
	XYPair<T> IPointTransformable2D<XYPair<T>>.TransformedByInverseOf(Transform2D transform, XYPair<float> transformationOrigin) => TransformedByInverseOf(transform, transformationOrigin);
	XYPair<T> IPointTransformable2D<XYPair<T>>.TransformedAroundOriginBy(Transform2D transform) => TransformedAroundOriginBy(transform);
	XYPair<T> IPointTransformable2D<XYPair<T>>.TransformedAroundOriginByInverseOf(Transform2D transform) => TransformedAroundOriginByInverseOf(transform);

	/// <summary>
	/// Returns this pair with <paramref name="transform"/> applied around the origin (<see cref="Zero"/>); equivalent to <see cref="TransformedAroundOriginBy"/>.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> TransformedBy(Transform2D transform, MidpointRounding midpointRounding = MidpointRounding.ToEven) => TransformedAroundOriginBy(transform, midpointRounding);

	/// <summary>
	/// Returns this pair with the inverse of <paramref name="transform"/> applied around the origin (<see cref="Zero"/>); the exact reverse of <see cref="TransformedBy(Transform2D,MidpointRounding)"/>.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> TransformedByInverseOf(Transform2D transform, MidpointRounding midpointRounding = MidpointRounding.ToEven) => TransformedAroundOriginByInverseOf(transform, midpointRounding);

	/// <summary>
	/// Returns this pair with <paramref name="transform"/> applied around the origin (<see cref="Zero"/>).
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public XYPair<T> TransformedAroundOriginBy(Transform2D transform, MidpointRounding midpointRounding = MidpointRounding.ToEven) {
		return XYPair<float>.FromVector2(Vector2.Transform(ToVector2(), transform.ToMatrix()))
			.CastWithRoundingIfNecessary<float, T>(midpointRounding);
	}

	/// <summary>
	/// Returns this pair with the inverse of <paramref name="transform"/> applied around the origin (<see cref="Zero"/>); the exact reverse of <see cref="TransformedAroundOriginBy"/>.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public XYPair<T> TransformedAroundOriginByInverseOf(Transform2D transform, MidpointRounding midpointRounding = MidpointRounding.ToEven) {
		return XYPair<float>.FromVector2(Vector2.Transform(ToVector2(), MathUtils.ForceInvertMatrix(transform.ToMatrix())))
			.CastWithRoundingIfNecessary<float, T>(midpointRounding);
	}

	/// <summary>
	/// Returns this pair with <paramref name="transform"/> applied around <paramref name="transformationOrigin"/>.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	/// <param name="transformationOrigin">The point the transform is applied around. Does not need to be this pair.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public XYPair<T> TransformedBy(Transform2D transform, XYPair<float> transformationOrigin, MidpointRounding midpointRounding = MidpointRounding.ToEven) {
		return XYPair<float>.FromVector2(Vector2.Transform(Cast<float>().MovedBy(-transformationOrigin).ToVector2(), transform.ToMatrix()))
			.MovedBy(transformationOrigin)
			.CastWithRoundingIfNecessary<float, T>(midpointRounding);
	}

	/// <summary>
	/// Returns this pair with the inverse of <paramref name="transform"/> applied around <paramref name="transformationOrigin"/>; the exact reverse of <see cref="TransformedBy(Transform2D,XYPair{float},MidpointRounding)"/>.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="transformationOrigin">The point the inverse transform is applied around. Does not need to be this pair.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public XYPair<T> TransformedByInverseOf(Transform2D transform, XYPair<float> transformationOrigin, MidpointRounding midpointRounding = MidpointRounding.ToEven) {
		return XYPair<float>.FromVector2(Vector2.Transform(Cast<float>().MovedBy(-transformationOrigin).ToVector2(), MathUtils.ForceInvertMatrix(transform.ToMatrix())))
			.MovedBy(transformationOrigin)
			.CastWithRoundingIfNecessary<float, T>(midpointRounding);
	}
	#endregion

	#region Clamping and Interpolation
	static XYPair<T> IInterpolatable<XYPair<T>>.Interpolate(XYPair<T> start, XYPair<T> end, float distance) => Interpolate(start, end, distance);

	/// <summary>
	/// Returns the point <paramref name="distance"/> of the way from <paramref name="start"/> to <paramref name="end"/>.
	/// </summary>
	/// <param name="start">The start value, returned when <paramref name="distance"/> is <c>0f</c>.</param>
	/// <param name="end">The end value, returned when <paramref name="distance"/> is <c>1f</c>.</param>
	/// <param name="distance">How far between <paramref name="start"/> and <paramref name="end"/> to interpolate. Can be outside the range <c>0f &lt;= n &lt;= 1f</c> to extrapolate.</param>
	/// <param name="midpointRounding">How to round the result if <typeparamref name="T"/> is not a floating-point type.</param>
	public static XYPair<T> Interpolate(XYPair<T> start, XYPair<T> end, float distance, MidpointRounding midpointRounding = MidpointRounding.ToEven) {
		return start + (end - start).Cast<float>().ScaledBy(distance).CastWithRoundingIfNecessary<float, T>(midpointRounding);
	}
	/// <inheritdoc/>
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

/// <summary>
/// Extension methods relating to <see cref="XYPair{T}"/>.
/// </summary>
public static class XYPairExtensions {
	/// <summary>
	/// Returns this pair (considered as a 2D vector) scaled to a length of exactly <c>1f</c>.
	/// </summary>
	/// <param name="this">The pair to normalize.</param>
	/// <returns><paramref name="this"/> unchanged if it is <see cref="XYPair{T}.Zero"/> (since it has no meaningful direction to normalize); the unit-length pair otherwise.</returns>
	public static XYPair<float> WithLengthOne(this XYPair<float> @this) => @this.LengthSquared != 0f ? XYPair<float>.FromVector2(Vector2.Normalize(@this.ToVector2())) : @this;

	/// <summary>
	/// Returns the point on the infinite 2D line described by <paramref name="anyPointOn2DLine"/> and <paramref name="unitLength2DLineDirection"/> that is closest to <paramref name="this"/>.
	/// </summary>
	/// <param name="this">The point to measure from.</param>
	/// <param name="anyPointOn2DLine">Any point that lies on the line.</param>
	/// <param name="unitLength2DLineDirection">The direction of the line. Must be unit-length.</param>
	public static XYPair<float> ClosestPointOn2DLine(this XYPair<float> @this, XYPair<float> anyPointOn2DLine, XYPair<float> unitLength2DLineDirection) {
		return (@this - anyPointOn2DLine).Dot(unitLength2DLineDirection) * unitLength2DLineDirection + anyPointOn2DLine;
	}

	/// <summary>
	/// Returns the point on the finite 2D line segment between <paramref name="startPointOf2DBoundedRay"/> and <paramref name="endPointOf2DBoundedRay"/> that is closest to <paramref name="this"/>.
	/// </summary>
	/// <param name="this">The point to measure from.</param>
	/// <param name="startPointOf2DBoundedRay">The start of the line segment.</param>
	/// <param name="endPointOf2DBoundedRay">The end of the line segment.</param>
	public static XYPair<float> ClosestPointOn2DBoundedRay(this XYPair<float> @this, XYPair<float> startPointOf2DBoundedRay, XYPair<float> endPointOf2DBoundedRay) {
		var startToEnd = endPointOf2DBoundedRay - startPointOf2DBoundedRay;
		var maxDistance = startToEnd.Length;
		var direction = startToEnd.WithLengthOne();

		return MathF.Min((@this - startPointOf2DBoundedRay).Dot(direction), maxDistance) * direction + startPointOf2DBoundedRay;
	}

	#region Rounding
	/// <summary>
	/// Rounds this pair's <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> to the nearest whole number, converting the result to <typeparamref name="TNew"/>.
	/// </summary>
	/// <typeparam name="T">The pair's current numeric type.</typeparam>
	/// <typeparam name="TNew">The desired numeric type of the result.</typeparam>
	/// <param name="this">The pair to round.</param>
	/// <param name="midpointRounding">How to round a value that is exactly halfway between two whole numbers.</param>
	public static XYPair<TNew> Round<T, TNew>(this XYPair<T> @this, MidpointRounding midpointRounding = MidpointRounding.ToEven) where T : unmanaged, IFloatingPoint<T> where TNew : unmanaged, INumber<TNew> {
		return new(TNew.CreateSaturating(T.Round(@this.X, midpointRounding)), TNew.CreateSaturating(T.Round(@this.Y, midpointRounding)));
	}

	/// <summary>
	/// Rounds this pair's <see cref="XYPair{T}.X"/> and <see cref="XYPair{T}.Y"/> to <paramref name="roundingDigits"/> decimal places, converting the result to <typeparamref name="TNew"/>.
	/// </summary>
	/// <typeparam name="T">The pair's current numeric type.</typeparam>
	/// <typeparam name="TNew">The desired numeric type of the result.</typeparam>
	/// <param name="this">The pair to round.</param>
	/// <param name="roundingDigits">The number of decimal places to round to.</param>
	/// <param name="midpointRounding">How to round a value that is exactly halfway between two rounded values.</param>
	public static XYPair<TNew> Round<T, TNew>(this XYPair<T> @this, int roundingDigits, MidpointRounding midpointRounding = MidpointRounding.ToEven) where T : unmanaged, IFloatingPoint<T> where TNew : unmanaged, IFloatingPoint<TNew> {
		return new(TNew.CreateSaturating(T.Round(@this.X, roundingDigits, midpointRounding)), TNew.CreateSaturating(T.Round(@this.Y, roundingDigits, midpointRounding)));
	}

	/// <summary>
	/// Converts this pair from <typeparamref name="T"/> to <typeparamref name="TNew"/>, rounding first if (and only if) <typeparamref name="TNew"/> is not a floating-point type.
	/// </summary>
	/// <remarks>
	/// This avoids the double-rounding/truncation that could otherwise occur when converting a fractional floating-point value straight to an integral type: the value is rounded to the nearest whole number first, then converted.
	/// </remarks>
	/// <typeparam name="T">The pair's current (floating-point) numeric type.</typeparam>
	/// <typeparam name="TNew">The desired numeric type of the result.</typeparam>
	/// <param name="this">The pair to convert.</param>
	/// <param name="midpointRounding">How to round a value that is exactly halfway between two whole numbers, if rounding is applied.</param>
	public static XYPair<TNew> CastWithRoundingIfNecessary<T, TNew>(this XYPair<T> @this, MidpointRounding midpointRounding = MidpointRounding.ToEven) where T : unmanaged, IFloatingPoint<T> where TNew : unmanaged, INumber<TNew> {
		return XYPair<TNew>.IsFloatingPoint
			? @this.Cast<TNew>()
			: new(TNew.CreateSaturating(T.Round(@this.X, midpointRounding)), TNew.CreateSaturating(T.Round(@this.Y, midpointRounding)));
	}
	#endregion

	/// <summary>
	/// Treats this pair as the dimensions of a 2D grid and <paramref name="xy"/> as a coordinate within it, returning the equivalent flattened 1D index (row-major, with <see cref="XYPair{T}.X"/> as the row length).
	/// </summary>
	/// <param name="this">The dimensions of the grid.</param>
	/// <param name="xy">The 2D coordinate to flatten.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Index(this XYPair<int> @this, XYPair<int> xy) => @this.X * xy.Y + xy.X;
	/// <inheritdoc cref="Index(XYPair{int},XYPair{int})" />
	/// <param name="this">The dimensions of the grid.</param>
	/// <param name="x">The X component of the 2D coordinate to flatten.</param>
	/// <param name="y">The Y component of the 2D coordinate to flatten.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Index(this XYPair<int> @this, int x, int y) => @this.Index(new(x, y));

	/// <summary>
	/// Equivalent to <see cref="Index(XYPair{int},XYPair{int})"/>, but first clamps <paramref name="xy"/> to lie within the grid described by this pair.
	/// </summary>
	/// <param name="this">The dimensions of the grid.</param>
	/// <param name="xy">The 2D coordinate to clamp and flatten.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexClamped(this XYPair<int> @this, XYPair<int> xy) => @this.Index(xy.Clamp(XYPair<int>.Zero, @this - XYPair<int>.One));
	/// <inheritdoc cref="IndexClamped(XYPair{int},XYPair{int})" />
	/// <param name="this">The dimensions of the grid.</param>
	/// <param name="x">The X component of the 2D coordinate to clamp and flatten.</param>
	/// <param name="y">The Y component of the 2D coordinate to clamp and flatten.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexClamped(this XYPair<int> @this, int x, int y) => @this.IndexClamped(new(x, y));

	/// <summary>
	/// Treats this pair as the dimensions of a 2D grid and <paramref name="index"/> as a flattened 1D index into it (see <see cref="Index(XYPair{int},XYPair{int})"/>), returning the equivalent 2D coordinate.
	/// </summary>
	/// <param name="this">The dimensions of the grid.</param>
	/// <param name="index">The flattened 1D index to expand.</param>
	/// <returns><see cref="XYPair{T}.Zero"/> if this pair's <see cref="XYPair{T}.X"/> is zero (since no grid of that shape can hold any index); the 2D coordinate otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<int> ReverseIndex(this XYPair<int> @this, int index) {
		if (@this.X == 0) return XYPair<int>.Zero;
		var (y, x) = Int32.DivRem(index, @this.X);
		return new(x, y);
	}
	/// <summary>
	/// Equivalent to <see cref="ReverseIndex"/>, but first clamps <paramref name="index"/> to a valid index within the grid described by this pair.
	/// </summary>
	/// <param name="this">The dimensions of the grid.</param>
	/// <param name="index">The flattened 1D index to clamp and expand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<int> ReverseIndexClamped(this XYPair<int> @this, int index) => ReverseIndex(@this, Int32.Clamp(index, 0, Int32.Max(@this.Area - 1, 1)));
}