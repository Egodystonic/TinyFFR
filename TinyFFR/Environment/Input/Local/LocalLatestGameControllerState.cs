// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Input.Local;

sealed unsafe class LocalLatestGameControllerState : ILatestGameControllerInputStateRetriever, IDisposable {
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
	int _iterationVersion = 0;

	IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButtonEvent> ILatestGameControllerInputStateRetriever.NewButtonEvents => new(
		this, _iterationVersion, &GetNewButtonEventsSpanLength, &GetIterationVersion, &GetNewButtonEvent 	
	);
	IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton> ILatestGameControllerInputStateRetriever.NewButtonDownEvents => new(
		this, _iterationVersion, &GetNewButtonDownEventsSpanLength, &GetIterationVersion, &GetNewButtonDownEvent
	);
	IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton> ILatestGameControllerInputStateRetriever.NewButtonUpEvents => new(
		this, _iterationVersion, &GetNewButtonUpEventsSpanLength, &GetIterationVersion, &GetNewButtonUpEvent
	);
	IndirectEnumerable<ILatestGameControllerInputStateRetriever, GameControllerButton> ILatestGameControllerInputStateRetriever.CurrentlyPressedButtons => new(
		this, _iterationVersion, &GetCurrentlyPressedButtonsSpanLength, &GetIterationVersion, &GetCurrentlyPressedButton
	);

	public LocalLatestGameControllerState(UIntPtr handle) {
		Handle = handle;
		NameBuffer = new(MaxControllerNameLength, true);
	}

	static LocalLatestGameControllerState CastWithDisposeCheck(ILatestGameControllerInputStateRetriever input) {
		var result = ((LocalLatestGameControllerState) input);
		result.ThrowIfThisIsDisposed();
		return result;
	}

	static int GetNewButtonEventsSpanLength(ILatestGameControllerInputStateRetriever input) {
		return CastWithDisposeCheck(input).NewButtonEvents.Count;
	}
	static GameControllerButtonEvent GetNewButtonEvent(ILatestGameControllerInputStateRetriever input, int index) {
		return CastWithDisposeCheck(input).NewButtonEvents[index];
	}
	static int GetNewButtonDownEventsSpanLength(ILatestGameControllerInputStateRetriever input) {
		return CastWithDisposeCheck(input).NewButtonDownEvents.Count;
	}
	static GameControllerButton GetNewButtonDownEvent(ILatestGameControllerInputStateRetriever input, int index) {
		return CastWithDisposeCheck(input).NewButtonDownEvents[index];
	}
	static int GetNewButtonUpEventsSpanLength(ILatestGameControllerInputStateRetriever input) {
		return CastWithDisposeCheck(input).NewButtonUpEvents.Count;
	}
	static GameControllerButton GetNewButtonUpEvent(ILatestGameControllerInputStateRetriever input, int index) {
		return CastWithDisposeCheck(input).NewButtonUpEvents[index];
	}
	static int GetCurrentlyPressedButtonsSpanLength(ILatestGameControllerInputStateRetriever input) {
		return CastWithDisposeCheck(input).CurrentlyPressedButtons.Count;
	}
	static GameControllerButton GetCurrentlyPressedButton(ILatestGameControllerInputStateRetriever input, int index) {
		return CastWithDisposeCheck(input).CurrentlyPressedButtons[index];
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

	public void Iterate() {
		_iterationVersion++;
	}
	static int GetIterationVersion(ILatestGameControllerInputStateRetriever input) => ((LocalLatestGameControllerState) input)._iterationVersion;

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