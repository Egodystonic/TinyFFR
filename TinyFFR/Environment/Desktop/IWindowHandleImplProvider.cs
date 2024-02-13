// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Desktop;

interface IWindowHandleImplProvider {
	int GetTitleMaxLength();
	int GetTitle(WindowHandle handle, Span<char> dest);
	void SetTitle(WindowHandle handle, ReadOnlySpan<char> src);

	Display GetDisplay(WindowHandle handle);
	void SetDisplay(WindowHandle handle, Display newDisplay);

	XYPair<int> GetSize(WindowHandle handle);
	void SetSize(WindowHandle handle, XYPair<int> newDimensions);

	XYPair<int> GetPosition(WindowHandle handle);
	void SetPosition(WindowHandle handle, XYPair<int> newDimensions);

	WindowFullscreenStyle GetFullscreenState(WindowHandle handle);
	void SetFullscreenState(WindowHandle handle, WindowFullscreenStyle style);

	bool GetCursorLockState(WindowHandle handle);
	void SetCursorLockState(WindowHandle handle, bool state);

	bool IsDisposed(WindowHandle handle);
	void Dispose(WindowHandle handle);
}