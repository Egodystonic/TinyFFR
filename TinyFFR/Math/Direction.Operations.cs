// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

public readonly record struct NearestOrientationResult<TOrientation>(TOrientation AsEnum, Direction AsDirection) where TOrientation : Enum;

partial struct Direction :
	IInvertible<Direction>,
	IMultiplyOperators<Direction, float, Vect>,
	IModulusOperators<Direction, Angle, Rotation>,
	IPrecomputationInterpolatable<Direction, Rotation>,
	IInnerProductSpace<Direction>,
	IVectorProductSpace<Direction>,
	IAngleMeasurable<Direction, Direction>,
	ITransitionRepresentable<Direction, Rotation>,
	IRotatable<Direction>,
	IOrthogonalizable<Direction, Direction>,
	IParallelizable<Direction, Direction>,
	IProjectionTarget<Direction, Vect>,
	IOrthogonalizationTarget<Direction, Vect>,
	IParallelizationTarget<Direction, Vect>,
	IOrthogonalizationTarget<Direction, Direction>,
	IParallelizationTarget<Direction, Direction> {
	public const float DefaultParallelOrthogonalTestApproximationDegrees = 0.1f;
	const float ParallelComponentsCheckErrorMargin = 1E-5f;
	const float OrthogonalDotErrorMargin = 1E-5f;

	public float this[Axis axis] => axis switch {
		Axis.X => X,
		Axis.Y => Y,
		Axis.Z => Z,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} must not be anything except {nameof(Axis.X)}, {nameof(Axis.Y)} or {nameof(Axis.Z)}.")
	};
	public XYPair<float> this[Axis first, Axis second] => new(this[first], this[second]);
	public Direction this[Axis first, Axis second, Axis third] => new(this[first], this[second], this[third]);

	internal bool IsUnitLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get {
			const float FloatingPointErrorMargin = 2E-3f;
			return MathF.Abs(AsVector4.LengthSquared() - 1f) < FloatingPointErrorMargin;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction operator -(Direction operand) => operand.Flipped;
	public Direction Flipped {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-AsVector4);
	}
	Direction IInvertible<Direction>.Inverted => Flipped;

	public NearestOrientationResult<CardinalOrientation3D> NearestOrientationCardinal {
		get {
			GetNearestDirectionAndOrientation(this, AllCardinals, out var e, out var d);
			return new((CardinalOrientation3D) e, d);
		}
	}
	public NearestOrientationResult<IntercardinalOrientation3D> NearestOrientationIntercardinal {
		get {
			GetNearestDirectionAndOrientation(this, AllIntercardinals, out var e, out var d);
			return new((IntercardinalOrientation3D) e, d);
		}
	}
	public NearestOrientationResult<DiagonalOrientation3D> NearestOrientationDiagonal {
		get {
			GetNearestDirectionAndOrientation(this, AllDiagonals, out var e, out var d);
			return new((DiagonalOrientation3D) e, d);
		}
	}
	public NearestOrientationResult<Orientation3D> NearestOrientation {
		get {
			GetNearestDirectionAndOrientation(this, AllOrientations, out var e, out var d);
			return new(e, d);
		}
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect AsVect() => (Vect) this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Direction directionOperand, float scalarOperand) => directionOperand.AsVect(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(float scalarOperand, Direction directionOperand) => directionOperand.AsVect(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect AsVect(float length) => new(AsVector4 * length);


	// TODO in XMLDoc indicate that this is the dot product of the two directions, and that therefore the range is 1 for identical, to -1 for complete opposite, with 0 being orthogonal; and that this is the cosine of the angle
	// Maintainer's note: Clamping dot product is necessary to prevent nasty issues elsewhere.
	// FP inaccuracy can result in values outside the -1 to 1 range, which then really fucks up stuff like inverse trigonometic functions (arccos/arcsin will return NaN).
	// The expectation is that any two unit vectors' dot product will always be in the [-1, 1] range, so this just helps ensure that.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Dot(Direction other) => Single.Clamp(Vector4.Dot(AsVector4, other.AsVector4), -1f, 1f);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Dot(Vect other) => other.Dot(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction Cross(Direction other) => FromVector3(Vector3.Cross(ToVector3(), other.ToVector3()));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction Cross(Vect other) => FromVector3(Vector3.Cross(ToVector3(), other.ToVector3()));



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Direction d1, Direction d2) => Angle.FromAngleBetweenDirections(d1, d2);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Direction other) => Angle.FromAngleBetweenDirections(this, other);

	public Direction AnyPerpendicular() {
		return FromVector3(Vector3.Cross(
			ToVector3(),
			MathF.Abs(Z) > MathF.Abs(X) ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f)
		));
	}

	public Direction? OrthogonalizedAgainst(Direction d) {
		if (this == None || d == None) return this;
		if (IsParallelTo(d)) return null;
		return new(Normalize(AsVector4 - d.AsVector4 * Dot(d)));
	}
	public Direction FastOrthogonalizedAgainst(Direction d) => new(Normalize(AsVector4 - d.AsVector4 * Vector4.Dot(AsVector4, d.AsVector4)));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizationOf(Vect v) => v.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizationOf(Vect v) => v.FastOrthogonalizedAgainst(this);

	public Direction? ParallelizedWith(Direction d) {
		if (this == None || d == None) return this;
		var dot = Vector4.Dot(AsVector4, d.AsVector4);
		if (MathF.Abs(dot) < OrthogonalDotErrorMargin) return null;
		return new(d.AsVector4 * MathF.Sign(dot));
	}
	public Direction FastParallelizedWith(Direction d) => new(d.AsVector4 * MathF.Sign(Vector4.Dot(AsVector4, d.AsVector4)));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ParallelizationOf(Vect v) => v.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizationOf(Vect v) => v.FastParallelizedWith(this);

	Vect ProjectionOf(Vect v) => v.ProjectedOnTo(this);
	Vect? IProjectionTarget<Vect>.ProjectionOf(Vect v) => ProjectionOf(v);
	Vect IProjectionTarget<Vect>.FastProjectionOf(Vect v) => ProjectionOf(v);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? OrthogonalizationOf(Direction d) => d.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastOrthogonalizationOf(Direction d) => d.FastOrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? ParallelizationOf(Direction d) => d.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastParallelizationOf(Direction d) => d.FastParallelizedWith(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // TODO make it clear in xmldoc that this is prone to fp inaccuracy, better to use approximate variant in most cases, but this is faster and helps determine whether ParallelizedWith will return null
	public bool IsOrthogonalTo(Direction other) => MathF.Abs(Vector4.Dot(AsVector4, other.AsVector4)) < OrthogonalDotErrorMargin && this != None && other != None;
	public bool IsApproximatelyOrthogonalTo(Direction other) => IsApproximatelyOrthogonalTo(other, DefaultParallelOrthogonalTestApproximationDegrees);
	public bool IsApproximatelyOrthogonalTo(Direction other, Angle tolerance) {
		if (this == None || other == None) return false;
		return AngleTo(other).Equals(Angle.QuarterCircle, tolerance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // TODO make it clear in xmldoc that this is prone to fp inaccuracy, better to use approximate variant in most cases, but this is faster, but this is faster and helps determine whether OrthogonalizedWith will return null
	public bool IsParallelTo(Direction other) => (Equals(other, ParallelComponentsCheckErrorMargin) || Equals(-other, ParallelComponentsCheckErrorMargin)) && this != None;
	public bool IsApproximatelyParallelTo(Direction other) => IsApproximatelyParallelTo(other, DefaultParallelOrthogonalTestApproximationDegrees);
	public bool IsApproximatelyParallelTo(Direction other, Angle tolerance) {
		if (this == None || other == None) return false;
		var angle = AngleTo(other);
		return angle.Equals(Angle.Zero, tolerance) || angle.Equals(Angle.HalfCircle, tolerance);
	}

	public bool IsOrthogonalTo(Vect v) => IsOrthogonalTo(v.Direction);
	public bool IsApproximatelyOrthogonalTo(Vect v) => IsApproximatelyOrthogonalTo(v, DefaultParallelOrthogonalTestApproximationDegrees);
	public bool IsApproximatelyOrthogonalTo(Vect v, Angle tolerance) => IsApproximatelyOrthogonalTo(v.Direction, tolerance);
	public bool IsParallelTo(Vect v) => IsParallelTo(v.Direction);
	public bool IsApproximatelyParallelTo(Vect v) => IsApproximatelyParallelTo(v, DefaultParallelOrthogonalTestApproximationDegrees);
	public bool IsApproximatelyParallelTo(Vect v, Angle tolerance) => IsApproximatelyParallelTo(v.Direction, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator >>(Direction start, Direction end) => Rotation.FromStartAndEndDirection(start, end);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator <<(Direction end, Direction start) => Rotation.FromStartAndEndDirection(start, end);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation RotationTo(Direction other) => Rotation.FromStartAndEndDirection(this, other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation RotationFrom(Direction other) => Rotation.FromStartAndEndDirection(other, this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator %(Direction axis, Angle angle) => new(angle, axis);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator %(Angle angle, Direction axis) => new(angle, axis);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction RotatedBy(Rotation rotation) => rotation.Rotate(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction operator *(Direction d, Rotation r) => r.Rotate(d);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction operator *(Rotation r, Direction d) => r.Rotate(d);

	public static Direction Interpolate(Direction start, Direction end, float distance) {
		return Rotation.FromStartAndEndDirection(start, end).ScaledBy(distance) * start;
	}
	public static Rotation CreateInterpolationPrecomputation(Direction start, Direction end) {
		return Rotation.FromStartAndEndDirection(start, end);
	}
	public static Direction InterpolateUsingPrecomputation(Direction start, Direction end, Rotation precomputation, float distance) {
		return precomputation.ScaledBy(distance) * start;
	}

	// TODO xmldoc that this clamps on to the arc between two directions in 3D space. It's a mistake to try to clamp on any arc >= 180deg
	// TODO because this function will clamp to the shortest arc between min and max (so trying to restrict to 270deg viewing arc for example is wrong)
	// TODO if the user wants to clamp within a 3D cone (even >= 90deg cone), they should use the other Clamp overload.
	// TODO If the user wants to clamp within a 2D arc, they should use the other other Clamp overload
	public Direction Clamp(Direction min, Direction max) {
		// Doesn't make sense to clamp to "None", so return this
		if (min == None || max == None || this == None) return this;

		// Create a plane that is aligned with the great circle formed between min and max on the unit sphere, 
		// and then create a dimension converter to convert min, max, and this to 2D; with 'min' representing the X-axis
		// (and therefore min in 2D is equal to <1, 0>). Because min is set by definition to be <1, 0> (e.g. the X-axis
		// basis), its polar angle will be, by definition, 0 degrees.
		var minLoc = (Location) min;
		var maxLoc = (Location) max;
		var thisLoc = (Location) this;
		var greatCirclePlane = Plane.FromTriangleOnSurface(minLoc, maxLoc, Location.Origin);
		if (greatCirclePlane == null) {
			// If min and max are antipodal then the entire range of possible directions is valid, so just return this
			if (min.Equals(-max, 0.5f)) return this;
			// Else if min and max are the same then we just return min
			else return min;
		}
		var converter = greatCirclePlane.Value.CreateDimensionConverter(Location.Origin, min);

		// Project max and this on to the 2D plane. Then, compare their polar angles. If this direction's polar angle is between 0 and
		// max's polar angle, it already lies on the arc (after projection), so return the renormalization of the projected value back in
		// to 3D (along the great-circle plane).
		// Otherwise, if the polar angle is greater than max's, we just need to find which point is closer (min or max). We do that by seeing
		// how far around the great circle 'this' is-- if it's further than halfway around from max back to min, we return min; otherwise
		// we return max (that's the final if statement at the bottom).
		// Finally, if this direction was projected down to <0, 0> it is exactly perpendicular to the plane, and therefore perpendicular
		// to the arc. "ThisAngle" will be null, and the if check will result in 'false', and we'll fall through to the final
		// check below, returning 'min'. This is fine, as anywhere on the arc is equally valid here.
		var maxProjection = converter.Convert(maxLoc);
		var thisProjection = converter.Convert(thisLoc);
		var thisAngle = thisProjection.PolarAngle;
		if (thisAngle < maxProjection.PolarAngle) return FromVector3(converter.Convert(thisProjection).ToVector3());
		var midpoint = (Angle.FullCircle - maxProjection.PolarAngle) * 0.5f + maxProjection.PolarAngle;
		return (thisAngle < midpoint) ? max : min;
	}

	public Direction Clamp(Direction target, Angle maxDifference) {
		if (target == None || this == None) return this;
		maxDifference = maxDifference.ClampZeroToHalfCircle();

		var difference = target ^ this;
		if (difference <= maxDifference) return this;

		return (target >> this).ScaledBy(maxDifference.AsRadians / difference.AsRadians) * target;
	}

	// TODO xmldoc: Clamps on to a given arc (arcCentre + maxDiff) around a plane (plane); either leaving this direction directly clamped on to the plane or still with the 3D component (retainEtc). Also mention that the plane's location is not used, only its normal
	// TODO xmldoc make it clear that the max arc difference is split across the centre direction; so e.g. a max diff of 90deg will result in 45deg either side
	public Direction Clamp(Plane plane, Direction arcCentre, Angle maxArcCentreDifference, bool retainOrthogonalDimension) {
		if (this == None || arcCentre == None) return this;
		if (arcCentre.ParallelizedWith(plane) == null) return this;
		var halfArc = maxArcCentreDifference.ClampZeroToFullCircle() * 0.5f;
		var converter = plane.CreateDimensionConverter(Location.Origin, arcCentre);
		var resultOnPlane = converter.ConvertDisregardingOrigin((Location) this);
		var polarAngle = (resultOnPlane with { Y = -resultOnPlane.Y }).PolarAngle; // We have to flip Y because the co-ordinate system after 2D conversion has inverted coordinates
		if (polarAngle == null) return this;

		// Outside the max arc diff
		if (polarAngle.Value.NormalizedDifferenceTo(Angle.Zero) > halfArc) {
			resultOnPlane = XYPair<float>.FromPolarAngleAndLength(polarAngle > Angle.HalfCircle ? halfArc : -halfArc, resultOnPlane.ToVector2().Length());
		}

		var result = FromVector3(converter.ConvertDisregardingOrigin(resultOnPlane).ToVector3());
		if (!retainOrthogonalDimension) return result;

		var angleToPlane = SignedAngleTo(plane);
		return (result >> plane.Normal).WithAngle(angleToPlane) * result;
	}

	public static Direction CreateNewRandom() {
		Direction result;
		do {
			result = new(
				RandomUtils.NextSingleNegOneToOneInclusive(),
				RandomUtils.NextSingleNegOneToOneInclusive(),
				RandomUtils.NextSingleNegOneToOneInclusive()
			);
		} while (result == None);
		return result;
	}
	public static Direction CreateNewRandom(Direction minInclusive, Direction maxExclusive) {
		return (minInclusive >> maxExclusive).ScaledBy(RandomUtils.NextSingle()) * minInclusive;
	}
	public static Direction CreateNewRandom(Direction coneCentre, Angle coneAngleMax) => CreateNewRandom(coneCentre, coneAngleMax, Angle.Zero);
	public static Direction CreateNewRandom(Direction coneCentre, Angle coneAngleMax, Angle coneAngleMin) {
		if (coneCentre == None) return CreateNewRandom();

		var offset = coneCentre * (coneCentre >> coneCentre.AnyPerpendicular()).WithAngle(Angle.CreateNewRandom(coneAngleMin.ClampZeroToHalfCircle(), coneAngleMax.ClampZeroToHalfCircle()));
		return offset * new Rotation(Angle.CreateNewRandom(Angle.Zero, Angle.FullCircle), coneCentre);
	}
	public static Direction CreateNewRandom(Plane plane) => CreateNewRandom(plane, plane.Normal.AnyPerpendicular(), Angle.FullCircle);
	public static Direction CreateNewRandom(Plane plane, Direction arcCentre, Angle arcAngle) {
		if (arcCentre.ParallelizedWith(plane) == null) arcCentre = plane.Normal.AnyPerpendicular();
		var halfAngle = arcAngle * 0.5f;
		return FromPlaneAndPolarAngle(plane, arcCentre, Angle.CreateNewRandom(-halfAngle, halfAngle)); 
	}

	public static int GetIndexOfNearestDirectionInSpan(Direction targetDir, ReadOnlySpan<Direction> span) {
		var result = -1;
		var resultAngle = Angle.FullCircle;
		for (var i = 0; i < span.Length; ++i) {
			var newAngle = span[i] ^ targetDir;
			if (newAngle >= resultAngle) continue;

			resultAngle = newAngle;
			result = i;
		}
		return result;
	}
	static void GetNearestDirectionAndOrientation(Direction targetDir, ReadOnlySpan<Direction> span, out Orientation3D orientation, out Direction direction) {
		orientation = Orientation3D.None;
		direction = None;
		if (targetDir == None) {
			return;
		}

		var dirAngle = Angle.FullCircle;
		for (var i = 0; i < span.Length; ++i) {
			var testDir = span[i];
			if (targetDir.X != 0f && Single.Sign(testDir.X) == -Single.Sign(targetDir.X)) continue;
			if (targetDir.Y != 0f && Single.Sign(testDir.Y) == -Single.Sign(targetDir.Y)) continue;
			if (targetDir.Z != 0f && Single.Sign(testDir.Z) == -Single.Sign(targetDir.Z)) continue;

			var newAngle = testDir ^ targetDir;
			if (newAngle >= dirAngle) continue;

			dirAngle = newAngle;
			direction = testDir;
		}

		orientation = OrientationUtils.CreateOrientationFromValueSigns(direction.X, direction.Y, direction.Z);
	}
}