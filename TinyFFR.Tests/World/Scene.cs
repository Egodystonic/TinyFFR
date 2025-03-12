// Created on 2025-03-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.World;

[TestFixture]
class SceneTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromUniversalBrightness() {
		Assert.AreEqual(Scene.DefaultLux, Scene.BrightnessToLux(1f));
		Assert.AreEqual(1f, Scene.LuxToBrightness(Scene.DefaultLux));

		Assert.AreEqual(Scene.DefaultLux * 4f, Scene.BrightnessToLux(2f));
		Assert.AreEqual(2f, Scene.LuxToBrightness(Scene.DefaultLux * 4f));

		Assert.AreEqual(Scene.DefaultLux * 0.25f, Scene.BrightnessToLux(0.5f));
		Assert.AreEqual(0.5f, Scene.LuxToBrightness(Scene.DefaultLux * 0.25f));
	}

	[Test]
	public void ShouldCorrectlyHandleInvalidBrightnessInputs() {
		Assert.AreEqual(0f, Scene.BrightnessToLux(Single.PositiveInfinity));
		Assert.AreEqual(0f, Scene.BrightnessToLux(Single.NegativeInfinity));
		Assert.AreEqual(0f, Scene.BrightnessToLux(Single.NaN));
		Assert.AreEqual(0f, Scene.BrightnessToLux(Single.NegativeZero));
		Assert.AreEqual(0f, Scene.BrightnessToLux(-1f));
		Assert.AreEqual(Scene.BrightnessToLux(Scene.MaxBrightness), Scene.BrightnessToLux(Scene.MaxBrightness + 1E10f));

		Assert.AreEqual(0f, Scene.LuxToBrightness(Single.PositiveInfinity));
		Assert.AreEqual(0f, Scene.LuxToBrightness(Single.NegativeInfinity));
		Assert.AreEqual(0f, Scene.LuxToBrightness(Single.NaN));
		Assert.AreEqual(0f, Scene.LuxToBrightness(Single.NegativeZero));
		Assert.AreEqual(0f, Scene.LuxToBrightness(-1f));
		Assert.AreEqual(Scene.LuxToBrightness(Scene.BrightnessToLux(Scene.MaxBrightness)), Scene.LuxToBrightness(Scene.BrightnessToLux(Scene.MaxBrightness) + 1E10f));
	}
}