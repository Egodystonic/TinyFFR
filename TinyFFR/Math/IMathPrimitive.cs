﻿// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

public interface IMathPrimitive : ISpanFormattable {

}

public interface IMathPrimitive<TSelf> : IMathPrimitive, 
	ISpanParsable<TSelf>, 
	IByteSpanSerializable<TSelf>,
	IToleranceEquatable<TSelf>, 
	IEqualityOperators<TSelf, TSelf, bool>,
	IRandomizable<TSelf>
	where TSelf : IMathPrimitive<TSelf> {
}