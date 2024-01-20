// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Security;
using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Windowing;

[SuppressUnmanagedCodeSecurity]
sealed class NativeWindowBuilder : IWindowBuilder, IWindowHandleImplProvider {
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "create_window")]
	static extern InteropBool CreateWindow(out WindowHandle outResult, int width, int height, int xPos, int yPos);
	public WindowHandle Build(in WindowCreationConfig config) {
		CreateWindow(
			out var result,
			(int) (config.ScreenDimensions?.X ?? -1f),
			(int) (config.ScreenDimensions?.Y ?? -1f),
			(int) (config.ScreenLocation?.X ?? -1f),
			(int) (config.ScreenLocation?.Y ?? -1f)
		).ThrowIfFalse();
		return result;
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "dispose_window")]
	static extern InteropBool DisposeWindow(WindowHandle handle);
	public void Dispose(WindowHandle handle) {
		DisposeWindow(
			handle
		).ThrowIfFalse();
	}
}