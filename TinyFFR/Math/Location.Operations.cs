// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Location : 
	IPhysicalValidityDeterminable,
	ITransitionRepresentable<Location, Vect>,
	ISubtractionOperators<Location, Location, Vect>,
	IPointTransformable<Location>,
	IDistanceMeasurable<Location, Location> {

	/// <summary>
	/// Converts this location to a <see cref="Vect"/> by treating its coordinates as vector components.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect AsVect() => (Vect) this;

	/// <summary>
	/// Determines whether this location has finite <see cref="X"/>, <see cref="Y"/>, and <see cref="Z"/> values.
	/// </summary>
	public bool IsPhysicallyValid => Single.IsFinite(X) && Single.IsFinite(Y) && Single.IsFinite(Z);

	#region Addition/Subtraction/Move
	/// <summary>
	/// Moves <paramref name="locationOperand"/> by <paramref name="vectOperand"/>; equivalent to <c>locationOperand.MovedBy(vectOperand)</c>.
	/// </summary>
	/// <param name="locationOperand">The location to move.</param>
	/// <param name="vectOperand">The vector to move by.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator +(Location locationOperand, Vect vectOperand) => locationOperand.MovedBy(vectOperand);
	/// <summary>
	/// Moves <paramref name="locationOperand"/> by <paramref name="vectOperand"/>; equivalent to <c>locationOperand.MovedBy(vectOperand)</c>.
	/// </summary>
	/// <param name="vectOperand">The vector to move by.</param>
	/// <param name="locationOperand">The location to move.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator +(Vect vectOperand, Location locationOperand) => locationOperand.MovedBy(vectOperand);
	/// <summary>
	/// Moves <paramref name="locationOperand"/> in the direction opposite to <paramref name="vectOperand"/>; equivalent to <c>locationOperand.MovedBy(-vectOperand)</c>.
	/// </summary>
	/// <param name="locationOperand">The location to move.</param>
	/// <param name="vectOperand">The vector to move by, in reverse.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator -(Location locationOperand, Vect vectOperand) => locationOperand.MovedBy(-vectOperand);
	/// <summary>
	/// Returns this location moved by <paramref name="vect"/>.
	/// </summary>
	/// <param name="vect">The vector to move by.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location MovedBy(Vect vect) => new(AsVector4 + vect.AsVector4);
	#endregion

	#region Interactions w/ Location
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator >>(Location start, Location end) => start.VectTo(end);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator <<(Location end, Location start) => start.VectTo(end);
	/// <summary>
	/// Returns the vector from <paramref name="rhs"/> to <paramref name="lhs"/>; equivalent to <c>lhs.VectFrom(rhs)</c>.
	/// </summary>
	/// <param name="lhs">The left-hand operand.</param>
	/// <param name="rhs">The right-hand operand.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator -(Location lhs, Location rhs) => lhs.VectFrom(rhs);
	/// <summary>
	/// Returns the vector needed to travel from <paramref name="otherLocation"/> to this location.
	/// </summary>
	/// <param name="otherLocation">The location to measure from.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect VectFrom(Location otherLocation) => new(AsVector4 - otherLocation.AsVector4);
	/// <summary>
	/// Returns the vector needed to travel from this location to <paramref name="otherLocation"/>.
	/// </summary>
	/// <param name="otherLocation">The location to measure to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect VectTo(Location otherLocation) => new(otherLocation.AsVector4 - AsVector4);
	/// <summary>
	/// Returns the direction from <paramref name="otherLocation"/> towards this location.
	/// </summary>
	/// <param name="otherLocation">The location to measure from. If this is equal to the current location, the result is <see cref="Direction.None"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction DirectionFrom(Location otherLocation) => VectFrom(otherLocation).Direction;
	/// <summary>
	/// Returns the direction from this location towards <paramref name="otherLocation"/>.
	/// </summary>
	/// <param name="otherLocation">The location to measure to. If this is equal to the current location, the result is <see cref="Direction.None"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction DirectionTo(Location otherLocation) => VectTo(otherLocation).Direction;

	/// <summary>
	/// Returns the distance between this location and <paramref name="otherLocation"/>.
	/// </summary>
	/// <param name="otherLocation">The location to measure the distance to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location otherLocation) => VectFrom(otherLocation).Length;
	/// <summary>
	/// Returns the square of the distance between this location and <paramref name="otherLocation"/>.
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="DistanceFrom"/> as it avoids a square root, and is sufficient when you only need
	/// to compare distances rather than know the exact value.
	/// </remarks>
	/// <param name="otherLocation">The location to measure the distance to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Location otherLocation) => VectFrom(otherLocation).LengthSquared;
	/// <summary>
	/// Returns the distance between this location and <see cref="Origin"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromOrigin() => ((Vect) this).Length;
	/// <summary>
	/// Returns the square of the distance between this location and <see cref="Origin"/>.
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="DistanceFromOrigin"/> as it avoids a square root, and is sufficient when you only
	/// need to compare distances rather than know the exact value.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromOrigin() => ((Vect) this).LengthSquared;

	/// <summary>
	/// Determines whether this location is within <paramref name="distance"/> of <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The other location to measure against.</param>
	/// <param name="distance">The maximum permitted distance between the two locations, inclusive.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsWithinDistanceOf(Location other, float distance) => (this - other).LengthSquared <= distance * distance;
	#endregion

	#region Rotation
	static Location IMultiplyOperators<Location, Rotation, Location>.operator *(Location left, Rotation right) => left.RotatedAroundOriginBy(right);
	static Location IRotatable<Location>.operator *(Rotation left, Location right) => right.RotatedAroundOriginBy(left);
	Location IRotatable<Location>.RotatedBy(Rotation rot) => RotatedAroundOriginBy(rot);
	/// <summary>
	/// Returns this location after being rotated by <paramref name="rotation"/> around <see cref="Origin"/>.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	public Location RotatedAroundOriginBy(Rotation rotation) => (AsVect() * rotation).AsLocation();
	Location IRotatable<Location>.RotatedBy(Quaternion rotQuat) => RotatedAroundOriginBy(rotQuat);
	/// <summary>
	/// Returns this location after being rotated by <paramref name="rotationQuaternion"/> around <see cref="Origin"/>.
	/// </summary>
	/// <param name="rotationQuaternion">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	public Location RotatedAroundOriginBy(Quaternion rotationQuaternion) => AsVect().RotatedBy(rotationQuaternion).AsLocation();

	/// <summary>
	/// Returns <paramref name="locationToRotate"/> after being rotated around <paramref name="pivotRotationTuple"/>'s pivot by its rotation; equivalent to <c>locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot)</c>.
	/// </summary>
	/// <param name="locationToRotate">The location to rotate.</param>
	/// <param name="pivotRotationTuple">The pivot point to rotate around, and the rotation to apply.</param>
	public static Location operator *(Location locationToRotate, (Location Pivot, Rotation Rotation) pivotRotationTuple) => locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <summary>
	/// Returns <paramref name="locationToRotate"/> after being rotated around <paramref name="pivotRotationTuple"/>'s pivot by its rotation; equivalent to <c>locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot)</c>.
	/// </summary>
	/// <param name="pivotRotationTuple">The pivot point to rotate around, and the rotation to apply.</param>
	/// <param name="locationToRotate">The location to rotate.</param>
	public static Location operator *((Location Pivot, Rotation Rotation) pivotRotationTuple, Location locationToRotate) => locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <summary>
	/// Returns <paramref name="locationToRotate"/> after being rotated around <paramref name="pivotRotationTuple"/>'s pivot by its rotation; equivalent to <c>locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot)</c>.
	/// </summary>
	/// <param name="locationToRotate">The location to rotate.</param>
	/// <param name="pivotRotationTuple">The rotation to apply, and the pivot point to rotate around.</param>
	public static Location operator *(Location locationToRotate, (Rotation Rotation, Location Pivot) pivotRotationTuple) => locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <summary>
	/// Returns <paramref name="locationToRotate"/> after being rotated around <paramref name="pivotRotationTuple"/>'s pivot by its rotation; equivalent to <c>locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot)</c>.
	/// </summary>
	/// <param name="pivotRotationTuple">The rotation to apply, and the pivot point to rotate around.</param>
	/// <param name="locationToRotate">The location to rotate.</param>
	public static Location operator *((Rotation Rotation, Location Pivot) pivotRotationTuple, Location locationToRotate) => locationToRotate.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <summary>
	/// Returns this location after being rotated by <paramref name="rotation"/> around <paramref name="pivot"/>.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	/// <param name="pivot">The point to rotate around.</param>
	public Location RotatedBy(Rotation rotation, Location pivot) => pivot + VectFrom(pivot) * rotation;
	/// <summary>
	/// Returns this location after being rotated by <paramref name="rotationQuaternion"/> around <paramref name="pivot"/>.
	/// </summary>
	/// <param name="rotationQuaternion">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	/// <param name="pivot">The point to rotate around.</param>
	public Location RotatedBy(Quaternion rotationQuaternion, Location pivot) => pivot + VectFrom(pivot).RotatedBy(rotationQuaternion);
	#endregion

	#region Transformation and Scaling
	static Location IMultiplyOperators<Location, float, Location>.operator *(Location left, float right) => left.ScaledFromOriginBy(right);
	static Location IDivisionOperators<Location, float, Location>.operator /(Location left, float right) => left.ScaledFromOriginBy(1f / right);
	static Location IMultiplicative<Location, float, Location>.operator *(float left, Location right) => right.ScaledFromOriginBy(left);
	Location IScalable<Location>.ScaledBy(float scalar) => ScaledFromOriginBy(scalar);
	Location IIndependentAxisScalable<Location>.ScaledBy(Vect vect) => ScaledFromOriginBy(vect);
	Location IPointIndependentAxisScalable<Location>.ScaledBy(Vect vect, Location scalingOrigin) => TransformedBy(new Transform(scaling: vect), scalingOrigin);
	/// <summary>
	/// Returns this location after being scaled by <paramref name="scalar"/> around <see cref="Origin"/>.
	/// </summary>
	/// <param name="scalar">The scale factor to apply uniformly to all three axes.</param>
	public Location ScaledFromOriginBy(float scalar) => FromVector3(ToVector3() * scalar);
	/// <summary>
	/// Returns this location after being scaled independently per axis by <paramref name="vect"/>'s components, around <see cref="Origin"/>.
	/// </summary>
	/// <param name="vect">The per-axis scale factors to apply.</param>
	public Location ScaledFromOriginBy(Vect vect) => FromVector3(ToVector3() * vect.ToVector3());

	/// <summary>
	/// Returns <paramref name="location"/> after being transformed by <paramref name="transform"/> around <see cref="Origin"/>; equivalent to <c>location.TransformedAroundOriginBy(transform)</c>.
	/// </summary>
	/// <param name="location">The location to transform.</param>
	/// <param name="transform">The transform to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator *(Location location, Transform transform) => location.TransformedAroundOriginBy(transform);
	/// <summary>
	/// Returns <paramref name="location"/> after being transformed by <paramref name="transform"/> around <see cref="Origin"/>; equivalent to <c>location.TransformedAroundOriginBy(transform)</c>.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	/// <param name="location">The location to transform.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator *(Transform transform, Location location) => location.TransformedAroundOriginBy(transform);
	Location ITransformable<Location>.TransformedBy(Transform transform) => TransformedAroundOriginBy(transform);
	/// <summary>
	/// Returns this location after being transformed by <paramref name="transform"/>, treating <see cref="Origin"/> as the transform's origin.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	public Location TransformedAroundOriginBy(Transform transform) => new(Transform(AsVector4, transform.ToMatrix()));
	/// <summary>
	/// Returns this location after being transformed by <paramref name="transform"/>, treating <paramref name="transformationOrigin"/> as the transform's origin.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	/// <param name="transformationOrigin">The point to treat as the origin of <paramref name="transform"/>.</param>
	public Location TransformedBy(Transform transform, Location transformationOrigin) => MovedBy(-transformationOrigin.AsVect()).TransformedAroundOriginBy(transform).MovedBy(transformationOrigin.AsVect());
	Location ITransformable<Location>.TransformedByInverseOf(Transform transform) => TransformedAroundOriginByInverseOf(transform);
	/// <summary>
	/// Returns this location after being transformed by the inverse of <paramref name="transform"/>, treating <see cref="Origin"/> as the transform's origin.
	/// </summary>
	/// <remarks>
	/// This undoes the effect of <see cref="TransformedAroundOriginBy"/>. For (almost) any <paramref name="transform"/>,
	/// <c>location.TransformedAroundOriginBy(transform).TransformedAroundOriginByInverseOf(transform)</c> returns a
	/// location equal to the original (not accounting for floating-point error accrual).
	/// </remarks>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	public Location TransformedAroundOriginByInverseOf(Transform transform) => new(Transform(AsVector4, ForceInvertMatrix(transform.ToMatrix())));
	/// <summary>
	/// Returns this location after being transformed by the inverse of <paramref name="transform"/>, treating <paramref name="transformationOrigin"/> as the transform's origin.
	/// </summary>
	/// <remarks>
	/// This undoes the effect of <see cref="TransformedBy(Transform,Location)"/>. For (almost) any <paramref name="transform"/>,
	/// <c>location.TransformedBy(transform, origin).TransformedByInverseOf(transform, origin)</c> returns a
	/// location equal to the original (not accounting for floating-point error accrual).
	/// </remarks>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="transformationOrigin">The point to treat as the origin of <paramref name="transform"/>.</param>
	public Location TransformedByInverseOf(Transform transform, Location transformationOrigin) => MovedBy(-transformationOrigin.AsVect()).TransformedAroundOriginByInverseOf(transform).MovedBy(transformationOrigin.AsVect());
	#endregion

	#region Clamping and Interpolation
	/// <summary>
	/// Clamps this location on to the line segment between <paramref name="min"/> and <paramref name="max"/>.
	/// </summary>
	/// <remarks>
	/// This is not a volumetric clamp; the result is always on the straight line segment connecting
	/// <paramref name="min"/> and <paramref name="max"/>.
	/// For a volumetric clamp (i.e. treating <paramref name="min"/> and <paramref name="max"/> as opposite corners of a cuboid),
	/// use <see cref="PositionedCuboid.FromOppositeCorners">PositionedCuboid.FromOppositeCorners(min, max)</see>.<see cref="PositionedCuboid.PointClosestTo(Location)">ClosestPointTo(this)</see> instead.
	/// </remarks>
	/// <param name="min">One end of the line segment to clamp within. Can be swapped with max to no effect.</param>
	/// <param name="max">The other end of the line segment to clamp within. Can be swapped with min to no effect.</param>
	public Location Clamp(Location min, Location max) => ClosestPointOn(new BoundedRay(min, max));

	/// <inheritdoc />
	public static Location Interpolate(Location start, Location end, float distance) {
		return start + (end - start) * distance;
	}
	#endregion
}