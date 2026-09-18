// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Enumeration of the shapes the mouse cursor can be given while it is over a window (see <see cref="Egodystonic.TinyFFR.Environment.Local.Window.CursorStyle"/>).
/// </summary>
/// <remarks>
/// These are the host operating system's own standard cursors, so their exact appearance varies by platform and user theme; the descriptions below
/// indicate what each one conventionally communicates to the user rather than exactly how it will be drawn.
/// </remarks>
public enum MouseCursorStyle {
	/// <summary>
	/// The standard arrow pointer.
	/// </summary>
	Arrow = 0,
	/// <summary>
	/// The text-editing cursor (usually an "I-beam"), conventionally indicating text that can be selected or typed in to.
	/// </summary>
	TextInput = 1,
	/// <summary>
	/// The "busy" cursor (usually an hourglass or spinner), conventionally indicating that the application is working and will not respond until it is finished.
	/// </summary>
	Wait = 2,
	/// <summary>
	/// A crosshair, conventionally indicating precise selection or aiming.
	/// </summary>
	Crosshair = 3,
	/// <summary>
	/// The arrow pointer combined with a "busy" indicator, conventionally indicating that the application is working in the background but is still responsive.
	/// </summary>
	WaitArrow = 4,
	/// <summary>
	/// A double-headed arrow pointing along the top-left/bottom-right diagonal, conventionally indicating a corner that can be dragged to resize.
	/// </summary>
	ResizeTopLeftBottomRight = 5,
	/// <summary>
	/// A double-headed arrow pointing along the top-right/bottom-left diagonal, conventionally indicating a corner that can be dragged to resize.
	/// </summary>
	ResizeTopRightBottomLeft = 6,
	/// <summary>
	/// A double-headed horizontal arrow, conventionally indicating an edge that can be dragged left or right to resize.
	/// </summary>
	ResizeHorizontal = 7,
	/// <summary>
	/// A double-headed vertical arrow, conventionally indicating an edge that can be dragged up or down to resize.
	/// </summary>
	ResizeVertical = 8,
	/// <summary>
	/// A four-way arrow, conventionally indicating something that can be dragged in any direction.
	/// </summary>
	ResizeAll = 9,
	/// <summary>
	/// The "not allowed" cursor (usually a slashed circle), conventionally indicating that the attempted action can not be performed here.
	/// </summary>
	NotAllowed = 10,
	/// <summary>
	/// A pointing hand, conventionally indicating something that can be clicked.
	/// </summary>
	Hand = 11,
	/// <summary>
	/// Hides the cursor entirely while it is over the window.
	/// </summary>
	Invisible = 100_000
}
