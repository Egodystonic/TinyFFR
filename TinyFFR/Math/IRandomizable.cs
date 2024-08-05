// Created on 2024-02-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public interface IRandomizable<out TSelf> where TSelf : IRandomizable<TSelf> {
	static abstract TSelf Random();
}
public interface IBoundedRandomizable<TSelf> : IRandomizable<TSelf> where TSelf : IBoundedRandomizable<TSelf>, IRandomizable<TSelf> {
	static abstract TSelf Random(TSelf minInclusive, TSelf maxExclusive);
}