// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.World.Lighting;

[TestFixture]
class LightCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new RenderQualityConfig {
			ShadowQuality = Quality.VeryLow
		};
		var testConfigB = new RenderQualityConfig {
			ShadowQuality = Quality.VeryHigh
		};

		static void ComparisonFunc(RenderQualityConfig expected, RenderQualityConfig actual) {
			Assert.AreEqual(expected.ShadowQuality, actual.ShadowQuality);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertObjects<RenderQualityConfig>()
			.Next((int) Quality.VeryLow)
			.For(testConfigA);

		AssertObjects<RenderQualityConfig>()
			.Next((int) Quality.VeryHigh)
			.For(testConfigB);

		AssertPropertiesAccountedFor<RenderQualityConfig>()
			.Including(nameof(RenderQualityConfig.ShadowQuality))
			.End();
	}
}