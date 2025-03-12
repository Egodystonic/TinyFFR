// Created on 2025-03-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.World.Lighting;

[TestFixture]
class PointLightTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromUniversalBrightness() {
		Assert.AreEqual(PointLight.DefaultLumens, PointLight.BrightnessToLumens(1f));
		Assert.AreEqual(1f, PointLight.LumensToBrightness(PointLight.DefaultLumens));

		Assert.AreEqual(PointLight.DefaultLumens * 4f, PointLight.BrightnessToLumens(2f));
		Assert.AreEqual(2f, PointLight.LumensToBrightness(PointLight.DefaultLumens * 4f));

		Assert.AreEqual(PointLight.DefaultLumens * 0.25f, PointLight.BrightnessToLumens(0.5f));
		Assert.AreEqual(0.5f, PointLight.LumensToBrightness(PointLight.DefaultLumens * 0.25f));
	}

	[Test]
	public void ShouldCorrectlyHandleInvalidBrightnessInputs() {
		Assert.AreEqual(0f, PointLight.BrightnessToLumens(Single.PositiveInfinity));
		Assert.AreEqual(0f, PointLight.BrightnessToLumens(Single.NegativeInfinity));
		Assert.AreEqual(0f, PointLight.BrightnessToLumens(Single.NaN));
		Assert.AreEqual(0f, PointLight.BrightnessToLumens(Single.NegativeZero));
		Assert.AreEqual(0f, PointLight.BrightnessToLumens(-1f));
		Assert.AreEqual(PointLight.BrightnessToLumens(PointLight.MaxBrightness), PointLight.BrightnessToLumens(PointLight.MaxBrightness + 1E10f));

		Assert.AreEqual(0f, PointLight.LumensToBrightness(Single.PositiveInfinity));
		Assert.AreEqual(0f, PointLight.LumensToBrightness(Single.NegativeInfinity));
		Assert.AreEqual(0f, PointLight.LumensToBrightness(Single.NaN));
		Assert.AreEqual(0f, PointLight.LumensToBrightness(Single.NegativeZero));
		Assert.AreEqual(0f, PointLight.LumensToBrightness(-1f));
		Assert.AreEqual(PointLight.LumensToBrightness(PointLight.BrightnessToLumens(PointLight.MaxBrightness)), PointLight.LumensToBrightness(PointLight.BrightnessToLumens(PointLight.MaxBrightness) + 1E10f));
	}
}