// Created on 2024-02-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

/// <summary>
/// Interface marking a type as being capable of producing random instances of itself.
/// </summary>
/// <seealso cref="IBoundedRandomizable{TSelf}"/>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IRandomizable<out TSelf> where TSelf : IRandomizable<TSelf> {
	/// <summary>
	/// Produce a random value of this type.
	/// </summary>
	/// <returns>A new <typeparamref name="TSelf"/>. The bounds for this value may be picked from arbitrary defaults or non-existent.</returns>
	static abstract TSelf Random();
}

/// <summary>
/// Interface marking a type as being capable of producing random instances of itself within a min/max boundary. Extends <see cref="IRandomizable{TSelf}"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IBoundedRandomizable<TSelf> : IRandomizable<TSelf> where TSelf : IBoundedRandomizable<TSelf>, IRandomizable<TSelf> {
	/// <summary>
	/// Produce a random value of this type.
	/// </summary>
	/// <param name="minInclusive">The minimum value that can be produced. No values lower/less than this value will be produced, but this value itself <i>is</i> permitted.</param>
	/// <param name="maxExclusive">The ceiling of values that can be produced. No values higher/greater than this value will be produced, and nor will this value itself.</param>
	/// <returns>A new <typeparamref name="TSelf"/> <c>n</c> such that <c>minInclusive &lt;= n &lt; maxExclusive</c>.</returns>
	static abstract TSelf Random(TSelf minInclusive, TSelf maxExclusive);
}