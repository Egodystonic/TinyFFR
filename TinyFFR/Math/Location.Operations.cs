// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Location : 
	ITranslatable<Location>,
	ITransitionRepresentable<Location, Vect>,
	ISubtractionOperators<Location, Location, Vect>,
	IPointRotatable<Location>,
	IDistanceMeasurable<Location, Location> {
	internal const float DefaultRandomRange = 100f;

	public float this[Axis axis] => axis switch {
		Axis.X => X,
		Axis.Y => Y,
		Axis.Z => Z,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} must not be anything except {nameof(Axis.X)}, {nameof(Axis.Y)} or {nameof(Axis.Z)}.")
	};
	public XYPair<float> this[Axis first, Axis second] => new(this[first], this[second]);
	public Location this[Axis first, Axis second, Axis third] => new(this[first], this[second], this[third]);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator +(Location locationOperand, Vect vectOperand) => locationOperand.MovedBy(vectOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator +(Vect vectOperand, Location locationOperand) => locationOperand.MovedBy(vectOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator -(Location locationOperand, Vect vectOperand) => locationOperand.MovedBy(-vectOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location MovedBy(Vect vect) => new(AsVector4 + vect.AsVector4);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator >>(Location start, Location end) => start.VectTo(end);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator <<(Location end, Location start) => start.VectTo(end);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator -(Location lhs, Location rhs) => lhs.VectFrom(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect VectFrom(Location otherLocation) => new(AsVector4 - otherLocation.AsVector4);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect VectTo(Location otherLocation) => new(otherLocation.AsVector4 - AsVector4);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction DirectionFrom(Location otherLocation) => VectFrom(otherLocation).Direction;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction DirectionTo(Location otherLocation) => VectTo(otherLocation).Direction;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect VectFromOrigin() => (Vect) this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect VectToOrigin() => -((Vect) this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect AsVect() => (Vect) this;

	public static Location operator *(Location locationToRotate, (Location Pivot, Rotation Rotation) pivotRotationTuple) => locationToRotate.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public static Location operator *((Location Pivot, Rotation Rotation) pivotRotationTuple, Location locationToRotate) => locationToRotate.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public static Location operator *(Location locationToRotate, (Rotation Rotation, Location Pivot) pivotRotationTuple) => locationToRotate.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public static Location operator *((Rotation Rotation, Location Pivot) pivotRotationTuple, Location locationToRotate) => locationToRotate.RotatedAroundPoint(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public Location RotatedAroundPoint(Rotation rotation, Location pivot) => pivot + VectFrom(pivot) * rotation;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location otherLocation) => VectFrom(otherLocation).Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Location otherLocation) => VectFrom(otherLocation).LengthSquared;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromOrigin() => ((Vect) this).Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromOrigin() => ((Vect) this).LengthSquared;

	public Location Clamp(Location min, Location max) => ClosestPointOn(new BoundedRay(min, max));

	public static Location Interpolate(Location start, Location end, float distance) {
		return start + (end - start) * distance;
	}

	public static Location CreateNewRandom() {
		return FromVector3(new Vector3(
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive()
		) * DefaultRandomRange);
	}
	public static Location CreateNewRandom(Location minInclusive, Location maxExclusive) {
		return minInclusive + ((minInclusive >> maxExclusive) * RandomUtils.NextSingle());
	}
}