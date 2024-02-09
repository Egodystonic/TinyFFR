// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Environment.Desktop;
using Egodystonic.TinyFFR.Environment.Loop;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Input;

[SuppressUnmanagedCodeSecurity]
sealed class NativeInputTracker : IInputTracker, IGameControllerHandleImplProvider, IDisposable {
	const int InitialEventBufferLength = 50;
	static NativeInputTracker _nativePollInstance = null!;
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
		_config = config;
		_amalgamatedController = new GameController(GameControllerHandle.Amalgamated, this);
		_amalgamatedControllerState = new NativeGameControllerState(config.MaxControllerNameLength);
		SetEventPollDelegates(
			&FilterKeycode,
			&ResizeCurrentPollInstanceKbmEventBuffer,
			&ResizeCurrentPollInstanceControllerEventBuffer
		);
	}

	public void ExecuteIteration() {
		ThrowIfThisIsDisposed();
		var (numKbmEvents, numControllerEvents) = InvokeNativeEventPolling(this);
		_kbmEventBufferCount = numKbmEvents;
		// TODO
		UpdateCurrentlyPressedKeys();
		// Update controllers and amalgamated controller tracker, while checking for new controllers
		// Get new mouse position
		// Get user quit status
	}

	void UpdateCurrentlyPressedKeys() {
		foreach (var kbmEvent in NewKeyEvents) {
			if (kbmEvent.KeyDown) {
				if (!_currentlyPressedKeys.Contains(kbmEvent.Key)) _currentlyPressedKeys.Add(kbmEvent.Key);
			}
			else {
				_currentlyPressedKeys.Remove(kbmEvent.Key);
			}
		}
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_event_poll_delegates")]
	static extern unsafe InteropResult SetEventPollDelegates(
		delegate* unmanaged<int, InteropBool> filterKeycapValueDelegate,
		delegate* unmanaged<KeyboardOrMouseKeyEvent*> doubleKbmEventBufferDelegate,
		delegate* unmanaged<RawGameControllerButtonEvent*> doubleControllerEventBufferDelegate
	);
	[UnmanagedCallersOnly]
	static unsafe KeyboardOrMouseKeyEvent* ResizeCurrentPollInstanceKbmEventBuffer() {
		_nativePollInstance._kbmEventBuffer.DoubleSize();
		return _nativePollInstance._kbmEventBuffer.BufferPointer;
	}
	[UnmanagedCallersOnly]
	static unsafe RawGameControllerButtonEvent* ResizeCurrentPollInstanceControllerEventBuffer() {
		_nativePollInstance._controllerEventBuffer.DoubleSize();
		return _nativePollInstance._controllerEventBuffer.BufferPointer;
	}
	[UnmanagedCallersOnly]
	static InteropBool FilterKeycode(int keycode) {
		return Enum.IsDefined((KeyboardOrMouseKey) keycode) && keycode < KeyboardOrMouseKeyExtensions.NonSdlKeyStartValue;
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "iterate_events")]
	static extern unsafe InteropResult IterateEvents(
		KeyboardOrMouseKeyEvent* kbmEventBufferPtr,
		int kbmEventBufferLen,
		RawGameControllerButtonEvent* controllerEventBufferPtr,
		int controllerEventBufferLen,
		out int numKbmEventsWritten,
		out int numControllerEventsWritten
	);
	static unsafe (int NumKbmEvents, int NumControllerEvents) InvokeNativeEventPolling(NativeInputTracker @this) {
		_nativePollInstance = @this;

		IterateEvents(
			@this._kbmEventBuffer.BufferPointer,
			@this._kbmEventBuffer.Length,
			@this._controllerEventBuffer.BufferPointer,
			@this._controllerEventBuffer.Length,
			out var numKbmEvents,
			out var numControllerEvents
		).ThrowIfFailure();

		return (numKbmEvents, numControllerEvents);
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

	public void Dispose() {
		if (_isDisposed) return;
		try {
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
		if (_isDisposed) throw new InvalidOperationException("Tracker has been disposed.");
	}
}