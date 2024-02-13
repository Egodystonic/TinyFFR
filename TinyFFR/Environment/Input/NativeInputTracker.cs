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
sealed class NativeInputTracker : IInputTracker, IGameControllerHandleImplProvider, IDisposable {
	const int InitialEventBufferLength = 50;
	static NativeInputTracker? _liveInstance = null;
	readonly UnmanagedBuffer<KeyboardOrMouseKeyEvent> _kbmEventBuffer = new(InitialEventBufferLength);
	readonly UnmanagedBuffer<RawGameControllerButtonEvent> _controllerEventBuffer = new(InitialEventBufferLength);
	readonly UnmanagedBuffer<MouseClickEvent> _clickEventBuffer = new(InitialEventBufferLength);
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _currentlyPressedKeys = new();
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _keyDownEventBuffer = new();
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _keyUpEventBuffer = new();
	readonly ArrayPoolBackedVector<GameController> _detectedControllers = new();
	readonly ArrayPoolBackedMap<GameControllerHandle, NativeGameControllerState> _controllerInputTrackers = new();
	readonly InputTrackerConfig _config;
	readonly GameController _amalgamatedController;
	readonly NativeGameControllerState _amalgamatedControllerState;
	bool _userQuitRequested = false;
	XYPair<int> _mouseCursorPos = default;
	int _kbmEventBufferCount = 0;
	int _clickEventBufferCount = 0;
	bool _isDisposed = false;

	public ReadOnlySpan<KeyboardOrMouseKeyEvent> NewKeyEvents => _kbmEventBuffer.AsSpan[.._kbmEventBufferCount];
	public ReadOnlySpan<KeyboardOrMouseKey> NewKeyDownEvents => _keyDownEventBuffer.AsSpan;
	public ReadOnlySpan<KeyboardOrMouseKey> NewKeyUpEvents => _keyUpEventBuffer.AsSpan;
	public ReadOnlySpan<KeyboardOrMouseKey> CurrentlyPressedKeys => _currentlyPressedKeys.AsSpan;
	public ReadOnlySpan<MouseClickEvent> NewMouseClicks => _clickEventBuffer.AsSpan[.._clickEventBufferCount];
	public bool UserQuitRequested => _userQuitRequested;
	public XYPair<int> MouseCursorPosition => _mouseCursorPos;
	public ReadOnlySpan<GameController> GameControllers => _detectedControllers.AsSpan;
	public GameController GetAmalgamatedGameController() => _amalgamatedController;

