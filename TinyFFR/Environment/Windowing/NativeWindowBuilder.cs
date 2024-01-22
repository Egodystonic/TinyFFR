// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Security;
using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Windowing;

[SuppressUnmanagedCodeSecurity]
sealed class NativeWindowBuilder : IWindowBuilder, IWindowHandleImplProvider, IDisposable {
	const int InitialWindowHandleTrackingSpace = 20;
	readonly HashSet<WindowHandle> _activeWindows = new(InitialWindowHandleTrackingSpace);
	readonly InteropStringBuffer _windowTitleBuffer;
	bool _isDisposed = false;

	public IReadOnlyCollection<WindowHandle> ActiveWindows => _activeWindows;

	public NativeWindowBuilder(WindowBuilderCreationConfig config) {
		if (config.MaxWindowTitleLength <= 0) throw new ArgumentOutOfRangeException(nameof(config.MaxWindowTitleLength), config.MaxWindowTitleLength, "Max window title length must be positive.");
		_windowTitleBuffer = new InteropStringBuffer(config.MaxWindowTitleLength + 1); // + 1 for null terminator
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "create_window")]
	static extern InteropBool CreateWindow(out WindowPtr outPtr, int width, int height, int xPos, int yPos);
	public WindowHandle Build(in WindowCreationConfig config) {
		ThrowIfThisIsDisposed();
		CreateWindow(
			out var outPtr,
			(int) (config.ScreenDimensions?.X ?? -1f),
			(int) (config.ScreenDimensions?.Y ?? -1f),
			(int) (config.ScreenLocation?.X ?? -1f),
			(int) (config.ScreenLocation?.Y ?? -1f)
		).ThrowIfFalse();
		var result = PointerToHandle(outPtr);
		_activeWindows.Add(result);

		SetTitle(outPtr, config.Title);
		return result;
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_title")]
	static extern InteropBool SetWindowTitle(WindowPtr ptr, ref readonly byte utf8BufferPtr);
	public void SetTitle(WindowPtr ptr, ReadOnlySpan<char> src) {
		ThrowIfPointerOrThisIsDisposed(ptr);
		_windowTitleBuffer.WriteFrom(src);
		SetWindowTitle(
			ptr, 
			ref _windowTitleBuffer.BufferRef
		).ThrowIfFalse();
	}

	public int GetTitleMaxLength() => _windowTitleBuffer.BufferLength;

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_title")]
	static extern InteropBool GetWindowTitle(WindowPtr ptr, ref byte utf8BufferPtr, int bufferLength);
	public int GetTitle(WindowPtr ptr, Span<char> dest) {
		ThrowIfPointerOrThisIsDisposed(ptr);
		GetWindowTitle(
			ptr,
			ref _windowTitleBuffer.BufferRef,
			_windowTitleBuffer.BufferLength
		).ThrowIfFalse();
		return _windowTitleBuffer.ReadTo(dest);
	}

	public bool IsDisposed(WindowPtr ptr) => !_activeWindows.Contains(PointerToHandle(ptr));

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "dispose_window")]
	static extern InteropBool DisposeWindow(WindowPtr ptr);
	public void Dispose(WindowPtr ptr) {
		ThrowIfPointerOrThisIsDisposed(ptr);
		DisposeWindow(
			ptr
		).ThrowIfFalse();
		_activeWindows.Remove(PointerToHandle(ptr));
	}

	public void Dispose() {
		if (_isDisposed) throw new InvalidOperationException("Build has already been disposed.");
		try {
			foreach (var ow in _activeWindows) ow.Dispose();
			_activeWindows.Clear();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		if (_isDisposed) throw new InvalidOperationException("Builder has been disposed.");
	}
	void ThrowIfPointerOrThisIsDisposed(WindowPtr ptr) {
		if (IsDisposed(ptr)) throw new InvalidOperationException("Window has been disposed.");
		ThrowIfThisIsDisposed();
	}
	WindowHandle PointerToHandle(WindowPtr ptr) => new(ptr, this);
}