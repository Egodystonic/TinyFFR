// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public enum PlaneObjectRelationship {
	PlaneIntersectsObject,
	PlaneFacesTowardsObject,
	PlaneFacesAwayFromObject
}
public readonly record struct ConvexShapeLineIntersection(Location First, Location? Second) {
	public Location First { get; } = First;
	public Location? Second { get; } = Second;
	
	public static ConvexShapeLineIntersection? FromTwoPotentiallyNullArgs(Location? a, Location? b) {
		return (a, b) switch {
			(not null, _) => new(a.Value, b),
			(null, not null) => new(b.Value, a),
			_ => null
		};
	}
}

public interface IShape<TSelf> : IGeometryPrimitive<TSelf> where TSelf : IShape<TSelf> {
	TSelf ScaledBy(float scalar);
}
public interface IFullyInteractableConvexShape<TSelf> : 
	IShape<TSelf>,
	ISurfaceDistanceMeasurable<Location>, IContainmentTestable<Location>, IClosestEndogenousSurfacePointDiscoverable<Location>,
	ILineSurfaceDistanceMeasurable, ILineClosestSurfacePointDiscoverable,
	ISignedDistanceMeasurable<Plane>, IClosestPointDiscoverable<Plane>, IRelationshipDeterminable<Plane, PlaneObjectRelationship>,
	ILineIntersectable<ConvexShapeLineIntersection>
	where TSelf : IFullyInteractableConvexShape<TSelf> {

}

