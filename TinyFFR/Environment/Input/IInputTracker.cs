// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

public interface IInputTracker { 
	bool UserQuitRequested { get; }

	ReadOnlySpan<KeyboardOrMouseKeyEvent> NewKeyEvents { get; }
	ReadOnlySpan<KeyboardOrMouseKey> NewKeyDownEvents { get; }
	ReadOnlySpan<KeyboardOrMouseKey> NewKeyUpEvents { get; }
	ReadOnlySpan<KeyboardOrMouseKey> CurrentlyPressedKeys { get; } 

	XYPair<int> MouseCursorPosition { get; } // TODO document that this is relative to window, e.g. (0, 0) is top left corner of the window
	int MouseScrollWheelDelta { get; } // TODO document that down is positive, up is negative

	ReadOnlySpan<GameController> GameControllers { get; }
	GameController GetAmalgamatedGameController();

	bool KeyIsCurrentlyDown(KeyboardOrMouseKey key);
	bool KeyWasPressedThisIteration(KeyboardOrMouseKey key);
	bool KeyWasReleasedThisIteration(KeyboardOrMouseKey key);
}