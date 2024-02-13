// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Desktop;

public readonly record struct DisplayMode(XYPair<int> Resolution, int RefreshRateHz) {
	public override string ToString() {
		return $"{RefreshRateHz}Hz @ {Resolution.X:#} x {Resolution.Y:#}";
	}
}