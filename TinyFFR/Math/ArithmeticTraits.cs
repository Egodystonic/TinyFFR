// Created on 2024-05-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024
#pragma warning disable CA1716 // "Don't use 'end' because VB.NET uses it" -- I kinda went back and forward on this one but ultimately I really like the param name 'end' and I don't think VB.NET is a huge target for this lib

namespace Egodystonic.TinyFFR;

/// <summary>
/// Trait interface used to mark a type as being able to provide a canonical, "normalized" version of any of its values.
/// </summary>
/// <remarks>
/// What "normalized" means is defined by the implementing type; for example an <see cref="Angle"/> normalizes into the
/// range 0° to 360°, whereas a <see cref="Rotation"/> normalizes its internal representation without changing the
/// rotation it represents.
/// <para>
/// Generally speaking, a normalized version of a value is the one that uniquely represents that value within a smaller
/// subset of unique values; where that subset is the smallest possible set of values that can be used to affect a typical operation.
/// For example, "rotate a cube by 90°" and "rotate a cube by 450°" produce the same final output, so the second version can be <i>normalized</i> to the first;
/// and the unique subset of normalized values could be considered all rotations within the range <c>[0°, 360°)</c>.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface INormalizable<out TSelf>
	where TSelf : INormalizable<TSelf> {
	/// <summary>
	/// Returns the normalized (canonical) version of this value.
	/// </summary>
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

/// <summary>
/// Trait interface used to mark a type as being able to produce its own inverse.
/// </summary>
/// <remarks>
/// The unary negation operator (<c>-value</c>) is equivalent to reading <see cref="Inverted"/>.
/// </remarks>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IInvertible<TSelf> :
	IUnaryNegationOperators<TSelf, TSelf>
	where TSelf : IInvertible<TSelf> {
	/// <summary>
	/// Returns the inverse of this value.
	/// </summary>
	TSelf Inverted { get; }
}

/// <summary>
/// Trait interface used to mark a type as being able to produce its own multiplicative inverse (reciprocal).
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IMultiplicativeInvertible<TSelf>
	where TSelf : struct, IMultiplicativeInvertible<TSelf> {
	/// <summary>
	/// Returns the reciprocal of this value (i.e. the value that, when multiplied by this value, yields the
	/// multiplicative identity), or <see langword="null"/> if no such value exists (for example, because this value is zero).
	/// </summary>
	TSelf? Reciprocal { get; }
}

/// <summary>
/// Trait interface composing the addition and subtraction operators (<c>+</c>/<c>-</c>) for a type, along with
/// method-style equivalents.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
/// <typeparam name="TOther">The type of the other operand being added or subtracted.</typeparam>
/// <typeparam name="TResult">The type of the result of the addition or subtraction.</typeparam>
public interface IAdditive<TSelf, TOther, TResult> :
	IAdditionOperators<TSelf, TOther, TResult>,
	ISubtractionOperators<TSelf, TOther, TResult>
	where TSelf : IAdditive<TSelf, TOther, TResult> {
	/// <summary>
	/// Equivalent to <c>right + left</c>; provided so that addition can be written with the operands in either order.
	/// </summary>
	/// <param name="left">The left-hand operand.</param>
	/// <param name="right">The right-hand operand.</param>
	static abstract TSelf operator +(TOther left, TSelf right);
	/// <summary>
	/// Equivalent to the <c>+</c> operator; returns the result of adding <paramref name="other"/> to this value.
	/// </summary>
	/// <param name="other">The value to add.</param>
	TResult Plus(TOther other);
	/// <summary>
	/// Equivalent to the <c>-</c> operator; returns the result of subtracting <paramref name="other"/> from this value.
	/// </summary>
	/// <param name="other">The value to subtract.</param>
	TResult Minus(TOther other);
}

/// <summary>
/// Trait interface composing the multiplication and division operators (<c>*</c>/<c>/</c>) for a type, along with
/// method-style equivalents.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
/// <typeparam name="TOther">The type of the other operand being multiplied or divided.</typeparam>
/// <typeparam name="TResult">The type of the result of the multiplication or division.</typeparam>
public interface IMultiplicative<TSelf, TOther, TResult> :
	IMultiplyOperators<TSelf, TOther, TResult>,
	IDivisionOperators<TSelf, TOther, TResult>
	where TSelf : IMultiplicative<TSelf, TOther, TResult> {
	/// <summary>
	/// Equivalent to <c>right * left</c>; provided so that multiplication can be written with the operands in either order.
	/// </summary>
	/// <param name="left">The left-hand operand.</param>
	/// <param name="right">The right-hand operand.</param>
	static abstract TSelf operator *(TOther left, TSelf right);
	/// <summary>
	/// Equivalent to the <c>*</c> operator; returns the result of multiplying this value by <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The value to multiply by.</param>
	TResult MultipliedBy(TOther other);
	/// <summary>
	/// Equivalent to the <c>/</c> operator; returns the result of dividing this value by <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The value to divide by.</param>
	TResult DividedBy(TOther other);
}

/// <summary>
/// Trait interface used to mark a type as being able to blend between two values of itself according to a normalized
/// distance factor.
/// </summary>
/// <remarks>
/// In practice, most blendable types in TinyFFR implement <see cref="IInterpolatable{TSelf}"/> instead, which provides
/// this same functionality via <see cref="IInterpolatable{TSelf}.Interpolate"/>.
/// </remarks>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IBlendable<TSelf> {
	/// <summary>
	/// Blends between <paramref name="start"/> and <paramref name="end"/> according to the normalized <paramref name="distance"/>.
	/// </summary>
	/// <param name="start">The starting value (i.e. the value returned when <paramref name="distance"/> is <c>0f</c>).</param>
	/// <param name="end">The ending value (i.e. the value returned when <paramref name="distance"/> is <c>1f</c>).</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate.</param>
	static abstract TSelf Blend(TSelf start, TSelf end, float distance);
}

