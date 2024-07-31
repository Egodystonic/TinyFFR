// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR;

partial struct BoundedRay : IScalable<BoundedRay>, ILengthAdjustable<BoundedRay> {
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
		return new(endPoint + _vect.Flipped * rotation, endPoint);
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
	public static BoundedRay NewRandom() => new(Location.NewRandom(), Location.NewRandom());
	public static BoundedRay NewRandom(BoundedRay minInclusive, BoundedRay maxExclusive) => new(Location.NewRandom(minInclusive.StartPoint, maxExclusive.StartPoint), Location.NewRandom(minInclusive.EndPoint, maxExclusive.EndPoint));

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
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new BoundedRay(intersectionPoint.Value, Direction.FastReflectedBy(plane) * (Length - intersectionPoint.Value.DistanceFrom(StartPoint)));
	}
	public BoundedRay FastReflectedBy(Plane plane) {
		var intersectionPoint = FastIntersectionWith(plane);
		return new BoundedRay(intersectionPoint, Direction.FastReflectedBy(plane) * (Length - intersectionPoint.DistanceFrom(StartPoint)));
	}

	float? GetUnboundedPlaneIntersectionDistance(Plane plane) {
		var similarityToNormal = plane.Normal.Dot(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		return (plane.PointClosestToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
	}

	public Angle? IncidentAngleWith(Plane plane) => IsIntersectedBy(plane) ? plane.IncidentAngleWith(Direction) : null;
	public Angle FastIncidentAngleWith(Plane plane) => plane.FastIncidentAngleWith(Direction);

	public Location? IntersectionWith(Plane plane) {
		var distance = GetUnboundedPlaneIntersectionDistance(plane);
		return distance >= 0f && distance <= Length ? UnboundedLocationAtDistance(distance.Value) : null; // Null means Plane parallel with line or outside line boundaries
	}
	public Pair<BoundedRay, BoundedRay>? SplitBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		return intersectionPoint == null ? null : new(new(StartPoint, intersectionPoint.Value), new(intersectionPoint.Value, EndPoint));
	}
	public Location FastIntersectionWith(Plane plane) => UnboundedLocationAtDistance((plane.PointClosestToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / plane.Normal.Dot(Direction));
	public Pair<BoundedRay, BoundedRay> FastSplitBy(Plane plane) {
		var intersectionPoint = FastIntersectionWith(plane);
		return new(new(StartPoint, intersectionPoint), new(intersectionPoint, EndPoint));
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

	// Note: Projection treats this like two points (start/end), whereas parallelize/orthogonalize treat it as a start-point + vect; hence the ostensible discrepancy
	// That being said, I feel like projection vs parallelization/orthogonalization are subtly different things even if they're thought of in a similar vein; hence why I chose it this way
	public BoundedRay ProjectedOnTo(Plane plane) => new(StartPoint.ClosestPointOn(plane), EndPoint.ClosestPointOn(plane)); // TODO in xmldoc note that this does not preserve length, returns a zero-length ray if orthogonal to plane
	BoundedRay? IProjectable<BoundedRay, Plane>.ProjectedOnTo(Plane plane) => ProjectedOnTo(plane);
	BoundedRay IProjectable<BoundedRay, Plane>.FastProjectedOnTo(Plane plane) => ProjectedOnTo(plane);

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

	// TODO xmldoc for all Fast... variants here make it clear that as well direction not being None or ortho/parallel etc, StartToEndVect can not be zero
	public BoundedRay? ParallelizedWith(Direction direction) => ParallelizedAroundStartWith(direction);
	public BoundedRay FastParallelizedWith(Direction direction) => FastParallelizedAroundStartWith(direction);
	public BoundedRay? OrthogonalizedAgainst(Direction direction) => OrthogonalizedAroundStartAgainst(direction);
	public BoundedRay FastOrthogonalizedAgainst(Direction direction) => FastOrthogonalizedAroundStartAgainst(direction);

	// TODO add these for Plane too, and add overloads for line types
	public BoundedRay? ParallelizedAroundStartWith(Direction direction) {
		var newVect = StartToEndVect.ParallelizedWith(direction);
		return newVect == null ? null : FromStartPointAndVect(StartPoint, newVect.Value);
	}
	public BoundedRay? ParallelizedAroundMiddleWith(Direction direction) {
		var newDir = Direction.ParallelizedWith(direction);
		if (newDir == null) return null;
		return RotatedAroundMiddleBy(Direction >> newDir.Value);
	}
	public BoundedRay? ParallelizedAroundEndWith(Direction direction) {
		var newVect = StartToEndVect.ParallelizedWith(direction);
		return newVect == null ? null : FromStartPointAndVect(EndPoint - newVect.Value, newVect.Value);
	}
	public BoundedRay? ParallelizedAroundPivotDistanceWith(Direction direction, float signedPivotDistance) {
		var newDir = Direction.ParallelizedWith(direction);
		if (newDir == null) return null;
		return RotatedAroundPoint(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	public BoundedRay FastParallelizedAroundStartWith(Direction direction) => FromStartPointAndVect(StartPoint, StartToEndVect.FastParallelizedWith(direction));
	public BoundedRay FastParallelizedAroundMiddleWith(Direction direction) => RotatedAroundMiddleBy(Direction >> Direction.FastParallelizedWith(direction));
	public BoundedRay FastParallelizedAroundEndWith(Direction direction) {
		var newVect = StartToEndVect.FastParallelizedWith(direction);
		return FromStartPointAndVect(EndPoint - newVect, newVect);
	}
	public BoundedRay FastParallelizedAroundPivotDistanceWith(Direction direction, float signedPivotDistance) => RotatedAroundPoint(Direction >> Direction.FastParallelizedWith(direction), UnboundedLocationAtDistance(signedPivotDistance));

	public BoundedRay? OrthogonalizedAroundStartAgainst(Direction direction) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(direction);
		return newVect == null ? null : FromStartPointAndVect(StartPoint, newVect.Value);
	}
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Direction direction) {
		var newDir = Direction.OrthogonalizedAgainst(direction);
		if (newDir == null) return null;
		return RotatedAroundMiddleBy(Direction >> newDir.Value);
	}
	public BoundedRay? OrthogonalizedAroundEndAgainst(Direction direction) {
		var newVect = StartToEndVect.OrthogonalizedAgainst(direction);
		return newVect == null ? null : FromStartPointAndVect(EndPoint - newVect.Value, newVect.Value);
	}
	public BoundedRay? OrthogonalizedAroundPivotDistanceAgainst(Direction direction, float signedPivotDistance) {
		var newDir = Direction.OrthogonalizedAgainst(direction);
		if (newDir == null) return null;
		return RotatedAroundPoint(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Direction direction) => FromStartPointAndVect(StartPoint, StartToEndVect.FastOrthogonalizedAgainst(direction));
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Direction direction) => RotatedAroundMiddleBy(Direction >> Direction.FastOrthogonalizedAgainst(direction));
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Direction direction) {
		var newVect = StartToEndVect.FastOrthogonalizedAgainst(direction);
		return FromStartPointAndVect(EndPoint - newVect, newVect);
	}
	public BoundedRay FastOrthogonalizedAroundPivotDistanceAgainst(Direction direction, float signedPivotDistance) => RotatedAroundPoint(Direction >> Direction.FastOrthogonalizedAgainst(direction), UnboundedLocationAtDistance(signedPivotDistance));

	public BoundedRay? ParallelizedAroundStartWith(Line line) => ParallelizedAroundStartWith(line.Direction);
	public BoundedRay? ParallelizedAroundMiddleWith(Line line) => ParallelizedAroundMiddleWith(line.Direction);
	public BoundedRay? ParallelizedAroundEndWith(Line line) => ParallelizedAroundEndWith(line.Direction);
	public BoundedRay? ParallelizedAroundPivotDistanceWith(Line line, float signedPivotDistance) => ParallelizedAroundPivotDistanceWith(line.Direction, signedPivotDistance);
	public BoundedRay FastParallelizedAroundStartWith(Line line) => FastParallelizedAroundStartWith(line.Direction);
	public BoundedRay FastParallelizedAroundMiddleWith(Line line) => FastParallelizedAroundMiddleWith(line.Direction);
	public BoundedRay FastParallelizedAroundEndWith(Line line) => FastParallelizedAroundEndWith(line.Direction);
	public BoundedRay FastParallelizedAroundPivotDistanceWith(Line line, float signedPivotDistance) => FastParallelizedAroundPivotDistanceWith(line.Direction, signedPivotDistance);
	public BoundedRay? ParallelizedAroundStartWith(Ray ray) => ParallelizedAroundStartWith(ray.Direction);
	public BoundedRay? ParallelizedAroundMiddleWith(Ray ray) => ParallelizedAroundMiddleWith(ray.Direction);
	public BoundedRay? ParallelizedAroundEndWith(Ray ray) => ParallelizedAroundEndWith(ray.Direction);
	public BoundedRay? ParallelizedAroundPivotDistanceWith(Ray ray, float signedPivotDistance) => ParallelizedAroundPivotDistanceWith(ray.Direction, signedPivotDistance);
	public BoundedRay FastParallelizedAroundStartWith(Ray ray) => FastParallelizedAroundStartWith(ray.Direction);
	public BoundedRay FastParallelizedAroundMiddleWith(Ray ray) => FastParallelizedAroundMiddleWith(ray.Direction);
	public BoundedRay FastParallelizedAroundEndWith(Ray ray) => FastParallelizedAroundEndWith(ray.Direction);
	public BoundedRay FastParallelizedAroundPivotDistanceWith(Ray ray, float signedPivotDistance) => FastParallelizedAroundPivotDistanceWith(ray.Direction, signedPivotDistance);
	public BoundedRay? ParallelizedAroundStartWith(BoundedRay ray) => ParallelizedAroundStartWith(ray.Direction);
	public BoundedRay? ParallelizedAroundMiddleWith(BoundedRay ray) => ParallelizedAroundMiddleWith(ray.Direction);
	public BoundedRay? ParallelizedAroundEndWith(BoundedRay ray) => ParallelizedAroundEndWith(ray.Direction);
	public BoundedRay? ParallelizedAroundPivotDistanceWith(BoundedRay ray, float signedPivotDistance) => ParallelizedAroundPivotDistanceWith(ray.Direction, signedPivotDistance);
	public BoundedRay FastParallelizedAroundStartWith(BoundedRay ray) => FastParallelizedAroundStartWith(ray.Direction);
	public BoundedRay FastParallelizedAroundMiddleWith(BoundedRay ray) => FastParallelizedAroundMiddleWith(ray.Direction);
	public BoundedRay FastParallelizedAroundEndWith(BoundedRay ray) => FastParallelizedAroundEndWith(ray.Direction);
	public BoundedRay FastParallelizedAroundPivotDistanceWith(BoundedRay ray, float signedPivotDistance) => FastParallelizedAroundPivotDistanceWith(ray.Direction, signedPivotDistance);

	public BoundedRay? OrthogonalizedAroundStartAgainst(Line line) => OrthogonalizedAroundStartAgainst(line.Direction);
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Line line) => OrthogonalizedAroundMiddleAgainst(line.Direction);
	public BoundedRay? OrthogonalizedAroundEndAgainst(Line line) => OrthogonalizedAroundEndAgainst(line.Direction);
	public BoundedRay? OrthogonalizedAroundPivotDistanceAgainst(Line line, float signedPivotDistance) => OrthogonalizedAroundPivotDistanceAgainst(line.Direction, signedPivotDistance);
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Line line) => FastOrthogonalizedAroundStartAgainst(line.Direction);
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Line line) => FastOrthogonalizedAroundMiddleAgainst(line.Direction);
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Line line) => FastOrthogonalizedAroundEndAgainst(line.Direction);
	public BoundedRay FastOrthogonalizedAroundPivotDistanceAgainst(Line line, float signedPivotDistance) => FastOrthogonalizedAroundPivotDistanceAgainst(line.Direction, signedPivotDistance);
	public BoundedRay? OrthogonalizedAroundStartAgainst(Ray ray) => OrthogonalizedAroundStartAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(Ray ray) => OrthogonalizedAroundMiddleAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAroundEndAgainst(Ray ray) => OrthogonalizedAroundEndAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAroundPivotDistanceAgainst(Ray ray, float signedPivotDistance) => OrthogonalizedAroundPivotDistanceAgainst(ray.Direction, signedPivotDistance);
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Ray ray) => FastOrthogonalizedAroundStartAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Ray ray) => FastOrthogonalizedAroundMiddleAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Ray ray) => FastOrthogonalizedAroundEndAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAroundPivotDistanceAgainst(Ray ray, float signedPivotDistance) => FastOrthogonalizedAroundPivotDistanceAgainst(ray.Direction, signedPivotDistance);
	public BoundedRay? OrthogonalizedAroundStartAgainst(BoundedRay ray) => OrthogonalizedAroundStartAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAroundMiddleAgainst(BoundedRay ray) => OrthogonalizedAroundMiddleAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAroundEndAgainst(BoundedRay ray) => OrthogonalizedAroundEndAgainst(ray.Direction);
	public BoundedRay? OrthogonalizedAroundPivotDistanceAgainst(BoundedRay ray, float signedPivotDistance) => OrthogonalizedAroundPivotDistanceAgainst(ray.Direction, signedPivotDistance);
	public BoundedRay FastOrthogonalizedAroundStartAgainst(BoundedRay ray) => FastOrthogonalizedAroundStartAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(BoundedRay ray) => FastOrthogonalizedAroundMiddleAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAroundEndAgainst(BoundedRay ray) => FastOrthogonalizedAroundEndAgainst(ray.Direction);
	public BoundedRay FastOrthogonalizedAroundPivotDistanceAgainst(BoundedRay ray, float signedPivotDistance) => FastOrthogonalizedAroundPivotDistanceAgainst(ray.Direction, signedPivotDistance);

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
	public BoundedRay? ParallelizedAroundPivotDistanceWith(Plane plane, float signedPivotDistance) {
		var newDir = Direction.ParallelizedWith(plane);
		if (newDir == null) return null;
		return RotatedAroundPoint(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	public BoundedRay FastParallelizedAroundStartWith(Plane plane) => new(StartPoint, StartToEndVect.FastParallelizedWith(plane));
	public BoundedRay FastParallelizedAroundMiddleWith(Plane plane) => RotatedAroundMiddleBy(Direction >> Direction.FastParallelizedWith(plane));
	public BoundedRay FastParallelizedAroundEndWith(Plane plane) => new(EndPoint - StartToEndVect.FastParallelizedWith(plane), EndPoint);
	public BoundedRay FastParallelizedAroundPivotDistanceWith(Plane plane, float signedPivotDistance) => RotatedAroundPoint(Direction >> Direction.FastParallelizedWith(plane), UnboundedLocationAtDistance(signedPivotDistance));

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
	public BoundedRay? OrthogonalizedAroundPivotDistanceAgainst(Plane plane, float signedPivotDistance) {
		var newDir = Direction.OrthogonalizedAgainst(plane);
		if (newDir == null) return null;
		return RotatedAroundPoint(Direction >> newDir.Value, UnboundedLocationAtDistance(signedPivotDistance));
	}
	public BoundedRay FastOrthogonalizedAroundStartAgainst(Plane plane) => new(StartPoint, StartToEndVect.FastOrthogonalizedAgainst(plane));
	public BoundedRay FastOrthogonalizedAroundMiddleAgainst(Plane plane) => RotatedAroundMiddleBy(Direction >> Direction.FastOrthogonalizedAgainst(plane));
	public BoundedRay FastOrthogonalizedAroundEndAgainst(Plane plane) => new(EndPoint - StartToEndVect.FastOrthogonalizedAgainst(plane), EndPoint);
	public BoundedRay FastOrthogonalizedAroundPivotDistanceAgainst(Plane plane, float signedPivotDistance) => RotatedAroundPoint(Direction >> Direction.FastParallelizedWith(plane), UnboundedLocationAtDistance(signedPivotDistance));
}