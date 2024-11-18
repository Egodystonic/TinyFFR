// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Environment.Input.Local;

// This class and NativeKeyboardAndInputState + LocalGameControllerState are all a bit overly-incestuous but it's fine for an MVP build
[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalInputSnapshotProvider : IInputSnapshotProvider, IDisposable {
	internal const int InitialEventBufferLength = 50;
	static readonly UIntPtr CombinedGameControllerHandle = UIntPtr.Zero;
	readonly LocalKeyboardAndMouseInputSnapshotProvider _kbmSnapshot = new();
	readonly UnmanagedBuffer<RawLocalGameControllerButtonEvent> _controllerEventBuffer = new(InitialEventBufferLength);
	readonly ArrayPoolBackedVector<IGameControllerInputSnapshotProvider> _detectedControllerSnapshotVector = new();
	readonly ArrayPoolBackedMap<UIntPtr, LocalGameControllerState> _detectedControllerSnapshotMap = new();
	readonly LocalGameControllerState _combinedControllerState = new(CombinedGameControllerHandle);
	bool _isDisposed = false;

	public bool UserQuitRequested { get; private set; } = false;
	public IKeyboardAndMouseInputSnapshotProvider KeyboardAndMouse => _kbmSnapshot;
	public ReadOnlySpan<IGameControllerInputSnapshotProvider> GameControllers => _detectedControllerSnapshotVector.AsSpan;
	public IGameControllerInputSnapshotProvider GameControllersCombined => _combinedControllerState;

	public void Initialize() {
		DetectControllers();
	}

	public void IterateSystemWideInput() {
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
		_kbmSnapshot.UpdateCurrentlyPressedKeys(numKbmEvents, numClickEvents);
		_kbmSnapshot.MouseCursorPosition = (mousePosX == Int32.MinValue ? _kbmSnapshot.MouseCursorPosition.X : mousePosX, mousePosY == Int32.MinValue ? _kbmSnapshot.MouseCursorPosition.Y : mousePosY);
		_kbmSnapshot.MouseCursorDelta = (mouseDeltaX, mouseDeltaY);
		UserQuitRequested = quitRequested;
	}

	internal void GetEventBufferPointers(out KeyboardOrMouseKeyEvent* kbmEventBufferPtr, out int kbmEventBufferLen, out RawLocalGameControllerButtonEvent* controllerEventBufferPtr, out int controllerEventBufferLen, out MouseClickEvent* clickEventBufferPtr, out int clickEventBufferLen) {
		kbmEventBufferPtr = _kbmSnapshot.EventBuffer.BufferPointer;
		kbmEventBufferLen = _kbmSnapshot.EventBuffer.Length;
		controllerEventBufferPtr = _controllerEventBuffer.BufferPointer;
		controllerEventBufferLen = _controllerEventBuffer.Length;
		clickEventBufferPtr = _kbmSnapshot.ClickBuffer.BufferPointer;
		clickEventBufferLen = _kbmSnapshot.ClickBuffer.Length;
	}

	internal void HandlePotentialNewController(UIntPtr handle, byte* utf8NamePtr, int utf8NameLen) {
		foreach (var kvp in _detectedControllerSnapshotMap) {
			if (kvp.Value.Handle == handle) return;
		}

		var state = new LocalGameControllerState(handle);
		var nameSpan = new ReadOnlySpan<byte>(utf8NamePtr, utf8NameLen);
		if (nameSpan.Length > state.NameBuffer.AsSpan.Length) nameSpan = nameSpan[..state.NameBuffer.AsSpan.Length];
		nameSpan.CopyTo(state.NameBuffer.AsSpan);
		_detectedControllerSnapshotVector.Add(state);
		_detectedControllerSnapshotMap.Add(handle, state);
	}

	internal KeyboardOrMouseKeyEvent* DoubleKbmEventBufferSize() {
		_kbmSnapshot.EventBuffer.DoubleSize();
		return _kbmSnapshot.EventBuffer.BufferPointer;
	}

	internal RawLocalGameControllerButtonEvent* DoubleControllerEventBufferSize() {
		_controllerEventBuffer.DoubleSize();
		return _controllerEventBuffer.BufferPointer;
	}

	internal MouseClickEvent* DoubleClickEventBufferSize() {
		_kbmSnapshot.ClickBuffer.DoubleSize();
		return _kbmSnapshot.ClickBuffer.BufferPointer;
	}

	void UpdateControllerStates(int numNewEvents) {
		_combinedControllerState.ClearForNextIteration();
		foreach (var kvp in _detectedControllerSnapshotMap) {
			kvp.Value.ClearForNextIteration();
		}

		for (var i = 0; i < numNewEvents; ++i) {
			var rawEvent = _controllerEventBuffer.AsSpan[i];
			_combinedControllerState.ApplyEvent(rawEvent);
			var handle = rawEvent.Handle;
			if (!_detectedControllerSnapshotMap.TryGetValue(handle, out var state)) continue;
			state.ApplyEvent(rawEvent);
		}
	}

	public override string ToString() => "TinyFFR Native Input Snapshot Provider";

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "detect_controllers")]
	static extern InteropResult DetectControllers();
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "iterate_events")]
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
	#endregion

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _detectedControllerSnapshotMap) kvp.Value.Dispose();

			_controllerEventBuffer.Dispose();
			_detectedControllerSnapshotVector.Dispose();
			_detectedControllerSnapshotMap.Dispose();
			_combinedControllerState.Dispose();
			_kbmSnapshot.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IInputSnapshotProvider));
	}
	#endregion
}