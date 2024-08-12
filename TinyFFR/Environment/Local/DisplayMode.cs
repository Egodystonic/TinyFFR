// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly record struct DisplayMode(XYPair<int> Resolution, int RefreshRateHz) {
	public override string ToString() {
		return $"{Resolution.X:#} x {Resolution.Y:#} @ {RefreshRateHz}Hz";
	}
}