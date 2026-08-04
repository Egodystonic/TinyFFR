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
	public void ShouldDefaultToScreenConventionOriginAndNoBackdrop() {
		var config = new CanvasSceneCreationConfig();

		Assert.AreEqual(null, config.InitialBackdropColor);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new CanvasSceneCreationConfig {
			Name = "Aa Aa",
			InitialBackdropColor = ColorVect.RedOpaque
		};
		var testConfigB = new CanvasSceneCreationConfig {
			Name = "BBBbbb",
			InitialBackdropColor = null
		};

		static void ComparisonFunc(CanvasSceneCreationConfig expected, CanvasSceneCreationConfig actual) {
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
			Assert.AreEqual(expected.InitialBackdropColor, actual.InitialBackdropColor);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertPropertiesAccountedFor<CanvasSceneCreationConfig>()
			.Including(nameof(CanvasSceneCreationConfig.Name))
			.Including(nameof(CanvasSceneCreationConfig.InitialBackdropColor))
			.End();
	}
}
