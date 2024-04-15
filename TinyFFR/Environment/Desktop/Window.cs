// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Reflection.Metadata;

namespace Egodystonic.TinyFFR.Environment.Desktop;

public readonly unsafe struct Window : IEquatable<Window> {
	static readonly ArrayPoolBackedVector<UIntPtr> _activeWindows = new(); // TODO these should be threadsafe?
	static readonly ArrayPoolBackedMap<UIntPtr, Display> _displayMap = new();
	static readonly ArrayPoolBackedMap<UIntPtr, InteropStringBuffer> _titleBufferMap = new();
	readonly WindowHandle _handle;

	UIntPtr HandleAsPtr {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (UIntPtr) _handle;
	}

	public string Title {
		get {
			var maxSpanLength = GetTitleSpanMaxLength();
			var dest = maxSpanLength <= 1000 ? stackalloc char[maxSpanLength] : new char[maxSpanLength];

			var numCharsWritten = GetTitleUsingSpan(dest);
			return new(dest[..numCharsWritten]);
		}
		set => SetTitleUsingSpan(value);
	}

	public Display Display {
		get {
			ThrowIfThisIsDisposed();
			return _displayMap[HandleAsPtr];
		}
		set {
			ThrowIfThisIsDisposed();
			var localPos = Position;
			_displayMap[HandleAsPtr] = value;
			Position = localPos;
		}
	}
	public XYPair<int> Size {
		get {
			ThrowIfThisIsDisposed();
			GetWindowSize(
				_handle,
				out var width,
				out var height
			).ThrowIfFailure();
			return new(width, height);
		}
		set {
			ThrowIfThisIsDisposed();

			if (value.X < 0) throw new ArgumentOutOfRangeException(nameof(value), value, $"'{nameof(value.X)}' value must be positive or 0.");
			if (value.Y < 0) throw new ArgumentOutOfRangeException(nameof(value), value, $"'{nameof(value.Y)}' value must be positive or 0.");

			SetWindowSize(
				_handle,
				value.X,
				value.Y
			).ThrowIfFailure();
		}
	}
	public XYPair<int> Position { // TODO explain in XMLDoc that this is relative positioning on the selected Display
		get {
			ThrowIfThisIsDisposed();
			GetWindowPosition(
				_handle,
				out var x,
				out var y
			).ThrowIfFailure();
			return Display.TranslateGlobalWindowPositionToDisplayLocal(new(x, y));
		}
		set {
			ThrowIfThisIsDisposed();
			var translatedPosition = Display.TranslateDisplayLocalWindowPositionToGlobal(value);
			SetWindowPosition(
				_handle,
				translatedPosition.X,
				translatedPosition.Y
			).ThrowIfFailure();
		}
	}

	public WindowFullscreenStyle FullscreenStyle {
		get {
			ThrowIfThisIsDisposed();
			GetWindowFullscreenState(
				_handle,
				out var isFullscreen,
				out var isBorderless
			).ThrowIfFailure();

			return ((bool) isFullscreen, (bool) isBorderless) switch {
				(true, true) => WindowFullscreenStyle.FullscreenBorderless,
				(true, false) => WindowFullscreenStyle.Fullscreen,
				_ => WindowFullscreenStyle.NotFullscreen
			};
		}
		set {
			ThrowIfThisIsDisposed();
			SetWindowFullscreenState(
				_handle,
				value != WindowFullscreenStyle.NotFullscreen,
				value == WindowFullscreenStyle.FullscreenBorderless
			).ThrowIfFailure();
		}
	}

	public bool LockCursor {
		get {
			ThrowIfThisIsDisposed();
			GetWindowCursorLockState(
				_handle,
				out var result
			).ThrowIfFailure();
			return result;
		}
		set {
			ThrowIfThisIsDisposed();
			SetWindowCursorLockState(
				_handle,
				value
			).ThrowIfFailure();
		}
	}

	internal Window(WindowHandle handle, Display connectedDisplay, InteropStringBuffer titleBuffer) {
		if (_activeWindows.Count > 0) throw new NotImplementedException("Currently TinyFFR only supports one window at a time. This restriction will be lifted in the future.");
		_handle = handle;
		_activeWindows.Add(HandleAsPtr);
		_displayMap.Add(HandleAsPtr, connectedDisplay);
		_titleBufferMap.Add(HandleAsPtr, titleBuffer);
	}

	public void SetTitleUsingSpan(ReadOnlySpan<char> src) {
		ThrowIfThisIsDisposed();
		var titleBuffer = _titleBufferMap[HandleAsPtr];
		titleBuffer.ConvertFromUtf16(src);
		SetWindowTitle(
			_handle,
			ref titleBuffer.BufferRef
		).ThrowIfFailure();
	}

	public int GetTitleUsingSpan(Span<char> dest) {
		ThrowIfThisIsDisposed();
		var titleBuffer = _titleBufferMap[HandleAsPtr];
		GetWindowTitle(
			_handle,
			ref titleBuffer.BufferRef,
			titleBuffer.BufferLength
		).ThrowIfFailure();
		return titleBuffer.ConvertToUtf16(dest);
	}

	public int GetTitleSpanMaxLength() {
		ThrowIfThisIsDisposed();
		return _titleBufferMap[HandleAsPtr].BufferLength;
	}

	public override string ToString() => $"{nameof(Window)} \"{Title}\"";

	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_size")]
	static extern InteropResult GetWindowSize(WindowHandle handle, out int outWidth, out int outHeight);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_size")]
	static extern InteropResult SetWindowSize(WindowHandle handle, int newWidth, int newHeight);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_position")]
	static extern InteropResult GetWindowPosition(WindowHandle handle, out int outX, out int outY);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_position")]
	static extern InteropResult SetWindowPosition(WindowHandle handle, int newX, int newY);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_fullscreen_state")]
	static extern InteropResult SetWindowFullscreenState(WindowHandle handle, InteropBool fullscreen, InteropBool borderless);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_fullscreen_state")]
	static extern InteropResult GetWindowFullscreenState(WindowHandle handle, out InteropBool fullscreen, out InteropBool borderless);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_cursor_lock_state")]
	static extern InteropResult GetWindowCursorLockState(WindowHandle handle, out InteropBool outLockState);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_cursor_lock_state")]
	static extern InteropResult SetWindowCursorLockState(WindowHandle handle, InteropBool lockState);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_title")]
	static extern InteropResult GetWindowTitle(WindowHandle handle, ref byte utf8BufferPtr, int bufferLength);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_title")]
	static extern InteropResult SetWindowTitle(WindowHandle handle, ref readonly byte utf8BufferPtr);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "dispose_window")]
	static extern InteropResult DisposeWindow(WindowHandle handle);
	#endregion

	#region Disposal
	bool IsDisposed => !_activeWindows.Contains(HandleAsPtr);

	public void Dispose() {
		if (IsDisposed) return;
		try {
			DisposeWindow(_handle).ThrowIfFailure();
		}
		finally {
			_titleBufferMap.Remove(HandleAsPtr);
			_displayMap.Remove(HandleAsPtr);
			_activeWindows.Remove(HandleAsPtr);
		}
	}

	internal void ThrowIfInvalid() => InvalidObjectException.ThrowIfDefault(this);

	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);
	#endregion

	#region Equality
	public bool Equals(Window other) => _handle == other._handle;
	public override bool Equals(object? obj) => obj is Window other && Equals(other);
	public override int GetHashCode() => HandleAsPtr.GetHashCode();
	public static bool operator ==(Window left, Window right) => left.Equals(right);
	public static bool operator !=(Window left, Window right) => !left.Equals(right);
	#endregion
}