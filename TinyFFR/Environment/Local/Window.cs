// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly unsafe struct Window : IEquatable<Window>, IDisposable {
	readonly WindowHandle _handle;
	readonly IWindowImplProvider _impl;

	internal IWindowImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Window>();
	internal WindowHandle Handle => _handle;

	public string Title {
		get => Implementation.GetTitle(_handle);
		set => Implementation.SetTitle(_handle, value);
	}

	public Display Display {
		get => Implementation.GetDisplay(_handle);
		set => Implementation.SetDisplay(_handle, value);
	}
	public XYPair<int> Size {
		get => Implementation.GetSize(_handle);
		set => Implementation.SetSize(_handle, value);
	}
	public XYPair<int> Position { // TODO explain in XMLDoc that this is relative positioning on the selected Display
		get => Implementation.GetPosition(_handle);
		set => Implementation.SetPosition(_handle, value);
	}

	public WindowFullscreenStyle FullscreenStyle {
		get => Implementation.GetFullscreenStyle(_handle);
		set => Implementation.SetFullscreenStyle(_handle, value);
	}

	public bool LockCursor {
		get => Implementation.GetCursorLock(_handle);
		set => Implementation.SetCursorLock(_handle, value);
	}

	internal Window(WindowHandle handle, IWindowImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	public void SetTitleUsingSpan(ReadOnlySpan<char> src) => Implementation.SetTitleUsingSpan(_handle, src);
	public int GetTitleUsingSpan(Span<char> dest) => Implementation.GetTitleUsingSpan(_handle, dest);
	public int GetTitleSpanMaxLength() => Implementation.GetTitleSpanMaxLength(_handle);

	public override string ToString() => IsDisposed ? $"{nameof(Window)} (Disposed)" : $"{nameof(Window)} \"{Title}\"";

	#region Disposal
	bool IsDisposed => Implementation.IsDisposed(_handle);
	public void Dispose() => Implementation.Dispose(_handle);

	internal void ThrowIfInvalid() => InvalidObjectException.ThrowIfDefault(this);
	#endregion

	#region Equality
	public bool Equals(Window other) => _handle == other._handle && _impl == other._impl;
	public override bool Equals(object? obj) => obj is Window other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Window left, Window right) => left.Equals(right);
	public static bool operator !=(Window left, Window right) => !left.Equals(right);
	#endregion
}