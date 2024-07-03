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
}

partial struct Ray {
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

partial struct BoundedRay {
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