// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Windowing;

public readonly record struct WindowCreationConfig {
	public XYPair? ScreenLocation { get; init; } = null;
	public XYPair? ScreenDimensions { get; init; } = null;

	public WindowCreationConfig() { }
}