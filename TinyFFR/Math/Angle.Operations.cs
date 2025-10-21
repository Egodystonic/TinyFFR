// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Angle :
	IAlgebraicGroup<Angle>,
	IScalable<Angle>,
	IOrdinal<Angle>,
	INormalizable<Angle>,
	IAbsolutizable<Angle> {
	static Angle IAdditiveIdentity<Angle, Angle>.AdditiveIdentity => Zero;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator -(Angle operand) => operand.Negated;
	public Angle Negated {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromRadians(-Radians);
	}
	Angle IInvertible<Angle>.Inverted => Negated;


	public Angle Absolute { // TODO make it clear that this is not the same as normalizing
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromRadians(MathF.Abs(Radians));
	}
	public Angle Normalized {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromRadians(TrueModulus(Radians, Tau));
	}

	#region Scaling and Addition/Subtraction
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator *(Angle angle, float scalar) => angle.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator *(float scalar, Angle angle) => angle.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator /(Angle angle, float scalar) => FromRadians(angle.Radians / scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ScaledBy(float scalar) => FromRadians(Radians * scalar);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator +(Angle lhs, Angle rhs) => lhs.Plus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator -(Angle lhs, Angle rhs) => lhs.Minus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle Plus(Angle other) => FromRadians(Radians + other.Radians);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle Minus(Angle other) => FromRadians(Radians - other.Radians);

	// TODO xmldoc explain that this is the difference "around the clock" to the other angle; e.g. 270 & 180 = 90; 270 & 90 = 180, etc. Range is always between 0 and 180
	public Angle ShortestDifferenceTo(Angle other) {
		return FromRadians(MathF.Min((this - other).Normalized.Radians, (other - this).Normalized.Radians));
	}
	#endregion

	#region Trigonometry
	public float Sine {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Sin(Radians);
	}
	public float Cosine {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Cos(Radians);
	}

	public Orientation2D PolarOrientation { // TODO make it clear that this is four-quadrant 2D plane direction
		get {
			const float SegmentSize = 45f * DegreesToRadiansRatio;
			const float SegmentHalfSize = SegmentSize / 2f;
			return Normalized.Radians switch {
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
		return FromRadians(Math.Clamp(Radians, min.Radians, max.Radians));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampZeroToHalfCircle() => Clamp(Zero, HalfCircle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampZeroToFullCircle() => Clamp(Zero, FullCircle); // TODO make it clear that this is not the same as normalizing
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampNegativeFullCircleToFullCircle() => Clamp(-FullCircle, FullCircle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampNegativeHalfCircleToHalfCircle() => Clamp(-HalfCircle, HalfCircle);

	// TODO xmldoc this creates a triangle wave when plotting y = x.Triangularize(p) where p is the maximum and -p is the minimum.
	// The period of the wave is peak * 4. Submitting a negative peak flips the wave. 
	public Angle Triangularize(Angle peak) {
		if (peak == Zero) return Zero;
		var period = peak * 4f;
		var ieeeRemainder = MathF.IEEERemainder(Radians / period.Radians, 1f);
		var amplitude = (0.25f - MathF.Abs(0.25f - MathF.Abs(ieeeRemainder))) * period.Radians;
		return FromRadians(MathF.CopySign(amplitude, ieeeRemainder));
	}

	// TODO xmldoc this creates a triangle wave when plotting y = x.TriangularizeRectified(p) where p is the extreme and 0 is the minimum.
	// All results are one side of the x-axis (or 0) and the period of the wave is peak * 2
	public Angle TriangularizeRectified(Angle peak) {
		if (peak == Zero) return Zero;
		var period = peak * 4f;
		var ieeeRemainder = MathF.IEEERemainder(Radians / period.Radians, 1f);
		var amplitude = (0.25f - MathF.Abs(0.25f - MathF.Abs(ieeeRemainder))) * period.Radians;
		return FromRadians(amplitude);
	}

	public static Angle Interpolate(Angle start, Angle end, float distance) => FromRadians(Single.Lerp(start.Radians, end.Radians, distance));
	public static Angle InterpolateShortestDifference(Angle start, Angle end, float distance) {
		var shortestDiff = start.ShortestDifferenceTo(end);
		var isPositiveDelta = end.ShortestDifferenceTo(start + shortestDiff) < end.ShortestDifferenceTo(start - shortestDiff);
		var startNorm = start.Normalized;
		return FromRadians(Single.Lerp(
			startNorm.Radians, 
			startNorm.Radians + (isPositiveDelta ? shortestDiff.Radians : -shortestDiff.Radians), 
			distance
		)).Normalized;
	}
	#endregion

	#region Comparison
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(Angle other) => Radians.CompareTo(other.Radians);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(Angle left, Angle right) => left.Radians > right.Radians;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(Angle left, Angle right) => left.Radians >= right.Radians;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(Angle left, Angle right) => left.Radians < right.Radians;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(Angle left, Angle right) => left.Radians <= right.Radians;
	#endregion
}