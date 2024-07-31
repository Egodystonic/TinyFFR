// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Vect :
	IAlgebraicRing<Vect>,
	IScalable<Vect>,
	IRotatable<Vect>,
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
	internal const float DefaultRandomRange = 100f;

	public float this[Axis axis] => axis switch {
		Axis.X => X,
		Axis.Y => Y,
		Axis.Z => Z,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} must not be anything except {nameof(Axis.X)}, {nameof(Axis.Y)} or {nameof(Axis.Z)}.")
	};
	public XYPair<float> this[Axis first, Axis second] => new(this[first], this[second]);
	public Vect this[Axis first, Axis second, Axis third] => new(this[first], this[second], this[third]);
	static Vect IAdditiveIdentity<Vect, Vect>.AdditiveIdentity => Zero;

	public float Length {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Length();
	}
	public float LengthSquared {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.LengthSquared();
	}
	public bool IsUnitLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get {
			const float FloatingPointErrorMargin = 1E-3f;
			return MathF.Abs(1f - LengthSquared) < FloatingPointErrorMargin;
		}
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator -(Vect operand) => operand.Flipped;
	public Vect Flipped {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-AsVector4);
	}
	Vect IInvertible<Vect>.Inverted => Flipped;
	public Vect? Reciprocal {
		get {
			if (X == 0f || Y == 0f || Z == 0f) return null;
			return new Vect(1f / X, 1f / Y, 1f / Z);
		}
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator +(Vect lhs, Vect rhs) => lhs.Plus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Plus(Vect other) => new(AsVector4 + other.AsVector4);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator -(Vect lhs, Vect rhs) => lhs.Minus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Minus(Vect other) => new(AsVector4 - other.AsVector4);


	public static Vect operator *(Vect left, Vect right) => left.MultipliedBy(right);
	public static Vect operator /(Vect left, Vect right) => right.MultipliedBy(left);
	public Vect MultipliedBy(Vect other) => new(AsVector4 * other.AsVector4);
	public Vect DividedBy(Vect other) => new(AsVector4 / other.AsVector4);
	static Vect IMultiplicativeIdentity<Vect, Vect>.MultiplicativeIdentity => new(1f, 1f, 1f);


	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(NormalizeOrZero(AsVector4));
	}
	public Vect AsUnitLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(NormalizeOrZero(AsVector4));
	}


	// Friendlier name for the maths-impaired like me
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float LengthWhenProjectedOnTo(Direction d) => Dot(d);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Dot(Vect other) => Vector4.Dot(AsVector4, other.AsVector4);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Dot(Direction other) => Vector4.Dot(AsVector4, other.AsVector4);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Cross(Vect other) => FromVector3(Vector3.Cross(ToVector3(), other.ToVector3()));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect Cross(Direction other) => FromVector3(Vector3.Cross(ToVector3(), other.ToVector3()));


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ProjectedOnTo(Direction d) => d * LengthWhenProjectedOnTo(d);
	Vect? IProjectable<Vect, Direction>.ProjectedOnTo(Direction d) => ProjectedOnTo(d);
	Vect IProjectable<Vect, Direction>.FastProjectedOnTo(Direction d) => ProjectedOnTo(d);

	public Vect? OrthogonalizedAgainst(Direction d) {
		var orthogonalizedDir = Direction.OrthogonalizedAgainst(d);
		return orthogonalizedDir == null ? null : orthogonalizedDir * Length;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizedAgainst(Direction d) => Direction.FastOrthogonalizedAgainst(d) * Length;

	public Vect? ParallelizedWith(Direction d) {
		var parallelizedDir = Direction.ParallelizedWith(d);
		return parallelizedDir == null ? null : parallelizedDir * Length;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizedWith(Direction d) => Direction.FastParallelizedWith(d) * Length;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizedAgainst(Vect other) => OrthogonalizedAgainst(other.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizedAgainst(Vect other) => FastOrthogonalizedAgainst(other.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ProjectedOnTo(Vect other) => ProjectedOnTo(other.Direction);
	Vect? IProjectable<Vect, Vect>.ProjectedOnTo(Vect other) => ProjectedOnTo(other);
	Vect IProjectable<Vect, Vect>.FastProjectedOnTo(Vect other) => ProjectedOnTo(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ParallelizedWith(Vect other) => ParallelizedWith(other.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizedWith(Vect other) => FastParallelizedWith(other.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ProjectionOf(Vect other) => other.ProjectedOnTo(this);
	Vect? IProjectionTarget<Vect>.ProjectionOf(Vect other) => ProjectionOf(other);
	Vect IProjectionTarget<Vect>.FastProjectionOf(Vect other) => ProjectionOf(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizationOf(Vect other) => other.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizationOf(Vect other) => other.FastOrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ParallelizationOf(Vect other) => other.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizationOf(Vect other) => other.FastParallelizedWith(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction d) => d.IsOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction d) => d.IsParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction d) => d.IsApproximatelyOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction d, Angle tolerance) => d.IsApproximatelyOrthogonalTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction d) => d.IsApproximatelyParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction d, Angle tolerance) => d.IsApproximatelyParallelTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Vect other) => IsOrthogonalTo(other.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Vect other) => IsParallelTo(other.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Vect other) => IsApproximatelyOrthogonalTo(other.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Vect other, Angle tolerance) => IsApproximatelyOrthogonalTo(other.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Vect other) => IsApproximatelyParallelTo(other.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Vect other, Angle tolerance) => IsApproximatelyParallelTo(other.Direction, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Vect d, Rotation r) => r.Rotate(d);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Rotation r, Vect d) => r.Rotate(d);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect RotatedBy(Rotation rotation) => rotation.Rotate(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location AsLocation() => (Location) this;



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Vect vectOperand, float scalarOperand) => vectOperand.ScaledBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(float scalarOperand, Vect vectOperand) => vectOperand.ScaledBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator /(Vect vectOperand, float scalarOperand) => vectOperand.ScaledBy(1f / scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ScaledBy(float scalar) => new(Multiply(AsVector4, scalar));


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithLength(float newLength) => new(Direction.AsVector4 * newLength);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithLengthOne() => AsUnitLength;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ShortenedBy(float lengthDecrease) => WithLength(Length - lengthDecrease);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect LengthenedBy(float lengthIncrease) => WithLength(Length + lengthIncrease);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithMaxLength(float maxLength) => WithLength(MathF.Min(Length, maxLength));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithMinLength(float minLength) => WithLength(MathF.Max(Length, minLength));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect WithDirection(Direction newDirection) => newDirection * Length;

	public static Vect Interpolate(Vect start, Vect end, float distance) {
		return start + (end - start) * distance;
	}

	public Vect Clamp(Vect min, Vect max) => AsLocation().ClosestPointOn(new BoundedRay(min.AsLocation(), max.AsLocation())).AsVect();

	public static Vect CreateNewRandom() {
		return new Vect(
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive()
		) * DefaultRandomRange;
	}
	public static Vect CreateNewRandom(Vect minInclusive, Vect maxExclusive) {
		return new(
			RandomUtils.NextSingle(minInclusive.X, maxExclusive.X),
			RandomUtils.NextSingle(minInclusive.Y, maxExclusive.Y),
			RandomUtils.NextSingle(minInclusive.Z, maxExclusive.Z)
		);
	}
}