/// <summary>
/// Trait interface used to mark a type as supporting interpolation between two of its values, and clamping of a value
/// between two bounds.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
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

/// <summary>
/// Extension of <see cref="IInterpolatable{TSelf}"/> for types where repeatedly interpolating between the same
/// <c>start</c>/<c>end</c> pair (but with different distance values each time) can be made faster by precomputing some
/// shared state once up front.
/// </summary>
/// <remarks>
/// This is useful when you need many interpolated values between the same two endpoints, for example when sampling an
/// animation curve every frame: calculate the precomputation once with <see cref="CreateInterpolationPrecomputation"/>,
/// then pass it to <see cref="InterpolateUsingPrecomputation"/> for every sample instead of paying the full cost of
/// <see cref="IInterpolatable{TSelf}.Interpolate"/> each time.
/// </remarks>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
/// <typeparam name="TPrecomputation">The type of the precomputed state shared across repeated interpolations between the same two values.</typeparam>
public interface IPrecomputationInterpolatable<TSelf, TPrecomputation> :
	IInterpolatable<TSelf>
	where TSelf : IInterpolatable<TSelf>, IPrecomputationInterpolatable<TSelf, TPrecomputation> {
	/// <summary>
	/// Precomputes and returns the shared state required to efficiently interpolate between <paramref name="start"/> and
	/// <paramref name="end"/> multiple times via <see cref="InterpolateUsingPrecomputation"/>.
	/// </summary>
	/// <param name="start">The starting value that will be passed to <see cref="InterpolateUsingPrecomputation"/>.</param>
	/// <param name="end">The ending value that will be passed to <see cref="InterpolateUsingPrecomputation"/>.</param>
	static abstract TPrecomputation CreateInterpolationPrecomputation(TSelf start, TSelf end);
	/// <summary>
	/// Interpolates between <paramref name="start"/> and <paramref name="end"/> using a <paramref name="precomputation"/>
	/// obtained from <see cref="CreateInterpolationPrecomputation"/>; equivalent to, but generally faster than, calling
	/// <see cref="IInterpolatable{TSelf}.Interpolate"/> directly.
	/// </summary>
	/// <param name="start">The same starting value originally passed to <see cref="CreateInterpolationPrecomputation"/>.</param>
	/// <param name="end">The same ending value originally passed to <see cref="CreateInterpolationPrecomputation"/>.</param>
	/// <param name="precomputation">The precomputation obtained from <see cref="CreateInterpolationPrecomputation"/> for <paramref name="start"/> and <paramref name="end"/>.</param>
	/// <param name="distance">The normalized distance between <paramref name="start"/> and <paramref name="end"/> to calculate.</param>
	static abstract TSelf InterpolateUsingPrecomputation(TSelf start, TSelf end, TPrecomputation precomputation, float distance);
}

