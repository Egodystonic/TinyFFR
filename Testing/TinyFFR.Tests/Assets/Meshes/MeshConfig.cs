// Created on 2025-09-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class MeshConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertReadConfigToAndFromHeapStorageFormat() {
		var testConfigA = new MeshReadConfig {
			FixCommonExportErrors = true,
			OptimizeForGpu = false,
			CorrectFlippedOrientation = true,
			LoadSkeletalAnimationDataIfPresent = false,
			SubMeshIndex = null,
			AnimationTicksPerSecondOverride = 50f
		};
		var testConfigB = new MeshReadConfig {
			FixCommonExportErrors = false,
			OptimizeForGpu = true,
			CorrectFlippedOrientation = false,
			LoadSkeletalAnimationDataIfPresent = true,
			SubMeshIndex = 1,
			AnimationTicksPerSecondOverride = null
		};

		void AssertConfigsMatch(MeshReadConfig expected, MeshReadConfig actual) {
			Assert.AreEqual(expected.FixCommonExportErrors, actual.FixCommonExportErrors);
			Assert.AreEqual(expected.OptimizeForGpu, actual.OptimizeForGpu);
			Assert.AreEqual(expected.CorrectFlippedOrientation, actual.CorrectFlippedOrientation);
			Assert.AreEqual(expected.LoadSkeletalAnimationDataIfPresent, actual.LoadSkeletalAnimationDataIfPresent);
			Assert.AreEqual(expected.SubMeshIndex, actual.SubMeshIndex);
			Assert.AreEqual(expected.AnimationTicksPerSecondOverride, actual.AnimationTicksPerSecondOverride);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<MeshReadConfig>()
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Bool(false)
			.Int(0)
			.Bool(true)
			.Float(50f)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<MeshReadConfig>()
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Bool(true)
			.Int(1)
			.Bool(false)
			.Float(0f)
			.For(testConfigB);

		AssertPropertiesAccountedFor<MeshReadConfig>()
			.Including(nameof(MeshReadConfig.FixCommonExportErrors))
			.Including(nameof(MeshReadConfig.OptimizeForGpu))
			.Including(nameof(MeshReadConfig.CorrectFlippedOrientation))
			.Including(nameof(MeshReadConfig.LoadSkeletalAnimationDataIfPresent))
			.Including(nameof(MeshReadConfig.SubMeshIndex))
			.Including(nameof(MeshReadConfig.AnimationTicksPerSecondOverride))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertGenerationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new MeshGenerationConfig {
			TextureTransform = Transform2D.Random()
		};
		var testConfigB = new MeshGenerationConfig {
			TextureTransform = Transform2D.Random()
		};

		void AssertConfigsMatch(MeshGenerationConfig expected, MeshGenerationConfig actual) {
			Assert.AreEqual(expected.TextureTransform, actual.TextureTransform);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<MeshGenerationConfig>()
			.Obj(testConfigA.TextureTransform)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<MeshGenerationConfig>()
			.Obj(testConfigB.TextureTransform)
			.For(testConfigB);

		AssertPropertiesAccountedFor<MeshGenerationConfig>()
			.Including(nameof(MeshGenerationConfig.TextureTransform))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertCreationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new MeshCreationConfig {
			FlipTriangles = true,
			InvertTextureU = false,
			InvertTextureV = true,
			OriginTranslation = Vect.Random(),
			LinearRescalingFactor = 123f,
			Name = "Aa Aa"
		};
		var testConfigB = new MeshCreationConfig {
			FlipTriangles = false,
			InvertTextureU = true,
			InvertTextureV = false,
			OriginTranslation = Vect.Random(),
			LinearRescalingFactor = -0.123f,
			Name = "BBBbbb"
		};

		void AssertConfigsMatch(MeshCreationConfig expected, MeshCreationConfig actual) {
			Assert.AreEqual(expected.FlipTriangles, actual.FlipTriangles);
			Assert.AreEqual(expected.InvertTextureU, actual.InvertTextureU);
			Assert.AreEqual(expected.InvertTextureV, actual.InvertTextureV);
			Assert.AreEqual(expected.OriginTranslation, actual.OriginTranslation);
			Assert.AreEqual(expected.LinearRescalingFactor, actual.LinearRescalingFactor);
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<MeshCreationConfig>()
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Obj(testConfigA.OriginTranslation)
			.Float(123f)
			.String("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<MeshCreationConfig>()
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Obj(testConfigB.OriginTranslation)
			.Float(-0.123f)
			.String("BBBbbb")
			.For(testConfigB);

		AssertPropertiesAccountedFor<MeshCreationConfig>()
			.Including(nameof(MeshCreationConfig.FlipTriangles))
			.Including(nameof(MeshCreationConfig.InvertTextureU))
			.Including(nameof(MeshCreationConfig.InvertTextureV))
			.Including(nameof(MeshCreationConfig.OriginTranslation))
			.Including(nameof(MeshCreationConfig.LinearRescalingFactor))
			.Including(nameof(MeshCreationConfig.Name))
			.End();
	}
}