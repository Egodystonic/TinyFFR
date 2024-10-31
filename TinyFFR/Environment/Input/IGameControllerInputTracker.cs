// Created on 2024-02-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Reflection.Metadata;

namespace Egodystonic.TinyFFR.Environment.Input;

public interface IGameControllerInputTracker {
	public string ControllerName { get; }

	public GameControllerStickPosition LeftStickPosition { get; }
	public GameControllerStickPosition RightStickPosition { get; }
	public GameControllerTriggerPosition LeftTriggerPosition { get; }
	public GameControllerTriggerPosition RightTriggerPosition { get; }

	public ReadOnlySpan<GameControllerButtonEvent> NewButtonEvents { get; }
	public ReadOnlySpan<GameControllerButton> NewButtonDownEvents { get; }
	public ReadOnlySpan<GameControllerButton> NewButtonUpEvents { get; }
	public ReadOnlySpan<GameControllerButton> CurrentlyPressedButtons { get; }

	public int GetControllerNameUsingSpan(Span<char> dest);
	public int GetControllerNameSpanMaxLength(); // TODO make this just report the actual length, not max
	public bool ButtonIsCurrentlyDown(GameControllerButton button);
	public bool ButtonWasPressedThisIteration(GameControllerButton button);
	public bool ButtonWasReleasedThisIteration(GameControllerButton button);
}