// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

[SuppressUnmanagedCodeSecurity]
sealed class LocalMeshBuilder : IMeshBuilder, IDisposable {
	

	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_event_poll_delegates")]
	static extern unsafe InteropResult SetEventPollDelegates(
		delegate* unmanaged<int*, InteropBool> filterTranslateKeycapValueDelegate,
		delegate* unmanaged<KeyboardOrMouseKeyEvent*> doubleKbmEventBufferDelegate,
		delegate* unmanaged<RawLocalGameControllerButtonEvent*> doubleControllerEventBufferDelegate,
		delegate* unmanaged<MouseClickEvent*> doubleClickEventBufferDelegate,
		delegate* unmanaged<GameControllerHandle, byte*, int, void> handleNewControllerDelegate
	);
	#endregion

	#region Disposal
	// TODO when disposing vbs/ibs/meshes, simply remove them from the collections until they're the last, and then dispose them
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