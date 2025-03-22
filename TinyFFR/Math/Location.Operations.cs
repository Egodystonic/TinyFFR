// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Location : 
	ITransitionRepresentable<Location, Vect>,
	ISubtractionOperators<Location, Location, Vect>,
	IPointTransformable<Location>,
	IDistanceMeasurable<Location, Location> {

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect AsVect() => (Vect) this;

	#region Addition/Subtraction/Move
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator +(Location locationOperand, Vect vectOperand) => locationOperand.MovedBy(vectOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator +(Vect vectOperand, Location locationOperand) => locationOperand.MovedBy(vectOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator -(Location locationOperand, Vect vectOperand) => locationOperand.MovedBy(-vectOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location MovedBy(Vect vect) => new(AsVector4 + vect.AsVector4);
	#endregion

	#region Interactions w/ Location
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
	public float DistanceFrom(Location otherLocation) => VectFrom(otherLocation).Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Location otherLocation) => VectFrom(otherLocation).LengthSquared;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromOrigin() => ((Vect) this).Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromOrigin() => ((Vect) this).LengthSquared;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsWithinDistanceOf(Location other, float distance) => (this - other).LengthSquared <= distance * distance;
	#endregion

	#region Rotation
	static Location IMultiplyOperators<Location, Rotation, Location>.operator *(Location left, Rotation right) => left.RotatedAroundOriginBy(right);
	static Location IRotatable<Location>.operator *(Rotation left, Location right) => right.RotatedAroundOriginBy(left);
	Location IRotatable<Location>.RotatedBy(Rotation rot) => RotatedAroundOriginBy(rot);
	public Location RotatedAroundOriginBy(Rotation rotation) => (AsVect() * rotation).AsLocation();

	public static Location operator *(Location locationToRotate, (Location Pivot, Rotation Rotation) pivotRotationTuple) => locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public static Location operator *((Location Pivot, Rotation Rotation) pivotRotationTuple, Location locationToRotate) => locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public static Location operator *(Location locationToRotate, (Rotation Rotation, Location Pivot) pivotRotationTuple) => locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public static Location operator *((Rotation Rotation, Location Pivot) pivotRotationTuple, Location locationToRotate) => locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	public Location RotatedBy(Rotation rotation, Location pivot) => pivot + VectFrom(pivot) * rotation;
	#endregion

	#region Transformation and Scaling
	static Location IMultiplyOperators<Location, float, Location>.operator *(Location left, float right) => left.ScaledFromOriginBy(right);
	static Location IDivisionOperators<Location, float, Location>.operator /(Location left, float right) => left.ScaledFromOriginBy(1f / right);
	static Location IMultiplicative<Location, float, Location>.operator *(float left, Location right) => right.ScaledFromOriginBy(left);
	Location IScalable<Location>.ScaledBy(float scalar) => ScaledFromOriginBy(scalar);
	Location IIndependentAxisScalable<Location>.ScaledBy(Vect vect) => ScaledFromOriginBy(vect);
	Location IPointIndependentAxisScalable<Location>.ScaledBy(Vect vect, Location scalingOrigin) => TransformedBy(new(scaling: vect), scalingOrigin);
	public Location ScaledFromOriginBy(float scalar) => FromVector3(ToVector3() * scalar);
	public Location ScaledFromOriginBy(Vect vect) => FromVector3(ToVector3() * vect.ToVector3());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator *(Location location, Transform transform) => location.TransformedAroundOriginBy(transform);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator *(Transform transform, Location location) => location.TransformedAroundOriginBy(transform);
	Location ITransformable<Location>.TransformedBy(Transform transform) => TransformedAroundOriginBy(transform);
	public Location TransformedAroundOriginBy(Transform transform) => AsVect().TransformedBy(transform).AsLocation();
	public Location TransformedBy(Transform transform, Location transformationOrigin) => transformationOrigin + (transformationOrigin >> this).TransformedBy(transform);
	#endregion

	#region Clamping and Interpolation
	public Location Clamp(Location min, Location max) => ClosestPointOn(new BoundedRay(min, max));

	public static Location Interpolate(Location start, Location end, float distance) {
		return start + (end - start) * distance;
	}
	#endregion
}