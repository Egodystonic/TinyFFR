// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Input.Local;

sealed unsafe class LocalLatestKeyboardAndMouseInputRetriever : ILatestKeyboardAndMouseInputRetriever, IDisposable {
	public readonly UnmanagedBuffer<KeyboardOrMouseKeyEvent> EventBuffer = new(LocalLatestInputRetriever.InitialEventBufferLength);
	public readonly UnmanagedBuffer<MouseClickEvent> ClickBuffer = new(LocalLatestInputRetriever.InitialEventBufferLength);
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _currentlyPressedKeys = new();
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _keyDownEventBuffer = new();
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _keyUpEventBuffer = new();
	int _iterationVersion = 0;
	int _kbmEventBufferCount = 0;
	int _clickEventBufferCount = 0;
	bool _isDisposed = false;

	public XYPair<int> MouseCursorPosition { get; internal set; }
	public XYPair<int> MouseCursorDelta { get; internal set; }

	public TypedReferentIterator<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKeyEvent> NewKeyEvents => new(
		this, _iterationVersion, &GetNewKeyEventsSpanLength, &GetIterationVersion, &GetNewKeyEvent
	);
	public TypedReferentIterator<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> NewKeyDownEvents => new(
		this, _iterationVersion, &GetNewKeyDownEventsSpanLength, &GetIterationVersion, &GetNewKeyDownEvent
	);
	public TypedReferentIterator<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> NewKeyUpEvents => new(
		this, _iterationVersion, &GetNewKeyUpEventsSpanLength, &GetIterationVersion, &GetNewKeyUpEvent
	);
	public TypedReferentIterator<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> CurrentlyPressedKeys => new(
		this, _iterationVersion, &GetCurrentlyPressedKeysSpanLength, &GetIterationVersion, &GetCurrentlyPressedKey
	);
	public TypedReferentIterator<ILatestKeyboardAndMouseInputRetriever, MouseClickEvent> NewMouseClicks => new(
		this, _iterationVersion, &GetNewMouseClicksSpanLength, &GetIterationVersion, &GetNewMouseClick
	);

	ReadOnlySpan<KeyboardOrMouseKeyEvent> NewKeyEventsSpan => EventBuffer.AsSpan[.._kbmEventBufferCount];
	ReadOnlySpan<KeyboardOrMouseKey> NewKeyDownEventsSpan => _keyDownEventBuffer.AsSpan;
	ReadOnlySpan<KeyboardOrMouseKey> NewKeyUpEventsSpan => _keyUpEventBuffer.AsSpan;
	ReadOnlySpan<KeyboardOrMouseKey> CurrentlyPressedKeysSpan => _currentlyPressedKeys.AsSpan;
	ReadOnlySpan<MouseClickEvent> NewMouseClicksSpan => ClickBuffer.AsSpan[.._clickEventBufferCount];

	static LocalLatestKeyboardAndMouseInputRetriever CastWithDisposeCheck(ILatestKeyboardAndMouseInputRetriever input) {
		var result = ((LocalLatestKeyboardAndMouseInputRetriever) input);
		result.ThrowIfThisIsDisposed();
		return result;
	}

	static int GetNewKeyEventsSpanLength(ILatestKeyboardAndMouseInputRetriever input) {
		return CastWithDisposeCheck(input).NewKeyEventsSpan.Length;
	}
	static KeyboardOrMouseKeyEvent GetNewKeyEvent(ILatestKeyboardAndMouseInputRetriever input, int index) {
		return CastWithDisposeCheck(input).NewKeyEventsSpan[index];
	}
	static int GetNewKeyDownEventsSpanLength(ILatestKeyboardAndMouseInputRetriever input) {
		return CastWithDisposeCheck(input).NewKeyDownEventsSpan.Length;
	}
	static KeyboardOrMouseKey GetNewKeyDownEvent(ILatestKeyboardAndMouseInputRetriever input, int index) {
		return CastWithDisposeCheck(input).NewKeyDownEventsSpan[index];
	}
	static int GetNewKeyUpEventsSpanLength(ILatestKeyboardAndMouseInputRetriever input) {
		return CastWithDisposeCheck(input).NewKeyUpEventsSpan.Length;
	}
	static KeyboardOrMouseKey GetNewKeyUpEvent(ILatestKeyboardAndMouseInputRetriever input, int index) {
		return CastWithDisposeCheck(input).NewKeyUpEventsSpan[index];
	}
	static int GetCurrentlyPressedKeysSpanLength(ILatestKeyboardAndMouseInputRetriever input) {
		return CastWithDisposeCheck(input).CurrentlyPressedKeysSpan.Length;
	}
	static KeyboardOrMouseKey GetCurrentlyPressedKey(ILatestKeyboardAndMouseInputRetriever input, int index) {
		return CastWithDisposeCheck(input).CurrentlyPressedKeysSpan[index];
	}
	static int GetNewMouseClicksSpanLength(ILatestKeyboardAndMouseInputRetriever input) {
		return CastWithDisposeCheck(input).NewMouseClicksSpan.Length;
	}
	static MouseClickEvent GetNewMouseClick(ILatestKeyboardAndMouseInputRetriever input, int index) {
		return CastWithDisposeCheck(input).NewMouseClicksSpan[index];
	}

	public int MouseScrollWheelDelta {
		get {
			ThrowIfThisIsDisposed();
			var result = 0;
			var newEvents = NewKeyDownEventsSpan;
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
		foreach (var kbmEvent in NewKeyEventsSpan) {
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
		var curKeys = CurrentlyPressedKeysSpan;
		for (var i = 0; i < curKeys.Length; ++i) {
			if (curKeys[i] == key) return true;
		}
		return false;
	}
	public bool KeyWasPressedThisIteration(KeyboardOrMouseKey key) {
		ThrowIfThisIsDisposed();
		var newDownEvents = NewKeyDownEventsSpan;
		for (var i = 0; i < newDownEvents.Length; ++i) {
			if (newDownEvents[i] == key) return true;
		}
		return false;
	}
	public bool KeyWasReleasedThisIteration(KeyboardOrMouseKey key) {
		ThrowIfThisIsDisposed();
		var newUpEvents = NewKeyUpEventsSpan;
		for (var i = 0; i < newUpEvents.Length; ++i) {
			if (newUpEvents[i] == key) return true;
		}
		return false;
	}

	public void Iterate() {
		_iterationVersion++;
	}
	static int GetIterationVersion(ILatestKeyboardAndMouseInputRetriever input) => ((LocalLatestKeyboardAndMouseInputRetriever) input)._iterationVersion;

	public override string ToString() => _isDisposed ? "TinyFFR Local Input State Provider [Keyboard/Mouse] [Disposed]" : "TinyFFR Local Input State Provider [Keyboard/Mouse]";

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
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(ILatestKeyboardAndMouseInputRetriever));
	}
	#endregion
}