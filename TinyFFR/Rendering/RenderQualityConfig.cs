// Created on 2025-05-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Rendering;
using static IConfigStruct;

public enum AntiAliasingMode {
	None = 0,
	Fxaa = 1,
	Taa = 2
}

public enum BuiltInQualityConfiguration {
	Medium,
	High,
	Ultra,
	Low,
	VeryLow
}

public readonly record struct RenderQualityConfig : IConfigStruct<RenderQualityConfig> {
	public const float MinInternalResolutionScalar = 0.1f;
	public const float MaxInternalResolutionScalar = 1f;
	
	public Quality ShadowQuality { get; init; } = Quality.Standard;
	public Quality ScreenSpaceEffectsQuality { get; init; } = Quality.Standard;
	public AntiAliasingMode AntiAliasingMode { get; init; } = AntiAliasingMode.Fxaa;
	public Quality AmbientOcclusionQuality { get; init; } = Quality.Standard;
	public bool PostProcessingEnabled { get; init; } = true;
	public float InternalResolutionScalar { get; init; } = 1f;
	public Quality HdrColorPrecision { get; init; } = Quality.Standard;
	public bool ShadowsEnabled { get; init; } = true;
	public Quality BloomQuality { get; init; } = Quality.Standard;
	public bool DitheringEnabled { get; init; } = true;

	public RenderQualityConfig() : this(BuiltInQualityConfiguration.High) { }
	public RenderQualityConfig(BuiltInQualityConfiguration builtInQuality) {
		switch (builtInQuality) {
			case BuiltInQualityConfiguration.VeryLow: {
				ShadowQuality = Quality.VeryLow;
				ScreenSpaceEffectsQuality = Quality.VeryLow;
				AntiAliasingMode = AntiAliasingMode.None;
				AmbientOcclusionQuality = Quality.VeryLow;
				InternalResolutionScalar = 0.666666f;
				HdrColorPrecision = Quality.VeryLow;
				BloomQuality = Quality.VeryLow;
				DitheringEnabled = false;
				break;
			}
			case BuiltInQualityConfiguration.Low: {
				ShadowQuality = Quality.Low;
				ScreenSpaceEffectsQuality = Quality.Low;
				AntiAliasingMode = AntiAliasingMode.None;
				AmbientOcclusionQuality = Quality.Low;
				InternalResolutionScalar = 0.75f;
				HdrColorPrecision = Quality.Low;
				BloomQuality = Quality.Low;
				DitheringEnabled = false;
				break;
			}
			default: {
				ShadowQuality = Quality.Standard;
				ScreenSpaceEffectsQuality = Quality.Standard;
				AntiAliasingMode = AntiAliasingMode.Fxaa;
				AmbientOcclusionQuality = Quality.Standard;
				InternalResolutionScalar = 1f;
				HdrColorPrecision = Quality.Standard;
				BloomQuality = Quality.Standard;
				DitheringEnabled = true;
				break;
			}
			case BuiltInQualityConfiguration.High: {
				ShadowQuality = Quality.High;
				ScreenSpaceEffectsQuality = Quality.High;
				AntiAliasingMode = AntiAliasingMode.Taa;
				AmbientOcclusionQuality = Quality.High;
				InternalResolutionScalar = 1f;
				HdrColorPrecision = Quality.High;
				BloomQuality = Quality.High;
				DitheringEnabled = true;
				break;
			}
			case BuiltInQualityConfiguration.Ultra: {
				ShadowQuality = Quality.VeryHigh;
				ScreenSpaceEffectsQuality = Quality.VeryHigh;
				AntiAliasingMode = AntiAliasingMode.Taa;
				AmbientOcclusionQuality = Quality.VeryHigh;
				InternalResolutionScalar = 1f;
				HdrColorPrecision = Quality.VeryHigh;
				BloomQuality = Quality.VeryHigh;
				DitheringEnabled = true;
				break;
			}
		}
	}
	
	internal void ThrowIfInvalid() {
		if (InternalResolutionScalar is < MinInternalResolutionScalar or > MaxInternalResolutionScalar) {
			throw new ArgumentOutOfRangeException(
				nameof(InternalResolutionScalar),
				InternalResolutionScalar,
				$"Must be between {nameof(MinInternalResolutionScalar)} ({MinInternalResolutionScalar}) and " +
				$"{nameof(MaxInternalResolutionScalar)} ({MaxInternalResolutionScalar}) (inclusive)."
			);
		}
	}

	public static int GetHeapStorageFormattedLength(in RenderQualityConfig src) {
		return SerializationSizeOfInt()  // ShadowQuality
			 + SerializationSizeOfInt()  // ScreenSpaceEffectsQuality
			 + SerializationSizeOfInt()  // AntiAliasingMode
			 + SerializationSizeOfInt()  // AmbientOcclusionQuality
			 + SerializationSizeOfBool()  // PostProcessingEnabled
			 + SerializationSizeOfFloat() // InternalResolutionScalar
			 + SerializationSizeOfInt()   // HdrColorPrecision
			 + SerializationSizeOfBool() // ShadowsEnabled
			 + SerializationSizeOfInt()  // BloomQuality
			 + SerializationSizeOfBool(); // DitheringEnabled
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in RenderQualityConfig src) {
		SerializationWriteInt(ref dest, (int) src.ShadowQuality);
		SerializationWriteInt(ref dest, (int) src.ScreenSpaceEffectsQuality);
		SerializationWriteInt(ref dest, (int) src.AntiAliasingMode);
		SerializationWriteInt(ref dest, (int) src.AmbientOcclusionQuality);
		SerializationWriteBool(ref dest, src.PostProcessingEnabled);
		SerializationWriteFloat(ref dest, src.InternalResolutionScalar);
		SerializationWriteInt(ref dest, (int) src.HdrColorPrecision);
		SerializationWriteBool(ref dest, src.ShadowsEnabled);
		SerializationWriteInt(ref dest, (int) src.BloomQuality);
		SerializationWriteBool(ref dest, src.DitheringEnabled);
	}
	public static RenderQualityConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			ShadowQuality = (Quality) SerializationReadInt(ref src),
			ScreenSpaceEffectsQuality = (Quality) SerializationReadInt(ref src),
			AntiAliasingMode = (AntiAliasingMode) SerializationReadInt(ref src),
			AmbientOcclusionQuality = (Quality) SerializationReadInt(ref src),
			PostProcessingEnabled = SerializationReadBool(ref src),
			InternalResolutionScalar = SerializationReadFloat(ref src),
			HdrColorPrecision = (Quality) SerializationReadInt(ref src),
			ShadowsEnabled = SerializationReadBool(ref src),
			BloomQuality = (Quality) SerializationReadInt(ref src),
			DitheringEnabled = SerializationReadBool(ref src),
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
