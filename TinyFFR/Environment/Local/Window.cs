// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly struct Window : IDisposableResource<Window, WindowHandle, IWindowImplProvider>, IRenderTarget {
	readonly WindowHandle _handle;
	readonly IWindowImplProvider _impl;

	internal IWindowImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Window>();
	internal WindowHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Window)) : _handle;

	IWindowImplProvider IResource<WindowHandle, IWindowImplProvider>.Implementation => Implementation;
	WindowHandle IResource<WindowHandle, IWindowImplProvider>.Handle => Handle;

	public ReadOnlySpan<char> Title {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTitle(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetTitle(_handle, value);
	}
	ReadOnlySpan<char> IStringSpanNameEnabled.Name => Title;

	public Display Display {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDisplay(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetDisplay(_handle, value);
	}
	public XYPair<int> Size {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSize(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSize(_handle, value);
	}
	public XYPair<int> Position { // TODO explain in XMLDoc that this is relative positioning on the selected Display
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
	}

	public WindowFullscreenStyle FullscreenStyle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetFullscreenStyle(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetFullscreenStyle(_handle, value);
	}

	public bool LockCursor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCursorLock(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCursorLock(_handle, value);
	}

	XYPair<int> IRenderTarget.ViewportOffset => XYPair<int>.Zero;
	XYPair<uint> IRenderTarget.ViewportDimensions => Size.Cast<uint>();

	internal Window(WindowHandle handle, IWindowImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	static Window IResource<Window>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new Window(rawHandle, impl as IWindowImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);
	#endregion

	public override string ToString() => IsDisposed ? $"{nameof(Window)} (Disposed)" : $"{nameof(Window)} \"{Title}\"";

	#region Equality
	public bool Equals(Window other) => _handle == other._handle && _impl == other._impl;
	public override bool Equals(object? obj) => obj is Window other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Window left, Window right) => left.Equals(right);
	public static bool operator !=(Window left, Window right) => !left.Equals(right);
	#endregion
}