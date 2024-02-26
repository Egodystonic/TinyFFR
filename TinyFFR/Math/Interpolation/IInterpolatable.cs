// Created on 2024-02-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

#pragma warning disable CA1716 // "Don't use 'end' because VB.NET uses it" -- I kinda went back and forward on this one but ultimately I really like the param name 'end' and I don't think VB.NET is a huge target for this lib
public interface IInterpolatable<TSelf> where TSelf : IInterpolatable<TSelf> {
	static abstract TSelf Interpolate(TSelf start, TSelf end, float distance);
}
public interface IPrecomputationInterpolatable<TSelf, TPrecomputation> : IInterpolatable<TSelf> where TSelf : IInterpolatable<TSelf>, IPrecomputationInterpolatable<TSelf, TPrecomputation> {
	static abstract TPrecomputation CreateInterpolationPrecomputation(TSelf start, TSelf end);
	static abstract TSelf InterpolateUsingPrecomputation(TSelf start, TSelf end, TPrecomputation precomputation, float distance);
}