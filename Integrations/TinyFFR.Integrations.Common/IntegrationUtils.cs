// Created on 2026-08-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Materials;
using static Egodystonic.TinyFFR.Rendering.RenderOutputBufferCreationConfig;

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

	public static XYPair<int> ClampToMaxPreservingAspectRatio(XYPair<int> target, XYPair<int>? maxOrNull) {
		if (maxOrNull is not { } max) return target;
		if (target.X <= 0 || target.Y <= 0) return target;
		if (target.X <= max.X && target.Y <= max.Y) return target;

		var scalar = Double.Min(max.X / (Double) target.X, max.Y / (Double) target.Y);
		return new XYPair<int>(
			Int32.Clamp((Int32) Double.Round(target.X * scalar), MinTextureDimensionXY, max.X),
			Int32.Clamp((Int32) Double.Round(target.Y * scalar), MinTextureDimensionXY, max.Y)
		);
	}
}
