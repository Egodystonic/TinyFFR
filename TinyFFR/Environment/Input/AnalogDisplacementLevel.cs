using System;

namespace Egodystonic.TinyFFR.Environment.Input;

public enum AnalogDisplacementLevel {
	None = 0,
	Slight = 4_915, // 15%
	Moderate = 13_107, // 40%
	Full = 24_576 // 75%
}