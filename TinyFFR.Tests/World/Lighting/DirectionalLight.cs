// Created on 2025-03-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.World.Lighting;

[TestFixture]
class DirectionalLightTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromUniversalBrightness() {
		Assert.AreEqual(DirectionalLight.DefaultLux, DirectionalLight.BrightnessToLux(1f));
		Assert.AreEqual(1f, DirectionalLight.LuxToBrightness(DirectionalLight.DefaultLux));

		Assert.AreEqual(DirectionalLight.DefaultLux * 2f, DirectionalLight.BrightnessToLux(2f));
		Assert.AreEqual(2f, DirectionalLight.LuxToBrightness(DirectionalLight.DefaultLux * 2f));

		Assert.AreEqual(DirectionalLight.DefaultLux * 0.5f, DirectionalLight.BrightnessToLux(0.5f));
		Assert.AreEqual(0.5f, DirectionalLight.LuxToBrightness(DirectionalLight.DefaultLux * 0.5f));
	}

	[Test]
	public void ShouldCorrectlyHandleInvalidBrightnessInputs() {
		Assert.AreEqual(0f, DirectionalLight.BrightnessToLux(Single.PositiveInfinity));
		Assert.AreEqual(0f, DirectionalLight.BrightnessToLux(Single.NegativeInfinity));
		Assert.AreEqual(0f, DirectionalLight.BrightnessToLux(Single.NaN));
		Assert.AreEqual(0f, DirectionalLight.BrightnessToLux(Single.NegativeZero));
		Assert.AreEqual(0f, DirectionalLight.BrightnessToLux(-1f));
		Assert.AreEqual(DirectionalLight.BrightnessToLux(DirectionalLight.MaxBrightness), DirectionalLight.BrightnessToLux(DirectionalLight.MaxBrightness + 1E10f));

		Assert.AreEqual(0f, DirectionalLight.LuxToBrightness(Single.PositiveInfinity));
		Assert.AreEqual(0f, DirectionalLight.LuxToBrightness(Single.NegativeInfinity));
		Assert.AreEqual(0f, DirectionalLight.LuxToBrightness(Single.NaN));
		Assert.AreEqual(0f, DirectionalLight.LuxToBrightness(Single.NegativeZero));
		Assert.AreEqual(0f, DirectionalLight.LuxToBrightness(-1f));
		Assert.AreEqual(DirectionalLight.LuxToBrightness(DirectionalLight.BrightnessToLux(DirectionalLight.MaxBrightness)), DirectionalLight.LuxToBrightness(DirectionalLight.BrightnessToLux(DirectionalLight.MaxBrightness) + 1E10f));
	}
}