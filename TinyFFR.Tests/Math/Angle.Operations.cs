// Created on 2023-09-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class AngleTest {
	[Test]
	public void ShouldCorrectlyNegateAngle() {
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.AreEqual(new Angle(-f), new Angle(f).Negated);
			Assert.AreEqual(new Angle(-f), -new Angle(f));
		}
	}

	[Test]
	public void ShouldCorrectlyReturnAbsoluteAngle() {
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.AreEqual(new Angle(MathF.Abs(f)), new Angle(f).Absolute);
		}
	}

	[Test]
	public void ShouldCorrectlyReturnNormalizedAngle() {
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			AssertToleranceEquals(new Angle(MathUtils.TrueModulus(f, 360f)), new Angle(f).Normalized, TestTolerance);
			Assert.AreEqual(new Angle(f).Sine, new Angle(f).Normalized.Sine, TestTolerance);
			Assert.GreaterOrEqual(new Angle(f).Normalized.Degrees, 0f);
			Assert.LessOrEqual(new Angle(f).Normalized.Degrees, 360f);
		}
	}

	[Test]
	public void ShouldCorrectlyMultiplyAndDivide() {
		for (var f = -MathF.Tau * 2f; f < MathF.Tau * 2.05f; f += MathF.Tau * 0.05f) {
			for (var s = -2f; s < 2.1f; s += 0.1f) {
				Assert.AreEqual(Angle.FromRadians(f * s), Angle.FromRadians(f) * s);
				Assert.AreEqual(Angle.FromRadians(f * s), s * Angle.FromRadians(f));
				Assert.AreEqual(Angle.FromRadians(f * s), Angle.FromRadians(f).MultipliedBy(s));
				Assert.AreEqual(Angle.FromRadians(f / s), Angle.FromRadians(f) / s);
				Assert.AreEqual(Angle.FromRadians(f / s), Angle.FromRadians(f).DividedBy(s));
			}
		}
	}

	[Test]
	public void ShouldCorrectlyAddAndSubtract() {
		for (var f = -MathF.Tau * 2f; f < MathF.Tau * 2.05f; f += MathF.Tau * 0.05f) {
			Assert.AreEqual(new Angle(f + f), new Angle(f) + new Angle(f));
			Assert.AreEqual(new Angle(f + f), new Angle(f).Plus(new Angle(f)));
			Assert.AreEqual(new Angle(f - f), new Angle(f) - new Angle(f));
			Assert.AreEqual(new Angle(f - f), new Angle(f).Minus(new Angle(f)));
		}
	}

	[Test]
	public void SineAndCosinePropertiesShouldBeCorrect() {
		foreach (var f in new[] { -1f, -0.75f, -0.5f, -0.25f, 0f, 0.25f, 0.5f, 0.75f, 1f }) {
			Assert.AreEqual(f, Angle.FromSine(f).Sine, TestTolerance);
			Assert.AreEqual(f, Angle.FromCosine(f).Cosine, TestTolerance);
		}
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		void AssertZeroToHalf(Angle input, Angle expectedOutput) => Assert.AreEqual(expectedOutput, input.ClampZeroToHalfCircle());
		void AssertZeroToFull(Angle input, Angle expectedOutput) => Assert.AreEqual(expectedOutput, input.ClampZeroToFullCircle());
		void AssertNegFullToFull(Angle input, Angle expectedOutput) => Assert.AreEqual(expectedOutput, input.ClampNegativeFullCircleToFullCircle());

		AssertZeroToHalf(0f, 0f);
		AssertZeroToHalf(90f, 90f);
		AssertZeroToHalf(180f, 180f);
		AssertZeroToHalf(270f, 180f);
		AssertZeroToHalf(-90f, 0f);
		AssertZeroToHalf(-180f, 0f);

		AssertZeroToFull(0f, 0f);
		AssertZeroToFull(90f, 90f);
		AssertZeroToFull(180f, 180f);
		AssertZeroToFull(270f, 270f);
		AssertZeroToFull(360f, 360f);
		AssertZeroToFull(450f, 360f);
		AssertZeroToFull(-90f, 0f);
		AssertZeroToFull(-180f, 0f);

		AssertNegFullToFull(0f, 0f);
		AssertNegFullToFull(90f, 90f);
		AssertNegFullToFull(180f, 180f);
		AssertNegFullToFull(270f, 270f);
		AssertNegFullToFull(360f, 360f);
		AssertNegFullToFull(450f, 360f);
		AssertNegFullToFull(-90f, -90f);
		AssertNegFullToFull(-180f, -180f);
		AssertNegFullToFull(-270f, -270f);
		AssertNegFullToFull(-360f, -360f);
		AssertNegFullToFull(-450f, -360f);
	}

	[Test]
	public void ShouldCorrectlyImplementComparisonOperators() {
		var angleList = new[] { -Angle.FullCircle, -Angle.HalfCircle, Angle.Zero, Angle.HalfCircle, Angle.FullCircle };

		for (var i = 0; i < angleList.Length; ++i) {
			for (var j = i; j < angleList.Length; ++j) {
				var lhs = angleList[i];
				var rhs = angleList[j];

				Assert.AreEqual(lhs.Radians > rhs.Radians, lhs > rhs);
				Assert.AreEqual(lhs.Radians >= rhs.Radians, lhs >= rhs);
				Assert.AreEqual(lhs.Radians < rhs.Radians, lhs < rhs);
				Assert.AreEqual(lhs.Radians <= rhs.Radians, lhs <= rhs);
				Assert.AreEqual(lhs.Radians.CompareTo(rhs.Radians), lhs.CompareTo(rhs));
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCalculatePolarDirection() {
		Assert.AreEqual(CardinalDirection.Right, Angle.FromPolarAngleAround2DPlane(1f, 0f)!.Value.PolarDirection);
		Assert.AreEqual(CardinalDirection.UpRight, Angle.FromPolarAngleAround2DPlane(1f, 1f)!.Value.PolarDirection);
		Assert.AreEqual(CardinalDirection.Up, Angle.FromPolarAngleAround2DPlane(0f, 1f)!.Value.PolarDirection);
		Assert.AreEqual(CardinalDirection.UpLeft, Angle.FromPolarAngleAround2DPlane(-1f, 1f)!.Value.PolarDirection);
		Assert.AreEqual(CardinalDirection.Left, Angle.FromPolarAngleAround2DPlane(-1f, 0f)!.Value.PolarDirection);
		Assert.AreEqual(CardinalDirection.DownLeft, Angle.FromPolarAngleAround2DPlane(-1f, -1f)!.Value.PolarDirection);
		Assert.AreEqual(CardinalDirection.Down, Angle.FromPolarAngleAround2DPlane(0f, -1f)!.Value.PolarDirection);
		Assert.AreEqual(CardinalDirection.DownRight, Angle.FromPolarAngleAround2DPlane(1f, -1f)!.Value.PolarDirection);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		AssertToleranceEquals(0f, Angle.Interpolate(-100f, 100f, 0.5f), TestTolerance);
		AssertToleranceEquals(-100f, Angle.Interpolate(-100f, 100f, 0f), TestTolerance);
		AssertToleranceEquals(100f, Angle.Interpolate(-100f, 100f, 1f), TestTolerance);
		AssertToleranceEquals(-200f, Angle.Interpolate(-100f, 100f, -0.5f), TestTolerance);
		AssertToleranceEquals(200f, Angle.Interpolate(-100f, 100f, 1.5f), TestTolerance);

		AssertToleranceEquals(30f, Angle.Interpolate(30f, 30f, -1f), TestTolerance);
		AssertToleranceEquals(30f, Angle.Interpolate(30f, 30f, 0f), TestTolerance);
		AssertToleranceEquals(30f, Angle.Interpolate(30f, 30f, 0.5f), TestTolerance);
		AssertToleranceEquals(30f, Angle.Interpolate(30f, 30f, 1f), TestTolerance);
		AssertToleranceEquals(30f, Angle.Interpolate(30f, 30f, 2f), TestTolerance);
	}
}