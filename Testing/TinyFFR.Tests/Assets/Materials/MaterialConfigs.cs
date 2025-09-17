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
	public void ShouldCorrectlyConvertOpaqueMaterialCreationConfigToAndFromHeapStorageFormat() {
		var colorTexImplSub = Substitute.For<ITextureImplProvider>();
		colorTexImplSub.IsDisposed(Arg.Any<ResourceHandle<Texture>>()).Returns(false);
		var testConfigA = new OpaqueMaterialCreationConfig {
			Name = "Aa Aa",
			ColorMap = new Texture(111, colorTexImplSub),
			NormalMap = new Texture(222, colorTexImplSub),
			OrmMap = new Texture(333, colorTexImplSub)
		};
		var testConfigB = new OpaqueMaterialCreationConfig {
			Name = "BBBbbb",
			ColorMap = new Texture(1111, colorTexImplSub),
			NormalMap = new Texture(2222, colorTexImplSub),
			OrmMap = new Texture(3333, colorTexImplSub)
		};

		void CompareConfigs(OpaqueMaterialCreationConfig expected, OpaqueMaterialCreationConfig actual) {
			Assert.AreEqual(expected.ColorMap, actual.ColorMap);
			Assert.AreEqual(expected.NormalMap, actual.NormalMap);
			Assert.AreEqual(expected.OrmMap, actual.OrmMap);
			CompareBaseConfigs(expected.BaseConfig, actual.BaseConfig);
		}

		AssertRoundTripHeapStorage(testConfigA, CompareConfigs);
		AssertRoundTripHeapStorage(testConfigB, CompareConfigs);

		AssertHeapSerializationWithObjects<OpaqueMaterialCreationConfig>()
			.Resource(testConfigA.ColorMap)
			.Resource(testConfigA.NormalMap)
			.Resource(testConfigA.OrmMap)
			.SubConfig(testConfigA.BaseConfig)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<OpaqueMaterialCreationConfig>()
			.Resource(testConfigB.ColorMap)
			.Resource(testConfigB.NormalMap)
			.Resource(testConfigB.OrmMap)
			.SubConfig(testConfigB.BaseConfig)
			.For(testConfigB);

		AssertPropertiesAccountedFor<OpaqueMaterialCreationConfig>()
			.Including(nameof(OpaqueMaterialCreationConfig.ColorMap))
			.Including(nameof(OpaqueMaterialCreationConfig.NormalMap))
			.Including(nameof(OpaqueMaterialCreationConfig.OrmMap))
			.Including(nameof(OpaqueMaterialCreationConfig.BaseConfig))
			.Including(nameof(OpaqueMaterialCreationConfig.Name));
	}

	[Test]
	public void ShouldCorrectlyConvertAlphaAwareMaterialCreationConfigToAndFromHeapStorageFormat() {
		var colorTexImplSub = Substitute.For<ITextureImplProvider>();
		colorTexImplSub.IsDisposed(Arg.Any<ResourceHandle<Texture>>()).Returns(false);
		var testConfigA = new AlphaAwareMaterialCreationConfig {
			Name = "Aa Aa",
			ColorMap = new Texture(111, colorTexImplSub),
			NormalMap = new Texture(222, colorTexImplSub),
			OrmMap = new Texture(333, colorTexImplSub),
			Type = AlphaMaterialType.ShadowMask
		};
		var testConfigB = new AlphaAwareMaterialCreationConfig {
			Name = "BBBbbb",
			ColorMap = new Texture(1111, colorTexImplSub),
			NormalMap = new Texture(2222, colorTexImplSub),
			OrmMap = new Texture(3333, colorTexImplSub),
			Type = AlphaMaterialType.Standard
		};

		void CompareConfigs(AlphaAwareMaterialCreationConfig expected, AlphaAwareMaterialCreationConfig actual) {
			Assert.AreEqual(expected.ColorMap, actual.ColorMap);
			Assert.AreEqual(expected.NormalMap, actual.NormalMap);
			Assert.AreEqual(expected.OrmMap, actual.OrmMap);
			Assert.AreEqual(expected.Type, actual.Type);
			CompareBaseConfigs(expected.BaseConfig, actual.BaseConfig);
		}

		AssertRoundTripHeapStorage(testConfigA, CompareConfigs);
		AssertRoundTripHeapStorage(testConfigB, CompareConfigs);

		AssertHeapSerializationWithObjects<AlphaAwareMaterialCreationConfig>()
			.Resource(testConfigA.ColorMap)
			.Resource(testConfigA.NormalMap)
			.Resource(testConfigA.OrmMap)
			.Int((int) testConfigA.Type)
			.SubConfig(testConfigA.BaseConfig)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<AlphaAwareMaterialCreationConfig>()
			.Resource(testConfigB.ColorMap)
			.Resource(testConfigB.NormalMap)
			.Resource(testConfigB.OrmMap)
			.Int((int) testConfigB.Type)
			.SubConfig(testConfigB.BaseConfig)
			.For(testConfigB);

		AssertPropertiesAccountedFor<AlphaAwareMaterialCreationConfig>()
			.Including(nameof(AlphaAwareMaterialCreationConfig.ColorMap))
			.Including(nameof(AlphaAwareMaterialCreationConfig.NormalMap))
			.Including(nameof(AlphaAwareMaterialCreationConfig.OrmMap))
			.Including(nameof(AlphaAwareMaterialCreationConfig.Type))
			.Including(nameof(AlphaAwareMaterialCreationConfig.BaseConfig))
			.Including(nameof(AlphaAwareMaterialCreationConfig.Name));
	}
}