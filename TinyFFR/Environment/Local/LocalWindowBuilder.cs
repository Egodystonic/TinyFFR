// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalWindowBuilder : IWindowBuilder, IWindowImplProvider, IDisposable {
	const string LogoResourceName = "logo_128.png";
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly InteropStringBuffer _windowTitleBuffer;
	readonly InteropStringBuffer _iconFilePathBuffer;
	readonly ArrayPoolBackedVector<ResourceHandle<Window>> _activeWindows = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Window>, Display> _displayMap = new();
	bool _isDisposed = false;

	public LocalWindowBuilder(LocalFactoryGlobalObjectGroup globals, WindowBuilderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_globals = globals;
		_windowTitleBuffer = new InteropStringBuffer(config.MaxWindowTitleLength, addOneForNullTerminator: true);
		_iconFilePathBuffer = new InteropStringBuffer(config.MaxIconFilePathLengthChars, addOneForNullTerminator: true);
	}

	public Window CreateWindow(in WindowConfig config) {
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
		if (!config.Title.IsEmpty) SetTitleOnWindow(result.Handle, config.Title);
		TrySetDefaultIcon(result.Handle);
		return result;
	}

	void TrySetDefaultIcon(ResourceHandle<Window> handle) {
		try {
			var iconData = EmbeddedResourceResolver.GetResource(LogoResourceName);
			SetWindowIconFromMemory(
				handle,
				iconData.DataPtr,
				iconData.DataLenBytes
			).ThrowIfFailure();
		}
#if DEBUG
		catch { throw; }
#else
#pragma warning disable CA1031 // "Don't swallow all exceptions" -- it's fine for this
		catch { /* do nothing, none of this is important enough to crash the app */ }
#pragma warning restore CA1031
#endif
	}

	public ReadOnlySpan<char> GetTitle(ResourceHandle<Window> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var maxSpanLength = _windowTitleBuffer.BufferLength;
		var dest = maxSpanLength <= 1000 ? stackalloc char[maxSpanLength] : new char[maxSpanLength];

		var numCharsWritten = ReadTitleFromWindow(handle, dest);
		_globals.ReplaceResourceName(handle.Ident, dest[..numCharsWritten]);
		return _globals.GetResourceName(handle.Ident, default);
	}
	public void SetTitle(ResourceHandle<Window> handle, ReadOnlySpan<char> newTitle) {
		SetTitleOnWindow(handle, newTitle);
		_globals.ReplaceResourceName(handle.Ident, newTitle);
	}
	public int ReadTitleFromWindow(ResourceHandle<Window> handle, Span<char> dest) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowTitle(
			handle,
			ref _windowTitleBuffer.BufferRef,
			_windowTitleBuffer.BufferLength
		).ThrowIfFailure();
		return _windowTitleBuffer.ConvertToUtf16(dest);
	}
	public void SetTitleOnWindow(ResourceHandle<Window> handle, ReadOnlySpan<char> src) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_windowTitleBuffer.ConvertFromUtf16(src);
		SetWindowTitle(
			handle,
			ref _windowTitleBuffer.BufferRef
		).ThrowIfFailure();
	}

	public void SetIcon(ResourceHandle<Window> handle, ReadOnlySpan<char> newIconFilePath) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_iconFilePathBuffer.ConvertFromUtf16(newIconFilePath);
		try {
			SetWindowIcon(
				handle,
				ref _iconFilePathBuffer.BufferRef
			).ThrowIfFailure();
		}
		catch (Exception e) {
			if (!File.Exists(newIconFilePath.ToString())) throw new InvalidOperationException($"File '{newIconFilePath}' does not exist.", e);
			throw;
		}
	}

	public Display GetDisplay(ResourceHandle<Window> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _displayMap[handle];
	}
	public void SetDisplay(ResourceHandle<Window> handle, Display newDisplay) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var localPos = GetPosition(handle);
		_displayMap[handle] = newDisplay;
		SetPosition(handle, localPos);
	}

	public XYPair<int> GetSize(ResourceHandle<Window> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowSize(
			handle,
			out var width,
			out var height
		).ThrowIfFailure();
		return new(width, height);
	}
	public void SetSize(ResourceHandle<Window> handle, XYPair<int> newSize) {
		ThrowIfThisOrHandleIsDisposed(handle);

		if (newSize.X < 0) throw new ArgumentOutOfRangeException(nameof(newSize), newSize, $"'{nameof(newSize.X)}' value must be positive or 0.");
		if (newSize.Y < 0) throw new ArgumentOutOfRangeException(nameof(newSize), newSize, $"'{nameof(newSize.Y)}' value must be positive or 0.");

		SetWindowSize(
			handle,
			newSize.X,
			newSize.Y
		).ThrowIfFailure();
	}

	public XYPair<int> GetPosition(ResourceHandle<Window> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowPosition(
			handle,
			out var x,
			out var y
		).ThrowIfFailure();
		return _displayMap[handle].TranslateGlobalWindowPositionToDisplayLocal(new(x, y));
	}
	public void SetPosition(ResourceHandle<Window> handle, XYPair<int> newPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var translatedPosition = _displayMap[handle].TranslateDisplayLocalWindowPositionToGlobal(newPosition);
		SetWindowPosition(
			handle,
			translatedPosition.X,
			translatedPosition.Y
		).ThrowIfFailure();
	}

	public WindowFullscreenStyle GetFullscreenStyle(ResourceHandle<Window> handle) {
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
	public void SetFullscreenStyle(ResourceHandle<Window> handle, WindowFullscreenStyle newStyle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetWindowFullscreenState(
			handle,
			newStyle != WindowFullscreenStyle.NotFullscreen,
			newStyle == WindowFullscreenStyle.FullscreenBorderless
		).ThrowIfFailure();
	}

	public bool GetCursorLock(ResourceHandle<Window> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowCursorLockState(
			handle,
			out var result
		).ThrowIfFailure();
		return result;
	}
	public void SetCursorLock(ResourceHandle<Window> handle, bool newLockSetting) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetWindowCursorLockState(
			handle,
			newLockSetting
		).ThrowIfFailure();
	}

	public override string ToString() => _isDisposed ? "TinyFFR Window Builder [Disposed]" : "TinyFFR Window Builder";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Window HandleToInstance(ResourceHandle<Window> h) => new(h, this);

	#region Disposal
	public void Dispose(ResourceHandle<Window> handle) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		try {
			DisposeWindow(handle).ThrowIfFailure();
		}
		finally {
			_displayMap.Remove(handle);
			_activeWindows.Remove(handle);
		}
	}
	public bool IsDisposed(ResourceHandle<Window> handle) {
		return _isDisposed || !_activeWindows.Contains(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var handle in _activeWindows) DisposeWindow(handle).ThrowIfFailure();
			_activeWindows.Dispose();
			_displayMap.Dispose();
			_iconFilePathBuffer.Dispose();
			_windowTitleBuffer.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Window> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Window));
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_window_icon")]
	static extern InteropResult SetWindowIcon(UIntPtr handle, ref readonly byte iconFilePathBufferPtr);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_window_icon_from_memory")]
	static extern InteropResult SetWindowIconFromMemory(UIntPtr handle, UIntPtr iconDataPtr, int iconDataLengthBytes);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_window")]
	static extern InteropResult DisposeWindow(UIntPtr handle);
	#endregion
}