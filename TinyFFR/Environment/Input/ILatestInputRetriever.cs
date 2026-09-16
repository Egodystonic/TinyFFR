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

	/// <summary>
	/// The latest state of the keyboard and mouse.
	/// </summary>
	ILatestKeyboardAndMouseInputRetriever KeyboardAndMouse { get; }

	/// <summary>
	/// The latest state of each connected game controller, one entry per controller.
	/// </summary>
	/// <remarks>
	/// If your application does not actually care which controller should be usef, <see cref="GameControllersCombined"/> is simpler
	/// and does not require you to decide which of several connected controllers to listen to.
	/// </remarks>
	IndirectEnumerable<ILatestInputRetriever, ILatestGameControllerInputRetriever> GameControllers { get; }
	/// <summary>
	/// The latest state of every connected game controller merged in to one, so that any controller can drive the application.
	/// </summary>
	/// <remarks>
	/// Every connected controller's input is applied to this one state, as though they were all the same device: pressing a button on any controller presses it
	/// here, and moving a stick or trigger on any controller moves it here. This is usually what a single-user application wants, as it saves the user from having
	/// to use the particular controller the application happened to pick. Where two controllers are used at once their input simply interleaves, with the most
	/// recent input winning; that is rarely a problem in practice, but it is why this is not a substitute for reading <see cref="GameControllers"/> individually
	/// when more than one simultaneousu user is expected.
	/// </remarks>
	ILatestGameControllerInputRetriever GameControllersCombined { get; }

	/// <summary>
	/// Access to the system clipboard.
	/// </summary>
	IInputClipboard Clipboard { get; }
}