// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

public interface IWindowImplProvider : IDisposableResourceImplProvider<Window> {
	ReadOnlySpan<char> GetTitle(ResourceHandle<Window> handle);
	ReadOnlySpan<char> IResourceImplProvider<Window>.GetName(ResourceHandle<Window> handle) => GetTitle(handle);
	void SetTitle(ResourceHandle<Window> handle, ReadOnlySpan<char> src);

	Display GetDisplay(ResourceHandle<Window> handle);
	void SetDisplay(ResourceHandle<Window> handle, Display newDisplay);

	XYPair<int> GetSize(ResourceHandle<Window> handle);
	void SetSize(ResourceHandle<Window> handle, XYPair<int> newSize);

	XYPair<int> GetPosition(ResourceHandle<Window> handle);
	void SetPosition(ResourceHandle<Window> handle, XYPair<int> newPosition);

	WindowFullscreenStyle GetFullscreenStyle(ResourceHandle<Window> handle);
	void SetFullscreenStyle(ResourceHandle<Window> handle, WindowFullscreenStyle newStyle);

	bool GetCursorLock(ResourceHandle<Window> handle);
	void SetCursorLock(ResourceHandle<Window> handle, bool newLockSetting);
}