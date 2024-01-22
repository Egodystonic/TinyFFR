// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Security;
using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Windowing;

[SuppressUnmanagedCodeSecurity]
sealed class NativeWindowBuilder : IWindowBuilder, IWindowHandleImplProvider, IDisposable {
	const int InitialWindowHandleTrackingSpace = 20;
	readonly HashSet<WindowPtr> _activeWindows = new(InitialWindowHandleTrackingSpace);
	readonly InteropStringBuffer _windowTitleBuffer;
	bool _isDisposed = false;

	public NativeWindowBuilder(WindowBuilderCreationConfig config) {
		_windowTitleBuffer = new InteropStringBuffer(config.MaxWindowTitleLength + 1); // + 1 for null terminator
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "create_window")]
	static extern InteropResult CreateWindow(out WindowPtr outPtr, int width, int height, int xPos, int yPos);
	public Window Build(in WindowCreationConfig config) {
		ThrowIfThisIsDisposed();
		CreateWindow(
			out var outPtr,
			(int) (config.Size?.X ?? -1f),
			(int) (config.Size?.Y ?? -1f),
			(int) (config.Position?.X ?? -1f),
			(int) (config.Position?.Y ?? -1f)
		).ThrowIfFailure();
		_activeWindows.Add(outPtr);
		SetTitle(outPtr, config.Title);
		return new(outPtr, this);
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_title")]
	static extern InteropResult SetWindowTitle(WindowPtr ptr, ref readonly byte utf8BufferPtr);
	public void SetTitle(WindowPtr ptr, ReadOnlySpan<char> src) {
		ThrowIfPointerOrThisIsDisposed(ptr);
		_windowTitleBuffer.WriteFrom(src);
		SetWindowTitle(
			ptr, 
			ref _windowTitleBuffer.BufferRef
		).ThrowIfFailure();
	}

	public int GetTitleMaxLength() => _windowTitleBuffer.BufferLength;

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_title")]
	static extern InteropResult GetWindowTitle(WindowPtr ptr, ref byte utf8BufferPtr, int bufferLength);
	public int GetTitle(WindowPtr ptr, Span<char> dest) {
		ThrowIfPointerOrThisIsDisposed(ptr);
		GetWindowTitle(
			ptr,
			ref _windowTitleBuffer.BufferRef,
			_windowTitleBuffer.BufferLength
		).ThrowIfFailure();
		return _windowTitleBuffer.ReadTo(dest);
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_size")]
	static extern InteropResult SetWindowSize(WindowPtr ptr, int newWidth, int newHeight);
	public void SetSize(WindowPtr ptr, XYPair newDimensions) {
		ThrowIfPointerOrThisIsDisposed(ptr);

		var newWidth = (int) newDimensions.X;
		var newHeight = (int) newDimensions.Y;

		if (newWidth < 0) throw new ArgumentOutOfRangeException(nameof(newDimensions), newDimensions, $"'{nameof(XYPair.X)}' value must be positive or 0.");
		if (newHeight < 0) throw new ArgumentOutOfRangeException(nameof(newDimensions), newDimensions, $"'{nameof(XYPair.Y)}' value must be positive or 0.");

		SetWindowSize(
			ptr,
			newWidth,
			newHeight
		).ThrowIfFailure();
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_size")]
	static extern InteropResult GetWindowSize(WindowPtr ptr, out int outWidth, out int outHeight);
	public XYPair GetSize(WindowPtr ptr) {
		ThrowIfPointerOrThisIsDisposed(ptr);
		GetWindowSize(
			ptr,
			out var width,
			out var height
		).ThrowIfFailure();
		return new(width, height);
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_window_position")]
	static extern InteropResult SetWindowPosition(WindowPtr ptr, int newX, int newY);
	public void SetPosition(WindowPtr ptr, XYPair newPosition) {
		ThrowIfPointerOrThisIsDisposed(ptr);
		SetWindowPosition(
			ptr,
			(int) newPosition.X,
			(int) newPosition.Y
		).ThrowIfFailure();
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_window_position")]
	static extern InteropResult GetWindowPosition(WindowPtr ptr, out int outX, out int outY);
	public XYPair GetPosition(WindowPtr ptr) {
		ThrowIfPointerOrThisIsDisposed(ptr);
		GetWindowPosition(
			ptr,
			out var x,
			out var y
		).ThrowIfFailure();
		return new(x, y);
	}

	public bool IsDisposed(WindowPtr ptr) => !_activeWindows.Contains(ptr);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "dispose_window")]
	static extern InteropResult DisposeWindow(WindowPtr ptr);
	public void Dispose(WindowPtr ptr) {
		if (_isDisposed || IsDisposed(ptr)) return;
		DisposeWindow(
			ptr
		).ThrowIfFailure();
		_activeWindows.Remove(ptr);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var ptr in _activeWindows) DisposeWindow(ptr).ThrowIfFailure();
			_activeWindows.Clear();
			_windowTitleBuffer.Dispose();
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
}