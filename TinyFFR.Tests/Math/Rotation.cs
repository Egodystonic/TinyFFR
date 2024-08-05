// Created on 2023-10-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using static Egodystonic.TinyFFR.Direction;

namespace Egodystonic.TinyFFR;

[TestFixture]
class RotationTest {
	const float TestTolerance = 0.001f;
	static readonly Rotation NinetyAroundDown = 90f % Down;
	static readonly Rotation NinetyAroundUp = 90f % Up;
	static readonly Rotation NegativeNinetyAroundDown = -90f % Down;
	static readonly Rotation NegativeNinetyAroundUp = -90f % Up;

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<Rotation>();

	[Test]
	public void ShouldCorrectlyInitializeStaticMembers() {
		Assert.AreEqual(new Rotation(Quaternion.Identity), Rotation.None);
	}

	[Test]
	public void AxisAndAnglePropertiesAndWithMethodsShouldBeImplementedCorrectly() {
		foreach (var cardinal in AllCardinals) {
			for (var r = 0f; r < MathF.Tau * 0.95f; r += MathF.Tau * 0.1f) {
				var cosHalfAngle = MathF.Cos(r / 2f);
				var sinHalfAngle = MathF.Sin(r / 2f);
				var rot = new Rotation(new Quaternion(
					cardinal.X * sinHalfAngle,
					cardinal.Y * sinHalfAngle,
					cardinal.Z * sinHalfAngle,
					cosHalfAngle
				));

				Assert.AreEqual(r, rot.Angle.AsRadians, TestTolerance);
				var expectedAxis = r == 0f ? None : cardinal;
				AssertToleranceEquals(expectedAxis, rot.Axis, TestTolerance);

				Assert.AreEqual(Rotation.None, rot.WithAngle(Angle.Zero));
				Assert.AreEqual(Rotation.None, rot.WithAxis(None));

				if (r == 0f) {
					Assert.AreEqual(Rotation.None, rot.WithAxis(Left));
					Assert.AreEqual(Rotation.None, rot.WithAngle(270f));
					continue;
				}

				var anyPerp = cardinal.AnyOrthogonal();
				rot = rot.WithAxis(anyPerp);
				AssertToleranceEquals(anyPerp, rot.Axis, TestTolerance);
				Assert.AreEqual(r, rot.Angle.AsRadians, TestTolerance);
				rot = rot.WithAngle(Angle.FromRadians(r * 0.5f));
				AssertToleranceEquals(anyPerp, rot.Axis, TestTolerance);
				Assert.AreEqual(r * 0.5f, rot.Angle.AsRadians, TestTolerance);
			}
		}

		// Check that it's never possible to create a non-unit quaternion underlying a rotation using these properties
		// (assuming Direction is unit length or Zero-- if it's not that's a different invariant being violated that we should be stopping in the Direction type if we can)
		void AssertWithMethodsOnNoneRotQuatIsUnit(Angle angle, Direction axis) {
			Assert.AreEqual(1f, Rotation.None.WithAngle(angle).WithAxis(axis).AsQuaternion.Length(), TestTolerance);
			Assert.AreEqual(1f, Rotation.None.WithAxis(axis).WithAngle(angle).AsQuaternion.Length(), TestTolerance);
		}
		AssertWithMethodsOnNoneRotQuatIsUnit(-360f, None);
		AssertWithMethodsOnNoneRotQuatIsUnit(-180f, None);
		AssertWithMethodsOnNoneRotQuatIsUnit(-90f, None);
		AssertWithMethodsOnNoneRotQuatIsUnit(0f, None);
		AssertWithMethodsOnNoneRotQuatIsUnit(90f, None);
		AssertWithMethodsOnNoneRotQuatIsUnit(180f, None);
		AssertWithMethodsOnNoneRotQuatIsUnit(360f, None);
		foreach (var cardinal in AllCardinals) {
			AssertWithMethodsOnNoneRotQuatIsUnit(-360f, cardinal);
			AssertWithMethodsOnNoneRotQuatIsUnit(-180f, cardinal);
			AssertWithMethodsOnNoneRotQuatIsUnit(-90f, cardinal);
			AssertWithMethodsOnNoneRotQuatIsUnit(0f, cardinal);
			AssertWithMethodsOnNoneRotQuatIsUnit(90f, cardinal);
			AssertWithMethodsOnNoneRotQuatIsUnit(180f, cardinal);
			AssertWithMethodsOnNoneRotQuatIsUnit(360f, cardinal);
		}
	}

