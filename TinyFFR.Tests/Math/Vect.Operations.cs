// Created on 2023-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;
using NUnit.Framework.Internal;

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
		Assert.AreEqual(false, Vect.Zero.IsUnitLength);
		Assert.AreEqual(false, Vect.Zero.AsUnitLength.IsUnitLength);
		Assert.AreEqual(false, OneTwoNegThree.IsUnitLength);
		Assert.AreEqual(true, OneTwoNegThree.AsUnitLength.IsUnitLength);
		Assert.AreEqual(true, new Vect(1f, 0f, 0f).IsUnitLength);
		Assert.AreEqual(true, new Vect(0f, -1f, 0f).IsUnitLength);
		Assert.AreEqual(true, new Vect(0.707f, 0f, 0.707f).IsUnitLength);
		Assert.AreEqual(true, new Vect(0f, 0.707f, -0.707f).IsUnitLength);
	}

	[Test]
	public void UnitLengthTestShouldUseAppropriateErrorMargin() {
		Assert.AreEqual(true, new Vect(1f, 0f, 0f).IsUnitLength);
		Assert.AreEqual(true, new Vect(0.9999f, 0f, 0f).IsUnitLength);
		Assert.AreEqual(false, new Vect(0.999f, 0f, 0f).IsUnitLength);
	}

	[Test]
	public void ShouldCorrectlyReverse() {
		Assert.AreEqual(Vect.Zero, -Vect.Zero);
		Assert.AreEqual(new Vect(-1f, -2f, 3f), -OneTwoNegThree);
		Assert.AreEqual(new Vect(-1f, -1f, -1f), new Vect(1f, 1f, 1f).Inverted);
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
					var vNorm = v.AsUnitLength;

					AssertToleranceEquals(new Direction(vNorm.X, vNorm.Y, vNorm.Z), v.Direction, TestTolerance);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyNormalize() {
		AssertToleranceEquals(new Vect(0.707f, 0f, -0.707f), new Vect(1f, 0f, -1f).AsUnitLength, TestTolerance);
		Assert.AreEqual(Vect.Zero, Vect.Zero.AsUnitLength);
		Assert.AreEqual(Vect.Zero, (-Vect.Zero).AsUnitLength);
		Assert.AreEqual(new Vect(0f, 1f, 0f), new Vect(0f, 0.0001f, 0f).AsUnitLength);
	}

	[Test]
	public void ShouldCorrectlyProjectOnToDirection() {
		Assert.AreEqual(new Vect(1f, 0f, 0f), new Vect(1f, 1f, 0f).ProjectedOnTo(new Direction(1f, 0f, 0f)));
		Assert.AreEqual(new Vect(1f, 0f, 0f), new Vect(1f, 1f, 0f).ProjectedOnTo(new Direction(1f, 0f, 0f)));

		// https://www.wolframalpha.com/input?i=project+%5B14.2%2C+-7.1%2C+8.9%5D+on+to+%5B0.967%2C+0.137%2C+-0.216%5D
		AssertToleranceEquals(new Vect(10.473f, 1.484f, -2.339f), new Vect(14.2f, -7.1f, 8.9f).ProjectedOnTo(new Direction(0.967f, 0.137f, -0.216f)), TestTolerance);

		Assert.AreEqual(Vect.Zero, new Vect(1f, 0f, 0f).ProjectedOnTo(new Direction(0f, 1f, 0f)));
		Assert.AreEqual(Vect.Zero, new Vect(1f, 0f, 0f).ProjectedOnTo(new Direction(0f, 1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstDirection() {
		Assert.AreEqual(null, Vect.Zero.OrthogonalizedAgainst(Direction.Up));
		Assert.AreEqual(null, (Direction.Up * 100f).OrthogonalizedAgainst(Direction.Up));
		AssertToleranceEquals(null, OneTwoNegThree.OrthogonalizedAgainst(Direction.None), TestTolerance);
		Assert.AreEqual(null, Vect.Zero.OrthogonalizedAgainst(Direction.None));

		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).OrthogonalizedAgainst(new Direction(0f, 1f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).FastOrthogonalizedAgainst(new Direction(0f, 1f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).OrthogonalizedAgainst(new Direction(-1f, 0f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).FastOrthogonalizedAgainst(new Direction(-1f, 0f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).OrthogonalizedAgainst(new Direction(1f, 0f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).FastOrthogonalizedAgainst(new Direction(1f, 0f, 0f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyRescale() {
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLength(-10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLength(0f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLength(10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLengthOne());
		Assert.AreEqual(OneTwoNegThree, OneTwoNegThree.WithLength(OneTwoNegThree.Length));

		// https://www.wolframalpha.com/input?i=normalize+%5B1%2C+2%2C+-3%5D
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f), OneTwoNegThree.WithLength(1f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f), OneTwoNegThree.WithLengthOne(), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * 2f, OneTwoNegThree.WithLength(2f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * -1f, OneTwoNegThree.WithLength(-1f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * -1f, OneTwoNegThree.WithLengthOne().Inverted, TestTolerance);
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
	public void ShouldCorrectlyShortenAndLengthen() {
		Assert.AreEqual(Vect.Zero, Vect.Zero.LengthenedBy(10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.LengthenedBy(0f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.ShortenedBy(10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.ShortenedBy(0f));

		AssertToleranceEquals(OneTwoNegThree.WithLength(10f), OneTwoNegThree.WithLength(7f).LengthenedBy(3f), TestTolerance);
		AssertToleranceEquals(OneTwoNegThree.WithLength(-10f), OneTwoNegThree.WithLength(7f).LengthenedBy(-17f), TestTolerance);
		AssertToleranceEquals(OneTwoNegThree.WithLength(-10f), OneTwoNegThree.WithLength(-7f).LengthenedBy(3f), TestTolerance);
		AssertToleranceEquals(OneTwoNegThree.WithLength(10f), OneTwoNegThree.WithLength(-7f).LengthenedBy(-17f), TestTolerance);
		AssertToleranceEquals(Vect.Zero, OneTwoNegThree.WithLength(7f).LengthenedBy(-7f), TestTolerance);
		AssertToleranceEquals(Vect.Zero, OneTwoNegThree.WithLength(-7f).LengthenedBy(-7f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		AssertToleranceEquals(OneTwoNegThree, Vect.Interpolate(OneTwoNegThree, Vect.Zero, 0f), TestTolerance);
		AssertToleranceEquals(Vect.Zero, Vect.Interpolate(OneTwoNegThree, Vect.Zero, 1f), TestTolerance);
		AssertToleranceEquals(Vect.FromVector3(OneTwoNegThree.ToVector3() * 0.5f), Vect.Interpolate(OneTwoNegThree, Vect.Zero, 0.5f), TestTolerance);
		AssertToleranceEquals(Vect.FromVector3(OneTwoNegThree.ToVector3() * 2f), Vect.Interpolate(OneTwoNegThree, Vect.Zero, -1f), TestTolerance);
		AssertToleranceEquals(Vect.FromVector3(OneTwoNegThree.ToVector3() * -1f), Vect.Interpolate(OneTwoNegThree, Vect.Zero, 2f), TestTolerance);

		var testList = new List<Vect>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}
		for (var i = 0; i < testList.Count; ++i) {
			for (var j = i; j < testList.Count; ++j) {
				var start = testList[i];
				var end = testList[j];

				for (var f = -1f; f <= 2f; f += 0.1f) {
					AssertToleranceEquals(new(Single.Lerp(start.X, end.X, f), Single.Lerp(start.Y, end.Y, f), Single.Lerp(start.Z, end.Z, f)), Vect.Interpolate(start, end, f), TestTolerance);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCreateNonBoundedRandomValues() {
		const int NumIterations = 50_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = Vect.CreateNewRandom();
			Assert.GreaterOrEqual(val.X, -Vect.DefaultRandomRange);
			Assert.LessOrEqual(val.X, Vect.DefaultRandomRange);
			Assert.GreaterOrEqual(val.Y, -Vect.DefaultRandomRange);
			Assert.LessOrEqual(val.Y, Vect.DefaultRandomRange);
			Assert.GreaterOrEqual(val.Z, -Vect.DefaultRandomRange);
			Assert.LessOrEqual(val.Z, Vect.DefaultRandomRange);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateBoundedRandomValues() {
		const int NumIterations = 50_000;

		for (var i = 0; i < NumIterations; ++i) {
			var a = Vect.CreateNewRandom();
			var b = a + new Vect(3f, 3f, 3f);
			var val = Vect.CreateNewRandom(a, b);
			Assert.GreaterOrEqual(val.X, a.X);
			Assert.LessOrEqual(val.X, b.X);
			Assert.GreaterOrEqual(val.Y, a.Y);
			Assert.LessOrEqual(val.Y, b.Y);
			Assert.GreaterOrEqual(val.Z, a.Z);
			Assert.LessOrEqual(val.Z, b.Z);
		}
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = new Vect(-3f, 1f, 3f);
		var max = new Vect(3f, -1f, -3f);

		AssertToleranceEquals(
			new Vect(0f, 0f, 0f),
			new Vect(0f, 0f, 0f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(-3f, 1f, 3f),
			new Vect(-3f, 1f, 3f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(3f, -1f, -3f),
			new Vect(3f, -1f, -3f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(-3f, 1f, 3f),
			new Vect(-4f, 2f, 4f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(3f, -1f, -3f),
			new Vect(4f, -2f, -4f).Clamp(min, max),
			TestTolerance
		);


		AssertToleranceEquals(
			new Vect(-0.158f, 0.0526f, 0.158f),
			new Vect(0f, 1f, 0f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0.158f, -0.0526f, -0.158f),
			new Vect(0f, -1f, 0f).Clamp(min, max),
			TestTolerance
		);
	}
}