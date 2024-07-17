// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

partial struct Line {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Line line) => ILineLike.AngleTo(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Ray ray) => ILineLike.AngleTo(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(BoundedRay ray) => ILineLike.AngleTo(this, ray);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => DistanceSquaredFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Plane plane) => DistanceSquaredFrom(ClosestPointOn(plane));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedBy(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => IntersectionWith(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line) => FastIntersectionWith(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line, float lineThickness) => ILineLike.FastIntersectionWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay boundedRay) => boundedRay.PointClosestTo(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay boundedRay) => DistanceFrom(ClosestPointOn(boundedRay));
}
partial struct Ray {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Line line) => ILineLike.AngleTo(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Ray ray) => ILineLike.AngleTo(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(BoundedRay ray) => ILineLike.AngleTo(this, ray);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => DistanceSquaredFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Plane plane) => DistanceSquaredFrom(ClosestPointOn(plane));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedBy(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => IntersectionWith(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line) => FastIntersectionWith(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line, float lineThickness) => ILineLike.FastIntersectionWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay boundedRay) => boundedRay.PointClosestTo(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay boundedRay) => DistanceFrom(ClosestPointOn(boundedRay));
}
partial struct BoundedRay {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Line line) => ILineLike.AngleTo(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Ray ray) => ILineLike.AngleTo(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(BoundedRay ray) => ILineLike.AngleTo(this, ray);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => DistanceSquaredFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Plane plane) => DistanceSquaredFrom(ClosestPointOn(plane));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedBy(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => IntersectionWith(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line) => FastIntersectionWith(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line, float lineThickness) => ILineLike.FastIntersectionWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay boundedRay) => boundedRay.PointClosestTo(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay boundedRay) => DistanceFrom(ClosestPointOn(boundedRay));
}

partial struct Location : ILineDistanceMeasurable, ILineClosestExogenousPointDiscoverable, ILineContainable {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => line.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => ray.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay ray) => ray.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => line.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => ray.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => ray.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(Line line) => line.Contains(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(Ray ray) => ray.Contains(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(BoundedRay ray) => ray.Contains(this);
}
partial struct Plane : 
	ILineProjectionTarget, 
	ILineOrthogonalizationTarget, 
	ILineParallelizationTarget, 
	ILineReflectionTarget,
	ILineSignedDistanceMeasurable, 
	ILineClosestEndogenousPointDiscoverable, 
	ILineClosestExogenousPointDiscoverable,
	ILineRelatable<PlaneObjectRelationship>,
	IIntersectionDeterminable<Plane, Line, Ray>,
	IIntersectionDeterminable<Plane, Ray, Ray>,
	IIntersectionDeterminable<Plane, BoundedRay, BoundedRay> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ProjectionOf(Line line) => line.ProjectedOnTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ProjectionOf(Ray ray) => ray.ProjectedOnTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay ProjectionOf(BoundedRay ray) => ray.ProjectedOnTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastProjectionOf(Line line) => line.FastProjectedOnTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastProjectionOf(Ray ray) => ray.FastProjectedOnTo(this);
	BoundedRay? IProjectionTarget<BoundedRay>.ProjectionOf(BoundedRay ray) => ProjectionOf(ray);
	BoundedRay IProjectionTarget<BoundedRay>.FastProjectionOf(BoundedRay ray) => ProjectionOf(ray);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizationOf(Line line) => line.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizationOf(Ray ray) => ray.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizationOf(Line line) => line.FastOrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizationOf(Ray ray) => ray.FastOrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizationOf(BoundedRay ray) => ray.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizationOf(BoundedRay ray) => ray.FastOrthogonalizedAgainst(this);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizationOf(Line line) => line.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizationOf(Ray ray) => ray.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizationOf(BoundedRay ray) => ray.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizationOf(Line line) => line.FastParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizationOf(Ray ray) => ray.FastParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizationOf(BoundedRay ray) => ray.FastParallelizedWith(this);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ReflectionOf(Line line) => line.ReflectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastReflectionOf(Line line) => line.FastReflectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(Line line) => line.IncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(Line line) => line.FastIncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ReflectionOf(Ray ray) => ray.ReflectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastReflectionOf(Ray ray) => ray.FastReflectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(Ray ray) => ray.IncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(Ray ray) => ray.FastIncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ReflectionOf(BoundedRay ray) => ray.ReflectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastReflectionOf(BoundedRay ray) => ray.FastReflectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(BoundedRay ray) => ray.IncidentAngleWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(BoundedRay ray) => ray.FastIncidentAngleWith(this);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => line.IsIntersectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => ray.IsIntersectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => ray.IsIntersectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? IntersectionWith(Line line) => line.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? IntersectionWith(Ray ray) => ray.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? IntersectionWith(BoundedRay ray) => ray.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastIntersectionWith(Line line) => line.FastIntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastIntersectionWith(Ray ray) => ray.FastIntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastIntersectionWith(BoundedRay ray) => ray.FastIntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => line.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(Line line) => line.SignedDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => line.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => ray.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(Ray ray) => ray.SignedDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => ray.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay ray) => ray.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(BoundedRay ray) => ray.SignedDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => ray.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(Line line) => line.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(Ray ray) => ray.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(BoundedRay ray) => ray.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo(Line line) => line.RelationshipTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo(Ray ray) => ray.RelationshipTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo(BoundedRay ray) => ray.RelationshipTo(this);
}