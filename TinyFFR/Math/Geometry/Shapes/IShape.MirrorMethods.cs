namespace Egodystonic.TinyFFR;

partial struct Location : IClosestConvexShapePointsDiscoverable, IConvexShapeDistanceMeasurable {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfacePointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestConvexShapePointsDiscoverable.PointClosestTo<TShape>(TShape shape) => this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestConvexShapePointsDiscoverable.PointClosestToSurfaceOf<TShape>(TShape shape) => this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceSquaredFrom(this);
}

partial struct Plane : IClosestConvexShapePointsDiscoverable, IConvexShapeDistanceMeasurable {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfacePointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointToSurfaceOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceSquaredFrom(this);
}

partial struct Line {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfacePointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointToSurfaceOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(OriginSphere shape) => shape.IncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(OriginSphere shape) => shape.FastIncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection FastIntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastIntersectionWith(this);
}

partial struct Ray : IConvexShapeReflectable<Ray> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfacePointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointToSurfaceOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.IncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastIncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ReflectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastReflectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection FastIntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastIntersectionWith(this);
}

partial struct BoundedRay : IConvexShapeReflectable<BoundedRay> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfacePointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointToSurfaceOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.IncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastIncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ReflectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastReflectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection FastIntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastIntersectionWith(this);
}