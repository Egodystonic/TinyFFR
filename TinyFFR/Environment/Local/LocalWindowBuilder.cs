// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local;
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
	readonly ArrayPoolBackedMap<ResourceHandle<Window>, Display> _lastSetDisplayMap = new();
	readonly RenderingBackendApi _actualRenderingApi;
	readonly ReadOnlyMemory<Display> _displaysOrderedByIndex;
	bool _isDisposed = false;

	public LocalWindowBuilder(LocalFactoryGlobalObjectGroup globals, WindowBuilderConfig config, RenderingBackendApi actualRenderingApi, ReadOnlyMemory<Display> displaysOrderedByIndex) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_actualRenderingApi = actualRenderingApi;
		_displaysOrderedByIndex = displaysOrderedByIndex;
		_globals = globals;
		_windowTitleBuffer = new InteropStringBuffer(config.MaxWindowTitleLength, addOneForNullTerminator: true);
		_iconFilePathBuffer = new InteropStringBuffer(config.MaxIconFilePathLengthChars, addOneForNullTerminator: true);
	}

	public Window CreateWindow(in WindowCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		var globalPosition = config.Display.TranslateDisplayLocalWindowPositionToGlobal(config.Position);
		CreateWindow(
			out var outHandle,
			config.Size.X,
			config.Size.Y,
			globalPosition.X,
			globalPosition.Y,
			(int) _actualRenderingApi
		).ThrowIfFailure();
		var result = new Window(outHandle, this);
		_activeWindows.Add(outHandle);
		_lastSetDisplayMap.Add(outHandle, config.Display);
		result.FullscreenStyle = config.FullscreenStyle;
		if (!config.Title.IsEmpty) SetTitleOnWindow(result.Handle, config.Title);
		SetDefaultIcon(result.Handle);
		return result;
	}

	void SetDefaultIcon(ResourceHandle<Window> handle) {
		var iconData = EmbeddedResourceResolver.GetResource(LogoResourceName);
		SetWindowIconFromMemory(
			handle,
			iconData.DataPtr,
			iconData.DataLenBytes
		).ThrowIfFailure();
	}

	void ReadTitleFromWindow(ResourceHandle<Window> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);

		var maxSpanLength = _windowTitleBuffer.Length;
		var dest = maxSpanLength <= 1000 ? stackalloc char[maxSpanLength] : new char[maxSpanLength];

		GetWindowTitle(
			handle,
			ref _windowTitleBuffer.AsRef,
			_windowTitleBuffer.Length
		).ThrowIfFailure();

		var numChars = _windowTitleBuffer.ConvertToUtf16(dest);
		_globals.ReplaceResourceName(handle.Ident, dest[..numChars]);
	}
	void SetTitleOnWindow(ResourceHandle<Window> handle, ReadOnlySpan<char> src) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		_windowTitleBuffer.ConvertFromUtf16(src);
		SetWindowTitle(
			handle,
			ref _windowTitleBuffer.AsRef
		).ThrowIfFailure();
	}
	public string GetTitleAsNewStringObject(ResourceHandle<Window> handle) {
		ReadTitleFromWindow(handle);
		return new String(_globals.GetResourceName(handle.Ident, default));
	}
	public int GetTitleLength(ResourceHandle<Window> handle) {
		ReadTitleFromWindow(handle);
		return _globals.GetResourceName(handle.Ident, default).Length;
	}
	public void CopyTitle(ResourceHandle<Window> handle, Span<char> destinationBuffer) {
		ReadTitleFromWindow(handle);
		_globals.CopyResourceName(handle.Ident, default, destinationBuffer);
	}
	public void SetTitle(ResourceHandle<Window> handle, ReadOnlySpan<char> newTitle) {
		SetTitleOnWindow(handle, newTitle);
		_globals.ReplaceResourceName(handle.Ident, newTitle);
	}

	public void SetIcon(ResourceHandle<Window> handle, ReadOnlySpan<char> newIconFilePath) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_iconFilePathBuffer.ConvertFromUtf16(newIconFilePath);
		try {
			SetWindowIcon(
				handle,
				ref _iconFilePathBuffer.AsRef
			).ThrowIfFailure();
		}
		catch (Exception e) {
			if (!File.Exists(newIconFilePath.ToString())) throw new InvalidOperationException($"File '{newIconFilePath}' does not exist.", e);
			throw;
		}
	}

	public Display GetDisplay(ResourceHandle<Window> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		GetWindowDisplayIndex(
			handle,
			out var index
		).ThrowIfFailure();
		if (index < 0 || index >= _displaysOrderedByIndex.Length) return _lastSetDisplayMap[handle];
		return _displaysOrderedByIndex.Span[index];
	}
	public void SetDisplay(ResourceHandle<Window> handle, Display newDisplay) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var localPos = GetPosition(handle);
		_lastSetDisplayMap[handle] = newDisplay;
		SetPosition(handle, localPos, newDisplay);
	}

	public XYPair<int> GetSize(ResourceHandle<Window> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);

		var fsStyle = GetFullscreenStyle(handle);
		if (fsStyle is WindowFullscreenStyle.Fullscreen) {
			GetWindowFullscreenMode(
				handle,
				out var fsWidth,
				out var fsHeight,
				out _
			).ThrowIfFailure();
			// Sometimes we can't get this (usually on e.g. Wayland with its weirdness when one window overlaps two displays)
			// In that case width/height will be -1. In those cases, we will just use GetWindowSize
			if (fsWidth >= 0 && fsHeight >= 0) return new(fsWidth, fsHeight);
		}
		
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
		
		// =========== Linux =========== 
		if (OperatingSystem.IsLinux()) {
			var fsStyle = GetFullscreenStyle(handle);
			
			if (fsStyle is WindowFullscreenStyle.Fullscreen or WindowFullscreenStyle.FullscreenBorderless) {
				var targetArea = newSize.Area;

				var display = GetDisplay(handle);
				var bestMatch = new DisplayMode(XYPair<int>.Zero, 0);
				var bestMatchAreaDelta = targetArea;
				var bestMatchIndex = -1;

				for (var i = 0; i < display.SupportedDisplayModes.Length; ++i) {
					var thisMode = display.SupportedDisplayModes[i];
					var thisModeArea = display.SupportedDisplayModes[i].Resolution.Area;
					var thisModeAreaDelta = Math.Abs(thisModeArea - targetArea);
					if (thisModeAreaDelta < bestMatchAreaDelta || (thisModeAreaDelta == bestMatchAreaDelta && thisMode.RefreshRateHz > bestMatch.RefreshRateHz)) {
						bestMatch = thisMode;
						bestMatchAreaDelta = thisModeAreaDelta;
						bestMatchIndex = i;
					}
				}

				if (bestMatchIndex >= 0) {
					newSize = display.SupportedDisplayModes[bestMatchIndex].Resolution;
					if (newSize == display.HighestSupportedResolutionMode.Resolution) {
						SetWindowFullscreenState(
							handle,
							false,
							false
						).ThrowIfFailure();
						
						SetWindowSize(
							handle,
							1,
							1
						).ThrowIfFailure();
						
						SetWindowFullscreenState(
							handle,
							true,
							true
						).ThrowIfFailure();
						return;
					}
					
					SetWindowFullscreenState(
						handle,
						true,
						false
					).ThrowIfFailure();
					
					SetWindowFullscreenMode(
						handle,
						display.Handle,
						bestMatchIndex
					).ThrowIfFailure();
				}
			}

			SetWindowSize(
				handle,
				newSize.X,
				newSize.Y
			).ThrowIfFailure();
		}
		// =========== Windows / MacOS ===========
		else {
			var fsStyle = GetFullscreenStyle(handle);
			if (fsStyle is WindowFullscreenStyle.Fullscreen) {
				var targetArea = newSize.Area;

				var display = GetDisplay(handle);
				var bestMatch = new DisplayMode(XYPair<int>.Zero, 0);
				var bestMatchAreaDelta = targetArea;
				var bestMatchIndex = -1;

				for (var i = 0; i < display.SupportedDisplayModes.Length; ++i) {
					var thisMode = display.SupportedDisplayModes[i];
					var thisModeArea = display.SupportedDisplayModes[i].Resolution.Area;
					var thisModeAreaDelta = Math.Abs(thisModeArea - targetArea);
					if (thisModeAreaDelta < bestMatchAreaDelta || (thisModeAreaDelta == bestMatchAreaDelta && thisMode.RefreshRateHz > bestMatch.RefreshRateHz)) {
						bestMatch = thisMode;
						bestMatchAreaDelta = thisModeAreaDelta;
						bestMatchIndex = i;
					}
				}

				if (bestMatchIndex >= 0) {
					SetWindowFullscreenMode(
						handle,
						display.Handle,
						bestMatchIndex
					).ThrowIfFailure();
				}
			}

			SetWindowSize(
				handle,
				newSize.X,
				newSize.Y
			).ThrowIfFailure();
		}
	}
	public XYPair<int> GetViewportDimensions(ResourceHandle<Window> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowBackBufferSizeActual(
			handle,
			out var outWidth,
			out var outHeight
		).ThrowIfFailure();

		return new(outWidth, outHeight);
	}

	public XYPair<int> GetPosition(ResourceHandle<Window> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetWindowPosition(
			handle,
			out var x,
			out var y
		).ThrowIfFailure();
		return GetDisplay(handle).TranslateGlobalWindowPositionToDisplayLocal(new(x, y));
	}
	public void SetPosition(ResourceHandle<Window> handle, XYPair<int> newPosition) => SetPosition(handle, newPosition, GetDisplay(handle));
	public void SetPosition(ResourceHandle<Window> handle, XYPair<int> newPosition, Display display) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		var translatedPosition = display.TranslateDisplayLocalWindowPositionToGlobal(newPosition);
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
		var existingSize = GetSize(handle);
		var existingStyle = GetFullscreenStyle(handle);
		
		// Wayland permits only borderless mode if you want the native resolution; and only native resolution as a precursor to borderless
		if (OperatingSystem.IsLinux()) {
			var curVpSizeIsMaxRes = GetViewportDimensions(handle) == GetDisplay(handle).HighestSupportedResolutionMode.Resolution;
			if (newStyle is WindowFullscreenStyle.Fullscreen && curVpSizeIsMaxRes) {
				newStyle = WindowFullscreenStyle.FullscreenBorderless;
			}
			else if (newStyle is WindowFullscreenStyle.FullscreenBorderless && !curVpSizeIsMaxRes) {
				SetWindowFullscreenState(
					handle,
					false,
					false
				).ThrowIfFailure();
				
				SetWindowSize(
					handle,
					1,
					1
				).ThrowIfFailure();
				
				SetWindowFullscreenState(
					handle,
					true,
					true
				).ThrowIfFailure();
				
				return;
			}
		}
		
		SetWindowFullscreenState(
			handle,
			newStyle != WindowFullscreenStyle.NotFullscreen,
			newStyle == WindowFullscreenStyle.FullscreenBorderless
		).ThrowIfFailure();	
		
		// If we're swapping between fullscreen and non-fullscreen styles we have to re-set the size according to the right method
		if ((newStyle == WindowFullscreenStyle.Fullscreen) != (existingStyle == WindowFullscreenStyle.Fullscreen)) {
			SetSize(handle, existingSize);
		}
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
			_lastSetDisplayMap.Remove(handle);
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
			_lastSetDisplayMap.Dispose();
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
	static extern InteropResult CreateWindow(out UIntPtr outHandle, int width, int height, int xPos, int yPos, int renderingApiIndex);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_window_size")]
	static extern InteropResult GetWindowSize(UIntPtr handle, out int outWidth, out int outHeight);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_window_display_index")]
	static extern InteropResult GetWindowDisplayIndex(UIntPtr handle, out int displayIndex);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_window_size")]
	static extern InteropResult SetWindowSize(UIntPtr handle, int newWidth, int newHeight);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_window_fullscreen_display_mode")]
	static extern InteropResult GetWindowFullscreenMode(UIntPtr handle, out int outWidth, out int outHeight, out int outRefreshRateHz);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_window_fullscreen_display_mode")]
	static extern InteropResult SetWindowFullscreenMode(UIntPtr handle, UIntPtr displayHandle, int displayModeIndex);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_window_back_buffer_size_actual")]
	static extern InteropResult GetWindowBackBufferSizeActual(UIntPtr handle, out int outWidth, out int outHeight);

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