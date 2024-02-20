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
		Assert.AreEqual(XYPair<float>.Zero, ThreeFourFloat - ThreeFourFloat);
		Assert.AreEqual(new XYPair<float>(2f, 4f), new XYPair<float>(-1f, -2f) + new XYPair<float>(3f, 6f));
		Assert.AreEqual(new XYPair<float>(-4f, -8f), new XYPair<float>(-1f, -2f) - new XYPair<float>(3f, 6f));
	}

	[Test]
	public void ShouldCorrectlyReverse() {
		Assert.AreEqual(XYPair<float>.Zero, -XYPair<float>.Zero);
		Assert.AreEqual(new XYPair<float>(-3f, -4f), -ThreeFourFloat);
		Assert.AreEqual(new XYPair<float>(-1f, -1f), new XYPair<float>(1f, 1f).Reversed);
	}

	[Test]
	public void ShouldCorrectlyCalculatePolarAngle() {
		for (var x = -1f; x <= 1.05f; x += 0.05f) {
			for (var y = -1f; y <= 1.05f; y += 0.05f) {
				Assert.AreEqual(new XYPair<float>(x, y).PolarAngle, Angle.FromPolarAngleAround2DPlane(x, y));
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
}