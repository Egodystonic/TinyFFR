// Created on 2024-02-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class XYPairTest {
	[Test]
	public void ShouldCorrectlyAddAndSubtract() {
		Assert.AreEqual(XYPair<float>.Zero, XYPair<float>.Zero + XYPair<float>.Zero);
		Assert.AreEqual(XYPair<float>.Zero, XYPair<float>.Zero - XYPair<float>.Zero);
		Assert.AreEqual(ThreeFourFloat * 2f, ThreeFourFloat + ThreeFourFloat);
		Assert.AreEqual(ThreeFourFloat * 2, ThreeFourFloat + ThreeFourFloat);
		Assert.AreEqual(XYPair<float>.Zero, ThreeFourFloat - ThreeFourFloat);
		Assert.AreEqual(new XYPair<float>(2f, 4f), new XYPair<float>(-1f, -2f) + new XYPair<float>(3f, 6f));
		Assert.AreEqual(new XYPair<float>(-4f, -8f), new XYPair<float>(-1f, -2f) - new XYPair<float>(3f, 6f));
	}

	[Test]
	public void ShouldCorrectlyReverse() {
		Assert.AreEqual(XYPair<float>.Zero, -XYPair<float>.Zero);
		Assert.AreEqual(new XYPair<float>(-3f, -4f), -ThreeFourFloat);
		Assert.AreEqual(new XYPair<float>(-1f, -1f), new XYPair<float>(1f, 1f).Negated);
	}

	[Test]
	public void ShouldCorrectlyReciprocate() {
		Assert.AreEqual(null, XYPair<float>.Zero.Reciprocal);
		Assert.AreEqual(null, new XYPair<int>(0, 1).Reciprocal);
		Assert.AreEqual(null, new XYPair<int>(1, 0).Reciprocal);
		Assert.AreEqual(new XYPair<float>(1f / 3f, 1f / 4f), ThreeFourFloat.Reciprocal);
		Assert.AreEqual(new XYPair<float>(-1f / 3f, -1f / 4f), -ThreeFourFloat.Reciprocal);
	}

	[Test]
	public void ShouldCorrectlyCalculatePolarAngle() {
		for (var x = -1f; x <= 1.05f; x += 0.05f) {
			for (var y = -1f; y <= 1.05f; y += 0.05f) {
				Assert.AreEqual(new XYPair<float>(x, y).PolarAngle, Angle.From2DPolarAngle(x, y));
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCalculateAbsolute() {
		Assert.AreEqual(3f, ThreeFourFloat.Absolute.X);
		Assert.AreEqual(4f, ThreeFourFloat.Absolute.Y);

		Assert.AreEqual(3f, new XYPair<float>(-3f, -4f).Absolute.X);
		Assert.AreEqual(4f, new XYPair<float>(-3f, -4f).Absolute.Y);
	}

	[Test]
	public void ShouldCorrectlyScale() {
		void AssertForType<T>() where T : unmanaged, INumber<T> {
			Assert.AreEqual(XYPair<T>.Zero, XYPair<T>.Zero * -10f);
			Assert.AreEqual(XYPair<T>.Zero, XYPair<T>.Zero * 0f);
			Assert.AreEqual(XYPair<T>.Zero, XYPair<T>.Zero * 10f);
			Assert.AreEqual(XYPair<T>.Zero, new XYPair<T>(T.CreateChecked(1), T.CreateChecked(2)) * 0f);
			AssertToleranceEquals(new XYPair<T>(T.CreateChecked(2), T.CreateChecked(4)), new XYPair<T>(T.CreateChecked(1), T.CreateChecked(2)) * 2f, TestTolerance);
			AssertToleranceEquals(new XYPair<T>(T.CreateChecked(-2), T.CreateChecked(-4)), new XYPair<T>(T.CreateChecked(1), T.CreateChecked(2)) * -2f, TestTolerance);

			for (var x = -5; x <= 5; x += 1) {
				for (var y = -5; y <= 5; y += 1) {
					var v = new XYPair<T>(T.CreateChecked(x), T.CreateChecked(y));

					AssertToleranceEquals(v * x, x * v, TestTolerance);
					
					if (x == 0) continue;
					AssertToleranceEquals(v * (1f / x), v / x, TestTolerance);
				}
			}
		}

		AssertForType<int>();
		AssertForType<float>();
		AssertForType<long>();
		AssertForType<double>();
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		void AssertForType<T>() where T : unmanaged, INumber<T> {
			var nonZeroTestInput = new XYPair<T>(T.CreateSaturating(-200f), T.CreateSaturating(400f));

			AssertToleranceEquals(nonZeroTestInput, XYPair<T>.Interpolate(nonZeroTestInput, XYPair<T>.Zero, 0f), TestTolerance);
			AssertToleranceEquals(XYPair<T>.Zero, XYPair<T>.Interpolate(nonZeroTestInput, XYPair<T>.Zero, 1f), TestTolerance);
			AssertToleranceEquals(XYPair<T>.FromVector2(nonZeroTestInput.ToVector2() * 0.5f), XYPair<T>.Interpolate(nonZeroTestInput, XYPair<T>.Zero, 0.5f), TestTolerance);
			AssertToleranceEquals(XYPair<T>.FromVector2(nonZeroTestInput.ToVector2() * 2f), XYPair<T>.Interpolate(nonZeroTestInput, XYPair<T>.Zero, -1f), TestTolerance);
			AssertToleranceEquals(XYPair<T>.FromVector2(nonZeroTestInput.ToVector2() * -1f), XYPair<T>.Interpolate(nonZeroTestInput, XYPair<T>.Zero, 2f), TestTolerance);

			var testList = new List<XYPair<T>>();
			for (var x = -5f; x <= 5f; x += 1f) {
				for (var y = -5f; y <= 5f; y += 1f) {
					testList.Add(new(T.CreateSaturating(x * 100f), T.CreateSaturating(y * 100f)));
				}
			}
			for (var i = 0; i < testList.Count; ++i) {
				for (var j = i; j < testList.Count; ++j) {
					var start = testList[i];
					var end = testList[j];

					for (var f = -1f; f <= 2f; f += 0.1f) {
						AssertToleranceEquals(
							new(
								T.CreateSaturating(Single.CreateSaturating(end.X - start.X) * f + Single.CreateSaturating(start.X)), 
								T.CreateSaturating(Single.CreateSaturating(end.Y - start.Y) * f + Single.CreateSaturating(start.Y))
							), 
							XYPair<T>.Interpolate(start, end, f), 
							TestTolerance + 1
						);
					}
				}
			}
		}

		AssertForType<int>();
		AssertForType<float>();
		AssertForType<long>();
		AssertForType<double>();
	}

	[Test]
	public void ShouldCorrectlyCreateNonBoundedRandomValues() {
		const int NumIterations = 10_000;

		void AssertForType<T>() where T : unmanaged, INumber<T> {
			for (var i = 0; i < NumIterations; ++i) {
				var val = XYPair<T>.NewRandom();
				Assert.GreaterOrEqual(val.X, T.CreateChecked(-XYPair<T>.DefaultRandomRange));
				Assert.GreaterOrEqual(val.Y, T.CreateChecked(-XYPair<T>.DefaultRandomRange));
				Assert.LessOrEqual(val.X, T.CreateChecked(XYPair<T>.DefaultRandomRange));
				Assert.LessOrEqual(val.Y, T.CreateChecked(XYPair<T>.DefaultRandomRange));
			}
		}

		AssertForType<int>();
		AssertForType<float>();
		AssertForType<long>();
		AssertForType<double>();
	}

	[Test]
	public void ShouldCorrectlyCreateBoundedRandomValues() {
		const int NumIterations = 10_000;

		void AssertForType<T>() where T : unmanaged, INumber<T> {
			for (var i = 0; i < NumIterations; ++i) {
				var val = XYPair<T>.NewRandom(T.CreateChecked(-1000), T.CreateChecked(1000));
				Assert.GreaterOrEqual(val.X, T.CreateChecked(-1000));
				Assert.GreaterOrEqual(val.Y, T.CreateChecked(-1000));
				Assert.LessOrEqual(val.X, T.CreateChecked(1000));
				Assert.LessOrEqual(val.Y, T.CreateChecked(1000));

				val = XYPair<T>.NewRandom((T.CreateChecked(-500), T.CreateChecked(-200)), (T.CreateChecked(500), T.CreateChecked(200)));
				Assert.GreaterOrEqual(val.X, T.CreateChecked(-500));
				Assert.GreaterOrEqual(val.Y, T.CreateChecked(-200));
				Assert.LessOrEqual(val.X, T.CreateChecked(500));
				Assert.LessOrEqual(val.Y, T.CreateChecked(200));
			}
		}

		AssertForType<int>();
		AssertForType<float>();
		AssertForType<long>();
		AssertForType<double>();
	}

	[Test]
	public void ShouldCorrectlyCalculateLength() {
		Assert.AreEqual(MathF.Sqrt(9f + 16f), ThreeFourFloat.Length, TestTolerance);
		Assert.AreEqual(9f + 16f, ThreeFourFloat.LengthSquared, TestTolerance);

		Assert.AreEqual(MathF.Sqrt(9f + 16f), ThreeFourFloat.Cast<int>().Length);
		Assert.AreEqual(9f + 16f, ThreeFourFloat.Cast<int>().LengthSquared);
	}

	[Test]
	public void ShouldCorrectlyCalculateDistance() {
		Assert.AreEqual(10f, ThreeFourFloat.DistanceFrom(-ThreeFourFloat), TestTolerance);
		Assert.AreEqual(5f, ThreeFourFloat.DistanceFrom(default), TestTolerance);
		Assert.AreEqual(5f, ThreeFourFloat.Negated.DistanceFrom(default), TestTolerance);

		Assert.AreEqual(100f, ThreeFourFloat.DistanceSquaredFrom(-ThreeFourFloat), TestTolerance);
		Assert.AreEqual(25f, ThreeFourFloat.DistanceSquaredFrom(default), TestTolerance);
		Assert.AreEqual(25f, ThreeFourFloat.Negated.DistanceSquaredFrom(default), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCast() {
		Assert.AreEqual(3, ThreeFourFloat.Cast<int>().X);
		Assert.AreEqual(4, ThreeFourFloat.Cast<int>().Y);
		Assert.AreEqual(3f, new XYPair<int>(3, 4).Cast<float>().X);
		Assert.AreEqual(4f, new XYPair<int>(3, 4).Cast<float>().Y);
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		Assert.AreEqual(ThreeFourFloat, ThreeFourFloat.Clamp((2f, 3f), (4f, 5f)));
		Assert.AreEqual(ThreeFourFloat, ThreeFourFloat.Clamp((4f, 5f), (2f, 3f)));
		Assert.AreEqual(ThreeFourFloat, ThreeFourFloat.Clamp((3f, 4f), (3f, 4f)));

		Assert.AreEqual(new XYPair<float>(2f, 5f), new XYPair<float>(1f, 6f).Clamp((2f, 3f), (4f, 5f)));
		Assert.AreEqual(new XYPair<float>(4f, 3f), new XYPair<float>(5f, 2f).Clamp((2f, 3f), (4f, 5f)));
		Assert.AreEqual(new XYPair<float>(2f, 5f), new XYPair<float>(1f, 6f).Clamp((4f, 3f), (2f, 5f)));
		Assert.AreEqual(new XYPair<float>(4f, 3f), new XYPair<float>(5f, 2f).Clamp((4f, 3f), (2f, 5f)));
	}
}