using System;

namespace Egodystonic.TinyFFR.Environment.Input;

public enum AnalogDisplacementLevel {
	None = 0,
	Slight = 4_915, // 15%
	Moderate = 13_107, // 40%
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