// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Provides controls to access the system clipboard.
/// </summary>
public interface IInputClipboard { 
	/// <summary>
	/// Returns the length, in characters, of the text currently on the system clipboard (or <c>0</c> if it holds no text).
	/// </summary>
	int GetClipboardTextLength();
	/// <summary>
	/// Copies the text currently on the system clipboard in to <paramref name="destinationBuffer"/>, without allocating, and returns the number of characters written.
	/// </summary>
	/// <remarks>
	/// The clipboard is shared with every other application on the machine and can therefore change at any moment, including between a call to
	/// <see cref="GetClipboardTextLength"/> and this one. Accordingly, this writes at most <c><paramref name="destinationBuffer"/>.Length</c> characters and tells
	/// you how many it wrote, rather than assuming the buffer is big enough.
	/// </remarks>
	/// <param name="destinationBuffer">The buffer to copy the clipboard text in to.</param>
	/// <returns>The number of characters written to <paramref name="destinationBuffer"/>.</returns>
	int CopyClipboardText(Span<char> destinationBuffer);
	/// <summary>
	/// Returns the text currently on the system clipboard as a newly-allocated <see cref="string"/> (or an empty string if it holds no text).
	/// </summary>
	/// <remarks>
	/// Prefer <see cref="GetClipboardTextLength"/> and <see cref="CopyClipboardText"/> instead if you want to avoid allocating, for example to copy the text in to a pooled or stack-allocated buffer.
	/// </remarks>
	string GetClipboardTextAsNewStringObject();
	/// <summary>
	/// Replaces the contents of the system clipboard with <paramref name="newText"/>.
	/// </summary>
	/// <param name="newText">The text to place on the clipboard.</param>
	void SetClipboardText(ReadOnlySpan<char> newText);
}