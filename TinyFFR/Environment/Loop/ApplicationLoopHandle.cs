// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Loop;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
readonly record struct ApplicationLoopHandle(int Index) {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator int(ApplicationLoopHandle h) => h.Index;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ApplicationLoopHandle(int i) => new(i);
}