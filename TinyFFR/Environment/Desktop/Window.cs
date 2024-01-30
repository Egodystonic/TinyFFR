// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Desktop;

public readonly struct Window : IEquatable<Window>, ITrackedDisposable {
	readonly IWindowHandleImplProvider _impl;
	internal WindowHandle Handle { get; }

	public bool IsDisposed => _impl.IsDisposed(Handle);

	public string Title {
		get {
			var maxSpanLength = GetTitleSpanMaxLength();
			var dest = maxSpanLength <= 1000 ? stackalloc char[maxSpanLength] : new char[maxSpanLength];

			var numCharsWritten = GetTitleUsingSpan(dest);
			return new(dest[..numCharsWritten]);
		}
		set => SetTitleUsingSpan(value);
	}

	public Display Display {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetDisplay(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => _impl.SetDisplay(Handle, value);
	}
	public XYPair Size {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetSize(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => _impl.SetSize(Handle, value);
	}
	public XYPair Position { // TODO explain in XMLDoc that this is relative positioning on the selected Display
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetPosition(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => _impl.SetPosition(Handle, value);
	}

	public WindowFullscreenStyle FullscreenStyle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetFullscreenState(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => _impl.SetFullscreenState(Handle, value);
	}

	internal Window(WindowHandle handle, IWindowHandleImplProvider impl) {
		Handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTitleUsingSpan(ReadOnlySpan<char> src) => _impl.SetTitle(Handle, src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTitleUsingSpan(Span<char> dest) => _impl.GetTitle(Handle, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTitleSpanMaxLength() => _impl.GetTitleMaxLength();

	public bool Equals(Window other) => Handle == other.Handle;
	public override bool Equals(object? obj) => obj is Window other && Equals(other);
	public override int GetHashCode() => Handle.GetHashCode();
	public static bool operator ==(Window left, Window right) => left.Equals(right);
	public static bool operator !=(Window left, Window right) => !left.Equals(right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _impl.Dispose(Handle);

	public override string ToString() => $"{nameof(Window)} \"{Title}\"";
}