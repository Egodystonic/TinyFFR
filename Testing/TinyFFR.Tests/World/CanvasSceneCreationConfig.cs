// Created on 2026-08-04 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.World;

[TestFixture]
class CanvasSceneCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldDefaultToNoBackdrop() {
		var config = new CanvasSceneCreationConfig();

		Assert.AreEqual(null, config.InitialBackgroundColor);
		Assert.AreEqual(null, config.BaseConfig.InitialBackdropColor);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new CanvasSceneCreationConfig {
			Name = "Aa Aa",
			InitialBackgroundColor = ColorVect.RedOpaque
		};
		var testConfigB = new CanvasSceneCreationConfig {
			Name = "BBBbbb",
			InitialBackgroundColor = null
		};

		static void ComparisonFunc(CanvasSceneCreationConfig expected, CanvasSceneCreationConfig actual) {
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
			Assert.AreEqual(expected.InitialBackgroundColor, actual.InitialBackgroundColor);
			Assert.AreEqual(expected.BaseConfig.Name.ToString(), actual.BaseConfig.Name.ToString());
			Assert.AreEqual(expected.BaseConfig.InitialBackdropColor, actual.BaseConfig.InitialBackdropColor);
			Assert.AreEqual(expected.BaseConfig.InitialBackdrop, actual.BaseConfig.InitialBackdrop);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<CanvasSceneCreationConfig>()
			.SubConfig(new SceneCreationConfig {
				Name = "Aa Aa",
				InitialBackdropColor = ColorVect.RedOpaque
			})
			.For(testConfigA);

		AssertHeapSerializationWithObjects<CanvasSceneCreationConfig>()
			.SubConfig(new SceneCreationConfig {
				Name = "BBBbbb",
				InitialBackdropColor = null
			})
			.For(testConfigB);

		AssertPropertiesAccountedFor<CanvasSceneCreationConfig>()
			.Including(nameof(CanvasSceneCreationConfig.BaseConfig))
			.Including(nameof(CanvasSceneCreationConfig.Name))
			.Including(nameof(CanvasSceneCreationConfig.InitialBackgroundColor))
			.End();
	}
}
