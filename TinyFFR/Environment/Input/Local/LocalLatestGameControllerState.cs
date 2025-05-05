// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Input.Local;

sealed class LocalLatestGameControllerState : ILatestGameControllerInputStateRetriever, IDisposable {
	const int MaxControllerNameLength = 500;
	public InteropStringBuffer NameBuffer { get; }
	public UIntPtr Handle { get; }
	public ArrayPoolBackedVector<GameControllerButtonEvent> NewButtonEvents { get; } = new();
	public ArrayPoolBackedVector<GameControllerButton> NewButtonDownEvents { get; } = new();
	public ArrayPoolBackedVector<GameControllerButton> NewButtonUpEvents { get; } = new();
	public ArrayPoolBackedVector<GameControllerButton> CurrentlyPressedButtons { get; } = new();
	public GameControllerStickPosition LeftStickPosition { get; set; } = default;
	public GameControllerStickPosition RightStickPosition { get; set; } = default;
	public GameControllerTriggerPosition LeftTriggerPosition { get; set; } = default;
	public GameControllerTriggerPosition RightTriggerPosition { get; set; } = default;
	readonly UnmanagedBuffer<char> _utf16NameBuffer = new(16);
	bool _isDisposed = false;

	ReadOnlySpan<GameControllerButtonEvent> ILatestGameControllerInputStateRetriever.NewButtonEvents {
		get {
			ThrowIfThisIsDisposed();
			return NewButtonEvents.AsSpan;
		}
	}
	ReadOnlySpan<GameControllerButton> ILatestGameControllerInputStateRetriever.NewButtonDownEvents {
		get {
			ThrowIfThisIsDisposed();
			return NewButtonDownEvents.AsSpan;
		}
	}
	ReadOnlySpan<GameControllerButton> ILatestGameControllerInputStateRetriever.NewButtonUpEvents {
		get {
			ThrowIfThisIsDisposed();
			return NewButtonUpEvents.AsSpan;
		}
	}
	ReadOnlySpan<GameControllerButton> ILatestGameControllerInputStateRetriever.CurrentlyPressedButtons {
		get {
			ThrowIfThisIsDisposed();
			return CurrentlyPressedButtons.AsSpan;
		}
	}

	public LocalLatestGameControllerState(UIntPtr handle) {
		Handle = handle;
		NameBuffer = new(MaxControllerNameLength, true);
	}

	ReadOnlySpan<char> GetNameBufferSpan() {
		ThrowIfThisIsDisposed();
		var minLengthRequired = NameBuffer.GetUtf16Length();
		if (minLengthRequired > _utf16NameBuffer.Length) _utf16NameBuffer.Resize(minLengthRequired);
		NameBuffer.ConvertToUtf16(_utf16NameBuffer.AsSpan);
		return _utf16NameBuffer.AsSpan[..minLengthRequired];
	}
	public string GetNameAsNewStringObject() => new String(GetNameBufferSpan());
	public int GetNameLength() => GetNameBufferSpan().Length;
	public void CopyName(Span<char> destinationBuffer) => GetNameBufferSpan().CopyTo(destinationBuffer);

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

	public void ApplyEvent(RawLocalGameControllerButtonEvent rawEvent) {
		ThrowIfThisIsDisposed();
		switch (rawEvent.Type) {
			case RawLocalGameControllerEventType.LeftStickAxisX:
				LeftStickPosition = LeftStickPosition with { RawDisplacementHorizontal = rawEvent.NewValue };
				break;
			case RawLocalGameControllerEventType.LeftStickAxisY:
				LeftStickPosition = LeftStickPosition with { RawDisplacementVertical = rawEvent.NewValue != Int16.MinValue ? ((short) -rawEvent.NewValue) : Int16.MaxValue };
				break;
			case RawLocalGameControllerEventType.RightStickAxisX:
				RightStickPosition = RightStickPosition with { RawDisplacementHorizontal = rawEvent.NewValue };
				break;
			case RawLocalGameControllerEventType.RightStickAxisY:
				RightStickPosition = RightStickPosition with { RawDisplacementVertical = rawEvent.NewValue != Int16.MinValue ? ((short) -rawEvent.NewValue) : Int16.MaxValue };
				break;
			case RawLocalGameControllerEventType.LeftTrigger: {
				var prevDisplacementLevel = LeftTriggerPosition.DisplacementLevel;
				LeftTriggerPosition = new(rawEvent.NewValue);
				var newDisplacementLevel = LeftTriggerPosition.DisplacementLevel;
				if (prevDisplacementLevel == AnalogDisplacementLevel.None && newDisplacementLevel != AnalogDisplacementLevel.None) {
					PushButtonEvent(RawLocalGameControllerEventType.LeftTrigger, true);
				}
				else if (prevDisplacementLevel != AnalogDisplacementLevel.None && newDisplacementLevel == AnalogDisplacementLevel.None) {
					PushButtonEvent(RawLocalGameControllerEventType.LeftTrigger, false);
				}
				break;
			}
			case RawLocalGameControllerEventType.RightTrigger: {
				var prevDisplacementLevel = RightTriggerPosition.DisplacementLevel;
				RightTriggerPosition = new(rawEvent.NewValue);
				var newDisplacementLevel = RightTriggerPosition.DisplacementLevel;
				if (prevDisplacementLevel == AnalogDisplacementLevel.None && newDisplacementLevel != AnalogDisplacementLevel.None) {
					PushButtonEvent(RawLocalGameControllerEventType.RightTrigger, true);
				}
				else if (prevDisplacementLevel != AnalogDisplacementLevel.None && newDisplacementLevel == AnalogDisplacementLevel.None) {
					PushButtonEvent(RawLocalGameControllerEventType.RightTrigger, false);
				}
				break;
			}
			default:
				PushButtonEvent(rawEvent.Type, rawEvent.NewValue != 0);
				break;
		}
	}

	void PushButtonEvent(RawLocalGameControllerEventType type, bool down) {
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

	public override string ToString() => $"TinyFFR Local Input State Provider {(_isDisposed ? "[Game Controller] [Disposed]" : $"[Game Controller '{GetNameAsNewStringObject()}']")}";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			NewButtonEvents.Dispose();
			NewButtonDownEvents.Dispose();
			NewButtonUpEvents.Dispose();
			CurrentlyPressedButtons.Dispose();
			NameBuffer.Dispose();
			_utf16NameBuffer.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(ILatestGameControllerInputStateRetriever));
	}
	#endregion
}