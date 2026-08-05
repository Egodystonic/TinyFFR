// Created on 2026-08-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR;

static class IntegrationUtils {
	public static void BlitRgbaToBgra(ReadOnlySpan<TexelRgba32> src, Span<byte> dest) {
		var srcAsWords = MemoryMarshal.Cast<TexelRgba32, uint>(src);
		var destAsWords = MemoryMarshal.Cast<byte, uint>(dest)[..srcAsWords.Length];

		for (var i = 0; i < srcAsWords.Length; ++i) {
			var word = srcAsWords[i];
			destAsWords[i] = (word & 0xFF00FF00U) | ((word & 0xFF0000U) >> 16) | ((word & 0xFFU) << 16);
		}
	}
}
