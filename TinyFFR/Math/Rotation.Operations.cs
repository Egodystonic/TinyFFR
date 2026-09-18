// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static System.Numerics.Quaternion;
using static Egodystonic.TinyFFR.MathUtils;

namespace Egodystonic.TinyFFR;

partial struct Rotation : 
	IPhysicalValidityDeterminable,
	INormalizable<Rotation>,
	IAlgebraicGroup<Rotation>,
	IAngleMeasurable<Rotation, Rotation>,
	IScalable<Rotation>,
	IPrecomputationInterpolatable<Rotation, Pair<Quaternion, Quaternion>> {
	/// <summary>
	/// Negates <paramref name="operand"/>; equivalent to reading <see cref="Reversed"/>.
	/// </summary>
	/// <param name="operand">The rotation to negate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator -(Rotation operand) => operand.Reversed;
	/// <summary>
	/// Returns the rotation that exactly undoes this one (the same axis, with the angle negated).
	/// </summary>
	public Rotation Reversed => new(-Angle, Axis);
	Rotation IInvertible<Rotation>.Inverted => Reversed;
	static Rotation IAdditiveIdentity<Rotation, Rotation>.AdditiveIdentity => None;

	/// <summary>
	/// Determines whether this rotation's angle and axis are physically valid.
	/// </summary>
	/// <remarks>
	/// See: <see cref="Direction.IsPhysicallyValid"/> and <see cref="TinyFFR.Angle.IsPhysicallyValid"/>
	/// </remarks>
	public bool IsPhysicallyValid {
		get {
			return Axis.IsPhysicallyValid && Angle.IsPhysicallyValid;
		}
	}

	/// <summary>
	/// Returns this rotation normalized so that its <see cref="Angle"/> lies in the range <c>0° &lt;= n &lt; 180°</c>, while still representing an equivalent rotation.
	/// </summary>
	/// <remarks>
	/// Because rotating by <c>θ</c> around an axis has the same effect as rotating by <c>360°-θ</c> around the opposite
	/// axis, this canonicalizes any rotation into a single, consistent form: for an input angle in <c>[0°, 180°)</c> the
	/// axis is left unchanged; for an input in <c>[180°, 360°)</c> the axis is flipped and the angle becomes <c>360°-angle</c>;
	/// this then repeats for angles beyond a full circle (e.g. an input in <c>[360°, 540°)</c> behaves like the first case again, and so on).
	/// </remarks>
	public Rotation Normalized {
		get {
			var normalizedAngle = Angle.Normalized;
			return normalizedAngle < Angle.HalfCircle
				? new Rotation(normalizedAngle, Axis)
				: new Rotation(Angle.FullCircle - normalizedAngle, -Axis);
		}
	}

	#region Scaling and Addition/Subtraction
	/// <summary>
	/// Combines <paramref name="lhs"/> and <paramref name="rhs"/>; equivalent to <c>lhs.CombinedAndNormalizedWith(rhs)</c>.
	/// </summary>
	/// <param name="lhs">The rotation applied first.</param>
	/// <param name="rhs">The rotation applied second.</param>
	public static Rotation operator +(Rotation lhs, Rotation rhs) => lhs.CombinedAndNormalizedWith(rhs);
	/// <summary>
	/// Combines <paramref name="lhs"/> with the reverse of <paramref name="rhs"/>; equivalent to <c>lhs.CombinedAndNormalizedWith(rhs.Reversed)</c>.
	/// </summary>
	/// <param name="lhs">The rotation applied first.</param>
	/// <param name="rhs">The rotation whose reverse is applied second.</param>
	public static Rotation operator -(Rotation lhs, Rotation rhs) => lhs.CombinedAndNormalizedWith(rhs.Reversed);
	Rotation IAdditive<Rotation, Rotation, Rotation>.Plus(Rotation other) => CombinedAndNormalizedWith(other);
	Rotation IAdditive<Rotation, Rotation, Rotation>.Minus(Rotation other) => CombinedAndNormalizedWith(other.Reversed);
	/// <summary>
	/// Returns the rotation that, when applied after this one, would result in <paramref name="other"/>; equivalent to <c>other.CombinedAndNormalizedWith(this.Reversed)</c>.
	/// </summary>
	/// <param name="other">The target rotation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation NormalizedDifferenceTo(Rotation other) => CombineAndNormalize(other, Reversed);
	/// <summary>
	/// Returns the rotation equivalent to applying this rotation first, then <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The rotation to apply after this one.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation CombinedAndNormalizedWith(Rotation other) => CombineAndNormalize(this, other);
	/// <summary>
	/// Returns the rotation equivalent to applying this rotation first, then <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The rotation, as a raw <see cref="Quaternion"/>, to apply after this one.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation CombinedAndNormalizedWith(Quaternion other) => FromQuaternionPreNormalized(CombineAndNormalize(ToQuaternion(), other));

	/// <summary>
	/// Returns the rotation equivalent to applying <paramref name="initial"/> first, then <paramref name="following"/>.
	/// </summary>
	/// <param name="initial">The rotation to apply first.</param>
	/// <param name="following">The rotation to apply second.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation CombineAndNormalize(Rotation initial, Rotation following) => FromQuaternionPreNormalized(CombineAndNormalize(initial.ToQuaternion(), following.ToQuaternion()));
	/// <summary>
	/// Returns the quaternion equivalent to applying <paramref name="initial"/> first, then <paramref name="following"/>, normalizing the result.
	/// </summary>
	/// <param name="initial">The rotation, as a raw <see cref="Quaternion"/>, to apply first.</param>
	/// <param name="following">The rotation, as a raw <see cref="Quaternion"/>, to apply second.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion CombineAndNormalize(Quaternion initial, Quaternion following) => NormalizeOrIdentity(following * initial);
	/// <summary>
	/// Returns the quaternion equivalent to applying <paramref name="initial"/> first, then <paramref name="following"/>, without normalizing the result.
	/// </summary>
	/// <remarks>
	/// This is a faster alternative to <see cref="CombineAndNormalize(Quaternion,Quaternion)"/> for when you know the
	/// inputs are already unit-length and can tolerate the small amount of error that repeated combination without
	/// renormalization can accumulate.
	/// </remarks>
	/// <param name="initial">The rotation, as a raw <see cref="Quaternion"/>, to apply first.</param>
	/// <param name="following">The rotation, as a raw <see cref="Quaternion"/>, to apply second.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion Combine(Quaternion initial, Quaternion following) => following * initial;

	/// <summary>
	/// Multiplies <paramref name="rotation"/> by <paramref name="scalar"/>; equivalent to <c>rotation.ScaledBy(scalar)</c>.
	/// </summary>
	/// <param name="rotation">The rotation to scale.</param>
	/// <param name="scalar">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator *(Rotation rotation, float scalar) => rotation.ScaledBy(scalar);
	/// <summary>
	/// Multiplies <paramref name="rotation"/> by <paramref name="scalar"/>; equivalent to <c>rotation.ScaledBy(scalar)</c>.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	/// <param name="rotation">The rotation to scale.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator *(float scalar, Rotation rotation) => rotation.ScaledBy(scalar);
	/// <summary>
	/// Divides <paramref name="rotation"/> by <paramref name="scalar"/>; equivalent to <c>rotation.ScaledBy(1f / scalar)</c>.
	/// </summary>
	/// <param name="rotation">The rotation to scale.</param>
	/// <param name="scalar">The divisor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator /(Rotation rotation, float scalar) => rotation.ScaledBy(1f / scalar);
	/// <summary>
	/// Returns this rotation with its <see cref="Angle"/> multiplied by <paramref name="scalar"/>, keeping the same <see cref="Axis"/>.
	/// </summary>
	/// <param name="scalar">The scale factor. Can be negative (which also reverses the rotation) or zero (which yields <see cref="None"/>).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation ScaledBy(float scalar) => new(Angle * scalar, Axis);

	/// <summary>
	/// Scales the rotation represented by <paramref name="q"/> by <paramref name="scalar"/>, returning the result as a raw <see cref="Quaternion"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="ScaledBy"/>, this operates directly on a quaternion, so it is subject to all ambiguities intrinsic
	/// to Quaternion math; prefer <see cref="ScaledBy"/> except in performance-sensitive scenarios.
	/// </remarks>
	/// <param name="q">The quaternion to scale.</param>
	/// <param name="scalar">The scale factor.</param>
	public static Quaternion ScaleQuaternion(Quaternion q, float scalar) {
		// Quaternion exponentiation
		var halfAngleRadians = MathF.Acos(q.W);
		var newHalfAngleRadians = halfAngleRadians * scalar;
		var (sinNewHalfAngle, cosNewHalfAngle) = MathF.SinCos(newHalfAngleRadians);
		
		var normalizedVectorComponent = Vector3.Normalize(new(q.X, q.Y, q.Z));
		if (!Single.IsFinite(normalizedVectorComponent.X)) return Identity;
		
		return NormalizeOrIdentity(new(
			normalizedVectorComponent * sinNewHalfAngle,
			cosNewHalfAngle
		));
	}

	/// <summary>
	/// Returns this rotation with <paramref name="addition"/> added to its <see cref="Angle"/>, keeping the same <see cref="Axis"/>.
	/// </summary>
	/// <param name="addition">The angle to add. Can be negative, which decreases the angle instead.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation WithAngleIncreasedBy(Angle addition) => new(Angle + addition, Axis);

	/// <summary>
	/// Returns this rotation with <paramref name="subtraction"/> subtracted from its <see cref="Angle"/>, keeping the same <see cref="Axis"/>.
	/// </summary>
	/// <param name="subtraction">The angle to subtract. Can be negative, which increases the angle instead.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation WithAngleDecreasedBy(Angle subtraction) => new(Angle - subtraction, Axis);
	#endregion

	#region Interactions w/ Rotation
	/// <summary>
	/// Calculates the angle between <paramref name="left"/> and <paramref name="right"/>; equivalent to <c>left.NormalizedAngleTo(right)</c>.
	/// </summary>
	/// <param name="left">The first rotation.</param>
	/// <param name="right">The second rotation.</param>
	public static Angle operator ^(Rotation left, Rotation right) => left.NormalizedAngleTo(right);
	Angle IAngleMeasurable<Rotation>.AngleTo(Rotation other) => NormalizedDifferenceTo(other).Angle;
	/// <summary>
	/// Calculates the angle of the rotation needed to turn this rotation into <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The other rotation to compare to.</param>
	/// <returns>This is the <see cref="Angle"/> of <see cref="NormalizedDifferenceTo"/>, so it is always in the range <c>0° &lt;= n &lt; 180°</c>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle NormalizedAngleTo(Rotation other) => NormalizedDifferenceTo(other).Angle;

	/// <summary>
	/// Returns this rotation with <paramref name="rotation"/> applied to its <see cref="Axis"/>, keeping the same <see cref="Angle"/>.
	/// </summary>
	/// <param name="rotation">The rotation to apply to this rotation's axis.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation WithAxisRotatedBy(Rotation rotation) => new(Angle, Axis * rotation);
	#endregion

	#region Rotation
	static Vector4 Rotate(Quaternion q, Vector4 v) {
		var quatVec = new Vector3(q.X, q.Y, q.Z);
		var targetVec = new Vector3(v.X, v.Y, v.Z);
		var t = Vector3.Cross(quatVec, targetVec) * 2f;
		return new Vector4(
			targetVec + q.W * t + Vector3.Cross(quatVec, t),
			v.W
		);
	}

	/// <summary>
	/// Returns <paramref name="d"/> after being turned by this rotation.
	/// </summary>
	/// <remarks>
	/// The result is renormalized back to unit length, correcting for any floating-point error accumulated by the
	/// rotation math. If you're rotating the same direction repeatedly and are sensitive to the small extra cost of
	/// renormalization, see <see cref="RotateWithoutRenormalizing(Direction)"/>.
	/// </remarks>
	/// <param name="d">The direction to rotate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction Rotate(Direction d) => Rotate(d, ToQuaternion());
	/// <summary>
	/// Executes the same function as <see cref="Rotate(Direction)"/> but skips renormalizing the result, trading a small amount of accuracy for speed.
	/// </summary>
	/// <remarks>
	/// Note that continuously applying a non-renormalized operation to a target value will cause it to accrue more and more floating-point degeneracy;
	/// it's advisable to either apply <see cref="Direction.Renormalize"/> at the end of the chain or only apply a small "chain" of non-normalized operations
	/// to a single value.
	/// </remarks>
	/// <param name="d">The direction to rotate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction RotateWithoutRenormalizing(Direction d) => RotateWithoutRenormalizing(d, ToQuaternion());
	/// <summary>
	/// Returns <paramref name="d"/> after being turned by <paramref name="q"/>, renormalized back to unit length.
	/// </summary>
	/// <remarks>
	/// The result is renormalized back to unit length, correcting for any floating-point error accumulated by the
	/// rotation math. If you're rotating the same direction repeatedly and are sensitive to the small extra cost of
	/// renormalization, see <see cref="RotateWithoutRenormalizing(Direction, Quaternion)"/>.
	/// </remarks>
	/// <param name="d">The direction to rotate.</param>
	/// <param name="q">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	public static Direction Rotate(Direction d, Quaternion q) => Direction.Renormalize(RotateWithoutRenormalizing(d, q));
	/// <summary>
	/// Executes the same function as <see cref="Rotate(Direction,Quaternion)"/> but skips renormalizing the result, trading a small amount of accuracy for speed.
	/// </summary>
	/// <remarks>
	/// Note that continuously applying a non-renormalized operation to a target value will cause it to accrue more and more floating-point degeneracy;
	/// it's advisable to either apply <see cref="Direction.Renormalize"/> at the end of the chain or only apply a small "chain" of non-normalized operations
	/// to a single value.
	/// </remarks>
	/// <param name="d">The direction to rotate.</param>
	/// <param name="q">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	public static Direction RotateWithoutRenormalizing(Direction d, Quaternion q) => new(Rotate(q, d.AsVector4));

	/// <summary>
	/// Returns <paramref name="v"/> after being turned by this rotation. The vector's length is unaffected.
	/// </summary>
	/// <remarks>
	/// The result's length is corrected back to <paramref name="v"/>'s original length, correcting for any floating-point
	/// error accumulated by the rotation math. If you're rotating the same vector repeatedly and are sensitive to the
	/// small extra cost of this correction, see <see cref="RotateWithoutCorrectingLength(Vect)"/>.
	/// </remarks>
	/// <param name="v">The vector to rotate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Rotate(Vect v) => Rotate(v, ToQuaternion());
	/// <summary>
	/// Executes the same function as <see cref="Rotate(Vect)"/> but skips correcting the result's length, trading a small amount of accuracy for speed.
	/// </summary>
	/// <remarks>
	/// Note that continuously applying a non-renormalized operation to a target value will cause it to accrue more and more floating-point degeneracy;
	/// it's advisable to either apply <see cref="Direction.Renormalize"/> at the end of the chain or only apply a small "chain" of non-normalized operations
	/// to a single value.
	/// </remarks>
	/// <param name="v">The vector to rotate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect RotateWithoutCorrectingLength(Vect v) => RotateWithoutCorrectingLength(v, ToQuaternion());
	/// <summary>
	/// Returns <paramref name="v"/> after being turned by <paramref name="q"/>, with its length corrected back to <paramref name="v"/>'s original length.
	/// </summary>
	/// <param name="v">The vector to rotate.</param>
	/// <param name="q">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	public static Vect Rotate(Vect v, Quaternion q) => RotateWithoutCorrectingLength(v, q).WithLength(v.Length);
	/// <summary>
	/// Executes the same function as <see cref="Rotate(Vect,Quaternion)"/> but skips correcting the result's length, trading a small amount of accuracy for speed.
	/// </summary>
	/// <remarks>
	/// Note that continuously applying a non-renormalized operation to a target value will cause it to accrue more and more floating-point degeneracy;
	/// it's advisable to either apply <see cref="Direction.Renormalize"/> at the end of the chain or only apply a small "chain" of non-normalized operations
	/// to a single value.
	/// </remarks>
	/// <param name="v">The vector to rotate.</param>
	/// <param name="q">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	public static Vect RotateWithoutCorrectingLength(Vect v, Quaternion q) => new(Rotate(q, v.AsVector4));

	/// <summary>
	/// Calculates how far this rotation turns things around <paramref name="axis"/>, expressed as a signed angle.
	/// </summary>
	/// <remarks>
	/// This is useful for decomposing a rotation's effect on to a specific axis (e.g. "how much does this rotation spin
	/// something around its own up axis?"), regardless of this rotation's own <see cref="Axis"/>.
	/// </remarks>
	/// <param name="axis">The axis to measure the rotation's effect around.</param>
	public Angle AngleAroundAxis(Direction axis) {
		var orthogonalVect = axis.AnyOrthogonal();
		return orthogonalVect.SignedAngleTo(orthogonalVect * this, axis);
	}
	#endregion

	#region Clamping and Interpolation
	/// <inheritdoc />
	/// <remarks>
	/// This turns smoothly and at a constant angular rate from <paramref name="start"/> to <paramref name="end"/> as
	/// <paramref name="distance"/> goes from <c>0f</c> to <c>1f</c>, choosing whichever of <see cref="AccuratelyInterpolate(Rotation,Rotation,float)"/>
	/// or <see cref="ApproximatelyInterpolate(Rotation,Rotation,float)"/> is appropriate for the angle between the two rotations.
	/// </remarks>
	public static Rotation Interpolate(Rotation start, Rotation end, float distance) => FromQuaternion(Interpolate(start.ToQuaternion(), end.ToQuaternion(), distance));
	/// <summary>
	/// Interpolates between two raw quaternions <paramref name="start"/> and <paramref name="end"/> according to the normalized <paramref name="distance"/>.
	/// </summary>
	/// <remarks>
	/// This automatically chooses between <see cref="AccuratelyInterpolate(Quaternion,Quaternion,float)"/> and
	/// <see cref="ApproximatelyInterpolate(Quaternion,Quaternion,float)"/> depending on how close together the two
	/// quaternions are, using the (usually much faster) approximate method wherever it would be indistinguishable from the accurate one.
	/// </remarks>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate.</param>
	public static Quaternion Interpolate(Quaternion start, Quaternion end, float distance) {
		const float CosPhiMinForLinearRenormalization = 1f - 1E-3f;

		return Dot(start, end) switch {
			> CosPhiMinForLinearRenormalization => ApproximatelyInterpolate(start, end, distance),
			< -CosPhiMinForLinearRenormalization => ApproximatelyInterpolate(start, Negate(end), distance),
			_ => AccuratelyInterpolate(start, end, distance)
		};
	}

	/// <summary>
	/// Interpolates between <paramref name="start"/> and <paramref name="end"/> according to the normalized <paramref name="distance"/>, using spherical (constant angular rate) interpolation.
	/// </summary>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate.</param>
	public static Rotation AccuratelyInterpolate(Rotation start, Rotation end, float distance) => FromQuaternion(AccuratelyInterpolate(start.ToQuaternion(), end.ToQuaternion(), distance));
	/// <summary>
	/// Interpolates between two raw quaternions <paramref name="start"/> and <paramref name="end"/> according to the normalized <paramref name="distance"/>, using spherical (constant angular rate) interpolation.
	/// </summary>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate.</param>
	public static Quaternion AccuratelyInterpolate(Quaternion start, Quaternion end, float distance) { // Quaternion slerp
		return Slerp(start, end, distance);
	}

	/// <summary>
	/// Interpolates between <paramref name="start"/> and <paramref name="end"/> according to the normalized <paramref name="distance"/>, using a faster but less accurate linear approximation.
	/// </summary>
	/// <remarks>
	/// This is cheaper to compute than <see cref="AccuratelyInterpolate(Rotation,Rotation,float)"/> but only produces a
	/// close approximation when <paramref name="start"/> and <paramref name="end"/> are less than a quarter-circle (90°) apart;
	/// beyond that, the interpolated rotation's angular rate is no longer approximately constant.
	/// </remarks>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate.</param>
	public static Rotation ApproximatelyInterpolate(Rotation start, Rotation end, float distance) => FromQuaternion(ApproximatelyInterpolate(start.ToQuaternion(), end.ToQuaternion(), distance));
	/// <summary>
	/// Interpolates between two raw quaternions <paramref name="start"/> and <paramref name="end"/> according to the normalized <paramref name="distance"/>, using a faster but less accurate linear approximation.
	/// </summary>
	/// <remarks>
	/// This is cheaper to compute than <see cref="AccuratelyInterpolate(Quaternion,Quaternion,float)"/> but only produces
	/// a close approximation when <paramref name="start"/> and <paramref name="end"/> are less than a quarter-circle (90°) apart.
	/// </remarks>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate.</param>
	public static Quaternion ApproximatelyInterpolate(Quaternion start, Quaternion end, float distance) { // Vector lerp
		return start + (end - start) * distance;
	}

	/// <inheritdoc />
	public static Pair<Quaternion, Quaternion> CreateInterpolationPrecomputation(Rotation start, Rotation end) => new(start.ToQuaternion(), end.ToQuaternion());

	/// <inheritdoc />
	public static Rotation InterpolateUsingPrecomputation(Rotation start, Rotation end, Pair<Quaternion, Quaternion> precomputation, float distance) {
		return FromQuaternion(Interpolate(precomputation.First, precomputation.Second, distance));
	}

	/// <summary>
	/// Returns a rotation around the fixed <paramref name="axis"/>, with its angle interpolated between <paramref name="startAngle"/> and <paramref name="endAngle"/> according to the normalized <paramref name="distance"/>.
	/// </summary>
	/// <param name="startAngle">The starting angle (i.e. the angle used when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="endAngle">The ending angle (i.e. the angle used when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="axis">The fixed axis of the resultant rotation.</param>
	/// <param name="distance">The normalized distance between <paramref name="startAngle"/> and <paramref name="endAngle"/> to calculate.</param>
	public static Rotation Interpolate(Angle startAngle, Angle endAngle, Direction axis, float distance) {
		return new(Angle.Interpolate(startAngle, endAngle, distance), axis);
	}

	/// <summary>
	/// Clamps this rotation's <see cref="Angle"/> and <see cref="Axis"/> independently between the corresponding components of <paramref name="min"/> and <paramref name="max"/>.
	/// </summary>
	/// <remarks>
	/// This is a fairly esoteric operation: it breaks the rotation down in to its constituent angle and axis and clamps
	/// each separately (via <see cref="Egodystonic.TinyFFR.Angle.Clamp"/> and <see cref="Egodystonic.TinyFFR.Direction.Clamp(Direction,Direction)"/> respectively), which rarely
	/// corresponds to a meaningful real-world constraint on a rotation. If you're looking to restrict a direction to
	/// within a cone or arc, see the various <c>Clamp</c> overloads on <see cref="Egodystonic.TinyFFR.Direction"/> instead.
	/// <see cref="None"/> is treated as a fixed point: if this rotation, <paramref name="min"/>, or <paramref name="max"/> is <see cref="None"/>, this method returns <c>this</c> unchanged.
	/// </remarks>
	/// <param name="min">The lower bound for both the angle and axis.</param>
	/// <param name="max">The upper bound for both the angle and axis.</param>
	public Rotation Clamp(Rotation min, Rotation max) {
		if (this == None || min == None || max == None) return this;

		min = min.Normalized;
		max = max.Normalized;

		var (minAngle, minAxis) = min;
		var (maxAngle, maxAxis) = max;
		return new(Angle.Clamp(minAngle, maxAngle), Axis.Clamp(minAxis, maxAxis));
	}
	#endregion
}