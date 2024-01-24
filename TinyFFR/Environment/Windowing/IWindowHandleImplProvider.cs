// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Desktop;

namespace Egodystonic.TinyFFR.Environment.Windowing;

interface IWindowHandleImplProvider {
	int GetTitleMaxLength();
	int GetTitle(WindowHandle handle, Span<char> dest);
	void SetTitle(WindowHandle handle, ReadOnlySpan<char> src);

	Monitor GetMonitor(WindowHandle handle);
	void SetMonitor(WindowHandle handle, Monitor newMonitor);

	XYPair GetSize(WindowHandle handle);
	void SetSize(WindowHandle handle, XYPair newDimensions);

	XYPair GetPosition(WindowHandle handle);
	void SetPosition(WindowHandle handle, XYPair newDimensions);

	WindowFullscreenStyle GetFullscreenState(WindowHandle handle);
	void SetFullscreenState(WindowHandle handle, WindowFullscreenStyle style);

	bool IsDisposed(WindowHandle handle);
	void Dispose(WindowHandle handle);
}