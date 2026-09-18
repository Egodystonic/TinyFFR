// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

partial struct Line {
	/// <summary>
	/// Calculates the (unsigned) angle between this line's and <paramref name="line"/>'s directions.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Line line) => ILineLike.AngleTo(this, line);
	/// <summary>
	/// Calculates the (unsigned) angle between this line's and <paramref name="ray"/>'s directions.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Ray ray) => ILineLike.AngleTo(this, ray);
	/// <summary>
	/// Calculates the (unsigned) angle between this line's and <paramref name="ray"/>'s directions.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(BoundedRay ray) => ILineLike.AngleTo(this, ray);

	/// <summary>
	/// Calculates the angle formed between this line's and <paramref name="line"/>'s directions, additionally attributing a sign (+ or -) to the result making it possible to differentiate the winding/chirality between them.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Direction.SignedAngleTo(Direction, Direction)"/> on the two line-likes' <see cref="Direction"/> values.
	/// </remarks>
	/// <param name="line">The target line.</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise winding from this to <paramref name="line"/> will be reported with a positive value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(Line line, Direction clockwiseAxis) => ILineLike.SignedAngleTo(this, line, clockwiseAxis);
	/// <summary>
	/// Calculates the angle formed between this line's and <paramref name="ray"/>'s directions, additionally attributing a sign (+ or -) to the result making it possible to differentiate the winding/chirality between them.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Direction.SignedAngleTo(Direction, Direction)"/> on the two line-likes' <see cref="Direction"/> values.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise winding from this to <paramref name="ray"/> will be reported with a positive value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(Ray ray, Direction clockwiseAxis) => ILineLike.SignedAngleTo(this, ray, clockwiseAxis);
	/// <summary>
	/// Calculates the angle formed between this line's and <paramref name="ray"/>'s directions, additionally attributing a sign (+ or -) to the result making it possible to differentiate the winding/chirality between them.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Direction.SignedAngleTo(Direction, Direction)"/> on the two line-likes' <see cref="Direction"/> values.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise winding from this to <paramref name="ray"/> will be reported with a positive value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(BoundedRay ray, Direction clockwiseAxis) => ILineLike.SignedAngleTo(this, ray, clockwiseAxis);

	/// <summary>
	/// Calculates the square of the distance between this line and <paramref name="line"/>. Cheaper than <see cref="DistanceFrom(Line)"/> when only comparing distances.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => DistanceSquaredFrom(ClosestPointOn(line));
	/// <summary>
	/// Calculates the square of the distance between this line and <paramref name="ray"/>. Cheaper than <see cref="DistanceFrom(Ray)"/> when only comparing distances.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	/// <summary>
	/// Calculates the square of the distance between this line and <paramref name="ray"/>. Cheaper than <see cref="DistanceFrom(BoundedRay)"/> when only comparing distances.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	/// <summary>
	/// Calculates the square of the distance between this line and <paramref name="plane"/>. Cheaper than <see cref="DistanceFrom(Plane)"/> when only comparing distances.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Plane plane) => DistanceSquaredFrom(ClosestPointOn(plane));

	/// <summary>
	/// Determines whether this line intersects <paramref name="line"/>, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedBy(line, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Determines whether this line intersects <paramref name="line"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness) != null;
	/// <summary>
	/// Determines whether this line intersects <paramref name="ray"/>, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Determines whether this line intersects <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	/// <summary>
	/// Determines whether this line intersects <paramref name="ray"/>, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Determines whether this line intersects <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	/// <summary>
	/// Calculates the intersection between this line and <paramref name="line"/>, if any, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => IntersectionWith(line, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Calculates the intersection between this line and <paramref name="line"/>, if any.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness);
	/// <summary>
	/// Calculates the intersection between this line and <paramref name="ray"/>, if any, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Calculates the intersection between this line and <paramref name="ray"/>, if any.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	/// <summary>
	/// Calculates the intersection between this line and <paramref name="ray"/>, if any, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Calculates the intersection between this line and <paramref name="ray"/>, if any.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line and <paramref name="line"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line) => FastIntersectionWith(line, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Line,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line and <paramref name="line"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">Unused by this overload; present only to mirror the two-argument <see cref="IntersectionWith(Line,float)"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line, float lineThickness) => ILineLike.FastIntersectionWith(this, line, lineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Ray,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">Unused by this overload; present only to mirror the two-argument <see cref="IntersectionWith(Ray,float)"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(BoundedRay,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">Unused by this overload; present only to mirror the two-argument <see cref="IntersectionWith(BoundedRay,float)"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);

	/// <summary>
	/// Returns the point on <paramref name="line"/> that is closest to this line.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	/// <summary>
	/// Returns the point on <paramref name="ray"/> that is closest to this line.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	/// <summary>
	/// Returns the point on <paramref name="boundedRay"/> that is closest to this line.
	/// </summary>
	/// <param name="boundedRay">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay boundedRay) => boundedRay.PointClosestTo(this);

