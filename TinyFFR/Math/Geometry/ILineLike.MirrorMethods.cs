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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizedWith(Line line) => ParallelizedWith(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(Line line) => FastParallelizedWith(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizedWith(Ray ray) => ParallelizedWith(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(Ray ray) => FastParallelizedWith(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizedWith(BoundedRay ray) => ParallelizedWith(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(BoundedRay ray) => FastParallelizedWith(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction direction) => Direction.IsParallelTo(direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Line line) => IsParallelTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Ray ray) => IsParallelTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(BoundedRay ray) => IsParallelTo(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction) => IsApproximatelyParallelTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyParallelTo(direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line) => IsApproximatelyParallelTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line, Angle tolerance) => IsApproximatelyParallelTo(line.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray) => IsApproximatelyParallelTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray) => IsApproximatelyParallelTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizedAgainst(Line line) => OrthogonalizedAgainst(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizedAgainst(Line line) => FastOrthogonalizedAgainst(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizedAgainst(Ray ray) => OrthogonalizedAgainst(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizedAgainst(Ray ray) => FastOrthogonalizedAgainst(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizedAgainst(BoundedRay ray) => OrthogonalizedAgainst(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizedAgainst(BoundedRay ray) => FastOrthogonalizedAgainst(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction direction) => Direction.IsOrthogonalTo(direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Line line) => IsOrthogonalTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Ray ray) => IsOrthogonalTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(BoundedRay ray) => IsOrthogonalTo(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction) => IsApproximatelyOrthogonalTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyOrthogonalTo(direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line) => IsApproximatelyOrthogonalTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line, Angle tolerance) => IsApproximatelyOrthogonalTo(line.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Line line, float lineThickness) => ILineLike.IsExactlyColinearWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Ray ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(BoundedRay ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line) => ILineLike.IsApproximatelyColinearWith(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, line, lineThickness, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => Direction.AngleTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Plane plane) => Direction.IsParallelTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Plane plane) => IsApproximatelyParallelTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	public bool IsApproximatelyParallelTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.Zero, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Plane plane) => Direction.IsOrthogonalTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Plane plane) => IsApproximatelyOrthogonalTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	public bool IsApproximatelyOrthogonalTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.QuarterCircle, tolerance);

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
	public Line? OrthogonalizationOf(Line line) => line.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizationOf(Ray ray) => ray.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizationOf(BoundedRay ray) => ray.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizationOf(Line line) => line.FastParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizationOf(Ray ray) => ray.FastParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizationOf(BoundedRay ray) => ray.FastParallelizedWith(this);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizedWith(Line line) => ParallelizedWith(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizedWith(Line line) => FastParallelizedWith(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizedWith(Ray ray) => ParallelizedWith(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizedWith(Ray ray) => FastParallelizedWith(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizedWith(BoundedRay ray) => ParallelizedWith(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizedWith(BoundedRay ray) => FastParallelizedWith(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction direction) => Direction.IsParallelTo(direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Line line) => IsParallelTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Ray ray) => IsParallelTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(BoundedRay ray) => IsParallelTo(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction) => IsApproximatelyParallelTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyParallelTo(direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line) => IsApproximatelyParallelTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line, Angle tolerance) => IsApproximatelyParallelTo(line.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray) => IsApproximatelyParallelTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray) => IsApproximatelyParallelTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizedAgainst(Line line) => OrthogonalizedAgainst(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizedAgainst(Line line) => FastOrthogonalizedAgainst(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizedAgainst(Ray ray) => OrthogonalizedAgainst(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizedAgainst(Ray ray) => FastOrthogonalizedAgainst(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizedAgainst(BoundedRay ray) => OrthogonalizedAgainst(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizedAgainst(BoundedRay ray) => FastOrthogonalizedAgainst(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction direction) => Direction.IsOrthogonalTo(direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Line line) => IsOrthogonalTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Ray ray) => IsOrthogonalTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(BoundedRay ray) => IsOrthogonalTo(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction) => IsApproximatelyOrthogonalTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyOrthogonalTo(direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line) => IsApproximatelyOrthogonalTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line, Angle tolerance) => IsApproximatelyOrthogonalTo(line.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Line line, float lineThickness) => ILineLike.IsExactlyColinearWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Ray ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(BoundedRay ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line) => ILineLike.IsApproximatelyColinearWith(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, line, lineThickness, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => Direction.AngleTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Plane plane) => Direction.IsParallelTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Plane plane) => IsApproximatelyParallelTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	public bool IsApproximatelyParallelTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.Zero, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Plane plane) => Direction.IsOrthogonalTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Plane plane) => IsApproximatelyOrthogonalTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	public bool IsApproximatelyOrthogonalTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.QuarterCircle, tolerance);

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
	public Line? OrthogonalizationOf(Line line) => line.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizationOf(Ray ray) => ray.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizationOf(BoundedRay ray) => ray.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizationOf(Line line) => line.FastParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizationOf(Ray ray) => ray.FastParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizationOf(BoundedRay ray) => ray.FastParallelizedWith(this);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizedWith(Line line) => ParallelizedWith(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizedWith(Line line) => FastParallelizedWith(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizedWith(Ray ray) => ParallelizedWith(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizedWith(Ray ray) => FastParallelizedWith(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizedWith(BoundedRay ray) => ParallelizedWith(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizedWith(BoundedRay ray) => FastParallelizedWith(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction direction) => Direction.IsParallelTo(direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Line line) => IsParallelTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Ray ray) => IsParallelTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(BoundedRay ray) => IsParallelTo(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction) => IsApproximatelyParallelTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyParallelTo(direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line) => IsApproximatelyParallelTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line, Angle tolerance) => IsApproximatelyParallelTo(line.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray) => IsApproximatelyParallelTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray) => IsApproximatelyParallelTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizedAgainst(Line line) => OrthogonalizedAgainst(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizedAgainst(Line line) => FastOrthogonalizedAgainst(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizedAgainst(Ray ray) => OrthogonalizedAgainst(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizedAgainst(Ray ray) => FastOrthogonalizedAgainst(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizedAgainst(BoundedRay ray) => OrthogonalizedAgainst(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizedAgainst(BoundedRay ray) => FastOrthogonalizedAgainst(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction direction) => Direction.IsOrthogonalTo(direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Line line) => IsOrthogonalTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Ray ray) => IsOrthogonalTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(BoundedRay ray) => IsOrthogonalTo(ray.Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction) => IsApproximatelyOrthogonalTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyOrthogonalTo(direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line) => IsApproximatelyOrthogonalTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line, Angle tolerance) => IsApproximatelyOrthogonalTo(line.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Line line, float lineThickness) => ILineLike.IsExactlyColinearWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Ray ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(BoundedRay ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line) => ILineLike.IsApproximatelyColinearWith(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, line, lineThickness, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => Direction.AngleTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Plane plane) => Direction.IsParallelTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Plane plane) => IsApproximatelyParallelTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	public bool IsApproximatelyParallelTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.Zero, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Plane plane) => Direction.IsOrthogonalTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Plane plane) => IsApproximatelyOrthogonalTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	public bool IsApproximatelyOrthogonalTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.QuarterCircle, tolerance);

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
	public Line? OrthogonalizationOf(Line line) => line.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizationOf(Ray ray) => ray.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizationOf(BoundedRay ray) => ray.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizationOf(Line line) => line.FastParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizationOf(Ray ray) => ray.FastParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizationOf(BoundedRay ray) => ray.FastParallelizedWith(this);
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
partial struct Direction : ILineOrthogonalizationTarget, ILineParallelizationTarget {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Line line) => line.IsOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Ray ray) => ray.IsOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(BoundedRay ray) => ray.IsOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Line line) => line.IsParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Ray ray) => ray.IsParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(BoundedRay ray) => ray.IsParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line) => line.IsApproximatelyOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line, Angle tolerance) => line.IsApproximatelyOrthogonalTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray) => ray.IsApproximatelyOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray, Angle tolerance) => ray.IsApproximatelyOrthogonalTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray) => ray.IsApproximatelyOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray, Angle tolerance) => ray.IsApproximatelyOrthogonalTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line) => line.IsApproximatelyParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line, Angle tolerance) => line.IsApproximatelyParallelTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray) => ray.IsApproximatelyParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray, Angle tolerance) => ray.IsApproximatelyParallelTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray) => ray.IsApproximatelyParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray, Angle tolerance) => ray.IsApproximatelyParallelTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizationOf(Line line) => line.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizationOf(Ray ray) => ray.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizationOf(BoundedRay ray) => ray.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizationOf(Line line) => line.FastOrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizationOf(Ray ray) => ray.FastOrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizationOf(BoundedRay ray) => ray.FastOrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizationOf(Line line) => line.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizationOf(Ray ray) => ray.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizationOf(BoundedRay ray) => ray.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizationOf(Line line) => line.FastOrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizationOf(Ray ray) => ray.FastOrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizationOf(BoundedRay ray) => ray.FastOrthogonalizedAgainst(this);
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
	IIntersectionDeterminable<Plane, Line, Location>,
	IIntersectionDeterminable<Plane, Ray, Location>,
	IIntersectionDeterminable<Plane, BoundedRay, Location> {
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
	public Line? ReflectionOf(Line line) => line.ReflectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastReflectionOf(Line line) => line.FastReflectedBy(this);
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
	public Pair<Ray, Ray>? Split(Line line) => line.SplitBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<BoundedRay, Ray>? Split(Ray ray) => ray.SplitBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<BoundedRay, BoundedRay>? Split(BoundedRay ray) => ray.SplitBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<Ray, Ray> FastSplit(Line line) => line.FastSplitBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<BoundedRay, Ray> FastSplit(Ray ray) => ray.FastSplitBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<BoundedRay, BoundedRay> FastSplit(BoundedRay ray) => ray.FastSplitBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => line.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => ray.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => ray.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line) => line.FastIntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray) => ray.FastIntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray) => ray.FastIntersectionWith(this);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Line line) => line.IsOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Ray ray) => ray.IsOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(BoundedRay ray) => ray.IsOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line) => line.IsApproximatelyOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line, Angle tolerance) => line.IsApproximatelyOrthogonalTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray) => ray.IsApproximatelyOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray, Angle tolerance) => ray.IsApproximatelyOrthogonalTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray) => ray.IsApproximatelyOrthogonalTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray, Angle tolerance) => ray.IsApproximatelyOrthogonalTo(this, tolerance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Line line) => line.IsParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Ray ray) => ray.IsParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(BoundedRay ray) => ray.IsParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line) => line.IsApproximatelyParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line, Angle tolerance) => line.IsApproximatelyParallelTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray) => ray.IsApproximatelyParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray, Angle tolerance) => ray.IsApproximatelyParallelTo(this, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray) => ray.IsApproximatelyParallelTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray, Angle tolerance) => ray.IsApproximatelyParallelTo(this, tolerance);
}