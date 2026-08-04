// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Input;

sealed unsafe class UiSourcedKeyboardAndMouseInputRetriever : ILatestKeyboardAndMouseInputRetriever, IDisposable {
	List<KeyboardOrMouseKeyEvent> _pendingKeyEvents = new();
	List<KeyboardOrMouseKeyEvent> _currentKeyEvents = new();
	List<MouseClickEvent> _pendingClickEvents = new();
	List<MouseClickEvent> _currentClickEvents = new();
	readonly List<KeyboardOrMouseKey> _keyDownEvents = new();
	readonly List<KeyboardOrMouseKey> _keyUpEvents = new();
	readonly List<KeyboardOrMouseKey> _currentlyPressedKeys = new();
	readonly HashSet<KeyboardOrMouseKey> _currentlyPressedKeysAccordingToFramework = new();
	readonly List<char> _pendingTextInput = new();
	readonly List<char> _currentTextInput = new();
	XYPair<int>? _cursorDeltaOrigin;
	XYPair<int>? _pendingCursorPosition;
	XYPair<int> _pendingCursorDelta;
	double _pendingScrollNotches;
	int _iterationVersion = 0;
	bool _isDisposed = false;

	public XYPair<int> MouseCursorPosition { get; private set; }
	public XYPair<int> MouseCursorDelta { get; private set; }

	public ReadOnlySpan<char> TranscribedText {
		get {
			ThrowIfThisIsDisposed();
			return CollectionsMarshal.AsSpan(_currentTextInput);
		}
	}

	public IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKeyEvent> NewKeyEvents => new(
		this, _iterationVersion, &GetNewKeyEventsCount, &GetIterationVersion, &GetNewKeyEvent
	);
	public IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> NewKeyDownEvents => new(
		this, _iterationVersion, &GetNewKeyDownEventsCount, &GetIterationVersion, &GetNewKeyDownEvent
	);
	public IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> NewKeyUpEvents => new(
		this, _iterationVersion, &GetNewKeyUpEventsCount, &GetIterationVersion, &GetNewKeyUpEvent
	);
	public IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey> CurrentlyPressedKeys => new(
		this, _iterationVersion, &GetCurrentlyPressedKeysCount, &GetIterationVersion, &GetCurrentlyPressedKey
	);
	public IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, MouseClickEvent> NewMouseClicks => new(
		this, _iterationVersion, &GetNewMouseClicksCount, &GetIterationVersion, &GetNewMouseClick
	);

	static UiSourcedKeyboardAndMouseInputRetriever CastWithDisposeCheck(ILatestKeyboardAndMouseInputRetriever input) {
		var result = ((UiSourcedKeyboardAndMouseInputRetriever) input);
		result.ThrowIfThisIsDisposed();
		return result;
	}

	static int GetNewKeyEventsCount(ILatestKeyboardAndMouseInputRetriever input) {
		return CastWithDisposeCheck(input)._currentKeyEvents.Count;
	}
	static KeyboardOrMouseKeyEvent GetNewKeyEvent(ILatestKeyboardAndMouseInputRetriever input, int index) {
		return CastWithDisposeCheck(input)._currentKeyEvents[index];
	}
	static int GetNewKeyDownEventsCount(ILatestKeyboardAndMouseInputRetriever input) {
		return CastWithDisposeCheck(input)._keyDownEvents.Count;
	}
	static KeyboardOrMouseKey GetNewKeyDownEvent(ILatestKeyboardAndMouseInputRetriever input, int index) {
		return CastWithDisposeCheck(input)._keyDownEvents[index];
	}
	static int GetNewKeyUpEventsCount(ILatestKeyboardAndMouseInputRetriever input) {
		return CastWithDisposeCheck(input)._keyUpEvents.Count;
	}
	static KeyboardOrMouseKey GetNewKeyUpEvent(ILatestKeyboardAndMouseInputRetriever input, int index) {
		return CastWithDisposeCheck(input)._keyUpEvents[index];
	}
	static int GetCurrentlyPressedKeysCount(ILatestKeyboardAndMouseInputRetriever input) {
		return CastWithDisposeCheck(input)._currentlyPressedKeys.Count;
	}
	static KeyboardOrMouseKey GetCurrentlyPressedKey(ILatestKeyboardAndMouseInputRetriever input, int index) {
		return CastWithDisposeCheck(input)._currentlyPressedKeys[index];
	}
	static int GetNewMouseClicksCount(ILatestKeyboardAndMouseInputRetriever input) {
		return CastWithDisposeCheck(input)._currentClickEvents.Count;
	}
	static MouseClickEvent GetNewMouseClick(ILatestKeyboardAndMouseInputRetriever input, int index) {
		return CastWithDisposeCheck(input)._currentClickEvents[index];
	}

	public int MouseScrollWheelDelta {
		get {
			ThrowIfThisIsDisposed();
			var result = 0;
			for (var i = 0; i < _keyDownEvents.Count; ++i) {
				result += _keyDownEvents[i] switch {
					KeyboardOrMouseKey.MouseWheelDown => 1,
					KeyboardOrMouseKey.MouseWheelUp => -1,
					_ => 0
				};
			}
			return result;
		}
	}

	public bool KeyIsCurrentlyDown(KeyboardOrMouseKey key) {
		ThrowIfThisIsDisposed();
		return _currentlyPressedKeys.Contains(key);
	}
	public bool KeyWasPressedThisIteration(KeyboardOrMouseKey key) {
		ThrowIfThisIsDisposed();
		return _keyDownEvents.Contains(key);
	}
	public bool KeyWasReleasedThisIteration(KeyboardOrMouseKey key) {
		ThrowIfThisIsDisposed();
		return _keyUpEvents.Contains(key);
	}

