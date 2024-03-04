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
	float DistanceFrom<TLine>(TLine line) where TLine : ILine;
	Location? IntersectionPointWith<TLine>(TLine line) where TLine : ILine;
}
// TODO mention in XMLDoc that this represents specifically a shape whose origin is always Location.Origin (e.g. all parameters are shape-local).
public interface IShape : IPointTestable, ILineTestable {
	Location ClosestSurfacePointTo(Location location);
	Location ClosestSurfacePointTo<TLine>(TLine line) where TLine : ILine;
	float SurfaceDistanceFrom(Location location);
	float SurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine;

	// These two lines are essentially a rename of IntersectionPointWith to SurfaceIntersectionPointWith for shapes
	// as anything BUT a surface intersection doesn't really make much sense once you think about it and I wanted to
	// be explicit to keep with the naming convention of all the other SurfaceXyz methods.
	Location? ILineTestable.IntersectionPointWith<TLine>(TLine line) => SurfaceIntersectionPointWith(line);
	Location? SurfaceIntersectionPointWith<TLine>(TLine line) where TLine : ILine;
}
public interface IShape<TSelf> : IShape, IMathPrimitive<TSelf, float>, IInterpolatable<TSelf>, IBoundedRandomizable<TSelf> where TSelf : IShape<TSelf> {
	TSelf ScaledBy(float scalar);
}