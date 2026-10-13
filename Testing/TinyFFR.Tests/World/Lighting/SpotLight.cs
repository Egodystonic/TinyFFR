// Created on 2025-03-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.World.Lighting;

[TestFixture]
class SpotLightTest {
	const float TestTolerance = 0.001f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromUniversalBrightness() {
		Assert.AreEqual(SpotLight.DefaultCandela, SpotLight.BrightnessToCandela(1f));
		Assert.AreEqual(1f, SpotLight.CandelaToBrightness(SpotLight.DefaultCandela));

		Assert.AreEqual(SpotLight.DefaultCandela * 4f, SpotLight.BrightnessToCandela(2f));
		Assert.AreEqual(2f, SpotLight.CandelaToBrightness(SpotLight.DefaultCandela * 4f));

		Assert.AreEqual(SpotLight.DefaultCandela * 0.25f, SpotLight.BrightnessToCandela(0.5f));
		Assert.AreEqual(0.5f, SpotLight.CandelaToBrightness(SpotLight.DefaultCandela * 0.25f));
	}

	[Test]
	public void ShouldCorrectlyHandleInvalidBrightnessInputs() {
		Assert.AreEqual(0f, SpotLight.BrightnessToCandela(Single.PositiveInfinity));
		Assert.AreEqual(0f, SpotLight.BrightnessToCandela(Single.NegativeInfinity));
		Assert.AreEqual(0f, SpotLight.BrightnessToCandela(Single.NaN));
		Assert.AreEqual(0f, SpotLight.BrightnessToCandela(Single.NegativeZero));
		Assert.AreEqual(0f, SpotLight.BrightnessToCandela(-1f));
		Assert.AreEqual(SpotLight.BrightnessToCandela(SpotLight.MaxBrightness), SpotLight.BrightnessToCandela(SpotLight.MaxBrightness + 1E10f));

		Assert.AreEqual(0f, SpotLight.CandelaToBrightness(Single.PositiveInfinity));
		Assert.AreEqual(0f, SpotLight.CandelaToBrightness(Single.NegativeInfinity));
		Assert.AreEqual(0f, SpotLight.CandelaToBrightness(Single.NaN));
		Assert.AreEqual(0f, SpotLight.CandelaToBrightness(Single.NegativeZero));
		Assert.AreEqual(0f, SpotLight.CandelaToBrightness(-1f));
		Assert.AreEqual(SpotLight.CandelaToBrightness(SpotLight.BrightnessToCandela(SpotLight.MaxBrightness)), SpotLight.CandelaToBrightness(SpotLight.BrightnessToCandela(SpotLight.MaxBrightness) + 1E10f));
	}

	[Test]
	public void NativeLumensShouldBeCandelaTimesFourPi() {
		foreach (var brightness in new[] { 0f, 0.5f, 1f, 2f, 10f }) {
			var expected = SpotLight.BrightnessToCandela(brightness) * 4f * MathF.PI;
			Assert.AreEqual(expected, SpotLight.BrightnessToNativeLumensNoClamp(brightness), Math.Max(expected, 1f) * TestTolerance);
		}
	}

	[Test]
	public void PresetsShouldMapToExpectedCandela() {
		var expected = new (SpotLightBrightnessPreset Preset, float Candela)[] {
			(SpotLightBrightnessPreset.FlashlightTypical, 3_000f),
			(SpotLightBrightnessPreset.FlashlightDim, 500f),
			(SpotLightBrightnessPreset.FlashlightBright, 20_000f),
			(SpotLightBrightnessPreset.CarHeadlight, 30_000f),
			(SpotLightBrightnessPreset.StageSpotlight, 100_000f),
			(SpotLightBrightnessPreset.Searchlight, 1_000_000f),
			(SpotLightBrightnessPreset.DeskLamp, 150f),
		};
		Assert.That(expected.Select(e => e.Preset), Is.EquivalentTo(Enum.GetValues<SpotLightBrightnessPreset>()));
		foreach (var (preset, candela) in expected) {
			Assert.AreEqual(candela, preset.ToCandela(), preset.ToString());
			Assert.AreEqual(SpotLight.CandelaToBrightness(candela), preset.ToBrightnessValue(), TestTolerance, preset.ToString());
		}
		Assert.AreEqual(SpotLight.DefaultCandela, default(SpotLightBrightnessPreset).ToCandela());
		Assert.AreEqual(1f, default(SpotLightBrightnessPreset).ToBrightnessValue());
	}

	[Test]
	public void BrightnessCandelaShouldGetAndSetBrightness() {
		using var factory = new LocalTinyFfrFactory();
		using var light = factory.LightBuilder.CreateSpotLight();
		Assert.AreEqual(SpotLight.DefaultCandela, light.BrightnessCandela, TestTolerance);

		light.BrightnessCandela = 12_000f;
		Assert.AreEqual(2f, light.Brightness, TestTolerance);
		Assert.AreEqual(12_000f, light.BrightnessCandela, 12_000f * TestTolerance);

		light.SetBrightness(SpotLightBrightnessPreset.CarHeadlight);
		Assert.AreEqual(30_000f, light.BrightnessCandela, 30_000f * TestTolerance);

		using var presetLight = factory.LightBuilder.CreateSpotLight(brightnessPreset: SpotLightBrightnessPreset.DeskLamp);
		Assert.AreEqual(150f, presetLight.BrightnessCandela, 150f * TestTolerance);
	}
}
