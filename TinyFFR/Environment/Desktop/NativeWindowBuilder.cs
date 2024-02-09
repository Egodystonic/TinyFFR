// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Desktop;

[SuppressUnmanagedCodeSecurity]
sealed class NativeWindowBuilder : IWindowBuilder, IWindowHandleImplProvider, IDisposable {
	readonly ArrayPoolBackedVector<WindowHandle> _activeWindows = new();
	readonly ArrayPoolBackedMap<WindowHandle, Display> _windowDisplayMap = new();
	readonly InteropStringBuffer _windowTitleBuffer;
	bool _isDisposed = false;

	public NativeWindowBuilder(WindowBuilderConfig config) {
		_windowTitleBuffer = new InteropStringBuffer(config.MaxWindowTitleLength + 1); // + 1 for null terminator
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "create_window")]
	static extern InteropResult CreateWindow(out WindowHandle outHandle, int width, int height, int xPos, int yPos);
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
			out var outPtr,
			(int) config.Size.X,
			(int) config.Size.Y,
			(int) globalPosition.X,
			(int) globalPosition.Y
		).ThrowIfFailure();
		_activeWindows.Add(outPtr);
		_windowDisplayMap.Add(outPtr, config.Display);
		SetTitle(outPtr, config.Title);
		SetFullscreenState(outPtr, config.FullscreenStyle);
		return new(outPtr, this);
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_title")]
	static extern InteropResult SetWindowTitle(WindowHandle handle, ref readonly byte utf8BufferPtr);
	public void SetTitle(WindowHandle handle, ReadOnlySpan<char> src) {
		ThrowIfHandleOrThisIsDisposed(handle);
		_windowTitleBuffer.WriteFrom(src);
		SetWindowTitle(
			handle, 
			ref _windowTitleBuffer.BufferRef
		).ThrowIfFailure();
	}

	public int GetTitleMaxLength() => _windowTitleBuffer.BufferLength;

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_title")]
	static extern InteropResult GetWindowTitle(WindowHandle handle, ref byte utf8BufferPtr, int bufferLength);
	public int GetTitle(WindowHandle handle, Span<char> dest) {
		ThrowIfHandleOrThisIsDisposed(handle);
		GetWindowTitle(
			handle,
			ref _windowTitleBuffer.BufferRef,
			_windowTitleBuffer.BufferLength
		).ThrowIfFailure();
		return _windowTitleBuffer.ReadTo(dest);
	}

	public Display GetDisplay(WindowHandle handle) {
		if (!_windowDisplayMap.TryGetValue(handle, out var result)) throw new InvalidOperationException($"Given {nameof(Window)} did not have a corresponding {nameof(Display)} mapped.");
		return result;
	}

	public void SetDisplay(WindowHandle handle, Display newDisplay) {
		ThrowIfHandleOrThisIsDisposed(handle);
		var localPos = GetPosition(handle);
		_windowDisplayMap[handle] = newDisplay;
		SetPosition(handle, localPos);
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_size")]
	static extern InteropResult SetWindowSize(WindowHandle handle, int newWidth, int newHeight);
	public void SetSize(WindowHandle handle, XYPair newDimensions) {
		ThrowIfHandleOrThisIsDisposed(handle);

		var newWidth = (int) newDimensions.X;
		var newHeight = (int) newDimensions.Y;

		if (newWidth < 0) throw new ArgumentOutOfRangeException(nameof(newDimensions), newDimensions, $"'{nameof(XYPair.X)}' value must be positive or 0.");
		if (newHeight < 0) throw new ArgumentOutOfRangeException(nameof(newDimensions), newDimensions, $"'{nameof(XYPair.Y)}' value must be positive or 0.");

		SetWindowSize(
			handle,
			newWidth,
			newHeight
		).ThrowIfFailure();
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_size")]
	static extern InteropResult GetWindowSize(WindowHandle handle, out int outWidth, out int outHeight);
	public XYPair GetSize(WindowHandle handle) {
		ThrowIfHandleOrThisIsDisposed(handle);
		GetWindowSize(
			handle,
			out var width,
			out var height
		).ThrowIfFailure();
		return new(width, height);
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_position")]
	static extern InteropResult SetWindowPosition(WindowHandle handle, int newX, int newY);
	public void SetPosition(WindowHandle handle, XYPair newPosition) {
		ThrowIfHandleOrThisIsDisposed(handle);
		var translatedPosition = GetDisplay(handle).TranslateDisplayLocalWindowPositionToGlobal(newPosition);
		SetWindowPosition(
			handle,
			(int) translatedPosition.X,
			(int) translatedPosition.Y
		).ThrowIfFailure();
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_position")]
	static extern InteropResult GetWindowPosition(WindowHandle handle, out int outX, out int outY);
	public XYPair GetPosition(WindowHandle handle) {
		ThrowIfHandleOrThisIsDisposed(handle);
		GetWindowPosition(
			handle,
			out var x,
			out var y
		).ThrowIfFailure();
		return GetDisplay(handle).TranslateGlobalWindowPositionToDisplayLocal(new(x, y));
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_fullscreen_state")]
	static extern InteropResult SetWindowFullscreenState(WindowHandle handle, InteropBool fullscreen, InteropBool borderless);
	public void SetFullscreenState(WindowHandle handle, WindowFullscreenStyle style) {
		ThrowIfHandleOrThisIsDisposed(handle);
		SetWindowFullscreenState(
			handle,
			style != WindowFullscreenStyle.NotFullscreen,
			style == WindowFullscreenStyle.FullscreenBorderless
		).ThrowIfFailure();
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_fullscreen_state")]
	static extern InteropResult GetWindowFullscreenState(WindowHandle handle, out InteropBool fullscreen, out InteropBool borderless);
	public WindowFullscreenStyle GetFullscreenState(WindowHandle handle) {
		ThrowIfHandleOrThisIsDisposed(handle);
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

	public bool IsDisposed(WindowHandle handle) => !_activeWindows.Contains(handle);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "dispose_window")]
	static extern InteropResult DisposeWindow(WindowHandle handle);
	public void Dispose(WindowHandle handle) {
		if (_isDisposed || IsDisposed(handle)) return;
		DisposeWindow(
			handle
		).ThrowIfFailure();
		_activeWindows.Remove(handle);
		_windowDisplayMap.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var ptr in _activeWindows) DisposeWindow(ptr).ThrowIfFailure();
			_activeWindows.Dispose();
			_windowDisplayMap.Dispose();
			_windowTitleBuffer.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
	void ThrowIfHandleOrThisIsDisposed(WindowHandle handle) {
		ObjectDisposedException.ThrowIf(IsDisposed(handle), handle);
		ThrowIfThisIsDisposed();
	}
}