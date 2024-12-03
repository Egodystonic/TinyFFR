// Created on 2024-02-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Reflection.Metadata;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Input;

public interface ILatestGameControllerInputStateRetriever : IStringSpanNameEnabled {
	public GameControllerStickPosition LeftStickPosition { get; }
	public GameControllerStickPosition RightStickPosition { get; }
	public GameControllerTriggerPosition LeftTriggerPosition { get; }
	public GameControllerTriggerPosition RightTriggerPosition { get; }

	public ReadOnlySpan<GameControllerButtonEvent> NewButtonEvents { get; }
	public ReadOnlySpan<GameControllerButton> NewButtonDownEvents { get; }
	public ReadOnlySpan<GameControllerButton> NewButtonUpEvents { get; }
	public ReadOnlySpan<GameControllerButton> CurrentlyPressedButtons { get; }

	public bool ButtonIsCurrentlyDown(GameControllerButton button);
	public bool ButtonWasPressedThisIteration(GameControllerButton button);
	public bool ButtonWasReleasedThisIteration(GameControllerButton button);
}