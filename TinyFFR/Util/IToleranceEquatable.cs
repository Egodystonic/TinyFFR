// Created on 2023-09-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

/// <summary>
/// A TinyFFR-specific extension of <see cref="IEquatable{T}"/> that adds floating-point aware equality functionality.
/// </summary>
/// <typeparam name="T">The type of the operand being compared to for equality.</typeparam>
public interface IToleranceEquatable<T> : IEquatable<T> where T : allows ref struct {
	/// <summary>
	/// Determines whether this value is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	bool Equals(T? other, float tolerance);
}