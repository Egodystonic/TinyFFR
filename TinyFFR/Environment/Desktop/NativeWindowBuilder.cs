// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Desktop;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class NativeWindowBuilder : IWindowBuilder, IDisposable {
	readonly InteropStringBuffer _windowTitleBuffer;
	bool _isDisposed = false;

	public NativeWindowBuilder(WindowBuilderConfig config) {
		_windowTitleBuffer = new InteropStringBuffer(config.MaxWindowTitleLength, true);
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
		var result = new Window(outHandle, config.Display, _windowTitleBuffer);
		result.FullscreenStyle = config.FullscreenStyle;
		return result;
	}

	public override string ToString() => _isDisposed ? "TinyFFR Native Window Builder [Disposed]" : "TinyFFR Native Window Builder";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			_windowTitleBuffer.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion

	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "create_window")]
	static extern InteropResult CreateWindow(out WindowHandle outHandle, int width, int height, int xPos, int yPos);
	#endregion
}