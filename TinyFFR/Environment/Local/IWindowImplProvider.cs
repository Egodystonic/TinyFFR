// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Local;

public unsafe interface IWindowImplProvider {
	string GetTitle(WindowHandle handle);
	void SetTitle(WindowHandle handle, string newTitle);

	int GetTitleUsingSpan(WindowHandle handle, Span<char> dest);
	void SetTitleUsingSpan(WindowHandle handle, ReadOnlySpan<char> src);
	int GetTitleSpanMaxLength(WindowHandle handle);

	Display GetDisplay(WindowHandle handle);
	void SetDisplay(WindowHandle handle, Display newDisplay);

	XYPair<int> GetSize(WindowHandle handle);
	void SetSize(WindowHandle handle, XYPair<int> newSize);

	XYPair<int> GetPosition(WindowHandle handle);
	void SetPosition(WindowHandle handle, XYPair<int> newPosition);

	WindowFullscreenStyle GetFullscreenStyle(WindowHandle handle);
	void SetFullscreenStyle(WindowHandle handle, WindowFullscreenStyle newStyle);

	bool GetCursorLock(WindowHandle handle);
	void SetCursorLock(WindowHandle handle, bool newLockSetting);

	bool IsDisposed(WindowHandle handle);
	void Dispose(WindowHandle handle);
}