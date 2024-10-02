// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

public interface IWindowImplProvider : IDisposableResourceImplProvider<WindowHandle> {
	string GetTitle(WindowHandle handle);
	string IResourceImplProvider<WindowHandle>.GetName(WindowHandle handle) => GetTitle(handle);
	void SetTitle(WindowHandle handle, string newTitle);

	int GetTitleUsingSpan(WindowHandle handle, Span<char> dest);
	int IResourceImplProvider<WindowHandle>.GetNameUsingSpan(WindowHandle handle, Span<char> dest) => GetTitleUsingSpan(handle, dest);
	void SetTitleUsingSpan(WindowHandle handle, ReadOnlySpan<char> src);
	int GetTitleSpanLength(WindowHandle handle);
	int IResourceImplProvider<WindowHandle>.GetNameSpanLength(WindowHandle handle) => GetTitleSpanLength(handle);

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
}