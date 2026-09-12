// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct BoundedRay : IPointTransformable<BoundedRay>, IPointScalable<BoundedRay>, ILengthAdjustable<BoundedRay> {
	/// <summary>
	/// Converts this ray to a <see cref="Ray"/> starting at <see cref="StartPoint"/>, pointing in <see cref="Direction"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromStart() => new(StartPoint, Direction);
	/// <summary>
	/// Converts this ray to a <see cref="Ray"/> starting at <see cref="EndPoint"/>, pointing back towards <see cref="StartPoint"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromEnd() => new(EndPoint, -Direction);
	/// <summary>
	/// Converts this ray to a <see cref="Ray"/> starting at the point found by travelling <paramref name="signedDistanceAlongLine"/> along this ray from <see cref="StartPoint"/>, pointing in <see cref="Direction"/>.
	/// </summary>
	/// <param name="signedDistanceAlongLine">The distance along this ray, from <see cref="StartPoint"/>, of the resultant ray's start point. Can be outside the bounds of this ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRay(float signedDistanceAlongLine) => new(UnboundedLocationAtDistance(signedDistanceAlongLine), Direction);

	/// <summary>
	/// Converts this ray to a <see cref="Line"/> passing through <see cref="StartPoint"/> in <see cref="Direction"/>, discarding its length.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ToLine() => new(StartPoint, Direction);

	/// <summary>
	/// Negates <paramref name="operand"/>; equivalent to reading <see cref="Flipped"/>.
	/// </summary>
	/// <param name="operand">The ray to negate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator -(BoundedRay operand) => operand.Flipped;
	/// <summary>
	/// Returns this ray with its <see cref="StartPoint"/> and <see cref="EndPoint"/> swapped, so it points in the opposite direction.
	/// </summary>
	public BoundedRay Flipped {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(EndPoint, StartPoint);
	}
	BoundedRay IInvertible<BoundedRay>.Inverted => Flipped;

	#region With Methods
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> set to <paramref name="newLength"/>, keeping <see cref="StartPoint"/> and <see cref="Direction"/> fixed (i.e. only <see cref="EndPoint"/> moves).
	/// </summary>
	/// <param name="newLength">The new length. A negative value flips the ray's direction.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLength(float newLength) => new(_startPoint, _vect.WithLength(newLength));
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> reduced by <paramref name="lengthDecrease"/>, keeping <see cref="StartPoint"/> fixed.
	/// </summary>
	/// <param name="lengthDecrease">The amount to subtract from <see cref="Length"/>. Can be negative to increase the length instead.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthDecreasedBy(float lengthDecrease) => WithLength(Length - lengthDecrease);
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> increased by <paramref name="lengthIncrease"/>, keeping <see cref="StartPoint"/> fixed.
	/// </summary>
	/// <param name="lengthIncrease">The amount to add to <see cref="Length"/>. Can be negative to decrease the length instead.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthIncreasedBy(float lengthIncrease) => WithLength(Length + lengthIncrease);
	/// <summary>
	/// Returns this ray, shortened to <paramref name="maxLength"/> (keeping <see cref="StartPoint"/> fixed) if it is currently longer than that; otherwise returns this ray unchanged.
	/// </summary>
	/// <param name="maxLength">The maximum permitted length. Must be non-negative.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> was negative.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMaxLength(float maxLength) => WithLength(MathF.Min(Length, maxLength >= 0f ? maxLength : throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be non-negative.")));
	/// <summary>
	/// Returns this ray, lengthened to <paramref name="minLength"/> (keeping <see cref="StartPoint"/> fixed) if it is currently shorter than that; otherwise returns this ray unchanged.
	/// </summary>
	/// <param name="minLength">The minimum permitted length. Must be non-negative.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> was negative.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMinLength(float minLength) => WithLength(MathF.Max(Length, minLength >= 0f ? minLength : throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Must be non-negative.")));
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> set to <paramref name="newLength"/>, scaling around the point found by travelling <paramref name="scalingOriginSignedDistance"/> along this ray from <see cref="StartPoint"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="WithLength(float)"/>, this can move <see cref="StartPoint"/> as well as <see cref="EndPoint"/> — both ends move away from (or towards) the pivot as the length changes.
	/// </remarks>
	/// <param name="newLength">The new length.</param>
	/// <param name="scalingOriginSignedDistance">The distance along this ray, from <see cref="StartPoint"/>, of the pivot point around which the ray is scaled. Can be outside the bounds of this ray.</param>
	public BoundedRay WithLength(float newLength, float scalingOriginSignedDistance) {
		var scalar = newLength / Length;
		return Single.IsFinite(scalar) ? ScaledBy(scalar, scalingOriginSignedDistance) : this;
	}
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> reduced by <paramref name="lengthDecrease"/>, scaling around the given pivot (see <see cref="WithLength(float,float)"/>).
	/// </summary>
	/// <param name="lengthDecrease">The amount to subtract from <see cref="Length"/>. Can be negative to increase the length instead.</param>
	/// <param name="scalingOriginSignedDistance">The distance along this ray, from <see cref="StartPoint"/>, of the pivot point around which the ray is scaled. Can be outside the bounds of this ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthDecreasedBy(float lengthDecrease, float scalingOriginSignedDistance) => WithLength(Length - lengthDecrease, scalingOriginSignedDistance);
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> increased by <paramref name="lengthIncrease"/>, scaling around the given pivot (see <see cref="WithLength(float,float)"/>).
	/// </summary>
	/// <param name="lengthIncrease">The amount to add to <see cref="Length"/>. Can be negative to decrease the length instead.</param>
	/// <param name="scalingOriginSignedDistance">The distance along this ray, from <see cref="StartPoint"/>, of the pivot point around which the ray is scaled. Can be outside the bounds of this ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthIncreasedBy(float lengthIncrease, float scalingOriginSignedDistance) => WithLength(Length + lengthIncrease, scalingOriginSignedDistance);
	/// <summary>
	/// Returns this ray, shortened to <paramref name="maxLength"/> (scaling around the given pivot, see <see cref="WithLength(float,float)"/>) if it is currently longer than that; otherwise returns this ray unchanged.
	/// </summary>
	/// <param name="maxLength">The maximum permitted length. Must be non-negative.</param>
	/// <param name="scalingOriginSignedDistance">The distance along this ray, from <see cref="StartPoint"/>, of the pivot point around which the ray is scaled. Can be outside the bounds of this ray.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> was negative.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMaxLength(float maxLength, float scalingOriginSignedDistance) => WithLength(MathF.Min(Length, maxLength >= 0f ? maxLength : throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be non-negative.")), scalingOriginSignedDistance);
	/// <summary>
	/// Returns this ray, lengthened to <paramref name="minLength"/> (scaling around the given pivot, see <see cref="WithLength(float,float)"/>) if it is currently shorter than that; otherwise returns this ray unchanged.
	/// </summary>
	/// <param name="minLength">The minimum permitted length. Must be non-negative.</param>
	/// <param name="scalingOriginSignedDistance">The distance along this ray, from <see cref="StartPoint"/>, of the pivot point around which the ray is scaled. Can be outside the bounds of this ray.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> was negative.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMinLength(float minLength, float scalingOriginSignedDistance) => WithLength(MathF.Max(Length, minLength >= 0f ? minLength : throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Must be non-negative.")), scalingOriginSignedDistance);
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> set to <paramref name="newLength"/>, scaling around <paramref name="scalingOrigin"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="WithLength(float)"/>, this can move <see cref="StartPoint"/> as well as <see cref="EndPoint"/> — both ends move away from (or towards) <paramref name="scalingOrigin"/> as the length changes.
	/// </remarks>
	/// <param name="newLength">The new length.</param>
	/// <param name="scalingOrigin">The point around which the ray is scaled. Does not need to lie on this ray.</param>
	public BoundedRay WithLength(float newLength, Location scalingOrigin) {
		var scalar = newLength / Length;
		return Single.IsFinite(scalar) ? ScaledBy(scalar, scalingOrigin) : this;
	}

	/// <summary>
	/// Returns this ray with its <see cref="Length"/> reduced by <paramref name="lengthDecrease"/>, scaling around <paramref name="scalingOrigin"/> (see <see cref="WithLength(float,Location)"/>).
	/// </summary>
	/// <param name="lengthDecrease">The amount to subtract from <see cref="Length"/>. Can be negative to increase the length instead.</param>
	/// <param name="scalingOrigin">The point around which the ray is scaled. Does not need to lie on this ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthDecreasedBy(float lengthDecrease, Location scalingOrigin) => WithLength(Length - lengthDecrease, scalingOrigin);
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> increased by <paramref name="lengthIncrease"/>, scaling around <paramref name="scalingOrigin"/> (see <see cref="WithLength(float,Location)"/>).
	/// </summary>
	/// <param name="lengthIncrease">The amount to add to <see cref="Length"/>. Can be negative to decrease the length instead.</param>
	/// <param name="scalingOrigin">The point around which the ray is scaled. Does not need to lie on this ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthIncreasedBy(float lengthIncrease, Location scalingOrigin) => WithLength(Length + lengthIncrease, scalingOrigin);
	/// <summary>
	/// Returns this ray, shortened to <paramref name="maxLength"/> (scaling around <paramref name="scalingOrigin"/>, see <see cref="WithLength(float,Location)"/>) if it is currently longer than that; otherwise returns this ray unchanged.
	/// </summary>
	/// <param name="maxLength">The maximum permitted length. Must be non-negative.</param>
	/// <param name="scalingOrigin">The point around which the ray is scaled. Does not need to lie on this ray.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> was negative.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMaxLength(float maxLength, Location scalingOrigin) => WithLength(MathF.Min(Length, maxLength >= 0f ? maxLength : throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be non-negative.")), scalingOrigin);
	/// <summary>
	/// Returns this ray, lengthened to <paramref name="minLength"/> (scaling around <paramref name="scalingOrigin"/>, see <see cref="WithLength(float,Location)"/>) if it is currently shorter than that; otherwise returns this ray unchanged.
	/// </summary>
	/// <param name="minLength">The minimum permitted length. Must be non-negative.</param>
	/// <param name="scalingOrigin">The point around which the ray is scaled. Does not need to lie on this ray.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> was negative.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMinLength(float minLength, Location scalingOrigin) => WithLength(MathF.Max(Length, minLength >= 0f ? minLength : throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Must be non-negative.")), scalingOrigin);
	#endregion

	#region Line-Like Methods
	/// <inheritdoc />
	public bool DistanceIsWithinLineBounds(float signedDistanceFromStart) => signedDistanceFromStart >= 0f && signedDistanceFromStart * signedDistanceFromStart <= LengthSquared;
	/// <inheritdoc />
	public float BindDistance(float signedDistanceFromStart) => Single.Clamp(signedDistanceFromStart, 0f, Length);
	/// <inheritdoc />
	public Location BoundedLocationAtDistance(float signedDistanceFromStart) => UnboundedLocationAtDistance(BindDistance(signedDistanceFromStart));
	/// <inheritdoc />
	public Location UnboundedLocationAtDistance(float signedDistanceFromStart) => _startPoint + _vect.WithLength(signedDistanceFromStart);
	/// <inheritdoc />
	public Location? LocationAtDistanceOrNull(float signedDistanceFromStart) => DistanceIsWithinLineBounds(signedDistanceFromStart) ? UnboundedLocationAtDistance(signedDistanceFromStart) : null;
	/// <inheritdoc />
	public float UnboundedDistanceAtPointClosestTo(Location point) => ToLine().DistanceAtPointClosestTo(point);
	/// <inheritdoc />
	public float BoundedDistanceAtPointClosestTo(Location point) => PointClosestTo(point).DistanceFrom(StartPoint);
	#endregion

	#region Translation
	/// <summary>
	/// Moves <paramref name="ray"/> by <paramref name="v"/>; equivalent to <see cref="MovedBy(Vect)"/>.
	/// </summary>
	/// <param name="ray">The ray to move.</param>
	/// <param name="v">The vector to move it by.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator +(BoundedRay ray, Vect v) => ray.MovedBy(v);
	/// <inheritdoc cref="operator +(BoundedRay,Vect)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator +(Vect v, BoundedRay ray) => ray.MovedBy(v);
	/// <summary>
	/// Moves <paramref name="ray"/> by the negation of <paramref name="v"/>; equivalent to <see cref="MovedBy(Vect)"/> with <paramref name="v"/> negated.
	/// </summary>
	/// <param name="ray">The ray to move.</param>
	/// <param name="v">The vector to move it by, negated.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator -(BoundedRay ray, Vect v) => ray.MovedBy(-v);
	/// <summary>
	/// Returns this ray moved by <paramref name="v"/>, i.e. with <paramref name="v"/> added to both <see cref="StartPoint"/> and <see cref="EndPoint"/>.
	/// </summary>
	/// <param name="v">The vector to move this ray by.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay MovedBy(Vect v) => new(_startPoint + v, _vect);
	#endregion

	#region Rotation
	/// <summary>
	/// Rotates <paramref name="ray"/> by <paramref name="rot"/> around its own <see cref="StartPoint"/>; equivalent to <see cref="RotatedAroundStartBy(Rotation)"/>.
	/// </summary>
	/// <param name="ray">The ray to rotate.</param>
	/// <param name="rot">The rotation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(BoundedRay ray, Rotation rot) => ray.RotatedAroundStartBy(rot);
	/// <inheritdoc cref="operator *(BoundedRay,Rotation)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(Rotation rot, BoundedRay ray) => ray.RotatedAroundStartBy(rot);
	/// <summary>
	/// Rotates <paramref name="ray"/> by <paramref name="rotPivotTuple"/>'s rotation around <paramref name="rotPivotTuple"/>'s pivot; equivalent to <see cref="RotatedBy(Rotation,Location)"/>.
	/// </summary>
	/// <param name="ray">The ray to rotate.</param>
	/// <param name="rotPivotTuple">The rotation to apply, and the point to rotate around.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(BoundedRay ray, (Rotation Rotation, Location Pivot) rotPivotTuple) => ray.RotatedBy(rotPivotTuple.Rotation, rotPivotTuple.Pivot);
	/// <inheritdoc cref="operator *(BoundedRay,ValueTuple{Rotation,Location})" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(BoundedRay ray, (Location Pivot, Rotation Rotation) rotPivotTuple) => ray.RotatedBy(rotPivotTuple.Rotation, rotPivotTuple.Pivot);
	/// <inheritdoc cref="operator *(BoundedRay,ValueTuple{Rotation,Location})" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *((Rotation Rotation, Location Pivot) rotPivotTuple, BoundedRay ray) => ray.RotatedBy(rotPivotTuple.Rotation, rotPivotTuple.Pivot);
	/// <inheritdoc cref="operator *(BoundedRay,ValueTuple{Rotation,Location})" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *((Location Pivot, Rotation Rotation) rotPivotTuple, BoundedRay ray) => ray.RotatedBy(rotPivotTuple.Rotation, rotPivotTuple.Pivot);

	/// <summary>
	/// Returns this ray rotated by <paramref name="rotation"/> around its own <see cref="StartPoint"/>, which therefore stays fixed.
	/// </summary>
	/// <remarks>
	/// This is the pivot used by the various operator overloads and by the explicit <see cref="IRotatable{TSelf}"/> implementation, matching the equivalent default on <see cref="Ray"/>.
	/// </remarks>
	/// <param name="rotation">The rotation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay RotatedAroundStartBy(Rotation rotation) => new(_startPoint, _vect * rotation);
	/// <summary>
	/// Returns this ray rotated by <paramref name="rotation"/> around its own <see cref="EndPoint"/>, which therefore stays fixed.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	public BoundedRay RotatedAroundEndBy(Rotation rotation) {
		var endPoint = _startPoint + _vect;
		return new(endPoint + _vect.Reversed * rotation, endPoint);
	}
	/// <summary>
	/// Returns this ray rotated by <paramref name="rotation"/> around its own <see cref="MiddlePoint"/>, which therefore stays fixed.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	public BoundedRay RotatedAroundMiddleBy(Rotation rotation) {
		var newVect = _vect * rotation;
		var newStartPoint = _startPoint + ((_vect * 0.5f) - (newVect * 0.5f));
		return new(newStartPoint, newVect);
	}

	/// <inheritdoc cref="RotatedAroundStartBy(Rotation)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay RotatedAroundStartBy(Quaternion rotationQuaternion) => new(_startPoint, _vect.RotatedBy(rotationQuaternion));
	/// <inheritdoc cref="RotatedAroundEndBy(Rotation)" />
	public BoundedRay RotatedAroundEndBy(Quaternion rotationQuaternion) {
		var endPoint = _startPoint + _vect;
		return new(endPoint + _vect.Reversed.RotatedBy(rotationQuaternion), endPoint);
	}
	/// <inheritdoc cref="RotatedAroundMiddleBy(Rotation)" />
	public BoundedRay RotatedAroundMiddleBy(Quaternion rotationQuaternion) {
		var newVect = _vect.RotatedBy(rotationQuaternion);
		var newStartPoint = _startPoint + ((_vect * 0.5f) - (newVect * 0.5f));
		return new(newStartPoint, newVect);
	}

	/// <summary>
	/// Returns this ray rotated by <paramref name="rot"/> around the world origin (<c>(0f, 0f, 0f)</c>), moving both <see cref="StartPoint"/> and <see cref="EndPoint"/> as if they were vectors from the origin.
	/// </summary>
	/// <param name="rot">The rotation to apply.</param>
	public BoundedRay RotatedAroundOriginBy(Rotation rot) {
		return new BoundedRay(
			StartPoint.AsVect().RotatedBy(rot).AsLocation(),
			EndPoint.AsVect().RotatedBy(rot).AsLocation()
		);
	}
	BoundedRay IRotatable<BoundedRay>.RotatedBy(Rotation rot) => RotatedAroundStartBy(rot); // We choose AroundStart as the "default" rotation because it keeps thing consistent with Ray
	/// <summary>
	/// Returns this ray rotated by <paramref name="rotation"/> around the point found by travelling <paramref name="signedPivotDistance"/> along this ray from <see cref="StartPoint"/>.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to rotate around. Can be outside the bounds of this ray.</param>
	public BoundedRay RotatedBy(Rotation rotation, float signedPivotDistance) => RotatedBy(rotation, UnboundedLocationAtDistance(signedPivotDistance));
	/// <summary>
	/// Returns this ray rotated by <paramref name="rotation"/> around <paramref name="pivot"/>, which therefore stays fixed.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	/// <param name="pivot">The point to rotate around. Does not need to lie on this ray.</param>
	public BoundedRay RotatedBy(Rotation rotation, Location pivot) {
		return new(pivot + (pivot >> StartPoint) * rotation, pivot + (pivot >> EndPoint) * rotation);
	}

	/// <inheritdoc cref="RotatedAroundOriginBy(Rotation)" />
	public BoundedRay RotatedAroundOriginBy(Quaternion rotQuat) {
		return new BoundedRay(
			StartPoint.AsVect().RotatedBy(rotQuat).AsLocation(),
			EndPoint.AsVect().RotatedBy(rotQuat).AsLocation()
		);
	}
	BoundedRay IRotatable<BoundedRay>.RotatedBy(Quaternion rotQuat) => RotatedAroundStartBy(rotQuat); // We choose AroundStart as the "default" rotation because it keeps thing consistent with Ray
	/// <inheritdoc cref="RotatedBy(Rotation,float)" />
	public BoundedRay RotatedBy(Quaternion rotationQuaternion, float signedPivotDistance) => RotatedBy(rotationQuaternion, UnboundedLocationAtDistance(signedPivotDistance));
	/// <inheritdoc cref="RotatedBy(Rotation,Location)" />
	public BoundedRay RotatedBy(Quaternion rotationQuaternion, Location pivot) {
		return new(pivot + (pivot >> StartPoint).RotatedBy(rotationQuaternion), pivot + (pivot >> EndPoint).RotatedBy(rotationQuaternion));
	}
	#endregion

	#region Scaling
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static BoundedRay IMultiplyOperators<BoundedRay, float, BoundedRay>.operator *(BoundedRay ray, float scalar) => ray.ScaledFromStartBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static BoundedRay IMultiplicative<BoundedRay, float, BoundedRay>.operator *(float scalar, BoundedRay ray) => ray.ScaledFromStartBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static BoundedRay IDivisionOperators<BoundedRay, float, BoundedRay>.operator /(BoundedRay ray, float scalar) => ray.ScaledFromStartBy(1f / scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	BoundedRay IScalable<BoundedRay>.ScaledBy(float scalar) => ScaledFromStartBy(scalar);
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> multiplied by <paramref name="scalar"/>, scaling from <see cref="StartPoint"/> (which therefore stays fixed).
	/// </summary>
	/// <remarks>
	/// This is the pivot used by the explicit <see cref="IScalable{TSelf}"/>/operator-overload implementations, matching the equivalent default on <see cref="Ray"/>.
	/// </remarks>
	/// <param name="scalar">The scale factor. A negative value flips the ray's direction as well as scaling its length.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay ScaledFromStartBy(float scalar) => new(_startPoint, _vect.ScaledBy(scalar));
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> multiplied by <paramref name="scalar"/>, scaling from <see cref="MiddlePoint"/> (which therefore stays fixed).
	/// </summary>
	/// <param name="scalar">The scale factor. A negative value flips the ray's direction as well as scaling its length.</param>
	public BoundedRay ScaledFromMiddleBy(float scalar) {
		var halfVect = _vect * 0.5f;
		var midPoint = _startPoint + halfVect;
		var scaledVect = _vect.ScaledBy(scalar);
		var newStart = midPoint - halfVect.ScaledBy(scalar);
		return new BoundedRay(newStart, newStart + scaledVect);
	}
	/// <summary>
	/// Returns this ray with its <see cref="Length"/> multiplied by <paramref name="scalar"/>, scaling from <see cref="EndPoint"/> (which therefore stays fixed).
	/// </summary>
	/// <param name="scalar">The scale factor. A negative value flips the ray's direction as well as scaling its length.</param>
	public BoundedRay ScaledFromEndBy(float scalar) {
		var scaledVect = _vect.ScaledBy(scalar);
		var newStart = (_startPoint + _vect) - scaledVect;
		return new BoundedRay(newStart, scaledVect);
	}
	/// <summary>
	/// Returns this ray scaled by <paramref name="scalar"/> around the world origin (<c>(0f, 0f, 0f)</c>), moving both <see cref="StartPoint"/> and <see cref="EndPoint"/> as if they were vectors from the origin.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	public BoundedRay ScaledFromOriginBy(float scalar) {
		return new BoundedRay(
			StartPoint.AsVect().ScaledBy(scalar).AsLocation(),
			EndPoint.AsVect().ScaledBy(scalar).AsLocation()
		);
	}
	/// <summary>
	/// Returns this ray scaled by <paramref name="scalar"/> around the point found by travelling <paramref name="scalingOriginSignedDistance"/> along this ray from <see cref="StartPoint"/>.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	/// <param name="scalingOriginSignedDistance">The distance along this ray, from <see cref="StartPoint"/>, of the pivot point around which the ray is scaled. Can be outside the bounds of this ray.</param>
	public BoundedRay ScaledBy(float scalar, float scalingOriginSignedDistance) {
		var pivotPoint = UnboundedLocationAtDistance(scalingOriginSignedDistance);
		var pivotToStartVect = -_vect.WithLength(scalingOriginSignedDistance);
		var pivotToEndVect = _vect.WithLength(_vect.Length - scalingOriginSignedDistance);
		return new BoundedRay(pivotPoint + pivotToStartVect * scalar, pivotPoint + pivotToEndVect * scalar);
	}
	/// <summary>
	/// Returns this ray scaled by <paramref name="scalar"/> around <paramref name="scalingOrigin"/>, which therefore stays fixed.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	/// <param name="scalingOrigin">The point to scale around. Does not need to lie on this ray.</param>
	public BoundedRay ScaledBy(float scalar, Location scalingOrigin) => ScaledBy(scalar, UnboundedDistanceAtPointClosestTo(scalingOrigin));

	BoundedRay IIndependentAxisScalable<BoundedRay>.ScaledBy(Vect vect) => ScaledFromMiddleBy(vect);
	/// <summary>
	/// Returns this ray with <see cref="StartToEndVect"/> scaled independently on each axis by <paramref name="vect"/>'s corresponding component, scaling from <see cref="StartPoint"/> (which therefore stays fixed).
	/// </summary>
	/// <param name="vect">The per-axis scale factors.</param>
	public BoundedRay ScaledFromStartBy(Vect vect) => new(_startPoint, _vect.ScaledBy(vect));
	/// <summary>
	/// Returns this ray with <see cref="StartToEndVect"/> scaled independently on each axis by <paramref name="vect"/>'s corresponding component, scaling from <see cref="MiddlePoint"/> (which therefore stays fixed).
	/// </summary>
	/// <param name="vect">The per-axis scale factors.</param>
	public BoundedRay ScaledFromMiddleBy(Vect vect) {
		var halfVect = _vect * 0.5f;
		var midPoint = _startPoint + halfVect;
		var scaledVect = _vect.ScaledBy(vect);
		var newStart = midPoint - halfVect.ScaledBy(vect);
		return new BoundedRay(newStart, newStart + scaledVect);
	}
	/// <summary>
	/// Returns this ray with <see cref="StartToEndVect"/> scaled independently on each axis by <paramref name="vect"/>'s corresponding component, scaling from <see cref="EndPoint"/> (which therefore stays fixed).
	/// </summary>
	/// <param name="vect">The per-axis scale factors.</param>
	public BoundedRay ScaledFromEndBy(Vect vect) {
		var scaledVect = _vect.ScaledBy(vect);
		var newStart = (_startPoint + _vect) - scaledVect;
		return new BoundedRay(newStart, scaledVect);
	}
	/// <summary>
	/// Returns this ray with <see cref="StartPoint"/> and <see cref="EndPoint"/> each scaled independently on each axis by <paramref name="vect"/>'s corresponding component, around the world origin (<c>(0f, 0f, 0f)</c>).
	/// </summary>
	/// <param name="vect">The per-axis scale factors.</param>
	public BoundedRay ScaledFromOriginBy(Vect vect) {
		return new BoundedRay(
			StartPoint.AsVect().ScaledBy(vect).AsLocation(),
			EndPoint.AsVect().ScaledBy(vect).AsLocation()
		);
	}
	/// <summary>
	/// Returns this ray with <see cref="StartToEndVect"/> scaled independently on each axis by <paramref name="vect"/>'s corresponding component, around the point found by travelling <paramref name="scalingOriginSignedDistance"/> along this ray from <see cref="StartPoint"/>.
	/// </summary>
	/// <param name="vect">The per-axis scale factors.</param>
	/// <param name="scalingOriginSignedDistance">The distance along this ray, from <see cref="StartPoint"/>, of the pivot point around which the ray is scaled. Can be outside the bounds of this ray.</param>
	public BoundedRay ScaledBy(Vect vect, float scalingOriginSignedDistance) {
		var pivotPoint = UnboundedLocationAtDistance(scalingOriginSignedDistance);
		var pivotToStartVect = -_vect.WithLength(scalingOriginSignedDistance);
		var pivotToEndVect = _vect.WithLength(_vect.Length - scalingOriginSignedDistance);
		return new BoundedRay(pivotPoint + pivotToStartVect * vect, pivotPoint + pivotToEndVect * vect);
	}
	/// <summary>
	/// Returns this ray with <see cref="StartToEndVect"/> scaled independently on each axis by <paramref name="vect"/>'s corresponding component, around <paramref name="scalingOrigin"/>, which therefore stays fixed.
	/// </summary>
	/// <param name="vect">The per-axis scale factors.</param>
	/// <param name="scalingOrigin">The point to scale around. Does not need to lie on this ray.</param>
	public BoundedRay ScaledBy(Vect vect, Location scalingOrigin) => ScaledBy(vect, UnboundedDistanceAtPointClosestTo(scalingOrigin));
	#endregion

	#region Transformation
	/// <summary>
	/// Applies <paramref name="transform"/> to <paramref name="ray"/> around its own <see cref="StartPoint"/>; equivalent to <see cref="TransformedAroundStartBy(Transform)"/>.
	/// </summary>
	/// <param name="ray">The ray to transform.</param>
	/// <param name="transform">The transform to apply.</param>
	public static BoundedRay operator *(BoundedRay ray, Transform transform) => ray.TransformedAroundStartBy(transform);
	/// <inheritdoc cref="operator *(BoundedRay,Transform)" />
	public static BoundedRay operator *(Transform transform, BoundedRay ray) => ray.TransformedAroundStartBy(transform);
	BoundedRay ITransformable<BoundedRay>.TransformedBy(Transform transform) => TransformedAroundOriginBy(transform);
	/// <summary>
	/// Returns this ray with <paramref name="transform"/> applied around its own <see cref="StartPoint"/>, which therefore stays fixed relative to any translation component of <paramref name="transform"/>.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	public BoundedRay TransformedAroundStartBy(Transform transform) => TransformedBy(transform, StartPoint);
	/// <summary>
	/// Returns this ray with <paramref name="transform"/> applied around its own <see cref="MiddlePoint"/>, which therefore stays fixed relative to any translation component of <paramref name="transform"/>.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	public BoundedRay TransformedAroundMiddleBy(Transform transform) => TransformedBy(transform, MiddlePoint);
	/// <summary>
	/// Returns this ray with <paramref name="transform"/> applied around its own <see cref="EndPoint"/>, which therefore stays fixed relative to any translation component of <paramref name="transform"/>.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	public BoundedRay TransformedAroundEndBy(Transform transform) => TransformedBy(transform, EndPoint);
	/// <summary>
	/// Returns this ray with <paramref name="transform"/> applied around the world origin (<c>(0f, 0f, 0f)</c>).
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	public BoundedRay TransformedAroundOriginBy(Transform transform) {
		return new(StartPoint.TransformedAroundOriginBy(transform), EndPoint.TransformedAroundOriginBy(transform));
	}
	/// <summary>
	/// Returns this ray with <paramref name="transform"/> applied around the point found by travelling <paramref name="transformationOriginSignedDistance"/> along this ray from <see cref="StartPoint"/>.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	/// <param name="transformationOriginSignedDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point the transform is applied around. Can be outside the bounds of this ray.</param>
	public BoundedRay TransformedBy(Transform transform, float transformationOriginSignedDistance) => TransformedBy(transform, UnboundedLocationAtDistance(transformationOriginSignedDistance));
	/// <summary>
	/// Returns this ray with <paramref name="transform"/> applied around <paramref name="transformationOrigin"/>.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	/// <param name="transformationOrigin">The point the transform is applied around. Does not need to lie on this ray.</param>
	public BoundedRay TransformedBy(Transform transform, Location transformationOrigin) {
		return new(StartPoint.TransformedBy(transform, transformationOrigin), EndPoint.TransformedBy(transform, transformationOrigin));
	}

	BoundedRay ITransformable<BoundedRay>.TransformedByInverseOf(Transform transform) => TransformedAroundOriginByInverseOf(transform);
	/// <summary>
	/// Returns this ray with the inverse of <paramref name="transform"/> applied around its own <see cref="StartPoint"/>; the exact reverse of <see cref="TransformedAroundStartBy(Transform)"/>.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="scaleAndRotateAroundStartPostTranslation">
	/// If <see langword="true"/> (the default), <see cref="StartPoint"/> is first moved back by <paramref name="transform"/>'s translation before being used as the scale/rotation pivot, undoing <see cref="TransformedAroundStartBy(Transform)"/> exactly.
	/// If <see langword="false"/>, the current <see cref="StartPoint"/> is used as the pivot directly instead.
	/// </param>
	public BoundedRay TransformedAroundStartByInverseOf(Transform transform, bool scaleAndRotateAroundStartPostTranslation = true) {
		return TransformedByInverseOf(transform, scaleAndRotateAroundStartPostTranslation ? StartPoint - transform.Translation : StartPoint);
	}
	/// <summary>
	/// Returns this ray with the inverse of <paramref name="transform"/> applied around its own <see cref="MiddlePoint"/>; the exact reverse of <see cref="TransformedAroundMiddleBy(Transform)"/>.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="scaleAndRotateAroundMiddlePostTranslation">
	/// If <see langword="true"/> (the default), <see cref="MiddlePoint"/> is first moved back by <paramref name="transform"/>'s translation before being used as the scale/rotation pivot, undoing <see cref="TransformedAroundMiddleBy(Transform)"/> exactly.
	/// If <see langword="false"/>, the current <see cref="MiddlePoint"/> is used as the pivot directly instead.
	/// </param>
	public BoundedRay TransformedAroundMiddleByInverseOf(Transform transform, bool scaleAndRotateAroundMiddlePostTranslation = true) {
		return TransformedByInverseOf(transform, scaleAndRotateAroundMiddlePostTranslation ? MiddlePoint - transform.Translation : MiddlePoint);
	}
	/// <summary>
	/// Returns this ray with the inverse of <paramref name="transform"/> applied around its own <see cref="EndPoint"/>; the exact reverse of <see cref="TransformedAroundEndBy(Transform)"/>.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="scaleAndRotateAroundEndPostTranslation">
	/// If <see langword="true"/> (the default), <see cref="EndPoint"/> is first moved back by <paramref name="transform"/>'s translation before being used as the scale/rotation pivot, undoing <see cref="TransformedAroundEndBy(Transform)"/> exactly.
	/// If <see langword="false"/>, the current <see cref="EndPoint"/> is used as the pivot directly instead.
	/// </param>
	public BoundedRay TransformedAroundEndByInverseOf(Transform transform, bool scaleAndRotateAroundEndPostTranslation = true) {
		return TransformedByInverseOf(transform, scaleAndRotateAroundEndPostTranslation ? EndPoint - transform.Translation : EndPoint);
	}
	/// <summary>
	/// Returns this ray with the inverse of <paramref name="transform"/> applied around the world origin (<c>(0f, 0f, 0f)</c>); the exact reverse of <see cref="TransformedAroundOriginBy(Transform)"/>.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	public BoundedRay TransformedAroundOriginByInverseOf(Transform transform) {
		return new(StartPoint.TransformedAroundOriginByInverseOf(transform), EndPoint.TransformedAroundOriginByInverseOf(transform));
	}
	/// <summary>
	/// Returns this ray with the inverse of <paramref name="transform"/> applied around the point found by travelling <paramref name="transformationOriginSignedDistance"/> along this ray from <see cref="StartPoint"/>.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="transformationOriginSignedDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point the inverse transform is applied around. Can be outside the bounds of this ray.</param>
	public BoundedRay TransformedByInverseOf(Transform transform, float transformationOriginSignedDistance) => TransformedByInverseOf(transform, UnboundedLocationAtDistance(transformationOriginSignedDistance));
	/// <summary>
	/// Returns this ray with the inverse of <paramref name="transform"/> applied around <paramref name="transformationOrigin"/>; the exact reverse of <see cref="TransformedBy(Transform,Location)"/>.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="transformationOrigin">The point the inverse transform is applied around. Does not need to lie on this ray.</param>
	public BoundedRay TransformedByInverseOf(Transform transform, Location transformationOrigin) {
		return new(StartPoint.TransformedByInverseOf(transform, transformationOrigin), EndPoint.TransformedByInverseOf(transform, transformationOrigin));
	}
	#endregion

	#region Distance / Closest Point / Containment
	/// <summary>
	/// Returns the point on this ray that is closest to <paramref name="location"/>.
	/// </summary>
	/// <remarks>
	/// Because this ray is bounded, the result may be <see cref="StartPoint"/> or <see cref="EndPoint"/> if <paramref name="location"/> is closest to (or beyond) one of the ends.
	/// </remarks>
	/// <param name="location">The location to measure from.</param>
	public Location PointClosestTo(Location location) {
		var vectCoefficient = Vector3.Dot((location - _startPoint).ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => Single.IsFinite(vectCoefficient) ? _startPoint + _vect * vectCoefficient : _startPoint
		};
	}
	/// <inheritdoc cref="ILineLike.PointClosestToOrigin" />
	public Location PointClosestToOrigin() {
		var vectCoefficient = -Vector3.Dot(_startPoint.ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => Single.IsFinite(vectCoefficient) ? _startPoint + _vect * vectCoefficient : _startPoint
		};
	}

	/// <summary>
	/// Calculates the distance between this ray and <paramref name="location"/>.
	/// </summary>
	/// <param name="location">The location to measure to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location location) => location.DistanceFrom(PointClosestTo(location));
	/// <summary>
	/// Calculates the square of the distance between this ray and <paramref name="location"/>. Cheaper than <see cref="DistanceFrom(Location)"/> when only comparing distances.
	/// </summary>
	/// <param name="location">The location to measure to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Location location) => location.DistanceSquaredFrom(PointClosestTo(location));
	/// <inheritdoc cref="ILineLike.DistanceFromOrigin" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromOrigin() => ((Vect) PointClosestToOrigin()).Length;
	/// <inheritdoc cref="ILineLike.DistanceSquaredFromOrigin" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromOrigin() => ((Vect) PointClosestToOrigin()).LengthSquared;
	/// <inheritdoc cref="ILineLike.Contains(Location)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, ILineLike.DefaultLineThickness);
	/// <inheritdoc cref="ILineLike.Contains(Location,float)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;

	/// <summary>
	/// Returns the point on this ray that is closest to <paramref name="line"/>.
	/// </summary>
	/// <param name="line">The line to measure against.</param>
	public Location PointClosestTo(Line line) {
		var intersectionDistance = ILineLike.CalculateUnboundedIntersectionDistanceOnThisLine(this, line);
		return intersectionDistance != null ? BoundedLocationAtDistance(intersectionDistance.Value) : StartPoint;
	}
	/// <summary>
	/// Returns the point on this ray that is closest to <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The ray to measure against.</param>
	public Location PointClosestTo(Ray ray) {
		var intersectionDistances = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(this, ray);
		if (intersectionDistances == null || !ray.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return PointClosestTo(ray.StartPoint);
		else return BoundedLocationAtDistance(intersectionDistances.Value.ThisDistance);
	}
	/// <summary>
	/// Returns the point on this ray that is closest to <paramref name="boundedRay"/>.
	/// </summary>
	/// <param name="boundedRay">The ray to measure against.</param>
	public Location PointClosestTo(BoundedRay boundedRay) {
		var intersectionDistances = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(this, boundedRay);
		if (intersectionDistances == null) {
			var distanceToOtherStart = DistanceFrom(boundedRay.StartPoint);
			var distanceToOtherEnd = DistanceFrom(boundedRay.EndPoint);
			return distanceToOtherStart < distanceToOtherEnd ? PointClosestTo(boundedRay.StartPoint) : PointClosestTo(boundedRay.EndPoint);
		}
		var boundOtherDistance = boundedRay.BindDistance(intersectionDistances.Value.OtherDistance);
		// ReSharper disable once CompareOfFloatsByEqualityOperator distance will be unchanged if within line bounds
		if (boundOtherDistance == intersectionDistances.Value.OtherDistance) {
			return BoundedLocationAtDistance(intersectionDistances.Value.ThisDistance);
		}
		else {
			return PointClosestTo(boundedRay.UnboundedLocationAtDistance(boundOtherDistance));
		}
	}
	#endregion

	#region Plane Intersection / Split / Incident Angle / Reflection / Distance / Closest Point
	float? GetUnboundedPlaneIntersectionDistance(Plane plane) {
		var similarityToNormal = plane.Normal.Dot(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		return (plane.PointClosestToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
	}

	/// <summary>
	/// Calculates where this ray intersects <paramref name="plane"/>, if at all.
	/// </summary>
	/// <param name="plane">The plane to test against.</param>
	/// <returns><see langword="null"/> if this ray is parallel to <paramref name="plane"/> or does not reach it within its bounds; the intersection point otherwise.</returns>
	public Location? IntersectionWith(Plane plane) {
		var distance = GetUnboundedPlaneIntersectionDistance(plane);
		return distance >= 0f && distance <= Length ? UnboundedLocationAtDistance(distance.Value) : null; // Null means Plane parallel with line or outside line boundaries
	}
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray does intersect <paramref name="plane"/> within its bounds. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to test against.</param>
	public Location FastIntersectionWith(Plane plane) => UnboundedLocationAtDistance((plane.PointClosestToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / plane.Normal.Dot(Direction));

	/// <summary>
	/// Determines whether this ray intersects <paramref name="plane"/> within its bounds.
	/// </summary>
	/// <param name="plane">The plane to test against.</param>
	public bool IsIntersectedBy(Plane plane) {
		var unboundedIntersectionDistance = GetUnboundedPlaneIntersectionDistance(plane);
		return unboundedIntersectionDistance >= 0f && unboundedIntersectionDistance <= Length;
	}

	/// <summary>
	/// Executes the same function as <see cref="SplitBy(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray does intersect <paramref name="plane"/> within its bounds. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to split this ray at.</param>
	public Pair<BoundedRay, BoundedRay> FastSplitBy(Plane plane) {
		var intersectionPoint = FastIntersectionWith(plane);
		return new(new(StartPoint, intersectionPoint), new(intersectionPoint, EndPoint));
	}
	/// <summary>
	/// Splits this ray into two at the point it intersects <paramref name="plane"/>, if it does.
	/// </summary>
	/// <param name="plane">The plane to split this ray at.</param>
	/// <returns><see langword="null"/> if this ray does not intersect <paramref name="plane"/> within its bounds; otherwise a pair of two rays, the first from <see cref="StartPoint"/> to the split point and the second from the split point to <see cref="EndPoint"/>.</returns>
	public Pair<BoundedRay, BoundedRay>? SplitBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		return intersectionPoint == null ? null : new(new(StartPoint, intersectionPoint.Value), new(intersectionPoint.Value, EndPoint));
	}

	/// <summary>
	/// Calculates the angle at which this ray meets <paramref name="plane"/>, if it intersects it within its bounds.
	/// </summary>
	/// <param name="plane">The plane to measure against.</param>
	/// <returns><see langword="null"/> if this ray does not intersect <paramref name="plane"/> within its bounds; the incident angle otherwise.</returns>
	public Angle? IncidentAngleWith(Plane plane) => IsIntersectedBy(plane) ? plane.IncidentAngleWith(Direction) : null;
	/// <summary>
	/// Executes the same function as <see cref="IncidentAngleWith(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray does intersect <paramref name="plane"/> within its bounds. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to measure against.</param>
	public Angle FastIncidentAngleWith(Plane plane) => plane.FastIncidentAngleWith(Direction);

	/// <summary>
	/// Reflects the portion of this ray beyond <paramref name="plane"/> back off it, if this ray intersects <paramref name="plane"/> within its bounds.
	/// </summary>
	/// <remarks>
	/// The resultant ray starts at the intersection point and has the same overall length as the portion of this ray it replaces (from the intersection point to <see cref="EndPoint"/>), just folded back off <paramref name="plane"/>.
	/// </remarks>
	/// <param name="plane">The plane to reflect off.</param>
	/// <returns><see langword="null"/> if this ray does not intersect <paramref name="plane"/> within its bounds; the reflected ray otherwise.</returns>
	public BoundedRay? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new BoundedRay(intersectionPoint.Value, Direction.FastReflectedBy(plane) * (Length - intersectionPoint.Value.DistanceFrom(StartPoint)));
	}
	/// <summary>
	/// Executes the same function as <see cref="ReflectedBy(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray does intersect <paramref name="plane"/> within its bounds. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to reflect off.</param>
	public BoundedRay FastReflectedBy(Plane plane) {
		var intersectionPoint = FastIntersectionWith(plane);
		return new BoundedRay(intersectionPoint, Direction.FastReflectedBy(plane) * (Length - intersectionPoint.DistanceFrom(StartPoint)));
	}

	/// <summary>
	/// Returns the point on this ray that is closest to <paramref name="plane"/>.
	/// </summary>
	/// <remarks>
	/// If this ray intersects <paramref name="plane"/> within its bounds, every point on <paramref name="plane"/> is equally close, so the intersection point is returned; otherwise the nearer of <see cref="StartPoint"/>/<see cref="EndPoint"/> is returned.
	/// </remarks>
	/// <param name="plane">The plane to measure against.</param>
	public Location PointClosestTo(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		return BoundedLocationAtDistance(unboundedDistance ?? 0f); // If unboundedDistance is null we're parallel so the StartPoint is as close as any other point
	}
	/// <summary>
	/// Returns the point on <paramref name="plane"/> that is closest to this ray.
	/// </summary>
	/// <param name="plane">The plane to measure against.</param>
	public Location ClosestPointOn(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		var closestPointOnLine = BoundedLocationAtDistance(unboundedDistance ?? 0f);
		if (unboundedDistance >= 0f && unboundedDistance <= Length) return closestPointOnLine; // Actual intersection
		else return plane.PointClosestTo(closestPointOnLine);
	}

	/// <summary>
	/// Calculates the distance between this ray and <paramref name="plane"/>, signed according to which side of <paramref name="plane"/> the closer end of this ray falls on.
	/// </summary>
	/// <remarks>
	/// The sign matches <see cref="Plane.SignedDistanceFrom(Location)"/>: positive when the closer end is on the side <see cref="Plane.Normal"/> points towards, negative on the opposite side, and (approximately) zero at an intersection.
	/// </remarks>
	/// <param name="plane">The plane to measure against.</param>
	public float SignedDistanceFrom(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane) ?? 0f;

		if (unboundedDistance <= 0f) return plane.SignedDistanceFrom(StartPoint);
		else if (unboundedDistance > Length) return plane.SignedDistanceFrom(EndPoint);
		else return plane.SignedDistanceFrom(UnboundedLocationAtDistance(unboundedDistance));
	}

	/// <summary>
	/// Calculates the (unsigned) distance between this ray and <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The plane to measure against.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => MathF.Abs(SignedDistanceFrom(plane));

	/// <summary>
	/// Determines how this ray sits relative to <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The plane to compare against.</param>
	public PlaneObjectRelationship RelationshipTo(Plane plane) {
		return SignedDistanceFrom(plane) switch {
			> 0f => PlaneObjectRelationship.PlaneFacesTowardsObject,
			< 0f => PlaneObjectRelationship.PlaneFacesAwayFromObject,
			_ => PlaneObjectRelationship.PlaneIntersectsObject
		};
	}
	#endregion

	#region Parallelization / Orthogonalization / Projection
	/// <inheritdoc/>
	public BoundedRay? ParallelizedWith(Direction direction) => ParallelizedAroundStartWith(direction);
	/// <inheritdoc/>
	public BoundedRay FastParallelizedWith(Direction direction) => FastParallelizedAroundStartWith(direction);

	/// <summary>
	/// Attempts to parallelize this ray with <paramref name="direction"/>, pivoting around its own <see cref="StartPoint"/> (which therefore stays fixed) and keeping its <see cref="Length"/> unchanged.
	/// </summary>
	/// <param name="direction">The target direction.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this ray is already exactly orthogonal to <paramref name="direction"/>); the parallelized result otherwise.</returns>
	public BoundedRay? ParallelizedAroundStartWith(Direction direction) {
		var newVect = StartToEndVect.ParallelizedWith(direction);
		return newVect == null ? null : new(StartPoint, newVect.Value);
	}
	/// <inheritdoc cref="ParallelizedAroundStartWith(Direction)" />
	public BoundedRay? ParallelizedAroundMiddleWith(Direction direction) {
		var newDir = Direction.ParallelizedWith(direction);
		if (newDir == null) return null;
		return RotatedAroundMiddleBy(Direction >> newDir.Value);
	}
	/// <inheritdoc cref="ParallelizedAroundStartWith(Direction)" />
	public BoundedRay? ParallelizedAroundEndWith(Direction direction) {
		var newVect = StartToEndVect.ParallelizedWith(direction);
		return newVect == null ? null : new(EndPoint - newVect.Value, newVect.Value);
	}
	/// <summary>
	/// Attempts to parallelize this ray with <paramref name="direction"/>, pivoting around the point found by travelling <paramref name="signedPivotDistance"/> along this ray from <see cref="StartPoint"/>, and keeping its <see cref="Length"/> unchanged.
	/// </summary>
	/// <param name="direction">The target direction.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this ray is already exactly orthogonal to <paramref name="direction"/>); the parallelized result otherwise.</returns>
	public BoundedRay? ParallelizedWith(Direction direction, float signedPivotDistance) {
		var newDir = Direction.ParallelizedWith(direction);
		if (newDir == null) return null;
		return RotatedBy(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundStartWith(Direction)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="direction"/> is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="direction">The target direction.</param>
	public BoundedRay FastParallelizedAroundStartWith(Direction direction) => new(StartPoint, StartToEndVect.FastParallelizedWith(direction));
	/// <inheritdoc cref="FastParallelizedAroundStartWith(Direction)" />
	public BoundedRay FastParallelizedAroundMiddleWith(Direction direction) => RotatedAroundMiddleBy(Direction >> Direction.FastParallelizedWith(direction));
	/// <inheritdoc cref="FastParallelizedAroundStartWith(Direction)" />
	public BoundedRay FastParallelizedAroundEndWith(Direction direction) {
		var newVect = StartToEndVect.FastParallelizedWith(direction);
		return new(EndPoint - newVect, newVect);
	}
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(Direction,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="direction"/> is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="direction">The target direction.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay FastParallelizedWith(Direction direction, float signedPivotDistance) => RotatedBy(Direction >> Direction.FastParallelizedWith(direction), UnboundedLocationAtDistance(signedPivotDistance));

	/// <inheritdoc/>
	public BoundedRay? OrthogonalizedAgainst(Direction direction) => OrthogonalizedAroundStartAgainst(direction);
	/// <inheritdoc/>
	public BoundedRay FastOrthogonalizedAgainst(Direction direction) => FastOrthogonalizedAroundStartAgainst(direction);

	/// <summary>
	/// Attempts to orthogonalize this ray against <paramref name="direction"/>, pivoting around its own <see cref="StartPoint"/> (which therefore stays fixed) and keeping its <see cref="Length"/> unchanged.
	/// </summary>
	/// <param name="direction">The target direction.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this ray is already exactly parallel or exactly opposite to <paramref name="direction"/>); the orthogonalized result otherwise.</returns>
	public BoundedRay? OrthogonalizedAroundStartAgainst(Direction direction) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(direction);
		return newVect == null ? null : new(StartPoint, newVect.Value);
	}
	/// <inheritdoc cref="OrthogonalizedAroundStartAgainst(Direction)" />
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Direction direction) {
		var newDir = Direction.OrthogonalizedAgainst(direction);
		if (newDir == null) return null;
		return RotatedAroundMiddleBy(Direction >> newDir.Value);
	}
	/// <inheritdoc cref="OrthogonalizedAroundStartAgainst(Direction)" />
	public BoundedRay? OrthogonalizedAroundEndAgainst(Direction direction) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(direction);
		return newVect == null ? null : new(EndPoint - newVect.Value, newVect.Value);
	}
	/// <summary>
	/// Attempts to orthogonalize this ray against <paramref name="direction"/>, pivoting around the point found by travelling <paramref name="signedPivotDistance"/> along this ray from <see cref="StartPoint"/>, and keeping its <see cref="Length"/> unchanged.
	/// </summary>
	/// <param name="direction">The target direction.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this ray is already exactly parallel or exactly opposite to <paramref name="direction"/>); the orthogonalized result otherwise.</returns>
	public BoundedRay? OrthogonalizedAgainst(Direction direction, float signedPivotDistance) {
		var newDir = Direction.OrthogonalizedAgainst(direction);
		if (newDir == null) return null;
		return RotatedBy(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundStartAgainst(Direction)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="direction"/> is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="direction">The target direction.</param>
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Direction direction) => new(StartPoint, StartToEndVect.FastOrthogonalizedAgainst(direction));
	/// <inheritdoc cref="FastOrthogonalizedAroundStartAgainst(Direction)" />
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Direction direction) => RotatedAroundMiddleBy(Direction >> Direction.FastOrthogonalizedAgainst(direction));
	/// <inheritdoc cref="FastOrthogonalizedAroundStartAgainst(Direction)" />
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Direction direction) {
		var newVect = StartToEndVect.FastOrthogonalizedAgainst(direction);
		return new(EndPoint - newVect, newVect);
	}
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(Direction,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="direction"/> is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="direction">The target direction.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay FastOrthogonalizedAgainst(Direction direction, float signedPivotDistance) => RotatedBy(Direction >> Direction.FastOrthogonalizedAgainst(direction), UnboundedLocationAtDistance(signedPivotDistance));


	/// <summary>
	/// Equivalent to <see cref="ParallelizedAroundStartWith(Direction)"/>, using <paramref name="line"/>'s direction.
	/// </summary>
	/// <param name="line">The target line.</param>
	public BoundedRay? ParallelizedAroundStartWith(Line line) => ParallelizedAroundStartWith(line.Direction);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedAroundMiddleWith(Direction)"/>, using <paramref name="line"/>'s direction.
	/// </summary>
	/// <param name="line">The target line.</param>
	public BoundedRay? ParallelizedAroundMiddleWith(Line line) => ParallelizedAroundMiddleWith(line.Direction);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedAroundEndWith(Direction)"/>, using <paramref name="line"/>'s direction.
	/// </summary>
	/// <param name="line">The target line.</param>
	public BoundedRay? ParallelizedAroundEndWith(Line line) => ParallelizedAroundEndWith(line.Direction);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedWith(Direction,float)"/>, using <paramref name="line"/>'s direction.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay? ParallelizedWith(Line line, float signedPivotDistance) => ParallelizedWith(line.Direction, signedPivotDistance);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundStartWith(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	public BoundedRay FastParallelizedAroundStartWith(Line line) => FastParallelizedAroundStartWith(line.Direction);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundMiddleWith(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	public BoundedRay FastParallelizedAroundMiddleWith(Line line) => FastParallelizedAroundMiddleWith(line.Direction);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundEndWith(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	public BoundedRay FastParallelizedAroundEndWith(Line line) => FastParallelizedAroundEndWith(line.Direction);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(Line,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay FastParallelizedWith(Line line, float signedPivotDistance) => FastParallelizedWith(line.Direction, signedPivotDistance);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedAroundStartWith(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	public BoundedRay? ParallelizedAroundStartWith(Ray ray) => ParallelizedAroundStartWith(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedAroundMiddleWith(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	public BoundedRay? ParallelizedAroundMiddleWith(Ray ray) => ParallelizedAroundMiddleWith(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedAroundEndWith(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	public BoundedRay? ParallelizedAroundEndWith(Ray ray) => ParallelizedAroundEndWith(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedWith(Direction,float)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay? ParallelizedWith(Ray ray, float signedPivotDistance) => ParallelizedWith(ray.Direction, signedPivotDistance);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundStartWith(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	public BoundedRay FastParallelizedAroundStartWith(Ray ray) => FastParallelizedAroundStartWith(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundMiddleWith(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	public BoundedRay FastParallelizedAroundMiddleWith(Ray ray) => FastParallelizedAroundMiddleWith(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundEndWith(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	public BoundedRay FastParallelizedAroundEndWith(Ray ray) => FastParallelizedAroundEndWith(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(Ray,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay FastParallelizedWith(Ray ray, float signedPivotDistance) => FastParallelizedWith(ray.Direction, signedPivotDistance);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedAroundStartWith(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay? ParallelizedAroundStartWith(BoundedRay ray) => ParallelizedAroundStartWith(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedAroundMiddleWith(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay? ParallelizedAroundMiddleWith(BoundedRay ray) => ParallelizedAroundMiddleWith(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedAroundEndWith(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay? ParallelizedAroundEndWith(BoundedRay ray) => ParallelizedAroundEndWith(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="ParallelizedWith(Direction,float)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay? ParallelizedWith(BoundedRay ray, float signedPivotDistance) => ParallelizedWith(ray.Direction, signedPivotDistance);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundStartWith(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay FastParallelizedAroundStartWith(BoundedRay ray) => FastParallelizedAroundStartWith(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundMiddleWith(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay FastParallelizedAroundMiddleWith(BoundedRay ray) => FastParallelizedAroundMiddleWith(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundEndWith(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay FastParallelizedAroundEndWith(BoundedRay ray) => FastParallelizedAroundEndWith(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(BoundedRay,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly orthogonal to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay FastParallelizedWith(BoundedRay ray, float signedPivotDistance) => FastParallelizedWith(ray.Direction, signedPivotDistance);

	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAroundStartAgainst(Direction)"/>, using <paramref name="line"/>'s direction.
	/// </summary>
	/// <param name="line">The target line.</param>
	public BoundedRay? OrthogonalizedAroundStartAgainst(Line line) => OrthogonalizedAroundStartAgainst(line.Direction);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAroundMiddleAgainst(Direction)"/>, using <paramref name="line"/>'s direction.
	/// </summary>
	/// <param name="line">The target line.</param>
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Line line) => OrthogonalizedAroundMiddleAgainst(line.Direction);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAroundEndAgainst(Direction)"/>, using <paramref name="line"/>'s direction.
	/// </summary>
	/// <param name="line">The target line.</param>
	public BoundedRay? OrthogonalizedAroundEndAgainst(Line line) => OrthogonalizedAroundEndAgainst(line.Direction);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAgainst(Direction,float)"/>, using <paramref name="line"/>'s direction.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay? OrthogonalizedAgainst(Line line, float signedPivotDistance) => OrthogonalizedAgainst(line.Direction, signedPivotDistance);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundStartAgainst(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Line line) => FastOrthogonalizedAroundStartAgainst(line.Direction);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundMiddleAgainst(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Line line) => FastOrthogonalizedAroundMiddleAgainst(line.Direction);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundEndAgainst(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Line line) => FastOrthogonalizedAroundEndAgainst(line.Direction);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(Line,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay FastOrthogonalizedAgainst(Line line, float signedPivotDistance) => FastOrthogonalizedAgainst(line.Direction, signedPivotDistance);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAroundStartAgainst(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	public BoundedRay? OrthogonalizedAroundStartAgainst(Ray ray) => OrthogonalizedAroundStartAgainst(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAroundMiddleAgainst(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Ray ray) => OrthogonalizedAroundMiddleAgainst(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAroundEndAgainst(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	public BoundedRay? OrthogonalizedAroundEndAgainst(Ray ray) => OrthogonalizedAroundEndAgainst(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAgainst(Direction,float)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay? OrthogonalizedAgainst(Ray ray, float signedPivotDistance) => OrthogonalizedAgainst(ray.Direction, signedPivotDistance);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundStartAgainst(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Ray ray) => FastOrthogonalizedAroundStartAgainst(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundMiddleAgainst(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Ray ray) => FastOrthogonalizedAroundMiddleAgainst(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundEndAgainst(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Ray ray) => FastOrthogonalizedAroundEndAgainst(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(Ray,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay FastOrthogonalizedAgainst(Ray ray, float signedPivotDistance) => FastOrthogonalizedAgainst(ray.Direction, signedPivotDistance);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAroundStartAgainst(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay? OrthogonalizedAroundStartAgainst(BoundedRay ray) => OrthogonalizedAroundStartAgainst(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAroundMiddleAgainst(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(BoundedRay ray) => OrthogonalizedAroundMiddleAgainst(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAroundEndAgainst(Direction)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay? OrthogonalizedAroundEndAgainst(BoundedRay ray) => OrthogonalizedAroundEndAgainst(ray.Direction);
	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAgainst(Direction,float)"/>, using <paramref name="ray"/>'s direction.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay? OrthogonalizedAgainst(BoundedRay ray, float signedPivotDistance) => OrthogonalizedAgainst(ray.Direction, signedPivotDistance);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundStartAgainst(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay FastOrthogonalizedAroundStartAgainst(BoundedRay ray) => FastOrthogonalizedAroundStartAgainst(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundMiddleAgainst(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(BoundedRay ray) => FastOrthogonalizedAroundMiddleAgainst(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundEndAgainst(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	public BoundedRay FastOrthogonalizedAroundEndAgainst(BoundedRay ray) => FastOrthogonalizedAroundEndAgainst(ray.Direction);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(BoundedRay,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/>'s direction is not <see cref="Direction.None"/>, that this ray is not already exactly parallel or exactly opposite to it, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when any of those conditions are broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay FastOrthogonalizedAgainst(BoundedRay ray, float signedPivotDistance) => FastOrthogonalizedAgainst(ray.Direction, signedPivotDistance);


	/// <inheritdoc/>
	public BoundedRay? ParallelizedWith(Plane plane) => ParallelizedAroundStartWith(plane);
	/// <inheritdoc/>
	public BoundedRay FastParallelizedWith(Plane plane) => FastParallelizedAroundStartWith(plane);

	/// <summary>
	/// Attempts to parallelize this ray with <paramref name="plane"/> (i.e. rotate it so it lies flat within the plane), pivoting around its own <see cref="StartPoint"/> (which therefore stays fixed) and keeping its <see cref="Length"/> unchanged.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this ray is already exactly orthogonal to <paramref name="plane"/>); the parallelized result otherwise.</returns>
	public BoundedRay? ParallelizedAroundStartWith(Plane plane) {
		var newVect = StartToEndVect.ParallelizedWith(plane);
		if (newVect == null) return null;
		return new BoundedRay(StartPoint, newVect.Value);
	}
	/// <inheritdoc cref="ParallelizedAroundStartWith(Plane)" />
	public BoundedRay? ParallelizedAroundMiddleWith(Plane plane) {
		var newDir = Direction.ParallelizedWith(plane);
		if (newDir == null) return null;
		return RotatedAroundMiddleBy(Direction >> newDir.Value);
	}
	/// <inheritdoc cref="ParallelizedAroundStartWith(Plane)" />
	public BoundedRay? ParallelizedAroundEndWith(Plane plane) {
		var newVect = StartToEndVect.ParallelizedWith(plane);
		if (newVect == null) return null;
		return new BoundedRay(EndPoint - newVect.Value, EndPoint);
	}
	/// <summary>
	/// Attempts to parallelize this ray with <paramref name="plane"/> (i.e. rotate it so it lies flat within the plane), pivoting around the point found by travelling <paramref name="signedPivotDistance"/> along this ray from <see cref="StartPoint"/>, and keeping its <see cref="Length"/> unchanged.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this ray is already exactly orthogonal to <paramref name="plane"/>); the parallelized result otherwise.</returns>
	public BoundedRay? ParallelizedWith(Plane plane, float signedPivotDistance) {
		var newDir = Direction.ParallelizedWith(plane);
		if (newDir == null) return null;
		return RotatedBy(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedAroundStartWith(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray is not already exactly orthogonal to <paramref name="plane"/>, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when either condition is broken.
	/// </remarks>
	/// <param name="plane">The target plane.</param>
	public BoundedRay FastParallelizedAroundStartWith(Plane plane) => new(StartPoint, StartToEndVect.FastParallelizedWith(plane));
	/// <inheritdoc cref="FastParallelizedAroundStartWith(Plane)" />
	public BoundedRay FastParallelizedAroundMiddleWith(Plane plane) => RotatedAroundMiddleBy(Direction >> Direction.FastParallelizedWith(plane));
	/// <inheritdoc cref="FastParallelizedAroundStartWith(Plane)" />
	public BoundedRay FastParallelizedAroundEndWith(Plane plane) => new(EndPoint - StartToEndVect.FastParallelizedWith(plane), EndPoint);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(Plane,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray is not already exactly orthogonal to <paramref name="plane"/>, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when either condition is broken.
	/// </remarks>
	/// <param name="plane">The target plane.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay FastParallelizedWith(Plane plane, float signedPivotDistance) => RotatedBy(Direction >> Direction.FastParallelizedWith(plane), UnboundedLocationAtDistance(signedPivotDistance));

	/// <inheritdoc/>
	public BoundedRay? OrthogonalizedAgainst(Plane plane) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(plane);
		if (newVect == null) return null;
		return new(StartPoint, newVect.Value);
	}
	/// <inheritdoc/>
	public BoundedRay FastOrthogonalizedAgainst(Plane plane) => new(StartPoint, StartToEndVect.FastOrthogonalizedAgainst(plane));

	/// <summary>
	/// Attempts to orthogonalize this ray against <paramref name="plane"/> (i.e. rotate it so it points directly along <see cref="Plane.Normal"/>), pivoting around its own <see cref="StartPoint"/> (which therefore stays fixed) and keeping its <see cref="Length"/> unchanged.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this ray is already exactly parallel to <paramref name="plane"/>); the orthogonalized result otherwise.</returns>
	public BoundedRay? OrthogonalizedAroundStartAgainst(Plane plane) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(plane);
		if (newVect == null) return null;
		return new BoundedRay(StartPoint, newVect.Value);
	}
	/// <inheritdoc cref="OrthogonalizedAroundStartAgainst(Plane)" />
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Plane plane) {
		var newDir = Direction.OrthogonalizedAgainst(plane);
		if (newDir == null) return null;
		return RotatedAroundMiddleBy(Direction >> newDir.Value);
	}
	/// <inheritdoc cref="OrthogonalizedAroundStartAgainst(Plane)" />
	public BoundedRay? OrthogonalizedAroundEndAgainst(Plane plane) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(plane);
		if (newVect == null) return null;
		return new BoundedRay(EndPoint - newVect.Value, EndPoint);
	}
	/// <summary>
	/// Attempts to orthogonalize this ray against <paramref name="plane"/> (i.e. rotate it so it points directly along <see cref="Plane.Normal"/>), pivoting around the point found by travelling <paramref name="signedPivotDistance"/> along this ray from <see cref="StartPoint"/>, and keeping its <see cref="Length"/> unchanged.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this ray is already exactly parallel to <paramref name="plane"/>); the orthogonalized result otherwise.</returns>
	public BoundedRay? OrthogonalizedAgainst(Plane plane, float signedPivotDistance) {
		var newDir = Direction.OrthogonalizedAgainst(plane);
		if (newDir == null) return null;
		return RotatedBy(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAroundStartAgainst(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray is not already exactly parallel to <paramref name="plane"/>, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when either condition is broken.
	/// </remarks>
	/// <param name="plane">The target plane.</param>
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Plane plane) => new(StartPoint, StartToEndVect.FastOrthogonalizedAgainst(plane));
	/// <inheritdoc cref="FastOrthogonalizedAroundStartAgainst(Plane)" />
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Plane plane) => RotatedAroundMiddleBy(Direction >> Direction.FastOrthogonalizedAgainst(plane));
	/// <inheritdoc cref="FastOrthogonalizedAroundStartAgainst(Plane)" />
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Plane plane) => new(EndPoint - StartToEndVect.FastOrthogonalizedAgainst(plane), EndPoint);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(Plane,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray is not already exactly parallel to <paramref name="plane"/>, and that <see cref="StartToEndVect"/> is not zero-length. The returned value of this function is undefined when either condition is broken.
	/// </remarks>
	/// <param name="plane">The target plane.</param>
	/// <param name="signedPivotDistance">The distance along this ray, from <see cref="StartPoint"/>, of the point to pivot around. Can be outside the bounds of this ray.</param>
	public BoundedRay FastOrthogonalizedAgainst(Plane plane, float signedPivotDistance) => RotatedBy(Direction >> Direction.FastOrthogonalizedAgainst(plane), UnboundedLocationAtDistance(signedPivotDistance));

	// Note: Projection treats this like two points (start/end), whereas parallelize/orthogonalize treat it as a start-point + vect; hence the ostensible discrepancy
	// That being said, I feel like projection vs parallelization/orthogonalization are subtly different things even if they're thought of in a similar vein; hence why I chose it this way
	/// <summary>
	/// Returns this ray projected onto <paramref name="plane"/>, i.e. with <see cref="StartPoint"/> and <see cref="EndPoint"/> each moved to their closest point on <paramref name="plane"/>.
	/// </summary>
	/// <remarks>
	/// Unlike parallelizing, this does not preserve <see cref="Length"/> — the projected ray can end up shorter than the original (or even zero-length, if this ray was exactly orthogonal to <paramref name="plane"/>).
	/// </remarks>
	/// <param name="plane">The plane to project onto.</param>
	public BoundedRay ProjectedOnTo(Plane plane) => new(StartPoint.ClosestPointOn(plane), EndPoint.ClosestPointOn(plane));
	BoundedRay? IProjectable<BoundedRay, Plane>.ProjectedOnTo(Plane plane) => ProjectedOnTo(plane);
	BoundedRay IProjectable<BoundedRay, Plane>.FastProjectedOnTo(Plane plane) => ProjectedOnTo(plane);
	#endregion

	#region Clamping and Interpolation
	/// <inheritdoc/>
	public static BoundedRay Interpolate(BoundedRay start, BoundedRay end, float distance) {
		return new(
			Location.Interpolate(start._startPoint, end._startPoint, distance),
			Vect.Interpolate(start._vect, end._vect, distance)
		);
	}
	/// <inheritdoc/>
	public BoundedRay Clamp(BoundedRay min, BoundedRay max) => new(StartPoint.Clamp(min.StartPoint, max.StartPoint), EndPoint.Clamp(min.EndPoint, max.EndPoint));
	#endregion
}