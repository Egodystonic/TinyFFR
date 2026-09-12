// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

/// <summary>
/// Describes how a <see cref="Plane"/> sits relative to some other object.
/// </summary>
public enum PlaneObjectRelationship {
	/// <summary>
	/// The plane passes through the object (or the object lies exactly on the plane).
	/// </summary>
	PlaneIntersectsObject,
	/// <summary>
	/// The object is entirely on the side of the plane that <see cref="Plane.Normal"/> points towards.
	/// </summary>
	PlaneFacesTowardsObject,
	/// <summary>
	/// The object is entirely on the side of the plane that <see cref="Plane.Normal"/> points away from.
	/// </summary>
	PlaneFacesAwayFromObject
}

partial struct Plane : 
	IInvertible<Plane>,
	ITranslatable<Plane>,
	IPointRotatable<Plane>,
	IDistanceMeasurable<Plane, Plane>,
	ISignedDistanceMeasurable<Plane, Location>, IContainer<Plane, Location>, IClosestEndogenousPointDiscoverable<Plane, Location>,
	IAngleMeasurable<Plane, Direction>, IReflectionTarget<Plane, Direction, Direction>, IParallelizationTarget<Plane, Direction>, IOrthogonalizationTarget<Plane, Direction>,
	IAngleMeasurable<Plane, Vect>, IReflectionTarget<Plane, Vect, Vect>, IProjectionTarget<Plane, Vect>, IParallelizationTarget<Plane, Vect>, IOrthogonalizationTarget<Plane, Vect>,
	IPrecomputationInterpolatable<Plane, Rotation>,
	IPhysicalValidityDeterminable {
	/// <summary>
	/// A sensible default angular tolerance to use when testing whether a direction or vector is approximately parallel or orthogonal to this plane.
	/// </summary>
	public const float DefaultParallelOrthogonalTestApproximationDegrees = Direction.DefaultParallelOrthogonalTestApproximationDegrees;

	/// <summary>
	/// Returns this plane with its <see cref="Normal"/> reversed, so it faces the opposite way while remaining in the same physical location.
	/// </summary>
	public Plane Flipped {
		get => new(-Normal, -_smallestDistanceFromOriginAlongNormal);
	}
	Plane IInvertible<Plane>.Inverted => Flipped;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator -(Plane operand) => operand.Flipped;

	/// <inheritdoc/>
	public bool IsPhysicallyValid => Normal.IsPhysicallyValidAndNotNone;

	#region Translation
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator +(Plane plane, Vect v) => plane.MovedBy(v);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator +(Vect v, Plane plane) => plane.MovedBy(v);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator -(Plane plane, Vect v) => plane.MovedBy(-v);
	/// <summary>
	/// Returns this plane moved by <paramref name="v"/>, i.e. with <paramref name="v"/> added to <see cref="Plane.PointClosestToOrigin"/>, keeping <see cref="Normal"/> unchanged.
	/// </summary>
	/// <param name="v">The vector to move this plane by.</param>
	public Plane MovedBy(Vect v) => new(Normal, PointClosestToOrigin + v);
	#endregion

	#region Rotation
	static Plane IMultiplyOperators<Plane, Rotation, Plane>.operator *(Plane left, Rotation right) => left.RotatedAroundOriginBy(right);
	static Plane IRotatable<Plane>.operator *(Rotation left, Plane right) => right.RotatedAroundOriginBy(left);
	Plane IRotatable<Plane>.RotatedBy(Rotation rot) => RotatedAroundOriginBy(rot);
	/// <inheritdoc/>
	public Plane RotatedAroundOriginBy(Rotation rot) => new(Normal * rot, PointClosestToOrigin.AsVect().RotatedBy(rot).AsLocation());
	Plane IRotatable<Plane>.RotatedBy(Quaternion rotQuat) => RotatedAroundOriginBy(rotQuat);
	/// <inheritdoc cref="RotatedAroundOriginBy(Rotation)" />
	public Plane RotatedAroundOriginBy(Quaternion rotQuat) => new(Normal.RotatedBy(rotQuat), PointClosestToOrigin.AsVect().RotatedBy(rotQuat).AsLocation());

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *(Plane plane, (Location Pivot, Rotation Rotation) rotTuple) => plane.RotatedBy(rotTuple.Rotation, rotTuple.Pivot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *((Location Pivot, Rotation Rotation) rotTuple, Plane plane) => plane.RotatedBy(rotTuple.Rotation, rotTuple.Pivot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *(Plane plane, (Rotation Rotation, Location Pivot) rotTuple) => plane.RotatedBy(rotTuple.Rotation, rotTuple.Pivot);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *((Rotation Rotation, Location Pivot) rotTuple, Plane plane) => plane.RotatedBy(rotTuple.Rotation, rotTuple.Pivot);
	/// <inheritdoc/>
	public Plane RotatedBy(Rotation rot, Location pivotPoint) => new(Normal * rot, PointClosestTo(pivotPoint) * (pivotPoint, rot));
	/// <inheritdoc/>
	public Plane RotatedBy(Quaternion rotQuat, Location pivotPoint) => new(Normal.RotatedBy(rotQuat), PointClosestTo(pivotPoint).RotatedBy(rotQuat, pivotPoint));
	#endregion

	#region Angle Measurement
	//0 to 1, where 1 is a direction completely perpendicular to the plane and 0 is completely parallel; is also the cosine of the angle formed with the normal
	float OrthogonalityWith(Direction direction) => MathF.Abs(Normal.Dot(direction));

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Direction dir) => plane.AngleTo(dir);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Direction dir, Plane plane) => plane.AngleTo(dir);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Vect v) => plane.AngleTo(v);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Vect v, Plane plane) => plane.AngleTo(v);
	/// <summary>
	/// Calculates the (unsigned) angle between <paramref name="p1"/> and <paramref name="p2"/>; equivalent to <c>p1.AngleTo(p2)</c>.
	/// </summary>
	/// <param name="p1">The first plane.</param>
	/// <param name="p2">The second plane.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane p1, Plane p2) => p1.AngleTo(p2);
	/// <summary>
	/// Calculates the (unsigned) angle between this plane and <paramref name="other"/>, i.e. the angle between their two <see cref="Normal"/>s (or its complement, whichever is smaller — this is always in the range <c>0° &lt;= n &lt;= 90°</c>).
	/// </summary>
	/// <param name="other">The other plane.</param>
	public Angle AngleTo(Plane other) => Angle.FromRadians(MathF.Acos(OrthogonalityWith(other.Normal)));
	/// <inheritdoc/>
	public Angle AngleTo(Direction direction) => direction != Direction.None ? Angle.FromRadians(MathF.Asin(OrthogonalityWith(direction))) : 0f;
	/// <inheritdoc/>
	public Angle AngleTo(Vect vect) => AngleTo(vect.Direction);
	/// <summary>
	/// Calculates the angle between this plane and <paramref name="direction"/>, signed according to which side of this plane <paramref name="direction"/> points towards.
	/// </summary>
	/// <remarks>
	/// This is in the range <c>-90° &lt;= n &lt;= 90°</c>: positive when <paramref name="direction"/> points (at least partly) towards the side <see cref="Normal"/> faces, negative when it points towards the opposite side, and (approximately) zero when it is parallel to this plane.
	/// </remarks>
	/// <param name="direction">The direction to measure against.</param>
	public Angle SignedAngleTo(Direction direction) => Angle.FromRadians(MathF.Asin(Normal.Dot(direction)));
	/// <inheritdoc cref="SignedAngleTo(Direction)" />
	public Angle SignedAngleTo(Vect vect) => SignedAngleTo(vect.Direction);
	#endregion

	#region Reflection / Incident Angle Measurement
	/// <inheritdoc/>
	public Angle? IncidentAngleWith(Direction direction) {
		var perpendicularity = OrthogonalityWith(direction);
		if (perpendicularity == 0f) return null;
		return Angle.FromRadians(MathF.Acos(perpendicularity));
	}
	/// <inheritdoc/>
	public Angle FastIncidentAngleWith(Direction direction) => Angle.FromRadians(MathF.Acos(OrthogonalityWith(direction)));
	/// <inheritdoc/>
	public Angle? IncidentAngleWith(Vect vect) => IncidentAngleWith(vect.Direction);
	/// <inheritdoc/>
	public Angle FastIncidentAngleWith(Vect vect) => FastIncidentAngleWith(vect.Direction);

	/// <inheritdoc/>
	public Direction? ReflectionOf(Direction direction) {
		if (direction.IsParallelTo(this)) return null;
		return FastReflectionOf(direction);
	}
	/// <inheritdoc/>
	public Vect? ReflectionOf(Vect vect) {
		if (vect.IsParallelTo(this)) return null;
		return FastReflectionOf(vect);
	}
	/// <summary>
	/// Executes the same function as <see cref="ReflectionOf(Direction)"/> but skips the check for whether <paramref name="direction"/> is parallel to this plane.
	/// </summary>
	/// <remarks>
	/// Unlike most other "Fast" variants in this codebase, this one is always safe to call: if <paramref name="direction"/> is exactly parallel to this plane, this function still returns <paramref name="direction"/> unchanged (rather than an undefined result), since that is the mathematically continuous answer as the input direction approaches parallel from either side.
	/// </remarks>
	/// <param name="direction">The direction to reflect.</param>
	public Direction FastReflectionOf(Direction direction) {
		return Direction.FromVector3(-2f * Vector3.Dot(Normal.ToVector3(), direction.ToVector3()) * Normal.ToVector3() + direction.ToVector3());
	}
	/// <summary>
	/// Executes the same function as <see cref="ReflectionOf(Vect)"/> but skips the check for whether <paramref name="vect"/> is parallel to this plane.
	/// </summary>
	/// <remarks>
	/// Unlike most other "Fast" variants in this codebase, this one is always safe to call: if <paramref name="vect"/> is exactly parallel to this plane, this function still returns <paramref name="vect"/> unchanged (rather than an undefined result), since that is the mathematically continuous answer as the input vector approaches parallel from either side.
	/// </remarks>
	/// <param name="vect">The vector to reflect.</param>
	public Vect FastReflectionOf(Vect vect) {
		return Vect.FromVector3(-2f * Vector3.Dot(Normal.ToVector3(), vect.ToVector3()) * Normal.ToVector3() + vect.ToVector3());
	}
	#endregion

	#region Parallelization / Orthogonalization / Projection
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction direction) => Normal.IsOrthogonalTo(direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction) => IsApproximatelyParallelTo(direction, DefaultParallelOrthogonalTestApproximationDegrees);
	/// <inheritdoc/>
	public bool IsApproximatelyParallelTo(Direction direction, Angle tolerance) => direction != Direction.None && AngleTo(direction).Equals(Angle.Zero, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Vect vect) => Normal.IsOrthogonalTo(vect);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Vect vect) => IsApproximatelyParallelTo(vect, DefaultParallelOrthogonalTestApproximationDegrees);
	/// <inheritdoc/>
	public bool IsApproximatelyParallelTo(Vect vect, Angle tolerance) => vect != Vect.Zero && AngleTo(vect).Equals(Angle.Zero, tolerance);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? ParallelizationOf(Direction direction) => direction.OrthogonalizedAgainst(Normal);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastParallelizationOf(Direction direction) => direction.FastOrthogonalizedAgainst(Normal);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ParallelizationOf(Vect vect) => vect.OrthogonalizedAgainst(Normal);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf(Vect)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// Unlike most other "Fast" variants in this codebase, this one is always safe to call: if <paramref name="vect"/> is exactly orthogonal to this plane (the one case where <see cref="ParallelizationOf(Vect)"/> would return <see langword="null"/>), this function instead returns a zero-length <see cref="Vect"/>, which is the expected/well-defined answer for that input rather than an undefined one.
	/// </remarks>
	/// <param name="vect">The vector to parallelize with this plane.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizationOf(Vect vect) => vect.FastOrthogonalizedAgainst(Normal);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction direction) => Normal.IsParallelTo(direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction) => IsApproximatelyOrthogonalTo(direction, DefaultParallelOrthogonalTestApproximationDegrees);
	/// <inheritdoc/>
	public bool IsApproximatelyOrthogonalTo(Direction direction, Angle tolerance) => direction != Direction.None && AngleTo(direction).Equals(Angle.QuarterCircle, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Vect vect) => Normal.IsParallelTo(vect);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Vect vect) => IsApproximatelyOrthogonalTo(vect, DefaultParallelOrthogonalTestApproximationDegrees);
	/// <inheritdoc/>
	public bool IsApproximatelyOrthogonalTo(Vect vect, Angle tolerance) => vect != Vect.Zero && AngleTo(vect).Equals(Angle.QuarterCircle, tolerance);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? OrthogonalizationOf(Direction direction) => direction.ParallelizedWith(Normal);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastOrthogonalizationOf(Direction direction) => direction.FastParallelizedWith(Normal);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizationOf(Vect vect) => vect.ParallelizedWith(Normal);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizationOf(Vect vect) => vect.FastParallelizedWith(Normal);

	/// <summary>
	/// Returns <paramref name="vect"/>'s projection on to this plane, i.e. <paramref name="vect"/> with the component of it that points along <see cref="Normal"/> removed.
	/// </summary>
	/// <remarks>
	/// Unlike the general <see cref="IProjectionTarget{TOther}"/>/<see cref="IProjectable{TSelf,TOther}"/> contract, this never returns <see langword="null"/>: if <paramref name="vect"/> is exactly orthogonal to this plane (i.e. parallel to <see cref="Normal"/>), the result is simply a zero-length <see cref="Vect"/> rather than an undefined value.
	/// </remarks>
	/// <param name="vect">The vector to project.</param>
	public Vect ProjectionOf(Vect vect) => vect - vect.ProjectedOnTo(Normal);
	Vect? IProjectionTarget<Vect>.ProjectionOf(Vect vect) => ProjectionOf(vect);
	Vect IProjectionTarget<Vect>.FastProjectionOf(Vect vect) => ProjectionOf(vect);
	#endregion

	#region Distance / Closest Point
	/// <inheritdoc/>
	public Location PointClosestTo(Location location) => location - PointClosestToOrigin.VectTo(location).ProjectedOnTo(Normal);
	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	/// <remarks>
	/// The sign matches <see cref="Normal"/>: positive when <paramref name="location"/> is on the side <see cref="Normal"/> points towards, negative on the opposite side, and (approximately) zero when <paramref name="location"/> lies on the plane.
	/// </remarks>
	/// <param name="location">The location to measure to.</param>
	public float SignedDistanceFrom(Location location) => Vector3.Dot(location.ToVector3(), _normal) - _smallestDistanceFromOriginAlongNormal;
	/// <inheritdoc/>
	public float DistanceFrom(Location location) => MathF.Abs(SignedDistanceFrom(location));
	float IDistanceMeasurable<Location>.DistanceSquaredFrom(Location location) {
		var distanceSqrt = DistanceFrom(location);
		return distanceSqrt * distanceSqrt;
	}
	/// <summary>
	/// Calculates the distance from the world origin (<c>(0f, 0f, 0f)</c>) to this plane, signed the same way as <see cref="SignedDistanceFrom(Location)"/>.
	/// </summary>
	public float SignedDistanceFromOrigin() => -_smallestDistanceFromOriginAlongNormal;
	/// <summary>
	/// Calculates the (unsigned) distance from the world origin (<c>(0f, 0f, 0f)</c>) to this plane.
	/// </summary>
	public float DistanceFromOrigin() => MathF.Abs(SignedDistanceFromOrigin());

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	/// <remarks>
	/// Two non-parallel planes always intersect somewhere, so this returns <c>0f</c> for any pair of planes that aren't parallel to each other; a meaningful non-zero distance is only ever returned between two parallel planes.
	/// </remarks>
	/// <param name="other">The other plane.</param>
	public float DistanceFrom(Plane other) => Normal.IsParallelTo(other.Normal) ? PointClosestToOrigin.DistanceFrom(other.PointClosestToOrigin) : 0f;
	/// <inheritdoc cref="DistanceFrom(Plane)" />
	public float DistanceSquaredFrom(Plane other) => Normal.IsParallelTo(other.Normal) ? PointClosestToOrigin.DistanceSquaredFrom(other.PointClosestToOrigin) : 0f;

	/// <summary>
	/// Determines whether <paramref name="other"/>'s <see cref="Normal"/> is within <paramref name="angle"/> of this plane's, and whether <paramref name="other"/>'s <see cref="PointClosestToOrigin"/> is within <paramref name="distance"/> of this one's.
	/// </summary>
	/// <param name="other">The other plane to compare to.</param>
	/// <param name="distance">The maximum permitted distance between the two planes' <see cref="PointClosestToOrigin"/> values.</param>
	/// <param name="angle">The maximum permitted angle between the two planes' <see cref="Normal"/> values.</param>
	public bool IsWithinDistanceAndAngleTo(Plane other, float distance, Angle angle) => Normal.IsWithinAngleTo(other.Normal, angle) && PointClosestToOrigin.IsWithinDistanceOf(other.PointClosestToOrigin, distance);
	#endregion

	#region Relationship / Containment
	// Implementation note: We use a plane thickness by default because relying on the signed distance being exactly 0 for anything other than axis-aligned planes is pretty much stochastic
	// due to FP inaccuracy. Even for axis-aligned ones it's still pretty bad, but is possibly more consistent when moving around on the surface of the plane. In these cases, if users
	// really want 0-thickness planes, they can still specify as such using the overloads that take a thickness parameter.
	/// <summary>
	/// Determines whether <paramref name="location"/> is on the side of this plane that <see cref="Normal"/> points towards, using <see cref="DefaultPlaneThickness"/>.
	/// </summary>
	/// <remarks>
	/// A point that lies on the plane itself (within <see cref="DefaultPlaneThickness"/>) returns <see langword="false"/> from both this method and <see cref="FacesAwayFrom(Location)"/>.
	/// </remarks>
	/// <param name="location">The location to test.</param>
	public bool FacesTowards(Location location) => FacesTowards(location, DefaultPlaneThickness);
	/// <summary>
	/// Determines whether <paramref name="location"/> is on the side of this plane that <see cref="Normal"/> points away from, using <see cref="DefaultPlaneThickness"/>.
	/// </summary>
	/// <remarks>
	/// A point that lies on the plane itself (within <see cref="DefaultPlaneThickness"/>) returns <see langword="false"/> from both this method and <see cref="FacesTowards(Location)"/>.
	/// </remarks>
	/// <param name="location">The location to test.</param>
	public bool FacesAwayFrom(Location location) => FacesAwayFrom(location, DefaultPlaneThickness);
	/// <summary>
	/// Determines whether <paramref name="location"/> is on the side of this plane that <see cref="Normal"/> points towards.
	/// </summary>
	/// <remarks>
	/// A point that lies on the plane itself (within <paramref name="planeThickness"/>) returns <see langword="false"/> from both this method and <see cref="FacesAwayFrom(Location,float)"/>.
	/// </remarks>
	/// <param name="location">The location to test.</param>
	/// <param name="planeThickness">How close to the plane <paramref name="location"/> can be while still being considered "on" it (and therefore facing neither way).</param>
	public bool FacesTowards(Location location, float planeThickness) => SignedDistanceFrom(location) > planeThickness;
	/// <summary>
	/// Determines whether <paramref name="location"/> is on the side of this plane that <see cref="Normal"/> points away from.
	/// </summary>
	/// <remarks>
	/// A point that lies on the plane itself (within <paramref name="planeThickness"/>) returns <see langword="false"/> from both this method and <see cref="FacesTowards(Location,float)"/>.
	/// </remarks>
	/// <param name="location">The location to test.</param>
	/// <param name="planeThickness">How close to the plane <paramref name="location"/> can be while still being considered "on" it (and therefore facing neither way).</param>
	public bool FacesAwayFrom(Location location, float planeThickness) => SignedDistanceFrom(location) < -planeThickness;
	/// <inheritdoc cref="FacesTowards(Location)" />
	public bool FacesTowardsOrigin() => FacesTowardsOrigin(DefaultPlaneThickness);
	/// <inheritdoc cref="FacesAwayFrom(Location)" />
	public bool FacesAwayFromOrigin() => FacesAwayFromOrigin(DefaultPlaneThickness);
	/// <inheritdoc cref="FacesTowards(Location,float)" />
	public bool FacesTowardsOrigin(float planeThickness) => SignedDistanceFromOrigin() > planeThickness;
	/// <inheritdoc cref="FacesAwayFrom(Location,float)" />
	public bool FacesAwayFromOrigin(float planeThickness) => SignedDistanceFromOrigin() < -planeThickness;

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, DefaultPlaneThickness);
	/// <summary>
	/// Determines whether <paramref name="location"/> lies on this plane, within <paramref name="planeThickness"/>.
	/// </summary>
	/// <param name="location">The location to test.</param>
	/// <param name="planeThickness">How far from the plane <paramref name="location"/> is permitted to be and still be considered contained within it.</param>
	public bool Contains(Location location, float planeThickness) => DistanceFrom(location) <= planeThickness;
	#endregion

	#region Intersection
	/// <summary>
	/// Determines whether this plane intersects <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// Two planes intersect (along a line) whenever they are not parallel to each other.
	/// </remarks>
	/// <param name="other">The other plane to test against.</param>
	public bool IsIntersectedBy(Plane other) => Vector3.Cross(_normal, other._normal).LengthSquared() != 0f;
	/// <summary>
	/// Calculates the line along which this plane and <paramref name="other"/> intersect, if they do.
	/// </summary>
	/// <param name="other">The other plane to test against.</param>
	/// <returns><see langword="null"/> if this plane is parallel to <paramref name="other"/>; the line of intersection otherwise.</returns>
	public Line? IntersectionWith(Plane other) {
		static (float A, float B) FindNonZeroComponents(float thisA, float thisB, float thisCoefficient, float otherA, float otherB, float otherCoefficient) {
			var divisor = thisA * otherB - otherA * thisB;

			return (
				(otherB * thisCoefficient - thisB * otherCoefficient) / divisor,
				(thisA * otherCoefficient - otherA * thisCoefficient) / divisor
			);
		}

		var lineDirection = Vector3.Cross(_normal, other._normal);
		if (lineDirection.LengthSquared() == 0f) return null; // parallel planes

		var dirXAbs = MathF.Abs(lineDirection.X);
		var dirYAbs = MathF.Abs(lineDirection.Y);
		var dirZAbs = MathF.Abs(lineDirection.Z);

		if (dirXAbs > dirYAbs) {
			if (dirXAbs > dirZAbs) goto calculateUsingZeroX;
			else goto calculateUsingZeroZ;
		}
		else if (dirYAbs > dirZAbs) goto calculateUsingZeroY;
		else goto calculateUsingZeroZ;

		calculateUsingZeroX:
		{
			var (y, z) = FindNonZeroComponents(_normal.Y, _normal.Z, _smallestDistanceFromOriginAlongNormal, other._normal.Y, other._normal.Z, other._smallestDistanceFromOriginAlongNormal);
			return new Line((0f, y, z), Direction.FromVector3(lineDirection));
		}

		calculateUsingZeroY:
		{
			var (x, z) = FindNonZeroComponents(_normal.X, _normal.Z, _smallestDistanceFromOriginAlongNormal, other._normal.X, other._normal.Z, other._smallestDistanceFromOriginAlongNormal);
			return new Line((x, 0f, z), Direction.FromVector3(lineDirection));
		}

		calculateUsingZeroZ:
		{
			var (x, y) = FindNonZeroComponents(_normal.X, _normal.Y, _smallestDistanceFromOriginAlongNormal, other._normal.X, other._normal.Y, other._smallestDistanceFromOriginAlongNormal);
			return new Line((x, y, 0f), Direction.FromVector3(lineDirection));
		}
	}
	#endregion

	#region Clamping and Interpolation
	/// <inheritdoc/>
	public static Plane Interpolate(Plane start, Plane end, float distance) {
		return new(
			Direction.Interpolate(start.Normal, end.Normal, distance),
			Location.Interpolate(start.PointClosestToOrigin, end.PointClosestToOrigin, distance)
		);
	}

	/// <inheritdoc/>
	public Plane Clamp(Plane min, Plane max) {
		return new(
			Normal.Clamp(min.Normal, max.Normal),
			PointClosestToOrigin.Clamp(min.PointClosestToOrigin, max.PointClosestToOrigin)
		);
	}
	/// <inheritdoc/>
	public static Rotation CreateInterpolationPrecomputation(Plane start, Plane end) => Direction.CreateInterpolationPrecomputation(start.Normal, end.Normal);
	/// <inheritdoc/>
	public static Plane InterpolateUsingPrecomputation(Plane start, Plane end, Rotation precomputation, float distance) {
		return new(
			Direction.InterpolateUsingPrecomputation(start.Normal, end.Normal, precomputation, distance),
			Location.Interpolate(start.PointClosestToOrigin, end.PointClosestToOrigin, distance)
		);
	}
	#endregion

	#region Dimension Conversion
	/// <summary>
	/// Creates a <see cref="DimensionConverter"/> that maps between this plane's surface and a 2D coordinate space, with the 2D origin at <see cref="PointClosestToOrigin"/> and arbitrarily-chosen (but consistent) X/Y axes lying within the plane.
	/// </summary>
	/// <remarks>
	/// The returned converter is only valid for this specific plane — constructed from an arbitrary orthogonal basis of <see cref="Normal"/>, it has no meaning relative to any other plane.
	/// </remarks>
	public DimensionConverter CreateDimensionConverter() {
		var xBasis = Normal.AnyOrthogonal();
		var yBasis = Direction.FromDualOrthogonalization(Normal, xBasis);
		var origin = PointClosestToOrigin;
		return new(xBasis, yBasis, Normal, origin);
	}
	/// <summary>
	/// Creates a <see cref="DimensionConverter"/> that maps between this plane's surface and a 2D coordinate space, with the 2D origin at <paramref name="twoDimensionalCoordinateOrigin"/>'s closest point on this plane and arbitrarily-chosen (but consistent) X/Y axes lying within the plane.
	/// </summary>
	/// <remarks>
	/// The returned converter is only valid for this specific plane — constructed from an arbitrary orthogonal basis of <see cref="Normal"/>, it has no meaning relative to any other plane.
	/// </remarks>
	/// <param name="twoDimensionalCoordinateOrigin">The point (not necessarily on this plane) whose closest point on this plane becomes the 2D coordinate space's origin.</param>
	public DimensionConverter CreateDimensionConverter(Location twoDimensionalCoordinateOrigin) {
		var xBasis = Normal.AnyOrthogonal();
		var yBasis = Direction.FromDualOrthogonalization(Normal, xBasis);
		var origin = PointClosestTo(twoDimensionalCoordinateOrigin);
		return new(xBasis, yBasis, Normal, origin);
	}
	/// <summary>
	/// Creates a <see cref="DimensionConverter"/> that maps between this plane's surface and a 2D coordinate space, with the 2D origin at <paramref name="twoDimensionalCoordinateOrigin"/>'s closest point on this plane and the 2D X axis aligned with <paramref name="twoDimensionalCoordinateXAxis"/>.
	/// </summary>
	/// <remarks>
	/// The returned converter is only valid for this specific plane. If <paramref name="twoDimensionalCoordinateXAxis"/> is exactly orthogonal to this plane (and so can't be parallelized with it), an arbitrary orthogonal basis of <see cref="Normal"/> is used instead, just as with <see cref="CreateDimensionConverter(Location)"/>. If you need full control over the resultant basis (including a skewed one), construct a <see cref="DimensionConverter"/> directly via its constructor instead.
	/// </remarks>
	/// <param name="twoDimensionalCoordinateOrigin">The point (not necessarily on this plane) whose closest point on this plane becomes the 2D coordinate space's origin.</param>
	/// <param name="twoDimensionalCoordinateXAxis">The direction the 2D X axis should point in (once parallelized with this plane).</param>
	public DimensionConverter CreateDimensionConverter(Location twoDimensionalCoordinateOrigin, Direction twoDimensionalCoordinateXAxis) {
		var xBasis = ParallelizationOf(twoDimensionalCoordinateXAxis) ?? Normal.AnyOrthogonal();
		var yBasis = Direction.FromDualOrthogonalization(Normal, xBasis);
		var origin = PointClosestTo(twoDimensionalCoordinateOrigin);
		return new(xBasis, yBasis, Normal, origin);
	}
	/// <summary>
	/// Creates a <see cref="DimensionConverter"/> that maps between this plane's surface and a 2D coordinate space, with the 2D origin at <paramref name="twoDimensionalCoordinateOrigin"/>'s closest point on this plane and the 2D X/Y axes aligned with <paramref name="twoDimensionalCoordinateXAxis"/>/<paramref name="twoDimensionalCoordinateYAxis"/>.
	/// </summary>
	/// <remarks>
	/// The returned converter is only valid for this specific plane. If either axis is exactly orthogonal to this plane, or the two axes end up parallel to each other once both are parallelized with this plane, an arbitrary orthogonal basis is substituted for the affected axis/axes instead (mirroring <see cref="CreateDimensionConverter(Location,Direction)"/>'s fallback). If you need full control over the resultant basis (including a skewed one), construct a <see cref="DimensionConverter"/> directly via its constructor instead.
	/// </remarks>
	/// <param name="twoDimensionalCoordinateOrigin">The point (not necessarily on this plane) whose closest point on this plane becomes the 2D coordinate space's origin.</param>
	/// <param name="twoDimensionalCoordinateXAxis">The direction the 2D X axis should point in (once parallelized with this plane).</param>
	/// <param name="twoDimensionalCoordinateYAxis">The direction the 2D Y axis should point in (once parallelized with this plane and orthogonalized against the X axis).</param>
	public DimensionConverter CreateDimensionConverter(Location twoDimensionalCoordinateOrigin, Direction twoDimensionalCoordinateXAxis, Direction twoDimensionalCoordinateYAxis) {
		var xBasis = ParallelizationOf(twoDimensionalCoordinateXAxis) ?? Normal.AnyOrthogonal();
		var yBasis = ParallelizationOf(twoDimensionalCoordinateYAxis)?.OrthogonalizedAgainst(xBasis) ?? Direction.FromDualOrthogonalization(xBasis, Normal);
		var origin = PointClosestTo(twoDimensionalCoordinateOrigin);
		return new(xBasis, yBasis, Normal, origin);
	}
	#endregion
}