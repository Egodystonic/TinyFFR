// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Location : 
	IAdditionOperators<Location, Vect, Location>,
	ISubtractionOperators<Location, Vect, Location>,
	ISubtractionOperators<Location, Location, Vect>,
	IMultiplyOperators<Location, (Location Pivot, Rotation Rotation), Location>,
	IMultiplyOperators<Location, (Rotation Rotation, Location Pivot), Location>,
	IInterpolatable<Location>,
	IBoundedRandomizable<Location> {
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
	public static Vect operator >>(Location start, Location end) => start.GetVectTo(end); // TODO maybe these should give Rays ... Use >>> for Vect? .. No, other way IMO
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator <<(Location end, Location start) => start.GetVectTo(end);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator -(Location lhs, Location rhs) => lhs.GetVectFrom(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect GetVectFrom(Location otherLocation) => new(AsVector4 - otherLocation.AsVector4);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect GetVectTo(Location otherLocation) => new(otherLocation.AsVector4 - AsVector4);

	public static Location operator *(Location locationToRotate, (Location Pivot, Rotation Rotation) pivotRotationTuple) => locationToRotate.RotatedAround(pivotRotationTuple.Pivot, pivotRotationTuple.Rotation);
	public static Location operator *((Location Pivot, Rotation Rotation) pivotRotationTuple, Location locationToRotate) => locationToRotate.RotatedAround(pivotRotationTuple.Pivot, pivotRotationTuple.Rotation);
	public static Location operator *(Location locationToRotate, (Rotation Rotation, Location Pivot) pivotRotationTuple) => locationToRotate.RotatedAround(pivotRotationTuple.Pivot, pivotRotationTuple.Rotation);
	public static Location operator *((Rotation Rotation, Location Pivot) pivotRotationTuple, Location locationToRotate) => locationToRotate.RotatedAround(pivotRotationTuple.Pivot, pivotRotationTuple.Rotation);
	public Location RotatedAround(Location pivotPoint, Rotation rot) => pivotPoint + GetVectFrom(pivotPoint) * rot;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator >>>(Location startPoint, Direction direction) => startPoint.CreateRay(direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray CreateRay(Direction direction) => new(this, direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator >>>(Location startPoint, Vect vect) => startPoint.CreateLine(vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine CreateLine(Vect vect) => new(this, vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator >>>(Location startPoint, Location endPoint) => startPoint.CreateLine(endPoint);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine CreateLine(Location endPoint) => new(this, endPoint);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction GetDirectionFrom(Location otherLocation) => GetVectFrom(otherLocation).Direction;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction GetDirectionTo(Location otherLocation) => GetVectTo(otherLocation).Direction;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location otherLocation) => GetVectFrom(otherLocation).Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Location otherLocation) => GetVectFrom(otherLocation).LengthSquared;

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