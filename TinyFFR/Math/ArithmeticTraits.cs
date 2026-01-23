// Created on 2024-05-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024
#pragma warning disable CA1716 // "Don't use 'end' because VB.NET uses it" -- I kinda went back and forward on this one but ultimately I really like the param name 'end' and I don't think VB.NET is a huge target for this lib

namespace Egodystonic.TinyFFR;

public interface INormalizable<out TSelf>
	where TSelf : INormalizable<TSelf> {
	TSelf Normalized { get; }
}

public interface IAbsolutizable<out TSelf>
	where TSelf : IAbsolutizable<TSelf> {
	TSelf Absolute { get; }
}

public interface IInvertible<TSelf> :
	IUnaryNegationOperators<TSelf, TSelf>
	where TSelf : IInvertible<TSelf> {
	TSelf Inverted { get; }
}

public interface IMultiplicativeInvertible<TSelf>
	where TSelf : struct, IMultiplicativeInvertible<TSelf> {
	TSelf? Reciprocal { get; }
}

public interface IAdditive<TSelf, TOther, TResult> :
	IAdditionOperators<TSelf, TOther, TResult>,
	ISubtractionOperators<TSelf, TOther, TResult>
	where TSelf : IAdditive<TSelf, TOther, TResult> {
	static abstract TSelf operator +(TOther left, TSelf right);
	TResult Plus(TOther other);
	TResult Minus(TOther other);
}

public interface IMultiplicative<TSelf, TOther, TResult> :
	IMultiplyOperators<TSelf, TOther, TResult>,
	IDivisionOperators<TSelf, TOther, TResult>
	where TSelf : IMultiplicative<TSelf, TOther, TResult> {
	static abstract TSelf operator *(TOther left, TSelf right);
	TResult MultipliedBy(TOther other);
	TResult DividedBy(TOther other);
}

public interface IInterpolatable<TSelf> :
	IBoundedRandomizable<TSelf>
	where TSelf : IInterpolatable<TSelf> {
	static abstract TSelf Interpolate(TSelf start, TSelf end, float distance);
	TSelf Clamp(TSelf min, TSelf max);
}

public interface IPrecomputationInterpolatable<TSelf, TPrecomputation> : 
	IInterpolatable<TSelf> 
	where TSelf : IInterpolatable<TSelf>, IPrecomputationInterpolatable<TSelf, TPrecomputation> {
	static abstract TPrecomputation CreateInterpolationPrecomputation(TSelf start, TSelf end);
	static abstract TSelf InterpolateUsingPrecomputation(TSelf start, TSelf end, TPrecomputation precomputation, float distance);
}

public interface IOrdinal<TSelf> :
	IInterpolatable<TSelf>,
	IComparable<TSelf>,
	IComparisonOperators<TSelf, TSelf, bool>
	where TSelf : IOrdinal<TSelf> {
	static abstract float GetInterpolationDistance(TSelf start, TSelf end, TSelf input);
}

public interface IAlgebraicGroup<TSelf> :
	IInvertible<TSelf>,
	IAdditive<TSelf, TSelf, TSelf>,
	IAdditiveIdentity<TSelf, TSelf>
	where TSelf : IAlgebraicGroup<TSelf>;

public interface IAlgebraicRing<TSelf> :
	IAlgebraicGroup<TSelf>,
	IMultiplicative<TSelf, TSelf, TSelf>,
	IMultiplicativeIdentity<TSelf, TSelf>,
	IMultiplicativeInvertible<TSelf>
	where TSelf : struct, IAlgebraicRing<TSelf> {
}

public interface IInnerProductSpace<in TSelf>
	where TSelf : IInnerProductSpace<TSelf>, allows ref struct {
	float Dot(TSelf other);
}
public interface IVectorProductSpace<TSelf>
	where TSelf : IVectorProductSpace<TSelf> {
	TSelf Cross(TSelf other);
}

public interface ITransitionRepresentable<in TSelf, out T> where TSelf : ITransitionRepresentable<TSelf, T>, allows ref struct {
	static abstract T operator >>(TSelf start, TSelf end);
	static abstract T operator <<(TSelf end, TSelf start);
}