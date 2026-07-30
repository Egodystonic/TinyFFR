// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Environment.Input.Ui;

// Stands in for a game controller state when no controller data source is available (no supported UI framework
// surfaces game controller input). Always reports a completely neutral/unpressed state.
sealed class NeutralGameControllerState : ILatestGameControllerInputStateRetriever {
	const string NoControllerName = "<None>";

	public GameControllerStickPosition LeftStickPosition => default;
	public GameControllerStickPosition RightStickPosition => default;
	public GameControllerTriggerPosition LeftTriggerPosition => default;
	public GameControllerTriggerPosition RightTriggerPosition => default;

	public IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButtonEvent> NewButtonEvents => IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButtonEvent>.Empty;
	public IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton> NewButtonDownEvents => IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton>.Empty;
	public IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton> NewButtonUpEvents => IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton>.Empty;
	public IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton> CurrentlyPressedButtons => IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton>.Empty;

	public bool ButtonIsCurrentlyDown(GameControllerButton button) => false;
	public bool ButtonWasPressedThisIteration(GameControllerButton button) => false;
	public bool ButtonWasReleasedThisIteration(GameControllerButton button) => false;

	public string GetNameAsNewStringObject() => NoControllerName;
	public int GetNameLength() => NoControllerName.Length;
	public void CopyName(Span<char> destinationBuffer) => NoControllerName.CopyTo(destinationBuffer);

	public override string ToString() => NoControllerName;
}
