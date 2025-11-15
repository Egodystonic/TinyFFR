// Created on 2025-09-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources;
using NSubstitute;
using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
class MaterialConfigsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	void CompareBaseConfigs(MaterialCreationConfig expected, MaterialCreationConfig actual) {
		Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
	}

	[Test]
	public void ShouldCorrectlyConvertMaterialCreationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new MaterialCreationConfig {
			Name = "Aa Aa"
		};
		var testConfigB = new MaterialCreationConfig {
			Name = "BBBbbb"
		};

		AssertRoundTripHeapStorage(testConfigA, CompareBaseConfigs);
		AssertRoundTripHeapStorage(testConfigB, CompareBaseConfigs);

		AssertHeapSerializationWithObjects<MaterialCreationConfig>()
			.String("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<MaterialCreationConfig>()
			.String("BBBbbb")
			.For(testConfigB);

		AssertPropertiesAccountedFor<MaterialCreationConfig>()
			.Including(nameof(MaterialCreationConfig.Name))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertStandardMaterialCreationConfigToAndFromHeapStorageFormat() {
		var colorTexImplSub = Substitute.For<ITextureImplProvider>();
		colorTexImplSub.IsDisposed(Arg.Any<ResourceHandle<Texture>>()).Returns(false);
		var testConfigA = new StandardMaterialCreationConfig {
			Name = "Aa Aa",
			ColorMap = new Texture(111, colorTexImplSub),
			NormalMap = null,
			OcclusionRoughnessMetallicMap = new Texture(333, colorTexImplSub),
			AnisotropyMap = null,
			EmissiveMap = new Texture(5555, colorTexImplSub),
			ClearCoatMap = null,
			AlphaMode = StandardMaterialAlphaMode.FullBlending
		};
		var testConfigB = new StandardMaterialCreationConfig {
			Name = "BBBbbb",
			ColorMap = new Texture(1111, colorTexImplSub),
			NormalMap = new Texture(2222, colorTexImplSub),
			OcclusionRoughnessMetallicMap = null,
			AnisotropyMap = new Texture(4444, colorTexImplSub),
			EmissiveMap = null,
			ClearCoatMap = new Texture(6666, colorTexImplSub),
			AlphaMode = StandardMaterialAlphaMode.MaskOnly
		};

		void CompareConfigs(StandardMaterialCreationConfig expected, StandardMaterialCreationConfig actual) {
			Assert.AreEqual(expected.ColorMap, actual.ColorMap);
			Assert.AreEqual(expected.NormalMap, actual.NormalMap);
			Assert.AreEqual(expected.OcclusionRoughnessMetallicMap, actual.OcclusionRoughnessMetallicMap);
			Assert.AreEqual(expected.OcclusionRoughnessMetallicReflectanceMap, actual.OcclusionRoughnessMetallicReflectanceMap);
			Assert.AreEqual(expected.AnisotropyMap, actual.AnisotropyMap);
			Assert.AreEqual(expected.EmissiveMap, actual.EmissiveMap);
			Assert.AreEqual(expected.ClearCoatMap, actual.ClearCoatMap);
			Assert.AreEqual(expected.AlphaMode, actual.AlphaMode);
			CompareBaseConfigs(expected.BaseConfig, actual.BaseConfig);
		}

		AssertRoundTripHeapStorage(testConfigA, CompareConfigs);
		AssertRoundTripHeapStorage(testConfigB, CompareConfigs);

		AssertHeapSerializationWithObjects<StandardMaterialCreationConfig>()
			.Resource(testConfigA.ColorMap)
			.Bool(false)
			.ZeroResource()
			.Bool(true)
			.Resource(testConfigA.OcclusionRoughnessMetallicMap.Value)
			.Bool(false)
			.ZeroResource()
			.Bool(true)
			.Resource(testConfigA.EmissiveMap.Value)
			.Bool(false)
			.ZeroResource()
			.Int((int) StandardMaterialAlphaMode.FullBlending)
			.SubConfig(testConfigA.BaseConfig)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<StandardMaterialCreationConfig>()
			.Resource(testConfigB.ColorMap)
			.Bool(true)
			.Resource(testConfigB.NormalMap.Value)
			.Bool(false)
			.ZeroResource()
			.Bool(true)
			.Resource(testConfigB.AnisotropyMap.Value)
			.Bool(false)
			.ZeroResource()
			.Bool(true)
			.Resource(testConfigB.ClearCoatMap.Value)
			.Int((int) StandardMaterialAlphaMode.MaskOnly)
			.SubConfig(testConfigB.BaseConfig)
			.For(testConfigB);

		AssertPropertiesAccountedFor<StandardMaterialCreationConfig>()
			.Including(nameof(StandardMaterialCreationConfig.ColorMap))
			.Including(nameof(StandardMaterialCreationConfig.NormalMap))
			.Including(nameof(StandardMaterialCreationConfig.OcclusionRoughnessMetallicMap))
			.Including(nameof(StandardMaterialCreationConfig.OcclusionRoughnessMetallicReflectanceMap))
			.Including(nameof(StandardMaterialCreationConfig.AnisotropyMap))
			.Including(nameof(StandardMaterialCreationConfig.EmissiveMap))
			.Including(nameof(StandardMaterialCreationConfig.ClearCoatMap))
			.Including(nameof(StandardMaterialCreationConfig.AlphaMode))
			.Including(nameof(StandardMaterialCreationConfig.Name));
	}

	[Test]
	public void ShouldCorrectlyConvertTransmissiveMaterialCreationConfigToAndFromHeapStorageFormat() {
		var colorTexImplSub = Substitute.For<ITextureImplProvider>();
		colorTexImplSub.IsDisposed(Arg.Any<ResourceHandle<Texture>>()).Returns(false);
		var testConfigA = new TransmissiveMaterialCreationConfig {
			Name = "Aa Aa",
			ColorMap = new Texture(111, colorTexImplSub),
			AbsorptionTransmissionMap = new Texture(11, colorTexImplSub),
			NormalMap = null,
			OcclusionRoughnessMetallicReflectanceMap = new Texture(333, colorTexImplSub),
			AnisotropyMap = null,
			EmissiveMap = new Texture(5555, colorTexImplSub),
			RefractionThickness = 1f,
			Quality = TransmissiveMaterialQuality.TrueReflectionsAndRefraction,
			AlphaMode = TransmissiveMaterialAlphaMode.FullBlending
		};
		var testConfigB = new TransmissiveMaterialCreationConfig {
			Name = "BBBbbb",
			ColorMap = new Texture(1111, colorTexImplSub),
			AbsorptionTransmissionMap = new Texture(111, colorTexImplSub),
			NormalMap = new Texture(2222, colorTexImplSub),
			OcclusionRoughnessMetallicReflectanceMap = null,
			AnisotropyMap = new Texture(4444, colorTexImplSub),
			EmissiveMap = null,
			RefractionThickness = 0.1f,
			Quality = TransmissiveMaterialQuality.SkyboxReflectionsAndRefraction,
			AlphaMode = TransmissiveMaterialAlphaMode.MaskOnly
		};

		void CompareConfigs(TransmissiveMaterialCreationConfig expected, TransmissiveMaterialCreationConfig actual) {
			Assert.AreEqual(expected.ColorMap, actual.ColorMap);
			Assert.AreEqual(expected.AbsorptionTransmissionMap, actual.AbsorptionTransmissionMap);
			Assert.AreEqual(expected.NormalMap, actual.NormalMap);
			Assert.AreEqual(expected.OcclusionRoughnessMetallicReflectanceMap, actual.OcclusionRoughnessMetallicReflectanceMap);
			Assert.AreEqual(expected.AnisotropyMap, actual.AnisotropyMap);
			Assert.AreEqual(expected.EmissiveMap, actual.EmissiveMap);
			Assert.AreEqual(expected.RefractionThickness, actual.RefractionThickness);
			Assert.AreEqual(expected.Quality, actual.Quality);
			Assert.AreEqual(expected.AlphaMode, actual.AlphaMode);
			CompareBaseConfigs(expected.BaseConfig, actual.BaseConfig);
		}

		AssertRoundTripHeapStorage(testConfigA, CompareConfigs);
		AssertRoundTripHeapStorage(testConfigB, CompareConfigs);

		AssertHeapSerializationWithObjects<TransmissiveMaterialCreationConfig>()
			.Resource(testConfigA.ColorMap)
			.Resource(testConfigA.AbsorptionTransmissionMap)
			.Bool(false)
			.ZeroResource()
			.Bool(true)
			.Resource(testConfigA.OcclusionRoughnessMetallicReflectanceMap.Value)
			.Bool(false)
			.ZeroResource()
			.Bool(true)
			.Resource(testConfigA.EmissiveMap.Value)
			.Float(1f)
			.Int((int) TransmissiveMaterialQuality.TrueReflectionsAndRefraction)
			.Int((int) TransmissiveMaterialAlphaMode.FullBlending)
			.SubConfig(testConfigA.BaseConfig)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TransmissiveMaterialCreationConfig>()
			.Resource(testConfigB.ColorMap)
			.Resource(testConfigB.AbsorptionTransmissionMap)
			.Bool(true)
			.Resource(testConfigB.NormalMap.Value)
			.Bool(false)
			.ZeroResource()
			.Bool(true)
			.Resource(testConfigB.AnisotropyMap.Value)
			.Bool(false)
			.ZeroResource()
			.Float(0.1f)
			.Int((int) TransmissiveMaterialQuality.SkyboxReflectionsAndRefraction)
			.Int((int) TransmissiveMaterialAlphaMode.MaskOnly)
			.SubConfig(testConfigB.BaseConfig)
			.For(testConfigB);

		AssertPropertiesAccountedFor<TransmissiveMaterialCreationConfig>()
			.Including(nameof(TransmissiveMaterialCreationConfig.ColorMap))
			.Including(nameof(TransmissiveMaterialCreationConfig.AbsorptionTransmissionMap))
			.Including(nameof(TransmissiveMaterialCreationConfig.NormalMap))
			.Including(nameof(TransmissiveMaterialCreationConfig.OcclusionRoughnessMetallicReflectanceMap))
			.Including(nameof(TransmissiveMaterialCreationConfig.AnisotropyMap))
			.Including(nameof(TransmissiveMaterialCreationConfig.EmissiveMap))
			.Including(nameof(TransmissiveMaterialCreationConfig.RefractionThickness))
			.Including(nameof(TransmissiveMaterialCreationConfig.Quality))
			.Including(nameof(TransmissiveMaterialCreationConfig.AlphaMode))
			.Including(nameof(TransmissiveMaterialCreationConfig.Name));
	}
}