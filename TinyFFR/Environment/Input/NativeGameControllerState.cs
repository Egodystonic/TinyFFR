// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Environment.Desktop;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Input;

[SuppressUnmanagedCodeSecurity]
sealed class NativeGameControllerState : IDisposable {
	public InteropStringBuffer NameBuffer { get; }
	public ArrayPoolBackedVector<GameControllerButtonEvent> NewButtonEvents { get; } = new();
	public ArrayPoolBackedVector<GameControllerButton> NewButtonDownEvents { get; } = new();
	public ArrayPoolBackedVector<GameControllerButton> NewButtonUpEvents { get; } = new();
	public ArrayPoolBackedVector<GameControllerButton> CurrentlyPressedButtons { get; } = new();
	public GameControllerStickPosition LeftStickPos { get; set; } = default;
	public GameControllerStickPosition RightStickPos { get; set; } = default;
	public GameControllerTriggerPosition LeftTriggerPos { get; set; } = default;
	public GameControllerTriggerPosition RightTriggerPos { get; set; } = default;
	bool _isDisposed = false;

	public NativeGameControllerState(int nameBufferLength) => NameBuffer = new(nameBufferLength);

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
				LeftStickPos = LeftStickPos with { HorizontalOffsetRaw = rawEvent.NewValue };
				break;
			case RawGameControllerEventType.LeftStickAxisY:
				LeftStickPos = LeftStickPos with { VerticalOffsetRaw = rawEvent.NewValue != Int16.MinValue ? ((short) -rawEvent.NewValue) : Int16.MaxValue };
				break;
			case RawGameControllerEventType.RightStickAxisX:
				RightStickPos = RightStickPos with { HorizontalOffsetRaw = rawEvent.NewValue };
				break;
			case RawGameControllerEventType.RightStickAxisY:
				RightStickPos = RightStickPos with { VerticalOffsetRaw = rawEvent.NewValue != Int16.MinValue ? ((short) -rawEvent.NewValue) : Int16.MaxValue };
				break;
			case RawGameControllerEventType.LeftTrigger: {
				var prevDisplacementLevel = LeftTriggerPos.DisplacementLevel;
				LeftTriggerPos = new(rawEvent.NewValue);
				var newDisplacementLevel = LeftTriggerPos.DisplacementLevel;
				if (prevDisplacementLevel == AnalogDisplacementLevel.None && newDisplacementLevel != AnalogDisplacementLevel.None) {
					PushButtonEvent(RawGameControllerEventType.LeftTrigger, true);
				}
				else if (prevDisplacementLevel != AnalogDisplacementLevel.None && newDisplacementLevel == AnalogDisplacementLevel.None) {
					PushButtonEvent(RawGameControllerEventType.LeftTrigger, false);
				}
				break;
			}
			case RawGameControllerEventType.RightTrigger: {
				var prevDisplacementLevel = RightTriggerPos.DisplacementLevel;
				RightTriggerPos = new(rawEvent.NewValue);
				var newDisplacementLevel = RightTriggerPos.DisplacementLevel;
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