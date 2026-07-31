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
			PostProcessingEnabled = false,
			InternalResolutionScalar = 0.5f,
			HdrColorPrecision = Quality.VeryHigh,
			ShadowsEnabled = false,
			BloomQuality = Quality.Low,
			DitheringEnabled = false
		};
		var testConfigB = new RenderQualityConfig {
			ShadowQuality = Quality.VeryHigh,
			ScreenSpaceEffectsQuality = Quality.High,
			AntiAliasingMode = AntiAliasingMode.None,
			AmbientOcclusionQuality = Quality.VeryLow,
			PostProcessingEnabled = true,
			InternalResolutionScalar = 0.75f,
			HdrColorPrecision = Quality.Low,
			ShadowsEnabled = true,
			BloomQuality = Quality.VeryHigh,
			DitheringEnabled = true
		};

		static void ComparisonFunc(RenderQualityConfig expected, RenderQualityConfig actual) {
			Assert.AreEqual(expected.ShadowQuality, actual.ShadowQuality);
			Assert.AreEqual(expected.ScreenSpaceEffectsQuality, actual.ScreenSpaceEffectsQuality);
			Assert.AreEqual(expected.AntiAliasingMode, actual.AntiAliasingMode);
			Assert.AreEqual(expected.AmbientOcclusionQuality, actual.AmbientOcclusionQuality);
			Assert.AreEqual(expected.PostProcessingEnabled, actual.PostProcessingEnabled);
			Assert.AreEqual(expected.InternalResolutionScalar, actual.InternalResolutionScalar);
			Assert.AreEqual(expected.HdrColorPrecision, actual.HdrColorPrecision);
			Assert.AreEqual(expected.ShadowsEnabled, actual.ShadowsEnabled);
			Assert.AreEqual(expected.BloomQuality, actual.BloomQuality);
			Assert.AreEqual(expected.DitheringEnabled, actual.DitheringEnabled);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<RenderQualityConfig>()
			.Int((int) Quality.VeryLow)
			.Int((int) Quality.Standard)
			.Int((int) AntiAliasingMode.Taa)
			.Int((int) Quality.High)
			.Bool(false)
			.Float(0.5f)
			.Int((int) Quality.VeryHigh)
			.Bool(false)
			.Int((int) Quality.Low)
			.Bool(false)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<RenderQualityConfig>()
			.Int((int) Quality.VeryHigh)
			.Int((int) Quality.High)
			.Int((int) AntiAliasingMode.None)
			.Int((int) Quality.VeryLow)
			.Bool(true)
			.Float(0.75f)
			.Int((int) Quality.Low)
			.Bool(true)
			.Int((int) Quality.VeryHigh)
			.Bool(true)
			.For(testConfigB);

		AssertPropertiesAccountedFor<RenderQualityConfig>()
			.Including(nameof(RenderQualityConfig.ShadowQuality))
			.Including(nameof(RenderQualityConfig.ScreenSpaceEffectsQuality))
			.Including(nameof(RenderQualityConfig.AntiAliasingMode))
			.Including(nameof(RenderQualityConfig.AmbientOcclusionQuality))
			.Including(nameof(RenderQualityConfig.PostProcessingEnabled))
			.Including(nameof(RenderQualityConfig.InternalResolutionScalar))
			.Including(nameof(RenderQualityConfig.HdrColorPrecision))
			.Including(nameof(RenderQualityConfig.ShadowsEnabled))
			.Including(nameof(RenderQualityConfig.BloomQuality))
			.Including(nameof(RenderQualityConfig.DitheringEnabled))
			.End();
	}
}
