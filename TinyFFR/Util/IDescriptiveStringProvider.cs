// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

/// <summary>
/// Applied to some types in TinyFFR that are capable of providing a <see cref="ToStringDescriptive"/> method that provides
/// additional information useful for debugging.
/// </summary>
public interface IDescriptiveStringProvider {
	/// <summary>
	/// Similar to <see cref="Object.ToString"/> but provides additional (sometimes flowery) information helpful for
	/// debugging.
	/// </summary>
	string ToStringDescriptive();
}