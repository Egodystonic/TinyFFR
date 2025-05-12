// Created on 2025-03-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.World.Lighting;

[TestFixture]
class SpotLightTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromUniversalBrightness() {
		Assert.AreEqual(SpotLight.DefaultLumens, SpotLight.BrightnessToLumens(1f));
		Assert.AreEqual(1f, SpotLight.LumensToBrightness(SpotLight.DefaultLumens));

		Assert.AreEqual(SpotLight.DefaultLumens * 4f, SpotLight.BrightnessToLumens(2f));
		Assert.AreEqual(2f, SpotLight.LumensToBrightness(SpotLight.DefaultLumens * 4f));

		Assert.AreEqual(SpotLight.DefaultLumens * 0.25f, SpotLight.BrightnessToLumens(0.5f));
		Assert.AreEqual(0.5f, SpotLight.LumensToBrightness(SpotLight.DefaultLumens * 0.25f));
	}

	[Test]
	public void ShouldCorrectlyHandleInvalidBrightnessInputs() {
		Assert.AreEqual(0f, SpotLight.BrightnessToLumens(Single.PositiveInfinity));
		Assert.AreEqual(0f, SpotLight.BrightnessToLumens(Single.NegativeInfinity));
		Assert.AreEqual(0f, SpotLight.BrightnessToLumens(Single.NaN));
		Assert.AreEqual(0f, SpotLight.BrightnessToLumens(Single.NegativeZero));
		Assert.AreEqual(0f, SpotLight.BrightnessToLumens(-1f));
		Assert.AreEqual(SpotLight.BrightnessToLumens(SpotLight.MaxBrightness), SpotLight.BrightnessToLumens(SpotLight.MaxBrightness + 1E10f));

		Assert.AreEqual(0f, SpotLight.LumensToBrightness(Single.PositiveInfinity));
		Assert.AreEqual(0f, SpotLight.LumensToBrightness(Single.NegativeInfinity));
		Assert.AreEqual(0f, SpotLight.LumensToBrightness(Single.NaN));
		Assert.AreEqual(0f, SpotLight.LumensToBrightness(Single.NegativeZero));
		Assert.AreEqual(0f, SpotLight.LumensToBrightness(-1f));
		Assert.AreEqual(SpotLight.LumensToBrightness(SpotLight.BrightnessToLumens(SpotLight.MaxBrightness)), SpotLight.LumensToBrightness(SpotLight.BrightnessToLumens(SpotLight.MaxBrightness) + 1E10f));
	}
}