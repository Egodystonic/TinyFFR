// Created on 2024-05-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024
#pragma warning disable CA1716 // "Don't use 'end' because VB.NET uses it" -- I kinda went back and forward on this one but ultimately I really like the param name 'end' and I don't think VB.NET is a huge target for this lib

namespace Egodystonic.TinyFFR;

public interface INormalizable<out TSelf>
	where TSelf : INormalizable<TSelf> {
	TSelf Normalized { get; }
}

/// <summary>
/// Trait interface used to mark a mathematical or geometric primitive as being absolutizable.
/// An absolutized value is one who sign identity is erased (e.g. all values are made positive).
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IAbsolutizable<out TSelf>
	where TSelf : IAbsolutizable<TSelf> {
	/// <summary>
	/// Returns the absolute (non-negative) value of this object.
	/// </summary>
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

public interface IBlendable<TSelf> {
	static abstract TSelf Blend(TSelf start, TSelf end, float distance);
}

public interface IInterpolatable<TSelf> :
	IBlendable<TSelf>,
	IBoundedRandomizable<TSelf>
	where TSelf : IInterpolatable<TSelf> {
	static TSelf IBlendable<TSelf>.Blend(TSelf start, TSelf end, float distance) => TSelf.Interpolate(start, end, distance);
	/// <summary>
	/// Interpolates a value from <paramref name="start"/> to <paramref name="end"/> according to the normalized <paramref name="distance"/>.
	/// </summary>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate (i.e. <c>0.5f</c> returns the value exactly halfway between start &amp; end).
	/// Values outside the range 0-1 are permitted and will extend the interpolation calculation beyond the start or end value respectively.</param>
	static abstract TSelf Interpolate(TSelf start, TSelf end, float distance);
	/// <summary>
	/// Clamps this value between <paramref name="min"/> and <paramref name="max"/>.
	/// </summary>
	/// <remarks>
	/// Note that unlike most .NET clamp functions, TinyFFR allows you to swap <paramref name="min"/> and <paramref name="max"/> freely
	/// and still get the same answer (i.e. <c>Clamp(4, 7)</c> is the same as <c>Clamp(7, 4)</c>).
	/// <para>
	/// This is a deliberate design choice given most clampable types in TinyFFR are not necessarily ordinal (i.e.
	/// they do not have an obvious ordering; for example vectors, directions, shapes, rotations, etc).
	/// </para>
	/// </remarks>
	/// <param name="min">The lower bound value (inclusive).</param>
	/// <param name="max">The upper bound value (inclusive).</param>
	/// <returns>A value <c>v</c> such that <c>min &lt;= v &lt;= max</c>.</returns>
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
	/// <summary>
	/// Calculates the normalized distance of <paramref name="input"/> from <paramref name="start"/> to <paramref name="end"/>.
	/// This function is essentially the "inverse" of <see cref="IInterpolatable{TSelf}.Interpolate"/>. 
	/// </summary>
	/// <param name="start">The starting value (i.e. when <paramref name="input"/> is equal to this  <c>0f</c> will be returned).</param>
	/// <param name="end">The ending value (i.e. when <paramref name="input"/> is equal to this <c>1f</c> will be returned).</param>
	/// <param name="input">The input value. Can be anything (e.g. could be between <paramref name="start"/> and <paramref name="end"/> or outside that range entirely).</param>
	/// <returns>A normalized mapping of <paramref name="input"/> from [<paramref name="start"/>-<paramref name="end"/>] to [0-1].</returns>
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