	/// <summary>
	/// Calculates the distance between this line and <paramref name="line"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	/// <summary>
	/// Calculates the distance between this line and <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	/// <summary>
	/// Calculates the distance between this line and <paramref name="boundedRay"/>.
	/// </summary>
	/// <param name="boundedRay">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay boundedRay) => DistanceFrom(ClosestPointOn(boundedRay));

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizedWith(Line line) => ParallelizedWith(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(Line line) => FastParallelizedWith(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizedWith(Ray ray) => ParallelizedWith(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(Ray ray) => FastParallelizedWith(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizedWith(BoundedRay ray) => ParallelizedWith(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizedWith(BoundedRay ray) => FastParallelizedWith(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction direction) => Direction.IsParallelTo(direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Line line) => IsParallelTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Ray ray) => IsParallelTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(BoundedRay ray) => IsParallelTo(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction) => IsApproximatelyParallelTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyParallelTo(direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line) => IsApproximatelyParallelTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line, Angle tolerance) => IsApproximatelyParallelTo(line.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray) => IsApproximatelyParallelTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray) => IsApproximatelyParallelTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizedAgainst(Line line) => OrthogonalizedAgainst(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizedAgainst(Line line) => FastOrthogonalizedAgainst(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizedAgainst(Ray ray) => OrthogonalizedAgainst(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizedAgainst(Ray ray) => FastOrthogonalizedAgainst(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizedAgainst(BoundedRay ray) => OrthogonalizedAgainst(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizedAgainst(BoundedRay ray) => FastOrthogonalizedAgainst(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction direction) => Direction.IsOrthogonalTo(direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Line line) => IsOrthogonalTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Ray ray) => IsOrthogonalTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(BoundedRay ray) => IsOrthogonalTo(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction) => IsApproximatelyOrthogonalTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyOrthogonalTo(direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line) => IsApproximatelyOrthogonalTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line, Angle tolerance) => IsApproximatelyOrthogonalTo(line.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);

	/// <summary>
	/// Determines whether this line is exactly colinear with <paramref name="line"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Line line, float lineThickness) => ILineLike.IsExactlyColinearWith(this, line, lineThickness);
	/// <summary>
	/// Determines whether this line is exactly colinear with <paramref name="ray"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Ray ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);
	/// <summary>
	/// Determines whether this line is exactly colinear with <paramref name="ray"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(BoundedRay ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);

	/// <summary>
	/// Determines whether this line is colinear with <paramref name="line"/>, within <see cref="ILineLike.DefaultLineThickness"/> and <see cref="ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line) => ILineLike.IsApproximatelyColinearWith(this, line);
	/// <summary>
	/// Determines whether this line is colinear with <paramref name="ray"/>, within <see cref="ILineLike.DefaultLineThickness"/> and <see cref="ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	/// <summary>
	/// Determines whether this line is colinear with <paramref name="ray"/>, within <see cref="ILineLike.DefaultLineThickness"/> and <see cref="ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	/// <summary>
	/// Determines whether this line is colinear with <paramref name="line"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two line-likes' directions are permitted to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, line, lineThickness, tolerance);
	/// <summary>
	/// Determines whether this line is colinear with <paramref name="ray"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two line-likes' directions are permitted to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);
	/// <summary>
	/// Determines whether this line is colinear with <paramref name="ray"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two line-likes' directions are permitted to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);

	/// <summary>
	/// Calculates the (unsigned) angle between this line's direction and <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => Direction.AngleTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Plane plane) => Direction.IsParallelTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Plane plane) => IsApproximatelyParallelTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	public bool IsApproximatelyParallelTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.Zero, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Plane plane) => Direction.IsOrthogonalTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Plane plane) => IsApproximatelyOrthogonalTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	public bool IsApproximatelyOrthogonalTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.QuarterCircle, tolerance);

	/// <summary>
	/// Attempts to parallelize <paramref name="line"/> with this line; equivalent to <c>line.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="line">The line to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizationOf(Line line) => line.ParallelizedWith(this);
	/// <summary>
	/// Attempts to parallelize <paramref name="ray"/> with this line; equivalent to <c>ray.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="ray">The ray to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizationOf(Ray ray) => ray.ParallelizedWith(this);
	/// <summary>
	/// Attempts to parallelize <paramref name="ray"/> with this line; equivalent to <c>ray.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="ray">The ray segment to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizationOf(BoundedRay ray) => ray.ParallelizedWith(this);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="line">The line to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizationOf(Line line) => line.FastParallelizedWith(this);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizationOf(Ray ray) => ray.FastParallelizedWith(this);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray segment to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizationOf(BoundedRay ray) => ray.FastParallelizedWith(this);
	/// <summary>
	/// Attempts to orthogonalize <paramref name="line"/> against this line; equivalent to <c>line.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="line">The line to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizationOf(Line line) => line.OrthogonalizedAgainst(this);
	/// <summary>
	/// Attempts to orthogonalize <paramref name="ray"/> against this line; equivalent to <c>ray.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="ray">The ray to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizationOf(Ray ray) => ray.OrthogonalizedAgainst(this);
	/// <summary>
	/// Attempts to orthogonalize <paramref name="ray"/> against this line; equivalent to <c>ray.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="ray">The ray segment to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizationOf(BoundedRay ray) => ray.OrthogonalizedAgainst(this);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizationOf(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="line">The line to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizationOf(Line line) => line.FastOrthogonalizedAgainst(this);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizationOf(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizationOf(Ray ray) => ray.FastOrthogonalizedAgainst(this);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizationOf(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray segment to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizationOf(BoundedRay ray) => ray.FastOrthogonalizedAgainst(this);
}
partial struct Ray {
	/// <summary>
	/// Calculates the (unsigned) angle between this ray's and <paramref name="line"/>'s directions.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Line line) => ILineLike.AngleTo(this, line);
	/// <summary>
	/// Calculates the (unsigned) angle between this ray's and <paramref name="ray"/>'s directions.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Ray ray) => ILineLike.AngleTo(this, ray);
	/// <summary>
	/// Calculates the (unsigned) angle between this ray's and <paramref name="ray"/>'s directions.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(BoundedRay ray) => ILineLike.AngleTo(this, ray);

	/// <summary>
	/// Calculates the angle formed between this ray's and <paramref name="line"/>'s directions, additionally attributing a sign (+ or -) to the result making it possible to differentiate the winding/chirality between them.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Direction.SignedAngleTo(Direction, Direction)"/> on the two line-likes' <see cref="Direction"/> values.
	/// </remarks>
	/// <param name="line">The target line.</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise winding from this to <paramref name="line"/> will be reported with a positive value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(Line line, Direction clockwiseAxis) => ILineLike.SignedAngleTo(this, line, clockwiseAxis);
	/// <summary>
	/// Calculates the angle formed between this ray's and <paramref name="ray"/>'s directions, additionally attributing a sign (+ or -) to the result making it possible to differentiate the winding/chirality between them.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Direction.SignedAngleTo(Direction, Direction)"/> on the two line-likes' <see cref="Direction"/> values.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise winding from this to <paramref name="ray"/> will be reported with a positive value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(Ray ray, Direction clockwiseAxis) => ILineLike.SignedAngleTo(this, ray, clockwiseAxis);
	/// <summary>
	/// Calculates the angle formed between this ray's and <paramref name="ray"/>'s directions, additionally attributing a sign (+ or -) to the result making it possible to differentiate the winding/chirality between them.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Direction.SignedAngleTo(Direction, Direction)"/> on the two line-likes' <see cref="Direction"/> values.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise winding from this to <paramref name="ray"/> will be reported with a positive value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(BoundedRay ray, Direction clockwiseAxis) => ILineLike.SignedAngleTo(this, ray, clockwiseAxis);

	/// <summary>
	/// Calculates the square of the distance between this ray and <paramref name="line"/>. Cheaper than <see cref="DistanceFrom(Line)"/> when only comparing distances.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => DistanceSquaredFrom(ClosestPointOn(line));
	/// <summary>
	/// Calculates the square of the distance between this ray and <paramref name="ray"/>. Cheaper than <see cref="DistanceFrom(Ray)"/> when only comparing distances.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	/// <summary>
	/// Calculates the square of the distance between this ray and <paramref name="ray"/>. Cheaper than <see cref="DistanceFrom(BoundedRay)"/> when only comparing distances.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	/// <summary>
	/// Calculates the square of the distance between this ray and <paramref name="plane"/>. Cheaper than <see cref="DistanceFrom(Plane)"/> when only comparing distances.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Plane plane) => DistanceSquaredFrom(ClosestPointOn(plane));

	/// <summary>
	/// Determines whether this ray intersects <paramref name="line"/>, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedBy(line, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Determines whether this ray intersects <paramref name="line"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness) != null;
	/// <summary>
	/// Determines whether this ray intersects <paramref name="ray"/>, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Determines whether this ray intersects <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	/// <summary>
	/// Determines whether this ray intersects <paramref name="ray"/>, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Determines whether this ray intersects <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	/// <summary>
	/// Calculates the intersection between this ray and <paramref name="line"/>, if any, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => IntersectionWith(line, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Calculates the intersection between this ray and <paramref name="line"/>, if any.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness);
	/// <summary>
	/// Calculates the intersection between this ray and <paramref name="ray"/>, if any, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Calculates the intersection between this ray and <paramref name="ray"/>, if any.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	/// <summary>
	/// Calculates the intersection between this ray and <paramref name="ray"/>, if any, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Calculates the intersection between this ray and <paramref name="ray"/>, if any.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray and <paramref name="line"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line) => FastIntersectionWith(line, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Line,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray and <paramref name="line"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">Unused by this overload; present only to mirror the two-argument <see cref="IntersectionWith(Line,float)"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line, float lineThickness) => ILineLike.FastIntersectionWith(this, line, lineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Ray,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">Unused by this overload; present only to mirror the two-argument <see cref="IntersectionWith(Ray,float)"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(BoundedRay,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">Unused by this overload; present only to mirror the two-argument <see cref="IntersectionWith(BoundedRay,float)"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);

	/// <summary>
	/// Returns the point on <paramref name="line"/> that is closest to this ray.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	/// <summary>
	/// Returns the point on <paramref name="ray"/> that is closest to this ray.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	/// <summary>
	/// Returns the point on <paramref name="boundedRay"/> that is closest to this ray.
	/// </summary>
	/// <param name="boundedRay">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay boundedRay) => boundedRay.PointClosestTo(this);

	/// <summary>
	/// Calculates the distance between this ray and <paramref name="line"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	/// <summary>
	/// Calculates the distance between this ray and <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	/// <summary>
	/// Calculates the distance between this ray and <paramref name="boundedRay"/>.
	/// </summary>
	/// <param name="boundedRay">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay boundedRay) => DistanceFrom(ClosestPointOn(boundedRay));

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizedWith(Line line) => ParallelizedWith(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizedWith(Line line) => FastParallelizedWith(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizedWith(Ray ray) => ParallelizedWith(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizedWith(Ray ray) => FastParallelizedWith(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizedWith(BoundedRay ray) => ParallelizedWith(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizedWith(BoundedRay ray) => FastParallelizedWith(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction direction) => Direction.IsParallelTo(direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Line line) => IsParallelTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Ray ray) => IsParallelTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(BoundedRay ray) => IsParallelTo(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction) => IsApproximatelyParallelTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyParallelTo(direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line) => IsApproximatelyParallelTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line, Angle tolerance) => IsApproximatelyParallelTo(line.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray) => IsApproximatelyParallelTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray) => IsApproximatelyParallelTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizedAgainst(Line line) => OrthogonalizedAgainst(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizedAgainst(Line line) => FastOrthogonalizedAgainst(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizedAgainst(Ray ray) => OrthogonalizedAgainst(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizedAgainst(Ray ray) => FastOrthogonalizedAgainst(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizedAgainst(BoundedRay ray) => OrthogonalizedAgainst(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizedAgainst(BoundedRay ray) => FastOrthogonalizedAgainst(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction direction) => Direction.IsOrthogonalTo(direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Line line) => IsOrthogonalTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Ray ray) => IsOrthogonalTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(BoundedRay ray) => IsOrthogonalTo(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction) => IsApproximatelyOrthogonalTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyOrthogonalTo(direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line) => IsApproximatelyOrthogonalTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line, Angle tolerance) => IsApproximatelyOrthogonalTo(line.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);

	/// <summary>
	/// Determines whether this ray is exactly colinear with <paramref name="line"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Line line, float lineThickness) => ILineLike.IsExactlyColinearWith(this, line, lineThickness);
	/// <summary>
	/// Determines whether this ray is exactly colinear with <paramref name="ray"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Ray ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);
	/// <summary>
	/// Determines whether this ray is exactly colinear with <paramref name="ray"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(BoundedRay ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);

	/// <summary>
	/// Determines whether this ray is colinear with <paramref name="line"/>, within <see cref="ILineLike.DefaultLineThickness"/> and <see cref="ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line) => ILineLike.IsApproximatelyColinearWith(this, line);
	/// <summary>
	/// Determines whether this ray is colinear with <paramref name="ray"/>, within <see cref="ILineLike.DefaultLineThickness"/> and <see cref="ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	/// <summary>
	/// Determines whether this ray is colinear with <paramref name="ray"/>, within <see cref="ILineLike.DefaultLineThickness"/> and <see cref="ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	/// <summary>
	/// Determines whether this ray is colinear with <paramref name="line"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two line-likes' directions are permitted to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, line, lineThickness, tolerance);
	/// <summary>
	/// Determines whether this ray is colinear with <paramref name="ray"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two line-likes' directions are permitted to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);
	/// <summary>
	/// Determines whether this ray is colinear with <paramref name="ray"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two line-likes' directions are permitted to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);

	/// <summary>
	/// Calculates the (unsigned) angle between this ray's direction and <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => Direction.AngleTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Plane plane) => Direction.IsParallelTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Plane plane) => IsApproximatelyParallelTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	public bool IsApproximatelyParallelTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.Zero, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Plane plane) => Direction.IsOrthogonalTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Plane plane) => IsApproximatelyOrthogonalTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	public bool IsApproximatelyOrthogonalTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.QuarterCircle, tolerance);

	/// <summary>
	/// Attempts to parallelize <paramref name="line"/> with this ray; equivalent to <c>line.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="line">The line to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizationOf(Line line) => line.ParallelizedWith(this);
	/// <summary>
	/// Attempts to parallelize <paramref name="ray"/> with this ray; equivalent to <c>ray.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="ray">The ray to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizationOf(Ray ray) => ray.ParallelizedWith(this);
	/// <summary>
	/// Attempts to parallelize <paramref name="ray"/> with this ray; equivalent to <c>ray.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="ray">The ray segment to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizationOf(BoundedRay ray) => ray.ParallelizedWith(this);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="line">The line to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizationOf(Line line) => line.FastParallelizedWith(this);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizationOf(Ray ray) => ray.FastParallelizedWith(this);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray segment to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizationOf(BoundedRay ray) => ray.FastParallelizedWith(this);
	/// <summary>
	/// Attempts to orthogonalize <paramref name="line"/> against this ray; equivalent to <c>line.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="line">The line to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizationOf(Line line) => line.OrthogonalizedAgainst(this);
	/// <summary>
	/// Attempts to orthogonalize <paramref name="ray"/> against this ray; equivalent to <c>ray.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="ray">The ray to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizationOf(Ray ray) => ray.OrthogonalizedAgainst(this);
	/// <summary>
	/// Attempts to orthogonalize <paramref name="ray"/> against this ray; equivalent to <c>ray.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="ray">The ray segment to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizationOf(BoundedRay ray) => ray.OrthogonalizedAgainst(this);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizationOf(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="line">The line to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizationOf(Line line) => line.FastOrthogonalizedAgainst(this);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizationOf(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizationOf(Ray ray) => ray.FastOrthogonalizedAgainst(this);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizationOf(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray segment to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizationOf(BoundedRay ray) => ray.FastOrthogonalizedAgainst(this);
}
partial struct BoundedRay {
	/// <summary>
	/// Calculates the (unsigned) angle between this ray segment's and <paramref name="line"/>'s directions.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Line line) => ILineLike.AngleTo(this, line);
	/// <summary>
	/// Calculates the (unsigned) angle between this ray segment's and <paramref name="ray"/>'s directions.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Ray ray) => ILineLike.AngleTo(this, ray);
	/// <summary>
	/// Calculates the (unsigned) angle between this ray segment's and <paramref name="ray"/>'s directions.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(BoundedRay ray) => ILineLike.AngleTo(this, ray);

	/// <summary>
	/// Calculates the angle formed between this ray segment's and <paramref name="line"/>'s directions, additionally attributing a sign (+ or -) to the result making it possible to differentiate the winding/chirality between them.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Direction.SignedAngleTo(Direction, Direction)"/> on the two line-likes' <see cref="Direction"/> values.
	/// </remarks>
	/// <param name="line">The target line.</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise winding from this to <paramref name="line"/> will be reported with a positive value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(Line line, Direction clockwiseAxis) => ILineLike.SignedAngleTo(this, line, clockwiseAxis);
	/// <summary>
	/// Calculates the angle formed between this ray segment's and <paramref name="ray"/>'s directions, additionally attributing a sign (+ or -) to the result making it possible to differentiate the winding/chirality between them.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Direction.SignedAngleTo(Direction, Direction)"/> on the two line-likes' <see cref="Direction"/> values.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise winding from this to <paramref name="ray"/> will be reported with a positive value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(Ray ray, Direction clockwiseAxis) => ILineLike.SignedAngleTo(this, ray, clockwiseAxis);
	/// <summary>
	/// Calculates the angle formed between this ray segment's and <paramref name="ray"/>'s directions, additionally attributing a sign (+ or -) to the result making it possible to differentiate the winding/chirality between them.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Direction.SignedAngleTo(Direction, Direction)"/> on the two line-likes' <see cref="Direction"/> values.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise winding from this to <paramref name="ray"/> will be reported with a positive value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(BoundedRay ray, Direction clockwiseAxis) => ILineLike.SignedAngleTo(this, ray, clockwiseAxis);

	/// <summary>
	/// Calculates the square of the distance between this ray segment and <paramref name="line"/>. Cheaper than <see cref="DistanceFrom(Line)"/> when only comparing distances.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => DistanceSquaredFrom(ClosestPointOn(line));
	/// <summary>
	/// Calculates the square of the distance between this ray segment and <paramref name="ray"/>. Cheaper than <see cref="DistanceFrom(Ray)"/> when only comparing distances.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	/// <summary>
	/// Calculates the square of the distance between this ray segment and <paramref name="ray"/>. Cheaper than <see cref="DistanceFrom(BoundedRay)"/> when only comparing distances.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	/// <summary>
	/// Calculates the square of the distance between this ray segment and <paramref name="plane"/>. Cheaper than <see cref="DistanceFrom(Plane)"/> when only comparing distances.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Plane plane) => DistanceSquaredFrom(ClosestPointOn(plane));

	/// <summary>
	/// Determines whether this ray segment intersects <paramref name="line"/>, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedBy(line, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Determines whether this ray segment intersects <paramref name="line"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness) != null;
	/// <summary>
	/// Determines whether this ray segment intersects <paramref name="ray"/>, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Determines whether this ray segment intersects <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	/// <summary>
	/// Determines whether this ray segment intersects <paramref name="ray"/>, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Determines whether this ray segment intersects <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	/// <summary>
	/// Calculates the intersection between this ray segment and <paramref name="line"/>, if any, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => IntersectionWith(line, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Calculates the intersection between this ray segment and <paramref name="line"/>, if any.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness);
	/// <summary>
	/// Calculates the intersection between this ray segment and <paramref name="ray"/>, if any, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Calculates the intersection between this ray segment and <paramref name="ray"/>, if any.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	/// <summary>
	/// Calculates the intersection between this ray segment and <paramref name="ray"/>, if any, using <see cref="ILineLike.DefaultLineThickness"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Calculates the intersection between this ray segment and <paramref name="ray"/>, if any.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How close together the two line-likes' closest points must be for them to be considered intersecting.</param>
	/// <returns><see langword="null"/> if the two line-likes do not intersect; the intersection point otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray segment and <paramref name="line"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line) => FastIntersectionWith(line, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Line,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray segment and <paramref name="line"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">Unused by this overload; present only to mirror the two-argument <see cref="IntersectionWith(Line,float)"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line, float lineThickness) => ILineLike.FastIntersectionWith(this, line, lineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray segment and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(Ray,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray segment and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">Unused by this overload; present only to mirror the two-argument <see cref="IntersectionWith(Ray,float)"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray segment and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray) => FastIntersectionWith(ray, ILineLike.DefaultLineThickness);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith(BoundedRay,float)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this ray segment and <paramref name="ray"/> do intersect. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">Unused by this overload; present only to mirror the two-argument <see cref="IntersectionWith(BoundedRay,float)"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.FastIntersectionWith(this, ray, lineThickness);

	/// <summary>
	/// Returns the point on <paramref name="line"/> that is closest to this ray segment.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	/// <summary>
	/// Returns the point on <paramref name="ray"/> that is closest to this ray segment.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	/// <summary>
	/// Returns the point on <paramref name="boundedRay"/> that is closest to this ray segment.
	/// </summary>
	/// <param name="boundedRay">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay boundedRay) => boundedRay.PointClosestTo(this);

	/// <summary>
	/// Calculates the distance between this ray segment and <paramref name="line"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	/// <summary>
	/// Calculates the distance between this ray segment and <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	/// <summary>
	/// Calculates the distance between this ray segment and <paramref name="boundedRay"/>.
	/// </summary>
	/// <param name="boundedRay">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay boundedRay) => DistanceFrom(ClosestPointOn(boundedRay));

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizedWith(Line line) => ParallelizedWith(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizedWith(Line line) => FastParallelizedWith(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizedWith(Ray ray) => ParallelizedWith(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizedWith(Ray ray) => FastParallelizedWith(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizedWith(BoundedRay ray) => ParallelizedWith(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizedWith(BoundedRay ray) => FastParallelizedWith(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction direction) => Direction.IsParallelTo(direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Line line) => IsParallelTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Ray ray) => IsParallelTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(BoundedRay ray) => IsParallelTo(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction) => IsApproximatelyParallelTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyParallelTo(direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line) => IsApproximatelyParallelTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line, Angle tolerance) => IsApproximatelyParallelTo(line.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray) => IsApproximatelyParallelTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray) => IsApproximatelyParallelTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray, Angle tolerance) => IsApproximatelyParallelTo(ray.Direction, tolerance);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizedAgainst(Line line) => OrthogonalizedAgainst(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizedAgainst(Line line) => FastOrthogonalizedAgainst(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizedAgainst(Ray ray) => OrthogonalizedAgainst(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizedAgainst(Ray ray) => FastOrthogonalizedAgainst(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizedAgainst(BoundedRay ray) => OrthogonalizedAgainst(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizedAgainst(BoundedRay ray) => FastOrthogonalizedAgainst(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction direction) => Direction.IsOrthogonalTo(direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Line line) => IsOrthogonalTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Ray ray) => IsOrthogonalTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(BoundedRay ray) => IsOrthogonalTo(ray.Direction);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction) => IsApproximatelyOrthogonalTo(direction, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Direction direction, Angle tolerance) => Direction.IsApproximatelyOrthogonalTo(direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line) => IsApproximatelyOrthogonalTo(line.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line, Angle tolerance) => IsApproximatelyOrthogonalTo(line.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray) => IsApproximatelyOrthogonalTo(ray.Direction);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray, Angle tolerance) => IsApproximatelyOrthogonalTo(ray.Direction, tolerance);

	/// <summary>
	/// Determines whether this ray segment is exactly colinear with <paramref name="line"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Line line, float lineThickness) => ILineLike.IsExactlyColinearWith(this, line, lineThickness);
	/// <summary>
	/// Determines whether this ray segment is exactly colinear with <paramref name="ray"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(Ray ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);
	/// <summary>
	/// Determines whether this ray segment is exactly colinear with <paramref name="ray"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsExactlyColinearWith(BoundedRay ray, float lineThickness) => ILineLike.IsExactlyColinearWith(this, ray, lineThickness);

	/// <summary>
	/// Determines whether this ray segment is colinear with <paramref name="line"/>, within <see cref="ILineLike.DefaultLineThickness"/> and <see cref="ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line) => ILineLike.IsApproximatelyColinearWith(this, line);
	/// <summary>
	/// Determines whether this ray segment is colinear with <paramref name="ray"/>, within <see cref="ILineLike.DefaultLineThickness"/> and <see cref="ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	/// <summary>
	/// Determines whether this ray segment is colinear with <paramref name="ray"/>, within <see cref="ILineLike.DefaultLineThickness"/> and <see cref="ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray) => ILineLike.IsApproximatelyColinearWith(this, ray);
	/// <summary>
	/// Determines whether this ray segment is colinear with <paramref name="line"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="line">The target line.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two line-likes' directions are permitted to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Line line, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, line, lineThickness, tolerance);
	/// <summary>
	/// Determines whether this ray segment is colinear with <paramref name="ray"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="ray">The target ray.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two line-likes' directions are permitted to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(Ray ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);
	/// <summary>
	/// Determines whether this ray segment is colinear with <paramref name="ray"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="ray">The target ray segment.</param>
	/// <param name="lineThickness">How far apart the two line-likes are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two line-likes' directions are permitted to be.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyColinearWith(BoundedRay ray, float lineThickness, Angle tolerance) => ILineLike.IsApproximatelyColinearWith(this, ray, lineThickness, tolerance);

	/// <summary>
	/// Calculates the (unsigned) angle between this ray segment's direction and <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The target plane.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => Direction.AngleTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Plane plane) => Direction.IsParallelTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Plane plane) => IsApproximatelyParallelTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	public bool IsApproximatelyParallelTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.Zero, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Plane plane) => Direction.IsOrthogonalTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Plane plane) => IsApproximatelyOrthogonalTo(plane, ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees);
	/// <inheritdoc/>
	public bool IsApproximatelyOrthogonalTo(Plane plane, Angle tolerance) => AngleTo(plane).Equals(Angle.QuarterCircle, tolerance);

	/// <summary>
	/// Attempts to parallelize <paramref name="line"/> with this ray segment; equivalent to <c>line.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="line">The line to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizationOf(Line line) => line.ParallelizedWith(this);
	/// <summary>
	/// Attempts to parallelize <paramref name="ray"/> with this ray segment; equivalent to <c>ray.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="ray">The ray to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizationOf(Ray ray) => ray.ParallelizedWith(this);
	/// <summary>
	/// Attempts to parallelize <paramref name="ray"/> with this ray segment; equivalent to <c>ray.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="ray">The ray segment to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizationOf(BoundedRay ray) => ray.ParallelizedWith(this);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="line">The line to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizationOf(Line line) => line.FastParallelizedWith(this);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizationOf(Ray ray) => ray.FastParallelizedWith(this);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray segment to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizationOf(BoundedRay ray) => ray.FastParallelizedWith(this);
	/// <summary>
	/// Attempts to orthogonalize <paramref name="line"/> against this ray segment; equivalent to <c>line.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="line">The line to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizationOf(Line line) => line.OrthogonalizedAgainst(this);
	/// <summary>
	/// Attempts to orthogonalize <paramref name="ray"/> against this ray segment; equivalent to <c>ray.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="ray">The ray to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizationOf(Ray ray) => ray.OrthogonalizedAgainst(this);
	/// <summary>
	/// Attempts to orthogonalize <paramref name="ray"/> against this ray segment; equivalent to <c>ray.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="ray">The ray segment to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizationOf(BoundedRay ray) => ray.OrthogonalizedAgainst(this);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizationOf(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="line">The line to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizationOf(Line line) => line.FastOrthogonalizedAgainst(this);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizationOf(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizationOf(Ray ray) => ray.FastOrthogonalizedAgainst(this);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizationOf(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="ray">The ray segment to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizationOf(BoundedRay ray) => ray.FastOrthogonalizedAgainst(this);
}

partial struct Location : ILineDistanceMeasurable, ILineClosestExogenousPointDiscoverable, ILineContainable {
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => line.DistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => ray.DistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay ray) => ray.DistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => line.DistanceSquaredFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => ray.DistanceSquaredFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => ray.DistanceSquaredFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay ray) => ray.PointClosestTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(Line line) => line.Contains(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(Ray ray) => ray.Contains(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(BoundedRay ray) => ray.Contains(this);
}
partial struct Direction : ILineOrthogonalizationTarget, ILineParallelizationTarget {
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Line line) => line.IsOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Ray ray) => ray.IsOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(BoundedRay ray) => ray.IsOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Line line) => line.IsParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Ray ray) => ray.IsParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(BoundedRay ray) => ray.IsParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line) => line.IsApproximatelyOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line, Angle tolerance) => line.IsApproximatelyOrthogonalTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray) => ray.IsApproximatelyOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray, Angle tolerance) => ray.IsApproximatelyOrthogonalTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray) => ray.IsApproximatelyOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray, Angle tolerance) => ray.IsApproximatelyOrthogonalTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line) => line.IsApproximatelyParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line, Angle tolerance) => line.IsApproximatelyParallelTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray) => ray.IsApproximatelyParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray, Angle tolerance) => ray.IsApproximatelyParallelTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray) => ray.IsApproximatelyParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray, Angle tolerance) => ray.IsApproximatelyParallelTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizationOf(Line line) => line.OrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizationOf(Ray ray) => ray.OrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizationOf(BoundedRay ray) => ray.OrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizationOf(Line line) => line.FastOrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizationOf(Ray ray) => ray.FastOrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizationOf(BoundedRay ray) => ray.FastOrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizationOf(Line line) => line.ParallelizedWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizationOf(Ray ray) => ray.ParallelizedWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizationOf(BoundedRay ray) => ray.ParallelizedWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizationOf(Line line) => line.FastParallelizedWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizationOf(Ray ray) => ray.FastParallelizedWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizationOf(BoundedRay ray) => ray.FastParallelizedWith(this);
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
	IIntersectionDeterminable<Plane, BoundedRay, Location>,
	ILineAngleMeasurable {
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Line line) => line.AngleTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Ray ray) => ray.AngleTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(BoundedRay ray) => ray.AngleTo(this);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ProjectionOf(Line line) => line.ProjectedOnTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ProjectionOf(Ray ray) => ray.ProjectedOnTo(this);
	/// <summary>
	/// Returns <paramref name="ray"/>'s projection on to this plane; equivalent to <c>ray.ProjectedOnTo(this)</c>.
	/// </summary>
	/// <remarks>
	/// Unlike the general <see cref="IProjectionTarget{TOther}.ProjectionOf"/> contract, this overload never returns <see langword="null"/>: projecting a <see cref="BoundedRay"/> on to a plane is always well-defined (it may just come out zero-length).
	/// </remarks>
	/// <param name="ray">The target ray segment.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay ProjectionOf(BoundedRay ray) => ray.ProjectedOnTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastProjectionOf(Line line) => line.FastProjectedOnTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastProjectionOf(Ray ray) => ray.FastProjectedOnTo(this);
	BoundedRay? IProjectionTarget<BoundedRay>.ProjectionOf(BoundedRay ray) => ProjectionOf(ray);
	BoundedRay IProjectionTarget<BoundedRay>.FastProjectionOf(BoundedRay ray) => ProjectionOf(ray);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? OrthogonalizationOf(Line line) => line.OrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? OrthogonalizationOf(Ray ray) => ray.OrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastOrthogonalizationOf(Line line) => line.FastOrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastOrthogonalizationOf(Ray ray) => ray.FastOrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? OrthogonalizationOf(BoundedRay ray) => ray.OrthogonalizedAgainst(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastOrthogonalizationOf(BoundedRay ray) => ray.FastOrthogonalizedAgainst(this);


	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ParallelizationOf(Line line) => line.ParallelizedWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ParallelizationOf(Ray ray) => ray.ParallelizedWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ParallelizationOf(BoundedRay ray) => ray.ParallelizedWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastParallelizationOf(Line line) => line.FastParallelizedWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastParallelizationOf(Ray ray) => ray.FastParallelizedWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastParallelizationOf(BoundedRay ray) => ray.FastParallelizedWith(this);


	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line? ReflectionOf(Line line) => line.ReflectedBy(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line FastReflectionOf(Line line) => line.FastReflectedBy(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(Line line) => line.IncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(Line line) => line.FastIncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ReflectionOf(Ray ray) => ray.ReflectedBy(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray FastReflectionOf(Ray ray) => ray.FastReflectedBy(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(Ray ray) => ray.IncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(Ray ray) => ray.FastIncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ReflectionOf(BoundedRay ray) => ray.ReflectedBy(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay FastReflectionOf(BoundedRay ray) => ray.FastReflectedBy(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(BoundedRay ray) => ray.IncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(BoundedRay ray) => ray.FastIncidentAngleWith(this);


	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => line.IsIntersectedBy(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => ray.IsIntersectedBy(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => ray.IsIntersectedBy(this);
	/// <summary>
	/// Splits <paramref name="line"/> into two rays at the point it intersects this plane, if it does; equivalent to <c>line.SplitBy(this)</c>.
	/// </summary>
	/// <param name="line">The line to split.</param>
	/// <returns><see langword="null"/> if <paramref name="line"/> does not intersect this plane; otherwise a pair of rays starting at the split point and pointing in opposite directions along <paramref name="line"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<Ray, Ray>? Split(Line line) => line.SplitBy(this);
	/// <summary>
	/// Splits <paramref name="ray"/> into a bounded and an unbounded ray at the point it intersects this plane, if it does; equivalent to <c>ray.SplitBy(this)</c>.
	/// </summary>
	/// <param name="ray">The ray to split.</param>
	/// <returns><see langword="null"/> if <paramref name="ray"/> does not intersect this plane; otherwise the bounded ray from <paramref name="ray"/>'s start to the split point, and the ray continuing on from the split point in the same direction.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<BoundedRay, Ray>? Split(Ray ray) => ray.SplitBy(this);
	/// <summary>
	/// Splits <paramref name="ray"/> into two at the point it intersects this plane, if it does; equivalent to <c>ray.SplitBy(this)</c>.
	/// </summary>
	/// <param name="ray">The ray segment to split.</param>
	/// <returns><see langword="null"/> if <paramref name="ray"/> does not intersect this plane within its bounds; otherwise a pair of the two resultant ray segments either side of the split point.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<BoundedRay, BoundedRay>? Split(BoundedRay ray) => ray.SplitBy(this);
	/// <summary>
	/// Executes the same function as <see cref="Split(Line)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="line"/> does intersect this plane. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="line">The line to split.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<Ray, Ray> FastSplit(Line line) => line.FastSplitBy(this);
	/// <summary>
	/// Executes the same function as <see cref="Split(Ray)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/> does intersect this plane. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The ray to split.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<BoundedRay, Ray> FastSplit(Ray ray) => ray.FastSplitBy(this);
	/// <summary>
	/// Executes the same function as <see cref="Split(BoundedRay)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="ray"/> does intersect this plane within its bounds. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="ray">The ray segment to split.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Pair<BoundedRay, BoundedRay> FastSplit(BoundedRay ray) => ray.FastSplitBy(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => line.IntersectionWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => ray.IntersectionWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => ray.IntersectionWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Line line) => line.FastIntersectionWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(Ray ray) => ray.FastIntersectionWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location FastIntersectionWith(BoundedRay ray) => ray.FastIntersectionWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => line.DistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(Line line) => line.SignedDistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => line.DistanceSquaredFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => ray.DistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(Ray ray) => ray.SignedDistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => ray.DistanceSquaredFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay ray) => ray.DistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(BoundedRay ray) => ray.SignedDistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => ray.DistanceSquaredFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(Line line) => line.ClosestPointOn(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(Ray ray) => ray.ClosestPointOn(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(BoundedRay ray) => ray.ClosestPointOn(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay ray) => ray.PointClosestTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo(Line line) => line.RelationshipTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo(Ray ray) => ray.RelationshipTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo(BoundedRay ray) => ray.RelationshipTo(this);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Line line) => line.IsOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Ray ray) => ray.IsOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(BoundedRay ray) => ray.IsOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line) => line.IsApproximatelyOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Line line, Angle tolerance) => line.IsApproximatelyOrthogonalTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray) => ray.IsApproximatelyOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Ray ray, Angle tolerance) => ray.IsApproximatelyOrthogonalTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray) => ray.IsApproximatelyOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(BoundedRay ray, Angle tolerance) => ray.IsApproximatelyOrthogonalTo(this, tolerance);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Line line) => line.IsParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Ray ray) => ray.IsParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(BoundedRay ray) => ray.IsParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line) => line.IsApproximatelyParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Line line, Angle tolerance) => line.IsApproximatelyParallelTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray) => ray.IsApproximatelyParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Ray ray, Angle tolerance) => ray.IsApproximatelyParallelTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray) => ray.IsApproximatelyParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(BoundedRay ray, Angle tolerance) => ray.IsApproximatelyParallelTo(this, tolerance);
}