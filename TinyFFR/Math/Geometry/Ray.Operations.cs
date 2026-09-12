// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;
using System.Net;

namespace Egodystonic.TinyFFR;

public readonly partial struct Ray : IPhysicalValidityDeterminable {
	/// <summary>
	/// Converts this ray to a <see cref="BoundedRay"/> from <see cref="StartPoint"/> to the point found by travelling <paramref name="signedDistanceToEndPoint"/> along this ray.
	/// </summary>
	/// <param name="signedDistanceToEndPoint">The distance along this ray, from <see cref="StartPoint"/>, of the resultant bounded ray's end point. A negative value points the result back the way this ray came.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay ToBoundedRay(float signedDistanceToEndPoint) => new(StartPoint, Direction * signedDistanceToEndPoint);
	/// <summary>
	/// Converts this ray to a <see cref="Line"/> passing through <see cref="StartPoint"/> in <see cref="Direction"/>, discarding the distinction between its two ends.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ToLine() => new(StartPoint, Direction);

	/// <summary>
	/// Returns this ray with its <see cref="Direction"/> reversed, keeping <see cref="StartPoint"/> fixed.
	/// </summary>
	public Ray Flipped {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(StartPoint, -Direction);
	}
	/// <summary>
	/// Negates <paramref name="operand"/>; equivalent to reading <see cref="Flipped"/>.
	/// </summary>
	/// <param name="operand">The ray to negate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator -(Ray operand) => operand.Flipped;
	Ray IInvertible<Ray>.Inverted => Flipped;

	/// <inheritdoc/>
	public bool IsPhysicallyValid => _direction != Direction.None;

	#region Line-Like Methods
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool DistanceIsWithinLineBounds(float signedDistanceFromStart) => signedDistanceFromStart >= 0f;
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float BindDistance(float signedDistanceFromStart) => MathF.Max(0f, signedDistanceFromStart);
	/// <inheritdoc />
	public Location BoundedLocationAtDistance(float signedDistanceFromStart) => UnboundedLocationAtDistance(BindDistance(signedDistanceFromStart));
	/// <inheritdoc />
	public Location UnboundedLocationAtDistance(float signedDistanceFromStart) => StartPoint + Direction * signedDistanceFromStart;
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
	public static Ray operator +(Ray ray, Vect v) => ray.MovedBy(v);
	/// <summary>
	/// Moves <paramref name="ray"/> by the negation of <paramref name="v"/>; equivalent to <see cref="MovedBy(Vect)"/> with <paramref name="v"/> negated.
	/// </summary>
	/// <param name="ray">The ray to move.</param>
	/// <param name="v">The vector to move it by, negated.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator -(Ray ray, Vect v) => ray.MovedBy(-v);
	/// <inheritdoc cref="operator +(Ray,Vect)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator +(Vect v, Ray ray) => ray.MovedBy(v);
	/// <summary>
	/// Returns this ray moved by <paramref name="v"/>, i.e. with <paramref name="v"/> added to <see cref="StartPoint"/>.
	/// </summary>
	/// <param name="v">The vector to move this ray by.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray MovedBy(Vect v) => new(StartPoint + v, Direction);
	#endregion

	#region Rotation
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *(Ray ray, Rotation rot) => ray.RotatedBy(rot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *(Rotation rot, Ray ray) => ray.RotatedBy(rot);
	/// <summary>
	/// Returns this ray rotated by <paramref name="rotation"/> around its own <see cref="StartPoint"/>, which therefore stays fixed.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray RotatedBy(Rotation rotation) => new(StartPoint, Direction.RotatedBy(rotation));

	/// <inheritdoc/>
	public Ray RotatedAroundOriginBy(Rotation rot) => new(StartPoint.AsVect().RotatedBy(rot).AsLocation(), Direction.RotatedBy(rot));
	/// <inheritdoc cref="RotatedBy(Rotation)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray RotatedBy(Quaternion rotationQuaternion) => new(StartPoint, Direction.RotatedBy(rotationQuaternion));
	/// <inheritdoc cref="RotatedAroundOriginBy(Rotation)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray RotatedAroundOriginBy(Quaternion rotQuat) => new(StartPoint.AsVect().RotatedBy(rotQuat).AsLocation(), Direction.RotatedBy(rotQuat));

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *(Ray ray, (Rotation Rotation, Location Pivot) pivotRotationTuple) => ray.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *(Ray ray, (Location Pivot, Rotation Rotation) pivotRotationTuple) => ray.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *((Rotation Rotation, Location Pivot) pivotRotationTuple, Ray ray) => ray.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Ray operator *((Location Pivot, Rotation Rotation) pivotRotationTuple, Ray ray) => ray.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <inheritdoc/>
	public Ray RotatedBy(Rotation rotation, float signedPivotDistance) => RotatedBy(rotation, UnboundedLocationAtDistance(signedPivotDistance));
	/// <inheritdoc/>
	public Ray RotatedBy(Rotation rotation, Location pivot) {
		var boundedRay = new BoundedRay(StartPoint, UnboundedLocationAtDistance(UnboundedDistanceAtPointClosestTo(pivot)));
		var rotatedRay = boundedRay.RotatedBy(rotation, pivot);
		return new Ray(rotatedRay.StartPoint, Direction * rotation);
	}
	/// <inheritdoc cref="RotatedBy(Rotation,float)" />
	public Ray RotatedBy(Quaternion rotationQuaternion, float signedPivotDistance) => RotatedBy(rotationQuaternion, UnboundedLocationAtDistance(signedPivotDistance));
	/// <inheritdoc cref="RotatedBy(Rotation,Location)" />
	public Ray RotatedBy(Quaternion rotationQuaternion, Location pivot) {
		var boundedRay = new BoundedRay(StartPoint, UnboundedLocationAtDistance(UnboundedDistanceAtPointClosestTo(pivot)));
		var rotatedRay = boundedRay.RotatedBy(rotationQuaternion, pivot);
		return new Ray(rotatedRay.StartPoint, Direction.RotatedBy(rotationQuaternion));
	}
	#endregion

	#region Distance / Closest Point / Containment
	/// <summary>
	/// Returns the point on this ray that is closest to <paramref name="location"/>.
	/// </summary>
	/// <remarks>
	/// Because this ray only extends in one direction, the result may be <see cref="StartPoint"/> if <paramref name="location"/> is behind it.
	/// </remarks>
	/// <param name="location">The location to measure from.</param>
	public Location PointClosestTo(Location location) {
		var distance = Vector3.Dot((location - StartPoint).ToVector3(), Direction.ToVector3());
		return distance switch {
			< 0f => StartPoint,
			_ => StartPoint + Direction * distance
		};
	}
	/// <inheritdoc cref="ILineLike.PointClosestToOrigin" />
	public Location PointClosestToOrigin() {
		var distance = -Vector3.Dot(StartPoint.ToVector3(), Direction.ToVector3());
		return distance switch {
			< 0f => StartPoint,
			_ => StartPoint + Direction * distance
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
		if (intersectionDistances == null || intersectionDistances.Value.OtherDistance < 0f) return PointClosestTo(boundedRay.StartPoint);
		else if (!boundedRay.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return PointClosestTo(boundedRay.EndPoint);
		else return BoundedLocationAtDistance(intersectionDistances.Value.ThisDistance);
	}

	/// <summary>
	/// Determines whether <paramref name="other"/> starts within <paramref name="distance"/> of this ray's <see cref="StartPoint"/> and points in a similar enough direction (within <paramref name="angle"/> of this ray's <see cref="Direction"/>).
	/// </summary>
	/// <param name="other">The other ray to compare to.</param>
	/// <param name="distance">The maximum permitted distance between the two rays' start points.</param>
	/// <param name="angle">The maximum permitted angle between the two rays' directions.</param>
	public bool IsWithinDistanceAndAngleTo(Ray other, float distance, Angle angle) {
		return StartPoint.DistanceSquaredFrom(other.StartPoint) <= distance * distance && Direction.IsWithinAngleTo(other.Direction, angle);
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
	/// <returns><see langword="null"/> if this ray is parallel to <paramref name="plane"/> or <paramref name="plane"/> is behind <see cref="StartPoint"/>; the intersection point otherwise.</returns>
	public Location? IntersectionWith(Plane plane) {
		var distance = GetUnboundedPlaneIntersectionDistance(plane);
		return distance >= 0f ? UnboundedLocationAtDistance(distance.Value) : null; // Null means Plane behind ray or parallel with ray
	}
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray does intersect <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to test against.</param>
	public Location FastIntersectionWith(Plane plane) => UnboundedLocationAtDistance((plane.PointClosestToOrigin - StartPoint).LengthWhenProjectedOnTo(plane.Normal) / plane.Normal.Dot(Direction));

	/// <summary>
	/// Determines whether this ray intersects <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The plane to test against.</param>
	public bool IsIntersectedBy(Plane plane) => GetUnboundedPlaneIntersectionDistance(plane) >= 0f;

	/// <summary>
	/// Splits this ray into a <see cref="BoundedRay"/> and a <see cref="Ray"/> at the point it intersects <paramref name="plane"/>, if it does.
	/// </summary>
	/// <param name="plane">The plane to split this ray at.</param>
	/// <returns><see langword="null"/> if this ray does not intersect <paramref name="plane"/>; otherwise a pair of the bounded ray from <see cref="StartPoint"/> to the split point, and the ray continuing on from the split point in the same <see cref="Direction"/>.</returns>
	public Pair<BoundedRay, Ray>? SplitBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		return intersectionPoint == null ? null : new(new(StartPoint, intersectionPoint.Value), new(intersectionPoint.Value, Direction));
	}
	/// <summary>
	/// Executes the same function as <see cref="SplitBy(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray does intersect <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to split this ray at.</param>
	public Pair<BoundedRay, Ray> FastSplitBy(Plane plane) {
		var intersectionPoint = FastIntersectionWith(plane);
		return new(new(StartPoint, intersectionPoint), new(intersectionPoint, Direction));
	}

	/// <summary>
	/// Calculates the angle at which this ray meets <paramref name="plane"/>, if it intersects it.
	/// </summary>
	/// <param name="plane">The plane to measure against.</param>
	/// <returns><see langword="null"/> if this ray does not intersect <paramref name="plane"/>; the incident angle otherwise.</returns>
	public Angle? IncidentAngleWith(Plane plane) => IsIntersectedBy(plane) ? plane.IncidentAngleWith(Direction) : null;
	/// <summary>
	/// Executes the same function as <see cref="IncidentAngleWith(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray does intersect <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to measure against.</param>
	public Angle FastIncidentAngleWith(Plane plane) => plane.FastIncidentAngleWith(Direction);

	/// <summary>
	/// Reflects the portion of this ray beyond <paramref name="plane"/> back off it, if this ray intersects <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The plane to reflect off.</param>
	/// <returns><see langword="null"/> if this ray does not intersect <paramref name="plane"/>; the reflected ray otherwise, starting at the intersection point.</returns>
	public Ray? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new Ray(intersectionPoint.Value, Direction.FastReflectedBy(plane));
	}
	/// <summary>
	/// Executes the same function as <see cref="ReflectedBy(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray does intersect <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to reflect off.</param>
	public Ray FastReflectedBy(Plane plane) => new(FastIntersectionWith(plane), Direction.FastReflectedBy(plane));

	/// <summary>
	/// Calculates the distance between this ray and <paramref name="plane"/>, signed according to which side of <paramref name="plane"/> <see cref="StartPoint"/> falls on.
	/// </summary>
	/// <remarks>
	/// The sign matches <see cref="Plane.SignedDistanceFrom(Location)"/>: positive when <see cref="StartPoint"/> is on the side <see cref="Plane.Normal"/> points towards, negative on the opposite side. If this ray intersects <paramref name="plane"/>, the result is <c>0f</c> regardless of which side <see cref="StartPoint"/> is on.
	/// </remarks>
	/// <param name="plane">The plane to measure against.</param>
	public float SignedDistanceFrom(Plane plane) {
		var unboundedDistance = GetUnboundedPlaneIntersectionDistance(plane);
		if (unboundedDistance >= 0f) return 0f;
		else return plane.SignedDistanceFrom(StartPoint);
	}

	/// <summary>
	/// Calculates the (unsigned) distance between this ray and <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The plane to measure against.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => MathF.Abs(SignedDistanceFrom(plane));

	/// <summary>
	/// Returns the point on this ray that is closest to <paramref name="plane"/>.
	/// </summary>
	/// <remarks>
	/// If this ray intersects <paramref name="plane"/>, every point on <paramref name="plane"/> is equally close, so the intersection point is returned; otherwise <see cref="StartPoint"/> is returned.
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
		if (unboundedDistance >= 0f) return closestPointOnLine; // Actual intersection
		else return plane.PointClosestTo(closestPointOnLine);
	}

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
	public Ray? ParallelizedWith(Direction direction) {
		var newDir = Direction.ParallelizedWith(direction);
		return newDir == null ? null : new(StartPoint, newDir.Value);
	}
	/// <inheritdoc/>
	public Ray FastParallelizedWith(Direction direction) => new(StartPoint, Direction.FastParallelizedWith(direction));
	/// <inheritdoc/>
	public Ray? OrthogonalizedAgainst(Direction direction) {
		var newDir = Direction.OrthogonalizedAgainst(direction);
		return newDir == null ? null : new(StartPoint, newDir.Value);
	}
	/// <inheritdoc/>
	public Ray FastOrthogonalizedAgainst(Direction direction) => new(StartPoint, Direction.FastOrthogonalizedAgainst(direction));

	/// <inheritdoc/>
	public Ray? ParallelizedWith(Plane plane) {
		var projectedDirection = Direction.ParallelizedWith(plane);
		if (projectedDirection == null) return null;
		return new Ray(StartPoint, projectedDirection.Value);
	}
	/// <inheritdoc/>
	public Ray FastParallelizedWith(Plane plane) => new(StartPoint, Direction.FastParallelizedWith(plane));

	/// <inheritdoc/>
	public Ray? OrthogonalizedAgainst(Plane plane) {
		var newDirection = Direction.OrthogonalizedAgainst(plane);
		if (newDirection == null) return null;
		return new(StartPoint, newDirection.Value);
	}
	/// <inheritdoc/>
	public Ray FastOrthogonalizedAgainst(Plane plane) => new(StartPoint, Direction.FastOrthogonalizedAgainst(plane));

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	/// <remarks>
	/// <see cref="StartPoint"/> moves to its closest point on <paramref name="plane"/> and <see cref="Direction"/> is parallelized with <paramref name="plane"/> (see <see cref="ParallelizedWith(Plane)"/>) — so, unlike parallelizing alone, this also relocates the ray to actually lie within <paramref name="plane"/>.
	/// </remarks>
	/// <param name="plane">The plane to project onto.</param>
	public Ray? ProjectedOnTo(Plane plane) {
		var projectedDirection = Direction.ParallelizedWith(plane);
		if (projectedDirection == null) return null;
		return new Ray(StartPoint.ClosestPointOn(plane), projectedDirection.Value);
	}
	/// <inheritdoc/>
	public Ray FastProjectedOnTo(Plane plane) => new(StartPoint.ClosestPointOn(plane), Direction.FastParallelizedWith(plane));
	#endregion

	#region Clamping and Interpolation
	/// <inheritdoc/>
	public static Ray Interpolate(Ray start, Ray end, float distance) {
		return new(
			Location.Interpolate(start.StartPoint, end.StartPoint, distance),
			Direction.Interpolate(start.Direction, end.Direction, distance)
		);
	}
	/// <inheritdoc/>
	public static Rotation CreateInterpolationPrecomputation(Ray start, Ray end) {
		return Direction.CreateInterpolationPrecomputation(start.Direction, end.Direction);
	}
	/// <inheritdoc/>
	public static Ray InterpolateUsingPrecomputation(Ray start, Ray end, Rotation precomputation, float distance) {
		return new(
			Location.Interpolate(start.StartPoint, end.StartPoint, distance),
			Direction.InterpolateUsingPrecomputation(start.Direction, end.Direction, precomputation, distance)
		);
	}

	/// <inheritdoc/>
	public Ray Clamp(Ray min, Ray max) => new(StartPoint.Clamp(min.StartPoint, max.StartPoint), Direction.Clamp(min.Direction, max.Direction));
	#endregion
}