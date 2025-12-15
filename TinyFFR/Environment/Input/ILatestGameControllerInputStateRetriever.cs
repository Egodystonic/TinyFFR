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

	public IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButtonEvent> NewButtonEvents { get; }
	public IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton> NewButtonDownEvents { get; }
	public IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton> NewButtonUpEvents { get; }
	public IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton> CurrentlyPressedButtons { get; }

	public bool ButtonIsCurrentlyDown(GameControllerButton button);
	public bool ButtonWasPressedThisIteration(GameControllerButton button);
	public bool ButtonWasReleasedThisIteration(GameControllerButton button);
}