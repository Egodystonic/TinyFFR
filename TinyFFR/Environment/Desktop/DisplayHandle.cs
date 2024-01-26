// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Desktop;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
readonly record struct DisplayHandle(int Index) {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator int(DisplayHandle h) => h.Index;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator DisplayHandle(int i) => new(i);
}