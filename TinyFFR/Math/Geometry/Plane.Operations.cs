// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

partial struct Plane : 
	IInvertible<Plane>,
	ITranslatable<Plane>,
	IPointRotatable<Plane>,
	IDistanceMeasurable<Plane, Plane>,
	ISignedDistanceMeasurable<Plane, Location>, IContainer<Plane, Location>, IClosestEndogenousPointDiscoverable<Plane, Location>,
	IAngleMeasurable<Plane, Direction>, IReflectionTarget<Plane, Direction, Direction>, IParallelizationTarget<Plane, Direction>, IOrthogonalizationTarget<Plane, Direction>,
	IAngleMeasurable<Plane, Vect>, IReflectionTarget<Plane, Vect, Vect>, IProjectionTarget<Plane, Vect>, IParallelizationTarget<Plane, Vect>, IOrthogonalizationTarget<Plane, Vect>,
	IPrecomputationInterpolatable<Plane, Rotation> {
	public const float DefaultParallelOrthogonalTestApproximationDegrees = Direction.DefaultParallelOrthogonalTestApproximationDegrees;

	public Plane Flipped {
		get => new(-Normal, -_smallestDistanceFromOriginAlongNormal);
	}
	Plane IInvertible<Plane>.Inverted => Flipped;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator -(Plane operand) => operand.Flipped;

	#region Translation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator +(Plane plane, Vect v) => plane.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator +(Vect v, Plane plane) => plane.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator -(Plane plane, Vect v) => plane.MovedBy(-v);
	public Plane MovedBy(Vect v) => new(Normal, PointClosestToOrigin + v);
	#endregion

	#region Rotation
	public Plane RotatedAroundOriginBy(Rotation rot) => new(Normal * rot, PointClosestToOrigin.AsVect().RotatedBy(rot).AsLocation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *(Plane plane, (Location Pivot, Rotation Rotation) rotTuple) => plane.RotatedBy(rotTuple.Rotation, rotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *((Location Pivot, Rotation Rotation) rotTuple, Plane plane) => plane.RotatedBy(rotTuple.Rotation, rotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *(Plane plane, (Rotation Rotation, Location Pivot) rotTuple) => plane.RotatedBy(rotTuple.Rotation, rotTuple.Pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *((Rotation Rotation, Location Pivot) rotTuple, Plane plane) => plane.RotatedBy(rotTuple.Rotation, rotTuple.Pivot);
	public Plane RotatedBy(Rotation rot, Location pivotPoint) => new(Normal * rot, PointClosestTo(pivotPoint) * (pivotPoint, rot));
	#endregion

	#region Angle Measurement
	//0 to 1, where 1 is a direction completely perpendicular to the plane and 0 is completely parallel; is also the cosine of the angle formed with the normal
	float OrthogonalityWith(Direction direction) => MathF.Abs(Normal.Dot(direction));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Direction dir) => plane.AngleTo(dir);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Direction dir, Plane plane) => plane.AngleTo(dir);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Vect v) => plane.AngleTo(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Vect v, Plane plane) => plane.AngleTo(v);
	public Angle AngleTo(Plane other) => Angle.FromRadians(MathF.Acos(OrthogonalityWith(other.Normal)));
	public Angle AngleTo(Direction direction) => direction != Direction.None ? Angle.FromRadians(MathF.Asin(OrthogonalityWith(direction))) : 0f;
	public Angle AngleTo(Vect vect) => AngleTo(vect.Direction);
	public Angle SignedAngleTo(Direction direction) => Angle.FromRadians(MathF.Asin(Normal.Dot(direction)));
	public Angle SignedAngleTo(Vect vect) => SignedAngleTo(vect.Direction);
	#endregion

	#region Reflection / Incident Angle Measurement
	public Angle? IncidentAngleWith(Direction direction) {
		var perpendicularity = OrthogonalityWith(direction);
		if (perpendicularity == 0f) return null;
		return Angle.FromRadians(MathF.Acos(perpendicularity));
	}
	public Angle FastIncidentAngleWith(Direction direction) => Angle.FromRadians(MathF.Acos(OrthogonalityWith(direction)));
	public Angle? IncidentAngleWith(Vect vect) => IncidentAngleWith(vect.Direction);
	public Angle FastIncidentAngleWith(Vect vect) => FastIncidentAngleWith(vect.Direction);

	public Direction? ReflectionOf(Direction direction) {
		if (direction.IsParallelTo(this)) return null;
		return FastReflectionOf(direction);
	}
	public Vect? ReflectionOf(Vect vect) {
		if (vect.IsParallelTo(this)) return null;
		return FastReflectionOf(vect);
	}
	public Direction FastReflectionOf(Direction direction) { // TODO explain in XML that this returns the same direction if the input is parallel to the plane (this is okay as it's continuous across the whole range, so is the expected answer, just need to note it)
		return Direction.FromVector3(-2f * Vector3.Dot(Normal.ToVector3(), direction.ToVector3()) * Normal.ToVector3() + direction.ToVector3());
	}
	public Vect FastReflectionOf(Vect vect) { // TODO explain in XML that this returns the same direction if the input is parallel to the plane (this is okay as it's continuous across the whole range, so is the expected answer, just need to note it)
		return Vect.FromVector3(-2f * Vector3.Dot(Normal.ToVector3(), vect.ToVector3()) * Normal.ToVector3() + vect.ToVector3());
	}
	#endregion

	#region Parallelization / Orthogonalization / Projection
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction direction) => Normal.IsOrthogonalTo(direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction) => IsApproximatelyParallelTo(direction, DefaultParallelOrthogonalTestApproximationDegrees);
	public bool IsApproximatelyParallelTo(Direction direction, Angle tolerance) => direction != Direction.None && AngleTo(direction).Equals(Angle.Zero, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Vect vect) => Normal.IsOrthogonalTo(vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Vect vect) => IsApproximatelyParallelTo(vect, DefaultParallelOrthogonalTestApproximationDegrees);
	public bool IsApproximatelyParallelTo(Vect vect, Angle tolerance) => vect != Vect.Zero && AngleTo(vect).Equals(Angle.Zero, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? ParallelizationOf(Direction direction) => direction.OrthogonalizedAgainst(Normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastParallelizationOf(Direction direction) => direction.FastOrthogonalizedAgainst(Normal);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ParallelizationOf(Vect vect) => vect.OrthogonalizedAgainst(Normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizationOf(Vect vect) => vect.FastOrthogonalizedAgainst(Normal); // TODO in xmldoc mention that length will be 0 if this is perpendicular. NOT undefined

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction direction) => Normal.IsParallelTo(direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction) => IsApproximatelyOrthogonalTo(direction, DefaultParallelOrthogonalTestApproximationDegrees);
	public bool IsApproximatelyOrthogonalTo(Direction direction, Angle tolerance) => direction != Direction.None && AngleTo(direction).Equals(Angle.QuarterCircle, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Vect vect) => Normal.IsParallelTo(vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Vect vect) => IsApproximatelyOrthogonalTo(vect, DefaultParallelOrthogonalTestApproximationDegrees);
	public bool IsApproximatelyOrthogonalTo(Vect vect, Angle tolerance) => vect != Vect.Zero && AngleTo(vect).Equals(Angle.QuarterCircle, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? OrthogonalizationOf(Direction direction) => direction.ParallelizedWith(Normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastOrthogonalizationOf(Direction direction) => direction.FastParallelizedWith(Normal);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizationOf(Vect vect) => vect.ParallelizedWith(Normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizationOf(Vect vect) => vect.FastParallelizedWith(Normal);

	public Vect ProjectionOf(Vect vect) => vect - vect.ProjectedOnTo(Normal); // TODO in xmldoc mention that length will be 0 if this is perpendicular
	Vect? IProjectionTarget<Vect>.ProjectionOf(Vect vect) => ProjectionOf(vect);
	Vect IProjectionTarget<Vect>.FastProjectionOf(Vect vect) => ProjectionOf(vect);
	#endregion

	#region Distance / Closest Point
	public Location PointClosestTo(Location location) => location - PointClosestToOrigin.VectTo(location).ProjectedOnTo(Normal);
	public float SignedDistanceFrom(Location location) => Vector3.Dot(location.ToVector3(), _normal) - _smallestDistanceFromOriginAlongNormal; // TODO xmldoc positive means normal faces towards, etc
	public float DistanceFrom(Location location) => MathF.Abs(SignedDistanceFrom(location));
	float IDistanceMeasurable<Location>.DistanceSquaredFrom(Location location) {
		var distanceSqrt = DistanceFrom(location);
		return distanceSqrt * distanceSqrt;
	}
	public float SignedDistanceFromOrigin() => -_smallestDistanceFromOriginAlongNormal;
	public float DistanceFromOrigin() => MathF.Abs(SignedDistanceFromOrigin());

	// TODO xmldoc make it clear that these will almost always be 0
	public float DistanceFrom(Plane other) => Normal.IsParallelTo(other.Normal) ? PointClosestToOrigin.DistanceFrom(other.PointClosestToOrigin) : 0f;
	public float DistanceSquaredFrom(Plane other) => Normal.IsParallelTo(other.Normal) ? PointClosestToOrigin.DistanceSquaredFrom(other.PointClosestToOrigin) : 0f;
	#endregion

	#region Relationship / Containment
	// TODO in Xml make it clear that "facing towards" means the normal is pointing out of this side of the plane; and that points on the plane (within the thickness value) will return false
	// Implementation note: We use a plane thickness by default because relying on the signed distance being exactly 0 for anything other than axis-aligned planes is pretty much stochastic
	// due to FP inaccuracy. Even for axis-aligned ones it's still pretty bad, but is possibly more consistent when moving around on the surface of the plane. In these cases, if users
	// really want 0-thickness planes, they can still specify as such using the overloads that take a thickness parameter.
	public bool FacesTowards(Location location) => FacesTowards(location, DefaultPlaneThickness);
	public bool FacesAwayFrom(Location location) => FacesAwayFrom(location, DefaultPlaneThickness);
	public bool FacesTowards(Location location, float planeThickness) => SignedDistanceFrom(location) > planeThickness;
	public bool FacesAwayFrom(Location location, float planeThickness) => SignedDistanceFrom(location) < -planeThickness;
	public bool FacesTowardsOrigin() => FacesTowardsOrigin(DefaultPlaneThickness);
	public bool FacesAwayFromOrigin() => FacesAwayFromOrigin(DefaultPlaneThickness);
	public bool FacesTowardsOrigin(float planeThickness) => SignedDistanceFromOrigin() > planeThickness;
	public bool FacesAwayFromOrigin(float planeThickness) => SignedDistanceFromOrigin() < -planeThickness;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, DefaultPlaneThickness);
	public bool Contains(Location location, float planeThickness) => DistanceFrom(location) <= planeThickness;
	#endregion

	#region Intersection
	public bool IsIntersectedBy(Plane other) => Vector3.Cross(_normal, other._normal).LengthSquared() != 0f;
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
	public static Plane Interpolate(Plane start, Plane end, float distance) {
		return new(
			Direction.Interpolate(start.Normal, end.Normal, distance),
			Location.Interpolate(start.PointClosestToOrigin, end.PointClosestToOrigin, distance)
		);
	}

	public Plane Clamp(Plane min, Plane max) {
		return new(
			Normal.Clamp(min.Normal, max.Normal),
			PointClosestToOrigin.Clamp(min.PointClosestToOrigin, max.PointClosestToOrigin)
		);
	}
	public static Rotation CreateInterpolationPrecomputation(Plane start, Plane end) => Direction.CreateInterpolationPrecomputation(start.Normal, end.Normal);
	public static Plane InterpolateUsingPrecomputation(Plane start, Plane end, Rotation precomputation, float distance) {
		return new(
			Direction.InterpolateUsingPrecomputation(start.Normal, end.Normal, precomputation, distance),
			Location.Interpolate(start.PointClosestToOrigin, end.PointClosestToOrigin, distance)
		);
	}
	#endregion

	#region Dimension Conversion
	// TODO xmldoc that this converter only works for the plane it was generated for
	public DimensionConverter CreateDimensionConverter() {
		var xBasis = Normal.AnyOrthogonal();
		var yBasis = Direction.FromDualOrthogonalization(Normal, xBasis);
		var origin = PointClosestToOrigin;
		return new(xBasis, yBasis, Normal, origin);
	}
	public DimensionConverter CreateDimensionConverter(Location twoDimensionalCoordinateOrigin) {
		var xBasis = Normal.AnyOrthogonal();
		var yBasis = Direction.FromDualOrthogonalization(Normal, xBasis);
		var origin = PointClosestTo(twoDimensionalCoordinateOrigin);
		return new(xBasis, yBasis, Normal, origin);
	}
	// TODO xmldoc what happens if axis is ortho to plane and also that people who want to skip all the checks or create a skewed basis etc can create their own converter directly with the ctor
	public DimensionConverter CreateDimensionConverter(Location twoDimensionalCoordinateOrigin, Direction twoDimensionalCoordinateXAxis) {
		var xBasis = ParallelizationOf(twoDimensionalCoordinateXAxis) ?? Normal.AnyOrthogonal();
		var yBasis = Direction.FromDualOrthogonalization(Normal, xBasis);
		var origin = PointClosestTo(twoDimensionalCoordinateOrigin);
		return new(xBasis, yBasis, Normal, origin);
	}
	// TODO xmldoc what happens if either axis is ortho to plane or parallel to each other and also that people who want to skip all the checks or create a skewed basis etc can create their own converter directly with the ctor
	public DimensionConverter CreateDimensionConverter(Location twoDimensionalCoordinateOrigin, Direction twoDimensionalCoordinateXAxis, Direction twoDimensionalCoordinateYAxis) {
		var xBasis = ParallelizationOf(twoDimensionalCoordinateXAxis) ?? Normal.AnyOrthogonal();
		var yBasis = ParallelizationOf(twoDimensionalCoordinateYAxis)?.OrthogonalizedAgainst(xBasis) ?? Direction.FromDualOrthogonalization(xBasis, Normal);
		var origin = PointClosestTo(twoDimensionalCoordinateOrigin);
		return new(xBasis, yBasis, Normal, origin);
	}
	// TODO xmldoc that these methods are slower than using a converter unless doing it literally only once
	public XYPair<float> ProjectionTo2DOf(Location location) => CreateDimensionConverter().ConvertLocation(location);
	public Location HolographTo3DOf(XYPair<float> xyPair) => CreateDimensionConverter().ConvertLocation(xyPair);
	public Location HolographTo3DOf(XYPair<float> xyPair, float zDimension) => CreateDimensionConverter().ConvertLocation(xyPair, zDimension);
	#endregion
}