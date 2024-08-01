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
			Assert.GreaterOrEqual(new Angle(f).Normalized.AsDegrees, 0f);
			Assert.LessOrEqual(new Angle(f).Normalized.AsDegrees, 360f);
		}
	}

	[Test]
	public void ShouldCorrectlyCalculateNormalizedDifferences() {
		AssertToleranceEquals(0f, Angle.Zero.AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, Angle.Zero.AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, Angle.Zero.AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.Zero.AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.Zero.AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(180f, Angle.HalfCircle.AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(180f, Angle.HalfCircle.AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(0f, Angle.HalfCircle.AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.HalfCircle.AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.HalfCircle.AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(180f, new Angle(-180f).AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(180f, new Angle(-180f).AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(0f, new Angle(-180f).AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(-180f).AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(-180f).AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, Angle.FullCircle.AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, Angle.FullCircle.AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, Angle.FullCircle.AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.FullCircle.AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.FullCircle.AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, new Angle(720f).AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, new Angle(720f).AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, new Angle(720f).AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(720f).AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(720f).AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, new Angle(360f).AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, new Angle(360f).AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, new Angle(360f).AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(360f).AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(360f).AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, Angle.Zero.AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, Angle.Zero.AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, Angle.Zero.AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.Zero.AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.Zero.AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(180f, Angle.HalfCircle.AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(180f, Angle.HalfCircle.AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(0f, Angle.HalfCircle.AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.HalfCircle.AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.HalfCircle.AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(180f, new Angle(-180f).AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(180f, new Angle(-180f).AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(0f, new Angle(-180f).AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(-180f).AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(-180f).AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, Angle.FullCircle.AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, Angle.FullCircle.AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, Angle.FullCircle.AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.FullCircle.AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.FullCircle.AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, new Angle(720f).AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, new Angle(720f).AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, new Angle(720f).AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(720f).AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(720f).AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, new Angle(360f).AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, new Angle(360f).AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, new Angle(360f).AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(360f).AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(360f).AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyMultiplyAndDivide() {
		for (var f = -MathF.Tau * 2f; f < MathF.Tau * 2.05f; f += MathF.Tau * 0.05f) {
			for (var s = -2f; s < 2.1f; s += 0.1f) {
				Assert.AreEqual(Angle.FromRadians(f * s), Angle.FromRadians(f) * s);
				Assert.AreEqual(Angle.FromRadians(f * s), s * Angle.FromRadians(f));
				Assert.AreEqual(Angle.FromRadians(f * s), Angle.FromRadians(f).ScaledBy(s));
				if (MathF.Abs(s) < 0.001f) continue;
				Assert.AreEqual(Angle.FromRadians(f / s), Angle.FromRadians(f) / s);
				AssertToleranceEquals(Angle.FromRadians(f / s), Angle.FromRadians(f).ScaledBy(1f / s), 0.01f);
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
		void AssertZeroToHalf(Angle input, Angle expectedOutput) {
			Assert.AreEqual(expectedOutput, input.ClampZeroToHalfCircle());
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.Zero, Angle.HalfCircle));
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.HalfCircle, Angle.Zero));
		}

		void AssertZeroToFull(Angle input, Angle expectedOutput) {
			Assert.AreEqual(expectedOutput, input.ClampZeroToFullCircle());
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.Zero, Angle.FullCircle));
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.FullCircle, Angle.Zero));
		}

		void AssertNegFullToFull(Angle input, Angle expectedOutput) {
			Assert.AreEqual(expectedOutput, input.ClampNegativeFullCircleToFullCircle());
			Assert.AreEqual(expectedOutput, input.Clamp(-Angle.FullCircle, Angle.FullCircle));
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.FullCircle, -Angle.FullCircle));
		}

		void AssertNegHalfToHalf(Angle input, Angle expectedOutput) {
			Assert.AreEqual(expectedOutput, input.ClampNegativeHalfCircleToHalfCircle());
			Assert.AreEqual(expectedOutput, input.Clamp(-Angle.HalfCircle, Angle.HalfCircle));
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.HalfCircle, -Angle.HalfCircle));
		}

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

		AssertNegHalfToHalf(0f, 0f);
		AssertNegHalfToHalf(90f, 90f);
		AssertNegHalfToHalf(180f, 180f);
		AssertNegHalfToHalf(270f, 180f);
		AssertNegHalfToHalf(360f, 180f);
		AssertNegHalfToHalf(450f, 180f);
		AssertNegHalfToHalf(-90f, -90f);
		AssertNegHalfToHalf(-180f, -180f);
		AssertNegHalfToHalf(-270f, -180f);
		AssertNegHalfToHalf(-360f, -180f);
		AssertNegHalfToHalf(-450f, -180f);

		Assert.AreEqual(new Angle(100f), new Angle(0f).Clamp(new Angle(100f), new Angle(100f)));
	}

	[Test]
	public void ShouldCorrectlyImplementComparisonOperators() {
		var angleList = new[] { -Angle.FullCircle, -Angle.HalfCircle, Angle.Zero, Angle.HalfCircle, Angle.FullCircle };

		for (var i = 0; i < angleList.Length; ++i) {
			for (var j = i; j < angleList.Length; ++j) {
				var lhs = angleList[i];
				var rhs = angleList[j];

				Assert.AreEqual(lhs.AsRadians > rhs.AsRadians, lhs > rhs);
				Assert.AreEqual(lhs.AsRadians >= rhs.AsRadians, lhs >= rhs);
				Assert.AreEqual(lhs.AsRadians < rhs.AsRadians, lhs < rhs);
				Assert.AreEqual(lhs.AsRadians <= rhs.AsRadians, lhs <= rhs);
				Assert.AreEqual(lhs.AsRadians.CompareTo(rhs.AsRadians), lhs.CompareTo(rhs));
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCalculatePolarDirection() {
		Assert.AreEqual(Orientation2D.Right, Angle.From2DPolarAngle(1f, 0f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.UpRight, Angle.From2DPolarAngle(1f, 1f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.Up, Angle.From2DPolarAngle(0f, 1f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.UpLeft, Angle.From2DPolarAngle(-1f, 1f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.Left, Angle.From2DPolarAngle(-1f, 0f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.DownLeft, Angle.From2DPolarAngle(-1f, -1f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.Down, Angle.From2DPolarAngle(0f, -1f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.DownRight, Angle.From2DPolarAngle(1f, -1f)!.Value.PolarOrientation);
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

	[Test]
	public void ShouldCorrectlyCreateNonBoundedRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = Angle.NewRandom();
			Assert.GreaterOrEqual(val.AsDegrees, 0f);
			Assert.Less(val.AsDegrees, 360f);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateBoundedRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = Angle.NewRandom(-720f, 720f);
			Assert.GreaterOrEqual(val.AsDegrees, -720f);
			Assert.Less(val.AsDegrees, 720f);
		}
	}
}