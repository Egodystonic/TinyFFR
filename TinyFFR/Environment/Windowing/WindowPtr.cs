// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Windowing;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
readonly record struct WindowPtr(IntPtr Pointer) {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator IntPtr(WindowPtr p) => p.Pointer;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator WindowPtr(IntPtr p) => new(p);
}