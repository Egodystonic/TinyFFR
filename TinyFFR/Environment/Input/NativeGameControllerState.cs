// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Environment.Desktop;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Input;

sealed class NativeGameControllerState : IGameControllerInputTracker, IDisposable {
	public InteropStringBuffer NameBuffer { get; }
	public GameControllerHandle Handle { get; }
	public ArrayPoolBackedVector<GameControllerButtonEvent> NewButtonEvents { get; } = new();
	public ArrayPoolBackedVector<GameControllerButton> NewButtonDownEvents { get; } = new();
	public ArrayPoolBackedVector<GameControllerButton> NewButtonUpEvents { get; } = new();
	public ArrayPoolBackedVector<GameControllerButton> CurrentlyPressedButtons { get; } = new();
	public GameControllerStickPosition LeftStickPosition { get; set; } = default;
	public GameControllerStickPosition RightStickPosition { get; set; } = default;
	public GameControllerTriggerPosition LeftTriggerPosition { get; set; } = default;
	public GameControllerTriggerPosition RightTriggerPosition { get; set; } = default;
	bool _isDisposed = false;

	public string ControllerName {
		get {
			ThrowIfThisIsDisposed();
			var maxSpanLength = GetControllerNameSpanMaxLength();
			var dest = maxSpanLength <= 1000 ? stackalloc char[maxSpanLength] : new char[maxSpanLength];

			var numCharsWritten = GetControllerNameUsingSpan(dest);
			return new(dest[..numCharsWritten]);
		}
	}

	ReadOnlySpan<GameControllerButtonEvent> IGameControllerInputTracker.NewButtonEvents {
		get {
			ThrowIfThisIsDisposed();
			return NewButtonEvents.AsSpan;
		}
	}
	ReadOnlySpan<GameControllerButton> IGameControllerInputTracker.NewButtonDownEvents {
		get {
			ThrowIfThisIsDisposed();
			return NewButtonDownEvents.AsSpan;
		}
	}
	ReadOnlySpan<GameControllerButton> IGameControllerInputTracker.NewButtonUpEvents {
		get {
			ThrowIfThisIsDisposed();
			return NewButtonUpEvents.AsSpan;
		}
	}
	ReadOnlySpan<GameControllerButton> IGameControllerInputTracker.CurrentlyPressedButtons {
		get {
			ThrowIfThisIsDisposed();
			return CurrentlyPressedButtons.AsSpan;
		}
	}

	public NativeGameControllerState(GameControllerHandle handle, InputTrackerConfig config) {
		Handle = handle;
		NameBuffer = new(config.MaxControllerNameLength);
	}

	public int GetControllerNameUsingSpan(Span<char> dest) {
		ThrowIfThisIsDisposed();
		return NameBuffer.ReadTo(dest);
	}
	public int GetControllerNameSpanMaxLength() {
		ThrowIfThisIsDisposed();
		return NameBuffer.BufferLength;
	}
	public bool ButtonIsCurrentlyDown(GameControllerButton button) {
		ThrowIfThisIsDisposed();
		var curButtons = CurrentlyPressedButtons;
		for (var i = 0; i < curButtons.Count; ++i) {
			if (curButtons[i] == button) return true;
		}
		return false;
	}
	public bool ButtonWasPressedThisIteration(GameControllerButton button) {
		ThrowIfThisIsDisposed();
		var downButtons = NewButtonDownEvents;
		for (var i = 0; i < downButtons.Count; ++i) {
			if (downButtons[i] == button) return true;
		}
		return false;
	}
	public bool ButtonWasReleasedThisIteration(GameControllerButton button) {
		ThrowIfThisIsDisposed();
		var upButtons = NewButtonUpEvents;
		for (var i = 0; i < upButtons.Count; ++i) {
			if (upButtons[i] == button) return true;
		}
		return false;
	}

	public void ClearForNextIteration() {
		ThrowIfThisIsDisposed();
		NewButtonEvents.ClearWithoutZeroingMemory();
		NewButtonDownEvents.ClearWithoutZeroingMemory();
		NewButtonUpEvents.ClearWithoutZeroingMemory();
		CurrentlyPressedButtons.ClearWithoutZeroingMemory();
	}

	public void ApplyEvent(RawGameControllerButtonEvent rawEvent) {
		ThrowIfThisIsDisposed();
		switch (rawEvent.Type) {
			case RawGameControllerEventType.LeftStickAxisX:
				LeftStickPosition = LeftStickPosition with { HorizontalOffsetRaw = rawEvent.NewValue };
				break;
			case RawGameControllerEventType.LeftStickAxisY:
				LeftStickPosition = LeftStickPosition with { VerticalOffsetRaw = rawEvent.NewValue != Int16.MinValue ? ((short) -rawEvent.NewValue) : Int16.MaxValue };
				break;
			case RawGameControllerEventType.RightStickAxisX:
				RightStickPosition = RightStickPosition with { HorizontalOffsetRaw = rawEvent.NewValue };
				break;
			case RawGameControllerEventType.RightStickAxisY:
				RightStickPosition = RightStickPosition with { VerticalOffsetRaw = rawEvent.NewValue != Int16.MinValue ? ((short) -rawEvent.NewValue) : Int16.MaxValue };
				break;
			case RawGameControllerEventType.LeftTrigger: {
				var prevDisplacementLevel = LeftTriggerPosition.DisplacementLevel;
				LeftTriggerPosition = new(rawEvent.NewValue);
				var newDisplacementLevel = LeftTriggerPosition.DisplacementLevel;
				if (prevDisplacementLevel == AnalogDisplacementLevel.None && newDisplacementLevel != AnalogDisplacementLevel.None) {
					PushButtonEvent(RawGameControllerEventType.LeftTrigger, true);
				}
				else if (prevDisplacementLevel != AnalogDisplacementLevel.None && newDisplacementLevel == AnalogDisplacementLevel.None) {
					PushButtonEvent(RawGameControllerEventType.LeftTrigger, false);
				}
				break;
			}
			case RawGameControllerEventType.RightTrigger: {
				var prevDisplacementLevel = RightTriggerPosition.DisplacementLevel;
				RightTriggerPosition = new(rawEvent.NewValue);
				var newDisplacementLevel = RightTriggerPosition.DisplacementLevel;
				if (prevDisplacementLevel == AnalogDisplacementLevel.None && newDisplacementLevel != AnalogDisplacementLevel.None) {
					PushButtonEvent(RawGameControllerEventType.RightTrigger, true);
				}
				else if (prevDisplacementLevel != AnalogDisplacementLevel.None && newDisplacementLevel == AnalogDisplacementLevel.None) {
					PushButtonEvent(RawGameControllerEventType.RightTrigger, false);
				}
				break;
			}
			default:
				PushButtonEvent(rawEvent.Type, rawEvent.NewValue != 0);
				break;
		}
	}

	void PushButtonEvent(RawGameControllerEventType type, bool down) {
		var nonRawButton = (GameControllerButton) (int) (type + 1);
		NewButtonEvents.Add(new GameControllerButtonEvent(nonRawButton, down));
		if (down) {
			NewButtonDownEvents.Add(nonRawButton);
			if (!CurrentlyPressedButtons.Contains(nonRawButton)) CurrentlyPressedButtons.Add(nonRawButton);
		}
		else {
			NewButtonUpEvents.Add(nonRawButton);
			CurrentlyPressedButtons.Remove(nonRawButton);
		}
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			NewButtonEvents.Dispose();
			NewButtonDownEvents.Dispose();
			NewButtonUpEvents.Dispose();
			CurrentlyPressedButtons.Dispose();
			NameBuffer.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
}