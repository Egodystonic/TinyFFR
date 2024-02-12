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
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _currentlyPressedKeys = new();
	readonly ArrayPoolBackedVector<GameController> _detectedControllers = new();
	readonly ArrayPoolBackedMap<GameControllerHandle, NativeGameControllerState> _controllerInputTrackers = new();
	readonly InputTrackerConfig _config;
	readonly GameController _amalgamatedController;
	readonly NativeGameControllerState _amalgamatedControllerState;
	bool _userQuitRequested = false;
	XYPair _mouseCursorPos = default;
	int _kbmEventBufferCount = 0;
	bool _isDisposed = false;

	public ReadOnlySpan<KeyboardOrMouseKeyEvent> NewKeyEvents => _kbmEventBuffer.AsSpan[.._kbmEventBufferCount];
	public ReadOnlySpan<KeyboardOrMouseKey> CurrentlyPressedKeys => _currentlyPressedKeys.AsSpan;
	public bool UserQuitRequested => _userQuitRequested;
	public XYPair MouseCursorPosition => _mouseCursorPos;
	public ReadOnlySpan<GameController> GameControllers => _detectedControllers.AsSpan;
	public GameController GetAmalgamatedGameController() => _amalgamatedController;

	public unsafe NativeInputTracker(InputTrackerConfig config) {
		if (_liveInstance != null) throw new InvalidOperationException($"Only one {nameof(NativeInputTracker)} may be active at any time.");
		_liveInstance = this;
		_config = config;
		_amalgamatedController = new GameController(GameControllerHandle.Amalgamated, this);
		_amalgamatedControllerState = new NativeGameControllerState(config.MaxControllerNameLength);
		SetEventPollDelegates(
			&FilterKeycode,
			&ResizeCurrentPollInstanceKbmEventBuffer,
			&ResizeCurrentPollInstanceControllerEventBuffer
		);
		SetEventPollBufferPointers(
			_kbmEventBuffer.BufferPointer,
			_kbmEventBuffer.Length,
			_controllerEventBuffer.BufferPointer,
			_controllerEventBuffer.Length
		);
	}

	public void ExecuteIteration() {
		ThrowIfThisIsDisposed();

		IterateEvents(
			out var numKbmEvents,
			out var numControllerEvents,
			out var mousePosX,
			out var mousePosY,
			out var quitRequested
		).ThrowIfFailure();

		// TODO mouse click events

		UpdateCurrentlyPressedKeys(numKbmEvents);
		UpdateControllerStates(numControllerEvents);
		_mouseCursorPos = (mousePosX, mousePosY);
		_userQuitRequested = quitRequested;
	}

	void UpdateCurrentlyPressedKeys(int numNewEvents) {
		_kbmEventBufferCount = numNewEvents;
		foreach (var kbmEvent in NewKeyEvents) {
			if (kbmEvent.KeyDown) {
				if (!_currentlyPressedKeys.Contains(kbmEvent.Key)) _currentlyPressedKeys.Add(kbmEvent.Key);
			}
			else {
				_currentlyPressedKeys.Remove(kbmEvent.Key);
			}
		}
	}

	void UpdateControllerStates(int numNewEvents) {
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

	public bool IsKeyDown(KeyboardOrMouseKey key) {
		var curKeys = CurrentlyPressedKeys;
		for (var i = 0; i < curKeys.Length; ++i) {
			if (curKeys[i] == key) return true;
		}
		return false;
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_event_poll_delegates")]
	static extern unsafe InteropResult SetEventPollDelegates(
		delegate* unmanaged<int, InteropBool> filterKeycapValueDelegate,
		delegate* unmanaged<KeyboardOrMouseKeyEvent*> doubleKbmEventBufferDelegate,
		delegate* unmanaged<RawGameControllerButtonEvent*> doubleControllerEventBufferDelegate
	);
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_event_poll_buffer_pointers")]
	static extern unsafe InteropResult SetEventPollBufferPointers(
		KeyboardOrMouseKeyEvent* kbmEventBufferPtr,
		int kbmEventBufferLen,
		RawGameControllerButtonEvent* controllerEventBufferPtr,
		int controllerEventBufferLen
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
	static InteropBool FilterKeycode(int keycode) {
		return Enum.IsDefined((KeyboardOrMouseKey) keycode) && keycode < KeyboardOrMouseKeyExtensions.NonSdlKeyStartValue;
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "iterate_events")]
	static extern InteropResult IterateEvents(
		out int numKbmEventsWritten,
		out int numControllerEventsWritten,
		out float mousePosX,
		out float mousePosY,
		out InteropBool quitRequested
	);

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
	public bool IsConnected(GameControllerHandle handle) {
		ThrowIfThisIsDisposed();
		return GetControllerState(handle).IsConnected;
	}
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

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_liveInstance = null;
			foreach (var kvp in _controllerInputTrackers) kvp.Value.Dispose();

			_kbmEventBuffer.Dispose();
			_controllerEventBuffer.Dispose();
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