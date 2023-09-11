// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

public interface ILinearAlgebraComposite : ISpanFormattable {
	
}

public interface ILinearAlgebraComposite<TSelf> : ISpanParsable<TSelf>, ISpanConvertible<TSelf, float> where TSelf : ILinearAlgebraComposite<TSelf> {
	
}