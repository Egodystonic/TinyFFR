// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public interface IPointTestable {
	Location ClosestPointTo(Location location);
	float DistanceFrom(Location location);
	bool Contains(Location location);
	bool Contains(Location location, float tolerance);
}
public interface ILineTestable {
	Location ClosestPointTo(Ray ray);
	Location ClosestPointTo(Line line);
	float DistanceFrom(Ray ray);
	float DistanceFrom(Line line);
	Location? GetIntersectionPoint(Ray ray);
	Location? GetIntersectionPoint(Line line);
	Location? GetIntersectionPoint(Ray ray, float tolerance);
	Location? GetIntersectionPoint(Line line, float tolerance);
}
// TODO mention in XMLDoc that this represents specifically a shape whose origin is always Location.Origin (e.g. all parameters are shape-local).
public interface IShape : IPointTestable, ILineTestable { }
public interface IShape<TSelf> : IShape, IMathPrimitive<TSelf, float>, IInterpolatable<TSelf>, IBoundedRandomizable<TSelf> where TSelf : IShape<TSelf> {
	TSelf ScaledBy(float scalar);
}