/// <summary>
/// Trait interface used to mark a type as having a well-defined ordering between any two of its values, in addition to
/// the interpolation functionality provided by <see cref="IInterpolatable{TSelf}"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
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

/// <summary>
/// Trait interface composing the operations required for values of a type to be added, subtracted, and negated, and for
/// the type to expose an additive identity/zero value (<see cref="IAdditiveIdentity{TSelf,TSelf}.AdditiveIdentity"/>).
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IAlgebraicGroup<TSelf> :
	IInvertible<TSelf>,
	IAdditive<TSelf, TSelf, TSelf>,
	IAdditiveIdentity<TSelf, TSelf>
	where TSelf : IAlgebraicGroup<TSelf>;

/// <summary>
/// Extension of <see cref="IAlgebraicGroup{TSelf}"/> that additionally composes the operations required for values of a
/// type to be multiplied, divided, and reciprocated, and for the type to expose a multiplicative identity/one value
/// (<see cref="IMultiplicativeIdentity{TSelf,TSelf}.MultiplicativeIdentity"/>).
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IAlgebraicRing<TSelf> :
	IAlgebraicGroup<TSelf>,
	IMultiplicative<TSelf, TSelf, TSelf>,
	IMultiplicativeIdentity<TSelf, TSelf>,
	IMultiplicativeInvertible<TSelf>
	where TSelf : struct, IAlgebraicRing<TSelf> {
}

/// <summary>
/// Trait interface used to mark a type as supporting the dot product operation against another value of the same type.
/// </summary>
/// <remarks>
/// The dot product of two vectors is a single number that measures how much they point in the same direction: it is
/// largest and positive when the vectors point the same way, zero when they are perpendicular, and largest-magnitude
/// negative when they point in opposite directions.
/// </remarks>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IInnerProductSpace<in TSelf>
	where TSelf : IInnerProductSpace<TSelf>, allows ref struct {
	/// <summary>
	/// Calculates the dot product of this value and <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The other value to calculate the dot product with.</param>
	float Dot(TSelf other);
}
/// <summary>
/// Trait interface used to mark a type as supporting the cross product operation against another value of the same type.
/// </summary>
/// <remarks>
/// The cross product of two vectors is a new vector that is perpendicular to both inputs, useful for finding "the
/// direction at right angles to these two directions" (for example, calculating a surface normal from two edges of a
/// triangle). Its magnitude also reflects how far from parallel the two input vectors are, shrinking to zero as they
/// become parallel.
/// </remarks>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IVectorProductSpace<TSelf>
	where TSelf : IVectorProductSpace<TSelf> {
	/// <summary>
	/// Calculates the cross product of this value and <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The other value to calculate the cross product with.</param>
	TSelf Cross(TSelf other);
}

/// <summary>
/// Trait interface used to mark a type as being able to represent the transition from one of its values to another as a
/// separate result type.
/// </summary>
/// <remarks>
/// For example, the transition between two <see cref="Location"/>s is represented as a <see cref="Vect"/> (the
/// straight-line displacement between them), and the transition between two <see cref="Direction"/>s is represented as a
/// <see cref="Rotation"/> (the rotation that would turn one into the other).
/// </remarks>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
/// <typeparam name="T">The type used to represent the transition between two values of <typeparamref name="TSelf"/>.</typeparam>
public interface ITransitionRepresentable<in TSelf, out T> where TSelf : ITransitionRepresentable<TSelf, T>, allows ref struct {
	/// <summary>
	/// Returns the transition from <paramref name="start"/> to <paramref name="end"/>.
	/// </summary>
	/// <param name="start">The starting value.</param>
	/// <param name="end">The ending value.</param>
	static abstract T operator >>(TSelf start, TSelf end);
	/// <summary>
	/// Returns the transition from <paramref name="start"/> to <paramref name="end"/>; equivalent to <c>start &gt;&gt; end</c>,
	/// provided so that the transition can also be written as <c>end &lt;&lt; start</c>.
	/// </summary>
	/// <param name="end">The ending value.</param>
	/// <param name="start">The starting value.</param>
	static abstract T operator <<(TSelf end, TSelf start);
}