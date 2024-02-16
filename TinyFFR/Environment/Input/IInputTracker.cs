// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

public interface IInputTracker { 
	bool UserQuitRequested { get; }

	IKeyboardAndMouseInputTracker KeyboardAndMouse { get; }

	ReadOnlySpan<IGameControllerInputTracker> GameControllers { get; }
	IGameControllerInputTracker GameControllersCombined { get; }
}