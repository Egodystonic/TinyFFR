// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Input;

sealed class StubGameControllerInputRetriever : ILatestGameControllerInputRetriever {
	const string Name = "Stub Ui Framework Controller (Not Actually Supported)";

	public GameControllerStickPosition LeftStickPosition => GameControllerStickPosition.Centered;
	public GameControllerStickPosition RightStickPosition => GameControllerStickPosition.Centered;
	public GameControllerTriggerPosition LeftTriggerPosition => GameControllerTriggerPosition.Zero;
	public GameControllerTriggerPosition RightTriggerPosition => GameControllerTriggerPosition.Zero;

	public IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButtonEvent> NewButtonEvents => IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButtonEvent>.Empty;
	public IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButton> NewButtonDownEvents => IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButton>.Empty;
	public IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButton> NewButtonUpEvents => IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButton>.Empty;
	public IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButton> CurrentlyPressedButtons => IndirectEnumerable<ILatestGameControllerInputRetriever, GameControllerButton>.Empty;

	public bool ButtonIsCurrentlyDown(GameControllerButton button) => false;
	public bool ButtonWasPressedThisIteration(GameControllerButton button) => false;
	public bool ButtonWasReleasedThisIteration(GameControllerButton button) => false;

	public string GetNameAsNewStringObject() => Name;
	public int GetNameLength() => Name.Length;
	public void CopyName(Span<char> destinationBuffer) => Name.CopyTo(destinationBuffer);

	public override string ToString() => Name;
}
