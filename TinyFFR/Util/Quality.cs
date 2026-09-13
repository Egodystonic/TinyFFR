using System;

namespace Egodystonic.TinyFFR;

/// <summary>
/// A general-purpose quality level, used across various parts of the API to trade off visual/audio fidelity against performance or resource usage (e.g. memory, load time).
/// </summary>
/// <remarks>
/// The exact effect of each level depends on where it is used; consult the documentation of the specific API accepting a <see cref="Quality"/> value for details.
/// </remarks>
public enum Quality {
	/// <summary>
	/// The lowest quality level.
	/// </summary>
	VeryLow = -2,
	/// <summary>
	/// A quality level lower than <see cref="Standard"/> but higher than <see cref="VeryLow"/>.
	/// </summary>
	Low = -1,
	/// <summary>
	/// The default/typical quality level.
	/// </summary>
	Standard = 0,
	/// <summary>
	/// A quality level higher than <see cref="Standard"/> but lower than <see cref="VeryHigh"/>.
	/// </summary>
	High = 1,
	/// <summary>
	/// The highest quality level.
	/// </summary>
	VeryHigh = 2
}