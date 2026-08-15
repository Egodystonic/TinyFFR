// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input.Local;

[SuppressUnmanagedCodeSecurity]
class LocalInputClipboard : IInputClipboard, IDisposable {
	const int MinBufferByteLength = 256;
	bool _isDisposed = false;
	InteropStringBuffer? _textBuffer;

	InteropStringBuffer GetOrCreateBufferOfAtLeast(int requiredByteLength) {
		if (_textBuffer is { } existing && existing.Length >= requiredByteLength) return existing;
		_textBuffer?.Dispose();
		var newLength = Int32.Max(MinBufferByteLength, requiredByteLength);
		var result = new InteropStringBuffer(newLength, true);
		_textBuffer = result;
		return result;
	}

	InteropStringBuffer? ReadIntoBuffer() {
		GetClipboardTextLength(out var byteLength).ThrowIfFailure();
		if (byteLength <= 0) return null;
		var buffer = GetOrCreateBufferOfAtLeast(byteLength + 1);
		GetClipboardText(ref buffer.AsRef, buffer.Length).ThrowIfFailure();
		return buffer;
	}

	public int GetClipboardTextLength() {
		ThrowIfThisIsDisposed();
		return ReadIntoBuffer()?.GetUtf16Length() ?? 0;
	}

	public int CopyClipboardText(Span<char> destinationBuffer) {
		ThrowIfThisIsDisposed();
		var buffer = ReadIntoBuffer();
		return buffer?.ConvertToUtf16(destinationBuffer) ?? 0;
	}

	public string GetClipboardTextAsNewStringObject() {
		ThrowIfThisIsDisposed();
		var buffer = ReadIntoBuffer();
		return buffer?.ToString() ?? String.Empty;
	}

	public void SetClipboardText(ReadOnlySpan<char> newText) {
		ThrowIfThisIsDisposed();
		var buffer = GetOrCreateBufferOfAtLeast(System.Text.Encoding.UTF8.GetByteCount(newText) + 1);
		buffer.ConvertFromUtf16(newText);
		SetClipboardText(ref buffer.AsRef).ThrowIfFailure();
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_textBuffer?.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}
	
	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IInputClipboard));
	}
	
	public override string ToString() => $"TinyFFR Local Clipboard{(_isDisposed ? " [Disposed]" : "")}";

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_clipboard_text_length")]
	static extern InteropResult GetClipboardTextLength(out int outByteLength);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_clipboard_text")]
	static extern InteropResult GetClipboardText(ref byte utf8BufferPtr, int bufferLength);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_clipboard_text")]
	static extern InteropResult SetClipboardText(ref readonly byte utf8BufferPtr);
}
