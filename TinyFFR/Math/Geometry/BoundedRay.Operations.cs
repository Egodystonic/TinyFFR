// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

partial struct BoundedRay : IScalable<BoundedRay>, ILengthAdjustable<BoundedRay> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromStart() => new(StartPoint, Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromEnd() => new(EndPoint, -Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ToLine() => new(StartPoint, Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator -(BoundedRay operand) => operand.Flipped;
	public BoundedRay Flipped {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(EndPoint, StartPoint);
	}
	BoundedRay IInvertible<BoundedRay>.Inverted => Flipped;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(BoundedRay ray, float scalar) => ray.ScaledFromMiddleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(float scalar, BoundedRay ray) => ray.ScaledFromMiddleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator /(BoundedRay ray, float scalar) => ray.ScaledFromMiddleBy(1f / scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	BoundedRay IScalable<BoundedRay>.ScaledBy(float scalar) => ScaledFromMiddleBy(scalar);
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
	public BoundedRay ScaledAroundPivotDistanceBy(float scalar, float signedPivotDistance) {
		var pivotPoint = UnboundedLocationAtDistance(signedPivotDistance);
		var pivotToStartVect = -_vect.WithLength(signedPivotDistance);
		var pivotToEndVect = _vect.WithLength(_vect.Length - signedPivotDistance);
		return new BoundedRay(pivotPoint + pivotToStartVect * scalar, pivotPoint + pivotToEndVect * scalar);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLength(float newLength) => new(_startPoint, _vect.WithLength(newLength));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay ShortenedBy(float lengthDecrease) => WithLength(Length - lengthDecrease);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay LengthenedBy(float lengthIncrease) => WithLength(Length + lengthIncrease);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMaxLength(float maxLength) => WithLength(MathF.Min(Length, maxLength));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMinLength(float minLength) => WithLength(MathF.Max(Length, minLength));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithLength(float newLength, float signedPivotDistance) => ScaledAroundPivotDistanceBy(newLength / Length, signedPivotDistance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay ShortenedBy(float lengthDecrease, float signedPivotDistance) => WithLength(Length - lengthDecrease, signedPivotDistance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay LengthenedBy(float lengthIncrease, float signedPivotDistance) => WithLength(Length + lengthIncrease, signedPivotDistance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMaxLength(float maxLength, float signedPivotDistance) => WithLength(MathF.Min(Length, maxLength), signedPivotDistance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay WithMinLength(float minLength, float signedPivotDistance) => WithLength(MathF.Max(Length, minLength), signedPivotDistance);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(BoundedRay ray, Rotation rot) => ray.RotatedAroundStartBy(rot); // We choose AroundStart as the "default" rotation because it keeps thing consistent with Ray
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(Rotation rot, BoundedRay ray) => ray.RotatedAroundStartBy(rot); // We choose AroundStart as the "default" rotation because it keeps thing consistent with Ray
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(BoundedRay ray, (Rotation Rotation, Location Pivot) rotPivotTuple) => ray.RotatedAroundPoint(rotPivotTuple.Rotation, rotPivotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *(BoundedRay ray, (Location Pivot, Rotation Rotation) rotPivotTuple) => ray.RotatedAroundPoint(rotPivotTuple.Rotation, rotPivotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *((Rotation Rotation, Location Pivot) rotPivotTuple, BoundedRay ray) => ray.RotatedAroundPoint(rotPivotTuple.Rotation, rotPivotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator *((Location Pivot, Rotation Rotation) rotPivotTuple, BoundedRay ray) => ray.RotatedAroundPoint(rotPivotTuple.Rotation, rotPivotTuple.Pivot);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay RotatedAroundStartBy(Rotation rotation) => new(_startPoint, _vect * rotation);
	public BoundedRay RotatedAroundEndBy(Rotation rotation) {
		var endPoint = _startPoint + _vect;
		return new(endPoint + _vect.Inverted * rotation, endPoint);
	}
	public BoundedRay RotatedAroundMiddleBy(Rotation rotation) {
		var newVect = _vect * rotation;
		var newStartPoint = _startPoint + ((_vect * 0.5f) - (newVect * 0.5f));
		return new(newStartPoint, newVect);
	}
	BoundedRay IRotatable<BoundedRay>.RotatedBy(Rotation rot) => RotatedAroundStartBy(rot); // We choose AroundStart as the "default" rotation because it keeps thing consistent with Ray
	public BoundedRay RotatedAroundPoint(Rotation rotation, Location pivot) {
		return new(pivot + (pivot >> StartPoint) * rotation, pivot + (pivot >> EndPoint) * rotation);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator +(BoundedRay ray, Vect v) => ray.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator +(Vect v, BoundedRay ray) => ray.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedRay operator -(BoundedRay ray, Vect v) => ray.MovedBy(-v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay MovedBy(Vect v) => new(_startPoint + v, _vect);


	public static BoundedRay Interpolate(BoundedRay start, BoundedRay end, float distance) {
		return new(
			Location.Interpolate(start._startPoint, end._startPoint, distance),
			Vect.Interpolate(start._vect, end._vect, distance)
		);
	}
	public BoundedRay Clamp(BoundedRay min, BoundedRay max) => new(StartPoint.Clamp(min.StartPoint, max.StartPoint), EndPoint.Clamp(min.EndPoint, max.EndPoint));
	public static BoundedRay CreateNewRandom() => new(Location.CreateNewRandom(), Location.CreateNewRandom());
	public static BoundedRay CreateNewRandom(BoundedRay minInclusive, BoundedRay maxExclusive) => new(Location.CreateNewRandom(minInclusive.StartPoint, maxExclusive.StartPoint), Location.CreateNewRandom(minInclusive.EndPoint, maxExclusive.EndPoint));

	public Location PointClosestTo(Location location) {
		var vectCoefficient = Vector3.Dot((location - _startPoint).ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => _startPoint + _vect * vectCoefficient
		};
	}
	public Location PointClosestToOrigin() {
		var vectCoefficient = -Vector3.Dot(_startPoint.ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => _startPoint + _vect * vectCoefficient
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

	public bool DistanceIsWithinLineBounds(float signedDistanceFromStart) => signedDistanceFromStart >= 0f && signedDistanceFromStart * signedDistanceFromStart <= LengthSquared;
	public float BindDistance(float signedDistanceFromStart) => Single.Clamp(signedDistanceFromStart, 0f, Length);
	public Location BoundedLocationAtDistance(float signedDistanceFromStart) => UnboundedLocationAtDistance(BindDistance(signedDistanceFromStart));
	public Location UnboundedLocationAtDistance(float signedDistanceFromStart) => _startPoint + _vect.WithLength(signedDistanceFromStart);
	public Location? LocationAtDistanceOrNull(float signedDistanceFromStart) => DistanceIsWithinLineBounds(signedDistanceFromStart) ? UnboundedLocationAtDistance(signedDistanceFromStart) : null;
	public float UnboundedDistanceAtPointClosestTo(Location point) => ToLine().DistanceAtPointClosestTo(point);
	public float BoundedDistanceAtPointClosestTo(Location point) => PointClosestTo(point).DistanceFrom(StartPoint);

	public BoundedRay? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane)?.StartPoint;
		if (intersectionPoint == null) return null;
		return new BoundedRay(intersectionPoint.Value, Direction.ReflectedBy(plane) * (Length - intersectionPoint.Value.DistanceFrom(StartPoint)));
	}

	float? GetUnboundedPlaneIntersectionDistance(Plane plane) {
		var similarityToNormal = plane.Normal.Dot(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		return (plane.PointClosestToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
	}

	public BoundedRay? IntersectionWith(Plane plane) {
		var distance = GetUnboundedPlaneIntersectionDistance(plane);
		if (distance >= 0f && distance <= Length) return new BoundedRay(UnboundedLocationAtDistance(distance.Value), EndPoint);
		else return null; // Plane parallel with line or outside line boundaries
	}
	public bool IsIntersectedBy(Plane plane) {
		var unboundedIntersectionDistance = GetUnboundedPlaneIntersectionDistance(plane);
		return unboundedIntersectionDistance >= 0f && unboundedIntersectionDistance <= Length;
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

	public BoundedRay ProjectedOnTo(Plane plane) {
		return new BoundedRay(StartPoint.ClosestPointOn(plane), EndPoint.ClosestPointOn(plane));
	}
	public BoundedRay ProjectedOnTo(Plane plane, bool preserveLength) {
		if (!preserveLength) return ProjectedOnTo(plane);
		var newVect = StartToEndVect.ParallelizedWith(plane);
		if (newVect.LengthSquared == 0f && LengthSquared > 0f) newVect = StartToEndVect;
		return new BoundedRay(StartPoint.ClosestPointOn(plane), newVect);
	}
	public BoundedRay ParallelizedWith(Plane plane) { // TODO in xmldoc mention that length will be 0 if this is perpendicular, regardless
		var newVect = StartToEndVect.ParallelizedWith(plane);
		if (newVect.LengthSquared == 0f && LengthSquared > 0f) newVect = StartToEndVect;
		return new BoundedRay(StartPoint, newVect);
	}
	public BoundedRay OrthogonalizedAgainst(Plane plane) {
		return new BoundedRay(StartPoint, StartToEndVect.OrthogonalizedAgainst(plane));
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

	public bool TrySplit(Plane plane, out BoundedRay outStartPointToPlane, out BoundedRay outPlaneToEndPoint) {
		var intersection = IntersectionWith(plane);
		if (intersection == null) {
			outStartPointToPlane = default;
			outPlaneToEndPoint = default;
			return false;
		}

		outStartPointToPlane = new BoundedRay(StartPoint, intersection.Value.StartPoint);
		outPlaneToEndPoint = intersection.Value;
		return true;
	}
}