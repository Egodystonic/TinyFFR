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
			var expected = new Direction(MathF.Sin(angle.Radians), 0f, MathF.Cos(angle.Radians));
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

		AssertToleranceEquals(FromVector3(Forward.ToVector3() + Left.ToVector3()), Rotation.FromAngleAroundAxis(-90f, Down).ScaledBy(0.5f) * Forward, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCombineRotations() {
		for (var f = 0f; f <= 360f; f += 18f) {
			var angle = Angle.FromDegrees(f);

			foreach (var cardinal in AllCardinals) {
				var expected = cardinal % angle;
				// TODO this fails when f = 360. Seems like the new check for 360 basically being 0 thwarts it. Need to think carefully
				AssertToleranceEquals(expected, cardinal % (angle * 0.5f) + cardinal % (angle * 0.5f), TestTolerance);
				AssertToleranceEquals(expected, cardinal % (angle * 0.25f) + cardinal % (angle * 0.25f) + cardinal % (angle * 0.25f) + cardinal % (angle * 0.25f), TestTolerance);
				AssertToleranceEquals(expected, cardinal % (angle * -0.5f) + cardinal % (angle * 1f) + cardinal % (angle * 0.5f), TestTolerance);
				AssertToleranceEquals(expected, cardinal % (angle * 1f) + cardinal % (angle * -0.5f) + cardinal % (angle * 0.5f), TestTolerance);
				AssertToleranceEquals(expected, cardinal % (angle * 1f) + cardinal % (angle * 0.5f) + cardinal % (angle * -0.5f), TestTolerance);
			}
		}

		Assert.IsTrue(NinetyAroundDown.EqualsForDirection(90f % Right + 90f % Forward, Forward, TestTolerance));
		Assert.IsTrue((45f % Up).FollowedBy(180f % Forward).EqualsForDirection(-45f % Up, Forward, TestTolerance));
	}

	[Test]
	public void ShouldCorrectlyCalculateDifferenceBetweenRotations() {
		void AssertPair(Angle a1, Direction d1, Angle a2, Direction d2, Rotation expectation) {
			AssertToleranceEquals(expectation, (a1 % d1).DifferenceTo(a2 % d2), TestTolerance);
			AssertToleranceEquals(-expectation, (a2 % d2).DifferenceTo(a1 % d1), TestTolerance);
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
			AssertToleranceEquals((Forward.ToVect() + Right.ToVect()).Direction, Forward * (NinetyAroundDown * (0.5f + f)), TestTolerance);
			AssertToleranceEquals(Right, Forward * (NinetyAroundDown * (1f + f)), TestTolerance);
			AssertToleranceEquals((Right.ToVect() + Backward.ToVect()).Direction, Forward * (NinetyAroundDown * (1.5f + f)), TestTolerance);
			AssertToleranceEquals(Backward, Forward * (NinetyAroundDown * (2f + f)), TestTolerance);
			AssertToleranceEquals((Backward.ToVect() + Left.ToVect()).Direction, Forward * (NinetyAroundDown * (2.5f + f)), TestTolerance);
			AssertToleranceEquals(Left, Forward * (NinetyAroundDown * (3f + f)), TestTolerance);
			AssertToleranceEquals((Left.ToVect() + Forward.ToVect()).Direction, Forward * (NinetyAroundDown * (3.5f + f)), TestTolerance);
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
	public void ShouldCorrectlyInterpolate() {
		// Some examples from external sources
		var a = Rotation.None;
		var b = Rotation.FromAngleAroundAxis(-Angle.HalfCircle, Up);
		var c = Rotation.FromAngleAroundAxis(Angle.FromRadians(-((3.1415f * 3f) / 2f)), Forward);

		AssertToleranceEquals(Rotation.FromQuaternion(new(0f, 0.58777f, 0f, 0.809028f)), Rotation.InterpolateSpherical(a, b, 0.4f), TestTolerance);
		AssertToleranceEquals(Rotation.FromQuaternion(new(0f, -0.233f, -0.688f, -0.688f)), Rotation.InterpolateSpherical(b, c, 0.85f), TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.InterpolateSpherical(c, a, 1f), TestTolerance);

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
							Rotation.InterpolateSpherical(start, end, f),
							Rotation.InterpolateLinear(start, end, f),
							0.01f
						);
					}
					catch (AssertionException) {
						Console.WriteLine(start + " -> " + end + " x " + f);
						Console.WriteLine("Distance " + distance);
						Console.WriteLine("\t" + Rotation.InterpolateSpherical(start, end, f) + " / " + Rotation.InterpolateSpherical(start, end, f).AsQuaternion);
						Console.WriteLine("\t" + Rotation.InterpolateLinear(start, end, f) + " / " + Rotation.InterpolateLinear(start, end, f).AsQuaternion);
						throw;
					}
				}
			}
		}
	}
}