// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

public interface IGameControllerInputTracker {
	GameControllerId? ControllerId { get; }
	bool IsConnected { get; }

	GameControllerStickPosition LeftStickPosition { get; }
	GameControllerStickPosition RightStickPosition { get; }
	GameControllerTriggerPosition LeftTriggerPosition { get; }
	GameControllerTriggerPosition RightTriggerPosition { get; }

	ReadOnlySpan<GameControllerButtonEvent> NewButtonEvents { get; }
	ReadOnlySpan<GameControllerButton> CurrentlyPressedButtons { get; }
}