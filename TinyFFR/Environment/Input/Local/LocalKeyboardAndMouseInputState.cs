// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Environment.Input.Local;

sealed class LocalKeyboardAndMouseInputState : IKeyboardAndMouseInputTracker, IDisposable {
	public readonly UnmanagedBuffer<KeyboardOrMouseKeyEvent> EventBuffer = new(LocalInputTracker.InitialEventBufferLength);
	public readonly UnmanagedBuffer<MouseClickEvent> ClickBuffer = new(LocalInputTracker.InitialEventBufferLength);
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _currentlyPressedKeys = new();
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _keyDownEventBuffer = new();
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _keyUpEventBuffer = new();
	int _kbmEventBufferCount = 0;
	int _clickEventBufferCount = 0;
	bool _isDisposed = false;

	public ReadOnlySpan<KeyboardOrMouseKeyEvent> NewKeyEvents => EventBuffer.AsSpan[.._kbmEventBufferCount];
	public ReadOnlySpan<KeyboardOrMouseKey> NewKeyDownEvents => _keyDownEventBuffer.AsSpan;
	public ReadOnlySpan<KeyboardOrMouseKey> NewKeyUpEvents => _keyUpEventBuffer.AsSpan;
	public ReadOnlySpan<KeyboardOrMouseKey> CurrentlyPressedKeys => _currentlyPressedKeys.AsSpan;
	public ReadOnlySpan<MouseClickEvent> NewMouseClicks => ClickBuffer.AsSpan[.._clickEventBufferCount];
	public XYPair<int> MouseCursorPosition { get; internal set; }
	public XYPair<int> MouseCursorDelta { get; internal set; }

	public int MouseScrollWheelDelta {
		get {
			ThrowIfThisIsDisposed();
			var result = 0;
			var newEvents = NewKeyDownEvents;
			for (var i = 0; i < newEvents.Length; ++i) {
				result += newEvents[i] switch {
					KeyboardOrMouseKey.MouseWheelDown => 1,
					KeyboardOrMouseKey.MouseWheelUp => -1,
					_ => 0
				};
			}
			return result;
		}
	}

	public void UpdateCurrentlyPressedKeys(int newKbmEventCount, int newClickEventCount) {
		ThrowIfThisIsDisposed();
		_kbmEventBufferCount = newKbmEventCount;
		_clickEventBufferCount = newClickEventCount;
		_keyDownEventBuffer.ClearWithoutZeroingMemory();
		_keyUpEventBuffer.ClearWithoutZeroingMemory();
		foreach (var kbmEvent in NewKeyEvents) {
			if (kbmEvent.KeyDown) {
				if (!_currentlyPressedKeys.Contains(kbmEvent.Key)) _currentlyPressedKeys.Add(kbmEvent.Key);
				_keyDownEventBuffer.Add(kbmEvent.Key);
			}
			else {
				_currentlyPressedKeys.Remove(kbmEvent.Key);
				_keyUpEventBuffer.Add(kbmEvent.Key);
			}
		}
	}

	public bool KeyIsCurrentlyDown(KeyboardOrMouseKey key) {
		ThrowIfThisIsDisposed();
		var curKeys = CurrentlyPressedKeys;
		for (var i = 0; i < curKeys.Length; ++i) {
			if (curKeys[i] == key) return true;
		}
		return false;
	}
	public bool KeyWasPressedThisIteration(KeyboardOrMouseKey key) {
		ThrowIfThisIsDisposed();
		var newDownEvents = NewKeyDownEvents;
		for (var i = 0; i < newDownEvents.Length; ++i) {
			if (newDownEvents[i] == key) return true;
		}
		return false;
	}
	public bool KeyWasReleasedThisIteration(KeyboardOrMouseKey key) {
		ThrowIfThisIsDisposed();
		var newUpEvents = NewKeyUpEvents;
		for (var i = 0; i < newUpEvents.Length; ++i) {
			if (newUpEvents[i] == key) return true;
		}
		return false;
	}

	public override string ToString() => _isDisposed ? "TinyFFR Native Input Tracker [Keyboard/Mouse] [Disposed]" : "TinyFFR Native Input Tracker [Keyboard/Mouse]";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			EventBuffer.Dispose();
			_keyDownEventBuffer.Dispose();
			_keyUpEventBuffer.Dispose();
			ClickBuffer.Dispose();
			_currentlyPressedKeys.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IKeyboardAndMouseInputTracker));
	}
	#endregion
}