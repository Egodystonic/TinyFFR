// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Environment.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class WindowBuilder : IWindowBuilder, IWindowImplProvider, IDisposable {
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly InteropStringBuffer _windowTitleBuffer;
	readonly ArrayPoolBackedVector<WindowHandle> _activeWindows = new();
	readonly ArrayPoolBackedMap<WindowHandle, Display> _displayMap = new();
	bool _isDisposed = false;

	public WindowBuilder(LocalFactoryGlobalObjectGroup globals, WindowBuilderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_globals = globals;
		_windowTitleBuffer = new InteropStringBuffer(config.MaxWindowTitleLength, addOneForNullTerminator: true);
	}

	public Window Build(Display display, WindowFullscreenStyle fullscreenStyle) {
		return Build(new() {
			Display = display,
			FullscreenStyle = fullscreenStyle,
			Size = fullscreenStyle == WindowFullscreenStyle.NotFullscreen ? display.CurrentResolution * 0.66f : display.CurrentResolution
		});
	}

	public Window Build(in WindowConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		var globalPosition = config.Display.TranslateDisplayLocalWindowPositionToGlobal(config.Position);
		CreateWindow(
			out var outHandle,
			config.Size.X,
			config.Size.Y,
			globalPosition.X,
			globalPosition.Y
		).ThrowIfFailure();
		var result = new Window(outHandle, this);
		_activeWindows.Add(outHandle);
		_displayMap.Add(outHandle, config.Display);
		result.FullscreenStyle = config.FullscreenStyle;
		return result;
	}

	public string GetTitle(WindowHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var maxSpanLength = GetTitleSpanMaxLength(handle);
		var dest = maxSpanLength <= 1000 ? stackalloc char[maxSpanLength] : new char[maxSpanLength];

		var numCharsWritten = GetTitleUsingSpan(handle, dest);
		return new(dest[..numCharsWritten]);
	}
	public void SetTitle(WindowHandle handle, string newTitle) => SetTitleUsingSpan(handle, newTitle);

	public int GetTitleUsingSpan(WindowHandle handle, Span<char> dest) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowTitle(
			handle,
			ref _windowTitleBuffer.BufferRef,
			_windowTitleBuffer.BufferLength
		).ThrowIfFailure();
		return _windowTitleBuffer.ConvertToUtf16(dest);
	}
	public void SetTitleUsingSpan(WindowHandle handle, ReadOnlySpan<char> src) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_windowTitleBuffer.ConvertFromUtf16(src);
		SetWindowTitle(
			handle,
			ref _windowTitleBuffer.BufferRef
		).ThrowIfFailure();
	}
	public int GetTitleSpanLength(WindowHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowTitle(
			handle,
			ref _windowTitleBuffer.BufferRef,
			_windowTitleBuffer.BufferLength
		).ThrowIfFailure();
		return _windowTitleBuffer.GetUtf16Length();
	}
	int GetTitleSpanMaxLength(WindowHandle handle) => _windowTitleBuffer.BufferLength;

	public Display GetDisplay(WindowHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _displayMap[handle];
	}
	public void SetDisplay(WindowHandle handle, Display newDisplay) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var localPos = GetPosition(handle);
		_displayMap[handle] = newDisplay;
		SetPosition(handle, localPos);
	}

	public XYPair<int> GetSize(WindowHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowSize(
			handle,
			out var width,
			out var height
		).ThrowIfFailure();
		return new(width, height);
	}
	public void SetSize(WindowHandle handle, XYPair<int> newSize) {
		ThrowIfThisOrHandleIsDisposed(handle);

		if (newSize.X < 0) throw new ArgumentOutOfRangeException(nameof(newSize), newSize, $"'{nameof(newSize.X)}' value must be positive or 0.");
		if (newSize.Y < 0) throw new ArgumentOutOfRangeException(nameof(newSize), newSize, $"'{nameof(newSize.Y)}' value must be positive or 0.");

		SetWindowSize(
			handle,
			newSize.X,
			newSize.Y
		).ThrowIfFailure();
	}

	public XYPair<int> GetPosition(WindowHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowPosition(
			handle,
			out var x,
			out var y
		).ThrowIfFailure();
		return _displayMap[handle].TranslateGlobalWindowPositionToDisplayLocal(new(x, y));
	}
	public void SetPosition(WindowHandle handle, XYPair<int> newPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var translatedPosition = _displayMap[handle].TranslateDisplayLocalWindowPositionToGlobal(newPosition);
		SetWindowPosition(
			handle,
			translatedPosition.X,
			translatedPosition.Y
		).ThrowIfFailure();
	}

	public WindowFullscreenStyle GetFullscreenStyle(WindowHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowFullscreenState(
			handle,
			out var isFullscreen,
			out var isBorderless
		).ThrowIfFailure();

		return ((bool) isFullscreen, (bool) isBorderless) switch {
			(true, true) => WindowFullscreenStyle.FullscreenBorderless,
			(true, false) => WindowFullscreenStyle.Fullscreen,
			_ => WindowFullscreenStyle.NotFullscreen
		};
	}
	public void SetFullscreenStyle(WindowHandle handle, WindowFullscreenStyle newStyle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetWindowFullscreenState(
			handle,
			newStyle != WindowFullscreenStyle.NotFullscreen,
			newStyle == WindowFullscreenStyle.FullscreenBorderless
		).ThrowIfFailure();
	}

	public bool GetCursorLock(WindowHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowCursorLockState(
			handle,
			out var result
		).ThrowIfFailure();
		return result;
	}
	public void SetCursorLock(WindowHandle handle, bool newLockSetting) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetWindowCursorLockState(
			handle,
			newLockSetting
		).ThrowIfFailure();
	}

	public override string ToString() => _isDisposed ? "TinyFFR Window Builder [Disposed]" : "TinyFFR Window Builder";

	#region Disposal
	public void Dispose(WindowHandle handle) {
		if (IsDisposed(handle)) return;
		try {
			DisposeWindow(handle).ThrowIfFailure();
		}
		finally {
			_displayMap.Remove(handle);
			_activeWindows.Remove(handle);
		}
	}
	public bool IsDisposed(WindowHandle handle) {
		return _isDisposed || !_activeWindows.Contains(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var handle in _activeWindows) DisposeWindow(handle).ThrowIfFailure();
			_activeWindows.Dispose();
			_displayMap.Dispose();
			_windowTitleBuffer.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(WindowHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Window));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "create_window")]
	static extern InteropResult CreateWindow(out UIntPtr outHandle, int width, int height, int xPos, int yPos);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_window_size")]
	static extern InteropResult GetWindowSize(UIntPtr handle, out int outWidth, out int outHeight);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_window_size")]
	static extern InteropResult SetWindowSize(UIntPtr handle, int newWidth, int newHeight);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_window_position")]
	static extern InteropResult GetWindowPosition(UIntPtr handle, out int outX, out int outY);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_window_position")]
	static extern InteropResult SetWindowPosition(UIntPtr handle, int newX, int newY);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_window_fullscreen_state")]
	static extern InteropResult SetWindowFullscreenState(UIntPtr handle, InteropBool fullscreen, InteropBool borderless);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_window_fullscreen_state")]
	static extern InteropResult GetWindowFullscreenState(UIntPtr handle, out InteropBool fullscreen, out InteropBool borderless);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_window_cursor_lock_state")]
	static extern InteropResult GetWindowCursorLockState(UIntPtr handle, out InteropBool outLockState);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_window_cursor_lock_state")]
	static extern InteropResult SetWindowCursorLockState(UIntPtr handle, InteropBool lockState);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_window_title")]
	static extern InteropResult GetWindowTitle(UIntPtr handle, ref byte utf8BufferPtr, int bufferLength);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_window_title")]
	static extern InteropResult SetWindowTitle(UIntPtr handle, ref readonly byte utf8BufferPtr);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_window")]
	static extern InteropResult DisposeWindow(UIntPtr handle);
	#endregion
}