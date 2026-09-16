// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Retrieves the latest state of user input devices (keyboard, mouse, gamepads) and provides access to the system <see cref="Clipboard"/>.
/// </summary>
/// <remarks>
/// The values exposed by this interface are generally updated once each time <see cref="ApplicationLoop.IterateOnce"/> is invoked.
/// </remarks>
public interface ILatestInputRetriever { 
	/// <summary>
	/// True if the user has requested the application quit since the last input update (usually via X button on a window or key combination such as Alt+F4).
	/// </summary>
	bool UserQuitRequested { get; }

	ILatestKeyboardAndMouseInputRetriever KeyboardAndMouse { get; }

	IndirectEnumerable<ILatestInputRetriever, ILatestGameControllerInputRetriever> GameControllers { get; }
	ILatestGameControllerInputRetriever GameControllersCombined { get; }
	
	IInputClipboard Clipboard { get; }
}