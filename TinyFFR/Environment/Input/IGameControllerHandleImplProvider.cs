// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

interface IGameControllerHandleImplProvider {
	int GetName(GameControllerHandle handle, Span<char> dest);
	int GetNameMaxLength();

	bool IsConnected(GameControllerHandle handle);

	GameControllerStickPosition GetStickPosition(GameControllerHandle handle, bool leftStick);
	GameControllerTriggerPosition GetTriggerPosition(GameControllerHandle handle, bool leftTrigger);

	ReadOnlySpan<GameControllerButtonEvent> GetNewButtonEvents(GameControllerHandle handle);
	ReadOnlySpan<GameControllerButton> GetCurrentlyPressedButtons(GameControllerHandle handle);

	bool IsButtonDown(GameControllerHandle handle, GameControllerButton button);
}