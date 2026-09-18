// Created on 2024-01-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// Specifies whether and how a <see cref="Window"/> occupies its entire <see cref="Display"/> (see <see cref="Window.FullscreenStyle"/>).
/// </summary>
public enum WindowFullscreenStyle {
	/// <summary>
	/// The window is a normal desktop window with a title bar and borders, sitting alongside other windows.
	/// </summary>
	NotFullscreen,
	/// <summary>
	/// The window takes over the entire display, asking the operating system to switch the display to the window's own resolution if it does not already match.
	/// </summary>
	/// <remarks>
	/// Because this changes the display's mode, switching away from the application (e.g. alt-tabbing) may be slower and can cause other windows on the desktop to be rearranged.
	/// Prefer <see cref="FullscreenBorderless"/> unless you specifically need the display to run at a different resolution than the user's desktop.
	/// </remarks>
	Fullscreen,
	/// <summary>
	/// The window covers the entire display without borders or a title bar, but leaves the display's resolution untouched.
	/// </summary>
	/// <remarks>
	/// This is usually the better choice for fullscreen presentation: switching to and from other applications is near-instant, at the cost of not being able to render at a resolution other than the user's current desktop resolution.
	/// </remarks>
	FullscreenBorderless
}