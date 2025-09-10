// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Environment.Local;
using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Environment;

[TestFixture]
class ApplicationLoopCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static void CompareBaseConfigs(ApplicationLoopCreationConfig expected, ApplicationLoopCreationConfig actual) {
		Assert.AreEqual(expected.FrameRateCapHz, actual.FrameRateCapHz);
		Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new ApplicationLoopCreationConfig {
			FrameRateCapHz = 60,
			Name = "Aa Aa"
		};
		var testConfigB = new ApplicationLoopCreationConfig {
			FrameRateCapHz = null,
			Name = "BBBbbb"
		};

		AssertRoundTripHeapStorage(testConfigA, CompareBaseConfigs);
		AssertRoundTripHeapStorage(testConfigB, CompareBaseConfigs);

		AssertHeapSerializationWithObjects<ApplicationLoopCreationConfig>()
			.Bool(true)
			.Int(60)
			.String("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<ApplicationLoopCreationConfig>()
			.Bool(false)
			.Int(0)
			.String("BBBbbb")
			.For(testConfigB);

		AssertPropertiesAccountedFor<ApplicationLoopCreationConfig>()
			.Including(nameof(ApplicationLoopCreationConfig.FrameRateCapHz))
			.Including(nameof(ApplicationLoopCreationConfig.Name))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertLocalVariantToAndFromHeapStorageFormat() {
		var testConfigA = new LocalApplicationLoopCreationConfig {
			FrameRateCapHz = 60,
			Name = "Aa Aa",
			FrameTimingPrecisionBusyWaitTime = TimeSpan.FromSeconds(3d),
			IterationShouldRefreshGlobalInputStates = true,
			WaitForVSync = true
		};
		var testConfigB = new LocalApplicationLoopCreationConfig {
			FrameRateCapHz = null,
			Name = "BBBbbb",
			FrameTimingPrecisionBusyWaitTime = TimeSpan.FromSeconds(13d),
			IterationShouldRefreshGlobalInputStates = false,
			WaitForVSync = false
		};

		static void ComparisonFunc(LocalApplicationLoopCreationConfig expected, LocalApplicationLoopCreationConfig actual) {
			Assert.AreEqual(expected.FrameTimingPrecisionBusyWaitTime, actual.FrameTimingPrecisionBusyWaitTime);
			Assert.AreEqual(expected.IterationShouldRefreshGlobalInputStates, actual.IterationShouldRefreshGlobalInputStates);
			Assert.AreEqual(expected.WaitForVSync, actual.WaitForVSync);
			CompareBaseConfigs(expected.BaseConfig, actual.BaseConfig);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<LocalApplicationLoopCreationConfig>()
			.SubConfig(new ApplicationLoopCreationConfig {
				FrameRateCapHz = 60,
				Name = "Aa Aa"
			})
			.Bool(true)
			.Long(TimeSpan.FromSeconds(3d).Ticks)
			.Bool(true)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<LocalApplicationLoopCreationConfig>()
			.SubConfig(new ApplicationLoopCreationConfig {
				FrameRateCapHz = null,
				Name = "BBBbbb"
			})
			.Bool(false)
			.Long(TimeSpan.FromSeconds(13d).Ticks)
			.Bool(false)
			.For(testConfigB);

		AssertPropertiesAccountedFor<LocalApplicationLoopCreationConfig>()
			.Including(nameof(LocalApplicationLoopCreationConfig.BaseConfig))
			.Including(nameof(LocalApplicationLoopCreationConfig.FrameRateCapHz))
			.Including(nameof(LocalApplicationLoopCreationConfig.Name))
			.Including(nameof(LocalApplicationLoopCreationConfig.FrameTimingPrecisionBusyWaitTime))
			.Including(nameof(LocalApplicationLoopCreationConfig.IterationShouldRefreshGlobalInputStates))
			.Including(nameof(LocalApplicationLoopCreationConfig.WaitForVSync))
			.End();
	}
}