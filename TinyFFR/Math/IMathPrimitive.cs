// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

/// <summary>
/// Base interface that represents any math or geometry primitive type in TinyFFR.
/// </summary>
/// <seealso cref="IMathPrimitive{TSelf}"/>
public interface IMathPrimitive : ISpanFormattable;

/// <summary>
/// Base interface that represents any math or geometry primitive type in TinyFFR. Extends <see cref="IMathPrimitive"/>.
/// </summary>
/// <remarks>
/// As well as marking all math primitives types in TinyFFR, this interface enforces all inheritors to implement a core set of functionality
/// (e.g. <see cref="ISpanParsable{TSelf}"/>, <see cref="ISpanFormattable"/>, <see cref="IFixedLengthByteSpanSerializable{TSelf}"/>,
/// <see cref="IToleranceEquatable{T}"/>, <see cref="IEqualityOperators{TSelf,TOther,TResult}"/>, <see cref="IRandomizable{TSelf}"/>, etc).
/// </remarks>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IMathPrimitive<TSelf> : IMathPrimitive, 
	ISpanParsable<TSelf>, 
	IFixedLengthByteSpanSerializable<TSelf>,
	IToleranceEquatable<TSelf>, 
	IEqualityOperators<TSelf, TSelf, bool>,
	IRandomizable<TSelf>
	where TSelf : IMathPrimitive<TSelf> {
}