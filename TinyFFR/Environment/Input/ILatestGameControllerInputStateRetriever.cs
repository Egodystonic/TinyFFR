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

	public TypedReferentIterator<ILatestGameControllerInputStateRetriever, GameControllerButtonEvent> NewButtonEvents { get; }
	public TypedReferentIterator<ILatestGameControllerInputStateRetriever, GameControllerButton> NewButtonDownEvents { get; }
	public TypedReferentIterator<ILatestGameControllerInputStateRetriever, GameControllerButton> NewButtonUpEvents { get; }
	public TypedReferentIterator<ILatestGameControllerInputStateRetriever, GameControllerButton> CurrentlyPressedButtons { get; }

	public bool ButtonIsCurrentlyDown(GameControllerButton button);
	public bool ButtonWasPressedThisIteration(GameControllerButton button);
	public bool ButtonWasReleasedThisIteration(GameControllerButton button);
}