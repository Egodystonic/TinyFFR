// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR;

public enum XyPairClockOrientation {
	Colinear = 0,
	Clockwise = -1,
	Anticlockwise = 1,
}

partial struct XYPair<T> :
	IAlgebraicRing<XYPair<T>>,
	IInterpolatable<XYPair<T>>,
	IDistanceMeasurable<XYPair<T>>,
	IAngleMeasurable<XYPair<T>, XYPair<T>>,
	IPointTransformable2D<XYPair<T>>,
	ILengthAdjustable<XYPair<T>>,
	IInnerProductSpace<XYPair<T>>,
	IRelatable<XYPair<T>, XYPair<T>, XyPairClockOrientation>  {

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

	public XYPair<TNew> Cast<TNew>() where TNew : unmanaged, INumber<TNew> {
		if (typeof(TNew) == typeof(T)) return Unsafe.As<XYPair<T>, XYPair<TNew>>(ref Unsafe.AsRef(in this));
		return new XYPair<TNew>(TNew.CreateTruncating(X), TNew.CreateTruncating(Y));
	}

	#region Length Modifiers
	public XYPair<T> WithLength(float newLength) => Cast<float>().WithLengthOne().ScaledBy(newLength).Cast<T>();
	public XYPair<T> ShortenedBy(float lengthDecrease) => WithLength(Length - lengthDecrease);
	public XYPair<T> LengthenedBy(float lengthIncrease) => WithLength(Length + lengthIncrease);
	public XYPair<T> WithMaxLength(float maxLength) => WithLength(MathF.Min(Length, maxLength >= 0f ? maxLength : throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be non-negative.")));
	public XYPair<T> WithMinLength(float minLength) => WithLength(MathF.Max(Length, minLength >= 0f ? minLength : throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Must be non-negative.")));
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
	public static XYPair<T> operator /(XYPair<T> pair, T divisor) => pair.ScaledBy(T.One / divisor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(T scalar, XYPair<T> pair) => pair.ScaledBy(scalar);
	public XYPair<T> ScaledBy(T scalar) => new(X * scalar, Y * scalar);

	[OverloadResolutionPriority(-1)]
	public static XYPair<T> operator *(XYPair<T> pair, float scalar) => pair.ScaledBy(scalar);
	[OverloadResolutionPriority(-1)]
	public static XYPair<T> operator /(XYPair<T> pair, float scalar) => pair.ScaledBy(1f / scalar);
	[OverloadResolutionPriority(-1)]
	public static XYPair<T> operator *(float scalar, XYPair<T> pair) => pair.ScaledBy(scalar);
	[OverloadResolutionPriority(-1)]
	public XYPair<T> ScaledBy(float scalar) => Cast<float>().ScaledBy(scalar).Cast<T>();

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
	
	[OverloadResolutionPriority(-1)]
	public XYPair<T> ScaledBy(XYPair<float> vect) => Cast<float>().ScaledBy(vect).Cast<T>();
	[OverloadResolutionPriority(-1)]
	public XYPair<T> ScaledFromOriginBy(XYPair<float> vect) => Cast<float>().ScaledFromOriginBy(vect).Cast<T>();
	[OverloadResolutionPriority(-1)]
	public XYPair<T> ScaledBy(XYPair<float> vect, XYPair<float> scalingOrigin) => Cast<float>().ScaledBy(vect, scalingOrigin).Cast<T>();
	#endregion

	#region Rotation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> left, Angle right) => left.RotatedAroundOriginBy(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(Angle left, XYPair<T> right) => right.RotatedAroundOriginBy(left);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> RotatedBy(Angle rot) => RotatedAroundOriginBy(rot);
	public XYPair<T> RotatedAroundOriginBy(Angle rot) => PolarAngle is { } a ? FromPolarAngleAndLength(a + rot, Length) : Zero;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> left, (Angle Rotation, XYPair<T> Pivot) right) => left.RotatedBy(right.Rotation, right.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> left, (XYPair<T> Pivot, Angle Rotation) right) => left.RotatedBy(right.Rotation, right.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *((Angle Rotation, XYPair<T> Pivot) left, XYPair<T> right) => right.RotatedBy(left.Rotation, left.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *((XYPair<T> Pivot, Angle Rotation) left, XYPair<T> right) => right.RotatedBy(left.Rotation, left.Pivot);
	public XYPair<T> RotatedBy(Angle rot, XYPair<T> pivot) => pivot + (this - pivot).RotatedAroundOriginBy(rot);

	[OverloadResolutionPriority(-1)]
	public static XYPair<T> operator *(XYPair<T> left, (Angle Rotation, XYPair<float> Pivot) right) => left.RotatedBy(right.Rotation, right.Pivot);
	[OverloadResolutionPriority(-1)]
	public static XYPair<T> operator *(XYPair<T> left, (XYPair<float> Pivot, Angle Rotation) right) => left.RotatedBy(right.Rotation, right.Pivot);
	[OverloadResolutionPriority(-1)]
	public static XYPair<T> operator *((Angle Rotation, XYPair<float> Pivot) left, XYPair<T> right) => right.RotatedBy(left.Rotation, left.Pivot);
	[OverloadResolutionPriority(-1)]
	public static XYPair<T> operator *((XYPair<float> Pivot, Angle Rotation) left, XYPair<T> right) => right.RotatedBy(left.Rotation, left.Pivot);
	[OverloadResolutionPriority(-1)]
	public XYPair<T> RotatedBy(Angle rot, XYPair<float> pivot) => Cast<float>().RotatedBy(rot, pivot).Cast<T>();

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

	[OverloadResolutionPriority(-1)]
	public static XYPair<T> operator +(XYPair<T> left, XYPair<float> right) => left.MovedBy(right);
	[OverloadResolutionPriority(-1)]
	public static XYPair<T> operator -(XYPair<T> left, XYPair<float> right) => left.MovedBy(-right);
	[OverloadResolutionPriority(-1)]
	public static XYPair<T> operator +(XYPair<float> left, XYPair<T> right) => right.MovedBy(left);

	[OverloadResolutionPriority(-1)]
	public XYPair<T> MovedBy(XYPair<float> v) => Cast<float>().MovedBy(v).Cast<T>();
	#endregion

	#region Transformation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(XYPair<T> left, Transform2D right) => left.TransformedBy(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair<T> operator *(Transform2D left, XYPair<T> right) => right.TransformedBy(left);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<T> TransformedBy(Transform2D transform) => TransformedAroundOriginBy(transform);
	
	public XYPair<T> TransformedAroundOriginBy(Transform2D transform) {
		return Cast<float>()
			.ScaledBy(transform.Scaling)
			.RotatedBy(transform.Rotation)
			.MovedBy(transform.Translation)
			.Cast<T>();
	}

	public XYPair<T> TransformedBy(Transform2D transform, XYPair<float> transformationOrigin) {
		return Cast<float>()
			.ScaledBy(transform.Scaling, transformationOrigin)
			.RotatedBy(transform.Rotation, transformationOrigin)
			.MovedBy(transform.Translation)
			.Cast<T>();
	}
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