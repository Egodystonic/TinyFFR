// Created on 2023-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class VectTest {
	[Test]
	public void ShouldCorrectlyCalculateLengthAndLengthSquared() {
		Assert.AreEqual(0f, Vect.Zero.Length);
		Assert.AreEqual(0f, Vect.Zero.LengthSquared);
		Assert.AreEqual(MathF.Sqrt(1f + 4f + 9f), OneTwoNegThree.Length);
		Assert.AreEqual(1f + 4f + 9f, OneTwoNegThree.LengthSquared);

		Assert.AreEqual(OneTwoNegThree.Length, new Vect(-1f, -2f, 3f).Length);
		Assert.AreEqual(OneTwoNegThree.LengthSquared, new Vect(-1f, -2f, 3f).LengthSquared);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var v = new Vect(x, y, z);

					Assert.AreEqual(Math.Sqrt(x * x + y * y + z * z), v.Length, TestTolerance);

					Assert.AreEqual(v.Length, (-v).Length);
					Assert.AreEqual(v.Length * v.Length, v.LengthSquared, TestTolerance);
					Assert.AreEqual(v.Length * 2f, (v * 2f).Length);
					Assert.IsTrue(v.Length >= 0f);
					Assert.IsTrue((v.Length > 1f && v.LengthSquared > v.Length) || (v.Length <= 1f && v.LengthSquared <= v.Length));
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineIfIsNormalized() {
		Assert.AreEqual(false, Vect.Zero.IsNormalized);
		Assert.AreEqual(false, Vect.Zero.Normalized.IsNormalized);
		Assert.AreEqual(false, OneTwoNegThree.IsNormalized);
		Assert.AreEqual(true, OneTwoNegThree.Normalized.IsNormalized);
		Assert.AreEqual(true, new Vect(1f, 0f, 0f).IsNormalized);
		Assert.AreEqual(true, new Vect(0f, -1f, 0f).IsNormalized);
		Assert.AreEqual(true, new Vect(0.707f, 0f, 0.707f).IsNormalized);
		Assert.AreEqual(true, new Vect(0f, 0.707f, -0.707f).IsNormalized);
	}

	[Test]
	public void ShouldCorrectlyReverse() {
		Assert.AreEqual(Vect.Zero, -Vect.Zero);
		Assert.AreEqual(new Vect(-1f, -2f, 3f), -OneTwoNegThree);
		Assert.AreEqual(new Vect(-1f, -1f, -1f), new Vect(1f, 1f, 1f).Reversed);
	}

	[Test]
	public void ShouldCorrectlyAddAndSubtract() {
		Assert.AreEqual(Vect.Zero, Vect.Zero + Vect.Zero);
		Assert.AreEqual(Vect.Zero, Vect.Zero - Vect.Zero);
		Assert.AreEqual(OneTwoNegThree * 2f, OneTwoNegThree + OneTwoNegThree);
		Assert.AreEqual(Vect.Zero, OneTwoNegThree - OneTwoNegThree);
		Assert.AreEqual(new Vect(2f, 4f, 6f), new Vect(-1f, -2f, -3f) + new Vect(3f, 6f, 9f));
		Assert.AreEqual(new Vect(-4f, -8f, 12f), new Vect(-1f, -2f, 9f) - new Vect(3f, 6f, -3f));
	}

	[Test]
	public void ShouldCorrectlyProvideDirection() {
		Assert.AreEqual(Direction.None, Vect.Zero.Direction);
		Assert.AreEqual(new Direction(1f, 0f, 0f), new Vect(40f, 0f, 0f).Direction);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var v = new Vect(x, y, z);
					var vNorm = v.Normalized;

					AssertToleranceEquals(new Direction(vNorm.X, vNorm.Y, vNorm.Z), v.Direction, TestTolerance);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyNormalize() {
		AssertToleranceEquals(new Vect(0.707f, 0f, -0.707f), new Vect(1f, 0f, -1f).Normalized, TestTolerance);
		Assert.AreEqual(Vect.Zero, Vect.Zero.Normalized);
		Assert.AreEqual(Vect.Zero, (-Vect.Zero).Normalized);
		Assert.AreEqual(new Vect(0f, 1f, 0f), new Vect(0f, 0.0001f, 0f).Normalized);
	}

	[Test]
	public void ShouldCorrectlyProjectOnToDirection() {
		Assert.AreEqual(new Vect(1f, 0f, 0f), new Vect(1f, 1f, 0f).ProjectedOnTo(new(1f, 0f, 0f)));
		AssertToleranceEquals(new Vect(1.4142f, 0f, 0f), new Vect(1f, 1f, 0f).ProjectedOnTo(new(1f, 0f, 0f), retainLength: true), TestTolerance);
		Assert.AreEqual(new Vect(1f, 0f, 0f), new Vect(1f, 1f, 0f).ProjectedOnTo(new(1f, 0f, 0f), retainLength: false));

		// https://www.wolframalpha.com/input?i=project+%5B14.2%2C+-7.1%2C+8.9%5D+on+to+%5B0.967%2C+0.137%2C+-0.216%5D
		AssertToleranceEquals(new Vect(10.473f, 1.484f, -2.339f), new Vect(14.2f, -7.1f, 8.9f).ProjectedOnTo(new(0.967f, 0.137f, -0.216f), retainLength: false), TestTolerance);
		Assert.AreEqual(new Vect(14.2f, -7.1f, 8.9f).Length, new Vect(14.2f, -7.1f, 8.9f).ProjectedOnTo(new(0.967f, 0.137f, -0.216f), retainLength: true).Length, TestTolerance);

		Assert.AreEqual(Vect.Zero, new Vect(1f, 0f, 0f).ProjectedOnTo(new Direction(0f, 1f, 0f)));
		Assert.AreEqual(Vect.Zero, new Vect(1f, 0f, 0f).ProjectedOnTo(new Direction(0f, 1f, 0f), retainLength: true));
		Assert.AreEqual(Vect.Zero, new Vect(1f, 0f, 0f).ProjectedOnTo(new Direction(0f, 1f, 0f), retainLength: false));
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstDirection() {
		Assert.AreEqual(Vect.Zero, Vect.Zero.OrthogonalizedAgainst(Direction.Up));
		Assert.AreEqual(Vect.Zero, (Direction.Up * 100f).OrthogonalizedAgainst(Direction.Up));
		AssertToleranceEquals(OneTwoNegThree, OneTwoNegThree.OrthogonalizedAgainst(Direction.None), TestTolerance);
		Assert.AreEqual(Vect.Zero, Vect.Zero.OrthogonalizedAgainst(Direction.None));

		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).OrthogonalizedAgainst(new(0f, 1f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).OrthogonalizedAgainst(new(-1f, 0f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).OrthogonalizedAgainst(new(1f, 0f, 0f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyRescale() {
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLength(-10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLength(0f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLength(10f));
		Assert.AreEqual(OneTwoNegThree, OneTwoNegThree.WithLength(OneTwoNegThree.Length));

		// https://www.wolframalpha.com/input?i=normalize+%5B1%2C+2%2C+-3%5D
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f), OneTwoNegThree.WithLength(1f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * 2f, OneTwoNegThree.WithLength(2f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * -1f, OneTwoNegThree.WithLength(-1f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * -2f, OneTwoNegThree.WithLength(-2f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * 0.5f, OneTwoNegThree.WithLength(0.5f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * -0.5f, OneTwoNegThree.WithLength(-0.5f), TestTolerance);
		Assert.AreEqual(Vect.Zero, OneTwoNegThree.WithLength(0f));
	}

	[Test]
	public void ShouldCorrectlyScale() {
		Assert.AreEqual(Vect.Zero, Vect.Zero * -10f);
		Assert.AreEqual(Vect.Zero, Vect.Zero * 0f);
		Assert.AreEqual(Vect.Zero, Vect.Zero * 10f);
		Assert.AreEqual(new Vect(2f, 4f, -6f), OneTwoNegThree * 2f);
		Assert.AreEqual(new Vect(0.5f, 1f, -1.5f), OneTwoNegThree * 0.5f);
		Assert.AreEqual(new Vect(-3f, -6f, 9f), OneTwoNegThree * -3f);
		Assert.AreEqual(Vect.Zero, OneTwoNegThree * 0f);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var v = new Vect(x, y, z);

					Assert.AreEqual(v * x, x * v);
					Assert.AreEqual(v * (1f / x), v / x);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCompare() {
		Assert.IsFalse(Vect.Zero < -Vect.Zero);
		Assert.IsFalse(Vect.Zero > -Vect.Zero);
		Assert.IsTrue(Vect.Zero <= -Vect.Zero);
		Assert.IsTrue(Vect.Zero >= -Vect.Zero);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var v = new Vect(x, y, z);

					Assert.AreEqual(v > OneTwoNegThree, v.Length > OneTwoNegThree.Length);
					Assert.AreEqual(v >= OneTwoNegThree, v.Length >= OneTwoNegThree.Length);
					Assert.AreEqual(v < OneTwoNegThree, v.Length < OneTwoNegThree.Length);
					Assert.AreEqual(v <= OneTwoNegThree, v.Length <= OneTwoNegThree.Length);
				}
			}
		}
	}
}