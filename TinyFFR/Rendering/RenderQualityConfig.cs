// Created on 2025-05-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Rendering;
using static IConfigStruct;

public enum Quality {
	VeryLow = -2,
	Low = -1,
	Standard = 0,
	High = 1,
	VeryHigh = 2
}

public readonly record struct RenderQualityConfig : IConfigStruct<RenderQualityConfig> {
	public Quality ShadowQuality { get; init; }

	public static int GetHeapStorageFormattedLength(in RenderQualityConfig src) {
		return SerializationSizeOfInt(); // ShadowQuality
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in RenderQualityConfig src) {
		SerializationWriteInt(ref dest, (int) src.ShadowQuality);
	}
	public static RenderQualityConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			ShadowQuality = (Quality) SerializationReadInt(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}