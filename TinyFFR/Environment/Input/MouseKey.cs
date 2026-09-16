// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Represents a button on a mouse; a mouse-only subset of <see cref="KeyboardOrMouseKey"/>.
/// </summary>
/// <remarks>
/// Each value here has the same underlying value as its <see cref="KeyboardOrMouseKey"/> counterpart, so the two are directly interchangeable
/// (see <see cref="KeyboardOrMouseKeyExtensions.ToKeyboardOrMouseKey"/>). Note that mouse wheel movements are not represented here, only buttons.
/// </remarks>
public enum MouseKey : int {
	/// <summary>
	/// Unknown or unrecognised mouse button.
	/// </summary>
	Unknown = 0,
	/// <summary>
	/// Left mouse button.
	/// </summary>
	MouseLeft = KeyboardOrMouseKey.MouseLeft,
	/// <summary>
	/// Middle mouse button (usually pressing down on the scroll wheel).
	/// </summary>
	MouseMiddle = KeyboardOrMouseKey.MouseMiddle,
	/// <summary>
	/// Right mouse button.
	/// </summary>
	MouseRight = KeyboardOrMouseKey.MouseRight,
	/// <summary>
	/// Fourth mouse button (usually the rearmost thumb button, conventionally 'back').
	/// </summary>
	Mouse4 = KeyboardOrMouseKey.Mouse4,
	/// <summary>
	/// Fifth mouse button (usually the frontmost thumb button, conventionally 'forward').
	/// </summary>
	Mouse5 = KeyboardOrMouseKey.Mouse5,
}