// Created on 2025-09-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
class TextureConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ProcessingConfigShouldCorrectlySetRequiresProcessingFlag() {
		Assert.AreEqual(false, new TextureProcessingConfig().FlipX);
		Assert.AreEqual(false, new TextureProcessingConfig().FlipY);
		Assert.AreEqual(false, new TextureProcessingConfig().InvertXRedChannel);
		Assert.AreEqual(false, new TextureProcessingConfig().InvertYGreenChannel);
		Assert.AreEqual(false, new TextureProcessingConfig().InvertZBlueChannel);
		Assert.AreEqual(false, new TextureProcessingConfig().InvertWAlphaChannel);
		Assert.AreEqual(ColorChannel.R, new TextureProcessingConfig().XRedFinalOutputSource);
		Assert.AreEqual(ColorChannel.G, new TextureProcessingConfig().YGreenFinalOutputSource);
		Assert.AreEqual(ColorChannel.B, new TextureProcessingConfig().ZBlueFinalOutputSource);
		Assert.AreEqual(ColorChannel.A, new TextureProcessingConfig().WAlphaFinalOutputSource);
		Assert.AreEqual(false, new TextureProcessingConfig().RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { FlipX = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { FlipX = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { FlipY = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { FlipY = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { InvertXRedChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertXRedChannel = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { InvertYGreenChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertYGreenChannel = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { InvertZBlueChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertZBlueChannel = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { InvertWAlphaChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertWAlphaChannel = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { XRedFinalOutputSource = ColorChannel.R }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { XRedFinalOutputSource = ColorChannel.G }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { YGreenFinalOutputSource = ColorChannel.G }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { YGreenFinalOutputSource = ColorChannel.B }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { ZBlueFinalOutputSource = ColorChannel.B }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { ZBlueFinalOutputSource = ColorChannel.A }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { WAlphaFinalOutputSource = ColorChannel.A }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { WAlphaFinalOutputSource = ColorChannel.R }.RequiresProcessing);

		Assert.AreEqual(
			false, 
			new TextureProcessingConfig {
				FlipX = false,
				FlipY = false,
				InvertXRedChannel = false,
				InvertYGreenChannel = false,
				InvertZBlueChannel = false,
				InvertWAlphaChannel = false,
				XRedFinalOutputSource = ColorChannel.R,
				YGreenFinalOutputSource = ColorChannel.G,
				ZBlueFinalOutputSource = ColorChannel.B,
				WAlphaFinalOutputSource = ColorChannel.A,
			}.RequiresProcessing
		);

		Assert.AreEqual(true, new TextureProcessingConfig { FlipX = true, FlipY = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { FlipX = true, FlipY = true }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { FlipY = true, FlipX = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { FlipY = true, FlipX = true }.RequiresProcessing);

		Assert.AreEqual(true, new TextureProcessingConfig { InvertXRedChannel = true, InvertYGreenChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertXRedChannel = true, InvertYGreenChannel = true }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertYGreenChannel = true, InvertXRedChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertYGreenChannel = true, InvertXRedChannel = true }.RequiresProcessing);

		Assert.AreEqual(true, new TextureProcessingConfig { InvertZBlueChannel = true, InvertWAlphaChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertZBlueChannel = true, InvertWAlphaChannel = true }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertWAlphaChannel = true, InvertZBlueChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertWAlphaChannel = true, InvertZBlueChannel = true }.RequiresProcessing);

		Assert.AreEqual(true, new TextureProcessingConfig { XRedFinalOutputSource = ColorChannel.G, YGreenFinalOutputSource = ColorChannel.G }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { XRedFinalOutputSource = ColorChannel.G, YGreenFinalOutputSource = ColorChannel.B }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { YGreenFinalOutputSource = ColorChannel.B, XRedFinalOutputSource = ColorChannel.R }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { YGreenFinalOutputSource = ColorChannel.B, XRedFinalOutputSource = ColorChannel.G }.RequiresProcessing);

		Assert.AreEqual(true, new TextureProcessingConfig { ZBlueFinalOutputSource = ColorChannel.A, WAlphaFinalOutputSource = ColorChannel.A }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { ZBlueFinalOutputSource = ColorChannel.A, WAlphaFinalOutputSource = ColorChannel.R }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { WAlphaFinalOutputSource = ColorChannel.R, ZBlueFinalOutputSource = ColorChannel.B }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { WAlphaFinalOutputSource = ColorChannel.R, ZBlueFinalOutputSource = ColorChannel.A }.RequiresProcessing);
	}

	[Test]
	public void ShouldCorrectlyConvertProcessingConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureProcessingConfig {
			FlipX = false,
			FlipY = true,
			InvertXRedChannel = true,
			InvertYGreenChannel = false,
			InvertZBlueChannel = true,
			InvertWAlphaChannel = false,
			XRedFinalOutputSource = ColorChannel.R,
			YGreenFinalOutputSource = ColorChannel.G,
			ZBlueFinalOutputSource = ColorChannel.B,
			WAlphaFinalOutputSource = ColorChannel.A,
		};
		var testConfigB = new TextureProcessingConfig {
			FlipX = true,
			FlipY = false,
			InvertXRedChannel = false,
			InvertYGreenChannel = true,
			InvertZBlueChannel = false,
			InvertWAlphaChannel = true,
			XRedFinalOutputSource = ColorChannel.G,
			YGreenFinalOutputSource = ColorChannel.B,
			ZBlueFinalOutputSource = ColorChannel.A,
			WAlphaFinalOutputSource = ColorChannel.R,
		};

		void AssertConfigsMatch(TextureProcessingConfig expected, TextureProcessingConfig actual) {
			Assert.AreEqual(expected.FlipX, actual.FlipX);
			Assert.AreEqual(expected.FlipY, actual.FlipY);
			Assert.AreEqual(expected.InvertXRedChannel, actual.InvertXRedChannel);
			Assert.AreEqual(expected.InvertYGreenChannel, actual.InvertYGreenChannel);
			Assert.AreEqual(expected.InvertZBlueChannel, actual.InvertZBlueChannel);
			Assert.AreEqual(expected.InvertWAlphaChannel, actual.InvertWAlphaChannel);
			Assert.AreEqual(expected.XRedFinalOutputSource, actual.XRedFinalOutputSource);
			Assert.AreEqual(expected.YGreenFinalOutputSource, actual.YGreenFinalOutputSource);
			Assert.AreEqual(expected.ZBlueFinalOutputSource, actual.ZBlueFinalOutputSource);
			Assert.AreEqual(expected.WAlphaFinalOutputSource, actual.WAlphaFinalOutputSource);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<TextureProcessingConfig>()
			.Bool(false)
			.Bool(true)
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Int((int) ColorChannel.R)
			.Int((int) ColorChannel.G)
			.Int((int) ColorChannel.B)
			.Int((int) ColorChannel.A)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TextureProcessingConfig>()
			.Bool(true)
			.Bool(false)
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Int((int) ColorChannel.G)
			.Int((int) ColorChannel.B)
			.Int((int) ColorChannel.A)
			.Int((int) ColorChannel.R)
			.For(testConfigB);

		AssertPropertiesAccountedFor<TextureProcessingConfig>()
			.Including(nameof(TextureProcessingConfig.FlipX))
			.Including(nameof(TextureProcessingConfig.FlipY))
			.Including(nameof(TextureProcessingConfig.InvertXRedChannel))
			.Including(nameof(TextureProcessingConfig.InvertYGreenChannel))
			.Including(nameof(TextureProcessingConfig.InvertZBlueChannel))
			.Including(nameof(TextureProcessingConfig.InvertWAlphaChannel))
			.Including(nameof(TextureProcessingConfig.XRedFinalOutputSource))
			.Including(nameof(TextureProcessingConfig.YGreenFinalOutputSource))
			.Including(nameof(TextureProcessingConfig.ZBlueFinalOutputSource))
			.Including(nameof(TextureProcessingConfig.WAlphaFinalOutputSource))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertGenerationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureGenerationConfig {
			Dimensions = (100, 200)
		};
		var testConfigB = new TextureGenerationConfig {
			Dimensions = (1000, 2000)
		};

		void AssertConfigsMatch(TextureGenerationConfig expected, TextureGenerationConfig actual) {
			Assert.AreEqual(expected.Dimensions, actual.Dimensions);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<TextureGenerationConfig>()
			.Obj(new XYPair<int>(100, 200))
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TextureGenerationConfig>()
			.Obj(new XYPair<int>(1000, 2000))
			.For(testConfigB);

		AssertPropertiesAccountedFor<TextureGenerationConfig>()
			.Including(nameof(TextureGenerationConfig.Dimensions))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertCreationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureCreationConfig {
			GenerateMipMaps = true,
			IsLinearColorspace = true,
			Name = "Aa Aa",
			ProcessingToApply = new TextureProcessingConfig {
				FlipX = false,
				FlipY = true,
				InvertXRedChannel = true,
				InvertYGreenChannel = false,
				InvertZBlueChannel = true,
				InvertWAlphaChannel = false,
				XRedFinalOutputSource = ColorChannel.R,
				YGreenFinalOutputSource = ColorChannel.G,
				ZBlueFinalOutputSource = ColorChannel.B,
				WAlphaFinalOutputSource = ColorChannel.A,
			}
		};
		var testConfigB = new TextureCreationConfig {
			GenerateMipMaps = false,
			IsLinearColorspace = false,
			Name = "BBBbbb",
			ProcessingToApply = new TextureProcessingConfig {
				FlipX = true,
				FlipY = false,
				InvertXRedChannel = false,
				InvertYGreenChannel = true,
				InvertZBlueChannel = false,
				InvertWAlphaChannel = true,
				XRedFinalOutputSource = ColorChannel.G,
				YGreenFinalOutputSource = ColorChannel.B,
				ZBlueFinalOutputSource = ColorChannel.A,
				WAlphaFinalOutputSource = ColorChannel.R,
			}
		};

		void AssertConfigsMatch(TextureCreationConfig expected, TextureCreationConfig actual) {
			Assert.AreEqual(expected.GenerateMipMaps, actual.GenerateMipMaps);
			Assert.AreEqual(expected.IsLinearColorspace, actual.IsLinearColorspace);
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
			Assert.AreEqual(expected.ProcessingToApply, actual.ProcessingToApply);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<TextureCreationConfig>()
			.Bool(true)
			.Bool(true)
			.String("Aa Aa")
			.SubConfig(testConfigA.ProcessingToApply)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TextureCreationConfig>()
			.Bool(false)
			.Bool(false)
			.String("BBBbbb")
			.SubConfig(testConfigB.ProcessingToApply)
			.For(testConfigB);

		AssertPropertiesAccountedFor<TextureCreationConfig>()
			.Including(nameof(TextureCreationConfig.GenerateMipMaps))
			.Including(nameof(TextureCreationConfig.IsLinearColorspace))
			.Including(nameof(TextureCreationConfig.Name))
			.Including(nameof(TextureCreationConfig.ProcessingToApply))
			.End();
	}
}