// Created on 2025-09-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;
using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Assets;

[TestFixture]
class ModelConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertReadConfigToAndFromHeapStorageFormat() {
		var testConfigA = new ModelReadConfig {
			MeshConfig = new() {
				FixCommonExportErrors = true,
				OptimizeForGpu = false	
			},
			TextureConfig = new() {
				IncludeWAlphaChannel = true
			}
		};
		var testConfigB = new ModelReadConfig {
			MeshConfig = new() {
				FixCommonExportErrors = false,
				OptimizeForGpu = true	
			},
			TextureConfig = new() {
				IncludeWAlphaChannel = false
			}
		};

		void AssertConfigsMatch(ModelReadConfig expected, ModelReadConfig actual) {
			Assert.AreEqual(expected.MeshConfig.FixCommonExportErrors, actual.MeshConfig.FixCommonExportErrors);
			Assert.AreEqual(expected.MeshConfig.OptimizeForGpu, actual.MeshConfig.OptimizeForGpu);
			Assert.AreEqual(expected.TextureConfig.IncludeWAlphaChannel, actual.TextureConfig.IncludeWAlphaChannel);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<ModelReadConfig>()
			.SubConfig(testConfigA.MeshConfig)
			.SubConfig(testConfigA.TextureConfig)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<ModelReadConfig>()
			.SubConfig(testConfigB.MeshConfig)
			.SubConfig(testConfigB.TextureConfig)
			.For(testConfigB);

		AssertPropertiesAccountedFor<ModelReadConfig>()
			.Including(nameof(ModelReadConfig.MeshConfig))
			.Including(nameof(ModelReadConfig.TextureConfig))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertCreationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new ModelCreationConfig {
			MeshConfig = new() {
				FlipTriangles = true,
				InvertTextureU = false,
				InvertTextureV = true,
				OriginTranslation = Vect.Random(),
				LinearRescalingFactor = 123f,
				Name = "Mesh Aa Aa"
			},
			TextureConfig = new() {
				GenerateMipMaps = true,
				IsLinearColorspace = false,
				ProcessingToApply = TextureProcessingConfig.Flip(true, false),
				Name = "Texture Aa Aa"
			},
			Name = "Aa Aa"
		};
		var testConfigB = new ModelCreationConfig {
			MeshConfig = new() {
				FlipTriangles = false,
				InvertTextureU = true,
				InvertTextureV = false,
				OriginTranslation = Vect.Random(),
				LinearRescalingFactor = -0.123f,
				Name = "Mesh BBBbbb"
			},
			TextureConfig = new() {
				GenerateMipMaps = false,
				IsLinearColorspace = true,
				ProcessingToApply = TextureProcessingConfig.Invert(),
				Name = "Texture BBBbbb"
			},
			Name = "BBBbbb"
		};

		void AssertConfigsMatch(ModelCreationConfig expected, ModelCreationConfig actual) {
			Assert.AreEqual(expected.MeshConfig.FlipTriangles, actual.MeshConfig.FlipTriangles);
			Assert.AreEqual(expected.MeshConfig.InvertTextureU, actual.MeshConfig.InvertTextureU);
			Assert.AreEqual(expected.MeshConfig.InvertTextureV, actual.MeshConfig.InvertTextureV);
			Assert.AreEqual(expected.MeshConfig.OriginTranslation, actual.MeshConfig.OriginTranslation);
			Assert.AreEqual(expected.MeshConfig.LinearRescalingFactor, actual.MeshConfig.LinearRescalingFactor);
			Assert.AreEqual(expected.MeshConfig.Name.ToString(), actual.MeshConfig.Name.ToString());
			
			Assert.AreEqual(expected.TextureConfig.GenerateMipMaps, actual.TextureConfig.GenerateMipMaps);
			Assert.AreEqual(expected.TextureConfig.IsLinearColorspace, actual.TextureConfig.IsLinearColorspace);
			Assert.AreEqual(expected.TextureConfig.Name.ToString(), actual.TextureConfig.Name.ToString());
			Assert.AreEqual(expected.TextureConfig.ProcessingToApply, actual.TextureConfig.ProcessingToApply);
			
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<ModelCreationConfig>()
			.SubConfig(testConfigA.MeshConfig)
			.SubConfig(testConfigA.TextureConfig)
			.String("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<ModelCreationConfig>()
			.SubConfig(testConfigB.MeshConfig)
			.SubConfig(testConfigB.TextureConfig)
			.String("BBBbbb")
			.For(testConfigB);

		AssertPropertiesAccountedFor<ModelCreationConfig>()
			.Including(nameof(ModelCreationConfig.MeshConfig))
			.Including(nameof(ModelCreationConfig.TextureConfig))
			.Including(nameof(ModelCreationConfig.Name))
			.End();
	}
}