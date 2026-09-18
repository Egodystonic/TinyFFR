// Created on 2024-07-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

/// <summary>
/// A simple pair of two values, which may be of different types.
/// </summary>
/// <remarks>
/// This is functionally equivalent to a <c>(TFirst, TSecond)</c> value tuple (and converts implicitly to/from one), but is a named type and readonly.
/// </remarks>
/// <param name="First">The first value.</param>
/// <param name="Second">The second value.</param>
public readonly record struct Pair<TFirst, TSecond>(TFirst First, TSecond Second) {
	/// <summary>
	/// This pair with <see cref="First"/> and <see cref="Second"/> swapped.
	/// </summary>
	public Pair<TSecond, TFirst> Swapped => new(Second, First);

	/// <summary>
	/// Converts a value tuple to an equivalent <see cref="Pair{TFirst,TSecond}"/>.
	/// </summary>
	/// <param name="tuple">The tuple to convert.</param>
	public static implicit operator Pair<TFirst, TSecond>((TFirst First, TSecond Second) tuple) => new(tuple.First, tuple.Second);
	/// <summary>
	/// Converts this pair to an equivalent value tuple.
	/// </summary>
	/// <param name="pair">The pair to convert.</param>
	public static implicit operator (TFirst First, TSecond Second)(Pair<TFirst, TSecond> pair) => (pair.First, pair.Second);
}