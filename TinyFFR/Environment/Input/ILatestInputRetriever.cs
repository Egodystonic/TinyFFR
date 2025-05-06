// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

public interface ILatestInputRetriever { 
	bool UserQuitRequested { get; }

	ILatestKeyboardAndMouseInputRetriever KeyboardAndMouse { get; }

	TypedReferentIterator<ILatestInputRetriever, ILatestGameControllerInputStateRetriever> GameControllers { get; }
	ILatestGameControllerInputStateRetriever GameControllersCombined { get; }
}