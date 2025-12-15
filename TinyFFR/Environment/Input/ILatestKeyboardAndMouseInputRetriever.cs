// Created on 2024-02-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

public interface ILatestKeyboardAndMouseInputRetriever {
	IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKeyEvent> NewKeyEvents { get; }
	IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> NewKeyDownEvents { get; }
	IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> NewKeyUpEvents { get; }
	IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> CurrentlyPressedKeys { get; }
	IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, MouseClickEvent> NewMouseClicks { get; }

	XYPair<int> MouseCursorPosition { get; } // TODO document that this is relative to window, e.g. (0, 0) is top left corner of the window
	XYPair<int> MouseCursorDelta { get; }
	int MouseScrollWheelDelta { get; } // TODO document that down is positive, up is negative

	bool KeyIsCurrentlyDown(KeyboardOrMouseKey key);
	bool KeyWasPressedThisIteration(KeyboardOrMouseKey key);
	bool KeyWasReleasedThisIteration(KeyboardOrMouseKey key);
}