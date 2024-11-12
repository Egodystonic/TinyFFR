// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

public interface IWindowBuilder {
	Window CreateWindow(Display display, WindowFullscreenStyle? fullscreenStyle = null, XYPair<int>? size = null, XYPair<int>? position = null, ReadOnlySpan<char> title = default);
	Window CreateWindow(in WindowConfig config);
}