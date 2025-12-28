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
			ScreenSpaceEffectsQuality = Quality.Standard
		};
		var testConfigB = new RenderQualityConfig {
			ShadowQuality = Quality.VeryHigh,
			ScreenSpaceEffectsQuality = Quality.High
		};

		static void ComparisonFunc(RenderQualityConfig expected, RenderQualityConfig actual) {
			Assert.AreEqual(expected.ShadowQuality, actual.ShadowQuality);
			Assert.AreEqual(expected.ScreenSpaceEffectsQuality, actual.ScreenSpaceEffectsQuality);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<RenderQualityConfig>()
			.Int((int) Quality.VeryLow)
			.Int((int) Quality.Standard)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<RenderQualityConfig>()
			.Int((int) Quality.VeryHigh)
			.Int((int) Quality.High)
			.For(testConfigB);

		AssertPropertiesAccountedFor<RenderQualityConfig>()
			.Including(nameof(RenderQualityConfig.ShadowQuality))
			.Including(nameof(RenderQualityConfig.ScreenSpaceEffectsQuality))
			.End();
	}
}