// Created on 2024-02-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Local;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Retrieves the latest state of the keyboard and mouse.
/// </summary>
/// <remarks>
/// <para>
/// This interface offers two views of the same input. The <c>New...</c> properties and the <c>...ThisIteration</c> methods describe what <i>changed</i> during the
/// most recent iteration of the <see cref="ApplicationLoop"/>, and are what you want for anything that should happen once per keypress (firing a weapon, activating
/// a menu item). <see cref="CurrentlyPressedKeys"/> and <see cref="KeyIsCurrentlyDown"/> instead describe what is <i>held down right now</i>, which is what you want
/// for anything continuous (walking whilst a key is held).
/// </para>
/// <para>
/// Everything here describes the state as of the most recent iteration and is replaced by the next one; input that arrives between iterations is not visible until
/// the iteration after that. Mouse buttons and the scroll wheel are reported as <see cref="KeyboardOrMouseKey"/> values alongside keyboard keys, so they appear in
/// all the same collections.
/// </para>
/// </remarks>
public interface ILatestKeyboardAndMouseInputRetriever {
	/// <summary>
	/// Every key press and release that occurred in the most recent iteration, in the order they occurred.
	/// </summary>
	/// <remarks>
	/// Where you only care about presses or only about releases, <see cref="NewKeyDownEvents"/> and <see cref="NewKeyUpEvents"/> say the same thing more directly.
	/// Note that the operating system's key-repeat (the stream of repeated presses produced by holding a key down) is filtered out, so holding a key produces
	/// exactly one press event; and that a release of a key whose press was never seen is discarded rather than reported.
	/// </remarks>
	IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKeyEvent> NewKeyEvents { get; }
	/// <summary>
	/// Every key that was pressed down in the most recent iteration, in the order they were pressed.
	/// </summary>
	/// <remarks>
	/// A key appears here only on the iteration in which it was first pressed, not on every iteration it remains held for; see <see cref="CurrentlyPressedKeys"/> for the latter.
	/// </remarks>
	IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> NewKeyDownEvents { get; }
	/// <summary>
	/// Every key that was released in the most recent iteration, in the order they were released.
	/// </summary>
	IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> NewKeyUpEvents { get; }
	/// <summary>
	/// Every key that is currently held down, including keys first pressed on an earlier iteration.
	/// </summary>
	/// <remarks>
	/// The scroll wheel is the exception to "held": because a scroll notch has no duration, <see cref="KeyboardOrMouseKey.MouseWheelUp"/> and
	/// <see cref="KeyboardOrMouseKey.MouseWheelDown"/> are reported as a press and a release in the same iteration, and therefore never appear here.
	/// </remarks>
	IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> CurrentlyPressedKeys { get; }
	/// <summary>
	/// Every mouse click that occurred in the most recent iteration, in the order they occurred.
	/// </summary>
	/// <remarks>
	/// These carry the position the click happened at and whether it formed part of a double-click, neither of which the plain key events describe; but every click
	/// here also appears as a press in <see cref="NewKeyDownEvents"/>.
	/// </remarks>
	IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, MouseClickEvent> NewMouseClicks { get; }

	/// <summary>
	/// Where the mouse cursor is, measured in pixels from the top-left corner of the window it is over, with <c>Y</c> increasing downward.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This holds its last known value when the cursor is not moving, and does not update at all whilst the cursor is locked to a window
	/// (see <see cref="Window.LockCursor"/>) because the operating system stops moving the cursor in that mode; use <see cref="MouseCursorDelta"/> there instead.
	/// </para>
	/// <para>
	/// Note that <c>Y</c> increases <i>downward</i> here, following the usual convention for on-screen coordinates and opposite to the convention used throughout
	/// most of the rest of TinyFFR.
	/// </para>
	/// </remarks>
	XYPair<int> MouseCursorPosition { get; }
	/// <summary>
	/// How far the mouse cursor moved during the most recent iteration, in pixels, using the same axis directions as <see cref="MouseCursorPosition"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is the total movement across the iteration rather than the difference between two sampled positions, so it does not lose movement that happened within a
	/// single iteration. It is zero on any iteration where the cursor did not move, and also on the first iteration the cursor is observed (as there is nothing to
	/// measure movement from). This remains accurate whilst the cursor is locked to a window, which is what makes it the right input for mouse-look controls.
	/// </para>
	/// <para>
	/// Note that <c>Y</c> increases <i>downward</i> here, following the usual convention for on-screen coordinates and opposite to the convention used throughout
	/// most of the rest of TinyFFR.
	/// </para>
	/// </remarks>
	XYPair<int> MouseCursorDelta { get; }
	/// <summary>
	/// How many notches the scroll wheel was turned during the most recent iteration. Positive values indicate the wheel was scrolled <i>down</i> (i.e. toward the user), negative values indicate up.
	/// </summary>
	/// <remarks>
	/// Note the sign convention: down is positive. Input devices that scroll smoothly rather than in discrete notches (such as a laptop trackpad) have their movement
	/// accumulated until it amounts to a whole notch, so a slow scroll may report <c>0</c> for several iterations before reporting <c>1</c>. Each notch is also
	/// reported as a <see cref="KeyboardOrMouseKey.MouseWheelUp"/>/<see cref="KeyboardOrMouseKey.MouseWheelDown"/> press-and-release pair in the key event collections.
	/// </remarks>
	int MouseScrollWheelDelta { get; }

	/// <summary>
	/// The text the user typed during the most recent iteration, or an empty span if they typed nothing or text transcription is disabled.
	/// </summary>
	/// <remarks>
	/// This is empty unless <see cref="ApplicationLoop.EnableInputTextTranscription"/> has been set to <see langword="true"/>. Where it is enabled, this is the
	/// correct source for anything the user types in to your application, as it accounts for their keyboard layout, modifier keys and any input method they use; and
	/// some keystrokes may then be reported only here and not as key events, because the operating system's text input handling can consume them.
	/// </remarks>
	ReadOnlySpan<char> TranscribedText { get; }

	/// <summary>
	/// Returns whether <paramref name="key"/> is currently held down, including if it was first pressed on an earlier iteration.
	/// </summary>
	/// <param name="key">The key to test.</param>
	bool KeyIsCurrentlyDown(KeyboardOrMouseKey key);
	/// <summary>
	/// Returns whether <paramref name="key"/> was pressed down during the most recent iteration.
	/// </summary>
	/// <remarks>
	/// This is <see langword="true"/> only on the iteration in which the key was first pressed, and not on subsequent iterations it remains held for; use
	/// <see cref="KeyIsCurrentlyDown"/> for the latter.
	/// </remarks>
	/// <param name="key">The key to test.</param>
	bool KeyWasPressedThisIteration(KeyboardOrMouseKey key);
	/// <summary>
	/// Returns whether <paramref name="key"/> was released during the most recent iteration.
	/// </summary>
	/// <param name="key">The key to test.</param>
	bool KeyWasReleasedThisIteration(KeyboardOrMouseKey key);
}