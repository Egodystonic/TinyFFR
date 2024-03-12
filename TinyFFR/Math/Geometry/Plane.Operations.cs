// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct Plane : 
	IAdditionOperators<Plane, Vect, Plane>,
	IMultiplyOperators<Plane, (Location Pivot, Rotation Rotation), Plane>,
	IMultiplyOperators<Plane, (Rotation Rotation, Location Pivot), Plane>,
	IUnaryNegationOperators<Plane, Plane> {

	public Plane Reversed {
		get => new(-_normal, -_coefficientOfNormalToCreateMinimalVectFromPlaneToOrigin);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator -(Plane operand) => operand.Reversed;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator +(Plane plane, Vect v) => plane.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator +(Vect v, Plane plane) => plane.MovedBy(v);
	public Plane MovedBy(Vect v) => new(Normal, PointClosestToOrigin + v);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *(Plane plane, (Location Pivot, Rotation Rotation) rotTuple) => plane.RotatedAround(rotTuple.Pivot, rotTuple.Rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *((Location Pivot, Rotation Rotation) rotTuple, Plane plane) => plane.RotatedAround(rotTuple.Pivot, rotTuple.Rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *(Plane plane, (Rotation Rotation, Location Pivot) rotTuple) => plane.RotatedAround(rotTuple.Pivot, rotTuple.Rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *((Rotation Rotation, Location Pivot) rotTuple, Plane plane) => plane.RotatedAround(rotTuple.Pivot, rotTuple.Rotation);
	public Plane RotatedAround(Location pivotPoint, Rotation rot) => new(Normal * rot, ClosestPointTo(pivotPoint) * (pivotPoint, rot));


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane lhs, Plane rhs) => lhs.AngleTo(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Line line) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Line line, Plane plane) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Ray line) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Ray line, Plane plane) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, BoundedLine line) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(BoundedLine line, Plane plane) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Direction dir) => plane.AngleTo(dir);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Direction dir, Plane plane) => plane.AngleTo(dir);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane other) => Normal.AngleTo(other.Normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo<TLine>(TLine line) where TLine : ILine => AngleTo(line.Direction);
	public Angle AngleTo(Direction direction) => Angle.FromRadians(1f - MathF.Acos(MathF.Abs(Normal.SimilarityTo(direction))));
	public Direction Reflect(Direction direction) { // TODO explain in XML that this returns the same direction if the input is parallel to the plane
		return Direction.FromVector3(-2f * Vector3.Dot(Normal.ToVector3(), direction.ToVector3()) * Normal.ToVector3() + direction.ToVector3());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TSplit? Reflect<TLine, TSplit>(TLine line) where TLine : ILine<TLine, TSplit> where TSplit : struct, ILine<TSplit> => line.ReflectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TrySplit<TLine, TSplit>(TLine line, out BoundedLine outBeforePlane, out TSplit outAfterPlane) where TLine : ILine<TLine, TSplit> where TSplit : struct, ILine<TSplit> => line.TrySplit(this, out outBeforePlane, out outAfterPlane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TLine ProjectLine<TLine>(TLine line) where TLine : ILine<TLine> => line.ProjectedOnTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TLine AlignLine<TLine>(TLine line) where TLine : ILine<TLine> => line.AlignedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine => line.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn<TLine>(TLine line) where TLine : ILine => line.ClosestPointTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TLine>(TLine line) where TLine : ILine => line.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionPointWith<TLine>(TLine line) where TLine : ILine => line.IntersectionPointWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo<TLine>(TLine line) where TLine : ILine => line.RelationshipTo(this);

	

	public Location ClosestPointTo(Location location) => location - PointClosestToOrigin.GetVectTo(location).ProjectedOnTo(Normal);
	public float SignedDistanceFrom(Location location) => Vector3.Dot(location.ToVector3(), _normal) + _coefficientOfNormalToCreateMinimalVectFromPlaneToOrigin;
	public float DistanceFrom(Location location) => MathF.Abs(SignedDistanceFrom(location));
	
	// TODO in Xml make it clear that "facing towards" means the normal is pointing out of this side of the plane; and that points on the plane (within the thickness value) will return false
	// Implementation note: We use a plane thickness by default because relying on the signed distance being exactly 0 for anything other than axis-aligned planes is pretty much stochastic
	// due to FP inaccuracy. Even for axis-aligned ones it's still pretty bad, but is possibly more consistent when moving around on the surface of the plane. In these cases, if users
	// really want 0-thickness planes, they can still specify as such using the overloads that take a thickness parameter.
	public bool FacesTowards(Location location) => FacesTowards(location, DefaultPlaneThickness);
	public bool FacesAwayFrom(Location location) => FacesAwayFrom(location, DefaultPlaneThickness);
	public bool FacesTowards(Location location, float planeThickness) => SignedDistanceFrom(location) > planeThickness;
	public bool FacesAwayFrom(Location location, float planeThickness) => SignedDistanceFrom(location) < -planeThickness;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, DefaultPlaneThickness);
	public bool Contains(Location location, float planeThickness) => DistanceFrom(location) <= planeThickness;

	public float DistanceFrom(Plane other) => MathF.Abs(Normal.SimilarityTo(other.Normal)) >= 0.999f ? PointClosestToOrigin.DistanceFrom(other.PointClosestToOrigin) : 0f;
	public Line? IntersectionLineWith(Plane other) {
		// Check for parallel
	}
}