	public int MouseScrollWheelDelta {
		get {
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

	public unsafe NativeInputTracker(InputTrackerConfig config) {
		if (_liveInstance != null) throw new InvalidOperationException($"Only one {nameof(NativeInputTracker)} may be active at any time.");
		_liveInstance = this;
		_config = config;
		_amalgamatedController = new GameController(GameControllerHandle.Amalgamated, this);
		_amalgamatedControllerState = new NativeGameControllerState(config.MaxControllerNameLength);
		SetEventPollDelegates(
			&FilterKeycode,
			&ResizeCurrentPollInstanceKbmEventBuffer,
			&ResizeCurrentPollInstanceControllerEventBuffer,
			&ResizeCurrentPollInstanceClickEventBuffer,
			&HandlePotentialNewController
		);
		SetEventPollBufferPointers(
			_kbmEventBuffer.BufferPointer,
			_kbmEventBuffer.Length,
			_controllerEventBuffer.BufferPointer,
			_controllerEventBuffer.Length,
			_clickEventBuffer.BufferPointer,
			_clickEventBuffer.Length
		);
		DetectControllers();
	}

	public void ExecuteIteration() {
		ThrowIfThisIsDisposed();

		IterateEvents(
			out var numKbmEvents,
			out var numControllerEvents,
			out var numClickEvents,
			out var mousePosX,
			out var mousePosY,
			out var quitRequested
		).ThrowIfFailure();

		UpdateCurrentlyPressedKeys(numKbmEvents, numClickEvents);
		UpdateControllerStates(numControllerEvents);
		_mouseCursorPos = (mousePosX == Int32.MinValue ? _mouseCursorPos.X : mousePosX, mousePosY == Int32.MinValue ? _mouseCursorPos.Y : mousePosY);
		_userQuitRequested = quitRequested;
	}

	void UpdateCurrentlyPressedKeys(int newKbmEventCount, int newClickEventCount) {
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

	void UpdateControllerStates(int numNewEvents) {
		_amalgamatedControllerState.ClearForNextIteration();
		foreach (var kvp in _controllerInputTrackers) {
			kvp.Value.ClearForNextIteration();
		}

		for (var i = 0; i < numNewEvents; ++i) {
			var rawEvent = _controllerEventBuffer.AsSpan[i];
			_amalgamatedControllerState.ApplyEvent(rawEvent);
			var handle = rawEvent.Handle;
			if (!_controllerInputTrackers.TryGetValue(handle, out var state)) {
				state = new NativeGameControllerState(_config.MaxControllerNameLength);
				_controllerInputTrackers.Add(handle, state);
				_detectedControllers.Add(new(handle, this));
			}
			state.ApplyEvent(rawEvent);
		}
	}

	public bool KeyIsCurrentlyDown(KeyboardOrMouseKey key) {
		var curKeys = CurrentlyPressedKeys;
		for (var i = 0; i < curKeys.Length; ++i) {
			if (curKeys[i] == key) return true;
		}
		return false;
	}
	public bool KeyWasPressedThisIteration(KeyboardOrMouseKey key) {
		var newDownEvents = NewKeyDownEvents;
		for (var i = 0; i < newDownEvents.Length; ++i) {
			if (newDownEvents[i] == key) return true;
		}
		return false;
	}
	public bool KeyWasReleasedThisIteration(KeyboardOrMouseKey key) {
		var newUpEvents = NewKeyUpEvents;
		for (var i = 0; i < newUpEvents.Length; ++i) {
			if (newUpEvents[i] == key) return true;
		}
		return false;
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_event_poll_delegates")]
	static extern unsafe InteropResult SetEventPollDelegates(
		delegate* unmanaged<int, InteropBool> filterKeycapValueDelegate,
		delegate* unmanaged<KeyboardOrMouseKeyEvent*> doubleKbmEventBufferDelegate,
		delegate* unmanaged<RawGameControllerButtonEvent*> doubleControllerEventBufferDelegate,
		delegate* unmanaged<MouseClickEvent*> doubleClickEventBufferDelegate,
		delegate* unmanaged<GameControllerHandle, byte*, int, void> handleNewControllerDelegate
	);
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_event_poll_buffer_pointers")]
	static extern unsafe InteropResult SetEventPollBufferPointers(
		KeyboardOrMouseKeyEvent* kbmEventBufferPtr,
		int kbmEventBufferLen,
		RawGameControllerButtonEvent* controllerEventBufferPtr,
		int controllerEventBufferLen,
		MouseClickEvent* clickEventBufferPtr,
		int clickEventBufferLen
	);
	[UnmanagedCallersOnly]
	static unsafe KeyboardOrMouseKeyEvent* ResizeCurrentPollInstanceKbmEventBuffer() {
		if (_liveInstance == null || _liveInstance._isDisposed) throw new InvalidOperationException("Live instance was null or disposed.");
		_liveInstance._kbmEventBuffer.DoubleSize();
		return _liveInstance._kbmEventBuffer.BufferPointer;
	}
	[UnmanagedCallersOnly]
	static unsafe RawGameControllerButtonEvent* ResizeCurrentPollInstanceControllerEventBuffer() {
		if (_liveInstance == null || _liveInstance._isDisposed) throw new InvalidOperationException("Live instance was null or disposed.");
		_liveInstance._controllerEventBuffer.DoubleSize();
		return _liveInstance._controllerEventBuffer.BufferPointer;
	}
	[UnmanagedCallersOnly]
	static unsafe MouseClickEvent* ResizeCurrentPollInstanceClickEventBuffer() {
		if (_liveInstance == null || _liveInstance._isDisposed) throw new InvalidOperationException("Live instance was null or disposed.");
		_liveInstance._clickEventBuffer.DoubleSize();
		return _liveInstance._clickEventBuffer.BufferPointer;
	}
	[UnmanagedCallersOnly]
	static InteropBool FilterKeycode(int keycode) {
		return Enum.IsDefined((KeyboardOrMouseKey) keycode) && keycode < KeyboardOrMouseKeyExtensions.NonSdlKeyStartValue;
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "detect_controllers")]
	static extern InteropResult DetectControllers();
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "iterate_events")]
	static extern InteropResult IterateEvents(
		out int numKbmEventsWritten,
		out int numControllerEventsWritten,
		out int numClickEventsWritten,
		out int mousePosX,
		out int mousePosY,
		out InteropBool quitRequested
	);

	[UnmanagedCallersOnly]
	static unsafe void HandlePotentialNewController(GameControllerHandle handle, byte* utf8NamePtr, int utf8NameLen) {
		if (_liveInstance == null || _liveInstance._isDisposed) throw new InvalidOperationException("Live instance was null or disposed.");
		for (var i = 0; i < _liveInstance._detectedControllers.Count; ++i) {
			if (_liveInstance._detectedControllers[i].Handle == handle) return;
		}
		_liveInstance._detectedControllers.Add(new(handle, _liveInstance));
		var state = new NativeGameControllerState(_liveInstance._config.MaxControllerNameLength);
		var nameSpan = new ReadOnlySpan<byte>(utf8NamePtr, utf8NameLen);
		if (nameSpan.Length > state.NameBuffer.AsSpan.Length) nameSpan = nameSpan[..state.NameBuffer.AsSpan.Length];
		nameSpan.CopyTo(state.NameBuffer.AsSpan);
		_liveInstance._controllerInputTrackers.Add(handle, state);
	}

	NativeGameControllerState GetControllerState(GameControllerHandle handle) {
		if (handle == _amalgamatedController.Handle) return _amalgamatedControllerState;
		if (!_controllerInputTrackers.TryGetValue(handle, out var result)) throw new InvalidOperationException($"Unrecognized {nameof(GameControllerHandle)}.");
		return result;
	}

	public int GetName(GameControllerHandle handle, Span<char> dest) {
		ThrowIfThisIsDisposed();
		return GetControllerState(handle).NameBuffer.ReadTo(dest);
	}
	public int GetNameMaxLength() => _config.MaxControllerNameLength;
	public GameControllerStickPosition GetStickPosition(GameControllerHandle handle, bool leftStick) {
		ThrowIfThisIsDisposed();
		return leftStick ? GetControllerState(handle).LeftStickPos : GetControllerState(handle).RightStickPos;
	}
	public GameControllerTriggerPosition GetTriggerPosition(GameControllerHandle handle, bool leftTrigger) {
		ThrowIfThisIsDisposed();
		return leftTrigger ? GetControllerState(handle).LeftTriggerPos : GetControllerState(handle).RightTriggerPos;
	}
	public ReadOnlySpan<GameControllerButtonEvent> GetNewButtonEvents(GameControllerHandle handle) {
		ThrowIfThisIsDisposed();
		return GetControllerState(handle).NewButtonEvents.AsSpan;
	}
	public ReadOnlySpan<GameControllerButton> GetNewButtonDownEvents(GameControllerHandle handle) {
		ThrowIfThisIsDisposed();
		return GetControllerState(handle).NewButtonDownEvents.AsSpan;
	}
	public ReadOnlySpan<GameControllerButton> GetNewButtonUpEvents(GameControllerHandle handle) {
		ThrowIfThisIsDisposed();
		return GetControllerState(handle).NewButtonUpEvents.AsSpan;
	}
	public ReadOnlySpan<GameControllerButton> GetCurrentlyPressedButtons(GameControllerHandle handle) {
		ThrowIfThisIsDisposed();
		return GetControllerState(handle).CurrentlyPressedButtons.AsSpan;
	}
	public bool IsButtonDown(GameControllerHandle handle, GameControllerButton button) {
		var curButtons = GetCurrentlyPressedButtons(handle);
		for (var i = 0; i < curButtons.Length; ++i) {
			if (curButtons[i] == button) return true;
		}
		return false;
	}
	public bool WasButtonPressed(GameControllerHandle handle, GameControllerButton button) {
		var newDownButtons = GetNewButtonDownEvents(handle);
		for (var i = 0; i < newDownButtons.Length; ++i) {
			if (newDownButtons[i] == button) return true;
		}
		return false;
	}
	public bool WasButtonReleased(GameControllerHandle handle, GameControllerButton button) {
		var newUpButtons = GetNewButtonUpEvents(handle);
		for (var i = 0; i < newUpButtons.Length; ++i) {
			if (newUpButtons[i] == button) return true;
		}
		return false;
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_liveInstance = null;
			foreach (var kvp in _controllerInputTrackers) kvp.Value.Dispose();

			_kbmEventBuffer.Dispose();
			_keyDownEventBuffer.Dispose();
			_keyUpEventBuffer.Dispose();
			_controllerEventBuffer.Dispose();
			_clickEventBuffer.Dispose();
			_detectedControllers.Dispose();
			_currentlyPressedKeys.Dispose();
			_controllerInputTrackers.Dispose();
			_amalgamatedControllerState.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
}