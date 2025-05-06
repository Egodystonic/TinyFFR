// Created on 2024-02-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

public interface ILatestKeyboardAndMouseInputRetriever {
	TypedReferentIterator<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKeyEvent> NewKeyEvents { get; }
	TypedReferentIterator<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> NewKeyDownEvents { get; }
	TypedReferentIterator<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> NewKeyUpEvents { get; }
	TypedReferentIterator<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> CurrentlyPressedKeys { get; }
	TypedReferentIterator<ILatestKeyboardAndMouseInputRetriever, MouseClickEvent> NewMouseClicks { get; }

	XYPair<int> MouseCursorPosition { get; } // TODO document that this is relative to window, e.g. (0, 0) is top left corner of the window
	XYPair<int> MouseCursorDelta { get; }
	int MouseScrollWheelDelta { get; } // TODO document that down is positive, up is negative

	bool KeyIsCurrentlyDown(KeyboardOrMouseKey key);
	bool KeyWasPressedThisIteration(KeyboardOrMouseKey key);
	bool KeyWasReleasedThisIteration(KeyboardOrMouseKey key);
}