// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

public interface IInputTracker { 
	bool UserQuitRequested { get; }

	NativeResourceCollection<KeyboardOrMouseKeyEvent> NewKeyEvents { get; }
	NativeResourceCollection<KeyboardOrMouseKey> CurrentlyPressedKeys { get; } 

	XYPair MouseCursorPosition { get; }

	NativeResourceCollection<GameControllerId> ConnectedGameControllers { get; }
	// TODO document that null input means we'll return an "amalgamated" tracker that just uses events from all game controllers together -- easy to support whatever the user wants to use then
	IGameControllerInputTracker GetInputTrackerForGameController(GameControllerId? controllerId = null);
}