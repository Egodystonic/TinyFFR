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
	public void ShouldCorrectlyInterpolateLinearly() {
		var algo = InterpolationAlgorithm<Real>.Linear();
		AssertToleranceEquals(0f, GetValue(algo, 0f), TestTolerance);
		AssertToleranceEquals(0.25f, GetValue(algo, 0.25f), TestTolerance);
		AssertToleranceEquals(0.5f, GetValue(algo, 0.5f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(algo, 1f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyInterpolatePowerEaseIn() {
		var quadratic = InterpolationAlgorithm<Real>.Accelerate(2f);
		AssertToleranceEquals(0f, GetValue(quadratic, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(quadratic, 1f), TestTolerance);
		AssertToleranceEquals(0.25f, GetValue(quadratic, 0.5f), TestTolerance);
		AssertToleranceEquals(0.01f, GetValue(quadratic, 0.1f), TestTolerance);
		AssertToleranceEquals(0.81f, GetValue(quadratic, 0.9f), TestTolerance);

		var cubic = InterpolationAlgorithm<Real>.Accelerate(3f);
		AssertToleranceEquals(0f, GetValue(cubic, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(cubic, 1f), TestTolerance);
		AssertToleranceEquals(0.125f, GetValue(cubic, 0.5f), TestTolerance);

		var linear = InterpolationAlgorithm<Real>.Accelerate(1f);
		AssertToleranceEquals(0.5f, GetValue(linear, 0.5f), TestTolerance);

		Assert.Less(GetValue(quadratic, 0.5f).AsFloat, 0.5f);
		Assert.Less(GetValue(cubic, 0.5f).AsFloat, 0.5f);
	}

	[Test]
	public void ShouldCorrectlyInterpolatePowerEaseOut() {
		var quadratic = InterpolationAlgorithm<Real>.Decelerate(2f);
		AssertToleranceEquals(0f, GetValue(quadratic, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(quadratic, 1f), TestTolerance);
		AssertToleranceEquals(0.75f, GetValue(quadratic, 0.5f), TestTolerance);
		AssertToleranceEquals(0.19f, GetValue(quadratic, 0.1f), TestTolerance);
		AssertToleranceEquals(0.99f, GetValue(quadratic, 0.9f), TestTolerance);

		var cubic = InterpolationAlgorithm<Real>.Decelerate(3f);
		AssertToleranceEquals(0f, GetValue(cubic, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(cubic, 1f), TestTolerance);
		AssertToleranceEquals(0.875f, GetValue(cubic, 0.5f), TestTolerance);

		Assert.Greater(GetValue(quadratic, 0.5f).AsFloat, 0.5f);
		Assert.Greater(GetValue(cubic, 0.5f).AsFloat, 0.5f);
	}

	[Test]
	public void ShouldCorrectlyInterpolateSmoothstep() {
		var algo = InterpolationAlgorithm<Real>.AccelerateDecelerate();
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
	public void ShouldCorrectlyInterpolateOvershootEaseIn() {
		var algo = InterpolationAlgorithm<Real>.AccelerateWithInitialReverse();
		AssertToleranceEquals(0f, GetValue(algo, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(algo, 1f), TestTolerance);
		AssertToleranceEquals((-0.0877f), GetValue(algo, 0.5f), TestTolerance);
		Assert.Less(GetValue(algo, 0.5f).AsFloat, 0f);

		var mild = InterpolationAlgorithm<Real>.AccelerateWithInitialReverse(1f);
		AssertToleranceEquals(0f, GetValue(mild, 0.5f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyInterpolateOvershootEaseOut() {
		var algo = InterpolationAlgorithm<Real>.DecelerateWithOvershoot();
		AssertToleranceEquals(0f, GetValue(algo, 0f), TestTolerance);
		AssertToleranceEquals(1f, GetValue(algo, 1f), TestTolerance);
		AssertToleranceEquals(1.0877f, GetValue(algo, 0.5f), TestTolerance);
		Assert.Greater(GetValue(algo, 0.5f).AsFloat, 1f);
	}

	[Test]
	public void ShouldCorrectlyInterpolateCubicBezier() {
		const float LocalTestTolerance = 0.01f;
		
		var linearBezier = InterpolationAlgorithm<Real>.CubicBezier((0f, 0f), (1f, 1f));
		AssertToleranceEquals(0f, GetValue(linearBezier, 0f), LocalTestTolerance);
		AssertToleranceEquals(0.5f, GetValue(linearBezier, 0.5f), LocalTestTolerance);
		AssertToleranceEquals(1f, GetValue(linearBezier, 1f), LocalTestTolerance);

		var cssEaseIn = InterpolationAlgorithm<Real>.CubicBezier((0.42f, 0f), (1f, 1f));
		AssertToleranceEquals(0f, GetValue(cssEaseIn, 0f), LocalTestTolerance);
		AssertToleranceEquals(1f, GetValue(cssEaseIn, 1f), LocalTestTolerance);
		Assert.Less(GetValue(cssEaseIn, 0.5f).AsFloat, 0.5f);

		var cssEaseOut = InterpolationAlgorithm<Real>.CubicBezier((0f, 0f), (0.58f, 1f));
		AssertToleranceEquals(0f, GetValue(cssEaseOut, 0f), LocalTestTolerance);
		AssertToleranceEquals(1f, GetValue(cssEaseOut, 1f), LocalTestTolerance);
		Assert.Greater(GetValue(cssEaseOut, 0.5f).AsFloat, 0.5f);

		var ease = InterpolationAlgorithm<Real>.CubicBezier((0.25f, 0.1f), (0.25f, 1f));
		var prevValue = 0f;
		for (var i = 1; i <= 10; ++i) {
			var value = GetValue(ease, i / 10f).AsFloat;
			Assert.GreaterOrEqual(value, prevValue);
			prevValue = value;
		}
	}
}
