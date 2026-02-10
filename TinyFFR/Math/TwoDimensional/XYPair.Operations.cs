// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Numerics;

namespace Egodystonic.TinyFFR;

public enum XyPairClockOrientation {
	Colinear = 0,
	Clockwise = -1,
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

	public T Area {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => T.Abs(X * Y);
	}
	public float? Ratio {
		get {
			var v2 = ToVector2();
			if (v2.Y == 0f) return null;
			return v2.X / v2.Y;
		}
	}

	static XYPair<T> IAdditiveIdentity<XYPair<T>, XYPair<T>>.AdditiveIdentity => new(T.Zero, T.Zero);
	static XYPair<T> IMultiplicativeIdentity<XYPair<T>, XYPair<T>>.MultiplicativeIdentity => new(T.One, T.One);

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

	public XYPair<T> WithLength(float newLength, MidpointRounding midpointRounding = MidpointRounding.ToEven) => Cast<float>().WithLengthOne().ScaledBy(newLength).CastWithRoundingIfNecessary<float, T>(midpointRounding);
	public XYPair<T> WithLengthDecreasedBy(float lengthDecrease, MidpointRounding midpointRounding = MidpointRounding.ToEven) => WithLength(Length - lengthDecrease, midpointRounding);
	public XYPair<T> WithLengthIncreasedBy(float lengthIncrease, MidpointRounding midpointRounding = MidpointRounding.ToEven) => WithLength(Length + lengthIncrease, midpointRounding);
	public XYPair<T> WithMaxLength(float maxLength, MidpointRounding midpointRounding = MidpointRounding.ToEven) => WithLength(MathF.Min(Length, maxLength >= 0f ? maxLength : throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be non-negative.")), midpointRounding);
	public XYPair<T> WithMinLength(float minLength, MidpointRounding midpointRounding = MidpointRounding.ToEven) => WithLength(MathF.Max(Length, minLength >= 0f ? minLength : throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Must be non-negative.")), midpointRounding);
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
		return thisAngle.Value.ShortestDifferenceTo(otherAngle.Value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(XYPair<T> lhs, XYPair<T> rhs) => lhs.AngleTo(rhs);
	public Angle SignedAngleTo(XYPair<T> other) => AngleTo(other) * ((int) AngleOrientationTo(other) | 0b1);

	public float Dot(XYPair<T> other) => Vector2.Dot(ToVector2(), other.ToVector2());

	public float Cross(XYPair<T> other) {
		var v = ToVector2();
		var w = other.ToVector2();
		return v.X * w.Y - v.Y * w.X;
	}

	public XyPairClockOrientation AngleOrientationTo(XYPair<T> target) => (XyPairClockOrientation) MathF.Sign(Cross(target));
	XyPairClockOrientation IRelatable<XYPair<T>, XyPairClockOrientation>.RelationshipTo(XYPair<T> other) => AngleOrientationTo(other);
	#endregion

	#region Interactions w/ XYPair
	public float DistanceSquaredFrom(XYPair<T> pair) => Vector2.DistanceSquared(ToVector2(), pair.ToVector2());
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(XYPair<T> pair) => Vector2.Distance(ToVector2(), pair.ToVector2());
	#endregion

	#region Scaling
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> pair, T scalar) => pair.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator /(XYPair<T> pair, T divisor) => new(pair.X / divisor, pair.Y / divisor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(T scalar, XYPair<T> pair) => pair.ScaledBy(scalar);
	public XYPair<T> ScaledBy(T scalar) => new(X * scalar, Y * scalar);
	public XYPair<T> ScaledByReal(float scalar, MidpointRounding midpointRounding = MidpointRounding.ToEven) => Cast<float>().ScaledBy(scalar).CastWithRoundingIfNecessary<float, T>(midpointRounding);
	public XYPair<T> ScaledByReal(XYPair<float> pair, MidpointRounding midpointRounding = MidpointRounding.ToEven) => Cast<float>().ScaledBy(pair).CastWithRoundingIfNecessary<float, T>(midpointRounding);
	public XYPair<T> ScaledByReal(XYPair<float> pair, XYPair<float> scalingOrigin, MidpointRounding midpointRounding = MidpointRounding.ToEven) => Cast<float>().ScaledBy(pair, scalingOrigin).CastWithRoundingIfNecessary<float, T>(midpointRounding);
	static XYPair<T> IMultiplyOperators<XYPair<T>, float, XYPair<T>>.operator *(XYPair<T> pair, float scalar) => ((IScalable<XYPair<T>>) pair).ScaledBy(scalar);
	static XYPair<T> IDivisionOperators<XYPair<T>, float, XYPair<T>>.operator /(XYPair<T> pair, float scalar) => ((IScalable<XYPair<T>>) pair).ScaledBy(1f / scalar);
	static XYPair<T> IMultiplicative<XYPair<T>, float, XYPair<T>>.operator *(float scalar, XYPair<T> pair) => ((IScalable<XYPair<T>>) pair).ScaledBy(scalar);
	XYPair<T> IScalable<XYPair<T>>.ScaledBy(float scalar) => ScaledByReal(scalar);

	public XYPair<T> MultipliedBy(XYPair<T> other) => new(X * other.X, Y * other.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> DividedBy(XYPair<T> other) => new(X / other.X, Y / other.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> left, XYPair<T> right) => left.ScaledBy(right);
	public static XYPair<T> operator /(XYPair<T> left, XYPair<T> right) => new(left.X / right.X, left.Y / right.Y);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> ScaledBy(XYPair<T> vect) => ScaledFromOriginBy(vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> ScaledFromOriginBy(XYPair<T> vect) => MultipliedBy(vect);
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
	public XYPair<T> RotatedAroundOriginBy(Angle rot) => PolarAngle is { } a ? FromPolarAngleAndLength(a + rot, Length) : Zero;
	public XYPair<T> RotatedBy(Angle rot, XYPair<T> pivot) => pivot + (this - pivot).RotatedAroundOriginBy(rot);
	XYPair<T> IPointRotatable2D<XYPair<T>>.RotatedBy(Angle rot, XYPair<float> pivot) => Cast<float>().RotatedBy(rot, pivot).CastWithRoundingIfNecessary<float, T>();
	#endregion

	#region Translation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator +(XYPair<T> lhs, XYPair<T> rhs) => lhs.Plus(rhs);
	public XYPair<T> Plus(XYPair<T> other) => new(X + other.X, Y + other.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator -(XYPair<T> lhs, XYPair<T> rhs) => lhs.Minus(rhs);
	public XYPair<T> Minus(XYPair<T> other) => new(X - other.X, Y - other.Y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> MovedBy(XYPair<T> other) => Plus(other);
	XYPair<T> ITranslatable2D<XYPair<T>>.MovedBy(XYPair<float> v) => Cast<float>().MovedBy(v).CastWithRoundingIfNecessary<float, T>();
	#endregion

	#region Transformation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> left, Transform2D right) => left.TransformedBy(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(Transform2D left, XYPair<T> right) => right.TransformedBy(left);

	XYPair<T> ITransformable2D<XYPair<T>>.TransformedBy(Transform2D transform) => TransformedBy(transform);
	XYPair<T> IPointTransformable2D<XYPair<T>>.TransformedBy(Transform2D transform, XYPair<float> transformationOrigin) => TransformedBy(transform, transformationOrigin);
	XYPair<T> IPointTransformable2D<XYPair<T>>.TransformedAroundOriginBy(Transform2D transform) => TransformedAroundOriginBy(transform);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> TransformedBy(Transform2D transform, MidpointRounding midpointRounding = MidpointRounding.ToEven) => TransformedAroundOriginBy(transform, midpointRounding);
	
	public XYPair<T> TransformedAroundOriginBy(Transform2D transform, MidpointRounding midpointRounding = MidpointRounding.ToEven) {
		return Cast<float>()
			.ScaledBy(transform.Scaling)
			.RotatedAroundOriginBy(transform.Rotation)
			.MovedBy(transform.Translation)
			.CastWithRoundingIfNecessary<float, T>(midpointRounding);
	}

	public XYPair<T> TransformedBy(Transform2D transform, XYPair<float> transformationOrigin, MidpointRounding midpointRounding = MidpointRounding.ToEven) {
		return Cast<float>()
			.ScaledBy(transform.Scaling, transformationOrigin)
			.RotatedBy(transform.Rotation, transformationOrigin)
			.MovedBy(transform.Translation)
			.CastWithRoundingIfNecessary<float, T>(midpointRounding);
	}
	#endregion

	#region Clamping and Interpolation
	static XYPair<T> IInterpolatable<XYPair<T>>.Interpolate(XYPair<T> start, XYPair<T> end, float distance) => Interpolate(start, end, distance);

	public static XYPair<T> Interpolate(XYPair<T> start, XYPair<T> end, float distance, MidpointRounding midpointRounding = MidpointRounding.ToEven) {
		return start + (end - start).Cast<float>().ScaledBy(distance).CastWithRoundingIfNecessary<float, T>(midpointRounding);
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

public static class XYPairExtensions {
	public static XYPair<float> WithLengthOne(this XYPair<float> @this) => @this.LengthSquared != 0f ? XYPair<float>.FromVector2(Vector2.Normalize(@this.ToVector2())) : @this;

	public static XYPair<float> ClosestPointOn2DLine(this XYPair<float> @this, XYPair<float> anyPointOn2DLine, XYPair<float> unitLength2DLineDirection) {
		return (@this - anyPointOn2DLine).Dot(unitLength2DLineDirection) * unitLength2DLineDirection + anyPointOn2DLine;
	}

	public static XYPair<float> ClosestPointOn2DBoundedRay(this XYPair<float> @this, XYPair<float> startPointOf2DBoundedRay, XYPair<float> endPointOf2DBoundedRay) {
		var startToEnd = endPointOf2DBoundedRay - startPointOf2DBoundedRay;
		var maxDistance = startToEnd.Length;
		var direction = startToEnd.WithLengthOne();

		return MathF.Min((@this - startPointOf2DBoundedRay).Dot(direction), maxDistance) * direction + startPointOf2DBoundedRay;
	}

	#region Rounding
	public static XYPair<TNew> Round<T, TNew>(this XYPair<T> @this, MidpointRounding midpointRounding = MidpointRounding.ToEven) where T : unmanaged, IFloatingPoint<T> where TNew : unmanaged, INumber<TNew> {
		return new(TNew.CreateSaturating(T.Round(@this.X, midpointRounding)), TNew.CreateSaturating(T.Round(@this.Y, midpointRounding)));
	}

	public static XYPair<TNew> Round<T, TNew>(this XYPair<T> @this, int roundingDigits, MidpointRounding midpointRounding = MidpointRounding.ToEven) where T : unmanaged, IFloatingPoint<T> where TNew : unmanaged, IFloatingPoint<TNew> {
		return new(TNew.CreateSaturating(T.Round(@this.X, roundingDigits, midpointRounding)), TNew.CreateSaturating(T.Round(@this.Y, roundingDigits, midpointRounding)));
	}

	// TODO xmldoc: Casts from T to TNew but with the given rounding, if and only if TNew represents a non-floating-point type; otherwise no rounding is applied
	public static XYPair<TNew> CastWithRoundingIfNecessary<T, TNew>(this XYPair<T> @this, MidpointRounding midpointRounding = MidpointRounding.ToEven) where T : unmanaged, IFloatingPoint<T> where TNew : unmanaged, INumber<TNew> {
		return XYPair<TNew>.IsFloatingPoint
			? @this.Cast<TNew>()
			: new(TNew.CreateSaturating(T.Round(@this.X, midpointRounding)), TNew.CreateSaturating(T.Round(@this.Y, midpointRounding)));
	}
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Index(this XYPair<int> @this, XYPair<int> xy) => @this.X * xy.Y + xy.X;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Index(this XYPair<int> @this, int x, int y) => @this.Index(new(x, y));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexClamped(this XYPair<int> @this, XYPair<int> xy) => @this.Index(xy.Clamp(XYPair<int>.Zero, @this - XYPair<int>.One));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexClamped(this XYPair<int> @this, int x, int y) => @this.IndexClamped(new(x, y));
}