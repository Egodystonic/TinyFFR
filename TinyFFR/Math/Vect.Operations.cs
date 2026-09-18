// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Vect :
	IPhysicalValidityDeterminable,
	IAlgebraicRing<Vect>,
	IAbsolutizable<Vect>,
	IRotatable<Vect>,
	IIndependentAxisScalable<Vect>,
	IInnerProductSpace<Vect>,
	IVectorProductSpace<Vect>,
	ILengthAdjustable<Vect>,
	IOrthogonalizable<Vect, Direction>,
	IProjectable<Vect, Direction>, 
	IParallelizable<Vect, Direction>, 
	IOrthogonalizable<Vect, Vect>,
	IProjectable<Vect, Vect>, 
	IParallelizable<Vect, Vect>,
	IProjectionTarget<Vect, Vect>,
	IOrthogonalizationTarget<Vect, Vect>,
	IParallelizationTarget<Vect, Vect> { 
	static Vect IAdditiveIdentity<Vect, Vect>.AdditiveIdentity => Zero;

	/// <summary>
	/// Returns the length (magnitude) of this vector.
	/// </summary>
	public float Length {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Length();
	}
	/// <summary>
	/// Returns the square of the length (magnitude) of this vector.
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="Length"/> as it avoids a square root, and is sufficient when you only need to compare lengths rather than know the exact value.
	/// </remarks>
	public float LengthSquared {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.LengthSquared();
	}
	/// <summary>
	/// Determines whether this vector's <see cref="Length"/> is (approximately) <c>1f</c>.
	/// </summary>
	public bool IsUnitLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get {
			const float FloatingPointErrorMargin = 1E-3f;
			return MathF.Abs(1f - LengthSquared) < FloatingPointErrorMargin;
		}
	}
	/// <summary>
	/// Returns this vector scaled to a length of exactly <c>1f</c>, preserving its direction.
	/// </summary>
	/// <remarks>
	/// If this is <see cref="Zero"/>, the result is also <see cref="Zero"/> rather than an invalid or <c>NaN</c> vector.
	/// </remarks>
	public Vect AsUnitLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(NormalizeOrZero(AsVector4));
	}
	/// <summary>
	/// Returns the <see cref="Egodystonic.TinyFFR.Direction"/> this vector points in, discarding its length.
	/// </summary>
	/// <remarks>
	/// If this is <see cref="Zero"/>, the result is <see cref="Egodystonic.TinyFFR.Direction.None"/>.
	/// </remarks>
	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(NormalizeOrZero(AsVector4));
	}

	/// <summary>
	/// Negates <paramref name="operand"/>; equivalent to reading <see cref="Reversed"/>.
	/// </summary>
	/// <param name="operand">The vector to negate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator -(Vect operand) => operand.Reversed;
	/// <summary>
	/// Returns this vector pointing in the opposite direction, with the same length.
	/// </summary>
	public Vect Reversed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-AsVector4);
	}
	Vect IInvertible<Vect>.Inverted => Reversed;

	/// <summary>
	/// Returns this vector with all components positive.
	/// </summary>
	public Vect Absolute {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(Abs(AsVector4));
	}

	/// <inheritdoc />
	/// <remarks>
	/// The reciprocal is calculated component-wise (i.e. <c>(1/X, 1/Y, 1/Z)</c>), so this is <see langword="null"/> if
	/// <i>any</i> single component of this vector is <c>0f</c>, not only when the whole vector is <see cref="Zero"/>.
	/// </remarks>
	public Vect? Reciprocal {
		get {
			if (X == 0f || Y == 0f || Z == 0f) return null;
			return new Vect(1f / X, 1f / Y, 1f / Z);
		}
	}

	/// <summary>
	/// Returns the largest of this vector's three components.
	/// </summary>
	public float MaxComponent {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Max(X, Y, Z);
	}
	/// <summary>
	/// Returns the smallest of this vector's three components.
	/// </summary>
	public float MinComponent {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Min(X, Y, Z);
	}
	/// <summary>
	/// Returns whichever of this vector's three components has the largest absolute value (magnitude), ignoring sign.
	/// </summary>
	public float MaxComponentMagnitude {
		get => new Vect(Abs(AsVector4)).MaxComponent;
	}
	/// <summary>
	/// Returns whichever of this vector's three components has the smallest absolute value (magnitude), ignoring sign.
	/// </summary>
	public float MinComponentMagnitude {
		get => new Vect(Abs(AsVector4)).MinComponent;
	}

	/// <summary>
	/// Converts this vector to a <see cref="Location"/> by treating its components as coordinates.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location AsLocation() => (Location) this;

	/// <summary>
	/// Determines whether this vector has finite <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/> values.
	/// </summary>
	public bool IsPhysicallyValid => Single.IsFinite(X) && Single.IsFinite(Y) && Single.IsFinite(Z);

	#region With Methods
	/// <summary>
	/// Returns this vector scaled to <paramref name="newLength"/>, preserving its direction.
	/// </summary>
	/// <param name="newLength">The desired length of the resultant vector. Can be negative, in which case the resultant vector points opposite to this one.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithLength(float newLength) => new(Direction.AsVector4 * newLength);
	/// <summary>
	/// Returns this vector scaled to a length of exactly <c>1f</c>; equivalent to reading <see cref="AsUnitLength"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithLengthOne() => AsUnitLength;
	/// <summary>
	/// Returns this vector with its length reduced by <paramref name="lengthDecrease"/>.
	/// </summary>
	/// <remarks>
	/// If <paramref name="lengthDecrease"/> is greater than this vector's <see cref="Length"/>, the result flips to point
	/// in the opposite direction (e.g. reducing a vector of length <c>7</c> by <c>17</c> yields a vector of length <c>10</c>
	/// pointing the other way), rather than clamping at zero.
	/// </remarks>
	/// <param name="lengthDecrease">The amount to reduce the length by. Can be negative, which increases the length instead.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithLengthDecreasedBy(float lengthDecrease) => WithLength(Length - lengthDecrease);
	/// <summary>
	/// Returns this vector with its length increased by <paramref name="lengthIncrease"/>.
	/// </summary>
	/// <param name="lengthIncrease">The amount to increase the length by. Can be negative, which decreases the length instead (see <see cref="WithLengthDecreasedBy"/>).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithLengthIncreasedBy(float lengthIncrease) => WithLength(Length + lengthIncrease);
	/// <summary>
	/// Returns this vector, shortened if necessary so its length does not exceed <paramref name="maxLength"/>.
	/// </summary>
	/// <param name="maxLength">The maximum permitted length of the resultant vector. Must be non-negative.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxLength"/> is negative.</exception>
	public Vect WithMaxLength(float maxLength) => WithLength(MathF.Min(Length, maxLength >= 0f ? maxLength : throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be non-negative.")));
	/// <summary>
	/// Returns this vector, lengthened if necessary so its length is not less than <paramref name="minLength"/>.
	/// </summary>
	/// <param name="minLength">The minimum permitted length of the resultant vector. Must be non-negative.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="minLength"/> is negative.</exception>
	public Vect WithMinLength(float minLength) => WithLength(MathF.Max(Length, minLength >= 0f ? minLength : throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Must be non-negative.")));

	/// <summary>
	/// Returns a vector with the same length as this one, but pointing in <paramref name="newDirection"/>.
	/// </summary>
	/// <param name="newDirection">The desired direction of the resultant vector.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithDirection(Direction newDirection) => newDirection * Length;
	#endregion

	#region Scaling and Addition/Subtraction
	/// <summary>
	/// Adds <paramref name="rhs"/> to <paramref name="lhs"/>; equivalent to <c>lhs.Plus(rhs)</c>.
	/// </summary>
	/// <param name="lhs">The left-hand operand.</param>
	/// <param name="rhs">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator +(Vect lhs, Vect rhs) => lhs.Plus(rhs);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Plus(Vect other) => new(AsVector4 + other.AsVector4);
	/// <summary>
	/// Subtracts <paramref name="rhs"/> from <paramref name="lhs"/>; equivalent to <c>lhs.Minus(rhs)</c>.
	/// </summary>
	/// <param name="lhs">The left-hand operand.</param>
	/// <param name="rhs">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator -(Vect lhs, Vect rhs) => lhs.Minus(rhs);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Minus(Vect other) => new(AsVector4 - other.AsVector4);

	/// <summary>
	/// Multiplies <paramref name="left"/> by <paramref name="right"/> component-wise; equivalent to <c>left.MultipliedBy(right)</c>.
	/// </summary>
	/// <param name="left">The left-hand operand.</param>
	/// <param name="right">The right-hand operand.</param>
	public static Vect operator *(Vect left, Vect right) => left.MultipliedBy(right);
	/// <summary>
	/// Divides <paramref name="left"/> by <paramref name="right"/> component-wise; equivalent to <c>left.DividedBy(right)</c>.
	/// </summary>
	/// <param name="left">The left-hand operand.</param>
	/// <param name="right">The right-hand operand.</param>
	public static Vect operator /(Vect left, Vect right) => left.DividedBy(right);
	/// <inheritdoc />
	/// <remarks>
	/// The multiplication is applied component-wise (i.e. <c>(X*other.X, Y*other.Y, Z*other.Z)</c>), not as a dot or cross product.
	/// </remarks>
	public Vect MultipliedBy(Vect other) => new(AsVector4 * other.AsVector4);
	/// <inheritdoc />
	/// <remarks>
	/// The division is applied component-wise (i.e. <c>(X/other.X, Y/other.Y, Z/other.Z)</c>). If any component of <paramref name="other"/>
	/// is <c>0f</c> (or the division otherwise produces a non-finite result), this returns <see cref="Zero"/> rather than
	/// a vector containing infinite or <c>NaN</c> components.
	/// </remarks>
	public Vect DividedBy(Vect other) {
		var v3 = ToVector3() / other.ToVector3();
		if (!Single.IsFinite(v3.X) || !Single.IsFinite(v3.Y) || !Single.IsFinite(v3.Z)) return Zero;
		else return FromVector3(v3);
	}
	static Vect IMultiplicativeIdentity<Vect, Vect>.MultiplicativeIdentity => One;

	/// <summary>
	/// Multiplies <paramref name="vectOperand"/> by <paramref name="scalarOperand"/>; equivalent to <c>vectOperand.ScaledBy(scalarOperand)</c>.
	/// </summary>
	/// <param name="vectOperand">The vector to scale.</param>
	/// <param name="scalarOperand">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Vect vectOperand, float scalarOperand) => vectOperand.ScaledBy(scalarOperand);
	/// <summary>
	/// Multiplies <paramref name="vectOperand"/> by <paramref name="scalarOperand"/>; equivalent to <c>vectOperand.ScaledBy(scalarOperand)</c>.
	/// </summary>
	/// <param name="scalarOperand">The scale factor.</param>
	/// <param name="vectOperand">The vector to scale.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(float scalarOperand, Vect vectOperand) => vectOperand.ScaledBy(scalarOperand);
	/// <summary>
	/// Divides <paramref name="vectOperand"/> by <paramref name="scalarOperand"/>.
	/// </summary>
	/// <remarks>
	/// If <paramref name="scalarOperand"/> is <c>0f</c> (or the division otherwise produces a non-finite result), this
	/// returns <see cref="Zero"/> rather than a vector containing infinite or <c>NaN</c> components.
	/// </remarks>
	/// <param name="vectOperand">The vector to scale.</param>
	/// <param name="scalarOperand">The divisor.</param>
	public static Vect operator /(Vect vectOperand, float scalarOperand) {
		var reciprocal = 1f / scalarOperand;
		return Single.IsFinite(reciprocal) ? vectOperand.ScaledBy(reciprocal) : Zero;
	}

	/// <summary>
	/// Returns this vector multiplied uniformly by <paramref name="scalar"/>.
	/// </summary>
	/// <param name="scalar">The scale factor. Can be negative (which also flips the vector's direction) or zero (which yields <see cref="Zero"/>).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ScaledBy(float scalar) => new(Multiply(AsVector4, scalar));
	/// <summary>
	/// Returns this vector scaled independently per axis by <paramref name="vect"/>'s components; equivalent to <c>this.MultipliedBy(vect)</c>.
	/// </summary>
	/// <param name="vect">The per-axis scale factors to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ScaledBy(Vect vect) => MultipliedBy(vect);
	#endregion

	#region Interactions w/ Direction
	/// <summary>
	/// Returns the length of this vector's projection on to <paramref name="d"/>; equivalent to <c>this.Dot(d)</c>.
	/// </summary>
	/// <param name="d">The direction to project on to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float LengthWhenProjectedOnTo(Direction d) => Dot(d);
	/// <summary>
	/// Calculates the dot product of this vector and <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// Because <paramref name="other"/> is unit-length, this is equivalent to the length of this vector's projection on
	/// to <paramref name="other"/> (see <see cref="LengthWhenProjectedOnTo"/>): positive when they broadly point the
	/// same way, negative when they broadly point opposite ways, and zero when they are orthogonal (perpendicular).
	/// </remarks>
	/// <param name="other">The direction to calculate the dot product with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Dot(Direction other) => Vector4.Dot(AsVector4, other.AsVector4);
	/// <summary>
	/// Calculates the cross product of this vector and <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// The result is a vector perpendicular to both operands, with a length proportional to both operands' lengths and
	/// to how far from parallel they are (shrinking to <see cref="Zero"/> as they become parallel).
	/// </remarks>
	/// <param name="other">The direction to calculate the cross product with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Cross(Direction other) => FromVector3(Vector3.Cross(ToVector3(), other.ToVector3()));

	/// <summary>
	/// Returns this vector's projection on to <paramref name="d"/>.
	/// </summary>
	/// <param name="d">The direction to project on to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ProjectedOnTo(Direction d) => d * LengthWhenProjectedOnTo(d);
	Vect? IProjectable<Vect, Direction>.ProjectedOnTo(Direction d) => ProjectedOnTo(d);
	Vect IProjectable<Vect, Direction>.FastProjectedOnTo(Direction d) => ProjectedOnTo(d);

	/// <summary>
	/// Attempts to orthogonalize this vector against <paramref name="d"/>.
	/// Orthogonalization refers to adjusting this vector's direction such that it forms an angle exactly 90° with the target (<paramref name="d"/>), preserving its length.
	/// </summary>
	/// <param name="d">The target direction. Can be <see cref="Egodystonic.TinyFFR.Direction.None"/> (in which case this function returns <c>this</c>).</param>
	/// <returns>This vector adjusted such that it forms a 90° angle with <paramref name="d"/>; or <c>null</c> if there is
	/// no single answer (i.e. this vector's direction is exactly parallel or exactly opposite to <paramref name="d"/>).
	/// If this vector is <see cref="Zero"/> or <paramref name="d"/> is <see cref="Egodystonic.TinyFFR.Direction.None"/>, returns <c>this</c>.</returns>
	public Vect? OrthogonalizedAgainst(Direction d) {
		var orthogonalizedDir = Direction.OrthogonalizedAgainst(d);
		return orthogonalizedDir == null ? null : orthogonalizedDir * Length;
	}
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(Direction)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this vector is not <see cref="Zero"/>, <paramref name="d"/> is not <see cref="Egodystonic.TinyFFR.Direction.None"/>,
	/// and this vector's direction is not parallel to <paramref name="d"/>. The returned value of this function is undefined when any condition above is broken.
	/// </remarks>
	/// <param name="d">The target direction.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizedAgainst(Direction d) => Direction.FastOrthogonalizedAgainst(d) * Length;

	/// <summary>
	/// Attempts to parallelize this vector with <paramref name="d"/>.
	/// Parallelization refers to adjusting this vector's direction such that it forms an angle of exactly 0° or 180° with the target (<paramref name="d"/>), preserving its length.
	/// </summary>
	/// <param name="d">The target direction. Can be <see cref="Egodystonic.TinyFFR.Direction.None"/> (in which case this function returns <c>this</c>).</param>
	/// <returns>This vector's length applied to either <paramref name="d"/> or <c>-</c><paramref name="d"/> (whichever
	/// is closer to this vector's direction); or <c>null</c> if there is no single answer (i.e. this vector's direction
	/// is exactly orthogonal to <paramref name="d"/>). If this vector is <see cref="Zero"/> or <paramref name="d"/> is
	/// <see cref="Egodystonic.TinyFFR.Direction.None"/>, returns <c>this</c>.</returns>
	public Vect? ParallelizedWith(Direction d) {
		var parallelizedDir = Direction.ParallelizedWith(d);
		return parallelizedDir == null ? null : parallelizedDir * Length;
	}
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(Direction)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this vector is not <see cref="Zero"/>, <paramref name="d"/> is not <see cref="Egodystonic.TinyFFR.Direction.None"/>,
	/// and this vector's direction is not orthogonal to <paramref name="d"/>. The returned value of this function is undefined when any condition above is broken.
	/// </remarks>
	/// <param name="d">The target direction.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizedWith(Direction d) => Direction.FastParallelizedWith(d) * Length;

	/// <summary>
	/// Determines whether this vector's direction is exactly orthogonal (perpendicular) to <paramref name="d"/>; equivalent to <c>d.IsOrthogonalTo(this)</c>.
	/// </summary>
	/// <param name="d">The direction to compare to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction d) => d.IsOrthogonalTo(this);
	/// <summary>
	/// Determines whether this vector's direction is exactly parallel (or exactly opposite) to <paramref name="d"/>; equivalent to <c>d.IsParallelTo(this)</c>.
	/// </summary>
	/// <param name="d">The direction to compare to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction d) => d.IsParallelTo(this);
	/// <summary>
	/// Determines whether this vector's direction is orthogonal (perpendicular) to <paramref name="d"/>, within <see cref="Egodystonic.TinyFFR.Direction.DefaultParallelOrthogonalTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="d">The direction to compare to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction d) => d.IsApproximatelyOrthogonalTo(this);
	/// <summary>
	/// Determines whether this vector's direction is orthogonal (perpendicular) to <paramref name="d"/>, within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="d">The direction to compare to.</param>
	/// <param name="tolerance">How far away from exactly 90° the angle between this vector's direction and <paramref name="d"/> is allowed to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction d, Angle tolerance) => d.IsApproximatelyOrthogonalTo(this, tolerance);
	/// <summary>
	/// Determines whether this vector's direction is parallel (or opposite) to <paramref name="d"/>, within <see cref="Egodystonic.TinyFFR.Direction.DefaultParallelOrthogonalTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="d">The direction to compare to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction d) => d.IsApproximatelyParallelTo(this);
	/// <summary>
	/// Determines whether this vector's direction is parallel (or opposite) to <paramref name="d"/>, within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="d">The direction to compare to.</param>
	/// <param name="tolerance">How far away from exactly 0° or exactly 180° the angle between this vector's direction and <paramref name="d"/> is allowed to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction d, Angle tolerance) => d.IsApproximatelyParallelTo(this, tolerance);
	#endregion

	#region Interactions w/ Vect
	/// <summary>
	/// Calculates the dot product of this vector and <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// The result is positive when the two vectors broadly point the same way, negative when they broadly point
	/// opposite ways, and zero when they are orthogonal (perpendicular); its magnitude also scales with both vectors' lengths.
	/// </remarks>
	/// <param name="other">The other vector to calculate the dot product with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Dot(Vect other) => Vector4.Dot(AsVector4, other.AsVector4);
	/// <summary>
	/// Calculates the cross product of this vector and <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// The result is a vector perpendicular to both operands, with a length proportional to both operands' lengths and
	/// to how far from parallel they are (shrinking to <see cref="Zero"/> as they become parallel).
	/// </remarks>
	/// <param name="other">The other vector to calculate the cross product with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Cross(Vect other) => FromVector3(Vector3.Cross(ToVector3(), other.ToVector3()));

	/// <summary>
	/// Attempts to orthogonalize this vector against <paramref name="other"/>'s direction; equivalent to <c>this.OrthogonalizedAgainst(other.Direction)</c>.
	/// </summary>
	/// <param name="other">The target vector.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizedAgainst(Vect other) => OrthogonalizedAgainst(other.Direction);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(Vect)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="other">The target vector.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizedAgainst(Vect other) => FastOrthogonalizedAgainst(other.Direction);
	/// <summary>
	/// Returns this vector's projection on to <paramref name="other"/>'s direction; equivalent to <c>this.ProjectedOnTo(other.Direction)</c>.
	/// </summary>
	/// <param name="other">The vector to project on to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ProjectedOnTo(Vect other) => ProjectedOnTo(other.Direction);
	Vect? IProjectable<Vect, Vect>.ProjectedOnTo(Vect other) => ProjectedOnTo(other);
	Vect IProjectable<Vect, Vect>.FastProjectedOnTo(Vect other) => ProjectedOnTo(other);
	/// <summary>
	/// Attempts to parallelize this vector with <paramref name="other"/>'s direction; equivalent to <c>this.ParallelizedWith(other.Direction)</c>.
	/// </summary>
	/// <param name="other">The target vector.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ParallelizedWith(Vect other) => ParallelizedWith(other.Direction);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(Vect)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="other">The target vector.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizedWith(Vect other) => FastParallelizedWith(other.Direction);
	/// <summary>
	/// Equivalent to <c>other.ProjectedOnTo(this)</c>.
	/// </summary>
	/// <param name="other">The vector to project.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ProjectionOf(Vect other) => other.ProjectedOnTo(this);
	Vect? IProjectionTarget<Vect>.ProjectionOf(Vect other) => ProjectionOf(other);
	Vect IProjectionTarget<Vect>.FastProjectionOf(Vect other) => ProjectionOf(other);
	/// <summary>
	/// Equivalent to <c>other.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="other">The vector to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizationOf(Vect other) => other.OrthogonalizedAgainst(this);
	/// <summary>
	/// Equivalent to <c>other.FastOrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="other">The vector to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizationOf(Vect other) => other.FastOrthogonalizedAgainst(this);
	/// <summary>
	/// Equivalent to <c>other.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="other">The vector to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ParallelizationOf(Vect other) => other.ParallelizedWith(this);
	/// <summary>
	/// Equivalent to <c>other.FastParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="other">The vector to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizationOf(Vect other) => other.FastParallelizedWith(this);

	/// <summary>
	/// Determines whether this vector's direction is exactly orthogonal (perpendicular) to <paramref name="other"/>'s direction; equivalent to <c>this.IsOrthogonalTo(other.Direction)</c>.
	/// </summary>
	/// <param name="other">The vector to compare to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Vect other) => IsOrthogonalTo(other.Direction);
	/// <summary>
	/// Determines whether this vector's direction is exactly parallel (or exactly opposite) to <paramref name="other"/>'s direction; equivalent to <c>this.IsParallelTo(other.Direction)</c>.
	/// </summary>
	/// <param name="other">The vector to compare to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Vect other) => IsParallelTo(other.Direction);
	/// <summary>
	/// Determines whether this vector's direction is orthogonal (perpendicular) to <paramref name="other"/>'s direction, within <see cref="Egodystonic.TinyFFR.Direction.DefaultParallelOrthogonalTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="other">The vector to compare to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Vect other) => IsApproximatelyOrthogonalTo(other.Direction);
	/// <summary>
	/// Determines whether this vector's direction is orthogonal (perpendicular) to <paramref name="other"/>'s direction, within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="other">The vector to compare to.</param>
	/// <param name="tolerance">How far away from exactly 90° the angle between the two vectors' directions is allowed to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Vect other, Angle tolerance) => IsApproximatelyOrthogonalTo(other.Direction, tolerance);
	/// <summary>
	/// Determines whether this vector's direction is parallel (or opposite) to <paramref name="other"/>'s direction, within <see cref="Egodystonic.TinyFFR.Direction.DefaultParallelOrthogonalTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="other">The vector to compare to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Vect other) => IsApproximatelyParallelTo(other.Direction);
	/// <summary>
	/// Determines whether this vector's direction is parallel (or opposite) to <paramref name="other"/>'s direction, within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="other">The vector to compare to.</param>
	/// <param name="tolerance">How far away from exactly 0° or exactly 180° the angle between the two vectors' directions is allowed to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Vect other, Angle tolerance) => IsApproximatelyParallelTo(other.Direction, tolerance);
	#endregion

	#region Rotation
	/// <summary>
	/// Returns <paramref name="d"/> after being turned by <paramref name="r"/>; equivalent to <c>d.RotatedBy(r)</c>.
	/// </summary>
	/// <param name="d">The vector to rotate.</param>
	/// <param name="r">The rotation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Vect d, Rotation r) => r.Rotate(d);
	/// <summary>
	/// Returns <paramref name="d"/> after being turned by <paramref name="r"/>; equivalent to <c>d.RotatedBy(r)</c>.
	/// </summary>
	/// <param name="r">The rotation to apply.</param>
	/// <param name="d">The vector to rotate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Rotation r, Vect d) => r.Rotate(d);
	/// <summary>
	/// Returns this vector after being turned by <paramref name="rotation"/>. The vector's length is unaffected.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect RotatedBy(Rotation rotation) => rotation.Rotate(this);
	/// <summary>
	/// Returns this vector after being turned by <paramref name="rotationQuaternion"/>. The vector's length is unaffected.
	/// </summary>
	/// <param name="rotationQuaternion">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect RotatedBy(Quaternion rotationQuaternion) => Rotation.Rotate(this, rotationQuaternion);
	#endregion

	#region Clamping and Interpolation
	/// <inheritdoc />
	public static Vect Interpolate(Vect start, Vect end, float distance) {
		return start + (end - start) * distance;
	}

	/// <summary>
	/// Clamps this vector on to the line segment between <paramref name="min"/> and <paramref name="max"/>.
	/// </summary>
	/// <remarks>
	/// Unlike a per-axis clamp, the result is always the closest point on the straight line segment connecting
	/// <paramref name="min"/> and <paramref name="max"/> to this vector, which may differ on every axis from the
	/// unclamped input if this vector lies off to the side of that segment.
	/// </remarks>
	/// <param name="min">One end of the line segment to clamp within.</param>
	/// <param name="max">The other end of the line segment to clamp within.</param>
	public Vect Clamp(Vect min, Vect max) => AsLocation().ClosestPointOn(new BoundedRay(min.AsLocation(), max.AsLocation())).AsVect();
	#endregion
}