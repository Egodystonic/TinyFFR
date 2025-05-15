// Created on 2025-05-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Rendering;

public enum Quality {
	VeryLow = -2,
	Low = -1,
	Standard = 0,
	High = 1,
	VeryHigh = 2
}

public readonly record struct RenderQualityConfig {
	public Quality ShadowQuality { get; init; }
}