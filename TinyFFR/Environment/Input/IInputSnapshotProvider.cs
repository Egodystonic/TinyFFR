// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

public interface IInputSnapshotProvider { 
	bool UserQuitRequested { get; }

	IKeyboardAndMouseInputSnapshotProvider KeyboardAndMouse { get; }

	ReadOnlySpan<IGameControllerInputSnapshotProvider> GameControllers { get; }
	IGameControllerInputSnapshotProvider GameControllersCombined { get; }
}