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
			.Next(true)
			.Next(60)
			.Next("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<ApplicationLoopCreationConfig>()
			.Next(false)
			.Next(0)
			.Next("BBBbbb")
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
			.Next(new ApplicationLoopCreationConfig {
				FrameRateCapHz = 60,
				Name = "Aa Aa"
			})
			.Next(true)
			.Next(TimeSpan.FromSeconds(3d).Ticks)
			.Next(true)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<LocalApplicationLoopCreationConfig>()
			.Next(new ApplicationLoopCreationConfig {
				FrameRateCapHz = null,
				Name = "BBBbbb"
			})
			.Next(false)
			.Next(TimeSpan.FromSeconds(13d).Ticks)
			.Next(false)
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