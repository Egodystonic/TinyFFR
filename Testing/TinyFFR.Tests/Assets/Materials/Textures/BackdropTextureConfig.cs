// Created on 2026-08-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
class BackdropTextureCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new BackdropTextureCreationConfig { Name = "Aa Aa" };
		var testConfigB = new BackdropTextureCreationConfig();

		static void ComparisonFunc(BackdropTextureCreationConfig expected, BackdropTextureCreationConfig actual) {
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<BackdropTextureCreationConfig>()
			.String("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<BackdropTextureCreationConfig>()
			.String("")
			.For(testConfigB);

		AssertPropertiesAccountedFor<BackdropTextureCreationConfig>()
			.Including(nameof(BackdropTextureCreationConfig.Name))
			.End();
	}
}
