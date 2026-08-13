// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Security;
using System.Threading;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input.Local;

[SuppressUnmanagedCodeSecurity]
static unsafe class LocalClipboard {
	const int MinBufferByteLength = 256;
	static readonly Lock _mutationLock = new();
	static InteropStringBuffer? _buffer;

	static InteropStringBuffer GetOrCreateBufferOfAtLeast(int requiredByteLength) {
		if (_buffer is { } existing && existing.Length >= requiredByteLength) return existing;
		_buffer?.Dispose();
		var newLength = Int32.Max(MinBufferByteLength, requiredByteLength);
		var result = new InteropStringBuffer(newLength, true);
		_buffer = result;
		return result;
	}

	static InteropStringBuffer? ReadIntoBuffer() {
		GetClipboardTextLength(out var byteLength).ThrowIfFailure();
		if (byteLength <= 0) return null;
		var buffer = GetOrCreateBufferOfAtLeast(byteLength + 1);
		GetClipboardText(ref buffer.AsRef, buffer.Length).ThrowIfFailure();
		return buffer;
	}

	public static int GetTextLength() {
		lock (_mutationLock) {
			return ReadIntoBuffer()?.GetUtf16Length() ?? 0;
		}
	}

	public static int CopyText(Span<char> destinationBuffer) {
		lock (_mutationLock) {
			var buffer = ReadIntoBuffer();
			return buffer?.ConvertToUtf16(destinationBuffer) ?? 0;
		}
	}

	public static string GetTextAsNewStringObject() {
		lock (_mutationLock) {
			var buffer = ReadIntoBuffer();
			return buffer?.ToString() ?? String.Empty;
		}
	}

	public static void SetText(ReadOnlySpan<char> newText) {
		lock (_mutationLock) {
			var buffer = GetOrCreateBufferOfAtLeast(System.Text.Encoding.UTF8.GetByteCount(newText) + 1);
			buffer.ConvertFromUtf16(newText);
			SetClipboardText(ref buffer.AsRef).ThrowIfFailure();
		}
	}

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_clipboard_text_length")]
	static extern InteropResult GetClipboardTextLength(out int outByteLength);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_clipboard_text")]
	static extern InteropResult GetClipboardText(ref byte utf8BufferPtr, int bufferLength);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_clipboard_text")]
	static extern InteropResult SetClipboardText(ref readonly byte utf8BufferPtr);
}
