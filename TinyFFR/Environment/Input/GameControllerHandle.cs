// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Input;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
readonly record struct GameControllerHandle(IntPtr Pointer) {
	public static GameControllerHandle Amalgamated = IntPtr.Zero;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator IntPtr(GameControllerHandle h) => h.Pointer;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator GameControllerHandle(IntPtr p) => new(p);
}