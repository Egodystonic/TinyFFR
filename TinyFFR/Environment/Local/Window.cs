// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly struct Window : IDisposableResource<Window, IWindowImplProvider>, IRenderTarget {
	readonly ResourceHandle<Window> _handle;
	readonly IWindowImplProvider _impl;

	internal IWindowImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Window>();
	internal ResourceHandle<Window> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Window)) : _handle;

	IWindowImplProvider IResource<Window, IWindowImplProvider>.Implementation => Implementation;
	ResourceHandle<Window> IResource<Window>.Handle => Handle;

	public Display Display {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDisplay(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetDisplay(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetDisplay(Display display) => Display = display;

	public XYPair<int> Size {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSize(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSize(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetSize(XYPair<int> size) => Size = size;

	public XYPair<int> Position { // TODO explain in XMLDoc that this is relative positioning on the selected Display
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(XYPair<int> position) => Position = position;

	public WindowFullscreenStyle FullscreenStyle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetFullscreenStyle(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetFullscreenStyle(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetFullscreenStyle(WindowFullscreenStyle style) => FullscreenStyle = style;

	public bool LockCursor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCursorLock(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCursorLock(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetLockCursor(bool lockCursor) => LockCursor = lockCursor;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // TODO xmldoc the icon must be no larger than 128px in either dimension
	public void SetIcon(ReadOnlySpan<char> iconFilePath) => Implementation.SetIcon(_handle, iconFilePath);

	XYPair<int> IRenderTarget.ViewportOffset => XYPair<int>.Zero;
	XYPair<int> IRenderTarget.ViewportDimensions => Size;

	internal Window(ResourceHandle<Window> handle, IWindowImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetTitleAsNewStringObject() => Implementation.GetTitleAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTitleLength() => Implementation.GetTitleLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTitle(Span<char> destinationBuffer) => Implementation.CopyTitle(_handle, destinationBuffer);

	string IStringSpanNameEnabled.GetNameAsNewStringObject() => GetTitleAsNewStringObject();
	int IStringSpanNameEnabled.GetNameLength() => GetTitleLength();
	void IStringSpanNameEnabled.CopyName(Span<char> destinationBuffer) => CopyTitle(destinationBuffer);

	public void SetTitle(ReadOnlySpan<char> newTitle) => Implementation.SetTitle(_handle, newTitle);

	static Window IResource<Window>.CreateFromHandleAndImpl(ResourceHandle<Window> handle, IResourceImplProvider impl) {
		return new Window(handle, impl as IWindowImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);
	#endregion

	public override string ToString() => IsDisposed ? $"{nameof(Window)} (Disposed)" : $"{nameof(Window)} \"{GetTitleAsNewStringObject()}\"";

	#region Equality
	public bool Equals(Window other) => _handle == other._handle && _impl == other._impl;
	public override bool Equals(object? obj) => obj is Window other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Window left, Window right) => left.Equals(right);
	public static bool operator !=(Window left, Window right) => !left.Equals(right);
	#endregion
}