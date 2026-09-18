// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// A builder interface used for creating desktop <see cref="Window"/>s.
/// </summary>
public interface IWindowBuilder {
	/// <summary>
	/// Creates a new <see cref="Window"/> on the given <paramref name="display"/>.
	/// </summary>
	/// <param name="display">The display the new window should be shown on.</param>
	/// <param name="fullscreenStyle">Whether and how the new window should occupy its entire display. Defaults to <see cref="WindowFullscreenStyle.NotFullscreen"/>.</param>
	/// <param name="size">
	/// The size of the new window (<c>X</c> = width, <c>Y</c> = height). Neither component may be negative. Defaults to the display's full resolution when creating
	/// a fullscreen window, or two thirds of it otherwise.
	/// </param>
	/// <param name="position">
	/// Where the new window's top-left corner should sit, measured in pixels from the top-left corner of <paramref name="display"/>, with <c>Y</c> increasing
	/// downward. Defaults to a position that leaves the default-sized window centred on the display.
	/// </param>
	/// <param name="title">Optional title for the new window; i.e. the text the operating system shows in its title bar.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> has a negative component.</exception>
	Window CreateWindow(Display display, WindowFullscreenStyle? fullscreenStyle = null, XYPair<int>? size = null, XYPair<int>? position = null, ReadOnlySpan<char> title = default) {
		return CreateWindow(new() {
			Display = display,
			FullscreenStyle = fullscreenStyle ?? WindowFullscreenStyle.NotFullscreen,
			Size = size ?? (fullscreenStyle == WindowFullscreenStyle.Fullscreen ? display.CurrentResolution : display.CurrentResolution.ScaledByReal(0.66f)),
			Position = position ?? (display.CurrentResolution.ScaledByReal(0.33f / 2f)),
			Title = title
		});
	}
	/// <summary>
	/// Creates a new <see cref="Window"/> according to the given <paramref name="config"/>.
	/// </summary>
	/// <param name="config">Configuration for the new window, including the display it should appear on, its title, and its size and position.</param>
	Window CreateWindow(in WindowCreationConfig config);
}