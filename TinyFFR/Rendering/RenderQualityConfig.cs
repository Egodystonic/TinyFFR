// Created on 2025-05-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;
using static IConfigStruct;

/// <summary>
/// Enumeration used to select an anti-aliasing mode inside a <see cref="RenderQualityConfig"/>.
/// </summary>
public enum AntiAliasingMode {
	/// <summary>
	/// Disables anti-aliasing.
	/// </summary>
	None = 0,
	/// <summary>
	/// Fast Approximate Anti-Aliasing (FXAA): A fast, single-frame edge-smoothing filter.
	/// </summary>
	/// <remarks>
	/// Cheaper than the TAA-based modes below, but generally produces less accurate results and can't remove some forms of aliasing (e.g. from fine sub-pixel detail) that they can.
	/// </remarks>
	Fxaa = 1,
	/// <summary>
	/// Temporal Anti-Aliasing (TAA): Blends information across multiple frames to smooth edges more thoroughly than <see cref="Fxaa"/>.
	/// This variant balances sharpness against artifacts like ghosting (faint trails left behind moving objects) and flickering.
	/// </summary>
	TaaBalanced = 2,
	/// <summary>
	/// As <see cref="TaaBalanced"/>, but tuned to further reduce ghosting (faint trails left behind moving objects), at some cost to overall sharpness.
	/// </summary>
	TaaReducedGhosting = 3,
	/// <summary>
	/// As <see cref="TaaBalanced"/>, but tuned to further reduce flickering, at some cost to overall sharpness.
	/// </summary>
	TaaReducedFlickering = 4,
	/// <summary>
	/// As <see cref="TaaBalanced"/>, but tuned to produce a sharper image, at an increased risk of ghosting and/or flickering; and some impact on performance.
	/// </summary>
	TaaIncreasedSharpening = 5
}

/// <summary>
/// Enumeration of default configuration layouts for <see cref="RenderQualityConfig"/>.
/// </summary>
public enum BuiltInQualityConfiguration {
	/// <summary>
	/// A configuration representing a high-quality experience at the best quality-to-performance tradeoff for most users on reasonably modern hardware.
	/// </summary>
	/// <remarks>
	/// This setting enables <see cref="AntiAliasingMode.Fxaa"/>, enables most postprocessing effects at high quality presets, and renders at an internal resolution scaling of 100%.  
	/// </remarks>
	High,
	/// <summary>
	/// A configuration representing a lower-cost, mid-quality experience, suitable for less powerful hardware but still providing a full-featured render experience.
	/// </summary>
	/// <remarks>
	/// This is the least resource-intensive preset that still enables anti-aliasing (<see cref="AntiAliasingMode.Fxaa"/>) and every post-processing
	/// effect, all set to their standard quality tier, and renders at an internal resolution scaling of 100%.
	/// </remarks>
	Medium,
	/// <summary>
	/// A configuration for high-end hardware, further improving on <see cref="High"/> with temporal anti-aliasing and higher-quality ambient occlusion and bloom.
	/// </summary>
	/// <remarks>
	/// This setting enables <see cref="AntiAliasingMode.TaaBalanced"/> anti-aliasing, mostly uses <c>High</c> or <c>VeryHigh</c> quality presets for other effects
	/// , and renders at an internal resolution scaling of 100%.
	/// </remarks>
	VeryHigh,
	/// <summary>
	/// A configuration prioritizing performance over visual fidelity, suitable for lower-end hardware.
	/// </summary>
	/// <remarks>
	/// This setting disables anti-aliasing and dithering, reduces most effect qualities to <c>Low</c>, and renders at a reduced internal resolution (75%) with FSR upscaling.
	/// </remarks>
	Low,
	/// <summary>
	/// The most performance-oriented quality preset.
	/// </summary>
	/// <remarks>
	/// This setting disables anti-aliasing and dithering, reduces most effect qualities to <c>VeryLow</c>, and renders at a significantly reduced internal resolution (66.7%) with FSR upscaling.
	/// </remarks>
	VeryLow,
	/// <summary>
	/// The highest-quality standard preset, intended for the very highest-end hardware or non-realtime rendering where maximum visual fidelity is required over framerate. 
	/// </summary>
	/// <remarks>
	/// This setting enables <see cref="AntiAliasingMode.TaaIncreasedSharpening"/> anti-aliasing and uses <c>VeryHigh</c> quality for all other effects.
	/// This includes enabling full-screen postprocessing buffers and various improvements to screen-space refraction/reflection; all take a significant toll on framerate.
	/// </remarks>
	Ultra,
	/// <summary>
	/// A specialized configuration designed to best suit <see cref="CanvasScene"/>s.
	/// </summary>
	/// <remarks>
	/// This setting disables post-processing, shadows, anti-aliasing, and dithering entirely,
	/// and sets ambient occlusion/bloom/depth-of-field strength to zero.
	/// </remarks>
	Canvas,
	/// <summary>
	/// An extremely-low-quality configuration intended for debugging and diagnostic rendering, where raw, unmodified pixel output is often useful; and spending GPU time on high visual fidelity is
	/// often a waste of time.
	/// </summary>
	/// <remarks>
	/// This setting disables all post-processing, shadows, anti-aliasing, dithering, and more. It does not alter the <see cref="RenderQualityConfig.InternalResolutionScalar"/>; you
	/// may wish to also reduce that value if you're happy for your debug scenes to be blurry.
	/// </remarks>
	DebugAndDiagnostic
}

