// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct BoundedLine : 
	IUnaryNegationOperators<BoundedLine, BoundedLine>,
	IMultiplyOperators<BoundedLine, float, BoundedLine>,
	IMultiplyOperators<BoundedLine, Rotation, BoundedLine>,
	IAdditionOperators<BoundedLine, Vect, BoundedLine> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromStart() => new(StartPoint, Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromEnd() => new(EndPoint, -Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ToLine() => new(StartPoint, Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator -(BoundedLine operand) => operand.Flipped;
	public BoundedLine Flipped {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(EndPoint, StartPoint);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator *(BoundedLine line, float scalar) => line.ScaledFromMiddleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator *(float scalar, BoundedLine line) => line.ScaledFromMiddleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine ScaledFromStartBy(float scalar) => new(_startPoint, _vect.ScaledBy(scalar));
	public BoundedLine ScaledFromMiddleBy(float scalar) {
		var halfVect = _vect * 0.5f;
		var midPoint = _startPoint + halfVect;
		var scaledVect = _vect.ScaledBy(scalar);
		var newStart = midPoint - halfVect.ScaledBy(scalar);
		return new BoundedLine(newStart, newStart + scaledVect);
	}
	public BoundedLine ScaledFromEndBy(float scalar) {
		var scaledVect = _vect.ScaledBy(scalar);
		var newStart = (_startPoint + _vect) - scaledVect;
		return new BoundedLine(newStart, scaledVect);
	}
	public BoundedLine ScaledAroundPivotDistanceBy(float scalar, float signedPivotDistance) {
		var pivotPoint = UnboundedLocationAtDistance(signedPivotDistance);
		var pivotToStartVect = -_vect.WithLength(signedPivotDistance);
		var pivotToEndVect = _vect.WithLength(_vect.Length - signedPivotDistance);
		return new BoundedLine(pivotPoint + pivotToStartVect * scalar, pivotPoint + pivotToEndVect * scalar);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine WithLength(float newLength) => new(_startPoint, _vect.WithLength(newLength));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine ShortenedBy(float lengthDecrease) => WithLength(Length - lengthDecrease);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine LengthenedBy(float lengthIncrease) => WithLength(Length + lengthIncrease);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine WithMaxLength(float maxLength) => WithLength(MathF.Min(Length, maxLength));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine WithMinLength(float minLength) => WithLength(MathF.Max(Length, minLength));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator *(BoundedLine line, Rotation rot) => line.RotatedAroundMiddleBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator *(Rotation rot, BoundedLine line) => line.RotatedAroundMiddleBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine RotatedAroundStartBy(Rotation rotation) => new(_startPoint, _vect * rotation);
	public BoundedLine RotatedAroundEndBy(Rotation rotation) {
		var endPoint = _startPoint + _vect;
		return new(endPoint + _vect.Reversed * rotation, endPoint);
	}
	public BoundedLine RotatedAroundMiddleBy(Rotation rotation) {
		var newVect = _vect * rotation;
		var newStartPoint = _startPoint + ((_vect * 0.5f) - (newVect * 0.5f));
		return new(newStartPoint, newVect);
	}
	public BoundedLine RotatedAroundPivotDistance(Rotation rotation, float signedPivotDistance) { // TODO something similar for both lines (maybe in interface)
		return RotatedAroundPoint(rotation, UnboundedLocationAtDistance(signedPivotDistance));
	}
	public BoundedLine RotatedAroundPoint(Rotation rotation, Location point) { // TODO add this to ILine as well, and the multi operators with tuples
		return new(point + (point >> StartPoint) * rotation, point + (point >> EndPoint) * rotation);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator +(BoundedLine line, Vect v) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator +(Vect v, BoundedLine line) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine MovedBy(Vect v) => new(_startPoint + v, _vect);


	public static BoundedLine Interpolate(BoundedLine start, BoundedLine end, float distance) {
		return new(
			Location.Interpolate(start._startPoint, end._startPoint, distance),
			Vect.Interpolate(start._vect, end._vect, distance)
		);
	}
	public static BoundedLine CreateNewRandom() => new(Location.CreateNewRandom(), Location.CreateNewRandom());
	public static BoundedLine CreateNewRandom(BoundedLine minInclusive, BoundedLine maxExclusive) => new(Location.CreateNewRandom(minInclusive.StartPoint, maxExclusive.StartPoint), Location.CreateNewRandom(minInclusive.EndPoint, maxExclusive.EndPoint));

	public Location ClosestPointTo(Location location) {
		var vectCoefficient = Vector3.Dot((location - _startPoint).ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => _startPoint + _vect * vectCoefficient
		};
	}
	public Location ClosestPointToOrigin() {
		var vectCoefficient = -Vector3.Dot(_startPoint.ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => _startPoint + _vect * vectCoefficient
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location location) => location.DistanceFrom(ClosestPointTo(location));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromOrigin() => ((Vect) ClosestPointToOrigin()).Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, ILine.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;


	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine {
		return line switch {
			Line l => ClosestPointTo(l),
			Ray r => ClosestPointTo(r),
			BoundedLine b => ClosestPointTo(b),
			_ => line.ClosestPointOn(this) // Possible stack overflow if the other line doesn't implement both sides (To/On), but this allows users to add their own line implementations
		};
	}
	public Location ClosestPointTo(Line line) {
		var intersectionDistance = ILine.CalculateUnboundedIntersectionDistanceOnThisLine(this, line);
		return intersectionDistance != null ? BoundedLocationAtDistance(intersectionDistance.Value) : StartPoint;
	}
	public Location ClosestPointTo(Ray ray) {
		var intersectionDistances = ILine.CalculateUnboundedIntersectionDistancesOnBothLines(this, ray);
		if (intersectionDistances == null || !ray.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return ClosestPointTo(ray.StartPoint);
		else return BoundedLocationAtDistance(intersectionDistances.Value.ThisDistance);
	}
	public Location ClosestPointTo(BoundedLine boundedLine) {
		var intersectionDistances = ILine.CalculateUnboundedIntersectionDistancesOnBothLines(this, boundedLine);
		if (intersectionDistances == null) {
			var distanceToOtherStart = DistanceFrom(boundedLine.StartPoint);
			var distanceToOtherEnd = DistanceFrom(boundedLine.EndPoint);
			return distanceToOtherStart < distanceToOtherEnd ? ClosestPointTo(boundedLine.StartPoint) : ClosestPointTo(boundedLine.EndPoint);
		}
		var boundOtherDistance = boundedLine.BindDistance(intersectionDistances.Value.OtherDistance);
		// ReSharper disable once CompareOfFloatsByEqualityOperator distance will be unchanged if within line bounds
		if (boundOtherDistance == intersectionDistances.Value.OtherDistance) {
			return BoundedLocationAtDistance(intersectionDistances.Value.ThisDistance);
		}
		else {
			return ClosestPointTo(boundedLine.UnboundedLocationAtDistance(boundOtherDistance));
		}
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn<TLine>(TLine line) where TLine : ILine => line.ClosestPointTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.ClosestPointTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.ClosestPointTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedLine boundedLine) => boundedLine.ClosestPointTo(this);

	public float DistanceFrom<TLine>(TLine line) where TLine : ILine => DistanceFrom(ClosestPointOn(line));
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	public float DistanceFrom(BoundedLine boundedLine) => DistanceFrom(ClosestPointOn(boundedLine));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location? ILineIntersectable<Location>.IntersectionWith<TLine>(TLine line) => IntersectionWith(line, ILine.DefaultLineThickness);
	public Location? IntersectionWith<TLine>(TLine line, float lineThickness = ILine.DefaultLineThickness) where TLine : ILine {
		var closestPointOnLine = line.ClosestPointTo(this);
		return DistanceFrom(closestPointOnLine) <= lineThickness ? closestPointOnLine : null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool ILineIntersectable<Location>.IsIntersectedBy<TLine>(TLine line) => IsIntersectedBy(line, ILine.DefaultLineThickness);
	public bool IsIntersectedBy<TLine>(TLine line, float lineThickness = ILine.DefaultLineThickness) where TLine : ILine {
		return DistanceFrom(ClosestPointOn(line)) <= lineThickness;
	}

	public bool DistanceIsWithinLineBounds(float signedDistanceFromStart) => signedDistanceFromStart >= 0f && signedDistanceFromStart * signedDistanceFromStart <= LengthSquared;
	public float BindDistance(float signedDistanceFromStart) => Single.Clamp(signedDistanceFromStart, 0f, Length);
	public Location BoundedLocationAtDistance(float signedDistanceFromStart) => UnboundedLocationAtDistance(BindDistance(signedDistanceFromStart));
	public Location UnboundedLocationAtDistance(float signedDistanceFromStart) => _startPoint + _vect.WithLength(signedDistanceFromStart);
	public Location? LocationAtDistanceOrNull(float signedDistanceFromStart) => DistanceIsWithinLineBounds(signedDistanceFromStart) ? UnboundedLocationAtDistance(signedDistanceFromStart) : null;

	public BoundedLine? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new BoundedLine(intersectionPoint.Value, Direction.ReflectedBy(plane) * (Length - intersectionPoint.Value.DistanceFrom(StartPoint)));
	}

	float? GetUnboundedPlaneIntersectionDistance(Plane plane) {
		var similarityToNormal = plane.Normal.Dot(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		return (plane.ClosestPointToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
	}

	public Location? IntersectionWith(Plane plane) {
		var distance = GetUnboundedPlaneIntersectionDistance(plane);
		if (distance >= 0f && distance <= Length) return UnboundedLocationAtDistance(distance.Value);
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

	public BoundedLine ProjectedOnTo(Plane plane) {
		return new BoundedLine(StartPoint.ClosestPointOn(plane), EndPoint.ClosestPointOn(plane));
	}
	public BoundedLine ProjectedOnTo(Plane plane, bool preserveLength) {
		if (!preserveLength) return ProjectedOnTo(plane);
		var newVect = StartToEndVect.ParallelizedWith(plane);
		if (newVect.LengthSquared == 0f && LengthSquared > 0f) newVect = StartToEndVect;
		return new BoundedLine(StartPoint.ClosestPointOn(plane), newVect);
	}
	public BoundedLine ParallelizedWith(Plane plane) { // TODO in xmldoc mention that length will be 0 if this is perpendicular, regardless
		var newVect = StartToEndVect.ParallelizedWith(plane);
		if (newVect.LengthSquared == 0f && LengthSquared > 0f) newVect = StartToEndVect;
		return new BoundedLine(StartPoint, newVect);
	}
	public BoundedLine OrthogonalizedAgainst(Plane plane) {
		return new BoundedLine(StartPoint, StartToEndVect.OrthogonalizedAgainst(plane));
	}

	public Location ClosestPointTo(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		return BoundedLocationAtDistance(unboundedDistance ?? 0f); // If unboundedDistance is null we're parallel so the StartPoint is as close as any other point
	}
	public Location ClosestPointOn(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		var closestPointOnLine = BoundedLocationAtDistance(unboundedDistance ?? 0f);
		if (unboundedDistance >= 0f && unboundedDistance <= Length) return closestPointOnLine; // Actual intersection
		else return plane.ClosestPointTo(closestPointOnLine);
	}

	public BoundedLine? SlicedBy(Plane plane) {
		if (TrySplit(plane, out _, out var result)) return result;
		else return null;
	}

	public bool TrySplit(Plane plane, out BoundedLine outStartPointToPlane, out BoundedLine outPlaneToEndPoint) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) {
			outStartPointToPlane = default;
			outPlaneToEndPoint = default;
			return false;
		}

		outStartPointToPlane = new BoundedLine(StartPoint, intersectionPoint.Value);
		outPlaneToEndPoint = new BoundedLine(intersectionPoint.Value, EndPoint);
		return true;
	}
}