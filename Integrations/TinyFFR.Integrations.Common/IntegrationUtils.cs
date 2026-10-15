// Created on 2026-08-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Egodystonic.TinyFFR.Assets.Materials;
using static Egodystonic.TinyFFR.Rendering.RenderOutputBufferCreationConfig;

namespace Egodystonic.TinyFFR;

static unsafe class IntegrationUtils {
	public static void BlitRgbaToBgra(ReadOnlySpan<TexelRgba32> src, Span<byte> dest) {
		var srcBytes = MemoryMarshal.AsBytes(src);
		var destBytes = dest[..srcBytes.Length];
		ref var srcRef = ref MemoryMarshal.GetReference(srcBytes);
		ref var destRef = ref MemoryMarshal.GetReference(destBytes);
		var i = 0;

		if (Avx2.IsSupported) {
			var shuffleIndices = Vector256.Create((byte) 2, 1, 0, 3, 6, 5, 4, 7, 10, 9, 8, 11, 14, 13, 12, 15, 2, 1, 0, 3, 6, 5, 4, 7, 10, 9, 8, 11, 14, 13, 12, 15);
			for (; i <= srcBytes.Length - Vector256<byte>.Count; i += Vector256<byte>.Count) {
				Avx2.Shuffle(Vector256.LoadUnsafe(ref srcRef, (nuint) i), shuffleIndices).StoreUnsafe(ref destRef, (nuint) i);
			}
		}
		if (Ssse3.IsSupported) {
			var shuffleIndices = Vector128.Create((byte) 2, 1, 0, 3, 6, 5, 4, 7, 10, 9, 8, 11, 14, 13, 12, 15);
			for (; i <= srcBytes.Length - Vector128<byte>.Count; i += Vector128<byte>.Count) {
				Ssse3.Shuffle(Vector128.LoadUnsafe(ref srcRef, (nuint) i), shuffleIndices).StoreUnsafe(ref destRef, (nuint) i);
			}
		}
		else if (AdvSimd.Arm64.IsSupported) {
			var shuffleIndices = Vector128.Create((byte) 2, 1, 0, 3, 6, 5, 4, 7, 10, 9, 8, 11, 14, 13, 12, 15);
			for (; i <= srcBytes.Length - Vector128<byte>.Count; i += Vector128<byte>.Count) {
				AdvSimd.Arm64.VectorTableLookup(Vector128.LoadUnsafe(ref srcRef, (nuint) i), shuffleIndices).StoreUnsafe(ref destRef, (nuint) i);
			}
		}

		var srcAsWords = MemoryMarshal.Cast<byte, uint>(srcBytes[i..]);
		var destAsWords = MemoryMarshal.Cast<byte, uint>(destBytes[i..]);
		for (var w = 0; w < srcAsWords.Length; ++w) {
			var word = srcAsWords[w];
			destAsWords[w] = (word & 0xFF00FF00U) | ((word & 0xFF0000U) >> 16) | ((word & 0xFFU) << 16);
		}
	}

	public static void BlitFrame(ReadOnlySpan<TexelRgba32> texels, XYPair<int> dimensions, byte* destination, int destinationStride, bool swapRedAndBlue) {
		if (texels.Length < dimensions.Area) throw new ArgumentException("Texel span is smaller than the given dimensions.", nameof(texels));
		var rowBytes = dimensions.X * TexelRgba32.TexelSizeBytes;
		if (destinationStride < rowBytes) throw new ArgumentOutOfRangeException(nameof(destinationStride));
		if (destinationStride == rowBytes) {
			BlitRows(texels[..dimensions.Area], new Span<byte>(destination, dimensions.Area * TexelRgba32.TexelSizeBytes), swapRedAndBlue);
			return;
		}

		for (var row = 0; row < dimensions.Y; ++row) {
			BlitRows(texels.Slice(row * dimensions.X, dimensions.X), new Span<byte>(destination + (nint) row * destinationStride, rowBytes), swapRedAndBlue);
		}
	}

	static void BlitRows(ReadOnlySpan<TexelRgba32> source, Span<byte> destination, bool swapRedAndBlue) {
		if (swapRedAndBlue) BlitRgbaToBgra(source, destination);
		else MemoryMarshal.AsBytes(source).CopyTo(destination);
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
