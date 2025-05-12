// Created on 2025-05-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.World;

public readonly record struct SunDiscConfig {
	public float Scaling { get; init; } = 1f;
	public float FringingScaling { get; init; } = 1f;
	public float FringingOuterRadiusScaling { get; init; } = 1f;

	public SunDiscConfig() { }
}