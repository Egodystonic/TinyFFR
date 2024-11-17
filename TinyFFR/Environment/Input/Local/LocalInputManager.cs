// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Environment.Input.Local;

[SuppressUnmanagedCodeSecurity]
static unsafe class LocalInputManager {
	sealed class BufferGroup : IDisposable {
		public UnmanagedBuffer<RawLocalGameControllerButtonEvent> ControllerEventBuffer { get; } = new(InitialEventBufferLength);
		public UnmanagedBuffer<KeyboardOrMouseKeyEvent> KbmEventBuffer { get; } = new(InitialEventBufferLength);
		public UnmanagedBuffer<MouseClickEvent> MouseClickBuffer { get; } = new(InitialEventBufferLength);

		public void Dispose() {
			ControllerEventBuffer.Dispose();
			KbmEventBuffer.Dispose();
			MouseClickBuffer.Dispose();
		}
	}

	public const int InitialEventBufferLength = 50;
	public static readonly UIntPtr CombinedGameControllerHandle = UIntPtr.Zero;
	static BufferGroup? _buffers;

	static BufferGroup Buffers => _buffers ?? throw CreateAccessBeforeInitException();

	public static void InitializeIfNecessary() {
		if (_buffers != null) return;
		_buffers = new();

		SetEventPollDelegates(
			&FilterAndTranslateKeycode,
			&ResizeCurrentPollInstanceKbmEventBuffer,
			&ResizeCurrentPollInstanceControllerEventBuffer,
			&ResizeCurrentPollInstanceClickEventBuffer,
			&HandlePotentialNewController
		).ThrowIfFailure();

		SetEventPollBufferPointers(
			Buffers.KbmEventBuffer.BufferPointer,
			Buffers.KbmEventBuffer.Length,
			Buffers.ControllerEventBuffer.BufferPointer,
			Buffers.ControllerEventBuffer.Length,
			Buffers.MouseClickBuffer.BufferPointer,
			Buffers.MouseClickBuffer.Length
		).ThrowIfFailure();

		DetectControllers().ThrowIfFailure();
	}

	public static void DisposeIfNecessary() {
		_buffers?.Dispose();
		_buffers = null;
	}

	public static void IterateLocalInput() {
		ThrowIfNotInitialized();

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

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_event_poll_delegates")]
	static extern unsafe InteropResult SetEventPollDelegates(
		delegate* unmanaged<int*, InteropBool> filterTranslateKeycapValueDelegate,
		delegate* unmanaged<KeyboardOrMouseKeyEvent*> doubleKbmEventBufferDelegate,
		delegate* unmanaged<RawLocalGameControllerButtonEvent*> doubleControllerEventBufferDelegate,
		delegate* unmanaged<MouseClickEvent*> doubleClickEventBufferDelegate,
		delegate* unmanaged<UIntPtr, byte*, int, void> handleNewControllerDelegate
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_event_poll_buffer_pointers")]
	static extern unsafe InteropResult SetEventPollBufferPointers(
		KeyboardOrMouseKeyEvent* kbmEventBufferPtr,
		int kbmEventBufferLen,
		RawLocalGameControllerButtonEvent* controllerEventBufferPtr,
		int controllerEventBufferLen,
		MouseClickEvent* clickEventBufferPtr,
		int clickEventBufferLen
	);
	[UnmanagedCallersOnly]
	static unsafe KeyboardOrMouseKeyEvent* ResizeCurrentPollInstanceKbmEventBuffer() {
		ThrowIfNotInitialized();
		_liveInstance._kbmStateObject.EventBuffer.DoubleSize();
		return _liveInstance._kbmStateObject.EventBuffer.BufferPointer;
	}
	[UnmanagedCallersOnly]
	static unsafe RawLocalGameControllerButtonEvent* ResizeCurrentPollInstanceControllerEventBuffer() {
		ThrowIfNotInitialized();
		_liveInstance._controllerEventBuffer.DoubleSize();
		return _liveInstance._controllerEventBuffer.BufferPointer;
	}
	[UnmanagedCallersOnly]
	static unsafe MouseClickEvent* ResizeCurrentPollInstanceClickEventBuffer() {
		ThrowIfNotInitialized();
		_liveInstance._kbmStateObject.ClickBuffer.DoubleSize();
		return _liveInstance._kbmStateObject.ClickBuffer.BufferPointer;
	}
	[UnmanagedCallersOnly]
	static unsafe InteropBool FilterAndTranslateKeycode(int* keycode) {
		*keycode |= (~*keycode & KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit) >> KeyboardOrMouseKeyExtensions.CharBasedValueBitDistanceToScancodeBit;
		return Enum.IsDefined((KeyboardOrMouseKey) (*keycode));
	}

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

	[UnmanagedCallersOnly]
	static unsafe void HandlePotentialNewController(UIntPtr handle, byte* utf8NamePtr, int utf8NameLen) {
		ThrowIfNotInitialized();
		foreach (var kvp in _liveInstance._detectedControllerStateObjectMap) {
			if (kvp.Value.Handle == handle) return;
		}

		var state = new LocalGameControllerState(handle);
		var nameSpan = new ReadOnlySpan<byte>(utf8NamePtr, utf8NameLen);
		if (nameSpan.Length > state.NameBuffer.AsSpan.Length) nameSpan = nameSpan[..state.NameBuffer.AsSpan.Length];
		nameSpan.CopyTo(state.NameBuffer.AsSpan);
		_liveInstance._detectedControllerStateObjectVector.Add(state);
		_liveInstance._detectedControllerStateObjectMap.Add(handle, state);
	}
	#endregion

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

	static void ThrowIfNotInitialized() {
		if (_buffers == null) throw CreateAccessBeforeInitException();
	}

	static InvalidOperationException CreateAccessBeforeInitException() {
		return new InvalidOperationException("Can not access input manager before initialization.");
	}
}