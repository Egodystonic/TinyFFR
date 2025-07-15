// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct BoundedRay : IPointTransformable<BoundedRay>, IPointScalable<BoundedRay>, ILengthAdjustable<BoundedRay> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromStart() => new(StartPoint, Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromEnd() => new(EndPoint, -Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRay(float signedDistanceAlongLine) => new(UnboundedLocationAtDistance(signedDistanceAlongLine), Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ToLine() => new(StartPoint, Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator -(BoundedRay operand) => operand.Flipped;
	public BoundedRay Flipped {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(EndPoint, StartPoint);
	}
	BoundedRay IInvertible<BoundedRay>.Inverted => Flipped;

	#region With Methods
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLength(float newLength) => new(_startPoint, _vect.WithLength(newLength));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthDecreasedBy(float lengthDecrease) => WithLength(Length - lengthDecrease);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthIncreasedBy(float lengthIncrease) => WithLength(Length + lengthIncrease);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMaxLength(float maxLength) => WithLength(MathF.Min(Length, maxLength >= 0f ? maxLength : throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be non-negative.")));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMinLength(float minLength) => WithLength(MathF.Max(Length, minLength >= 0f ? minLength : throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Must be non-negative.")));
	public BoundedRay WithLength(float newLength, float scalingOriginSignedDistance) {
		var scalar = newLength / Length;
		return Single.IsFinite(scalar) ? ScaledBy(scalar, scalingOriginSignedDistance) : this;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthDecreasedBy(float lengthDecrease, float scalingOriginSignedDistance) => WithLength(Length - lengthDecrease, scalingOriginSignedDistance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthIncreasedBy(float lengthIncrease, float scalingOriginSignedDistance) => WithLength(Length + lengthIncrease, scalingOriginSignedDistance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMaxLength(float maxLength, float scalingOriginSignedDistance) => WithLength(MathF.Min(Length, maxLength >= 0f ? maxLength : throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be non-negative.")), scalingOriginSignedDistance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMinLength(float minLength, float scalingOriginSignedDistance) => WithLength(MathF.Max(Length, minLength >= 0f ? minLength : throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Must be non-negative.")), scalingOriginSignedDistance);
	public BoundedRay WithLength(float newLength, Location scalingOrigin) {
		var scalar = newLength / Length;
		return Single.IsFinite(scalar) ? ScaledBy(scalar, scalingOrigin) : this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthDecreasedBy(float lengthDecrease, Location scalingOrigin) => WithLength(Length - lengthDecrease, scalingOrigin);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLengthIncreasedBy(float lengthIncrease, Location scalingOrigin) => WithLength(Length + lengthIncrease, scalingOrigin);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMaxLength(float maxLength, Location scalingOrigin) => WithLength(MathF.Min(Length, maxLength >= 0f ? maxLength : throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be non-negative.")), scalingOrigin);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMinLength(float minLength, Location scalingOrigin) => WithLength(MathF.Max(Length, minLength >= 0f ? minLength : throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Must be non-negative.")), scalingOrigin);
	#endregion

	#region Line-Like Methods
	public bool DistanceIsWithinLineBounds(float signedDistanceFromStart) => signedDistanceFromStart >= 0f && signedDistanceFromStart * signedDistanceFromStart <= LengthSquared;
	public float BindDistance(float signedDistanceFromStart) => Single.Clamp(signedDistanceFromStart, 0f, Length);
	public Location BoundedLocationAtDistance(float signedDistanceFromStart) => UnboundedLocationAtDistance(BindDistance(signedDistanceFromStart));
	public Location UnboundedLocationAtDistance(float signedDistanceFromStart) => _startPoint + _vect.WithLength(signedDistanceFromStart);
	public Location? LocationAtDistanceOrNull(float signedDistanceFromStart) => DistanceIsWithinLineBounds(signedDistanceFromStart) ? UnboundedLocationAtDistance(signedDistanceFromStart) : null;
	public float UnboundedDistanceAtPointClosestTo(Location point) => ToLine().DistanceAtPointClosestTo(point);
	public float BoundedDistanceAtPointClosestTo(Location point) => PointClosestTo(point).DistanceFrom(StartPoint);
	#endregion

	#region Translation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator +(BoundedRay ray, Vect v) => ray.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator +(Vect v, BoundedRay ray) => ray.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator -(BoundedRay ray, Vect v) => ray.MovedBy(-v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay MovedBy(Vect v) => new(_startPoint + v, _vect);
	#endregion

	#region Rotation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(BoundedRay ray, Rotation rot) => ray.RotatedAroundStartBy(rot); // We choose AroundStart as the "default" rotation because it keeps thing consistent with Ray
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(Rotation rot, BoundedRay ray) => ray.RotatedAroundStartBy(rot); // We choose AroundStart as the "default" rotation because it keeps thing consistent with Ray
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(BoundedRay ray, (Rotation Rotation, Location Pivot) rotPivotTuple) => ray.RotatedBy(rotPivotTuple.Rotation, rotPivotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(BoundedRay ray, (Location Pivot, Rotation Rotation) rotPivotTuple) => ray.RotatedBy(rotPivotTuple.Rotation, rotPivotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *((Rotation Rotation, Location Pivot) rotPivotTuple, BoundedRay ray) => ray.RotatedBy(rotPivotTuple.Rotation, rotPivotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *((Location Pivot, Rotation Rotation) rotPivotTuple, BoundedRay ray) => ray.RotatedBy(rotPivotTuple.Rotation, rotPivotTuple.Pivot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay RotatedAroundStartBy(Rotation rotation) => new(_startPoint, _vect * rotation);
	public BoundedRay RotatedAroundEndBy(Rotation rotation) {
		var endPoint = _startPoint + _vect;
		return new(endPoint + _vect.Reversed * rotation, endPoint);
	}
	public BoundedRay RotatedAroundMiddleBy(Rotation rotation) {
		var newVect = _vect * rotation;
		var newStartPoint = _startPoint + ((_vect * 0.5f) - (newVect * 0.5f));
		return new(newStartPoint, newVect);
	}

	public BoundedRay RotatedAroundOriginBy(Rotation rot) {
		return new BoundedRay(
			StartPoint.AsVect().RotatedBy(rot).AsLocation(),
			EndPoint.AsVect().RotatedBy(rot).AsLocation()
		);
	}
	BoundedRay IRotatable<BoundedRay>.RotatedBy(Rotation rot) => RotatedAroundStartBy(rot); // We choose AroundStart as the "default" rotation because it keeps thing consistent with Ray
	public BoundedRay RotatedBy(Rotation rotation, float signedPivotDistance) => RotatedBy(rotation, UnboundedLocationAtDistance(signedPivotDistance));
	public BoundedRay RotatedBy(Rotation rotation, Location pivot) {
		return new(pivot + (pivot >> StartPoint) * rotation, pivot + (pivot >> EndPoint) * rotation);
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay ScaledFromStartBy(float scalar) => new(_startPoint, _vect.ScaledBy(scalar));
	public BoundedRay ScaledFromMiddleBy(float scalar) {
		var halfVect = _vect * 0.5f;
		var midPoint = _startPoint + halfVect;
		var scaledVect = _vect.ScaledBy(scalar);
		var newStart = midPoint - halfVect.ScaledBy(scalar);
		return new BoundedRay(newStart, newStart + scaledVect);
	}
	public BoundedRay ScaledFromEndBy(float scalar) {
		var scaledVect = _vect.ScaledBy(scalar);
		var newStart = (_startPoint + _vect) - scaledVect;
		return new BoundedRay(newStart, scaledVect);
	}
	public BoundedRay ScaledFromOriginBy(float scalar) {
		return new BoundedRay(
			StartPoint.AsVect().ScaledBy(scalar).AsLocation(),
			EndPoint.AsVect().ScaledBy(scalar).AsLocation()
		);
	}
	public BoundedRay ScaledBy(float scalar, float scalingOriginSignedDistance) {
		var pivotPoint = UnboundedLocationAtDistance(scalingOriginSignedDistance);
		var pivotToStartVect = -_vect.WithLength(scalingOriginSignedDistance);
		var pivotToEndVect = _vect.WithLength(_vect.Length - scalingOriginSignedDistance);
		return new BoundedRay(pivotPoint + pivotToStartVect * scalar, pivotPoint + pivotToEndVect * scalar);
	}
	public BoundedRay ScaledBy(float scalar, Location scalingOrigin) => ScaledBy(scalar, UnboundedDistanceAtPointClosestTo(scalingOrigin));

	BoundedRay IIndependentAxisScalable<BoundedRay>.ScaledBy(Vect vect) => ScaledFromMiddleBy(vect);
	public BoundedRay ScaledFromStartBy(Vect vect) => new(_startPoint, _vect.ScaledBy(vect));
	public BoundedRay ScaledFromMiddleBy(Vect vect) {
		var halfVect = _vect * 0.5f;
		var midPoint = _startPoint + halfVect;
		var scaledVect = _vect.ScaledBy(vect);
		var newStart = midPoint - halfVect.ScaledBy(vect);
		return new BoundedRay(newStart, newStart + scaledVect);
	}
	public BoundedRay ScaledFromEndBy(Vect vect) {
		var scaledVect = _vect.ScaledBy(vect);
		var newStart = (_startPoint + _vect) - scaledVect;
		return new BoundedRay(newStart, scaledVect);
	}
	public BoundedRay ScaledFromOriginBy(Vect vect) {
		return new BoundedRay(
			StartPoint.AsVect().ScaledBy(vect).AsLocation(),
			EndPoint.AsVect().ScaledBy(vect).AsLocation()
		);
	}
	public BoundedRay ScaledBy(Vect vect, float scalingOriginSignedDistance) {
		var pivotPoint = UnboundedLocationAtDistance(scalingOriginSignedDistance);
		var pivotToStartVect = -_vect.WithLength(scalingOriginSignedDistance);
		var pivotToEndVect = _vect.WithLength(_vect.Length - scalingOriginSignedDistance);
		return new BoundedRay(pivotPoint + pivotToStartVect * vect, pivotPoint + pivotToEndVect * vect);
	}
	public BoundedRay ScaledBy(Vect vect, Location scalingOrigin) => ScaledBy(vect, UnboundedDistanceAtPointClosestTo(scalingOrigin));
	#endregion

	#region Transformation
	public static BoundedRay operator *(BoundedRay ray, Transform transform) => ray.TransformedAroundStartBy(transform);
	public static BoundedRay operator *(Transform transform, BoundedRay ray) => ray.TransformedAroundStartBy(transform);
	BoundedRay ITransformable<BoundedRay>.TransformedBy(Transform transform) => TransformedAroundStartBy(transform);
	public BoundedRay TransformedAroundStartBy(Transform transform) => TransformedBy(transform, StartPoint);
	public BoundedRay TransformedAroundMiddleBy(Transform transform) => TransformedBy(transform, MiddlePoint);
	public BoundedRay TransformedAroundEndBy(Transform transform) => TransformedBy(transform, EndPoint);
	public BoundedRay TransformedAroundOriginBy(Transform transform) => ScaledFromOriginBy(transform.Scaling).RotatedAroundOriginBy(transform.Rotation).MovedBy(transform.Translation);
	public BoundedRay TransformedBy(Transform transform, float transformationOriginSignedDistance) => TransformedBy(transform, UnboundedLocationAtDistance(transformationOriginSignedDistance));
	public BoundedRay TransformedBy(Transform transform, Location transformationOrigin) => ScaledBy(transform.Scaling, transformationOrigin).RotatedBy(transform.Rotation, transformationOrigin).MovedBy(transform.Translation);
	#endregion

	#region Distance / Closest Point / Containment
	public Location PointClosestTo(Location location) {
		var vectCoefficient = Vector3.Dot((location - _startPoint).ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => Single.IsFinite(vectCoefficient) ? _startPoint + _vect * vectCoefficient : _startPoint
		};
	}
	public Location PointClosestToOrigin() {
		var vectCoefficient = -Vector3.Dot(_startPoint.ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => Single.IsFinite(vectCoefficient) ? _startPoint + _vect * vectCoefficient : _startPoint
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location location) => location.DistanceFrom(PointClosestTo(location));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Location location) => location.DistanceSquaredFrom(PointClosestTo(location));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromOrigin() => ((Vect) PointClosestToOrigin()).Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromOrigin() => ((Vect) PointClosestToOrigin()).LengthSquared;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;

	public Location PointClosestTo(Line line) {
		var intersectionDistance = ILineLike.CalculateUnboundedIntersectionDistanceOnThisLine(this, line);
		return intersectionDistance != null ? BoundedLocationAtDistance(intersectionDistance.Value) : StartPoint;
	}
	public Location PointClosestTo(Ray ray) {
		var intersectionDistances = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(this, ray);
		if (intersectionDistances == null || !ray.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return PointClosestTo(ray.StartPoint);
		else return BoundedLocationAtDistance(intersectionDistances.Value.ThisDistance);
	}
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

	public Location? IntersectionWith(Plane plane) {
		var distance = GetUnboundedPlaneIntersectionDistance(plane);
		return distance >= 0f && distance <= Length ? UnboundedLocationAtDistance(distance.Value) : null; // Null means Plane parallel with line or outside line boundaries
	}
	public Location FastIntersectionWith(Plane plane) => UnboundedLocationAtDistance((plane.PointClosestToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / plane.Normal.Dot(Direction));

	public bool IsIntersectedBy(Plane plane) {
		var unboundedIntersectionDistance = GetUnboundedPlaneIntersectionDistance(plane);
		return unboundedIntersectionDistance >= 0f && unboundedIntersectionDistance <= Length;
	}

	public Pair<BoundedRay, BoundedRay> FastSplitBy(Plane plane) {
		var intersectionPoint = FastIntersectionWith(plane);
		return new(new(StartPoint, intersectionPoint), new(intersectionPoint, EndPoint));
	}
	public Pair<BoundedRay, BoundedRay>? SplitBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		return intersectionPoint == null ? null : new(new(StartPoint, intersectionPoint.Value), new(intersectionPoint.Value, EndPoint));
	}

	public Angle? IncidentAngleWith(Plane plane) => IsIntersectedBy(plane) ? plane.IncidentAngleWith(Direction) : null;
	public Angle FastIncidentAngleWith(Plane plane) => plane.FastIncidentAngleWith(Direction);

	public BoundedRay? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new BoundedRay(intersectionPoint.Value, Direction.FastReflectedBy(plane) * (Length - intersectionPoint.Value.DistanceFrom(StartPoint)));
	}
	public BoundedRay FastReflectedBy(Plane plane) {
		var intersectionPoint = FastIntersectionWith(plane);
		return new BoundedRay(intersectionPoint, Direction.FastReflectedBy(plane) * (Length - intersectionPoint.DistanceFrom(StartPoint)));
	}

	public Location PointClosestTo(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		return BoundedLocationAtDistance(unboundedDistance ?? 0f); // If unboundedDistance is null we're parallel so the StartPoint is as close as any other point
	}
	public Location ClosestPointOn(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		var closestPointOnLine = BoundedLocationAtDistance(unboundedDistance ?? 0f);
		if (unboundedDistance >= 0f && unboundedDistance <= Length) return closestPointOnLine; // Actual intersection
		else return plane.PointClosestTo(closestPointOnLine);
	}

	public float SignedDistanceFrom(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane) ?? 0f;

		if (unboundedDistance <= 0f) return plane.SignedDistanceFrom(StartPoint);
		else if (unboundedDistance > Length) return plane.SignedDistanceFrom(EndPoint);
		else return plane.SignedDistanceFrom(UnboundedLocationAtDistance(unboundedDistance));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => MathF.Abs(SignedDistanceFrom(plane));

	public PlaneObjectRelationship RelationshipTo(Plane plane) {
		return SignedDistanceFrom(plane) switch {
			> 0f => PlaneObjectRelationship.PlaneFacesTowardsObject,
			< 0f => PlaneObjectRelationship.PlaneFacesAwayFromObject,
			_ => PlaneObjectRelationship.PlaneIntersectsObject
		};
	}
	#endregion

	#region Parallelization / Orthogonalization / Projection
	// TODO xmldoc for all Fast... variants here make it clear that as well direction not being None or ortho/parallel etc, StartToEndVect can not be zero
	public BoundedRay? ParallelizedWith(Direction direction) => ParallelizedAroundStartWith(direction);
	public BoundedRay FastParallelizedWith(Direction direction) => FastParallelizedAroundStartWith(direction);

	public BoundedRay? ParallelizedAroundStartWith(Direction direction) {
		var newVect = StartToEndVect.ParallelizedWith(direction);
		return newVect == null ? null : new(StartPoint, newVect.Value);
	}
	public BoundedRay? ParallelizedAroundMiddleWith(Direction direction) {
		var newDir = Direction.ParallelizedWith(direction);
		if (newDir == null) return null;
		return RotatedAroundMiddleBy(Direction >> newDir.Value);
	}
	public BoundedRay? ParallelizedAroundEndWith(Direction direction) {
		var newVect = StartToEndVect.ParallelizedWith(direction);
		return newVect == null ? null : new(EndPoint - newVect.Value, newVect.Value);
	}
	public BoundedRay? ParallelizedWith(Direction direction, float signedPivotDistance) {
		var newDir = Direction.ParallelizedWith(direction);
		if (newDir == null) return null;
		return RotatedBy(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	public BoundedRay FastParallelizedAroundStartWith(Direction direction) => new(StartPoint, StartToEndVect.FastParallelizedWith(direction));
	public BoundedRay FastParallelizedAroundMiddleWith(Direction direction) => RotatedAroundMiddleBy(Direction >> Direction.FastParallelizedWith(direction));
	public BoundedRay FastParallelizedAroundEndWith(Direction direction) {
		var newVect = StartToEndVect.FastParallelizedWith(direction);
		return new(EndPoint - newVect, newVect);
	}
	public BoundedRay FastParallelizedWith(Direction direction, float signedPivotDistance) => RotatedBy(Direction >> Direction.FastParallelizedWith(direction), UnboundedLocationAtDistance(signedPivotDistance));

	public BoundedRay? OrthogonalizedAgainst(Direction direction) => OrthogonalizedAroundStartAgainst(direction);
	public BoundedRay FastOrthogonalizedAgainst(Direction direction) => FastOrthogonalizedAroundStartAgainst(direction);

	public BoundedRay? OrthogonalizedAroundStartAgainst(Direction direction) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(direction);
		return newVect == null ? null : new(StartPoint, newVect.Value);
	}
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Direction direction) {
		var newDir = Direction.OrthogonalizedAgainst(direction);
		if (newDir == null) return null;
		return RotatedAroundMiddleBy(Direction >> newDir.Value);
	}
	public BoundedRay? OrthogonalizedAroundEndAgainst(Direction direction) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(direction);
		return newVect == null ? null : new(EndPoint - newVect.Value, newVect.Value);
	}
	public BoundedRay? OrthogonalizedAgainst(Direction direction, float signedPivotDistance) {
		var newDir = Direction.OrthogonalizedAgainst(direction);
		if (newDir == null) return null;
		return RotatedBy(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Direction direction) => new(StartPoint, StartToEndVect.FastOrthogonalizedAgainst(direction));
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Direction direction) => RotatedAroundMiddleBy(Direction >> Direction.FastOrthogonalizedAgainst(direction));
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Direction direction) {
		var newVect = StartToEndVect.FastOrthogonalizedAgainst(direction);
		return new(EndPoint - newVect, newVect);
	}
	public BoundedRay FastOrthogonalizedAgainst(Direction direction, float signedPivotDistance) => RotatedBy(Direction >> Direction.FastOrthogonalizedAgainst(direction), UnboundedLocationAtDistance(signedPivotDistance));


	public BoundedRay? ParallelizedAroundStartWith(Line line) => ParallelizedAroundStartWith(line.Direction);
	public BoundedRay? ParallelizedAroundMiddleWith(Line line) => ParallelizedAroundMiddleWith(line.Direction);
	public BoundedRay? ParallelizedAroundEndWith(Line line) => ParallelizedAroundEndWith(line.Direction);
	public BoundedRay? ParallelizedWith(Line line, float signedPivotDistance) => ParallelizedWith(line.Direction, signedPivotDistance);
	public BoundedRay FastParallelizedAroundStartWith(Line line) => FastParallelizedAroundStartWith(line.Direction);
	public BoundedRay FastParallelizedAroundMiddleWith(Line line) => FastParallelizedAroundMiddleWith(line.Direction);
	public BoundedRay FastParallelizedAroundEndWith(Line line) => FastParallelizedAroundEndWith(line.Direction);
	public BoundedRay FastParallelizedWith(Line line, float signedPivotDistance) => FastParallelizedWith(line.Direction, signedPivotDistance);
	public BoundedRay? ParallelizedAroundStartWith(Ray ray) => ParallelizedAroundStartWith(ray.Direction);
	public BoundedRay? ParallelizedAroundMiddleWith(Ray ray) => ParallelizedAroundMiddleWith(ray.Direction);
	public BoundedRay? ParallelizedAroundEndWith(Ray ray) => ParallelizedAroundEndWith(ray.Direction);
	public BoundedRay? ParallelizedWith(Ray ray, float signedPivotDistance) => ParallelizedWith(ray.Direction, signedPivotDistance);
	public BoundedRay FastParallelizedAroundStartWith(Ray ray) => FastParallelizedAroundStartWith(ray.Direction);
	public BoundedRay FastParallelizedAroundMiddleWith(Ray ray) => FastParallelizedAroundMiddleWith(ray.Direction);
	public BoundedRay FastParallelizedAroundEndWith(Ray ray) => FastParallelizedAroundEndWith(ray.Direction);
	public BoundedRay FastParallelizedWith(Ray ray, float signedPivotDistance) => FastParallelizedWith(ray.Direction, signedPivotDistance);
	public BoundedRay? ParallelizedAroundStartWith(BoundedRay ray) => ParallelizedAroundStartWith(ray.Direction);
	public BoundedRay? ParallelizedAroundMiddleWith(BoundedRay ray) => ParallelizedAroundMiddleWith(ray.Direction);
	public BoundedRay? ParallelizedAroundEndWith(BoundedRay ray) => ParallelizedAroundEndWith(ray.Direction);
	public BoundedRay? ParallelizedWith(BoundedRay ray, float signedPivotDistance) => ParallelizedWith(ray.Direction, signedPivotDistance);
	public BoundedRay FastParallelizedAroundStartWith(BoundedRay ray) => FastParallelizedAroundStartWith(ray.Direction);
	public BoundedRay FastParallelizedAroundMiddleWith(BoundedRay ray) => FastParallelizedAroundMiddleWith(ray.Direction);
	public BoundedRay FastParallelizedAroundEndWith(BoundedRay ray) => FastParallelizedAroundEndWith(ray.Direction);
	public BoundedRay FastParallelizedWith(BoundedRay ray, float signedPivotDistance) => FastParallelizedWith(ray.Direction, signedPivotDistance);

	public BoundedRay? OrthogonalizedAroundStartAgainst(Line line) => OrthogonalizedAroundStartAgainst(line.Direction);
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Line line) => OrthogonalizedAroundMiddleAgainst(line.Direction);
	public BoundedRay? OrthogonalizedAroundEndAgainst(Line line) => OrthogonalizedAroundEndAgainst(line.Direction);
	public BoundedRay? OrthogonalizedAgainst(Line line, float signedPivotDistance) => OrthogonalizedAgainst(line.Direction, signedPivotDistance);
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Line line) => FastOrthogonalizedAroundStartAgainst(line.Direction);
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Line line) => FastOrthogonalizedAroundMiddleAgainst(line.Direction);
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Line line) => FastOrthogonalizedAroundEndAgainst(line.Direction);
	public BoundedRay FastOrthogonalizedAgainst(Line line, float signedPivotDistance) => FastOrthogonalizedAgainst(line.Direction, signedPivotDistance);
	public BoundedRay? OrthogonalizedAroundStartAgainst(Ray ray) => OrthogonalizedAroundStartAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Ray ray) => OrthogonalizedAroundMiddleAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAroundEndAgainst(Ray ray) => OrthogonalizedAroundEndAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAgainst(Ray ray, float signedPivotDistance) => OrthogonalizedAgainst(ray.Direction, signedPivotDistance);
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Ray ray) => FastOrthogonalizedAroundStartAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Ray ray) => FastOrthogonalizedAroundMiddleAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Ray ray) => FastOrthogonalizedAroundEndAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAgainst(Ray ray, float signedPivotDistance) => FastOrthogonalizedAgainst(ray.Direction, signedPivotDistance);
	public BoundedRay? OrthogonalizedAroundStartAgainst(BoundedRay ray) => OrthogonalizedAroundStartAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(BoundedRay ray) => OrthogonalizedAroundMiddleAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAroundEndAgainst(BoundedRay ray) => OrthogonalizedAroundEndAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAgainst(BoundedRay ray, float signedPivotDistance) => OrthogonalizedAgainst(ray.Direction, signedPivotDistance);
	public BoundedRay FastOrthogonalizedAroundStartAgainst(BoundedRay ray) => FastOrthogonalizedAroundStartAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(BoundedRay ray) => FastOrthogonalizedAroundMiddleAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAroundEndAgainst(BoundedRay ray) => FastOrthogonalizedAroundEndAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAgainst(BoundedRay ray, float signedPivotDistance) => FastOrthogonalizedAgainst(ray.Direction, signedPivotDistance);


	// TODO in xmldoc note that these preserve length or returns null if orthogonal/parallel to the plane
	public BoundedRay? ParallelizedWith(Plane plane) => ParallelizedAroundStartWith(plane);
	public BoundedRay FastParallelizedWith(Plane plane) => FastParallelizedAroundStartWith(plane);

	public BoundedRay? ParallelizedAroundStartWith(Plane plane) {
		var newVect = StartToEndVect.ParallelizedWith(plane);
		if (newVect == null) return null;
		return new BoundedRay(StartPoint, newVect.Value);
	}
	public BoundedRay? ParallelizedAroundMiddleWith(Plane plane) {
		var newDir = Direction.ParallelizedWith(plane);
		if (newDir == null) return null;
		return RotatedAroundMiddleBy(Direction >> newDir.Value);
	}
	public BoundedRay? ParallelizedAroundEndWith(Plane plane) {
		var newVect = StartToEndVect.ParallelizedWith(plane);
		if (newVect == null) return null;
		return new BoundedRay(EndPoint - newVect.Value, EndPoint);
	}
	public BoundedRay? ParallelizedWith(Plane plane, float signedPivotDistance) {
		var newDir = Direction.ParallelizedWith(plane);
		if (newDir == null) return null;
		return RotatedBy(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	public BoundedRay FastParallelizedAroundStartWith(Plane plane) => new(StartPoint, StartToEndVect.FastParallelizedWith(plane));
	public BoundedRay FastParallelizedAroundMiddleWith(Plane plane) => RotatedAroundMiddleBy(Direction >> Direction.FastParallelizedWith(plane));
	public BoundedRay FastParallelizedAroundEndWith(Plane plane) => new(EndPoint - StartToEndVect.FastParallelizedWith(plane), EndPoint);
	public BoundedRay FastParallelizedWith(Plane plane, float signedPivotDistance) => RotatedBy(Direction >> Direction.FastParallelizedWith(plane), UnboundedLocationAtDistance(signedPivotDistance));

	public BoundedRay? OrthogonalizedAgainst(Plane plane) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(plane);
		if (newVect == null) return null;
		return new(StartPoint, newVect.Value);
	}
	public BoundedRay FastOrthogonalizedAgainst(Plane plane) => new(StartPoint, StartToEndVect.FastOrthogonalizedAgainst(plane));

	public BoundedRay? OrthogonalizedAroundStartAgainst(Plane plane) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(plane);
		if (newVect == null) return null;
		return new BoundedRay(StartPoint, newVect.Value);
	}
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Plane plane) {
		var newDir = Direction.OrthogonalizedAgainst(plane);
		if (newDir == null) return null;
		return RotatedAroundMiddleBy(Direction >> newDir.Value);
	}
	public BoundedRay? OrthogonalizedAroundEndAgainst(Plane plane) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(plane);
		if (newVect == null) return null;
		return new BoundedRay(EndPoint - newVect.Value, EndPoint);
	}
	public BoundedRay? OrthogonalizedAgainst(Plane plane, float signedPivotDistance) {
		var newDir = Direction.OrthogonalizedAgainst(plane);
		if (newDir == null) return null;
		return RotatedBy(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Plane plane) => new(StartPoint, StartToEndVect.FastOrthogonalizedAgainst(plane));
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Plane plane) => RotatedAroundMiddleBy(Direction >> Direction.FastOrthogonalizedAgainst(plane));
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Plane plane) => new(EndPoint - StartToEndVect.FastOrthogonalizedAgainst(plane), EndPoint);
	public BoundedRay FastOrthogonalizedAgainst(Plane plane, float signedPivotDistance) => RotatedBy(Direction >> Direction.FastOrthogonalizedAgainst(plane), UnboundedLocationAtDistance(signedPivotDistance));

	// Note: Projection treats this like two points (start/end), whereas parallelize/orthogonalize treat it as a start-point + vect; hence the ostensible discrepancy
	// That being said, I feel like projection vs parallelization/orthogonalization are subtly different things even if they're thought of in a similar vein; hence why I chose it this way
	public BoundedRay ProjectedOnTo(Plane plane) => new(StartPoint.ClosestPointOn(plane), EndPoint.ClosestPointOn(plane)); // TODO in xmldoc note that this does not preserve length, returns a zero-length ray if orthogonal to plane
	BoundedRay? IProjectable<BoundedRay, Plane>.ProjectedOnTo(Plane plane) => ProjectedOnTo(plane);
	BoundedRay IProjectable<BoundedRay, Plane>.FastProjectedOnTo(Plane plane) => ProjectedOnTo(plane);
	#endregion

	#region Clamping and Interpolation
	public static BoundedRay Interpolate(BoundedRay start, BoundedRay end, float distance) {
		return new(
			Location.Interpolate(start._startPoint, end._startPoint, distance),
			Vect.Interpolate(start._vect, end._vect, distance)
		);
	}
	public BoundedRay Clamp(BoundedRay min, BoundedRay max) => new(StartPoint.Clamp(min.StartPoint, max.StartPoint), EndPoint.Clamp(min.EndPoint, max.EndPoint));
	#endregion
}