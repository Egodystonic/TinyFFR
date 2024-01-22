// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Windowing;

public sealed record WindowBuilderCreationConfig {
	public int MaxWindowTitleLength { get; init; } = 200;
}