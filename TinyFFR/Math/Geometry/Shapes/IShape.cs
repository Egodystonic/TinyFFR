// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents the result of intersecting a line-like object with a convex shape: either one or two intersection points, depending on whether the line grazes the shape's surface or passes through its interior.
/// </summary>
/// <param name="First">The first intersection point.</param>
/// <param name="Second">The second intersection point, or <see langword="null"/> if there is only one.</param>
public readonly record struct ConvexShapeLineIntersection(Location First, Location? Second) {
	/// <summary>
	/// The first intersection point.
	/// </summary>
	public Location First { get; } = First;
	/// <summary>
	/// The second intersection point, or <see langword="null"/> if there is only one.
	/// </summary>
	public Location? Second { get; } = Second;

	/// <summary>
	/// Constructs a <see cref="ConvexShapeLineIntersection"/> from two candidate points, both of which may or may not be null.
	/// </summary>
	/// <param name="a">The first candidate point.</param>
	/// <param name="b">The second candidate point.</param>
	/// <returns><see langword="null"/> if both <paramref name="a"/> and <paramref name="b"/> are <see langword="null"/>;
	/// otherwise a <see cref="ConvexShapeLineIntersection"/> with whichever of <paramref name="a"/>/<paramref name="b"/> is non-null assigned to <see cref="First"/>
	/// (and the other, if also non-null, assigned to <see cref="Second"/>).</returns>
	public static ConvexShapeLineIntersection? FromTwoPotentiallyNullArgs(Location? a, Location? b) {
		return (a, b) switch {
			(not null, _) => new(a.Value, b),
			(null, not null) => new(b.Value, a),
			_ => null
		};
	}
}

/// <summary>
/// Tag interface representing any shape geometric primitive.
/// </summary>
public interface IShape : IMathPrimitive, IPhysicalValidityDeterminable;
/// <summary>
/// Extension of <see cref="IShape"/> that includes a self type parameter allowing for
/// more functional definitions.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
public interface IShape<TSelf> :
	IShape,
	IMathPrimitive<TSelf>,
	IScalable<TSelf>,
	IInterpolatable<TSelf>
	where TSelf : IShape<TSelf>;
