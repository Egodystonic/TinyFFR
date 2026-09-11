// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Angle :
	IPhysicalValidityDeterminable,
	IAlgebraicGroup<Angle>,
	IScalable<Angle>,
	IOrdinal<Angle>,
	INormalizable<Angle>,
	IAbsolutizable<Angle> {
	static Angle IAdditiveIdentity<Angle, Angle>.AdditiveIdentity => Zero;

	/// <summary>
	/// Negates <paramref name="operand"/>; equivalent to reading <see cref="Negated"/>.
	/// </summary>
	/// <param name="operand">The value to negate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator -(Angle operand) => operand.Negated;
	/// <summary>
	/// Returns the negated value of this angle (e.g. <c>180°</c> becomes <c>-180°</c>, <c>-90°</c> becomes <c>90°</c>, etc).
	/// </summary>
	public Angle Negated {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromRadians(-Radians);
	}
	Angle IInvertible<Angle>.Inverted => Negated;

	/// <summary>
	/// Determines whether this angle has a finite value.
	/// </summary>
	/// <remarks>
	/// Finite positive, negative, and zero values return <c>true</c>.
	/// Non-finite or NaN values return <c>false</c>.
	/// </remarks>
	public bool IsPhysicallyValid => Single.IsFinite(_radians);

	/// <summary>
	/// Returns the absolute value of this angle (e.g. <c>180°</c> remains <c>180°</c>, <c>-90°</c> becomes <c>90°</c>, etc).
	/// </summary>
	/// <remarks>
	/// Note this is not the same as normalizing, see <see cref="Normalized"/>.
	/// </remarks>
	public Angle Absolute {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromRadians(MathF.Abs(Radians));
	}
	/// <summary>
	/// Returns this angle folded into the range 0° to 360°. The returned value will be equivalent around a circle.
	/// </summary>
	public Angle Normalized {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FromRadians(TrueModulus(Radians, Tau));
	}

	#region Scaling and Addition/Subtraction
	/// <summary>
	/// Multiplies <paramref name="angle"/> by <paramref name="scalar"/>; equivalent to <c>angle.ScaledBy(scalar)</c>.
	/// </summary>
	/// <param name="angle">The angle to scale.</param>
	/// <param name="scalar">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator *(Angle angle, float scalar) => angle.ScaledBy(scalar);
	/// <summary>
	/// Multiplies <paramref name="angle"/> by <paramref name="scalar"/>; equivalent to <c>angle.ScaledBy(scalar)</c>.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	/// <param name="angle">The angle to scale.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator *(float scalar, Angle angle) => angle.ScaledBy(scalar);
	/// <summary>
	/// Divides <paramref name="angle"/> by <paramref name="scalar"/>; equivalent to <c>angle.ScaledBy(1f / scalar)</c>.
	/// </summary>
	/// <param name="angle">The angle to scale.</param>
	/// <param name="scalar">The divisor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator /(Angle angle, float scalar) => FromRadians(angle.Radians / scalar);
	/// <summary>
	/// Returns this angle multiplied by <paramref name="scalar"/> (e.g. scaling <c>90°</c> by <c>2f</c> yields <c>180°</c>).
	/// </summary>
	/// <param name="scalar">The scale factor. Can be negative (which also flips the sign of the angle) or zero (which yields <see cref="Zero"/>).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ScaledBy(float scalar) => FromRadians(Radians * scalar);

	/// <summary>
	/// Adds <paramref name="rhs"/> to <paramref name="lhs"/>; equivalent to <c>lhs.Plus(rhs)</c>.
	/// </summary>
	/// <param name="lhs">The left-hand operand.</param>
	/// <param name="rhs">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator +(Angle lhs, Angle rhs) => lhs.Plus(rhs);
	/// <summary>
	/// Subtracts <paramref name="rhs"/> from <paramref name="lhs"/>; equivalent to <c>lhs.Minus(rhs)</c>.
	/// </summary>
	/// <param name="lhs">The left-hand operand.</param>
	/// <param name="rhs">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator -(Angle lhs, Angle rhs) => lhs.Minus(rhs);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle Plus(Angle other) => FromRadians(Radians + other.Radians);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle Minus(Angle other) => FromRadians(Radians - other.Radians);

	/// <summary>
	/// Calculates the shortest angular difference between this angle and <paramref name="other"/>, "around the clock".
	/// </summary>
	/// <remarks>
	/// Unlike simple subtraction, this treats both angles as positions on a circle rather than raw numeric values, so
	/// the shorter of the two arcs between them is always chosen. For example, the difference between <c>315°</c> and
	/// <c>45°</c> will be given here as <c>90°</c>, not <c>270°</c>.
	/// </remarks>
	/// <param name="other">The other angle to compare to. Can be positive, negative, or zero.</param>
	/// <returns>A value always between 0° and 180°.</returns>
	public Angle ShortestDifferenceTo(Angle other) {
		return FromRadians(MathF.Min((this - other).Normalized.Radians, (other - this).Normalized.Radians));
	}
	#endregion

	#region Trigonometry
	/// <summary>
	/// Returns the sine of this angle.
	/// </summary>
	public float Sine {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Sin(Radians);
	}
	/// <summary>
	/// Returns the cosine of this angle.
	/// </summary>
	public float Cosine {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => MathF.Cos(Radians);
	}

	/// <summary>
	/// Returns the compass-style <see cref="Orientation2D"/> that this angle is nearest to, when this angle is interpreted
	/// as a four-quadrant 2D plane direction (see <see cref="From2DPolarAngle(float,float)"/>).
	/// </summary>
	/// <remarks>
	/// This is approximately the inverse of <see cref="From2DPolarAngle(Orientation2D)"/>: it partitions the circle into
	/// eight 45°-wide segments centred on each <see cref="Orientation2D"/> value and returns whichever segment this
	/// angle's <see cref="Normalized"/> value falls within.
	/// </remarks>
	public Orientation2D PolarOrientation {
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
	/// <inheritdoc />
	public Angle Clamp(Angle min, Angle max) {
		if (min > max) (min, max) = (max, min);
		return FromRadians(Math.Clamp(Radians, min.Radians, max.Radians));
	}

	/// <summary>
	/// Equivalent to <c>Clamp(Angle.Zero, Angle.HalfCircle)</c>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampZeroToHalfCircle() => Clamp(Zero, HalfCircle);
	/// <summary>
	/// Equivalent to <c>Clamp(Angle.Zero, Angle.FullCircle)</c>.
	/// </summary>
	/// <remarks>
	/// Note this is not the same as normalizing; see <see cref="Normalized"/>.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampZeroToFullCircle() => Clamp(Zero, FullCircle);
	/// <summary>
	/// Equivalent to <c>Clamp(-Angle.FullCircle, Angle.FullCircle)</c>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampNegativeFullCircleToFullCircle() => Clamp(-FullCircle, FullCircle);
	/// <summary>
	/// Equivalent to <c>Clamp(-Angle.HalfCircle, Angle.HalfCircle)</c>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampNegativeHalfCircleToHalfCircle() => Clamp(-HalfCircle, HalfCircle);
	/// <summary>
	/// Equivalent to <c>Clamp(-Angle.QuarterCircle, Angle.QuarterCircle)</c>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle ClampNegativeQuarterCircleToQuarterCircle() => Clamp(-QuarterCircle, QuarterCircle);

	/// <summary>
	/// Plots this angle according to the given triangle-wave <paramref name="peak"/> amplitude.
	/// </summary>
	/// <remarks>
	/// When plotting <c>y = x.Triangularize(peak)</c> this function creates a triangle wave oscillating between
	/// <c>peak</c> and <c>-peak</c>. The period of the wave is <c>peak * 4</c>.
	/// <para>
	/// Functions like this are useful for creating a linear oscillation with a fixed amplitude. For example you can make
	/// something "bounce back and forward" over time, or calculate a "bounceback"/"overshoot" function.
	/// </para>
	/// </remarks>
	/// <param name="peak">The maximum value of the triangular peak. Can be negative to flip the resultant wave. If this is 0° the function will always return 0°.</param>
	/// <seealso cref="TriangularizeRectified"/>
	public Angle Triangularize(Angle peak) {
		if (peak == Zero) return Zero;
		var period = peak * 4f;
		var ieeeRemainder = MathF.IEEERemainder(Radians / period.Radians, 1f);
		var amplitude = (0.25f - MathF.Abs(0.25f - MathF.Abs(ieeeRemainder))) * period.Radians;
		return FromRadians(MathF.CopySign(amplitude, ieeeRemainder));
	}

	/// <summary>
	/// Plots this angle according to the given triangle-wave <paramref name="peak"/> amplitude, rectified so that all results sit on one side of
	/// the x-axis.
	/// </summary>
	/// <remarks>
	/// When plotting <c>y = x.Triangularize(peak)</c> this function creates a triangle wave oscillating between
	/// <c>peak</c> and <c>0</c>. The period of the wave is <c>peak * 2</c>.
	/// <para>
	/// Functions like this are useful for creating a linear oscillation with a fixed amplitude. For example you can make
	/// something "bounce back and forward" over time, or calculate a "bounceback"/"overshoot" function.
	/// </para>
	/// </remarks>
	/// <param name="peak">The maximum value of the triangular peak. Can be negative to mirror the resultant wave underneath the x-axis. If this is 0° the function will always return 0°.</param>
	/// <seealso cref="Triangularize"/>
	public Angle TriangularizeRectified(Angle peak) {
		if (peak == Zero) return Zero;
		var period = peak * 4f;
		var ieeeRemainder = MathF.IEEERemainder(Radians / period.Radians, 1f);
		var amplitude = (0.25f - MathF.Abs(0.25f - MathF.Abs(ieeeRemainder))) * period.Radians;
		return FromRadians(amplitude);
	}

	/// <inheritdoc />
	public static Angle Interpolate(Angle start, Angle end, float distance) => FromRadians(Single.Lerp(start.Radians, end.Radians, distance));
	/// <inheritdoc />
	public static float GetInterpolationDistance(Angle start, Angle end, Angle input) => Real.GetInterpolationDistance(start.Radians, end.Radians, input.Radians);

	/// <summary>
	/// Interpolates a value from <paramref name="start"/> to <paramref name="end"/> according to the normalized <paramref name="distance"/>, specifically
	/// taking the shortest path "around the circle" between them.
	/// </summary>
	/// <remarks>
	/// This function differs from <see cref="Interpolate"/> in that it interpolates the shortest path around the circle from <paramref name="start"/> to <paramref name="end"/>,
	/// rather than treating the two parameters purely numerically.
	/// <para>
	/// For example: <c>Angle.Interpolate(0f, 270f, 0.5f)</c> returns 135°. Conversely, <c>Angle.InterpolateShortestPath(0f, 270f, 0.5f)</c> returns 315°.
	/// </para>
	/// </remarks>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate (i.e. <c>0.5f</c> returns the value exactly halfway between start &amp; end).
	/// Values outside the range 0-1 are permitted and will extend the interpolation calculation beyond the start or end value respectively.</param>
	/// <returns>This function only ever returns a <see cref="Normalized"/> value, regardless of the range of <paramref name="start"/> and <paramref name="end"/>.</returns>
	public static Angle InterpolateShortestPath(Angle start, Angle end, float distance) {
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
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(Angle other) => Radians.CompareTo(other.Radians);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(Angle left, Angle right) => left.Radians > right.Radians;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(Angle left, Angle right) => left.Radians >= right.Radians;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(Angle left, Angle right) => left.Radians < right.Radians;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(Angle left, Angle right) => left.Radians <= right.Radians;
	#endregion
}