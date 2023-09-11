// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

public interface ISpanConvertible<TSelf, TSpanElement> where TSelf : ISpanConvertible<TSelf, TSpanElement> {
	static abstract ReadOnlySpan<TSpanElement> ConvertToSpan(in TSelf src);
	static abstract TSelf ConvertFromSpan(ReadOnlySpan<TSpanElement> src);
}