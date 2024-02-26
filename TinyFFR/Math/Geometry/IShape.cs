// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public interface IShape {
	// TODO
	// bool Contains(Location localLocation);
	// bool IsIntersectedBy(Ray localRay);
}
public interface IShape<TSelf> : IShape, IMathPrimitive<TSelf, float>, IInterpolatable<TSelf>, IBoundedRandomizable<TSelf> where TSelf : IShape<TSelf> {
	
}