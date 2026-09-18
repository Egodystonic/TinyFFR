// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="Window"/> resources.
/// </summary>
public interface IWindowImplProvider : IDisposableResourceImplProvider<Window> {
	/// <summary>
	/// Invoked via <see cref="Window.GetTitleAsNewStringObject"/>.
	/// </summary>
	string GetTitleAsNewStringObject(ResourceHandle<Window> handle);
	/// <summary>
	/// Invoked via <see cref="Window.GetTitleLength"/>.
	/// </summary>
	int GetTitleLength(ResourceHandle<Window> handle);
	/// <summary>
	/// Invoked via <see cref="Window.CopyTitle"/>.
	/// </summary>
	void CopyTitle(ResourceHandle<Window> handle, Span<char> destinationBuffer);
	/// <summary>
	/// Invoked via <see cref="Window.SetTitle"/>.
	/// </summary>
	void SetTitle(ResourceHandle<Window> handle, ReadOnlySpan<char> newTitle);

	string IResourceImplProvider<Window>.GetNameAsNewStringObject(ResourceHandle<Window> handle) => GetTitleAsNewStringObject(handle);
	int IResourceImplProvider<Window>.GetNameLength(ResourceHandle<Window> handle) => GetTitleLength(handle);
	void IResourceImplProvider<Window>.CopyName(ResourceHandle<Window> handle, Span<char> destinationBuffer) => CopyTitle(handle, destinationBuffer);

	/// <summary>
	/// Invoked via <see cref="Window.SetIcon"/>.
	/// </summary>
	void SetIcon(ResourceHandle<Window> handle, ReadOnlySpan<char> filePath);

	/// <summary>
	/// Invoked via <see cref="Window.Display"/>.
	/// </summary>
	Display GetDisplay(ResourceHandle<Window> handle);
	/// <summary>
	/// Invoked via <see cref="Window.Display"/>.
	/// </summary>
	void SetDisplay(ResourceHandle<Window> handle, Display newDisplay);

	/// <summary>
	/// Invoked via <see cref="Window.Size"/>.
	/// </summary>
	XYPair<int> GetSize(ResourceHandle<Window> handle);
	/// <summary>
	/// Invoked via <see cref="Window.Size"/>.
	/// </summary>
	void SetSize(ResourceHandle<Window> handle, XYPair<int> newSize);
	/// <summary>
	/// Invoked via <see cref="Rendering.IRenderTarget.ViewportDimensions"/> on a <see cref="Window"/>. Unlike <see cref="GetSize"/> this returns the window's true size in
	/// pixels, which differs from its size in the operating system's window coordinates on displays that have scaling enabled.
	/// </summary>
	XYPair<int> GetViewportDimensions(ResourceHandle<Window> handle);
	/// <summary>
	/// Invoked internally to determine whether the given window is currently minimized, in which case there is no point rendering to it.
	/// </summary>
	bool GetIsMinimized(ResourceHandle<Window> handle);

	/// <summary>
	/// Invoked via <see cref="Window.Position"/>.
	/// </summary>
	XYPair<int> GetPosition(ResourceHandle<Window> handle);
	/// <summary>
	/// Invoked via <see cref="Window.Position"/>.
	/// </summary>
	void SetPosition(ResourceHandle<Window> handle, XYPair<int> newPosition);

	/// <summary>
	/// Invoked via <see cref="Window.FullscreenStyle"/>.
	/// </summary>
	WindowFullscreenStyle GetFullscreenStyle(ResourceHandle<Window> handle);
	/// <summary>
	/// Invoked via <see cref="Window.FullscreenStyle"/>.
	/// </summary>
	void SetFullscreenStyle(ResourceHandle<Window> handle, WindowFullscreenStyle newStyle);

	/// <summary>
	/// Invoked via <see cref="Window.LockCursor"/>.
	/// </summary>
	bool GetCursorLock(ResourceHandle<Window> handle);
	/// <summary>
	/// Invoked via <see cref="Window.LockCursor"/>.
	/// </summary>
	void SetCursorLock(ResourceHandle<Window> handle, bool newLockSetting);

	/// <summary>
	/// Invoked via <see cref="Window.CursorStyle"/>.
	/// </summary>
	MouseCursorStyle GetCursorStyle(ResourceHandle<Window> handle);
	/// <summary>
	/// Invoked via <see cref="Window.CursorStyle"/>.
	/// </summary>
	void SetCursorStyle(ResourceHandle<Window> handle, MouseCursorStyle newStyle);
}