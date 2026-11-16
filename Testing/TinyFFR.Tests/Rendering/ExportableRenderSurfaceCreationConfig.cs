// Created on 2026-11-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class ExportableRenderSurfaceCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new ExportableRenderSurfaceCreationConfig {
			Dimensions = (123, 456),
			Name = "Aa Aa"
		};
		var testConfigB = new ExportableRenderSurfaceCreationConfig {
			Dimensions = (100, 200),
			Name = "BBBbbb"
		};

		static void ComparisonFunc(ExportableRenderSurfaceCreationConfig expected, ExportableRenderSurfaceCreationConfig actual) {
			Assert.AreEqual(expected.Dimensions, actual.Dimensions);
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<ExportableRenderSurfaceCreationConfig>()
			.Obj(new XYPair<int>(123, 456))
			.String("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<ExportableRenderSurfaceCreationConfig>()
			.Obj(new XYPair<int>(100, 200))
			.String("BBBbbb")
			.For(testConfigB);

		AssertPropertiesAccountedFor<ExportableRenderSurfaceCreationConfig>()
			.Including(nameof(ExportableRenderSurfaceCreationConfig.Dimensions))
			.Including(nameof(ExportableRenderSurfaceCreationConfig.Name))
			.End();
	}

	[Test]
	public void ShouldUseExpectedDefaults() {
		Assert.AreEqual(ExportableRenderSurfaceCreationConfig.DefaultDimensions, new ExportableRenderSurfaceCreationConfig().Dimensions);
	}

	[Test]
	public void ShouldThrowForOutOfRangeDimensions() {
		Assert.Throws<ArgumentException>(() => new ExportableRenderSurfaceCreationConfig { Dimensions = (0, 100) }.ThrowIfInvalid());
		Assert.Throws<ArgumentException>(() => new ExportableRenderSurfaceCreationConfig { Dimensions = (100, ExportableRenderSurfaceCreationConfig.MaxDimensionXY + 1) }.ThrowIfInvalid());
		Assert.DoesNotThrow(() => new ExportableRenderSurfaceCreationConfig { Dimensions = (1, 1) }.ThrowIfInvalid());
	}
}