	#region Recording (invoked by the host framework's event handlers)
	public void RecordKeyDown(KeyboardOrMouseKey key) {
		ThrowIfThisIsDisposed();
		if (key == KeyboardOrMouseKey.Unknown) return;
		if (!_currentlyPressedKeysAccordingToFramework.Add(key)) return; // Filters OS key-repeat, matching the native event poll's behaviour
		_pendingKeyEvents.Add(new(key, keyDown: true));
	}

	public void RecordKeyUp(KeyboardOrMouseKey key) {
		ThrowIfThisIsDisposed();
		if (key == KeyboardOrMouseKey.Unknown) return;
		if (!_currentlyPressedKeysAccordingToFramework.Remove(key)) return; // Never report a release for something we never saw pressed
		_pendingKeyEvents.Add(new(key, keyDown: false));
	}

	public void RecordClick(MouseKey key, XYPair<int> location, int consecutiveClickCount) {
		ThrowIfThisIsDisposed();
		_pendingClickEvents.Add(new(location, key, consecutiveClickCount));
	}

	public void RecordCursorPosition(XYPair<int> elementRelativePosition) {
		ThrowIfThisIsDisposed();
		if (_cursorDeltaOrigin is { } origin) _pendingCursorDelta += elementRelativePosition - origin;
		_cursorDeltaOrigin = elementRelativePosition;
		_pendingCursorPosition = elementRelativePosition;
	}

	public void RecordScroll(double notchDelta) {
		ThrowIfThisIsDisposed();
		_pendingScrollNotches += notchDelta;
		while (_pendingScrollNotches >= 1d) {
			_pendingScrollNotches -= 1d;
			RecordWheelNotch(KeyboardOrMouseKey.MouseWheelDown);
		}
		while (_pendingScrollNotches <= -1d) {
			_pendingScrollNotches += 1d;
			RecordWheelNotch(KeyboardOrMouseKey.MouseWheelUp);
		}
	}

	void RecordWheelNotch(KeyboardOrMouseKey key) {
		_pendingKeyEvents.Add(new(key, keyDown: true));
		_pendingKeyEvents.Add(new(key, keyDown: false));
	}

	public void ReleaseAllHeldKeyboardKeys() => ReleaseHeldKeys(releaseMouseKeys: false);
	public void ReleaseAllHeldMouseButtons() => ReleaseHeldKeys(releaseMouseKeys: true);

	void ReleaseHeldKeys(bool releaseMouseKeys) {
		ThrowIfThisIsDisposed();
		foreach (var key in _currentlyPressedKeysAccordingToFramework.ToArray()) {
			if ((key.GetCategory() == KeyboardOrMouseKeyCategory.Mouse) != releaseMouseKeys) continue;
			RecordKeyUp(key);
		}
	}

	public void ResetCursorDeltaOrigin() {
		ThrowIfThisIsDisposed();
		_cursorDeltaOrigin = null;
	}

	public void RecordTextInput(ReadOnlySpan<char> text) {
		ThrowIfThisIsDisposed();
		foreach (var c in text) _pendingTextInput.Add(c);
	}
	#endregion

	public void Iterate() {
		ThrowIfThisIsDisposed();

		(_currentKeyEvents, _pendingKeyEvents) = (_pendingKeyEvents, _currentKeyEvents);
		_pendingKeyEvents.Clear();
		(_currentClickEvents, _pendingClickEvents) = (_pendingClickEvents, _currentClickEvents);
		_pendingClickEvents.Clear();
		_currentTextInput.Clear();
		_currentTextInput.AddRange(_pendingTextInput);
		_pendingTextInput.Clear();

		_keyDownEvents.Clear();
		_keyUpEvents.Clear();
		foreach (var kbmEvent in _currentKeyEvents) {
			if (kbmEvent.KeyDown) {
				if (!_currentlyPressedKeys.Contains(kbmEvent.Key)) _currentlyPressedKeys.Add(kbmEvent.Key);
				_keyDownEvents.Add(kbmEvent.Key);
			}
			else {
				_currentlyPressedKeys.Remove(kbmEvent.Key);
				_keyUpEvents.Add(kbmEvent.Key);
			}
		}

		if (_pendingCursorPosition is { } newCursorPosition) {
			MouseCursorPosition = newCursorPosition;
			_pendingCursorPosition = null;
		}
		MouseCursorDelta = _pendingCursorDelta;
		_pendingCursorDelta = XYPair<int>.Zero;

		_iterationVersion++;
	}
	static int GetIterationVersion(ILatestKeyboardAndMouseInputRetriever input) => ((UiSourcedKeyboardAndMouseInputRetriever) input)._iterationVersion;

	public override string ToString() => _isDisposed ? "TinyFFR UI Input State Provider [Keyboard/Mouse] [Disposed]" : "TinyFFR UI Input State Provider [Keyboard/Mouse]";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			_pendingKeyEvents.Clear();
			_currentKeyEvents.Clear();
			_pendingClickEvents.Clear();
			_currentClickEvents.Clear();
			_keyDownEvents.Clear();
			_keyUpEvents.Clear();
			_currentlyPressedKeys.Clear();
			_currentlyPressedKeysAccordingToFramework.Clear();
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
