// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Windowing;

public readonly struct WindowHandle : IEquatable<WindowHandle>, IDisposable {
	readonly IWindowHandleImplProvider _impl;
	internal IntPtr Pointer { get; }
	internal WindowHandle(IntPtr pointer, IWindowHandleImplProvider impl) {
		Pointer = pointer;
		_impl = impl;
	}

	public bool Equals(WindowHandle other) => Pointer == other.Pointer;
	public override bool Equals(object? obj) => obj is WindowHandle other && Equals(other);
	public override int GetHashCode() => Pointer.GetHashCode();
	public static bool operator ==(WindowHandle left, WindowHandle right) => left.Equals(right);
	public static bool operator !=(WindowHandle left, WindowHandle right) => !left.Equals(right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _impl.Dispose(this);
}