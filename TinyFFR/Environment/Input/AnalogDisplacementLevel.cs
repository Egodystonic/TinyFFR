using System;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Stratification of displacement levels for a <see cref="GameControllerTriggerPosition"/> or <see cref="GameControllerStickPosition"/>.
/// </summary>
public enum AnalogDisplacementLevel {
	/// <summary>
	/// Zero or near-zero displacement. 
	/// </summary>
	None = 0,
	/// <summary>
	/// Slight displacement (roughly 15% to 40%).
	/// </summary>
	Slight = 4_915, // 15%
	/// <summary>
	/// Moderate displacement (roughly 40% to 75%).
	/// </summary>
	Moderate = 13_107, // 40%
	/// <summary>
	/// Full displacement (over 75%).
	/// </summary>
	Full = 24_576 // 75%
}

static class AnalogDisplacementLevelExtensions {
	public static AnalogDisplacementLevel FromRawDisplacementMagnitude(short rawDisplacementMagnitude) {
		return rawDisplacementMagnitude switch {
			>= (int) AnalogDisplacementLevel.Full => AnalogDisplacementLevel.Full,
			>= (int) AnalogDisplacementLevel.Moderate => AnalogDisplacementLevel.Moderate,
			>= (int) AnalogDisplacementLevel.Slight => AnalogDisplacementLevel.Slight,
			_ => AnalogDisplacementLevel.None
		};
	}
}