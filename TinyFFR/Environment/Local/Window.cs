// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// Represents a live desktop window. Created via the factory's <see cref="IWindowBuilder"/>.
/// </summary>
public readonly struct Window : IDisposableResource<Window, IWindowImplProvider>, IRenderTarget {
	readonly ResourceHandle<Window> _handle;
	readonly IWindowImplProvider _impl;

	internal IWindowImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Window>();
	internal ResourceHandle<Window> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Window)) : _handle;

	IWindowImplProvider IResource<Window, IWindowImplProvider>.Implementation => Implementation;
	ResourceHandle<Window> IResource<Window>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// The <see cref="Local.Display"/> this window is currently shown on. Setting this moves the window to the given display, keeping its <see cref="Position"/> relative to that display's top-left corner.
	/// </summary>
	public Display Display {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDisplay(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetDisplay(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Display"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="display">The display to move this window to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetDisplay(Display display) => Display = display;

	/// <summary>
	/// The size of this window (<c>X</c> = width, <c>Y</c> = height).
	/// </summary>
	/// <remarks>
	/// This is the window's size as the operating system reports it, which on displays with scaling enabled may not be the same as the number of pixels actually
	/// rendered: for that, render targets expose their true back-buffer dimensions separately. Whilst this window's <see cref="FullscreenStyle"/> is
	/// <see cref="WindowFullscreenStyle.Fullscreen"/>, setting this instead changes the display's resolution, picking whichever of its
	/// <see cref="Local.Display.SupportedDisplayModes"/> is closest in total pixel count to the size given.
	/// </remarks>
	public XYPair<int> Size {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSize(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSize(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Size"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="size">The new size for this window.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetSize(XYPair<int> size) => Size = size;

	/// <summary>
	/// Where this window's top-left corner sits, measured in pixels from the top-left corner of the <see cref="Display"/> it is on, with <c>Y</c> increasing downward.
	/// </summary>
	/// <remarks>
	/// This position is relative to the window's own display, not to the desktop as a whole: on a multi-monitor setup, a window at <c>(0, 0)</c> sits in the
	/// top-left corner of whichever display it is currently on, regardless of how those displays are arranged relative to one another.
	/// </remarks>
	public XYPair<int> Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Position"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="position">The new position for this window.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(XYPair<int> position) => Position = position;

	/// <summary>
	/// Whether and how this window occupies its entire <see cref="Display"/>.
	/// </summary>
	public WindowFullscreenStyle FullscreenStyle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetFullscreenStyle(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetFullscreenStyle(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="FullscreenStyle"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="style">The new fullscreen style for this window.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetFullscreenStyle(WindowFullscreenStyle style) => FullscreenStyle = style;

	/// <summary>
	/// Whether the mouse cursor is locked to this window, hiding it and confining it so that it can not leave or be clicked outside the window.
	/// </summary>
	/// <remarks>
	/// This is the mode conventionally used for mouse-look controls. While the cursor is locked the operating system stops moving it, so
	/// <see cref="ILatestKeyboardAndMouseInputRetriever.MouseCursorPosition"/> stops changing; read
	/// <see cref="ILatestKeyboardAndMouseInputRetriever.MouseCursorDelta"/> instead to track how the user is moving the mouse.
	/// </remarks>
	public bool LockCursor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCursorLock(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCursorLock(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="LockCursor"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="lockCursor">The new cursor lock setting for this window.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetLockCursor(bool lockCursor) => LockCursor = lockCursor;

	/// <summary>
	/// The shape the mouse cursor takes while it is over this window.
	/// </summary>
	public MouseCursorStyle CursorStyle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCursorStyle(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCursorStyle(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="CursorStyle"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="style">The new cursor style for this window.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetCursorStyle(MouseCursorStyle style) => CursorStyle = style;

	/// <summary>
	/// Sets the icon shown for this window by the operating system (e.g. in the title bar and taskbar).
	/// </summary>
	/// <param name="iconFilePath">The file path of the image to use as the icon. The image must be no larger than 128 pixels in either dimension.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetIcon(ReadOnlySpan<char> iconFilePath) => Implementation.SetIcon(_handle, iconFilePath);

	XYPair<int> IRenderTarget.ViewportOffset => XYPair<int>.Zero;
	XYPair<int> IRenderTarget.ViewportDimensions => Implementation.GetViewportDimensions(_handle);

	internal bool IsMinimized {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIsMinimized(_handle);
	}

	internal Window(ResourceHandle<Window> handle, IWindowImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	/// <summary>
	/// Returns this window's title (the text the operating system shows in its title bar) as a newly-allocated <see cref="string"/>.
	/// </summary>
	/// <remarks>
	/// Prefer <see cref="GetTitleLength"/> and <see cref="CopyTitle"/> instead if you want to avoid allocating, for example to copy the title into a pooled or stack-allocated buffer.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetTitleAsNewStringObject() => Implementation.GetTitleAsNewStringObject(_handle);
	/// <summary>
	/// Returns the length, in characters, of this window's title.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTitleLength() => Implementation.GetTitleLength(_handle);
	/// <summary>
	/// Copies this window's title into <paramref name="destinationBuffer"/>, without allocating.
	/// </summary>
	/// <param name="destinationBuffer">The buffer to copy the title into. Must be at least <see cref="GetTitleLength"/> characters long.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTitle(Span<char> destinationBuffer) => Implementation.CopyTitle(_handle, destinationBuffer);

	string IStringSpanNameEnabled.GetNameAsNewStringObject() => GetTitleAsNewStringObject();
	int IStringSpanNameEnabled.GetNameLength() => GetTitleLength();
	void IStringSpanNameEnabled.CopyName(Span<char> destinationBuffer) => CopyTitle(destinationBuffer);

	/// <summary>
	/// Sets this window's title; i.e. the text the operating system shows in its title bar.
	/// </summary>
	/// <param name="newTitle">The new title for this window.</param>
	public void SetTitle(ReadOnlySpan<char> newTitle) => Implementation.SetTitle(_handle, newTitle);

	static Window IResource<Window>.CreateFromHandleAndImpl(ResourceHandle<Window> handle, IResourceImplProvider impl) {
		return new Window(handle, impl as IWindowImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<Window> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<Window> IResource<Window>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);
	#endregion

	/// <inheritdoc />
	public override string ToString() => IsDisposed ? $"{nameof(Window)} (Disposed)" : $"{nameof(Window)} \"{GetTitleAsNewStringObject()}\"";

	#region Equality
	/// <inheritdoc />
	public bool Equals(Window other) => _handle == other._handle && _impl == other._impl;
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Window other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	/// <summary>
	/// <see cref="Equals(Window)"/>
	/// </summary>
	public static bool operator ==(Window left, Window right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(Window)"/>
	/// </summary>
	public static bool operator !=(Window left, Window right) => !left.Equals(right);
	#endregion
}