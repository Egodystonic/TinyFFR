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
			AntiAliasingMode = AntiAliasingMode.Taa,
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

		AssertHeapSerializationWithObjects<RenderQualityConfig>()
			.Int((int) Quality.VeryLow)
			.Int((int) Quality.Standard)
			.Int((int) AntiAliasingMode.Taa)
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
}
