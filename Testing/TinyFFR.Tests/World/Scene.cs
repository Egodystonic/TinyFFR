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
	public void BackdropIntensityShouldBeLinearInMeasuredLux() {
		const float LuxAtUnitIntensity = 500f;
		Assert.AreEqual(LuxAtUnitIntensity, BackdropIntensityUtils.IntensityToLux(1f, LuxAtUnitIntensity));
		Assert.AreEqual(1f, BackdropIntensityUtils.LuxToIntensity(LuxAtUnitIntensity, LuxAtUnitIntensity));

		Assert.AreEqual(LuxAtUnitIntensity * 2f, BackdropIntensityUtils.IntensityToLux(2f, LuxAtUnitIntensity));
		Assert.AreEqual(2f, BackdropIntensityUtils.LuxToIntensity(LuxAtUnitIntensity * 2f, LuxAtUnitIntensity));

		Assert.AreEqual(LuxAtUnitIntensity * 0.5f, BackdropIntensityUtils.IntensityToLux(0.5f, LuxAtUnitIntensity));
		Assert.AreEqual(0.5f, BackdropIntensityUtils.LuxToIntensity(LuxAtUnitIntensity * 0.5f, LuxAtUnitIntensity));
	}

	[Test]
	public void ShouldCorrectlyHandleInvalidBackdropIntensityInputs() {
		Assert.AreEqual(0f, BackdropIntensityUtils.ToNativeIntensity(Single.PositiveInfinity));
		Assert.AreEqual(0f, BackdropIntensityUtils.ToNativeIntensity(Single.NegativeInfinity));
		Assert.AreEqual(0f, BackdropIntensityUtils.ToNativeIntensity(Single.NaN));
		Assert.AreEqual(0f, BackdropIntensityUtils.ToNativeIntensity(Single.NegativeZero));
		Assert.AreEqual(0f, BackdropIntensityUtils.ToNativeIntensity(-1f));
		Assert.AreEqual(BackdropIntensityUtils.ToNativeIntensity(Scene.MaxBrightness), BackdropIntensityUtils.ToNativeIntensity(Scene.MaxBrightness + 1E10f));
	}
}
