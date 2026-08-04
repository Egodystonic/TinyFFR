// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Input.Local;

// This class and NativeKeyboardAndInputState + LocalLatestGameControllerState are all a bit overly-incestuous but it's fine for an MVP build
[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalLatestInputRetriever : ILatestInputRetriever, IDisposable {
	internal const int InitialEventBufferLength = 50;
	const string CombinedGameControllerName = "<Combined>";
	static readonly UIntPtr CombinedGameControllerHandle = UIntPtr.Zero;
	readonly LocalLatestKeyboardAndMouseInputRetriever _kbmState = new();
	readonly UnmanagedBuffer<RawLocalGameControllerButtonEvent> _controllerEventBuffer = new(InitialEventBufferLength);
	readonly ArrayPoolBackedVector<ILatestGameControllerInputRetriever> _detectedControllerStateVector = new();
	readonly ArrayPoolBackedMap<UIntPtr, LocalLatestGameControllerState> _detectedControllerStateMap = new();
	readonly LocalLatestGameControllerState _combinedControllerState;
	bool _isDisposed = false;
	int _iterationVersion = 0;

	public bool UserQuitRequested { get; private set; } = false;
	public ILatestKeyboardAndMouseInputRetriever KeyboardAndMouse => _kbmState;
	public IndirectEnumerable<ILatestInputRetriever, ILatestGameControllerInputRetriever> GameControllers => new(
		this, _iterationVersion, &GetGameControllersCount, &GetIterationVersion, &GetGameController
	);
	public ILatestGameControllerInputRetriever GameControllersCombined => _combinedControllerState;

	public LocalLatestInputRetriever() {
		_combinedControllerState = new(CombinedGameControllerHandle);
		_combinedControllerState.NameBuffer.ConvertFromUtf16(CombinedGameControllerName);
	}

	static int GetGameControllersCount(ILatestInputRetriever input) {
		var castInput = ((LocalLatestInputRetriever) input);
		castInput.ThrowIfThisIsDisposed();
		return castInput._detectedControllerStateVector.Count;
	}
	static ILatestGameControllerInputRetriever GetGameController(ILatestInputRetriever input, int index) {
		var castInput = ((LocalLatestInputRetriever) input);
		castInput.ThrowIfThisIsDisposed();
		return castInput._detectedControllerStateVector[index];
	}

	public void Initialize() {
		DetectControllers();
	}

	public void IterateSystemWideInput(bool transcribeTextInput) {
		ThrowIfThisIsDisposed();

		IterateEvents(
			out var numKbmEvents,
			out var numControllerEvents,
			out var numClickEvents,
			out var numTextInputBytes,
			out var mousePosX,
			out var mousePosY,
			out var mouseDeltaX,
			out var mouseDeltaY,
			out var quitRequested
		).ThrowIfFailure();

		UpdateControllerStates(numControllerEvents);
		_kbmState.UpdateCurrentlyPressedKeys(numKbmEvents, numClickEvents);
		_kbmState.UpdateTextInput(numTextInputBytes);
		_kbmState.MouseCursorPosition = (mousePosX == Int32.MinValue ? _kbmState.MouseCursorPosition.X : mousePosX, mousePosY == Int32.MinValue ? _kbmState.MouseCursorPosition.Y : mousePosY);
		_kbmState.MouseCursorDelta = (mouseDeltaX, mouseDeltaY);
		UserQuitRequested = quitRequested;

		_iterationVersion++;
		_kbmState.Iterate();
		_combinedControllerState.Iterate();
		foreach (var controller in _detectedControllerStateMap.Values) controller.Iterate();
	}

	internal void GetEventBufferPointers(out KeyboardOrMouseKeyEvent* kbmEventBufferPtr, out int kbmEventBufferLen, out RawLocalGameControllerButtonEvent* controllerEventBufferPtr, out int controllerEventBufferLen, out MouseClickEvent* clickEventBufferPtr, out int clickEventBufferLen, out byte* textInputBufferPtr, out int textInputBufferLen) {
		kbmEventBufferPtr = _kbmState.EventBuffer.BufferPointer;
		kbmEventBufferLen = _kbmState.EventBuffer.Length;
		controllerEventBufferPtr = _controllerEventBuffer.BufferPointer;
		controllerEventBufferLen = _controllerEventBuffer.Length;
		clickEventBufferPtr = _kbmState.ClickBuffer.BufferPointer;
		clickEventBufferLen = _kbmState.ClickBuffer.Length;
		textInputBufferPtr = _kbmState.TextInputBuffer.AsPointer;
		textInputBufferLen = _kbmState.TextInputBuffer.Length;
	}

	internal void HandlePotentialNewController(UIntPtr handle, byte* utf8NamePtr, int utf8NameLen) {
		foreach (var kvp in _detectedControllerStateMap) {
			if (kvp.Value.Handle == handle) return;
		}

		var state = new LocalLatestGameControllerState(handle);
		var nameSpan = new ReadOnlySpan<byte>(utf8NamePtr, utf8NameLen);
		if (nameSpan.Length > state.NameBuffer.AsSpan.Length) nameSpan = nameSpan[..state.NameBuffer.AsSpan.Length];
		nameSpan.CopyTo(state.NameBuffer.AsSpan);
		_detectedControllerStateVector.Add(state);
		_detectedControllerStateMap.Add(handle, state);
	}

	internal KeyboardOrMouseKeyEvent* DoubleKbmEventBufferSize() {
		_kbmState.EventBuffer.DoubleSize();
		return _kbmState.EventBuffer.BufferPointer;
	}

	internal RawLocalGameControllerButtonEvent* DoubleControllerEventBufferSize() {
		_controllerEventBuffer.DoubleSize();
		return _controllerEventBuffer.BufferPointer;
	}

	internal MouseClickEvent* DoubleClickEventBufferSize() {
		_kbmState.ClickBuffer.DoubleSize();
		return _kbmState.ClickBuffer.BufferPointer;
	}

	internal byte* DoubleTextInputBufferSize() => _kbmState.DoubleTextInputBufferSize();

	void UpdateControllerStates(int numNewEvents) {
		_combinedControllerState.ClearForNextIteration();
		foreach (var kvp in _detectedControllerStateMap) {
			kvp.Value.ClearForNextIteration();
		}

		for (var i = 0; i < numNewEvents; ++i) {
			var rawEvent = _controllerEventBuffer.AsSpan[i];
			_combinedControllerState.ApplyEvent(rawEvent);
			var handle = rawEvent.Handle;
			if (!_detectedControllerStateMap.TryGetValue(handle, out var state)) continue;
			state.ApplyEvent(rawEvent);
		}
	}

	static int GetIterationVersion(ILatestInputRetriever input) => ((LocalLatestInputRetriever) input)._iterationVersion;

	public override string ToString() => $"TinyFFR Local Input State Provider{(_isDisposed ? " [Disposed]" : "")}";

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "detect_controllers")]
	static extern InteropResult DetectControllers();
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "iterate_events")]
	static extern InteropResult IterateEvents(
		out int numKbmEventsWritten,
		out int numControllerEventsWritten,
		out int numClickEventsWritten,
		out int numTextInputBytesWritten,
		out int mousePosX,
		out int mousePosY,
		out int mouseDeltaX,
		out int mouseDeltaY,
		out InteropBool quitRequested
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_text_input_enabled")]
	static extern InteropResult SetTextInputEnabled(InteropBool enabled);
	#endregion

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _detectedControllerStateMap) kvp.Value.Dispose();

			_controllerEventBuffer.Dispose();
			_detectedControllerStateVector.Dispose();
			_detectedControllerStateMap.Dispose();
			_combinedControllerState.Dispose();
			_kbmState.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(ILatestInputRetriever));
	}
	#endregion
}