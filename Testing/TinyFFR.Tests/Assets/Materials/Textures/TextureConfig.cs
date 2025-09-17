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
	public void ShouldCorrectlyConvertReadConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureReadConfig {
			FilePath = "Aa Aa",
			IncludeWAlphaChannel = true
		};
		var testConfigB = new TextureReadConfig {
			FilePath = "BBBbbb",
			IncludeWAlphaChannel = false
		};

		void AssertConfigsMatch(TextureReadConfig expected, TextureReadConfig actual) {
			Assert.AreEqual(expected.FilePath.ToString(), actual.FilePath.ToString());
			Assert.AreEqual(expected.IncludeWAlphaChannel, actual.IncludeWAlphaChannel);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<TextureReadConfig>()
			.String("Aa Aa")
			.Bool(true)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TextureReadConfig>()
			.String("BBBbbb")
			.Bool(false)
			.For(testConfigB);

		AssertPropertiesAccountedFor<TextureReadConfig>()
			.Including(nameof(TextureReadConfig.FilePath))
			.Including(nameof(TextureReadConfig.IncludeWAlphaChannel))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertGenerationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureGenerationConfig {
			Width = 100,
			Height = 200
		};
		var testConfigB = new TextureGenerationConfig {
			Width = 1000,
			Height = 2000
		};

		void AssertConfigsMatch(TextureGenerationConfig expected, TextureGenerationConfig actual) {
			Assert.AreEqual(expected.Width, actual.Width);
			Assert.AreEqual(expected.Height, actual.Height);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<TextureGenerationConfig>()
			.Int(100)
			.Int(200)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TextureGenerationConfig>()
			.Int(1000)
			.Int(2000)
			.For(testConfigB);

		AssertPropertiesAccountedFor<TextureGenerationConfig>()
			.Including(nameof(TextureGenerationConfig.Width))
			.Including(nameof(TextureGenerationConfig.Height))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertCreationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureCreationConfig {
			GenerateMipMaps = true,
			FlipX = false,
			FlipY = true,
			InvertXRedChannel = true,
			InvertYGreenChannel = false,
			InvertZBlueChannel = true,
			InvertWAlphaChannel = false,
			IsLinearColorspace = true,
			Name = "Aa Aa"
		};
		var testConfigB = new TextureCreationConfig {
			GenerateMipMaps = false,
			FlipX = true,
			FlipY = false,
			InvertXRedChannel = false,
			InvertYGreenChannel = true,
			InvertZBlueChannel = false,
			InvertWAlphaChannel = true,
			IsLinearColorspace = false,
			Name = "BBBbbb"
		};

		void AssertConfigsMatch(TextureCreationConfig expected, TextureCreationConfig actual) {
			Assert.AreEqual(expected.GenerateMipMaps, actual.GenerateMipMaps);
			Assert.AreEqual(expected.FlipX, actual.FlipX);
			Assert.AreEqual(expected.FlipY, actual.FlipY);
			Assert.AreEqual(expected.InvertXRedChannel, actual.InvertXRedChannel);
			Assert.AreEqual(expected.InvertYGreenChannel, actual.InvertYGreenChannel);
			Assert.AreEqual(expected.InvertZBlueChannel, actual.InvertZBlueChannel);
			Assert.AreEqual(expected.InvertWAlphaChannel, actual.InvertWAlphaChannel);
			Assert.AreEqual(expected.IsLinearColorspace, actual.IsLinearColorspace);
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<TextureCreationConfig>()
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.String("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TextureCreationConfig>()
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.String("BBBbbb")
			.For(testConfigB);

		AssertPropertiesAccountedFor<TextureCreationConfig>()
			.Including(nameof(TextureCreationConfig.GenerateMipMaps))
			.Including(nameof(TextureCreationConfig.FlipX))
			.Including(nameof(TextureCreationConfig.FlipY))
			.Including(nameof(TextureCreationConfig.InvertXRedChannel))
			.Including(nameof(TextureCreationConfig.InvertYGreenChannel))
			.Including(nameof(TextureCreationConfig.InvertZBlueChannel))
			.Including(nameof(TextureCreationConfig.InvertWAlphaChannel))
			.Including(nameof(TextureCreationConfig.IsLinearColorspace))
			.Including(nameof(TextureCreationConfig.Name))
			.End();
	}
}