/// <summary>
/// Trait interface used to mark a shape as convex: a shape where a straight line between any two points inside it never leaves the shape.
/// </summary>
/// <remarks>
/// Convexity is what allows a shape to support a single well-defined closest point and at most two intersection points
/// with any given line, which is why this interface exists as the shared foundation for <see cref="Cuboid"/>, <see cref="Sphere"/>, and their wrapper types.
/// </remarks>
public interface IConvexShape : IShape,
	IClosestEndogenousPointDiscoverable<Location>,
	IDistanceMeasurable<Location>,
	IContainer<Location>,

	IReflectionTarget<Ray, Ray>,
	IReflectionTarget<BoundedRay, BoundedRay>,
	ILineClosestExogenousPointDiscoverable,
	ILineClosestEndogenousPointDiscoverable,
	ILineDistanceMeasurable,
	IContainer<BoundedRay>,
	ILineIntersectionDeterminable<ConvexShapeLineIntersection>,

	ISignedDistanceMeasurable<Plane>,
	IClosestEndogenousPointDiscoverable<Plane>,
	IClosestExogenousPointDiscoverable<Plane>,
	IRelatable<Plane, PlaneObjectRelationship> {
	/// <summary>
	/// Returns a random location within the interior of this shape, uniformly distributed by volume.
	/// </summary>
	Location GetRandomInternalLocation();

	/// <summary>
	/// Returns the point on the surface of this shape that is closest to <paramref name="point"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="IClosestEndogenousPointDiscoverable{TOther}.PointClosestTo"/>, this always returns a point on the surface, even if <paramref name="point"/> is inside this shape.
	/// </remarks>
	/// <param name="point">The point to find the closest surface point to.</param>
	Location SurfacePointClosestTo(Location point);
	/// <summary>
	/// Calculates the distance between <paramref name="point"/> and the closest point on the surface of this shape.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="IDistanceMeasurable{TOther}.DistanceFrom"/>, this is not clamped to <c>0f</c> when <paramref name="point"/> is inside this shape.
	/// </remarks>
	/// <param name="point">The point to measure the distance from.</param>
	float SurfaceDistanceFrom(Location point);
	/// <summary>
	/// Calculates the square of the distance between <paramref name="point"/> and the closest point on the surface of this shape.
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="SurfaceDistanceFrom(Location)"/> as it avoids a square root, and is sufficient when you only need to compare distances rather than know the exact value.
	/// </remarks>
	/// <param name="point">The point to measure the distance from.</param>
	float SurfaceDistanceSquaredFrom(Location point);

	/// <summary>
	/// Returns the point on the surface of this shape that is closest to <paramref name="line"/>.
	/// </summary>
	/// <param name="line">The line to find the closest surface point to.</param>
	Location SurfacePointClosestTo(Line line);
	/// <summary>
	/// Returns the point on <paramref name="line"/> that is closest to the surface of this shape.
	/// </summary>
	/// <param name="line">The line to find the closest point on.</param>
	Location ClosestPointToSurfaceOn(Line line);
	/// <summary>
	/// Calculates the distance between <paramref name="line"/> and the closest point on the surface of this shape.
	/// </summary>
	/// <param name="line">The line to measure the distance from.</param>
	float SurfaceDistanceFrom(Line line);
	/// <summary>
	/// Calculates the square of the distance between <paramref name="line"/> and the closest point on the surface of this shape.
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="SurfaceDistanceFrom(Line)"/> as it avoids a square root, and is sufficient when you only need to compare distances rather than know the exact value.
	/// </remarks>
	/// <param name="line">The line to measure the distance from.</param>
	float SurfaceDistanceSquaredFrom(Line line);

	/// <summary>
	/// Returns the point on the surface of this shape that is closest to <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The ray to find the closest surface point to.</param>
	Location SurfacePointClosestTo(Ray ray);
	/// <summary>
	/// Returns the point on <paramref name="ray"/> that is closest to the surface of this shape.
	/// </summary>
	/// <param name="ray">The ray to find the closest point on.</param>
	Location ClosestPointToSurfaceOn(Ray ray);
	/// <summary>
	/// Calculates the distance between <paramref name="ray"/> and the closest point on the surface of this shape.
	/// </summary>
	/// <param name="ray">The ray to measure the distance from.</param>
	float SurfaceDistanceFrom(Ray ray);
	/// <summary>
	/// Calculates the square of the distance between <paramref name="ray"/> and the closest point on the surface of this shape.
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="SurfaceDistanceFrom(Ray)"/> as it avoids a square root, and is sufficient when you only need to compare distances rather than know the exact value.
	/// </remarks>
	/// <param name="ray">The ray to measure the distance from.</param>
	float SurfaceDistanceSquaredFrom(Ray ray);

	/// <summary>
	/// Returns the point on the surface of this shape that is closest to <paramref name="ray"/>.
	/// </summary>
	/// <param name="ray">The ray to find the closest surface point to.</param>
	Location SurfacePointClosestTo(BoundedRay ray);
	/// <summary>
	/// Returns the point on <paramref name="ray"/> that is closest to the surface of this shape.
	/// </summary>
	/// <param name="ray">The ray to find the closest point on.</param>
	Location ClosestPointToSurfaceOn(BoundedRay ray);
	/// <summary>
	/// Calculates the distance between <paramref name="ray"/> and the closest point on the surface of this shape.
	/// </summary>
	/// <param name="ray">The ray to measure the distance from.</param>
	float SurfaceDistanceFrom(BoundedRay ray);
	/// <summary>
	/// Calculates the square of the distance between <paramref name="ray"/> and the closest point on the surface of this shape.
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="SurfaceDistanceFrom(BoundedRay)"/> as it avoids a square root, and is sufficient when you only need to compare distances rather than know the exact value.
	/// </remarks>
	/// <param name="ray">The ray to measure the distance from.</param>
	float SurfaceDistanceSquaredFrom(BoundedRay ray);

	/// <summary>
	/// Returns the point on the surface of this shape that is closest to <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The plane to find the closest surface point to.</param>
	Location SurfacePointClosestTo(Plane plane);
	/// <summary>
	/// Returns the point on <paramref name="plane"/> that is closest to the surface of this shape.
	/// </summary>
	/// <param name="plane">The plane to find the closest point on.</param>
	Location ClosestPointToSurfaceOn(Plane plane);
}
/// <summary>
/// Extension of <see cref="IConvexShape"/> that includes a self type parameter allowing for more functional definitions.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
public interface IConvexShape<TSelf> : IConvexShape, IShape<TSelf> where TSelf : IConvexShape<TSelf>;
