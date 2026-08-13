// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

public interface ILatestInputRetriever { 
	bool UserQuitRequested { get; }

	ILatestKeyboardAndMouseInputRetriever KeyboardAndMouse { get; }

	IndirectEnumerable<ILatestInputRetriever, ILatestGameControllerInputRetriever> GameControllers { get; }
	ILatestGameControllerInputRetriever GameControllersCombined { get; }

	int GetClipboardTextLength() => Local.LocalClipboard.GetTextLength();
	int CopyClipboardText(Span<char> destinationBuffer) => Local.LocalClipboard.CopyText(destinationBuffer);
	string GetClipboardTextAsNewStringObject() => Local.LocalClipboard.GetTextAsNewStringObject();
	void SetClipboardText(ReadOnlySpan<char> newText) => Local.LocalClipboard.SetText(newText);
}