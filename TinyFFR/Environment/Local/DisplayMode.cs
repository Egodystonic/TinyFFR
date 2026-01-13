// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly record struct DisplayMode(XYPair<int> Resolution, int RefreshRateHz) {
	public XYPair<int> AspectRatio => Resolution / (int) BigInteger.GreatestCommonDivisor(Resolution.X, Resolution.Y);
	
	public override string ToString() {
		var ar = AspectRatio;
		return $"{Resolution.X:#} x {Resolution.Y:#} ({ar.X}:{ar.Y}) @ {RefreshRateHz}Hz";
	}
}