// Created on 2023-10-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Numerics;
using static Egodystonic.TinyFFR.Direction;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class RotationTest {
	[Test]
	public void ShouldCorrectlyReverseRotations() {
		Assert.AreEqual(-90f % Up, -(90f % Up));
		Assert.AreEqual(20f % Down, -(-20f % Down));

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					for (var w = -5f; w <= 5f; w += 1f) {
						var rot = Rotation.FromQuaternion(new Quaternion(x, y, z, w));

						foreach (var cardinal in AllCardinals) {
							AssertToleranceEquals(cardinal, cardinal * rot * -rot, TestTolerance);
							AssertToleranceEquals(cardinal, cardinal * -rot * rot, TestTolerance);
							AssertToleranceEquals(cardinal * rot, cardinal * -(-rot), TestTolerance);
						}
					}
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyRotateDirectionsAndVects() {
		for (var f = 0f; f <= 360f; f += 18f) {
			var angle = Angle.FromDegrees(f);
			var expected = new Direction(MathF.Sin(angle.AsRadians), 0f, MathF.Cos(angle.AsRadians));
			AssertToleranceEquals(expected, angle % Up * Forward, TestTolerance);
		}

		Assert.AreEqual(Up, Up * Rotation.None);
		Assert.AreEqual(new Direction(14f, -15f, -0.2f), Rotation.None.RotateWithoutRenormalizing(new Direction(14f, -15f, -0.2f)));

		// https://www.wolframalpha.com/input?i=rotate+%280.801784%2C+-0.534522%2C+0.267261%29+around+axis+%280.840799%2C+0.0300285%2C+-0.540514%29+by+171+degrees
		AssertToleranceEquals(
			new Direction(0.023f, 0.456f, -0.890f),
			new Direction(0.841f, 0.030f, -0.541f) % 171f * new Direction(0.802f, -0.535f, 0.267f),
			TestTolerance
		);

		// https://www.wolframalpha.com/input?i=rotate+%280.742%2C+-0.314%2C+0.589%29+around+axis+%28-0.678%2C+0.124%2C+-0.724%29+by+-3.1+degrees
		AssertToleranceEquals(
			new Direction(0.750f, -0.306f, 0.583f),
			new Direction(-0.678f, 0.124f, -0.724f) % -3.1f * new Direction(0.742f, -0.314f, 0.589f),
			TestTolerance
		);

		// https://www.wolframalpha.com/input?i=rotate+%285.2%2C+1.3%2C+-19%29+around+axis+%28-0.813%2C+-0.273%2C+-0.515%29+by+69+degrees
		AssertToleranceEquals(
			new Vect(4.617f, -17.360f, -8.188f),
			new Direction(-0.813f, -0.273f, -0.515f) % 69f * new Vect(5.2f, 1.3f, -19f),
			TestTolerance
		);

		AssertToleranceEquals(FromVector3(Forward.ToVector3() + Left.ToVector3()), new Rotation(-90f, Down).ScaledBy(0.5f) * Forward, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCombineRotations() {
		for (var f = 0f; f <= 360f; f += 18f) {
			var angle = Angle.FromDegrees(f);

			foreach (var cardinal in AllCardinals) {
				var expected = cardinal % angle;
				AssertToleranceEquals(expected, cardinal % (angle * 0.5f) + cardinal % (angle * 0.5f), TestTolerance);
				AssertToleranceEquals(expected, cardinal % (angle * 0.25f) + cardinal % (angle * 0.25f) + cardinal % (angle * 0.25f) + cardinal % (angle * 0.25f), TestTolerance);
				AssertToleranceEquals(expected, cardinal % (angle * -0.5f) + cardinal % (angle * 1f) + cardinal % (angle * 0.5f), TestTolerance);
				AssertToleranceEquals(expected, cardinal % (angle * 1f) + cardinal % (angle * -0.5f) + cardinal % (angle * 0.5f), TestTolerance);
				AssertToleranceEquals(expected, cardinal % (angle * 1f) + cardinal % (angle * 0.5f) + cardinal % (angle * -0.5f), TestTolerance);
			}
		}

		Assert.IsTrue(NinetyAroundDown.EqualsForDirection(90f % Right + 90f % Forward, Forward, TestTolerance));
		Assert.IsTrue((45f % Up).Plus(180f % Forward).EqualsForDirection(-45f % Up, Forward, TestTolerance));
	}

	[Test]
	public void ShouldCorrectlyCalculateDifferenceBetweenRotations() {
		void AssertPair(Angle a1, Direction d1, Angle a2, Direction d2, Rotation expectation) {
			AssertToleranceEquals(expectation, (a1 % d1).Minus(a2 % d2), TestTolerance);
			AssertToleranceEquals(-expectation, (a2 % d2).Minus(a1 % d1), TestTolerance);
		}

		AssertPair(90f, Up, 70f, Up, -20f % Up);
		AssertPair(0f, Right, 0f, Down, Rotation.None);
		AssertPair(0f, Right, 0f, Left, Rotation.None);
		AssertPair(180f, Right, 180f, Left, 360f % Up);
		AssertPair(180f, Right, 180f, Right, Rotation.None);
		AssertPair(360f, Right, 360f, Left, Rotation.None);
		AssertPair(90f, Up, 90f, Right, 120f % FromVector3(Down.ToVector3() + Right.ToVector3() + Backward.ToVector3()));
		AssertPair(180f, Up, 180f, Right, 180f % Backward);
	}

	[Test]
	public void ShouldCorrectlyCalculateAngleBetweenRotations() {
		void AssertPair(Angle a1, Direction d1, Angle a2, Direction d2, Angle expectation) {
			AssertToleranceEquals(expectation, (a1 % d1).AngleTo(a2 % d2), TestTolerance);
			AssertToleranceEquals(expectation, (a2 % d2).AngleTo(a1 % d1), TestTolerance);
		}

		AssertPair(90f, Up, 70f, Up, 20f);
		AssertPair(0f, Right, 0f, Down, 0f);
		AssertPair(0f, Right, 0f, Left, 0f);
		AssertPair(180f, Right, 180f, Left, 360f);
		AssertPair(180f, Right, 180f, Right, 0f);
		AssertPair(360f, Right, 360f, Left, 0f);
		AssertPair(90f, Up, 90f, Right, 120f);
		AssertPair(180f, Up, 180f, Right, 180f);
	}

	[Test]
	public void ShouldCorrectlyScaleRotations() {
		AssertToleranceEquals(NegativeNinetyAroundUp, NinetyAroundUp * -1f, TestTolerance);
		AssertToleranceEquals(NinetyAroundUp, NegativeNinetyAroundUp * -1f, TestTolerance);
		AssertToleranceEquals(NegativeNinetyAroundDown, NinetyAroundDown * -1f, TestTolerance);
		AssertToleranceEquals(NinetyAroundDown, NegativeNinetyAroundDown * -1f, TestTolerance);

		AssertToleranceEquals(Rotation.None, NinetyAroundUp * 0f, TestTolerance);
		AssertToleranceEquals(Rotation.None, NegativeNinetyAroundUp * 0f, TestTolerance);
		AssertToleranceEquals(Rotation.None, NinetyAroundDown * 0f, TestTolerance);
		AssertToleranceEquals(Rotation.None, NegativeNinetyAroundDown * 0f, TestTolerance);

		AssertToleranceEquals(Rotation.None, Rotation.None * 0.5f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 1f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 0f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * -0.5f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * -1f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 100f, TestTolerance);

		AssertToleranceEquals(180f % Up, NinetyAroundUp * 2f, TestTolerance);
		AssertToleranceEquals(180f % Up, NinetyAroundDown * -2f, TestTolerance);
		AssertToleranceEquals(180f % Down, NegativeNinetyAroundUp * 2f, TestTolerance);
		AssertToleranceEquals(180f % Down, NegativeNinetyAroundDown * -2f, TestTolerance);

		for (var f = -12f; f <= 12f; f += 4f) {
			AssertToleranceEquals((Forward.AsVect() + Right.AsVect()).Direction, Forward * (NinetyAroundDown * (0.5f + f)), TestTolerance);
			AssertToleranceEquals(Right, Forward * (NinetyAroundDown * (1f + f)), TestTolerance);
			AssertToleranceEquals((Right.AsVect() + Backward.AsVect()).Direction, Forward * (NinetyAroundDown * (1.5f + f)), TestTolerance);
			AssertToleranceEquals(Backward, Forward * (NinetyAroundDown * (2f + f)), TestTolerance);
			AssertToleranceEquals((Backward.AsVect() + Left.AsVect()).Direction, Forward * (NinetyAroundDown * (2.5f + f)), TestTolerance);
			AssertToleranceEquals(Left, Forward * (NinetyAroundDown * (3f + f)), TestTolerance);
			AssertToleranceEquals((Left.AsVect() + Forward.AsVect()).Direction, Forward * (NinetyAroundDown * (3.5f + f)), TestTolerance);
			AssertToleranceEquals(Forward, Forward * (NinetyAroundDown * (4f + f)), TestTolerance);
		}

		Assert.AreEqual(Rotation.None, default(Rotation) * 0f);
		Assert.AreEqual(Rotation.None, default(Rotation) * -2f);
		Assert.AreEqual(Rotation.None, default(Rotation) * -1f);
		Assert.AreEqual(Rotation.None, default(Rotation) * -0.5f);
		Assert.AreEqual(Rotation.None, default(Rotation) * 0.5f);
		Assert.AreEqual(Rotation.None, default(Rotation) * 1f);
		Assert.AreEqual(Rotation.None, default(Rotation) * 2f);

		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * 0f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * -2f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * -1f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * -0.5f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * 0.5f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * 1f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * 2f);
	}

	[Test]
	public void RotationScalingShouldUseAppropriateErrorMargin() {
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0.001f, 0f, 0f, 1f)) * 1f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(-0.001f, 0f, 0f, 1f)) * 1f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, 0.999f)) * 1f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, 1.001f)) * 1f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0.001f, 0f, 0f, -1f)) * 1f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(-0.001f, 0f, 0f, -1f)) * 1f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, -0.999f)) * 1f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, -1.001f)) * 1f);

		Assert.AreNotEqual(Rotation.None, (Left >> Right) * 0.0001f);
		Assert.AreEqual(Rotation.None, (Left >> Right) * 0.00001f);
		Assert.AreNotEqual(Rotation.None, (Left >> Right) * -0.0001f);
		Assert.AreEqual(Rotation.None, (Left >> Right) * -0.00001f);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		// Some examples from external sources
		var a = Rotation.None;
		var b = new Rotation(-Angle.HalfCircle, Up);
		var c = new Rotation(Angle.FromRadians(-((3.1415f * 3f) / 2f)), Forward);

		AssertToleranceEquals(Rotation.FromQuaternion(new(0f, 0.58777f, 0f, 0.809028f)), Rotation.AccuratelyInterpolate(a, b, 0.4f), TestTolerance);
		AssertToleranceEquals(Rotation.FromQuaternion(new(0f, -0.233f, -0.688f, -0.688f)), Rotation.AccuratelyInterpolate(b, c, 0.85f), TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.AccuratelyInterpolate(c, a, 1f), TestTolerance);

		// Testing similarity of linear/spherical
		var testList = new List<Rotation>();
		for (var x = -3f; x <= 3f; x += 1f) {
			for (var y = -3f; y <= 3f; y += 1f) {
				for (var z = -3f; z <= 3f; z += 1f) {
					for (var w = -3f; w <= 3f; w += 1f) {
						testList.Add(Rotation.FromQuaternion(new Quaternion(x, y, z, w)));
					}
				}
			}
		}
		
		for (var i = 0; i < testList.Count; ++i) {
			for (var j = i; j < testList.Count; ++j) {
				var start = testList[i];
				var end = testList[j];

				var distance = start.AngleTo(end);
				if (distance > Angle.QuarterCircle) continue; // Don't try this with rotations too far apart
				for (var f = -0.05f; f <= 1.05f; f += 0.05f) {
					try {
						AssertToleranceEquals(
							Rotation.AccuratelyInterpolate(start, end, f),
							Rotation.ApproximatelyInterpolate(start, end, f),
							0.01f
						);
					}
					catch (AssertionException) {
						Console.WriteLine(start + " -> " + end + " x " + f);
						Console.WriteLine("Distance " + distance);
						Console.WriteLine("\t" + Rotation.AccuratelyInterpolate(start, end, f) + " / " + Rotation.AccuratelyInterpolate(start, end, f).AsQuaternion);
						Console.WriteLine("\t" + Rotation.ApproximatelyInterpolate(start, end, f) + " / " + Rotation.ApproximatelyInterpolate(start, end, f).AsQuaternion);
						throw;
					}
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		// Same axis, clamp angle
		AssertToleranceEquals(
			new Rotation(30f, Up),
			new Rotation(60f, Up).Clamp(new Rotation(10f, Up), new Rotation(30f, Up)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Rotation(30f, Up),
			new Rotation(10f, Up).Clamp(new Rotation(30f, Up), new Rotation(60f, Up)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Rotation(30f, Up),
			new Rotation(30f, Up).Clamp(new Rotation(10f, Up), new Rotation(60f, Up)),
			TestTolerance
		);

		// Inverted axis, reversed angle
		AssertToleranceEquals(
			new Rotation(30f, Up),
			new Rotation(60f, Up).Clamp(new Rotation(-10f, Down), new Rotation(-30f, Down)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Rotation(30f, Up),
			new Rotation(10f, Up).Clamp(new Rotation(-30f, Down), new Rotation(-60f, Down)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Rotation(30f, Up),
			new Rotation(30f, Up).Clamp(new Rotation(-10f, Down), new Rotation(-60f, Down)),
			TestTolerance
		);

		// Orthogonal axis
		AssertToleranceEquals(
			new Rotation(20f, Right),
			new Rotation(20f, Up).Clamp(new Rotation(10f, Right), new Rotation(30f, Right)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Rotation(10f, Right),
			new Rotation(5f, Down).Clamp(new Rotation(10f, Right), new Rotation(30f, Right)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Rotation(30f, Right),
			new Rotation(40f, Down).Clamp(new Rotation(10f, Right), new Rotation(30f, Right)),
			TestTolerance
		);

		// All over the place
		AssertToleranceEquals(
			new Rotation(20f, Up),
			new Rotation(20f, Up).Clamp(new Rotation(10f, (1f, 1f, 0f)), new Rotation(30f, (-1f, 1f, 0f))),
			TestTolerance
		);
		AssertToleranceEquals(
			new Rotation(30f, (1f, 1f, 0f)),
			new Rotation(40f, Left).Clamp(new Rotation(10f, (1f, 1f, 0f)), new Rotation(30f, (-1f, 1f, 0f))),
			TestTolerance
		);
		AssertToleranceEquals(
			new Rotation(10f, (-1f, 1f, 0f)),
			new Rotation(5f, Right).Clamp(new Rotation(10f, (1f, 1f, 0f)), new Rotation(30f, (-1f, 1f, 0f))),
			TestTolerance
		);

		// None
		var testList = new List<Rotation>();
		for (var x = -2f; x <= 2f; x += 1f) {
			for (var y = -2f; y <= 2f; y += 1f) {
				for (var z = -2f; z <= 2f; z += 1f) {
					for (var w = -2f; w <= 2f; w += 1f) {
						testList.Add(Rotation.FromQuaternion(new Quaternion(x, y, z, w)));
					}
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var min = testList[i];
			if (min == Rotation.None) continue;
			for (var j = i; j < testList.Count; ++j) {
				var max = testList[j];
				if (max == Rotation.None) continue;

				Assert.AreEqual(Rotation.None, Rotation.None.Clamp(min, max));
			}
		}

		Assert.Throws<ArgumentException>(() => NinetyAroundDown.Clamp(Rotation.None, NinetyAroundUp));
		Assert.Throws<ArgumentException>(() => NinetyAroundDown.Clamp(NinetyAroundUp, Rotation.None));
	}
}