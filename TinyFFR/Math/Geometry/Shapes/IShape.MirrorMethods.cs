namespace Egodystonic.TinyFFR;

partial struct Location : IClosestConvexShapePointsDiscoverable, IConvexShapeDistanceMeasurable {
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.PointClosestTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfacePointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestConvexShapePointsDiscoverable.PointClosestTo<TShape>(TShape shape) => this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestConvexShapePointsDiscoverable.PointClosestToSurfaceOf<TShape>(TShape shape) => this;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceSquaredFrom(this);
}

partial struct Plane : IClosestConvexShapePointsDiscoverable, IConvexShapeDistanceMeasurable {
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.PointClosestTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfacePointClosestTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointOn(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointToSurfaceOn(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
}

partial struct Line {
	/// <summary>
	/// Returns the point inside (or on the surface of) <paramref name="shape"/> that is closest to this line.
	/// </summary>
	/// <remarks>
	/// This is the same as this line's own closest point on <paramref name="shape"/> if this line already passes through <paramref name="shape"/>. For the closest point on the surface even when passing through the shape, see <see cref="ClosestPointOnSurfaceOf{TShape}"/>.
	/// </remarks>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to find the closest point within.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.PointClosestTo(this);
	/// <summary>
	/// Returns the point on the surface of <paramref name="shape"/> that is closest to this line.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to find the closest surface point on.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfacePointClosestTo(this);
	/// <summary>
	/// Returns the point on this line that is closest to <paramref name="shape"/>.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure against.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointOn(this);
	/// <summary>
	/// Returns the point on this line that is closest to the surface of <paramref name="shape"/>.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure against.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointToSurfaceOn(this);
	/// <summary>
	/// Calculates the distance between this line and the closest point inside (or on the surface of) <paramref name="shape"/>.
	/// </summary>
	/// <remarks>
	/// This is <c>0f</c> if this line passes through <paramref name="shape"/>. For the distance to the surface even when passing through the shape, see <see cref="DistanceFromSurfaceOf{TShape}"/>.
	/// </remarks>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure the distance to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	/// <summary>
	/// Calculates the distance between this line and the closest point on the surface of <paramref name="shape"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="DistanceFrom{TShape}"/>, this is not clamped to <c>0f</c> when this line passes through <paramref name="shape"/>.
	/// </remarks>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure the distance to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceFrom(this);
	/// <summary>
	/// Calculates the square of the distance between this line and the closest point inside (or on the surface of) <paramref name="shape"/>. Cheaper than <see cref="DistanceFrom{TShape}"/> when only comparing distances.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure the distance to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
	/// <summary>
	/// Calculates the square of the distance between this line and the closest point on the surface of <paramref name="shape"/>. Cheaper than <see cref="DistanceFromSurfaceOf{TShape}"/> when only comparing distances.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure the distance to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceSquaredFrom(this);
	/// <summary>
	/// Calculates the incident angle between this line and <paramref name="shape"/>.
	/// </summary>
	/// <param name="shape">The sphere to measure the incident angle with.</param>
	/// <returns><see langword="null"/> if this line does not intersect <paramref name="shape"/>; the incident angle otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(Sphere shape) => shape.IncidentAngleWith(this);
	/// <summary>
	/// Executes the same function as <see cref="IncidentAngleWith(Sphere)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line does intersect <paramref name="shape"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="shape">The sphere to measure the incident angle with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(Sphere shape) => shape.FastIncidentAngleWith(this);
	/// <summary>
	/// Calculates the intersection(s) between this line and <paramref name="shape"/>, if any.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to test against.</typeparam>
	/// <param name="shape">The shape to test against.</param>
	/// <returns><see langword="null"/> if this line does not intersect <paramref name="shape"/>; the intersection(s) otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.IntersectionWith(this);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith{TShape}"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line does intersect <paramref name="shape"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <typeparam name="TShape">The type of the convex shape to test against.</typeparam>
	/// <param name="shape">The shape to test against.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection FastIntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastIntersectionWith(this);
}

partial struct Ray : IConvexShapeReflectable<Ray> {
	/// <inheritdoc cref="Line.ClosestPointInsideOf{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.PointClosestTo(this);
	/// <inheritdoc cref="Line.ClosestPointOnSurfaceOf{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfacePointClosestTo(this);
	/// <inheritdoc cref="Line.PointClosestTo{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointOn(this);
	/// <inheritdoc cref="Line.PointClosestToSurfaceOf{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointToSurfaceOn(this);
	/// <inheritdoc cref="Line.DistanceFrom{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	/// <inheritdoc cref="Line.DistanceFromSurfaceOf{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceFrom(this);
	/// <inheritdoc cref="Line.DistanceSquaredFrom{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
	/// <inheritdoc cref="Line.DistanceSquaredFromSurfaceOf{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceSquaredFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.IncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastIncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ReflectionOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastReflectionOf(this);
	/// <inheritdoc cref="Line.IntersectionWith{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.IntersectionWith(this);
	/// <inheritdoc cref="Line.FastIntersectionWith{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection FastIntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastIntersectionWith(this);
}

partial struct BoundedRay : IConvexShapeReflectable<BoundedRay> {
	/// <inheritdoc cref="Line.ClosestPointInsideOf{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.PointClosestTo(this);
	/// <inheritdoc cref="Line.ClosestPointOnSurfaceOf{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfacePointClosestTo(this);
	/// <inheritdoc cref="Line.PointClosestTo{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointOn(this);
	/// <inheritdoc cref="Line.PointClosestToSurfaceOf{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ClosestPointToSurfaceOn(this);
	/// <inheritdoc cref="Line.DistanceFrom{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceFrom(this);
	/// <inheritdoc cref="Line.DistanceFromSurfaceOf{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceFrom(this);
	/// <inheritdoc cref="Line.DistanceSquaredFrom{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.DistanceSquaredFrom(this);
	/// <inheritdoc cref="Line.DistanceSquaredFromSurfaceOf{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.SurfaceDistanceSquaredFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.IncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastIncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.ReflectionOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastReflectionOf(this);
	/// <inheritdoc cref="Line.IntersectionWith{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.IntersectionWith(this);
	/// <inheritdoc cref="Line.FastIntersectionWith{TShape}(TShape)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection FastIntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape> => shape.FastIntersectionWith(this);
}