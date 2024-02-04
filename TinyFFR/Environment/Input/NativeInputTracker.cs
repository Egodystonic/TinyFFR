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
sealed class NativeInputTracker : IInputTracker, IDisposable {
	const int InitialEventBufferLength = 50;
	static NativeInputTracker _nativePollInstance = null!;
	readonly UnmanagedBuffer<KeyboardOrMouseKeyEvent> _kbmEventBuffer = new(InitialEventBufferLength);
	readonly UnmanagedBuffer<RawGameControllerButtonEvent> _controllerEventBuffer = new(InitialEventBufferLength);
	readonly ArrayPoolBackedVector<KeyboardOrMouseKey> _currentlyPressedKeys = new();
	readonly ArrayPoolBackedVector<GameControllerId> _detectedControllerIds = new();
	readonly ArrayPoolBackedMap<GameControllerId?, NativeGameControllerInputTracker> _controllerInputTrackers = new();
	bool _userQuitRequested = false;
	XYPair _mouseCursorPos = default;
	int _kbmEventBufferCount = 0;
	bool _isDisposed = false;

	public ReadOnlySpan<KeyboardOrMouseKeyEvent> NewKeyEvents => _kbmEventBuffer.AsSpan[.._kbmEventBufferCount];
	public ReadOnlySpan<KeyboardOrMouseKey> CurrentlyPressedKeys => _currentlyPressedKeys.AsSpan;
	public bool UserQuitRequested => _userQuitRequested;
	public XYPair MouseCursorPosition => _mouseCursorPos;
	public ReadOnlySpan<GameControllerId> DetectedGameControllers => _detectedControllerIds.AsSpan;
	public IGameControllerInputTracker GetInputTrackerForGameController(GameControllerId? controllerId) => _controllerInputTrackers[controllerId];

	public NativeInputTracker() {
		_controllerInputTrackers[null] = new NativeGameControllerInputTracker();
	}

	public void ExecuteIteration() {
		ThrowIfThisIsDisposed();
		var (numKbmEvents, numControllerEvents) = InvokeNativeEventPolling(this);
		_kbmEventBufferCount = numKbmEvents;
		// TODO
		// Update currently pressed keys
		// Update controllers and amalgamated controller tracker, while checking for new controllers
		// Get new mouse position
		// Get user quit status
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "poll_events")]
	static extern unsafe InteropResult PollEvents(
		KeyboardOrMouseKeyEvent* kbmEventBufferPtr,
		int kbmEventBufferLen,
		RawGameControllerButtonEvent* controllerEventBufferPtr,
		int controllerEventBufferLen,
		delegate* unmanaged<KeyboardOrMouseKeyEvent*> doubleKbmEventBufferDelegate, // TODO can probably save two args by setting these once at init time
		delegate* unmanaged<RawGameControllerButtonEvent*> doubleControllerEventBufferDelegate,
		out int numKbmEventsWritten,
		out int numControllerEventsWritten
	);
	static unsafe (int NumKbmEvents, int NumControllerEvents) InvokeNativeEventPolling(NativeInputTracker @this) {
		_nativePollInstance = @this;
		
		PollEvents(
			@this._kbmEventBuffer.BufferPointer,
			@this._kbmEventBuffer.Length,
			@this._controllerEventBuffer.BufferPointer,
			@this._controllerEventBuffer.Length,
			&ResizeCurrentPollInstanceKbmEventBuffer,
			&ResizeCurrentPollInstanceControllerEventBuffer,
			out var numKbmEvents,
			out var numControllerEvents
		).ThrowIfFailure();

		return (numKbmEvents, numControllerEvents);
	}

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

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _controllerInputTrackers) kvp.Value.Dispose();

			_kbmEventBuffer.Dispose();
			_currentlyPressedKeys.Dispose();
			_controllerInputTrackers.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		if (_isDisposed) throw new InvalidOperationException("Tracker has been disposed.");
	}
}