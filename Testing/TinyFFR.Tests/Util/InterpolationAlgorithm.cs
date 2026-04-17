// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR;

[TestFixture]
unsafe class InterpolationAlgorithmTest {
	const float TestTolerance = 0.001f;

	static Real GetValue(InterpolationAlgorithm<Real> algo, float t) => algo.GetValue(0f, 1f, t);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyImplementLinearAlgorithm() {
		var algo = InterpolationAlgorithm<Real>.Linear();
		AssertToleranceEquals(0f, GetValue(algo, 0f), TestTolerance);
		AssertToleranceEquals(0.25f, GetValue(algo, 0.25f), TestTolerance);
		AssertToleranceEquals(0.5f, GetValue(algo, 0.5f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(algo, 1f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyImplementAccelerateAlgorithm() {
		var quadratic = InterpolationAlgorithm<Real>.AccelerateFromSlow(2f);
		AssertToleranceEquals(0f, GetValue(quadratic, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(quadratic, 1f), TestTolerance);
		AssertToleranceEquals(0.25f, GetValue(quadratic, 0.5f), TestTolerance);
		AssertToleranceEquals(0.01f, GetValue(quadratic, 0.1f), TestTolerance);
		AssertToleranceEquals(0.81f, GetValue(quadratic, 0.9f), TestTolerance);

		var cubic = InterpolationAlgorithm<Real>.AccelerateFromSlow(3f);
		AssertToleranceEquals(0f, GetValue(cubic, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(cubic, 1f), TestTolerance);
		AssertToleranceEquals(0.125f, GetValue(cubic, 0.5f), TestTolerance);
		
		var quartic = InterpolationAlgorithm<Real>.AccelerateFromSlow(4f);
		AssertToleranceEquals(0f, GetValue(quartic, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(quartic, 1f), TestTolerance);
		AssertToleranceEquals(0.0625f, GetValue(quartic, 0.5f), TestTolerance);

		var linear = InterpolationAlgorithm<Real>.AccelerateFromSlow(1f);
		AssertToleranceEquals(0.5f, GetValue(linear, 0.5f), TestTolerance);

		Assert.Less(GetValue(quadratic, 0.5f).AsFloat, 0.5f);
		Assert.Less(GetValue(cubic, 0.5f).AsFloat, 0.5f);
	}

	[Test]
	public void ShouldCorrectlyImplementDecelerateAlgorithm() {
		var quadratic = InterpolationAlgorithm<Real>.DecelerateFromFast(2f);
		AssertToleranceEquals(0f, GetValue(quadratic, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(quadratic, 1f), TestTolerance);
		AssertToleranceEquals(0.75f, GetValue(quadratic, 0.5f), TestTolerance);
		AssertToleranceEquals(0.19f, GetValue(quadratic, 0.1f), TestTolerance);
		AssertToleranceEquals(0.99f, GetValue(quadratic, 0.9f), TestTolerance);

		var cubic = InterpolationAlgorithm<Real>.DecelerateFromFast(3f);
		AssertToleranceEquals(0f, GetValue(cubic, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(cubic, 1f), TestTolerance);
		AssertToleranceEquals(0.875f, GetValue(cubic, 0.5f), TestTolerance);
		
		var quartic = InterpolationAlgorithm<Real>.DecelerateFromFast(4f);
		AssertToleranceEquals(0f, GetValue(quartic, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(quartic, 1f), TestTolerance);
		AssertToleranceEquals(0.9375f, GetValue(quartic, 0.5f), TestTolerance);

		Assert.Greater(GetValue(quadratic, 0.5f).AsFloat, 0.5f);
		Assert.Greater(GetValue(cubic, 0.5f).AsFloat, 0.5f);
	}

	[Test]
	public void ShouldCorrectlyImplementAccelerateDecelerateAlgorithm() {
		var algo = InterpolationAlgorithm<Real>.Natural();
		AssertToleranceEquals(0f, GetValue(algo, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(algo, 1f), TestTolerance);
		AssertToleranceEquals(0.5f, GetValue(algo, 0.5f), TestTolerance);
		AssertToleranceEquals(0.028f, GetValue(algo, 0.1f), TestTolerance);
		AssertToleranceEquals(0.972f, GetValue(algo, 0.9f), TestTolerance);

		var atQuarter = GetValue(algo, 0.25f).AsFloat;
		var atThreeQuarters = GetValue(algo, 0.75f).AsFloat;
		AssertToleranceEquals(1f, atQuarter + atThreeQuarters, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyImplementAccelerateWithInitialReverseAlgorithm() {
		var algo = InterpolationAlgorithm<Real>.AccelerateFromSlowWithInitialReverse();
		AssertToleranceEquals(0f, GetValue(algo, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(algo, 1f), TestTolerance);
		AssertToleranceEquals((-0.0877f), GetValue(algo, 0.5f), TestTolerance);
		Assert.Less(GetValue(algo, 0.5f).AsFloat, 0f);

		var mild = InterpolationAlgorithm<Real>.AccelerateFromSlowWithInitialReverse(1f);
		AssertToleranceEquals(0f, GetValue(mild, 0.5f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyImplementDecelerateWithOvershootAlgorithm() {
		var algo = InterpolationAlgorithm<Real>.DecelerateFromFastWithOvershoot();
		AssertToleranceEquals(0f, GetValue(algo, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(algo, 1f), TestTolerance);
		AssertToleranceEquals(1.0877f, GetValue(algo, 0.5f), TestTolerance);
		Assert.Greater(GetValue(algo, 0.5f).AsFloat, 1f);
	}

	[Test]
	public void ShouldCorrectlyImplementCubicBezierAlgorithm() {
		const float LocalTestTolerance = 0.01f;
		
		var linearBezier = InterpolationAlgorithm<Real>.CubicBezier((0f, 0f), (1f, 1f));
		AssertToleranceEquals(0f, GetValue(linearBezier, 0f), LocalTestTolerance);
		AssertToleranceEquals(0.5f, GetValue(linearBezier, 0.5f), LocalTestTolerance);
		AssertToleranceEquals(1f, GetValue(linearBezier, 1f), LocalTestTolerance);
		AssertToleranceEquals(-0.5f, GetValue(linearBezier, -0.5f), LocalTestTolerance);
		AssertToleranceEquals(-1f, GetValue(linearBezier, -1f), LocalTestTolerance);
		AssertToleranceEquals(1.5f, GetValue(linearBezier, 1.5f), LocalTestTolerance);
		AssertToleranceEquals(2f, GetValue(linearBezier, 2f), LocalTestTolerance);

		var cssEaseIn = InterpolationAlgorithm<Real>.CubicBezier((0.42f, 0f), (1f, 1f));
		AssertToleranceEquals(0f, GetValue(cssEaseIn, 0f), LocalTestTolerance);
		AssertToleranceEquals(1f, GetValue(cssEaseIn, 1f), LocalTestTolerance);
		Assert.Less(GetValue(cssEaseIn, 0.5f).AsFloat, 0.5f);
		AssertToleranceEquals(0f, GetValue(cssEaseIn, -0.5f), LocalTestTolerance);
		AssertToleranceEquals(1.862f, GetValue(cssEaseIn, 1.5f), LocalTestTolerance);

		var cssEaseOut = InterpolationAlgorithm<Real>.CubicBezier((0f, 0f), (0.58f, 1f));
		AssertToleranceEquals(0f, GetValue(cssEaseOut, 0f), LocalTestTolerance);
		AssertToleranceEquals(1f, GetValue(cssEaseOut, 1f), LocalTestTolerance);
		Assert.Greater(GetValue(cssEaseOut, 0.5f).AsFloat, 0.5f);
		
		// These test values taken from https://cubic-bezier.com/#.8,-0.44,0,1.23
		var urlExampleBezier = InterpolationAlgorithm<Real>.CubicBezier((0.8f, -0.44f), (0f, 1.23f));
		AssertToleranceEquals(0f, GetValue(urlExampleBezier, 0f), LocalTestTolerance);
		AssertToleranceEquals(-0.08f, GetValue(urlExampleBezier, 0.21f), LocalTestTolerance);
		AssertToleranceEquals(0.48f, GetValue(urlExampleBezier, 0.43f), LocalTestTolerance);
		AssertToleranceEquals(0.97f, GetValue(urlExampleBezier, 0.64f), LocalTestTolerance);
		AssertToleranceEquals(1f, GetValue(urlExampleBezier, 1f), LocalTestTolerance);
		AssertToleranceEquals(0.885f, GetValue(urlExampleBezier, 1.5f), LocalTestTolerance); // final slope = (1-1.23)/(1-0) = -0.23

		var ease = InterpolationAlgorithm<Real>.CubicBezier((0.25f, 0.1f), (0.25f, 1f));
		for (var i = 1; i <= 10; ++i) {
			Assert.GreaterOrEqual(GetValue(ease, i * 0.1f).AsFloat, GetValue(ease, (i - 1) * 0.1f).AsFloat);
		}
		AssertToleranceEquals(-0.2f, GetValue(ease, -0.5f), LocalTestTolerance);
		AssertToleranceEquals(-0.4f, GetValue(ease, -1f), LocalTestTolerance);
		AssertToleranceEquals(1f, GetValue(ease, 1.5f), LocalTestTolerance);
		AssertToleranceEquals(1f, GetValue(ease, 2f), LocalTestTolerance);
	}
}
