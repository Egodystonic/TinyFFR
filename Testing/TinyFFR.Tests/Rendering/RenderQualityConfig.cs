// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class RenderQualityConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new RenderQualityConfig {
			ShadowQuality = Quality.VeryLow,
			ScreenSpaceEffectsQuality = Quality.Standard,
			AntiAliasingMode = AntiAliasingMode.TaaBalanced,
			AmbientOcclusionQuality = Quality.High,
			AmbientOcclusionStrength = 0.5f,
			PostProcessingEnabled = false,
			InternalResolutionScalar = 0.5f,
			HdrColorPrecision = Quality.VeryHigh,
			ShadowsEnabled = false,
			BloomQuality = Quality.Low,
			BloomStrength = 1.5f,
			DepthOfFieldQuality = Quality.Low,
			DepthOfFieldStrength = 1.5f,
			DitheringEnabled = false
		};
		var testConfigB = new RenderQualityConfig {
			ShadowQuality = Quality.VeryHigh,
			ScreenSpaceEffectsQuality = Quality.High,
			AntiAliasingMode = AntiAliasingMode.None,
			AmbientOcclusionQuality = Quality.VeryLow,
			AmbientOcclusionStrength = 1.1f,
			PostProcessingEnabled = true,
			InternalResolutionScalar = 0.75f,
			HdrColorPrecision = Quality.Low,
			ShadowsEnabled = true,
			BloomQuality = Quality.VeryHigh,
			BloomStrength = 0.3f,
			DepthOfFieldQuality = Quality.VeryHigh,
			DepthOfFieldStrength = 0.3f,
			DitheringEnabled = true
		};
		var testConfigC = new RenderQualityConfig {
			ShadowQuality = Quality.Low,
			ScreenSpaceEffectsQuality = Quality.VeryLow,
			AntiAliasingMode = AntiAliasingMode.TaaReducedGhosting,
			AmbientOcclusionQuality = Quality.Standard,
			AmbientOcclusionStrength = 0.9f,
			PostProcessingEnabled = true,
			InternalResolutionScalar = 1f,
			HdrColorPrecision = Quality.Standard,
			ShadowsEnabled = true,
			BloomQuality = Quality.Standard,
			BloomStrength = 1f,
			DepthOfFieldQuality = Quality.Standard,
			DepthOfFieldStrength = 1f,
			DitheringEnabled = false
		};

		static void ComparisonFunc(RenderQualityConfig expected, RenderQualityConfig actual) {
			Assert.AreEqual(expected.ShadowQuality, actual.ShadowQuality);
			Assert.AreEqual(expected.ScreenSpaceEffectsQuality, actual.ScreenSpaceEffectsQuality);
			Assert.AreEqual(expected.AntiAliasingMode, actual.AntiAliasingMode);
			Assert.AreEqual(expected.AmbientOcclusionQuality, actual.AmbientOcclusionQuality);
			Assert.AreEqual(expected.AmbientOcclusionStrength, actual.AmbientOcclusionStrength);
			Assert.AreEqual(expected.PostProcessingEnabled, actual.PostProcessingEnabled);
			Assert.AreEqual(expected.InternalResolutionScalar, actual.InternalResolutionScalar);
			Assert.AreEqual(expected.HdrColorPrecision, actual.HdrColorPrecision);
			Assert.AreEqual(expected.ShadowsEnabled, actual.ShadowsEnabled);
			Assert.AreEqual(expected.BloomQuality, actual.BloomQuality);
			Assert.AreEqual(expected.BloomStrength, actual.BloomStrength);
			Assert.AreEqual(expected.DepthOfFieldQuality, actual.DepthOfFieldQuality);
			Assert.AreEqual(expected.DepthOfFieldStrength, actual.DepthOfFieldStrength);
			Assert.AreEqual(expected.DitheringEnabled, actual.DitheringEnabled);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigC, ComparisonFunc);

		AssertHeapSerializationWithObjects<RenderQualityConfig>()
			.Int((int) Quality.VeryLow)
			.Int((int) Quality.Standard)
			.Int((int) AntiAliasingMode.TaaBalanced)
			.Int((int) Quality.High)
			.Float(0.5f)
			.Bool(false)
			.Float(0.5f)
			.Int((int) Quality.VeryHigh)
			.Bool(false)
			.Int((int) Quality.Low)
			.Float(1.5f)
			.Int((int) Quality.Low)
			.Float(1.5f)
			.Bool(false)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<RenderQualityConfig>()
			.Int((int) Quality.VeryHigh)
			.Int((int) Quality.High)
			.Int((int) AntiAliasingMode.None)
			.Int((int) Quality.VeryLow)
			.Float(1.1f)
			.Bool(true)
			.Float(0.75f)
			.Int((int) Quality.Low)
			.Bool(true)
			.Int((int) Quality.VeryHigh)
			.Float(0.3f)
			.Int((int) Quality.VeryHigh)
			.Float(0.3f)
			.Bool(true)
			.For(testConfigB);

		AssertHeapSerializationWithObjects<RenderQualityConfig>()
			.Int((int) Quality.Low)
			.Int((int) Quality.VeryLow)
			.Int((int) AntiAliasingMode.TaaReducedGhosting)
			.Int((int) Quality.Standard)
			.Float(0.9f)
			.Bool(true)
			.Float(1f)
			.Int((int) Quality.Standard)
			.Bool(true)
			.Int((int) Quality.Standard)
			.Float(1f)
			.Int((int) Quality.Standard)
			.Float(1f)
			.Bool(false)
			.For(testConfigC);

		AssertPropertiesAccountedFor<RenderQualityConfig>()
			.Including(nameof(RenderQualityConfig.ShadowQuality))
			.Including(nameof(RenderQualityConfig.ScreenSpaceEffectsQuality))
			.Including(nameof(RenderQualityConfig.AntiAliasingMode))
			.Including(nameof(RenderQualityConfig.AmbientOcclusionQuality))
			.Including(nameof(RenderQualityConfig.AmbientOcclusionStrength))
			.Including(nameof(RenderQualityConfig.PostProcessingEnabled))
			.Including(nameof(RenderQualityConfig.InternalResolutionScalar))
			.Including(nameof(RenderQualityConfig.HdrColorPrecision))
			.Including(nameof(RenderQualityConfig.ShadowsEnabled))
			.Including(nameof(RenderQualityConfig.BloomQuality))
			.Including(nameof(RenderQualityConfig.BloomStrength))
			.Including(nameof(RenderQualityConfig.DepthOfFieldQuality))
			.Including(nameof(RenderQualityConfig.DepthOfFieldStrength))
			.Including(nameof(RenderQualityConfig.DitheringEnabled))
			.End();
	}

	static readonly AntiAliasingMode[] TemporalAntiAliasingModes = {
		AntiAliasingMode.TaaBalanced,
		AntiAliasingMode.TaaReducedGhosting,
		AntiAliasingMode.TaaReducedFlickering,
		AntiAliasingMode.TaaIncreasedSharpening
	};

	static readonly AntiAliasingMode[] NonTemporalAntiAliasingModes = {
		AntiAliasingMode.None,
		AntiAliasingMode.Fxaa
	};

	[Test]
	public void ShouldDowngradeTemporalAntiAliasingOnRetainPreviousScenesLayers() {
		foreach (var mode in TemporalAntiAliasingModes) {
			var config = new RenderQualityConfig { AntiAliasingMode = mode };
			var result = config.WithCompositingConstraintsApplied(RenderCompositionType.RetainPreviousScenes);
			Assert.AreEqual(
				AntiAliasingMode.Fxaa,
				result.AntiAliasingMode,
				$"{mode} should have been downgraded to Fxaa on a RetainPreviousScenes layer."
			);
		}
	}

	[Test]
	public void ShouldNotAlterNonTemporalAntiAliasingOnRetainPreviousScenesLayers() {
		foreach (var mode in NonTemporalAntiAliasingModes) {
			var config = new RenderQualityConfig { AntiAliasingMode = mode };
			var result = config.WithCompositingConstraintsApplied(RenderCompositionType.RetainPreviousScenes);
			Assert.AreEqual(mode, result.AntiAliasingMode, $"{mode} is not temporal and should have been left alone.");
		}
	}

	[Test]
	public void ShouldNotAlterAnyAntiAliasingModeOnStandardLayers() {
		foreach (var mode in TemporalAntiAliasingModes.Concat(NonTemporalAntiAliasingModes)) {
			var config = new RenderQualityConfig { AntiAliasingMode = mode };
			var result = config.WithCompositingConstraintsApplied(RenderCompositionType.Standard);
			Assert.AreEqual(mode, result.AntiAliasingMode, $"{mode} should never be altered on a Standard layer.");
		}
	}

	[Test]
	public void ShouldNotAlterAnyOtherFieldWhenDowngradingAntiAliasing() {
		var config = new RenderQualityConfig(BuiltInQualityConfiguration.Ultra);
		Assert.AreEqual(AntiAliasingMode.TaaIncreasedSharpening, config.AntiAliasingMode, "Precondition: Ultra should use temporal anti-aliasing.");

		var result = config.WithCompositingConstraintsApplied(RenderCompositionType.RetainPreviousScenes);

		Assert.AreEqual(AntiAliasingMode.Fxaa, result.AntiAliasingMode);
		Assert.AreEqual(config with { AntiAliasingMode = AntiAliasingMode.Fxaa }, result, "Only AntiAliasingMode should differ.");
	}
}
