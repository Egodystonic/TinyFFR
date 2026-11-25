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
			RequiredImageHandleType = ExportableRenderSurfaceImageHandleType.D3D11TextureNtHandle,
			Name = "Aa Aa"
		};
		var testConfigB = new ExportableRenderSurfaceCreationConfig {
			Dimensions = (100, 200),
			RequiredImageHandleType = ExportableRenderSurfaceImageHandleType.D3D11TextureLegacySharedHandle,
			Name = "BBBbbb"
		};

		static void ComparisonFunc(ExportableRenderSurfaceCreationConfig expected, ExportableRenderSurfaceCreationConfig actual) {
			Assert.AreEqual(expected.Dimensions, actual.Dimensions);
			Assert.AreEqual(expected.RequiredImageHandleType, actual.RequiredImageHandleType);
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<ExportableRenderSurfaceCreationConfig>()
			.Obj(new XYPair<int>(123, 456))
			.Int((int) ExportableRenderSurfaceImageHandleType.D3D11TextureNtHandle)
			.String("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<ExportableRenderSurfaceCreationConfig>()
			.Obj(new XYPair<int>(100, 200))
			.Int((int) ExportableRenderSurfaceImageHandleType.D3D11TextureLegacySharedHandle)
			.String("BBBbbb")
			.For(testConfigB);

		AssertPropertiesAccountedFor<ExportableRenderSurfaceCreationConfig>()
			.Including(nameof(ExportableRenderSurfaceCreationConfig.Dimensions))
			.Including(nameof(ExportableRenderSurfaceCreationConfig.RequiredImageHandleType))
			.Including(nameof(ExportableRenderSurfaceCreationConfig.Name))
			.End();
	}

	[Test]
	public void ShouldUseExpectedDefaults() {
		Assert.AreEqual(ExportableRenderSurfaceCreationConfig.DefaultDimensions, new ExportableRenderSurfaceCreationConfig().Dimensions);
		Assert.AreEqual(ExportableRenderSurfaceCreationConfig.DefaultImageHandleTypeForCurrentOperatingSystem, new ExportableRenderSurfaceCreationConfig().RequiredImageHandleType);
		var expectedDefaultHandleType = OperatingSystem.IsWindows() ? ExportableRenderSurfaceImageHandleType.D3D11TextureNtHandle
			: OperatingSystem.IsMacOS() ? ExportableRenderSurfaceImageHandleType.IOSurfaceRef
			: ExportableRenderSurfaceImageHandleType.VulkanOpaqueFileDescriptor;
		Assert.AreEqual(expectedDefaultHandleType, ExportableRenderSurfaceCreationConfig.DefaultImageHandleTypeForCurrentOperatingSystem);
	}

	[Test]
	public void ShouldThrowForOutOfRangeDimensions() {
		Assert.Throws<ArgumentException>(() => new ExportableRenderSurfaceCreationConfig { Dimensions = (0, 100) }.ThrowIfInvalid());
		Assert.Throws<ArgumentException>(() => new ExportableRenderSurfaceCreationConfig { Dimensions = (100, ExportableRenderSurfaceCreationConfig.DimensionsMaxComponent + 1) }.ThrowIfInvalid());
		Assert.DoesNotThrow(() => new ExportableRenderSurfaceCreationConfig { Dimensions = (1, 1) }.ThrowIfInvalid());
	}

	[Test]
	public void ShouldThrowForUndefinedOrNoneHandleType() {
		Assert.Throws<ArgumentException>(() => new ExportableRenderSurfaceCreationConfig { RequiredImageHandleType = ExportableRenderSurfaceImageHandleType.None }.ThrowIfInvalid());
		Assert.Throws<ArgumentException>(() => new ExportableRenderSurfaceCreationConfig { RequiredImageHandleType = (ExportableRenderSurfaceImageHandleType) 999 }.ThrowIfInvalid());
	}
}
