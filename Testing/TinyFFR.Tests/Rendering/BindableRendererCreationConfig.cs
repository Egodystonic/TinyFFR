// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class BindableRendererCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new BindableRendererCreationConfig {
			AutoUpdateCameraAspectRatio = true,
			GpuSynchronizationFrameBufferCount = 3,
			Name = "Aa Aa",
			Quality = new() {
				ShadowQuality = Quality.VeryHigh
			},
			DefaultBufferSize = (123, 456)
		};
		var testConfigB = new BindableRendererCreationConfig {
			AutoUpdateCameraAspectRatio = false,
			GpuSynchronizationFrameBufferCount = 1,
			Name = "BBBbbb",
			Quality = new() {
				ShadowQuality = Quality.VeryLow
			},
			DefaultBufferSize = (100, 200)
		};

		static void ComparisonFunc(BindableRendererCreationConfig expected, BindableRendererCreationConfig actual) {
			Assert.AreEqual(expected.AutoUpdateCameraAspectRatio, actual.AutoUpdateCameraAspectRatio);
			Assert.AreEqual(expected.GpuSynchronizationFrameBufferCount, actual.GpuSynchronizationFrameBufferCount);
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
			Assert.AreEqual(expected.Quality.ShadowQuality, actual.Quality.ShadowQuality);
			Assert.AreEqual(expected.DefaultBufferSize, actual.DefaultBufferSize);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<BindableRendererCreationConfig>()
			.Obj(new XYPair<int>(123, 456))
			.SubConfig(testConfigA.BaseConfig)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<BindableRendererCreationConfig>()
			.Obj(new XYPair<int>(100, 200))
			.SubConfig(testConfigB.BaseConfig)
			.For(testConfigB);

		AssertPropertiesAccountedFor<BindableRendererCreationConfig>()
			.Including(nameof(BindableRendererCreationConfig.AutoUpdateCameraAspectRatio))
			.Including(nameof(BindableRendererCreationConfig.GpuSynchronizationFrameBufferCount))
			.Including(nameof(BindableRendererCreationConfig.Name))
			.Including(nameof(BindableRendererCreationConfig.Quality))
			.Including(nameof(BindableRendererCreationConfig.BaseConfig))
			.Including(nameof(BindableRendererCreationConfig.DefaultBufferSize))
			.End();
	}
}