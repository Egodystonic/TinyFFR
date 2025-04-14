// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

public interface IWindowBuilder {
	Window CreateWindow(Display display, WindowFullscreenStyle? fullscreenStyle = null, XYPair<int>? size = null, XYPair<int>? position = null, ReadOnlySpan<char> title = default) {
		return CreateWindow(new() {
			Display = display,
			FullscreenStyle = fullscreenStyle ?? WindowFullscreenStyle.NotFullscreen,
			Size = size ?? (fullscreenStyle == WindowFullscreenStyle.Fullscreen ? display.CurrentResolution : display.CurrentResolution.ScaledByReal(0.66f)),
			Position = position ?? (display.CurrentResolution.ScaledByReal(0.33f / 2f)),
			Title = title
		});
	}
	Window CreateWindow(in WindowConfig config);
}