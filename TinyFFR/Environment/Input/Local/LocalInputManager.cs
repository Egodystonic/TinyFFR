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
	static LocalInputSnapshotProvider? _liveInstance = null;
	static int _instanceRefCount = 0;

	public static LocalInputSnapshotProvider IncrementRefCountAndGetProvider() {
		_instanceRefCount++;
		if (_liveInstance != null) return _liveInstance;

		SetEventPollDelegates(
			&FilterAndTranslateKeycode,
			&ResizeCurrentPollInstanceKbmEventBuffer,
			&ResizeCurrentPollInstanceControllerEventBuffer,
			&ResizeCurrentPollInstanceClickEventBuffer,
			&HandlePotentialNewController
		).ThrowIfFailure();

		_liveInstance = new LocalInputSnapshotProvider();
		_liveInstance.GetEventBufferPointers(
			out var kbmEventBufferPtr,
			out var kbmEventBufferLen,
			out var controllerEventBufferPtr,
			out var controllerEventBufferLen,
			out var clickEventBufferPtr,
			out var clickEventBufferLen
		);

		SetEventPollBufferPointers(
			kbmEventBufferPtr, 
			kbmEventBufferLen, 
			controllerEventBufferPtr, 
			controllerEventBufferLen, 
			clickEventBufferPtr, 
			clickEventBufferLen
		);

		_liveInstance.Initialize();
		return _liveInstance;
	}

	public static void DecrementRefCount() {
		_instanceRefCount--;
		if (_instanceRefCount < 0) throw new InvalidOperationException("Erroneous live instance ref count decrement in input manager.");
		else if (_instanceRefCount == 0) _liveInstance!.Dispose();
	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_event_poll_delegates")]
	static extern InteropResult SetEventPollDelegates(
		delegate* unmanaged<int*, InteropBool> filterTranslateKeycapValueDelegate,
		delegate* unmanaged<KeyboardOrMouseKeyEvent*> doubleKbmEventBufferDelegate,
		delegate* unmanaged<RawLocalGameControllerButtonEvent*> doubleControllerEventBufferDelegate,
		delegate* unmanaged<MouseClickEvent*> doubleClickEventBufferDelegate,
		delegate* unmanaged<UIntPtr, byte*, int, void> handleNewControllerDelegate
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_event_poll_buffer_pointers")]
	static extern InteropResult SetEventPollBufferPointers(
		KeyboardOrMouseKeyEvent* kbmEventBufferPtr,
		int kbmEventBufferLen,
		RawLocalGameControllerButtonEvent* controllerEventBufferPtr,
		int controllerEventBufferLen,
		MouseClickEvent* clickEventBufferPtr,
		int clickEventBufferLen
	);
	[UnmanagedCallersOnly]
	static KeyboardOrMouseKeyEvent* ResizeCurrentPollInstanceKbmEventBuffer() {
		ThrowIfNoLiveInstance();
		return _liveInstance.DoubleKbmEventBufferSize();
	}
	[UnmanagedCallersOnly]
	static RawLocalGameControllerButtonEvent* ResizeCurrentPollInstanceControllerEventBuffer() {
		ThrowIfNoLiveInstance();
		return _liveInstance.DoubleControllerEventBufferSize();
	}
	[UnmanagedCallersOnly]
	static MouseClickEvent* ResizeCurrentPollInstanceClickEventBuffer() {
		ThrowIfNoLiveInstance();
		return _liveInstance.DoubleClickEventBufferSize();
	}
	[UnmanagedCallersOnly]
	static InteropBool FilterAndTranslateKeycode(int* keycode) {
		*keycode |= (~*keycode & KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit) >> KeyboardOrMouseKeyExtensions.CharBasedValueBitDistanceToScancodeBit;
		return Enum.IsDefined((KeyboardOrMouseKey) (*keycode));
	}

	[UnmanagedCallersOnly]
	static void HandlePotentialNewController(UIntPtr handle, byte* utf8NamePtr, int utf8NameLen) {
		ThrowIfNoLiveInstance();
		_liveInstance.HandlePotentialNewController(handle, utf8NamePtr, utf8NameLen);
	}
	#endregion

	[MemberNotNull(nameof(_liveInstance))]
	static void ThrowIfNoLiveInstance() {
		if (_liveInstance == null) throw new InvalidOperationException("No live instance registered in input manager.");
	}
}