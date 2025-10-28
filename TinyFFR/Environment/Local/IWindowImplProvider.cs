// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

public interface IWindowImplProvider : IDisposableResourceImplProvider<Window> {
	string GetTitleAsNewStringObject(ResourceHandle<Window> handle);
	int GetTitleLength(ResourceHandle<Window> handle);
	void CopyTitle(ResourceHandle<Window> handle, Span<char> destinationBuffer);
	void SetTitle(ResourceHandle<Window> handle, ReadOnlySpan<char> newTitle);

	string IResourceImplProvider<Window>.GetNameAsNewStringObject(ResourceHandle<Window> handle) => GetTitleAsNewStringObject(handle);
	int IResourceImplProvider<Window>.GetNameLength(ResourceHandle<Window> handle) => GetTitleLength(handle);
	void IResourceImplProvider<Window>.CopyName(ResourceHandle<Window> handle, Span<char> destinationBuffer) => CopyTitle(handle, destinationBuffer);

	void SetIcon(ResourceHandle<Window> handle, ReadOnlySpan<char> filePath);

	Display GetDisplay(ResourceHandle<Window> handle);
	void SetDisplay(ResourceHandle<Window> handle, Display newDisplay);

	XYPair<int> GetSize(ResourceHandle<Window> handle);
	void SetSize(ResourceHandle<Window> handle, XYPair<int> newSize);
	XYPair<int> GetViewportDimensions(ResourceHandle<Window> handle);

	XYPair<int> GetPosition(ResourceHandle<Window> handle);
	void SetPosition(ResourceHandle<Window> handle, XYPair<int> newPosition);

	WindowFullscreenStyle GetFullscreenStyle(ResourceHandle<Window> handle);
	void SetFullscreenStyle(ResourceHandle<Window> handle, WindowFullscreenStyle newStyle);

	bool GetCursorLock(ResourceHandle<Window> handle);
	void SetCursorLock(ResourceHandle<Window> handle, bool newLockSetting);
}