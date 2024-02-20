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

// This class and NativeKeyboardAndInputState + NativeGameControllerState are all a bit overly-incestuous but it's fine for an MVP build
[SuppressUnmanagedCodeSecurity]
sealed class NativeInputTracker : IInputTracker, IDisposable {
	internal const int InitialEventBufferLength = 50;
	static NativeInputTracker? _liveInstance = null;
	readonly NativeKeyboardAndMouseInputState _kbmStateObject;
	readonly UnmanagedBuffer<RawGameControllerButtonEvent> _controllerEventBuffer = new(InitialEventBufferLength);
	readonly ArrayPoolBackedVector<IGameControllerInputTracker> _detectedControllerStateObjectVector = new();
	readonly ArrayPoolBackedMap<GameControllerHandle, NativeGameControllerState> _detectedControllerStateObjectMap = new();
	readonly NativeGameControllerState _combinedControllerState;
	bool _isDisposed = false;

	public bool UserQuitRequested { get; private set; } = false;
	public IKeyboardAndMouseInputTracker KeyboardAndMouse => _kbmStateObject;
	public ReadOnlySpan<IGameControllerInputTracker> GameControllers => _detectedControllerStateObjectVector.AsSpan;
	public IGameControllerInputTracker GameControllersCombined => _combinedControllerState;

	public unsafe NativeInputTracker() {
		if (_liveInstance != null) throw new InvalidOperationException($"Only one {nameof(NativeInputTracker)} may be active at any time.");
		_liveInstance = this;
		_kbmStateObject = new NativeKeyboardAndMouseInputState();
		_combinedControllerState = new NativeGameControllerState(GameControllerHandle.Combined);
		SetEventPollDelegates(
			&FilterAndTranslateKeycode,
			&ResizeCurrentPollInstanceKbmEventBuffer,
			&ResizeCurrentPollInstanceControllerEventBuffer,
			&ResizeCurrentPollInstanceClickEventBuffer,
			&HandlePotentialNewController
		).ThrowIfFailure();
		SetEventPollBufferPointers(
			_kbmStateObject.EventBuffer.BufferPointer,
			_kbmStateObject.EventBuffer.Length,
			_controllerEventBuffer.BufferPointer,
			_controllerEventBuffer.Length,
			_kbmStateObject.ClickBuffer.BufferPointer,
			_kbmStateObject.ClickBuffer.Length
		).ThrowIfFailure();
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
			out var mouseDeltaX,
			out var mouseDeltaY,
			out var quitRequested
		).ThrowIfFailure();

		UpdateControllerStates(numControllerEvents);
		_kbmStateObject.UpdateCurrentlyPressedKeys(numKbmEvents, numClickEvents);
		_kbmStateObject.MouseCursorPosition = (mousePosX == Int32.MinValue ? _kbmStateObject.MouseCursorPosition.X : mousePosX, mousePosY == Int32.MinValue ? _kbmStateObject.MouseCursorPosition.Y : mousePosY);
		_kbmStateObject.MouseCursorDelta = (mouseDeltaX, mouseDeltaY);
		UserQuitRequested = quitRequested;
	}

	void UpdateControllerStates(int numNewEvents) {
		_combinedControllerState.ClearForNextIteration();
		foreach (var kvp in _detectedControllerStateObjectMap) {
			kvp.Value.ClearForNextIteration();
		}

		for (var i = 0; i < numNewEvents; ++i) {
			var rawEvent = _controllerEventBuffer.AsSpan[i];
			_combinedControllerState.ApplyEvent(rawEvent);
			var handle = rawEvent.Handle;
			if (!_detectedControllerStateObjectMap.TryGetValue(handle, out var state)) continue;
			state.ApplyEvent(rawEvent);
		}
	}

	public override string ToString() => "TinyFFR Native Input Tracker";

	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_event_poll_delegates")]
	static extern unsafe InteropResult SetEventPollDelegates(
		delegate* unmanaged<int*, InteropBool> filterTranslateKeycapValueDelegate,
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
		_liveInstance._kbmStateObject.EventBuffer.DoubleSize();
		return _liveInstance._kbmStateObject.EventBuffer.BufferPointer;
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
		_liveInstance._kbmStateObject.ClickBuffer.DoubleSize();
		return _liveInstance._kbmStateObject.ClickBuffer.BufferPointer;
	}
	[UnmanagedCallersOnly]
	static unsafe InteropBool FilterAndTranslateKeycode(int* keycode) {
		*keycode |= (~*keycode & KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit) >> KeyboardOrMouseKeyExtensions.CharBasedValueBitDistanceToScancodeBit;
		return Enum.IsDefined((KeyboardOrMouseKey) (*keycode));
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
		out int mouseDeltaX,
		out int mouseDeltaY,
		out InteropBool quitRequested
	);

	[UnmanagedCallersOnly]
	static unsafe void HandlePotentialNewController(GameControllerHandle handle, byte* utf8NamePtr, int utf8NameLen) {
		if (_liveInstance == null || _liveInstance._isDisposed) throw new InvalidOperationException("Live instance was null or disposed.");
		foreach (var kvp in _liveInstance._detectedControllerStateObjectMap) {
			if (kvp.Value.Handle == handle) return;
		}

		var state = new NativeGameControllerState(handle);
		var nameSpan = new ReadOnlySpan<byte>(utf8NamePtr, utf8NameLen);
		if (nameSpan.Length > state.NameBuffer.AsSpan.Length) nameSpan = nameSpan[..state.NameBuffer.AsSpan.Length];
		nameSpan.CopyTo(state.NameBuffer.AsSpan);
		_liveInstance._detectedControllerStateObjectVector.Add(state);
		_liveInstance._detectedControllerStateObjectMap.Add(handle, state);
	}
	#endregion

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			_liveInstance = null;
			foreach (var kvp in _detectedControllerStateObjectMap) kvp.Value.Dispose();

			_controllerEventBuffer.Dispose();
			_detectedControllerStateObjectVector.Dispose();
			_detectedControllerStateObjectMap.Dispose();
			_combinedControllerState.Dispose();
			_kbmStateObject.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
	#endregion
}