	[Test]
	public void ConstructorsShouldCorrectlyConstruct() { // Also, the floor should be made out of floor
		Assert.AreEqual(Rotation.None, new Rotation());

		foreach (var cardinal in AllCardinals) {
			for (var r = 0f; r < MathF.Tau * 0.95f; r += MathF.Tau * 0.1f) {
				Assert.AreEqual(new Rotation(Quaternion.CreateFromAxisAngle(cardinal.ToVector3(), r)), new Rotation(Angle.FromRadians(r), cardinal));
			}
		}
	}

	[Test]
	public void StaticFactoryMethodsShouldCorrectlyConstruct() {
		foreach (var cardinal in AllCardinals) {
			for (var r = 0f; r < MathF.Tau * 0.95f; r += MathF.Tau * 0.1f) {
				Assert.AreEqual(new Rotation(Angle.FromRadians(r), cardinal), new Rotation(Angle.FromRadians(r), cardinal));
			}
		}

		Assert.AreEqual(Rotation.None, new Rotation(0f, None));
		Assert.AreEqual(Rotation.None, new Rotation(0f, Up));
		Assert.AreEqual(Rotation.None, new Rotation(90f, None));

		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Forward, Right));
		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Right, Backward));
		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Backward, Left));
		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Left, Forward));

		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Right, Forward));
		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Backward, Right));
		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Left, Backward));
		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Forward, Left));

		foreach (var cardinal in AllCardinals) {
			Assert.AreEqual(Rotation.None, Rotation.FromStartAndEndDirection(cardinal, cardinal));
		}

		for (var i = 0; i < AllCardinals.Length; ++i) {
			for (var j = i; j < AllCardinals.Length; ++j) {
				var dirA = AllCardinals[i];
				var dirB = AllCardinals[j];

				AssertToleranceEquals(dirB, Rotation.FromStartAndEndDirection(dirA, dirB) * dirA, TestTolerance);
				AssertToleranceEquals(dirA, Rotation.FromStartAndEndDirection(dirB, dirA) * dirB, TestTolerance);

				if ((dirA ^ dirB) == 180f) continue;

				Assert.AreEqual(Rotation.FromStartAndEndDirection(dirA, dirB), Rotation.FromStartAndEndDirection(-dirA, -dirB));
				Assert.AreEqual(-Rotation.FromStartAndEndDirection(dirA, dirB), Rotation.FromStartAndEndDirection(dirB, dirA));
				Assert.AreEqual(Rotation.FromStartAndEndDirection(dirA, dirB), -Rotation.FromStartAndEndDirection(dirB, dirA));
			}
		}

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					for (var w = -5f; w <= 5f; w += 1f) {
						var q = new Quaternion(x, y, z, w);
						Assert.AreEqual(q, Rotation.FromQuaternionPreNormalized(q).AsQuaternion);
						Assert.AreEqual(MathUtils.NormalizeOrIdentity(q), Rotation.FromQuaternion(q).AsQuaternion);
					}
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyDeconstruct() {
		foreach (var cardinal in AllCardinals) {
			for (var r = 0f; r < MathF.Tau * 0.95f; r += MathF.Tau * 0.1f) {
				var cosHalfAngle = MathF.Cos(r / 2f);
				var sinHalfAngle = MathF.Sin(r / 2f);
				var rot = new Rotation(new Quaternion(
					cardinal.X * sinHalfAngle,
					cardinal.Y * sinHalfAngle,
					cardinal.Z * sinHalfAngle,
					cosHalfAngle
				));

				var (angle, axis) = rot;

				Assert.AreEqual(r, angle.AsRadians, TestTolerance);
				var expectedAxis = r == 0f ? None : cardinal;
				AssertToleranceEquals(expectedAxis, axis, TestTolerance);
			}
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<Rotation>();
		foreach (var cardinal in AllCardinals) {
			for (var angle = -360f; angle <= 360f; angle += 36f) {
				var expected = new Rotation(angle, cardinal);
				ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(expected);
				ByteSpanSerializationTestUtils.AssertLittleEndianSingles(expected, expected.AsQuaternion.X, expected.AsQuaternion.Y, expected.AsQuaternion.Z, expected.AsQuaternion.W);
			}
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		var testCulture = CultureInfo.InvariantCulture;
		var testFormat = "N0";
		Span<char> formatSpan = stackalloc char[300];

		foreach (var cardinal in AllCardinals) {
			for (var angle = 10f; angle <= 170f; angle += 10f) {
				var expectedValue = $"{new Angle(angle).ToString(testFormat, testCulture)}{Rotation.ToStringMiddleSection}{cardinal.ToString(testFormat, testCulture)}";
				var testRot = new Rotation(angle, cardinal);

				Assert.IsTrue(testRot.TryFormat(formatSpan[..expectedValue.Length], out var charsWritten, testFormat, testCulture));
				Assert.AreEqual(expectedValue.Length, charsWritten);

				Assert.AreEqual(expectedValue, testRot.ToString(testFormat, testCulture));
				Assert.AreEqual(expectedValue, new String(formatSpan[..expectedValue.Length]));
			}
		}
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		int GetExpectedStrLen(Rotation input, string format, IFormatProvider? provider) {
			var dest = new char[200];
			input.Angle.TryFormat(dest, out var angleCharsWritten, format, provider);
			input.Axis.TryFormat(dest, out var dirCharsWritten, format, provider);
			return angleCharsWritten + Rotation.ToStringMiddleSection.Length + dirCharsWritten;
		}

		void AssertFail(Rotation input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			Rotation input,
			Span<char> destination,
			ReadOnlySpan<char> format,
			IFormatProvider? provider,
			ReadOnlySpan<char> expectedDestSpanValue
		) {
			var actualReturnValue = input.TryFormat(destination, out var numCharsWritten, format, provider);
			Assert.AreEqual(true, actualReturnValue);
			Assert.AreEqual(expectedDestSpanValue.Length, numCharsWritten);
			Assert.IsTrue(
				expectedDestSpanValue.SequenceEqual(destination[..expectedDestSpanValue.Length]),
				$"Destination as string was {new String(destination)}"
			);
		}

		var testRot = 12.345f % new Direction(1f, 1f, 1f);

		AssertFail(Rotation.None, Array.Empty<char>(), "", null);
		AssertFail(Rotation.None, new char[GetExpectedStrLen(Rotation.None, "", null) - 1], "", null);
		AssertSuccess(Rotation.None, new char[GetExpectedStrLen(Rotation.None, "N0", null)], "N0", null, "0" + Angle.ToStringSuffix + Rotation.ToStringMiddleSection + "<0, 0, 0>");
		AssertFail(testRot, new char[GetExpectedStrLen(testRot, "N0", null) - 1], "N0", null);
		AssertSuccess(testRot, new char[GetExpectedStrLen(testRot, "N0", null)], "N0", null, "12" + Angle.ToStringSuffix + Rotation.ToStringMiddleSection + "<1, 1, 1>");
		AssertSuccess(testRot, new char[GetExpectedStrLen(testRot, "N3", null)], "N3", null, "12.345" + Angle.ToStringSuffix + Rotation.ToStringMiddleSection + "<0.577, 0.577, 0.577>");
	}

	[Test]
	public void ShouldCorrectlyParseFromString() {
		var testCulture = CultureInfo.InvariantCulture;

		void AssertSuccess(string input, Rotation expectedResult) {
			AssertToleranceEquals(expectedResult, Rotation.Parse(input, testCulture), TestTolerance);
			AssertToleranceEquals(expectedResult, Rotation.Parse(input.AsSpan(), testCulture), TestTolerance);
			Assert.IsTrue(Rotation.TryParse(input, testCulture, out var parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
			Assert.IsTrue(Rotation.TryParse(input.AsSpan(), testCulture, out parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
		}

		void AssertFailure(string input) {
			Assert.Catch(() => Rotation.Parse(input, testCulture));
			Assert.Catch(() => Rotation.Parse(input.AsSpan(), testCulture));
			Assert.False(Rotation.TryParse(input, testCulture, out _));
			Assert.False(Rotation.TryParse(input.AsSpan(), testCulture, out _));
		}

		AssertSuccess("90 around <0, -1, 0>", NinetyAroundDown);
		AssertSuccess("90.000 around <0.000, -1.000, 0.000>", NinetyAroundDown);
		AssertSuccess("450" + Angle.ToStringSuffix + " around <0, -2, 0>", 450f % Down);
		AssertSuccess("90.000" + Angle.ToStringSuffix + " around <0.000, -3.000, 0.000>", NinetyAroundDown);
		AssertSuccess("-90 around <0, 1, 0>", NegativeNinetyAroundUp);
		AssertSuccess("-90.000 around <0.000, 1.000, 0.000>", NegativeNinetyAroundUp);
		AssertSuccess("-450" + Angle.ToStringSuffix + " around <0, 2, 0>", -450f % Up);
		AssertSuccess("-90.000" + Angle.ToStringSuffix + " around <0.000, 3.000, 0.000>", NegativeNinetyAroundUp);
		AssertSuccess("123.456 around <7.89, -10.111, 123.45>", new Rotation(123.456f, new(7.89f, -10.111f, 123.45f)));
		AssertSuccess("100 around <0, 0, 0>", Rotation.None);
		AssertSuccess("0 around <0, 1, 0>", Rotation.None);
		AssertSuccess("0" + Angle.ToStringSuffix + " around <0, 0, 0>", Rotation.None);

		AssertFailure("");
		AssertFailure("abc");
		AssertFailure(Angle.ToStringSuffix);
		AssertFailure(Angle.ToStringSuffix + " around <0, 1, 0>");
		AssertFailure("90 around <0, 1, 0");
		AssertFailure("90 around <0, 1>");
		AssertFailure("90 around <0, 1, >");
		AssertFailure(" around <0, 1, 1>");
		AssertFailure("abc around <0, 1, 1>");
		AssertFailure("90 round <0, 1, 1>");
	}

	[Test]
	public void ShouldCorrectlyImplementEquality() {
		Assert.AreEqual(Rotation.None, -Rotation.None);
		Assert.AreNotEqual(Rotation.None, NinetyAroundDown);
		Assert.IsTrue(NinetyAroundDown.Equals(NinetyAroundDown));
		Assert.IsFalse(NinetyAroundDown.Equals(NinetyAroundUp));
		Assert.IsTrue(NinetyAroundDown == (90f % Down));
		Assert.IsFalse(NinetyAroundDown == NinetyAroundUp);
		Assert.IsFalse(NegativeNinetyAroundUp != -90f % Up);
		Assert.IsTrue(NinetyAroundDown != NinetyAroundUp);

		Assert.IsTrue(Rotation.None.Equals(Rotation.None, 0f));
		Assert.IsTrue(NinetyAroundUp.Equals(NinetyAroundUp, 0f));
		Assert.IsTrue(Rotation.FromQuaternionPreNormalized(new(0.1f, 0.2f, 0.3f, 0.4f)).Equals(Rotation.FromQuaternionPreNormalized(new(0.2f, 0.1f, 0.4f, 0.5f)), 0.11f));
		Assert.IsFalse(Rotation.FromQuaternionPreNormalized(new(0.1f, 0.2f, 0.3f, 0.4f)).Equals(Rotation.FromQuaternionPreNormalized(new(0.2f, 0.1f, 0.4f, 0.5f)), 0.09f));

		Assert.IsTrue((90 % Down).EqualsForDirection(-45f % Down, Up));
		Assert.IsFalse((90 % Down).EqualsForDirection(-45f % Down, Right));
		Assert.IsFalse((89 % Down).EqualsForDirection(91f % Down, Right));
		Assert.IsTrue((89 % Down).EqualsForDirection(91f % Down, Right, 0.3f));

		Assert.AreEqual(Rotation.FromQuaternion(new(1f, 1f, 1f, 1f)), Rotation.FromQuaternion(new(-1f, -1f, -1f, -1f)));
		AssertToleranceEquals(Rotation.FromQuaternion(new(1f, 1f, 1f, 0.999f)), Rotation.FromQuaternion(new(-0.999f, -0.999f, -0.999f, -1f)), 0.001f);
	}

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

		Assert.AreEqual(NinetyAroundDown, NinetyAroundDown.Clamp(Rotation.None, NinetyAroundUp));
		Assert.AreEqual(NinetyAroundDown, NinetyAroundDown.Clamp(NinetyAroundUp, Rotation.None));
	}
}