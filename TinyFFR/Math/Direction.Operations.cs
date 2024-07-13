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
	IProjectionTarget<Direction, Vect>,
	IOrthogonalizationTarget<Direction, Vect> {
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
	public static Direction operator -(Direction operand) => operand.Inverted;
	public Direction Inverted {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-AsVector4);
	}

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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Dot(Direction other) => Vector4.Dot(AsVector4, other.AsVector4);
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
		const float DotProductFloatingPointErrorMargin = 1E-4f;
		const float ResultLengthSquaredMin = 1E-5f;
		if (d == None) return null;
		var dot = Vector4.Dot(AsVector4, d.AsVector4);
		// These checks are important to protect against fp inaccuracy with cases where we're orthogonalizing against the self or reverse of self etc
		dot = MathF.Abs(dot) switch {
			> 1f - DotProductFloatingPointErrorMargin => 1f * MathF.Sign(dot),
			< DotProductFloatingPointErrorMargin => 0f,
			_ => dot
		};
		var nonNormalizedResult = AsVector4 - d.AsVector4 * dot;
		if (nonNormalizedResult.LengthSquared() < ResultLengthSquaredMin) return null;
		else return new(Normalize(nonNormalizedResult));
	}
	public Direction FastOrthogonalizedAgainst(Direction d) => new(Normalize(AsVector4 - d.AsVector4 * Vector4.Dot(AsVector4, d.AsVector4)));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizationOf(Vect v) => v.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizationOf(Vect v) => v.FastOrthogonalizedAgainst(this);

	Vect ProjectionOf(Vect v) => v.ProjectedOnTo(this);
	Vect? IProjectionTarget<Vect>.ProjectionOf(Vect v) => ProjectionOf(v);
	Vect IProjectionTarget<Vect>.FastProjectionOf(Vect v) => ProjectionOf(v);


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

	public Direction Clamp(Direction min, Direction max) {
		const float OppositeDirectionsFloatingPointMargin = 1E-7f;
		
		// Doesn't make sense to clamp to "None", so throw exception
		if (min == None) throw new ArgumentException($"Neither min nor max may be '{nameof(None)}'.", nameof(min));
		if (max == None) throw new ArgumentException($"Neither min nor max may be '{nameof(None)}'.", nameof(max));

		// If this is None just return None
		if (this == None) return None;

		// If min and max are antipodal then the entire range of possible directions is valid, so just return this
		if (min.Dot(max) < -1f + OppositeDirectionsFloatingPointMargin) return this;

		// Create a plane that is aligned with the great circle formed between min and max on the unit sphere, 
		// and then create a dimension converter to convert min, max, and this to 2D; with 'min' representing the X-axis
		// (and therefore min in 2D is equal to <1, 0>). Because min is set by definition to be <1, 0> (e.g. the X-axis
		// basis), its polar angle will be, by definition, 0 degrees.
		var minLoc = (Location) min;
		var maxLoc = (Location) max;
		var thisLoc = (Location) this;
		var greatCirclePlane = Plane.FromTriangleOnSurface(minLoc, maxLoc, Location.Origin);
		var converter = greatCirclePlane.CreateDimensionConverter(Location.Origin, min);

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

	// TODO clamp to within a cone (angle) of another
	// TODO create new random within a cone (angle) of another

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
		const float NoneDirectionEqualityTolerance = 1E-4f;
		if (targetDir.Equals(None, NoneDirectionEqualityTolerance)) {
			orientation = Orientation3D.None;
			direction = None;
			return;
		}

		direction = default;
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