// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Provides controls to access the system clipboard.
/// </summary>
public interface IInputClipboard { 
	int GetClipboardTextLength();
	int CopyClipboardText(Span<char> destinationBuffer);
	string GetClipboardTextAsNewStringObject();
	void SetClipboardText(ReadOnlySpan<char> newText);
}