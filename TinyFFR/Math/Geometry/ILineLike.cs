// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public interface ILineLike : IPointTestable, ILineTestable {
	Location StartPoint { get; }
	Direction Direction { get; }
	float? Length { get; }
	float? LengthSquared { get; }
	Vect? StartToEndVect { get; }
	Location? EndPoint { get; }

	Location UnboundedProjectionOf(Location location);
}
public interface ILineLike<TSelf> : ILineLike, IMathPrimitive<TSelf, float>, IInterpolatable<TSelf>, IBoundedRandomizable<TSelf> where TSelf : ILineLike<TSelf> {

}