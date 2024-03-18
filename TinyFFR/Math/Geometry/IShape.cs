// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public interface IPointTestable {
	Location ClosestPointTo(Location location);
	float DistanceFrom(Location location);
	bool Contains(Location location);
}
public interface ILineTestable {
	Location ClosestPointTo<TLine>(TLine line) where TLine : ILine;
	Location ClosestPointOn<TLine>(TLine line) where TLine : ILine;
	float DistanceFrom<TLine>(TLine line) where TLine : ILine;
	Location? IntersectionPointWith<TLine>(TLine line) where TLine : ILine;
}
public enum PlaneObjectRelationship {
	PlaneIntersectsObject,
	PlaneFacesTowardsObject,
	PlaneFacesAwayFromObject
}
public interface IPlaneTestable {
	Location ClosestPointTo(Plane plane);
	Location ClosestPointOn(Plane plane);
	float SignedDistanceFrom(Plane plane); // TODO xmldoc note that positive = in front of plane, negative = behind
	float DistanceFrom(Plane plane);
	PlaneObjectRelationship RelationshipTo(Plane plane);
}
// TODO mention in XMLDoc that this represents specifically a shape whose origin is always Location.Origin (e.g. all parameters are shape-local). Or think of a better naming prefix for them all (LocalXyz?) (XyzParameters/Descriptor?)
public interface IShape : IPointTestable, ILineTestable, IPlaneTestable {
	Location ClosestPointOnSurfaceTo(Location location);
	Location ClosestPointOnSurfaceTo<TLine>(TLine line) where TLine : ILine;
	Location ClosestPointOnSurfaceTo(Plane plane);
	Location ClosestPointToSurfaceOn<TLine>(TLine line) where TLine : ILine;
	Location ClosestPointToSurfaceOn(Plane plane);
	float SurfaceDistanceFrom(Location location);
	float SurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine;
	float SurfaceDistanceFrom(Plane plane);

	// These two lines are essentially a rename of IntersectionPointWith to SurfaceIntersectionPointWith for shapes
	// as anything BUT a surface intersection doesn't really make much sense once you think about it (e.g. that's not really an intersection that's just a contains)
	// and I wanted to be explicit to keep with the naming convention of all the other SurfaceXyz methods.
	Location? ILineTestable.IntersectionPointWith<TLine>(TLine line) => SurfaceIntersectionPointWith(line);
	Location? SurfaceIntersectionPointWith<TLine>(TLine line) where TLine : ILine;

	// TODO RefIterator for faces and vertices
	// TODO would it be better to have type that holds a ref field of type T and can then easily iterate against that T assuming it implements an interface? Something like RefEnumerable? Yes, the interface approach means we can only have one implementation but that's fiiiiine
}
public interface IShape<TSelf> : IShape, IMathPrimitive<TSelf, float>, IInterpolatable<TSelf>, IBoundedRandomizable<TSelf> where TSelf : IShape<TSelf> {
	TSelf ScaledBy(float scalar);
}

// ==================== Below this line: Various "inverted" shape testing methods defined as either extensions or added directly in partial definitions ====================
// I do it this way to keep these definitions close by as they're basically just the same as the definitions above but "the inverse of"
// and I think it makes more sense to keep it all in this one file.
// ReSharper disable UnusedTypeParameter Type parameterization instead of directly using interface type is used to prevent boxing (instead relying on reification of each parameter combination)
public static class ShapeExtensions {
	// These are implemented as extension methods because it lets us generalize over all ILine types for free
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointTo<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.ClosestPointOn(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOn<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.ClosestPointTo(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFrom<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.DistanceFrom(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location? IntersectionPointWith<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.IntersectionPointWith(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOnSurfaceOf<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.ClosestPointOnSurfaceTo(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointToSurfaceOf<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.ClosestPointToSurfaceOn(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFromSurfaceOf<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.SurfaceDistanceFrom(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location? IntersectionPointWithSurfaceOf<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine where TShape : IShape => shape.SurfaceIntersectionPointWith(@this);

	// These are implemented as extension methods because Plane already has the exact same method names with the same number of generic arguments, so it wouldn't compile otherwise
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointTo<TPlaneTestable>(this Plane @this, TPlaneTestable planeTestableObject) where TPlaneTestable : IPlaneTestable => planeTestableObject.ClosestPointOn(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOn<TPlaneTestable>(this Plane @this, TPlaneTestable planeTestableObject) where TPlaneTestable : IPlaneTestable => planeTestableObject.ClosestPointTo(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFrom<TPlaneTestable>(this Plane @this, TPlaneTestable planeTestableObject) where TPlaneTestable : IPlaneTestable => planeTestableObject.DistanceFrom(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PlaneObjectRelationship RelationshipTo<TPlaneTestable>(this Plane @this, TPlaneTestable planeTestableObject) where TPlaneTestable : IPlaneTestable => planeTestableObject.RelationshipTo(@this);
}

partial struct Location {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn<TPointTestable>(TPointTestable pointTestableObject) where TPointTestable : IPointTestable => pointTestableObject.ClosestPointTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TPointTestable>(TPointTestable pointTestableObject) where TPointTestable : IPointTestable => pointTestableObject.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedBy<TPointTestable>(TPointTestable pointTestableObject) where TPointTestable : IPointTestable => pointTestableObject.Contains(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IShape => shape.ClosestPointOnSurfaceTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IShape => shape.SurfaceDistanceFrom(this);
}

partial struct Plane {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IShape => shape.ClosestPointOnSurfaceTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointToSurfaceOf<TShape>(TShape shape) where TShape : IShape => shape.ClosestPointToSurfaceOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IShape => shape.SurfaceDistanceFrom(this);
}