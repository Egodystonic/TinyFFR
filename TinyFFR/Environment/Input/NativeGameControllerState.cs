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
sealed class NativeGameControllerState : IDisposable {
	public InteropStringBuffer NameBuffer { get; }
	public ArrayPoolBackedVector<GameControllerButtonEvent> NewButtonEvents { get; } = new();
	public ArrayPoolBackedVector<GameControllerButton> CurrentlyPressedButtons { get; } = new();
	public bool IsConnected { get; set; } = true;
	public GameControllerStickPosition LeftStickPos { get; set; } = default;
	public GameControllerStickPosition RightStickPos { get; set; } = default;
	public GameControllerTriggerPosition LeftTriggerPos { get; set; } = default;
	public GameControllerTriggerPosition RightTriggerPos { get; set; } = default;
	bool _isDisposed = false;

	public NativeGameControllerState(int nameBufferLength) {
		NameBuffer = new(nameBufferLength);
	}

	public void ApplyEvent(RawGameControllerButtonEvent rawEvent) {

	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			NewButtonEvents.Dispose();
			CurrentlyPressedButtons.Dispose();
			NameBuffer.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}
}