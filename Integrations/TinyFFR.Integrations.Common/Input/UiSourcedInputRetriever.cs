// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Environment.Input.Ui;

// An ILatestInputRetriever populated from a host UI framework's own input events rather than from TinyFFR's native
// event poll (which must not be run whilst a UI framework owns the message loop). Game controllers are not exposed
// by any supported UI framework, hence the neutral state object and empty enumerable.
sealed class UiSourcedInputRetriever : ILatestInputRetriever, IDisposable {
	readonly UiSourcedKeyboardAndMouseRetriever _kbmState = new();
	readonly NeutralGameControllerState _combinedControllerState = new();
	bool _isDisposed = false;

	public bool UserQuitRequested { get; private set; } = false;

	public ILatestKeyboardAndMouseInputRetriever KeyboardAndMouse {
		get {
			ThrowIfThisIsDisposed();
			return _kbmState;
		}
	}
	public IndirectEnumerable<ILatestInputRetriever, ILatestGameControllerInputStateRetriever> GameControllers {
		get {
			ThrowIfThisIsDisposed();
			return IndirectEnumerable<ILatestInputRetriever, ILatestGameControllerInputStateRetriever>.Empty;
		}
	}
	public ILatestGameControllerInputStateRetriever GameControllersCombined {
		get {
			ThrowIfThisIsDisposed();
			return _combinedControllerState;
		}
	}

	internal UiSourcedKeyboardAndMouseRetriever KeyboardAndMouseState => _kbmState;

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
