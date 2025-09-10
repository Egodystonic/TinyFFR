﻿// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.World;

[TestFixture]
class SceneCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new SceneCreationConfig {
			InitialBackdropColor = new ColorVect(0.25f, 0.5f, 0.7f, 1f),
			Name = "Aa Aa"
		};
		var testConfigB = new SceneCreationConfig {
			InitialBackdropColor = null,
			Name = "BBBbbb"
		};

		static void ComparisonFunc(SceneCreationConfig expected, SceneCreationConfig actual) {
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
			Assert.AreEqual(expected.InitialBackdropColor, actual.InitialBackdropColor);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<SceneCreationConfig>()
			.String("Aa Aa")
			.Bool(true)
			.Obj(new ColorVect(0.25f, 0.5f, 0.7f, 1f))
			.For(testConfigA);

		AssertHeapSerializationWithObjects<SceneCreationConfig>()
			.String("BBBbbb")
			.Bool(false)
			.Obj(default(ColorVect))
			.For(testConfigB);

		AssertPropertiesAccountedFor<SceneCreationConfig>()
			.Including(nameof(SceneCreationConfig.Name))
			.Including(nameof(SceneCreationConfig.InitialBackdropColor))
			.End();
	}
}