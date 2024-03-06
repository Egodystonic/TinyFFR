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
	float DistanceFrom(Plane plane);
	PlaneObjectRelationship RelationshipTo(Plane plane);
}
// TODO mention in XMLDoc that this represents specifically a shape whose origin is always Location.Origin (e.g. all parameters are shape-local).
public interface IShape : IPointTestable, ILineTestable, IPlaneTestable {
	Location ClosestSurfacePointTo(Location location);
	Location ClosestSurfacePointTo<TLine>(TLine line) where TLine : ILine;
	float SurfaceDistanceFrom(Location location);
	float SurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine;

	// These two lines are essentially a rename of IntersectionPointWith to SurfaceIntersectionPointWith for shapes
	// as anything BUT a surface intersection doesn't really make much sense once you think about it and I wanted to
	// be explicit to keep with the naming convention of all the other SurfaceXyz methods.
	Location? ILineTestable.IntersectionPointWith<TLine>(TLine line) => SurfaceIntersectionPointWith(line);
	Location? SurfaceIntersectionPointWith<TLine>(TLine line) where TLine : ILine;

	// TODO RefIterator for faces and vertices
	// TODO would it be better to have type that holds a ref field of type T and can then easily iterate against that T assuming it implements an interface? Something like RefEnumerable? Yes, the interface approach means we can only have one implementation but that's fiiiiine
}
public interface IShape<TSelf> : IShape, IMathPrimitive<TSelf, float>, IInterpolatable<TSelf>, IBoundedRandomizable<TSelf> where TSelf : IShape<TSelf> {
	TSelf ScaledBy(float scalar);
}

public static class ShapeExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointTo<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine<TLine> where TShape : IShape<TShape> => shape.ClosestPointOn(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOn<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine<TLine> where TShape : IShape<TShape> => shape.ClosestPointTo(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFrom<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine<TLine> where TShape : IShape<TShape> => shape.DistanceFrom(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location? IntersectionPointWith<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine<TLine> where TShape : IShape<TShape> => shape.IntersectionPointWith(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointTo<TShape>(this Plane @this, TShape shape) where TShape : IShape<TShape> => shape.ClosestPointOn(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOn<TShape>(this Plane @this, TShape shape) where TShape : IShape<TShape> => shape.ClosestPointTo(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFrom<TShape>(this Plane @this, TShape shape) where TShape : IShape<TShape> => shape.DistanceFrom(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PlaneObjectRelationship RelationshipTo<TShape>(this Plane @this, TShape shape) where TShape : IShape<TShape> => shape.RelationshipTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOnSurfaceOf<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine<TLine> where TShape : IShape<TShape> => shape.ClosestSurfacePointTo(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFromSurfaceOf<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine<TLine> where TShape : IShape<TShape> => shape.SurfaceDistanceFrom(@this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location? IntersectionPointWithSurfaceOf<TLine, TShape>(this TLine @this, TShape shape) where TLine : ILine<TLine> where TShape : IShape<TShape> => shape.SurfaceIntersectionPointWith(@this);
}