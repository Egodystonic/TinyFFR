// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly record struct InitOptions {
	public bool TrackResourceLeaks { get; init; } = false;

	public InitOptions() { }
}