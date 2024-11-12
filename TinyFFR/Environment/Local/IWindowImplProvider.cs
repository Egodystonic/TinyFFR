// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

public interface IWindowImplProvider : IDisposableResourceImplProvider<WindowHandle> {
	ReadOnlySpan<char> GetTitle(WindowHandle handle);
	ReadOnlySpan<char> IResourceImplProvider<WindowHandle>.GetName(WindowHandle handle) => GetTitle(handle);
	void SetTitle(WindowHandle handle, ReadOnlySpan<char> src);

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