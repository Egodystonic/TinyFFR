// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.World.Lighting;

[TestFixture]
class LightCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static void CompareBaseConfigs(LightCreationConfig expected, LightCreationConfig actual) {
		Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
		Assert.AreEqual(expected.CastsShadows, actual.CastsShadows);
		Assert.AreEqual(expected.InitialBrightness, actual.InitialBrightness);
		Assert.AreEqual(expected.InitialColor, actual.InitialColor);
	}

	[Test]
	public void ShouldCorrectlyConvertBaseConfigToAndFromHeapStorageFormat() {
		var testConfigA = new LightCreationConfig {
			Name = "Aa Aa",
			CastsShadows = false,
			InitialBrightness = 123f,
			InitialColor = new ColorVect(0.2f, 0.4f, 0.6f, 0.8f)
		};
		var testConfigB = new LightCreationConfig {
			Name = "BBBbbb",
			CastsShadows = true,
			InitialBrightness = -0.1f,
			InitialColor = new ColorVect(0.8f, 0.6f, 0.4f, 0.2f)
		};

		AssertRoundTripHeapStorage(testConfigA, CompareBaseConfigs);
		AssertRoundTripHeapStorage(testConfigB, CompareBaseConfigs);

		AssertHeapSerializationWithObjects<LightCreationConfig>()
			.Next("Aa Aa")
			.Next(new ColorVect(0.2f, 0.4f, 0.6f, 0.8f))
			.Next(123f)
			.Next(false)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<LightCreationConfig>()
			.Next("BBBbbb")
			.Next(new ColorVect(0.8f, 0.6f, 0.4f, 0.2f))
			.Next(-0.1f)
			.Next(true)
			.For(testConfigB);

		AssertPropertiesAccountedFor<LightCreationConfig>()
			.Including(nameof(LightCreationConfig.Name))
			.Including(nameof(LightCreationConfig.CastsShadows))
			.Including(nameof(LightCreationConfig.InitialBrightness))
			.Including(nameof(LightCreationConfig.InitialColor))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertPointConfigToAndFromHeapStorageFormat() {
		var testConfigA = new PointLightCreationConfig {
			Name = "Aa Aa",
			CastsShadows = false,
			InitialBrightness = 123f,
			InitialColor = new ColorVect(0.2f, 0.4f, 0.6f, 0.8f),
			InitialMaxIlluminationRadius = 1.2f,
			InitialPosition = new Location(4f, -3f, 2f)
		};
		var testConfigB = new PointLightCreationConfig {
			Name = "BBBbbb",
			CastsShadows = true,
			InitialBrightness = -0.1f,
			InitialColor = new ColorVect(0.8f, 0.6f, 0.4f, 0.2f),
			InitialMaxIlluminationRadius = -0.1f,
			InitialPosition = new Location(-0.4f, 0.3f, -0.2f)
		};

		void CompareConfigs(PointLightCreationConfig expected, PointLightCreationConfig actual) {
			Assert.AreEqual(expected.InitialMaxIlluminationRadius, actual.InitialMaxIlluminationRadius);
			Assert.AreEqual(expected.InitialPosition, actual.InitialPosition);
			CompareBaseConfigs(expected.BaseConfig, actual.BaseConfig);
		}

		AssertRoundTripHeapStorage(testConfigA, CompareConfigs);
		AssertRoundTripHeapStorage(testConfigB, CompareConfigs);

		AssertHeapSerializationWithObjects<PointLightCreationConfig>()
			.Next(1.2f)
			.Next(new Location(4f, -3f, 2f))
			.Next(new LightCreationConfig {
				Name = "Aa Aa",
				CastsShadows = false,
				InitialBrightness = 123f,
				InitialColor = new ColorVect(0.2f, 0.4f, 0.6f, 0.8f)
			})
			.For(testConfigA);

		AssertHeapSerializationWithObjects<PointLightCreationConfig>()
			.Next(-0.1f)
			.Next(new Location(-0.4f, 0.3f, -0.2f))
			.Next(new LightCreationConfig {
				Name = "BBBbbb",
				CastsShadows = true,
				InitialBrightness = -0.1f,
				InitialColor = new ColorVect(0.8f, 0.6f, 0.4f, 0.2f)
			})
			.For(testConfigB);

		AssertPropertiesAccountedFor<PointLightCreationConfig>()
			.Including(nameof(PointLightCreationConfig.BaseConfig))
			.Including(nameof(PointLightCreationConfig.Name))
			.Including(nameof(PointLightCreationConfig.CastsShadows))
			.Including(nameof(PointLightCreationConfig.InitialBrightness))
			.Including(nameof(PointLightCreationConfig.InitialColor))
			.Including(nameof(PointLightCreationConfig.InitialMaxIlluminationRadius))
			.Including(nameof(PointLightCreationConfig.InitialPosition))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertSpotConfigToAndFromHeapStorageFormat() {
		var testConfigA = new SpotLightCreationConfig {
			Name = "Aa Aa",
			CastsShadows = false,
			InitialBrightness = 123f,
			InitialColor = new ColorVect(0.2f, 0.4f, 0.6f, 0.8f),
			InitialPosition = new Location(4f, -3f, 2f),
			InitialMaxIlluminationDistance = 1.2f,
			IsHighQuality = true,
			InitialConeDirection = new Direction(1f, 2f, 3f),
			InitialConeAngle = 27f,
			InitialIntenseBeamAngle = 8.4f,

		};
		var testConfigB = new SpotLightCreationConfig {
			Name = "BBBbbb",
			CastsShadows = true,
			InitialBrightness = -0.1f,
			InitialColor = new ColorVect(0.8f, 0.6f, 0.4f, 0.2f),
			InitialPosition = new Location(-0.4f, 0.3f, -0.2f),
			InitialMaxIlluminationDistance = 22f,
			IsHighQuality = false,
			InitialConeDirection = new Direction(-2f, 0f, 4f),
			InitialConeAngle = 2.7f,
			InitialIntenseBeamAngle = 1.4f,
		};

		void CompareConfigs(SpotLightCreationConfig expected, SpotLightCreationConfig actual) {
			Assert.AreEqual(expected.InitialPosition, actual.InitialPosition);
			Assert.AreEqual(expected.InitialMaxIlluminationDistance, actual.InitialMaxIlluminationDistance);
			Assert.AreEqual(expected.IsHighQuality, actual.IsHighQuality);
			Assert.AreEqual(expected.InitialConeDirection, actual.InitialConeDirection);
			Assert.AreEqual(expected.InitialConeAngle, actual.InitialConeAngle);
			Assert.AreEqual(expected.InitialIntenseBeamAngle, actual.InitialIntenseBeamAngle);
			CompareBaseConfigs(expected.BaseConfig, actual.BaseConfig);
		}

		AssertRoundTripHeapStorage(testConfigA, CompareConfigs);
		AssertRoundTripHeapStorage(testConfigB, CompareConfigs);

		AssertHeapSerializationWithObjects<SpotLightCreationConfig>()
			.Next(new Location(4f, -3f, 2f))
			.Next(1.2f)
			.Next(true)
			.Next(new Direction(1f, 2f, 3f))
			.Next(new Angle(27f))
			.Next(new Angle(8.4f))
			.Next(new LightCreationConfig {
				Name = "Aa Aa",
				CastsShadows = false,
				InitialBrightness = 123f,
				InitialColor = new ColorVect(0.2f, 0.4f, 0.6f, 0.8f)
			})
			.For(testConfigA);

		AssertHeapSerializationWithObjects<SpotLightCreationConfig>()
			.Next(new Location(-0.4f, 0.3f, -0.2f))
			.Next(22f)
			.Next(false)
			.Next(new Direction(-2f, 0f, 4f))
			.Next(new Angle(2.7f))
			.Next(new Angle(1.4f))
			.Next(new LightCreationConfig {
				Name = "BBBbbb",
				CastsShadows = true,
				InitialBrightness = -0.1f,
				InitialColor = new ColorVect(0.8f, 0.6f, 0.4f, 0.2f)
			})
			.For(testConfigB);

		AssertPropertiesAccountedFor<SpotLightCreationConfig>()
			.Including(nameof(SpotLightCreationConfig.BaseConfig))
			.Including(nameof(SpotLightCreationConfig.Name))
			.Including(nameof(SpotLightCreationConfig.CastsShadows))
			.Including(nameof(SpotLightCreationConfig.InitialBrightness))
			.Including(nameof(SpotLightCreationConfig.InitialColor))
			.Including(nameof(SpotLightCreationConfig.InitialPosition))
			.Including(nameof(SpotLightCreationConfig.InitialMaxIlluminationDistance))
			.Including(nameof(SpotLightCreationConfig.IsHighQuality))
			.Including(nameof(SpotLightCreationConfig.InitialConeDirection))
			.Including(nameof(SpotLightCreationConfig.InitialConeAngle))
			.Including(nameof(SpotLightCreationConfig.InitialIntenseBeamAngle))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertDirectionalConfigToAndFromHeapStorageFormat() {
		var testConfigA = new DirectionalLightCreationConfig {
			Name = "Aa Aa",
			CastsShadows = false,
			InitialBrightness = 123f,
			InitialColor = new ColorVect(0.2f, 0.4f, 0.6f, 0.8f),
			ShowSunDisc = true,
			InitialDirection = new Direction(4f, -3f, 2f)
		};
		var testConfigB = new DirectionalLightCreationConfig {
			Name = "BBBbbb",
			CastsShadows = true,
			InitialBrightness = -0.1f,
			InitialColor = new ColorVect(0.8f, 0.6f, 0.4f, 0.2f),
			ShowSunDisc = false,
			InitialDirection = new Direction(-0.4f, 0.3f, -0.2f)
		};

		void CompareConfigs(DirectionalLightCreationConfig expected, DirectionalLightCreationConfig actual) {
			Assert.AreEqual(expected.ShowSunDisc, actual.ShowSunDisc);
			Assert.AreEqual(expected.InitialDirection, actual.InitialDirection);
			CompareBaseConfigs(expected.BaseConfig, actual.BaseConfig);
		}

		AssertRoundTripHeapStorage(testConfigA, CompareConfigs);
		AssertRoundTripHeapStorage(testConfigB, CompareConfigs);

		AssertHeapSerializationWithObjects<DirectionalLightCreationConfig>()
			.Next(true)
			.Next(new Direction(4f, -3f, 2f))
			.Next(new LightCreationConfig {
				Name = "Aa Aa",
				CastsShadows = false,
				InitialBrightness = 123f,
				InitialColor = new ColorVect(0.2f, 0.4f, 0.6f, 0.8f)
			})
			.For(testConfigA);

		AssertHeapSerializationWithObjects<DirectionalLightCreationConfig>()
			.Next(false)
			.Next(new Direction(-0.4f, 0.3f, -0.2f))
			.Next(new LightCreationConfig {
				Name = "BBBbbb",
				CastsShadows = true,
				InitialBrightness = -0.1f,
				InitialColor = new ColorVect(0.8f, 0.6f, 0.4f, 0.2f)
			})
			.For(testConfigB);

		AssertPropertiesAccountedFor<DirectionalLightCreationConfig>()
			.Including(nameof(DirectionalLightCreationConfig.BaseConfig))
			.Including(nameof(DirectionalLightCreationConfig.Name))
			.Including(nameof(DirectionalLightCreationConfig.CastsShadows))
			.Including(nameof(DirectionalLightCreationConfig.InitialBrightness))
			.Including(nameof(DirectionalLightCreationConfig.InitialColor))
			.Including(nameof(DirectionalLightCreationConfig.ShowSunDisc))
			.Including(nameof(DirectionalLightCreationConfig.InitialDirection))
			.End();
	}
}