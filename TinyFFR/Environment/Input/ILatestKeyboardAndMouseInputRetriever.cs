// Created on 2024-02-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

public interface ILatestKeyboardAndMouseInputRetriever {
	ReadOnlySpan<KeyboardOrMouseKeyEvent> NewKeyEvents { get; }
	ReadOnlySpan<KeyboardOrMouseKey> NewKeyDownEvents { get; }
	ReadOnlySpan<KeyboardOrMouseKey> NewKeyUpEvents { get; }
	ReadOnlySpan<KeyboardOrMouseKey> CurrentlyPressedKeys { get; }
	ReadOnlySpan<MouseClickEvent> NewMouseClicks { get; }

	XYPair<int> MouseCursorPosition { get; } // TODO document that this is relative to window, e.g. (0, 0) is top left corner of the window
	XYPair<int> MouseCursorDelta { get; }
	int MouseScrollWheelDelta { get; } // TODO document that down is positive, up is negative

	bool KeyIsCurrentlyDown(KeyboardOrMouseKey key);
	bool KeyWasPressedThisIteration(KeyboardOrMouseKey key);
	bool KeyWasReleasedThisIteration(KeyboardOrMouseKey key);
}