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
	public static readonly Quality DefaultShadowQuality = Quality.Standard;
	public static readonly Quality DefaultScreenSpaceEffectsQuality = Quality.Standard;

	public Quality ShadowQuality { get; init; } = DefaultShadowQuality;
	public Quality ScreenSpaceEffectsQuality { get; init; } = DefaultScreenSpaceEffectsQuality;

	public RenderQualityConfig() { }
	public RenderQualityConfig(Quality universalQualityLevel) {
		ShadowQuality = universalQualityLevel;
		ScreenSpaceEffectsQuality = universalQualityLevel;
	}

	public static int GetHeapStorageFormattedLength(in RenderQualityConfig src) {
		return SerializationSizeOfInt() // ShadowQuality
			 + SerializationSizeOfInt(); // EnableScreenSpaceReflectionPass
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in RenderQualityConfig src) {
		SerializationWriteInt(ref dest, (int) src.ShadowQuality);
		SerializationWriteInt(ref dest, (int) src.ScreenSpaceEffectsQuality);
	}
	public static RenderQualityConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			ShadowQuality = (Quality) SerializationReadInt(ref src),
			ScreenSpaceEffectsQuality = (Quality) SerializationReadInt(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}