// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

public readonly partial struct Line : IPhysicalValidityDeterminable {
	/// <summary>
	/// Converts this line to a <see cref="Ray"/> starting at the point found by travelling <paramref name="signedDistanceAlongLine"/> from <see cref="PointOnLine"/>.
	/// </summary>
	/// <param name="signedDistanceAlongLine">The distance along this line, from <see cref="PointOnLine"/>, of the resultant ray's start point.</param>
	/// <param name="flipDirection">If <see langword="false"/>, the resultant ray points in this line's <see cref="Direction"/>; if <see langword="true"/>, it points the opposite way.</param>
	public Ray ToRay(float signedDistanceAlongLine, bool flipDirection) => new(LocationAtDistance(signedDistanceAlongLine), flipDirection ? Direction.Flipped : Direction);
	/// <summary>
	/// Converts this line to a <see cref="BoundedRay"/> between the two points found by travelling <paramref name="startSignedDistanceAlongLine"/> and <paramref name="endSignedDistanceAlongLine"/> from <see cref="PointOnLine"/>.
	/// </summary>
	/// <param name="startSignedDistanceAlongLine">The distance along this line, from <see cref="PointOnLine"/>, of the resultant ray's start point.</param>
	/// <param name="endSignedDistanceAlongLine">The distance along this line, from <see cref="PointOnLine"/>, of the resultant ray's end point.</param>
	public BoundedRay ToBoundedRay(float startSignedDistanceAlongLine, float endSignedDistanceAlongLine) {
		return new(LocationAtDistance(startSignedDistanceAlongLine), LocationAtDistance(endSignedDistanceAlongLine));
	}

	Line IInvertible<Line>.Inverted => new(PointOnLine, -Direction);
	static Line IUnaryNegationOperators<Line, Line>.operator -(Line line) => new Line(line.PointOnLine, -line.Direction);

	/// <inheritdoc/>
	public bool IsPhysicallyValid => _direction != Direction.None;

	#region Line-Like Methods
	/// <summary>
	/// Returns the point found by travelling <paramref name="signedDistanceFromStart"/> along this line from <see cref="PointOnLine"/>, in <see cref="Direction"/>.
	/// </summary>
	/// <param name="signedDistanceFromStart">The distance to travel from <see cref="PointOnLine"/>. A negative value travels in the opposite direction.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location LocationAtDistance(float signedDistanceFromStart) => PointOnLine + Direction * signedDistanceFromStart;

	// These are implemented explicitly because they're all basically the same thing or useless for an unbounded line.
	bool ILineLike.DistanceIsWithinLineBounds(float signedDistanceFromStart) => true;
	float ILineLike.BindDistance(float signedDistanceFromStart) => signedDistanceFromStart;
	Location ILineLike.BoundedLocationAtDistance(float signedDistanceFromStart) => LocationAtDistance(signedDistanceFromStart);
	Location ILineLike.UnboundedLocationAtDistance(float signedDistanceFromStart) => LocationAtDistance(signedDistanceFromStart);
	Location? ILineLike.LocationAtDistanceOrNull(float signedDistanceFromStart) => LocationAtDistance(signedDistanceFromStart);

	float ILineLike.BoundedDistanceAtPointClosestTo(Location point) => DistanceAtPointClosestTo(point);
	float ILineLike.UnboundedDistanceAtPointClosestTo(Location point) => DistanceAtPointClosestTo(point);
	/// <summary>
	/// Calculates the (signed) distance along this line, from <see cref="PointOnLine"/>, of the point on this line closest to <paramref name="point"/>.
	/// </summary>
	/// <remarks>
	/// The result is positive if the closest point is further along in <see cref="Direction"/> than <see cref="PointOnLine"/>, and negative if it is further along the opposite way.
	/// </remarks>
	/// <param name="point">The location to measure against.</param>
	public float DistanceAtPointClosestTo(Location point) {
		var closestPoint = PointClosestTo(point);
		return closestPoint.DistanceFrom(PointOnLine) * MathF.Sign((PointOnLine >> closestPoint).Dot(Direction));
	}
	#endregion

	#region Translation
	/// <summary>
	/// Moves <paramref name="line"/> by <paramref name="v"/>; equivalent to <see cref="MovedBy(Vect)"/>.
	/// </summary>
	/// <param name="line">The line to move.</param>
	/// <param name="v">The vector to move it by.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Line line, Vect v) => line.MovedBy(v);
	/// <summary>
	/// Moves <paramref name="line"/> by the negation of <paramref name="v"/>; equivalent to <see cref="MovedBy(Vect)"/> with <paramref name="v"/> negated.
	/// </summary>
	/// <param name="line">The line to move.</param>
	/// <param name="v">The vector to move it by, negated.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator -(Line line, Vect v) => line.MovedBy(-v);
	/// <inheritdoc cref="operator +(Line,Vect)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Vect v, Line line) => line.MovedBy(v);
	/// <summary>
	/// Returns this line moved by <paramref name="v"/>, i.e. with <paramref name="v"/> added to <see cref="PointOnLine"/>.
	/// </summary>
	/// <param name="v">The vector to move this line by.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line MovedBy(Vect v) => new(PointOnLine + v, Direction);
	#endregion

	#region Rotation
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, Rotation rot) => line.RotatedBy(rot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Rotation rot, Line line) => line.RotatedBy(rot);
	/// <summary>
	/// Returns this line rotated by <paramref name="rotation"/> around its own <see cref="PointOnLine"/>, which therefore stays fixed.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line RotatedBy(Rotation rotation) => new(PointOnLine, Direction.RotatedBy(rotation));

	/// <inheritdoc/>
	public Line RotatedAroundOriginBy(Rotation rot) => new(PointOnLine.AsVect().RotatedBy(rot).AsLocation(), Direction.RotatedBy(rot));
	/// <inheritdoc cref="RotatedBy(Rotation)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line RotatedBy(Quaternion rotationQuaternion) => new(PointOnLine, Direction.RotatedBy(rotationQuaternion));
	/// <inheritdoc cref="RotatedAroundOriginBy(Rotation)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line RotatedAroundOriginBy(Quaternion rotQuat) => new(PointOnLine.AsVect().RotatedBy(rotQuat).AsLocation(), Direction.RotatedBy(rotQuat));

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, (Rotation Rotation, Location Pivot) pivotRotationTuple) => line.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, (Location Pivot, Rotation Rotation) pivotRotationTuple) => line.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *((Rotation Rotation, Location Pivot) pivotRotationTuple, Line line) => line.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *((Location Pivot, Rotation Rotation) pivotRotationTuple, Line line) => line.RotatedBy(pivotRotationTuple.Rotation, pivotRotationTuple.Pivot);
	/// <inheritdoc/>
	public Line RotatedBy(Rotation rotation, float signedPivotDistance) => RotatedBy(rotation, LocationAtDistance(signedPivotDistance));
	/// <inheritdoc/>
	public Line RotatedBy(Rotation rotation, Location pivot) {
		return new(pivot + (pivot >> PointClosestTo(pivot)) * rotation, Direction * rotation);
	}
	/// <inheritdoc cref="RotatedBy(Rotation,float)" />
	public Line RotatedBy(Quaternion rotationQuaternion, float signedPivotDistance) => RotatedBy(rotationQuaternion, LocationAtDistance(signedPivotDistance));
	/// <inheritdoc cref="RotatedBy(Rotation,Location)" />
	public Line RotatedBy(Quaternion rotationQuaternion, Location pivot) {
		return new(pivot + (pivot >> PointClosestTo(pivot)).RotatedBy(rotationQuaternion), Direction.RotatedBy(rotationQuaternion));
	}
	#endregion

	#region Distance / Closest Point / Containment
	/// <summary>
	/// Returns the point on this line that is closest to <paramref name="location"/>.
	/// </summary>
	/// <param name="location">The location to measure from.</param>
	public Location PointClosestTo(Location location) {
		var distance = Vector3.Dot((location - PointOnLine).ToVector3(), Direction.ToVector3());
		return PointOnLine + Direction * distance;
	}
	/// <inheritdoc cref="ILineLike.PointClosestToOrigin" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToOrigin() {
		var distance = -Vector3.Dot(PointOnLine.ToVector3(), Direction.ToVector3());
		return PointOnLine + Direction * distance;
	}

	/// <summary>
	/// Returns the point on this line that is closest to <paramref name="line"/>.
	/// </summary>
	/// <param name="line">The line to measure against.</param>
	public Location PointClosestTo(Line line) {
		var intersectionDistance = ILineLike.CalculateUnboundedIntersectionDistanceOnThisLine(this, line);
		return LocationAtDistance(intersectionDistance ?? 0f);
	}
	/// <summary>
	/// Returns the point on this line that is closest to <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The ray to measure against.</param>
	public Location PointClosestTo(Ray ray) {
		var intersectionDistances = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(this, ray);
		if (intersectionDistances == null || !ray.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return PointClosestTo(ray.StartPoint);
		else return LocationAtDistance(intersectionDistances.Value.ThisDistance);
	}
	/// <summary>
	/// Returns the point on this line that is closest to <paramref name="boundedRay"/>.
	/// </summary>
	/// <param name="boundedRay">The ray to measure against.</param>
	public Location PointClosestTo(BoundedRay boundedRay) {
		var intersectionDistances = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(this, boundedRay);
		if (intersectionDistances == null || intersectionDistances.Value.OtherDistance < 0f) return PointClosestTo(boundedRay.StartPoint);
		else if (!boundedRay.DistanceIsWithinLineBounds(intersectionDistances.Value.OtherDistance)) return PointClosestTo(boundedRay.EndPoint);
		else return LocationAtDistance(intersectionDistances.Value.ThisDistance);
	}

	/// <inheritdoc cref="ILineLike.Contains(Location)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, ILineLike.DefaultLineThickness);
	/// <inheritdoc cref="ILineLike.Contains(Location,float)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;

	/// <summary>
	/// Calculates the distance between this line and <paramref name="location"/>.
	/// </summary>
	/// <param name="location">The location to measure to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location location) => location.DistanceFrom(PointClosestTo(location));
	/// <summary>
	/// Calculates the square of the distance between this line and <paramref name="location"/>. Cheaper than <see cref="DistanceFrom(Location)"/> when only comparing distances.
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

	/// <summary>
	/// Determines whether <paramref name="other"/> passes within <paramref name="distance"/> of this line and points in a similar enough direction (within <paramref name="angle"/> of this line's <see cref="Direction"/>, in either sense — see <see cref="Equals(Line)"/>).
	/// </summary>
	/// <param name="other">The other line to compare to.</param>
	/// <param name="distance">The maximum permitted distance between the two lines.</param>
	/// <param name="angle">The maximum permitted angle between the two lines' directions.</param>
	public bool IsWithinDistanceAndAngleTo(Line other, float distance, Angle angle) {
		return DistanceFrom(other) <= distance && (Direction.IsWithinAngleTo(other.Direction, angle) || Direction.IsWithinAngleTo(-other.Direction, angle));
	}
	#endregion

	#region Plane Intersection / Split / Incident Angle / Reflection / Distance / Closest Point
	/// <summary>
	/// Calculates where this line intersects <paramref name="plane"/>, if at all.
	/// </summary>
	/// <param name="plane">The plane to test against.</param>
	/// <returns><see langword="null"/> if this line is parallel to (and does not lie within) <paramref name="plane"/>; the intersection point otherwise.</returns>
	public Location? IntersectionWith(Plane plane) {
		var similarityToNormal = plane.Normal.Dot(Direction);
		if (similarityToNormal == 0f) return null; // Parallel with plane -- either infinite or zero answers. Return null either way

		var distance = (plane.PointClosestToOrigin - PointOnLine).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
		return LocationAtDistance(distance);
	}
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line is not parallel to <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to test against.</param>
	public Location FastIntersectionWith(Plane plane) {
		var similarityToNormal = plane.Normal.Dot(Direction);
		var distance = (plane.PointClosestToOrigin - PointOnLine).LengthWhenProjectedOnTo(plane.Normal) / similarityToNormal;
		return LocationAtDistance(distance);
	}
	/// <summary>
	/// Determines whether this line intersects <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The plane to test against.</param>
	public bool IsIntersectedBy(Plane plane) => plane.Normal.Dot(Direction) != 0f;

	/// <summary>
	/// Splits this line into two rays at the point it intersects <paramref name="plane"/>, if it does.
	/// </summary>
	/// <param name="plane">The plane to split this line at.</param>
	/// <returns><see langword="null"/> if this line does not intersect <paramref name="plane"/>; otherwise a pair of rays starting at the split point, one pointing in <see cref="Direction"/> and the other pointing the opposite way.</returns>
	public Pair<Ray, Ray>? SplitBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new(new Ray(intersectionPoint.Value, Direction), new Ray(intersectionPoint.Value, -Direction));
	}

	/// <summary>
	/// Executes the same function as <see cref="SplitBy(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line does intersect <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to split this line at.</param>
	public Pair<Ray, Ray> FastSplitBy(Plane plane) {
		var intersectionPoint = FastIntersectionWith(plane);
		return new(new Ray(intersectionPoint, Direction), new Ray(intersectionPoint, -Direction));
	}

	/// <summary>
	/// Calculates the angle at which this line meets <paramref name="plane"/>, if it intersects it.
	/// </summary>
	/// <param name="plane">The plane to measure against.</param>
	/// <returns><see langword="null"/> if this line does not intersect <paramref name="plane"/>; the incident angle otherwise.</returns>
	public Angle? IncidentAngleWith(Plane plane) => IsIntersectedBy(plane) ? plane.IncidentAngleWith(Direction) : null;
	/// <summary>
	/// Executes the same function as <see cref="IncidentAngleWith(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line does intersect <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to measure against.</param>
	public Angle FastIncidentAngleWith(Plane plane) => plane.FastIncidentAngleWith(Direction);

	/// <summary>
	/// Reflects this line off <paramref name="plane"/>, if it intersects it.
	/// </summary>
	/// <param name="plane">The plane to reflect off.</param>
	/// <returns><see langword="null"/> if this line does not intersect <paramref name="plane"/>; the reflected line otherwise, passing through the intersection point.</returns>
	public Line? ReflectedBy(Plane plane) {
		var intersectionPoint = IntersectionWith(plane);
		if (intersectionPoint == null) return null;
		return new Line(intersectionPoint.Value, Direction.FastReflectedBy(plane));
	}
	/// <summary>
	/// Executes the same function as <see cref="ReflectedBy(Plane)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line does intersect <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to reflect off.</param>
	public Line FastReflectedBy(Plane plane) => new(FastIntersectionWith(plane), Direction.FastReflectedBy(plane));

	/// <summary>
	/// Returns the point on this line that is closest to <paramref name="plane"/>.
	/// </summary>
	/// <remarks>
	/// If this line intersects <paramref name="plane"/>, every point on <paramref name="plane"/> is equally close, so the intersection point is returned; otherwise (i.e. this line is parallel to <paramref name="plane"/>) <see cref="PointOnLine"/> is returned, since every point on this line is then equally close. In most cases, <see cref="IntersectionWith(Plane)"/> and/or <see cref="DistanceFrom(Plane)"/> are more directly useful than this method or <see cref="ClosestPointOn(Plane)"/>.
	/// </remarks>
	/// <param name="plane">The plane to measure against.</param>
	public Location PointClosestTo(Plane plane) {
		// If we're parallel with the plane there are infinite answers so we just return the easiest one
		return IntersectionWith(plane) ?? PointOnLine;
	}
	/// <summary>
	/// Returns the point on <paramref name="plane"/> that is closest to this line.
	/// </summary>
	/// <remarks>
	/// If this line intersects <paramref name="plane"/>, the intersection point is returned; otherwise (i.e. this line is parallel to <paramref name="plane"/>) <see cref="Plane.PointClosestToOrigin"/> is returned, since every point on <paramref name="plane"/> is then equally close. In most cases, <see cref="IntersectionWith(Plane)"/> and/or <see cref="DistanceFrom(Plane)"/> are more directly useful than this method or <see cref="PointClosestTo(Plane)"/>.
	/// </remarks>
	/// <param name="plane">The plane to measure against.</param>
	public Location ClosestPointOn(Plane plane) {
		// If we're parallel with the plane there are infinite answers so we just return the easiest one
		return IntersectionWith(plane) ?? plane.PointClosestToOrigin;
	}

	/// <summary>
	/// Calculates the (unsigned) distance between this line and <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The plane to measure against.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => MathF.Abs(SignedDistanceFrom(plane));

	/// <summary>
	/// Calculates the distance between this line and <paramref name="plane"/>, signed according to which side of <paramref name="plane"/> this line falls on.
	/// </summary>
	/// <remarks>
	/// The sign matches <see cref="Plane.SignedDistanceFrom(Location)"/>: positive when this line is on the side <see cref="Plane.Normal"/> points towards, negative on the opposite side. If this line intersects <paramref name="plane"/>, the result is <c>0f</c>.
	/// </remarks>
	/// <param name="plane">The plane to measure against.</param>
	public float SignedDistanceFrom(Plane plane) {
		if (plane.Normal.Dot(Direction) != 0f) return 0f;
		var originToPlaneVect = (Vect) plane.PointClosestToOrigin;
		return ((Vect) PointOnLine).LengthWhenProjectedOnTo(originToPlaneVect.Direction) - originToPlaneVect.Length;
	}

	/// <summary>
	/// Determines how this line sits relative to <paramref name="plane"/>.
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizedWith(Direction dir) => ParallelizedWith(dir, 0f);
	/// <summary>
	/// Attempts to parallelize this line with <paramref name="dir"/>, pivoting around the point found by travelling <paramref name="pivotPointSignedDistance"/> along this line from <see cref="PointOnLine"/>.
	/// </summary>
	/// <param name="dir">The target direction.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this line is already exactly orthogonal to <paramref name="dir"/>); the parallelized result otherwise.</returns>
	public Line? ParallelizedWith(Direction dir, float pivotPointSignedDistance) {
		var newDir = Direction.ParallelizedWith(dir);
		if (newDir == null) return null;
		return new Line(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(Direction dir) => FastParallelizedWith(dir, 0f);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(Direction,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="dir"/> is not <see cref="Direction.None"/> and that this line is not already exactly orthogonal to it. The returned value of this function is undefined when either condition is broken.
	/// </remarks>
	/// <param name="dir">The target direction.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	public Line FastParallelizedWith(Direction dir, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastParallelizedWith(dir));
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizedAgainst(Direction dir) => OrthogonalizedAgainst(dir, 0f);
	/// <summary>
	/// Attempts to orthogonalize this line against <paramref name="dir"/>, pivoting around the point found by travelling <paramref name="pivotPointSignedDistance"/> along this line from <see cref="PointOnLine"/>.
	/// </summary>
	/// <param name="dir">The target direction.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this line is already exactly parallel to <paramref name="dir"/>); the orthogonalized result otherwise.</returns>
	public Line? OrthogonalizedAgainst(Direction dir, float pivotPointSignedDistance) {
		var newDir = Direction.OrthogonalizedAgainst(dir);
		if (newDir == null) return null;
		return new Line(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizedAgainst(Direction dir) => FastOrthogonalizedAgainst(dir, 0f);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(Direction,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="dir"/> is not <see cref="Direction.None"/> and that this line is not already exactly parallel to it. The returned value of this function is undefined when either condition is broken.
	/// </remarks>
	/// <param name="dir">The target direction.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	public Line FastOrthogonalizedAgainst(Direction dir, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastOrthogonalizedAgainst(dir));


	/// <summary>
	/// Equivalent to <see cref="ParallelizedWith(Direction,float)"/>, using <paramref name="line"/>'s direction.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	public Line? ParallelizedWith(Line line, float pivotPointSignedDistance) {
		var newDir = Direction.ParallelizedWith(line.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(Line,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/>'s direction is not <see cref="Direction.None"/> and that this line is not already exactly orthogonal to it. The returned value of this function is undefined when either condition is broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	public Line FastParallelizedWith(Line line, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastParallelizedWith(line.Direction));
	/// <inheritdoc cref="ParallelizedWith(Line,float)" />
	public Line? ParallelizedWith(Ray ray, float pivotPointSignedDistance) {
		var newDir = Direction.ParallelizedWith(ray.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	/// <inheritdoc cref="FastParallelizedWith(Line,float)" />
	public Line FastParallelizedWith(Ray ray, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastParallelizedWith(ray.Direction));
	/// <inheritdoc cref="ParallelizedWith(Line,float)" />
	public Line? ParallelizedWith(BoundedRay ray, float pivotPointSignedDistance) {
		var newDir = Direction.ParallelizedWith(ray.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	/// <inheritdoc cref="FastParallelizedWith(Line,float)" />
	public Line FastParallelizedWith(BoundedRay ray, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastParallelizedWith(ray.Direction));

	/// <summary>
	/// Equivalent to <see cref="OrthogonalizedAgainst(Direction,float)"/>, using <paramref name="line"/>'s direction.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	public Line? OrthogonalizedAgainst(Line line, float pivotPointSignedDistance) {
		var newDir = Direction.OrthogonalizedAgainst(line.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(Line,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/>'s direction is not <see cref="Direction.None"/> and that this line is not already exactly parallel to it. The returned value of this function is undefined when either condition is broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	public Line FastOrthogonalizedAgainst(Line line, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastOrthogonalizedAgainst(line.Direction));
	/// <inheritdoc cref="OrthogonalizedAgainst(Line,float)" />
	public Line? OrthogonalizedAgainst(Ray ray, float pivotPointSignedDistance) {
		var newDir = Direction.OrthogonalizedAgainst(ray.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	/// <inheritdoc cref="FastOrthogonalizedAgainst(Line,float)" />
	public Line FastOrthogonalizedAgainst(Ray ray, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastOrthogonalizedAgainst(ray.Direction));
	/// <inheritdoc cref="OrthogonalizedAgainst(Line,float)" />
	public Line? OrthogonalizedAgainst(BoundedRay ray, float pivotPointSignedDistance) {
		var newDir = Direction.OrthogonalizedAgainst(ray.Direction);
		return newDir == null ? null : new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	/// <inheritdoc cref="FastOrthogonalizedAgainst(Line,float)" />
	public Line FastOrthogonalizedAgainst(BoundedRay ray, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastOrthogonalizedAgainst(ray.Direction));


	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	/// <remarks>
	/// <see cref="PointOnLine"/> moves to its closest point on <paramref name="plane"/> and <see cref="Direction"/> is parallelized with <paramref name="plane"/> (see <see cref="ParallelizedWith(Plane)"/>) — so, unlike parallelizing alone, this also relocates the line to actually lie within <paramref name="plane"/>.
	/// </remarks>
	/// <param name="plane">The plane to project onto.</param>
	public Line? ProjectedOnTo(Plane plane) {
		var projectedDirection = Direction.ParallelizedWith(plane);
		return projectedDirection == null ? null : new Line(PointOnLine.ClosestPointOn(plane), projectedDirection.Value);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastProjectedOnTo(Plane plane) => new(PointOnLine.ClosestPointOn(plane), Direction.FastParallelizedWith(plane));

	/// <inheritdoc/>
	public Line? ParallelizedWith(Plane plane) => ParallelizedWith(plane, 0f);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(Plane plane) => FastParallelizedWith(plane, 0f);
	/// <summary>
	/// Attempts to parallelize this line with <paramref name="plane"/> (i.e. rotate it so it lies flat within the plane), pivoting around the point found by travelling <paramref name="pivotPointSignedDistance"/> along this line from <see cref="PointOnLine"/>.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this line is already exactly orthogonal to <paramref name="plane"/>); the parallelized result otherwise.</returns>
	public Line? ParallelizedWith(Plane plane, float pivotPointSignedDistance) {
		var projectedDirection = Direction.ParallelizedWith(plane);
		if (projectedDirection == null) return null;
		return new Line(LocationAtDistance(pivotPointSignedDistance), projectedDirection.Value);
	}
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(Plane,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line is not already exactly orthogonal to <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The target plane.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	public Line FastParallelizedWith(Plane plane, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastParallelizedWith(plane));

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizedAgainst(Plane plane) => OrthogonalizedAgainst(plane, 0f);
	/// <summary>
	/// Attempts to orthogonalize this line against <paramref name="plane"/> (i.e. rotate it so it points directly along <see cref="Plane.Normal"/>), pivoting around the point found by travelling <paramref name="pivotPointSignedDistance"/> along this line from <see cref="PointOnLine"/>.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this line is already exactly parallel to <paramref name="plane"/>); the orthogonalized result otherwise.</returns>
	public Line? OrthogonalizedAgainst(Plane plane, float pivotPointSignedDistance) {
		var newDir = Direction.OrthogonalizedAgainst(plane);
		if (newDir == null) return null;
		return new(LocationAtDistance(pivotPointSignedDistance), newDir.Value);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizedAgainst(Plane plane) => FastOrthogonalizedAgainst(plane, 0f);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(Plane,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line is not already exactly parallel to <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The target plane.</param>
	/// <param name="pivotPointSignedDistance">The distance along this line, from <see cref="PointOnLine"/>, of the point to pivot around.</param>
	public Line FastOrthogonalizedAgainst(Plane plane, float pivotPointSignedDistance) => new(LocationAtDistance(pivotPointSignedDistance), Direction.FastOrthogonalizedAgainst(plane));
	#endregion

	#region Clamping and Interpolation
	/// <inheritdoc/>
	public static Line Interpolate(Line start, Line end, float distance) {
		var startPoint = start.PointClosestTo(end);
		var endPoint = end.PointClosestTo(start);
		return new(
			Location.Interpolate(startPoint, endPoint, distance),
			Direction.Interpolate(start.Direction, end.Direction, distance)
		);
	}
	/// <inheritdoc/>
	public static Rotation CreateInterpolationPrecomputation(Line start, Line end) {
		return Direction.CreateInterpolationPrecomputation(start.Direction, end.Direction);
	}
	/// <inheritdoc/>
	public static Line InterpolateUsingPrecomputation(Line start, Line end, Rotation precomputation, float distance) {
		return new(
			Location.Interpolate(start.PointOnLine, end.PointOnLine, distance),
			Direction.InterpolateUsingPrecomputation(start.Direction, end.Direction, precomputation, distance)
		);
	}
	/// <inheritdoc/>
	public Line Clamp(Line min, Line max) {
		var startPoint = min.PointClosestTo(max);
		var endPoint = max.PointClosestTo(min);
		return new(
			new BoundedRay(startPoint, endPoint).PointClosestTo(this).Clamp(startPoint, endPoint),
			Direction.Clamp(min.Direction, max.Direction)
		);
	}
	#endregion
}