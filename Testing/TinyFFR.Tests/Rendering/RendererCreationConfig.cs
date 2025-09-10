// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class RendererCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new RendererCreationConfig {
			AutoUpdateCameraAspectRatio = true,
			GpuSynchronizationFrameBufferCount = 3,
			Name = "Aa Aa",
			Quality = new() {
				ShadowQuality = Quality.VeryHigh
			}
		};
		var testConfigB = new RendererCreationConfig {
			AutoUpdateCameraAspectRatio = false,
			GpuSynchronizationFrameBufferCount = 1,
			Name = "BBBbbb",
			Quality = new() {
				ShadowQuality = Quality.VeryLow
			}
		};

		static void ComparisonFunc(RendererCreationConfig expected, RendererCreationConfig actual) {
			Assert.AreEqual(expected.AutoUpdateCameraAspectRatio, actual.AutoUpdateCameraAspectRatio);
			Assert.AreEqual(expected.GpuSynchronizationFrameBufferCount, actual.GpuSynchronizationFrameBufferCount);
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
			Assert.AreEqual(expected.Quality.ShadowQuality, actual.Quality.ShadowQuality);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<RendererCreationConfig>()
			.Bool(true)
			.Int(3)
			.SubConfig(new RenderQualityConfig { ShadowQuality = Quality.VeryHigh })
			.String("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<RendererCreationConfig>()
			.Bool(false)
			.Int(1)
			.SubConfig(new RenderQualityConfig { ShadowQuality = Quality.VeryLow })
			.String("BBBbbb")
			.For(testConfigB);

		AssertPropertiesAccountedFor<RendererCreationConfig>()
			.Including(nameof(RendererCreationConfig.AutoUpdateCameraAspectRatio))
			.Including(nameof(RendererCreationConfig.GpuSynchronizationFrameBufferCount))
			.Including(nameof(RendererCreationConfig.Name))
			.Including(nameof(RendererCreationConfig.Quality))
			.End();
	}
}