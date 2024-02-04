// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Input;

[SuppressUnmanagedCodeSecurity]
sealed class NativeGameControllerInputTracker : IGameControllerInputTracker, IDisposable {
	readonly GameControllerId? _controllerId;
	readonly ArrayPoolBackedVector<GameControllerButtonEvent> _events;
	readonly ArrayPoolBackedVector<GameControllerButton> _currentlyPressedButtons;
	bool _isConnected = true;
	GameControllerStickPosition _leftStickPos = default;
	GameControllerStickPosition _rightStickPos = default;
	GameControllerTriggerPosition _leftTriggerPos = default;
	GameControllerTriggerPosition _rightTriggerPos = default;
	bool _isDisposed = false;

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_events.Dispose();
			_currentlyPressedButtons.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}
}