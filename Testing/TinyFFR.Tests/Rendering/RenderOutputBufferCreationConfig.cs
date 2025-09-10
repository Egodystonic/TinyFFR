// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class RenderOutputBufferCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new RenderOutputBufferCreationConfig {
			TextureDimensions = (123, 456),
			Name = "Aa Aa"
		};
		var testConfigB = new RenderOutputBufferCreationConfig {
			TextureDimensions = (100, 200),
			Name = "BBBbbb"
		};

		static void ComparisonFunc(RenderOutputBufferCreationConfig expected, RenderOutputBufferCreationConfig actual) {
			Assert.AreEqual(expected.TextureDimensions, actual.TextureDimensions);
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<RenderOutputBufferCreationConfig>()
			.Obj(new XYPair<int>(123, 456))
			.String("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<RenderOutputBufferCreationConfig>()
			.Obj(new XYPair<int>(100, 200))
			.String("BBBbbb")
			.For(testConfigB);

		AssertPropertiesAccountedFor<RenderOutputBufferCreationConfig>()
			.Including(nameof(RenderOutputBufferCreationConfig.TextureDimensions))
			.Including(nameof(RenderOutputBufferCreationConfig.Name))
			.End();
	}
}