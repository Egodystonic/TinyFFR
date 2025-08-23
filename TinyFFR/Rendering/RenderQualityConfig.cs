// Created on 2025-05-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Rendering;
using static Egodystonic.TinyFFR.IConfigStruct;

public enum Quality {
	VeryLow = -2,
	Low = -1,
	Standard = 0,
	High = 1,
	VeryHigh = 2
}

public readonly record struct RenderQualityConfig : IConfigStruct<RenderQualityConfig> {
	public Quality ShadowQuality { get; init; }

	public static int GetHeapStorableLength(in RenderQualityConfig src) {
		return SerializationSizeOf((int) src.ShadowQuality);
	}
	public static void ConvertToHeapStorable(Span<byte> dest, in RenderQualityConfig src) {
		SerializationWrite(ref dest, (int) src.ShadowQuality);
	}
	public static RenderQualityConfig ConvertFromHeapStorable(ReadOnlySpan<byte> src) {
		return new() {
			ShadowQuality = (Quality) SerializationReadInt(ref src)
		};
	}
}