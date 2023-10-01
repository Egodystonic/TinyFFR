// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

public interface ILinearAlgebraConstruct : ISpanFormattable {
	
}

public interface ILinearAlgebraConstruct<TSelf> : ILinearAlgebraConstruct, 
	ISpanParsable<TSelf>, 
	ISpanConvertible<TSelf, float>,
	IToleranceEquatable<TSelf>, 
	IEqualityOperators<TSelf, TSelf, bool>
	where TSelf : ILinearAlgebraConstruct<TSelf> {
	
}