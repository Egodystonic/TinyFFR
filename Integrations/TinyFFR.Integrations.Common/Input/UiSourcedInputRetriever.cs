// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Input;

sealed class UiSourcedInputRetriever : ILatestInputRetriever, IDisposable {
	readonly UiSourcedKeyboardAndMouseInputRetriever _kbmState = new();
	readonly StubGameControllerInputRetriever _combinedControllerState = new();
	bool _isDisposed = false;

	public bool UserQuitRequested { get; private set; } = false;

	public ILatestKeyboardAndMouseInputRetriever KeyboardAndMouse {
		get {
			ThrowIfThisIsDisposed();
			return _kbmState;
		}
	}
	public IndirectEnumerable<ILatestInputRetriever, ILatestGameControllerInputRetriever> GameControllers {
		get {
			ThrowIfThisIsDisposed();
			return IndirectEnumerable<ILatestInputRetriever, ILatestGameControllerInputRetriever>.Empty;
		}
	}
	public ILatestGameControllerInputRetriever GameControllersCombined {
		get {
			ThrowIfThisIsDisposed();
			return _combinedControllerState;
		}
	}

	internal UiSourcedKeyboardAndMouseInputRetriever KeyboardAndMouseState => _kbmState;

	internal void SetUserQuitRequested() => UserQuitRequested = true;

	internal void Iterate() {
		ThrowIfThisIsDisposed();
		_kbmState.Iterate();
	}

	public override string ToString() => $"TinyFFR UI Input State Provider{(_isDisposed ? " [Disposed]" : "")}";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
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
