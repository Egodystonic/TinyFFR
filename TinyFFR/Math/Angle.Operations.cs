// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Angle : 
	IAlgebraicGroup<Angle>,
	IScalable<Angle>, 
	IOrdinal<Angle> {
	static Angle IAdditiveIdentity<Angle, Angle>.AdditiveIdentity => Zero;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator -(Angle operand) => operand.Negated;
	public Angle Negated {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromRadians(-AsRadians);
	}
	Angle IInvertible<Angle>.Inverted => Negated;


	public Angle Absolute { // TODO make it clear that this is not the same as normalizing
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromRadians(MathF.Abs(AsRadians));
	}
	public Angle Normalized {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromRadians(TrueModulus(AsRadians, Tau));
	}

	#region Scaling and Addition/Subtraction
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator *(Angle angle, float scalar) => angle.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator *(float scalar, Angle angle) => angle.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator /(Angle angle, float scalar) => FromRadians(angle.AsRadians / scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ScaledBy(float scalar) => FromRadians(AsRadians * scalar);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator +(Angle lhs, Angle rhs) => lhs.Plus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator -(Angle lhs, Angle rhs) => lhs.Minus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle Plus(Angle other) => FromRadians(AsRadians + other.AsRadians);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle Minus(Angle other) => FromRadians(AsRadians - other.AsRadians);

	// TODO xmldoc explain that this is the difference "around the clock" to the other angle; e.g. 270 & 180 = 90; 270 & 90 = 180, etc. Range is always between 0 and 180
	public Angle AbsoluteDifferenceTo(Angle other) {
		return FromRadians(MathF.Min((this - other).Normalized.AsRadians, (other - this).Normalized.AsRadians));
	}
	#endregion

	#region Trigonometry
	public float Sine {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Sin(AsRadians);
	}
	public float Cosine {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Cos(AsRadians);
	}

	public Orientation2D PolarOrientation { // TODO make it clear that this is four-quadrant 2D plane direction
		get {
			const float SegmentSize = 45f * DegreesToRadiansRatio;
			const float SegmentHalfSize = SegmentSize / 2f;
			return Normalized.AsRadians switch {
				< SegmentHalfSize + SegmentSize * 0f => Orientation2D.Right,
				< SegmentHalfSize + SegmentSize * 1f => Orientation2D.UpRight,
				< SegmentHalfSize + SegmentSize * 2f => Orientation2D.Up,
				< SegmentHalfSize + SegmentSize * 3f => Orientation2D.UpLeft,
				< SegmentHalfSize + SegmentSize * 4f => Orientation2D.Left,
				< SegmentHalfSize + SegmentSize * 5f => Orientation2D.DownLeft,
				< SegmentHalfSize + SegmentSize * 6f => Orientation2D.Down,
				< SegmentHalfSize + SegmentSize * 7f => Orientation2D.DownRight,
				_ => Orientation2D.Right
			};
		}
	}
	#endregion

	#region Clamping and Interpolation
	public Angle Clamp(Angle min, Angle max) {
		if (min > max) (min, max) = (max, min);
		return FromRadians(Math.Clamp(AsRadians, min.AsRadians, max.AsRadians));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampZeroToHalfCircle() => Clamp(Zero, HalfCircle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampZeroToFullCircle() => Clamp(Zero, FullCircle); // TODO make it clear that this is not the same as normalizing
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampNegativeFullCircleToFullCircle() => Clamp(-FullCircle, FullCircle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampNegativeHalfCircleToHalfCircle() => Clamp(-HalfCircle, HalfCircle);

	public static Angle Interpolate(Angle start, Angle end, float distance) => FromRadians(Single.Lerp(start.AsRadians, end.AsRadians, distance));
	#endregion

	#region Comparison
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(Angle other) => AsRadians.CompareTo(other.AsRadians);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(Angle left, Angle right) => left.AsRadians > right.AsRadians;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(Angle left, Angle right) => left.AsRadians >= right.AsRadians;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(Angle left, Angle right) => left.AsRadians < right.AsRadians;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(Angle left, Angle right) => left.AsRadians <= right.AsRadians;
	#endregion
}