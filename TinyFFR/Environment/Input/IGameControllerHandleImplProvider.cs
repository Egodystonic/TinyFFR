// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

interface IGameControllerHandleImplProvider {
	int GetName(GameControllerHandle handle, Span<char> dest);
	int GetNameMaxLength();

	GameControllerStickPosition GetStickPosition(GameControllerHandle handle, bool leftStick);
	GameControllerTriggerPosition GetTriggerPosition(GameControllerHandle handle, bool leftTrigger);

	ReadOnlySpan<GameControllerButtonEvent> GetNewButtonEvents(GameControllerHandle handle);
	ReadOnlySpan<GameControllerButton> GetNewButtonDownEvents(GameControllerHandle handle);
	ReadOnlySpan<GameControllerButton> GetNewButtonUpEvents(GameControllerHandle handle);
	ReadOnlySpan<GameControllerButton> GetCurrentlyPressedButtons(GameControllerHandle handle);

	bool IsButtonDown(GameControllerHandle handle, GameControllerButton button);
	bool WasButtonPressed(GameControllerHandle handle, GameControllerButton button);
	bool WasButtonReleased(GameControllerHandle handle, GameControllerButton button);
}