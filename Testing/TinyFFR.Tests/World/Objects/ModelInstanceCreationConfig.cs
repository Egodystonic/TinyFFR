// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.World.Objects;

[TestFixture]
class ModelInstanceCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new ModelInstanceCreationConfig {
			Name = "Aa Aa",
			InitialTransform = Transform.Random()
		};
		var testConfigB = new ModelInstanceCreationConfig {
			Name = "BBBbbb",
			InitialTransform = Transform.Random()
		};

		static void ComparisonFunc(ModelInstanceCreationConfig expected, ModelInstanceCreationConfig actual) {
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
			Assert.AreEqual(expected.InitialTransform, actual.InitialTransform);
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertObjects<ModelInstanceCreationConfig>()
			.Next("Aa Aa")
			.Next(testConfigA.InitialTransform)
			.For(testConfigA);

		AssertObjects<ModelInstanceCreationConfig>()
			.Next("BBBbbb")
			.Next(testConfigB.InitialTransform)
			.For(testConfigB);

		AssertPropertiesAccountedFor<ModelInstanceCreationConfig>()
			.Including(nameof(ModelInstanceCreationConfig.Name))
			.Including(nameof(ModelInstanceCreationConfig.InitialTransform))
			.End();
	}
}