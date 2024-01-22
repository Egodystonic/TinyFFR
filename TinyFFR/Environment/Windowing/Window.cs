// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Windowing;

public readonly struct Window : IEquatable<Window>, ITrackedDisposable {
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

	public XYPair Size {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetSize(Pointer);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => _impl.SetSize(Pointer, value);
	}
	public XYPair Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetPosition(Pointer);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => _impl.SetPosition(Pointer, value);
	}


	internal Window(WindowPtr pointer, IWindowHandleImplProvider impl) {
		Pointer = pointer;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTitleUsingSpan(ReadOnlySpan<char> src) => _impl.SetTitle(Pointer, src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTitleUsingSpan(Span<char> dest) => _impl.GetTitle(Pointer, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTitleSpanMaxLength() => _impl.GetTitleMaxLength();

	public bool Equals(Window other) => Pointer == other.Pointer;
	public override bool Equals(object? obj) => obj is Window other && Equals(other);
	public override int GetHashCode() => Pointer.GetHashCode();
	public static bool operator ==(Window left, Window right) => left.Equals(right);
	public static bool operator !=(Window left, Window right) => !left.Equals(right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _impl.Dispose(Pointer);
}