// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

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
			Name = "TestConfigA",
			Quality = new() {
				ShadowQuality = Quality.VeryHigh
			}
		};
		var testConfigB = new RendererCreationConfig {
			AutoUpdateCameraAspectRatio = false,
			GpuSynchronizationFrameBufferCount = 1,
			Name = "TestConfigB",
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

		ConfigStructTestUtils.AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		ConfigStructTestUtils.AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		ConfigStructTestUtils.AssertObjects<RendererCreationConfig>()
			.With(true)
			.With(3)
			.With("TestConfigA")
			.With(new RenderQualityConfig { ShadowQuality = Quality.VeryHigh })
			.For(testConfigA);

		ConfigStructTestUtils.AssertObjects<RendererCreationConfig>()
			.With(false)
			.With(1)
			.With("TestConfigB")
			.With(new RenderQualityConfig { ShadowQuality = Quality.VeryLow })
			.For(testConfigB);
	}
}