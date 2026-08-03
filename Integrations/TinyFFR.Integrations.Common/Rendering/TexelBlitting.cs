// Created on 2026-08-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR.Rendering;

// Frames are read back from the GPU as RGBA because that is the only format for which Filament takes its memcpy
// fast path. WPF and WinForms have no 8-bit-per-channel RGBA pixel format (only BGRA), so those integrations swap
// the red and blue channels as part of the copy they already perform, rather than paying an extra full-frame pass.
static class TexelBlitting {
	public static void CopyRgbaAsBgra(ReadOnlySpan<TexelRgba32> source, Span<byte> destination) {
		var sourceWords = MemoryMarshal.Cast<TexelRgba32, uint>(source);
		var destinationWords = MemoryMarshal.Cast<byte, uint>(destination)[..sourceWords.Length];

		for (var i = 0; i < sourceWords.Length; ++i) {
			var word = sourceWords[i]; // Little-endian read of R,G,B,A gives 0xAABBGGRR; BGRA memory order wants 0xAARRGGBB
			destinationWords[i] = (word & 0xFF00FF00u) | ((word & 0x00FF0000u) >> 16) | ((word & 0x000000FFu) << 16);
		}
	}
}