// public interface IPointTestable {
// 	Location ClosestPointTo(Location location);
// 	float DistanceFrom(Location location);
// 	bool Contains(Location location);
// }
// public interface ILineTestable {
// 	Location ClosestPointTo<TLine>(TLine line) where TLine : ILine;
// 	Location ClosestPointOn<TLine>(TLine line) where TLine : ILine;
// 	float DistanceFrom<TLine>(TLine line) where TLine : ILine;
// 	Location? IntersectionWith<TLine>(TLine line) where TLine : ILine;
// }
// public enum PlaneObjectRelationship {
// 	PlaneIntersectsObject,
// 	PlaneFacesTowardsObject,
// 	PlaneFacesAwayFromObject
// }
// public interface IPlaneTestable {
// 	Location ClosestPointTo(Plane plane);
// 	Location ClosestPointOn(Plane plane);
// 	float SignedDistanceFrom(Plane plane); // TODO xmldoc note that positive = in front of plane, negative = behind
// 	float DistanceFrom(Plane plane);
// 	PlaneObjectRelationship RelationshipTo(Plane plane);
// }
// public readonly record struct ConvexShapeLineIntersection(Location? First, Location? Second) {
// 	public static readonly ConvexShapeLineIntersection NoIntersections = new(null, null);
//
// 	public Location? First { get; } = First;
// 	[property: MemberNotNull(nameof(First))]
// 	public Location? Second { get; } = Second;
// 	
// 	public bool None => First == null;
// 	public bool AtLeastOne => First != null;
// 	public bool Both => Second != null;
//
// 	public static ConvexShapeLineIntersection FromTwoPotentiallyNullArgs(Location? a, Location? b) => a == null ? new(b, a) : new(a, b);
// }
// // TODO mention in XMLDoc that this represents specifically a shape whose origin is always Location.Origin (e.g. all parameters are shape-local). Or think of a better naming prefix for them all (LocalXyz?) (XyzParameters/Descriptor?)
// public interface IShape : IPointTestable, ILineTestable, IPlaneTestable {
// 	Location ClosestPointOnSurfaceTo(Location location);
// 	Location ClosestPointOnSurfaceTo<TLine>(TLine line) where TLine : ILine;
// 	Location ClosestPointOnSurfaceTo(Plane plane);
// 	Location ClosestPointToSurfaceOn<TLine>(TLine line) where TLine : ILine;
// 	Location ClosestPointToSurfaceOn(Plane plane);
// 	float SurfaceDistanceFrom(Location location);
// 	float SurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine;
// 	float SurfaceDistanceFrom(Plane plane);
//
// 	// TODO RefIterator for faces and vertices
// 	// TODO would it be better to have type that holds a ref field of type T and can then easily iterate against that T assuming it implements an interface? Something like RefEnumerable? Yes, the interface approach means we can only have one implementation but that's fiiiiine
// }
// public interface IShape<TSelf> : IShape, IMathPrimitive<TSelf, float>, IInterpolatable<TSelf>, IBoundedRandomizable<TSelf> where TSelf : IShape<TSelf> {
// 	TSelf ScaledBy(float scalar);
// }
// public interface IConvexShape : IShape {
// 	Location? ILineTestable.IntersectionWith<TLine>(TLine line) => IntersectionWith(line).First;
// 	new ConvexShapeLineIntersection IntersectionWith<TLine>(TLine line) where TLine : ILine;
// }
// public interface IPlaneIntersectableShape<TPlaneIntersection> : IShape where TPlaneIntersection : struct {
// 	TPlaneIntersection? IntersectionWith(Plane plane);
// }
// // TODO add TPlaneIntersection overload of IShape e.g. Sphere -> Circle and Cuboid -> four points; replace TrySplit on Sphere with IntersectionWith
// // Circle and BoundedPlane
// // Finding polygon should be as easy as getting the intersection of the plane with every edge, but just in case: https://www.asawicki.info/news_1428_finding_polygon_of_plane-aabb_intersection
//
// // ==================== Below this line: Various "inverted" shape testing methods defined as either extensions or added directly in partial definitions ====================
// // I do it this way to keep these definitions close by as they're basically just the same as the definitions above but "the inverse of"
// // and I think it makes more sense to keep it all in this one file.
// // ReSharper disable UnusedTypeParameter Type parameterization instead of directly using interface type is used to prevent boxing (instead relying on reification of each parameter combination)
// public static class ShapeExtensions {
// 	// These are implemented as extension methods because it lets us generalize over all ILine types for free
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static Location ClosestPointTo<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.ClosestPointOn(@this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static Location ClosestPointOn<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.ClosestPointTo(@this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static float DistanceFrom<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.DistanceFrom(@this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static ConvexShapeLineIntersection IntersectionWith<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IConvexShape => shape.IntersectionWith(@this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static Location ClosestPointOnSurfaceOf<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.ClosestPointOnSurfaceTo(@this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static Location ClosestPointToSurfaceOf<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.ClosestPointToSurfaceOn(@this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static float DistanceFromSurfaceOf<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.SurfaceDistanceFrom(@this);
//
// 	// These are implemented as extension methods because Plane already has the exact same method names with the same number of generic arguments, so it wouldn't compile otherwise
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static Location ClosestPointTo<TPlaneTestable>(this Plane @this, TPlaneTestable planeTestableObject) where TPlaneTestable : IPlaneTestable => planeTestableObject.ClosestPointOn(@this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static Location ClosestPointOn<TPlaneTestable>(this Plane @this, TPlaneTestable planeTestableObject) where TPlaneTestable : IPlaneTestable => planeTestableObject.ClosestPointTo(@this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static float DistanceFrom<TPlaneTestable>(this Plane @this, TPlaneTestable planeTestableObject) where TPlaneTestable : IPlaneTestable => planeTestableObject.DistanceFrom(@this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public static PlaneObjectRelationship RelationshipTo<TPlaneTestable>(this Plane @this, TPlaneTestable planeTestableObject) where TPlaneTestable : IPlaneTestable => planeTestableObject.RelationshipTo(@this);
// }
//
// partial struct Location {
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public Location ClosestPointOn<TPointTestable>(TPointTestable pointTestableObject) where TPointTestable : IPointTestable => pointTestableObject.ClosestPointTo(this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public float DistanceFrom<TPointTestable>(TPointTestable pointTestableObject) where TPointTestable : IPointTestable => pointTestableObject.DistanceFrom(this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public bool IsContainedBy<TPointTestable>(TPointTestable pointTestableObject) where TPointTestable : IPointTestable => pointTestableObject.Contains(this);
//
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IShape => shape.ClosestPointOnSurfaceTo(this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IShape => shape.SurfaceDistanceFrom(this);
// }
//
// partial struct Plane {
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IShape => shape.ClosestPointOnSurfaceTo(this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public Location ClosestPointToSurfaceOf<TShape>(TShape shape) where TShape : IShape => shape.ClosestPointToSurfaceOn(this);
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IShape => shape.SurfaceDistanceFrom(this);
// }