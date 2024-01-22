// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Windowing;

public readonly struct WindowHandle : IEquatable<WindowHandle>, ITrackedDisposable {
	readonly IWindowHandleImplProvider _impl;
	internal WindowPtr Pointer { get; }

	public bool IsDisposed => _impl.IsDisposed(Pointer);

	public string Title {
		get {
			var maxSpanLength = GetTitleSpanMaxLength();
			var dest = maxSpanLength <= 1000 ? stackalloc char[maxSpanLength] : new char[maxSpanLength];

			var numCharsWritten = GetTitleUsingSpan(dest);
			return new(dest[..numCharsWritten]);
		}
		set => SetTitleUsingSpan(value);
	}

	internal WindowHandle(WindowPtr pointer, IWindowHandleImplProvider impl) {
		Pointer = pointer;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTitleUsingSpan(ReadOnlySpan<char> src) => _impl.SetTitle(Pointer, src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTitleUsingSpan(Span<char> dest) => _impl.GetTitle(Pointer, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTitleSpanMaxLength() => _impl.GetTitleMaxLength();

	public bool Equals(WindowHandle other) => Pointer == other.Pointer;
	public override bool Equals(object? obj) => obj is WindowHandle other && Equals(other);
	public override int GetHashCode() => Pointer.GetHashCode();
	public static bool operator ==(WindowHandle left, WindowHandle right) => left.Equals(right);
	public static bool operator !=(WindowHandle left, WindowHandle right) => !left.Equals(right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _impl.Dispose(Pointer);
}