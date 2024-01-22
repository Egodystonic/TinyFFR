// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Windowing;

public readonly record struct WindowCreationConfig {
	public XYPair? Position { get; init; } = null;
	public XYPair? Size { get; init; } = null;
	public string Title { get; init; } = "Tiny FFR Application";

	public WindowCreationConfig() { }
}