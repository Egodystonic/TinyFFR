using System;

namespace Egodystonic.TinyFFR;

public interface IGeometryPrimitive : IGeometryInteractable { }
public interface IGeometryPrimitive<TSelf> : IGeometryPrimitive, IMathPrimitive<TSelf>, IInterpolatable<TSelf>, IBoundedRandomizable<TSelf> where TSelf : IGeometryPrimitive<TSelf> {

}