/// <summary>
/// Specifies the render quality settings used by a <see cref="Renderer"/> (see <see cref="Renderer.SetQuality(RenderQualityConfig)"/>), determining how the library should trade performance and visual fidelity.
/// </summary>
/// <remarks>
/// Consider constructing this via one of the <see cref="BuiltInQualityConfiguration"/> presets and adjusting individual properties from there (using an object initializer), rather than setting every property manually.
/// </remarks>
public readonly record struct RenderQualityConfig : IConfigStruct<RenderQualityConfig> {
	/// <summary>
	/// The minimum permitted value for <see cref="InternalResolutionScalar"/>: <c>0.1f</c>.
	/// </summary>
	public const float MinInternalResolutionScalar = 0.1f;
	/// <summary>
	/// The maximum permitted value for <see cref="InternalResolutionScalar"/>: <c>1f</c>.
	/// </summary>
	public const float MaxInternalResolutionScalar = 1f;
	/// <summary>
	/// A <see cref="RenderQualityConfig"/> equivalent to using <see cref="BuiltInQualityConfiguration.High"/>.
	/// </summary>
	/// <remarks>
	/// This is the default config used for all non-canvas scenes.
	/// </remarks>
	public static readonly RenderQualityConfig SceneDefault = new(BuiltInQualityConfiguration.High);
	/// <summary>
	/// A <see cref="RenderQualityConfig"/> equivalent to using <see cref="BuiltInQualityConfiguration.Canvas"/>.
	/// </summary>
	/// <remarks>
	/// This is the default config used for all canvas scenes.
	/// </remarks>
	public static readonly RenderQualityConfig CanvasSceneDefault = new(BuiltInQualityConfiguration.Canvas);

	/// <summary>
	/// The quality level used for shadow rendering.
	/// </summary>
	/// <remarks>
	/// Lower levels lead to blockier, low-resolution shadows. Higher levels give smoother, less jaggy shadow edges.
	/// </remarks>
	public Quality ShadowQuality { get; init; } = Quality.Standard;
	/// <summary>
	/// The quality level used for screen-space effects (such as refraction, reflection, etc).
	/// </summary>
	/// <remarks>
	/// Levels <see cref="Quality.High"/> and <see cref="Quality.VeryHigh"/> enable screen-space reflections.
	/// </remarks>
	public Quality ScreenSpaceEffectsQuality { get; init; } = Quality.Standard;
	/// <summary>
	/// The anti-aliasing technique used to smooth jagged edges.
	/// </summary>
	/// <remarks>
	/// Note: If this <see cref="RenderQualityConfig"/> is used by a <see cref="Renderer"/> that is added to a <see cref="RendererCompositor"/>
	/// with <see cref="RenderCompositionType.RetainPreviousScenes"/>, any of the TAA-based modes are automatically downgraded to
	/// <see cref="Rendering.AntiAliasingMode.Fxaa"/> for that renderer; TAA
	/// relies on blending information across frames in a way that is not compatible with a layer whose previous frames aren't fully redrawn.
	/// <see cref="Rendering.AntiAliasingMode.None"/> and <see cref="Rendering.AntiAliasingMode.Fxaa"/> are unaffected.
	/// </remarks>
	public AntiAliasingMode AntiAliasingMode { get; init; } = AntiAliasingMode.Fxaa;
	/// <summary>
	/// The quality level used for ambient occlusion (subtle shadowing in crevices and corners where surfaces meet, approximating how much ambient light reaches them).
	/// </summary>
	/// <seealso cref="AmbientOcclusionStrength"/>
	public Quality AmbientOcclusionQuality { get; init; } = Quality.Standard;
	/// <summary>
	/// How strongly the ambient occlusion effect is applied, as a multiplier of its default intensity.
	/// </summary>
	/// <remarks>
	/// <c>1f</c> is the default strength; <c>0f</c> effectively disables its visible impact; <c>2f</c> doubles its impact, etc.
	/// </remarks>
	public float AmbientOcclusionStrength { get; init; } = 1f;
	/// <summary>
	/// Whether post-processing effects (such as HDR mapping, SSRO, bloom, etc) are applied at all.
	/// This also includes colour-space mapping (e.g. the sRGB/linear conversion pipeline).
	/// </summary>
	public bool PostProcessingEnabled { get; init; } = true;
	/// <summary>
	/// The scale factor applied to the resolution frames are internally rendered at, before being upscaled (via FSR)) to the target's actual resolution.
	/// Defaults to <c>1f</c> (i.e. 100%).
	/// </summary>
	/// <remarks>
	/// Rendering at a lower internal resolution and upscaling the result is a common technique for trading image sharpness for performance.
	/// Must be in the range <c>[<see cref="MinInternalResolutionScalar"/>, <see cref="MaxInternalResolutionScalar"/>]</c>.
	/// </remarks>
	public float InternalResolutionScalar { get; init; } = 1f;
	/// <summary>
	/// The quality/precision level used for high-dynamic-range (HDR) color data during rendering, before it is tonemapped down to the target's final (typically 8-bit-per-channel) output format.
	/// This setting tends to have a minimal effect on performance and visual fidelity. 
	/// </summary>
	public Quality HdrColorPrecision { get; init; } = Quality.Standard;
	/// <summary>
	/// Whether shadows are rendered at all, independent of <see cref="ShadowQuality"/>.
	/// </summary>
	public bool ShadowsEnabled { get; init; } = true;
	/// <summary>
	/// The quality level used for the bloom effect (a soft glow around very bright areas of the image, mimicking the way real cameras and eyes perceive intense light).
	/// </summary>
	/// <seealso cref="BloomStrength"/>
	public Quality BloomQuality { get; init; } = Quality.Standard;
	/// <summary>
	/// How strongly the bloom effect is applied, as a multiplier of its default intensity.
	/// </summary>
	/// <remarks>
	/// <c>1f</c> is the default strength; <c>0f</c> effectively disables its visible impact; <c>2f</c> doubles its impact, etc.
	/// </remarks>
	public float BloomStrength { get; init; } = 1f;
	/// <summary>
	/// The quality level used for the depth-of-field effect (blurring parts of the image that are outside the camera's focal range, mimicking real camera optics).
	/// </summary>
	/// <remarks>
	/// The <see cref="Quality.VeryHigh"/> setting uses a full-resolution effect buffer which looks the best but has a very large performance cost.
	/// </remarks>
	public Quality DepthOfFieldQuality { get; init; } = Quality.Standard;
	/// <summary>
	/// How strongly the depth-of-field effect is applied, as a multiplier of its default intensity.
	/// Note that the camera's <see cref="Camera.FocusDistance"/> must have a non-null value set to enable DoF at all.
	/// </summary>
	/// <remarks>
	/// <c>1f</c> is the default strength; <c>0f</c> effectively disables its visible impact; <c>2f</c> doubles its impact, etc.
	/// </remarks>
	public float DepthOfFieldStrength { get; init; } = 1f;
	/// <summary>
	/// Whether dithering (a subtle noise pattern used to hide visible banding in smooth color gradients) is applied to the final output.
	/// </summary>
	public bool DitheringEnabled { get; init; } = true;

	/// <summary>
	/// Constructs a new <see cref="RenderQualityConfig"/> equivalent to <see cref="BuiltInQualityConfiguration.High"/>.
	/// </summary>
	public RenderQualityConfig() : this(BuiltInQualityConfiguration.High) { }
	/// <summary>
	/// Constructs a new <see cref="RenderQualityConfig"/> using one of the built-in quality presets as a starting point.
	/// </summary>
	/// <param name="builtInQuality">The preset to base this configuration on. Use an object initializer to adjust individual properties from this starting point if needed.</param>
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
				DepthOfFieldQuality = Quality.VeryLow;
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
				DepthOfFieldQuality = Quality.Low;
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
				DepthOfFieldQuality = Quality.Standard;
				DitheringEnabled = true;
				break;
			}
			case BuiltInQualityConfiguration.High: {
				ShadowQuality = Quality.High;
				ScreenSpaceEffectsQuality = Quality.High;
				AntiAliasingMode = AntiAliasingMode.Fxaa;
				AmbientOcclusionQuality = Quality.Standard;
				InternalResolutionScalar = 1f;
				HdrColorPrecision = Quality.High;
				BloomQuality = Quality.High;
				DepthOfFieldQuality = Quality.High;
				DitheringEnabled = true;
				break;
			}
			case BuiltInQualityConfiguration.VeryHigh: {
				ShadowQuality = Quality.High;
				ScreenSpaceEffectsQuality = Quality.High;
				AntiAliasingMode = AntiAliasingMode.TaaBalanced;
				AmbientOcclusionQuality = Quality.High;
				InternalResolutionScalar = 1f;
				HdrColorPrecision = Quality.High;
				BloomQuality = Quality.VeryHigh;
				DepthOfFieldQuality = Quality.High;
				DitheringEnabled = true;
				break;
			}
			case BuiltInQualityConfiguration.Ultra: {
				ShadowQuality = Quality.VeryHigh;
				ScreenSpaceEffectsQuality = Quality.VeryHigh;
				AntiAliasingMode = AntiAliasingMode.TaaIncreasedSharpening;
				AmbientOcclusionQuality = Quality.VeryHigh;
				InternalResolutionScalar = 1f;
				HdrColorPrecision = Quality.VeryHigh;
				BloomQuality = Quality.VeryHigh;
				DepthOfFieldQuality = Quality.VeryHigh;
				DitheringEnabled = true;
				break;
			}
			case BuiltInQualityConfiguration.Canvas: {
				ShadowQuality = Quality.VeryLow;
				ScreenSpaceEffectsQuality = Quality.VeryLow;
				AmbientOcclusionQuality = Quality.VeryLow;
				BloomQuality = Quality.VeryLow;
				DepthOfFieldQuality = Quality.VeryLow;
				PostProcessingEnabled = false;
				ShadowsEnabled = false;
				AntiAliasingMode = AntiAliasingMode.None;
				DitheringEnabled = false;
				AmbientOcclusionStrength = 0f;
				BloomStrength = 0f;
				DepthOfFieldStrength = 0f;
				break;
			}
			case BuiltInQualityConfiguration.DebugAndDiagnostic: {
				ShadowQuality = Quality.VeryLow;
				ScreenSpaceEffectsQuality = Quality.VeryLow;
				AmbientOcclusionQuality = Quality.VeryLow;
				BloomQuality = Quality.VeryLow;
				DepthOfFieldQuality = Quality.VeryLow;
				PostProcessingEnabled = false;
				ShadowsEnabled = false;
				AntiAliasingMode = AntiAliasingMode.None;
				DitheringEnabled = false;
				AmbientOcclusionStrength = 0f;
				BloomStrength = 0f;
				DepthOfFieldStrength = 0f;
				break;
			}
		}
	}
	
	internal RenderQualityConfig WithCompositingConstraintsApplied(RenderCompositionType compositionType) {
		if (compositionType != RenderCompositionType.RetainPreviousScenes) return this;
		return AntiAliasingMode switch {
			AntiAliasingMode.TaaBalanced or AntiAliasingMode.TaaReducedGhosting
				or AntiAliasingMode.TaaReducedFlickering or AntiAliasingMode.TaaIncreasedSharpening
				=> this with { AntiAliasingMode = AntiAliasingMode.Fxaa },
			_ => this
		};
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

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in RenderQualityConfig src) {
		return SerializationSizeOfInt()  // ShadowQuality
			 + SerializationSizeOfInt()  // ScreenSpaceEffectsQuality
			 + SerializationSizeOfInt()  // AntiAliasingMode
			 + SerializationSizeOfInt()  // AmbientOcclusionQuality
			 + SerializationSizeOfFloat()  // AmbientOcclusionStrength
			 + SerializationSizeOfBool()  // PostProcessingEnabled
			 + SerializationSizeOfFloat() // InternalResolutionScalar
			 + SerializationSizeOfInt()   // HdrColorPrecision
			 + SerializationSizeOfBool() // ShadowsEnabled
			 + SerializationSizeOfInt()  // BloomQuality
			 + SerializationSizeOfFloat()  // BloomStrength
			 + SerializationSizeOfInt()  // DepthOfFieldQuality
			 + SerializationSizeOfFloat()  // DepthOfFieldStrength
			 + SerializationSizeOfBool(); // DitheringEnabled
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in RenderQualityConfig src) {
		SerializationWriteInt(ref dest, (int) src.ShadowQuality);
		SerializationWriteInt(ref dest, (int) src.ScreenSpaceEffectsQuality);
		SerializationWriteInt(ref dest, (int) src.AntiAliasingMode);
		SerializationWriteInt(ref dest, (int) src.AmbientOcclusionQuality);
		SerializationWriteFloat(ref dest, src.AmbientOcclusionStrength);
		SerializationWriteBool(ref dest, src.PostProcessingEnabled);
		SerializationWriteFloat(ref dest, src.InternalResolutionScalar);
		SerializationWriteInt(ref dest, (int) src.HdrColorPrecision);
		SerializationWriteBool(ref dest, src.ShadowsEnabled);
		SerializationWriteInt(ref dest, (int) src.BloomQuality);
		SerializationWriteFloat(ref dest, src.BloomStrength);
		SerializationWriteInt(ref dest, (int) src.DepthOfFieldQuality);
		SerializationWriteFloat(ref dest, src.DepthOfFieldStrength);
		SerializationWriteBool(ref dest, src.DitheringEnabled);
	}
	/// <inheritdoc/>
	public static RenderQualityConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			ShadowQuality = (Quality) SerializationReadInt(ref src),
			ScreenSpaceEffectsQuality = (Quality) SerializationReadInt(ref src),
			AntiAliasingMode = (AntiAliasingMode) SerializationReadInt(ref src),
			AmbientOcclusionQuality = (Quality) SerializationReadInt(ref src),
			AmbientOcclusionStrength = SerializationReadFloat(ref src),
			PostProcessingEnabled = SerializationReadBool(ref src),
			InternalResolutionScalar = SerializationReadFloat(ref src),
			HdrColorPrecision = (Quality) SerializationReadInt(ref src),
			ShadowsEnabled = SerializationReadBool(ref src),
			BloomQuality = (Quality) SerializationReadInt(ref src),
			BloomStrength = SerializationReadFloat(ref src),
			DepthOfFieldQuality = (Quality) SerializationReadInt(ref src),
			DepthOfFieldStrength = SerializationReadFloat(ref src),
			DitheringEnabled = SerializationReadBool(ref src),
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
