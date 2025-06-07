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

	void AssertEquivalence(Rotation expected, Rotation actual, float tolerance) {
		if (!expected.IsEquivalentForAllDirectionsTo(actual, tolerance)) {
			Assert.Fail($"{expected} was not equivalent to {actual} for all directions (within tolerance of {tolerance}).");
		}
	}

	void AssertEquivalence(Rotation expected, Rotation actual, Direction targetDir, float tolerance) {
		if (!expected.IsEquivalentForSingleDirectionTo(actual, targetDir, tolerance)) {
			Assert.Fail($"{expected} was not equivalent to {actual} for {targetDir} (within tolerance of {tolerance}).");
		}
	}

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<Rotation>(16);

	[Test]
	public void ShouldCorrectlyInitializeStaticMembers() {
		Assert.AreEqual(Rotation.FromQuaternionPreNormalized(Quaternion.Identity), Rotation.None);
		Assert.AreEqual(new Rotation(), Rotation.None);
	}

	[Test]
	public void AxisAndAnglePropertiesAndWithMethodsShouldBeImplementedCorrectly() {
		foreach (var cardinal in AllCardinals) {
			for (var r = 0f; r < MathF.Tau * 0.95f; r += MathF.Tau * 0.1f) {
				var cosHalfAngle = MathF.Cos(r / 2f);
				var sinHalfAngle = MathF.Sin(r / 2f);
				var rot = Rotation.FromQuaternionPreNormalized(new Quaternion(
					cardinal.X * sinHalfAngle,
					cardinal.Y * sinHalfAngle,
					cardinal.Z * sinHalfAngle,
					cosHalfAngle
				));

				Assert.AreEqual(r, rot.Angle.Radians, TestTolerance);
				var expectedAxis = r == 0f ? None : cardinal;
				AssertToleranceEquals(expectedAxis, rot.Axis, TestTolerance);

				AssertEquivalence(Rotation.None, rot with { Angle = Angle.Zero }, 0f);
				AssertEquivalence(Rotation.None, rot with { Axis = None }, 0f);

				if (r == 0f) {
					AssertEquivalence(Rotation.None, rot with { Axis = Left }, 0f);
					AssertEquivalence(Rotation.None, rot with { Angle = 270f }, 0f);
					continue;
				}

				var anyPerp = cardinal.AnyOrthogonal();
				rot = rot with { Axis = anyPerp };
				AssertToleranceEquals(anyPerp, rot.Axis, TestTolerance);
				Assert.AreEqual(r, rot.Angle.Radians, TestTolerance);
				rot = rot with { Angle = Angle.FromRadians(r * 0.5f) };
				AssertToleranceEquals(anyPerp, rot.Axis, TestTolerance);
				Assert.AreEqual(r * 0.5f, rot.Angle.Radians, TestTolerance);
			}
		}

		// Check that it's never possible to create a non-unit quaternion underlying a rotation using these properties
		// (assuming Direction is unit length or Zero-- if it's not that's a different invariant being violated that we should be stopping in the Direction type if we can)
		void AssertWithMethodsOnNoneRotQuatIsUnit(Angle angle, Direction axis) {
			Assert.AreEqual(1f, (Rotation.None with { Angle = angle, Axis = axis }).ToQuaternion().Length(), TestTolerance);
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
	public void ShouldCorrectlyIncreaseOrDecreaseAngle() {
		AssertToleranceEquals(new Rotation(110f, Down), NinetyAroundDown.WithAngleIncreasedBy(20f), TestTolerance);
		AssertToleranceEquals(new Rotation(70f, Down), NinetyAroundDown.WithAngleDecreasedBy(20f), TestTolerance);
		AssertToleranceEquals(new Rotation(-70f, Up), NegativeNinetyAroundUp.WithAngleIncreasedBy(20f), TestTolerance);
		AssertToleranceEquals(new Rotation(-110f, Up), NegativeNinetyAroundUp.WithAngleDecreasedBy(20f), TestTolerance);

		AssertToleranceEquals(new Rotation(20f, None), new Rotation().WithAngleIncreasedBy(20f), TestTolerance);
		AssertToleranceEquals(new Rotation(-20f, None), new Rotation().WithAngleIncreasedBy(-20f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyRotateAxis() {
		AssertToleranceEquals(
			new Rotation(90f, Left), 
			NinetyAroundDown.WithAxisRotatedBy(Forward % 90f),
			TestTolerance
		);

		AssertToleranceEquals(
			new Rotation(90f, Right),
			NinetyAroundDown.WithAxisRotatedBy(Forward % -90f),
			TestTolerance
		);

		AssertToleranceEquals(
			new Rotation(-90f, Right),
			NegativeNinetyAroundUp.WithAxisRotatedBy(Forward % 90f),
			TestTolerance
		);

		AssertToleranceEquals(
			new Rotation(-90f, Left),
			NegativeNinetyAroundUp.WithAxisRotatedBy(Forward % -90f),
			TestTolerance
		);

		Assert.AreEqual(Rotation.None, Rotation.None.WithAxisRotatedBy(90f % Forward));
		Assert.AreEqual(Rotation.None, Rotation.None.WithAxisRotatedBy(Rotation.None));
		AssertToleranceEquals(NinetyAroundUp, NinetyAroundUp.WithAxisRotatedBy(360f % Forward), TestTolerance);
		AssertToleranceEquals(NinetyAroundUp, NinetyAroundUp.WithAxisRotatedBy(-360f % Forward), TestTolerance);
		AssertToleranceEquals(NinetyAroundUp, NinetyAroundUp.WithAxisRotatedBy(0f % Forward), TestTolerance);
		AssertToleranceEquals(NinetyAroundUp, NinetyAroundUp.WithAxisRotatedBy(Rotation.None), TestTolerance);
		AssertToleranceEquals(NinetyAroundUp, NinetyAroundUp.WithAxisRotatedBy(90f % None), TestTolerance);
	}

	[Test]
	public void ConstructorsShouldCorrectlyConstruct() { // Also, the floor should be made out of floor
		Assert.AreEqual(Rotation.None, new Rotation());

		foreach (var cardinal in AllCardinals) {
			for (var r = 0f; r < MathF.Tau * 0.95f; r += MathF.Tau * 0.1f) {
				var fromQuat = Rotation.FromQuaternionPreNormalized(Quaternion.CreateFromAxisAngle(cardinal.ToVector3(), r));
				var fromCtor = new Rotation(Angle.FromRadians(r), cardinal);
				AssertToleranceEquals(Angle.FromRadians(r), fromCtor.Angle, TestTolerance);
				AssertToleranceEquals(cardinal, fromCtor.Axis, TestTolerance);
				AssertEquivalence(fromQuat, fromCtor, TestTolerance);
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
		// Assert.AreEqual(Rotation.None, new Rotation(0f, Up));
		// Assert.AreEqual(Rotation.None, new Rotation(90f, None));

		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Forward, Right));
		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Right, Backward));
		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Backward, Left));
		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Left, Forward));

		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Right, Forward));
		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Backward, Right));
		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Left, Backward));
		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Forward, Left));

		foreach (var cardinal in AllCardinals) {
			AssertEquivalence(Rotation.None, Rotation.FromStartAndEndDirection(cardinal, cardinal), 0f);
		}

		for (var i = 0; i < AllCardinals.Length; ++i) {
			for (var j = i; j < AllCardinals.Length; ++j) {
				var dirA = AllCardinals[i];
				var dirB = AllCardinals[j];

				AssertToleranceEquals(dirB, Rotation.FromStartAndEndDirection(dirA, dirB) * dirA, TestTolerance);
				AssertToleranceEquals(dirA, Rotation.FromStartAndEndDirection(dirB, dirA) * dirB, TestTolerance);

				if ((dirA ^ dirB) == 180f) continue;

				AssertEquivalence(Rotation.FromStartAndEndDirection(dirA, dirB), Rotation.FromStartAndEndDirection(-dirA, -dirB), 0f);
				AssertEquivalence(-Rotation.FromStartAndEndDirection(dirA, dirB), Rotation.FromStartAndEndDirection(dirB, dirA), 0f);
				AssertEquivalence(Rotation.FromStartAndEndDirection(dirA, dirB), -Rotation.FromStartAndEndDirection(dirB, dirA), 0f);
			}
		}

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					for (var w = -5f; w <= 5f; w += 1f) {
						var q = new Quaternion(x, y, z, w);
						var qNorm = MathUtils.NormalizeOrIdentity(q);
						var r = Rotation.FromQuaternion(q);
						var rQ = r.ToQuaternion();
						Assert.IsTrue(
								(MathF.Abs(qNorm.X - rQ.X) <= TestTolerance 
								&& MathF.Abs(qNorm.Y - rQ.Y) <= TestTolerance 
								&& MathF.Abs(qNorm.Z - rQ.Z) <= TestTolerance
								&& MathF.Abs(qNorm.W - rQ.W) <= TestTolerance)
							|| (MathF.Abs(-qNorm.X - rQ.X) <= TestTolerance
								&& MathF.Abs(-qNorm.Y - rQ.Y) <= TestTolerance
								&& MathF.Abs(-qNorm.Z - rQ.Z) <= TestTolerance
								&& MathF.Abs(-qNorm.W - rQ.W) <= TestTolerance)
						);

						Assert.IsFalse(Single.IsNaN(r.Angle.Radians));
						Assert.IsFalse(Single.IsNaN(r.Axis.X));
						Assert.IsFalse(Single.IsNaN(r.Axis.Y));
						Assert.IsFalse(Single.IsNaN(r.Axis.Z));
						Assert.IsTrue(Single.IsFinite(r.Angle.Radians));
						Assert.IsTrue(Single.IsFinite(r.Axis.X));
						Assert.IsTrue(Single.IsFinite(r.Axis.Y));
						Assert.IsTrue(Single.IsFinite(r.Axis.Z));
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
				var rot = Rotation.FromQuaternionPreNormalized(new Quaternion(
					cardinal.X * sinHalfAngle,
					cardinal.Y * sinHalfAngle,
					cardinal.Z * sinHalfAngle,
					cosHalfAngle
				));

				var (angle, axis) = rot;

				Assert.AreEqual(r, angle.Radians, TestTolerance);
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
				ByteSpanSerializationTestUtils.AssertLittleEndianSingles(expected, expected.Axis.X, expected.Axis.Y, expected.Axis.Z, expected.Angle.Radians);
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
		AssertSuccess("100 around <0, 0, 0>", new Rotation(100f, None));
		AssertSuccess("0 around <0, 1, 0>", new Rotation(0f, Up));
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
		Assert.IsTrue(new Rotation(Angle.FromDegrees(0.1f), FromVector3PreNormalized(new(0.2f, 0.3f, 0.4f))).Equals(new Rotation(Angle.FromDegrees(0.2f), FromVector3PreNormalized(new(0.1f, 0.3f, 0.4f))), 0.11f));
		Assert.IsFalse(new Rotation(Angle.FromDegrees(0.1f), FromVector3PreNormalized(new(0.2f, 0.3f, 0.4f))).Equals(new Rotation(Angle.FromDegrees(0.2f), FromVector3PreNormalized(new(0.1f, 0.3f, 0.4f))), 0.09f));
		
		AssertToleranceEquals(Rotation.FromQuaternion(new(1f, 1f, 1f, 1f)).Normalized, Rotation.FromQuaternion(new(-1f, -1f, -1f, -1f)).Normalized, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyImplementEquivalence() {
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					for (var w = -5f; w <= 5f; w += 1f) {
						var rotA = Rotation.FromQuaternion(new Quaternion(x, y, z, w));
						var rotB = Rotation.FromQuaternion(new Quaternion(-x, -y, -z, -w));

						Assert.IsTrue(rotA.IsEquivalentForAllDirectionsTo(rotB, TestTolerance));
						Assert.IsTrue(rotA.IsEquivalentForAllDirectionsTo(rotA.Normalized, TestTolerance));
						Assert.IsTrue(rotA.IsEquivalentForAllDirectionsTo(rotB.Normalized, TestTolerance));
						Assert.IsTrue(rotB.IsEquivalentForAllDirectionsTo(rotA, TestTolerance));
						Assert.IsTrue(rotB.IsEquivalentForAllDirectionsTo(rotA.Normalized, TestTolerance));
						Assert.IsTrue(rotB.IsEquivalentForAllDirectionsTo(rotB.Normalized, TestTolerance));
					}
				}
			}
		}

		Assert.IsTrue((90 % Down).IsEquivalentForSingleDirectionTo(-45f % Down, Up));
		Assert.IsFalse((90 % Down).IsEquivalentForSingleDirectionTo(-45f % Down, Right));
		Assert.IsFalse((89 % Down).IsEquivalentForSingleDirectionTo(91f % Down, Right));
		Assert.IsTrue((89 % Down).IsEquivalentForSingleDirectionTo(91f % Down, Right, 0.3f));
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
	public void ShouldCorrectlyNormalizeRotations() {
		AssertToleranceEquals(0f % Up, (0f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(0f % Down, (0f % Down).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Up, (90f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Down, (90f % Down).Normalized, TestTolerance);
		// No check for 180f exactly as it's not perfectly representable in FP
		AssertToleranceEquals(179.9f % Down, (180.1f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(179.9f % Up, (180.1f % Down).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Down, (270f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Up, (270f % Down).Normalized, TestTolerance);
		AssertToleranceEquals(0f % Down, (360f % Down).Normalized, TestTolerance);
		AssertToleranceEquals(0f % Up, (360f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Down, (450f % Down).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Up, (450f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Down, (-90f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Up, (-90f % Down).Normalized, TestTolerance);
		AssertToleranceEquals(179.9f % Up, (-180.1f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(179.9f % Down, (-180.1f % Down).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Up, (-270f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Down, (-270f % Down).Normalized, TestTolerance);
		AssertToleranceEquals(0f % Up, (-360f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(0f % Down, (-360f % Down).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Down, (-450f % Up).Normalized, TestTolerance);
		AssertToleranceEquals(90f % Up, (-450f % Down).Normalized, TestTolerance);

		AssertToleranceEquals(120f % Up, (240f % Down).Normalized, TestTolerance);
		AssertToleranceEquals(120f % Down, (240f % Up).Normalized, TestTolerance);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					for (var w = -5f; w <= 5f; w += 1f) {
						var rot = Rotation.FromQuaternion(new Quaternion(x, y, z, w));
						var norm = rot.Normalized;

						AssertEquivalence(rot, norm, TestTolerance);
						Assert.GreaterOrEqual(norm.Angle.Radians, Angle.Zero.Radians);
						Assert.Less(norm.Angle.Radians, Angle.HalfCircle.Radians);
						if (rot.Axis != None) Assert.IsTrue(rot.Axis.IsApproximatelyParallelTo(norm.Axis, TestTolerance));
						else Assert.AreEqual(None, norm.Axis);
					}
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyRotateDirectionsAndVects() {
		for (var f = 0f; f <= 360f; f += 18f) {
			var angle = Angle.FromDegrees(f);
			var expectedDir = new Direction(MathF.Sin(angle.Radians), 0f, MathF.Cos(angle.Radians));
			AssertToleranceEquals(expectedDir, angle % Up * Forward, TestTolerance);
			AssertToleranceEquals(expectedDir, (angle % Up).Rotate(Forward), TestTolerance);
			AssertToleranceEquals(expectedDir, (angle % Up).RotateWithoutRenormalizing(Forward), TestTolerance);
			var expectedVect = expectedDir * 3f;
			AssertToleranceEquals(expectedVect, angle % Up * (Forward * 3f), TestTolerance);
			AssertToleranceEquals(expectedVect, (angle % Up).Rotate(Forward * 3f), TestTolerance);
			AssertToleranceEquals(expectedVect, (angle % Up).RotateWithoutCorrectingLength(Forward * 3f), TestTolerance);
		}

		Assert.AreEqual(Up, Up * Rotation.None);
		Assert.AreEqual(new Direction(14f, -15f, -0.2f), Rotation.None.RotateWithoutRenormalizing(new Direction(14f, -15f, -0.2f)));
		Assert.AreEqual(FromVector3PreNormalized(14f, -15f, -0.2f), Rotation.None.RotateWithoutRenormalizing(FromVector3PreNormalized(14f, -15f, -0.2f)));
		Assert.AreEqual(FromVector3PreNormalized(14f, -15f, -0.2f).ToVector3().Length(), NinetyAroundDown.RotateWithoutRenormalizing(FromVector3PreNormalized(14f, -15f, -0.2f)).ToVector3().Length(), TestTolerance);
		Assert.AreEqual(new Vect(14f, -15f, -0.2f), Rotation.None.RotateWithoutCorrectingLength(new Vect(14f, -15f, -0.2f)));

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
				AssertEquivalence(expected, (cardinal % (angle * 0.5f)).CombinedAndNormalizedWith(cardinal % (angle * 0.5f)), TestTolerance);
				AssertEquivalence(expected, (cardinal % (angle * 0.25f)).CombinedAndNormalizedWith(cardinal % (angle * 0.25f)).CombinedAndNormalizedWith(cardinal % (angle * 0.25f)).CombinedAndNormalizedWith(cardinal % (angle * 0.25f)), TestTolerance);
				AssertEquivalence(expected, (cardinal % (angle * -0.5f)).CombinedAndNormalizedWith(cardinal % (angle * 1f)).CombinedAndNormalizedWith(cardinal % (angle * 0.5f)), TestTolerance);
				AssertEquivalence(expected, (cardinal % (angle * 1f)).CombinedAndNormalizedWith(cardinal % (angle * -0.5f)).CombinedAndNormalizedWith(cardinal % (angle * 0.5f)), TestTolerance);
				AssertEquivalence(expected, (cardinal % (angle * 1f)).CombinedAndNormalizedWith(cardinal % (angle * 0.5f)).CombinedAndNormalizedWith(cardinal % (angle * -0.5f)), TestTolerance);
			}
		}

		Assert.IsTrue(NinetyAroundDown.IsEquivalentForSingleDirectionTo((90f % Right).CombinedAndNormalizedWith(90f % Forward), Forward, TestTolerance));
		Assert.IsTrue((45f % Up).CombinedAndNormalizedWith(180f % Forward).IsEquivalentForSingleDirectionTo(-45f % Up, Forward, TestTolerance));
		Assert.IsTrue((45f % Up).CombinedAndNormalizedWith(180f % Forward).IsEquivalentForSingleDirectionTo(-45f % Up, Forward, TestTolerance));
	}

	[Test]
	public void ShouldCorrectlyCalculateDifferenceBetweenRotations() {
		void AssertPair(Angle a1, Direction d1, Angle a2, Direction d2, Rotation expectation) {
			AssertEquivalence(expectation, (a1 % d1).NormalizedDifferenceTo(a2 % d2), TestTolerance);
			AssertEquivalence(-expectation, (a2 % d2).NormalizedDifferenceTo(a1 % d1), TestTolerance);
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
			AssertToleranceEquals(expectation, (a1 % d1).NormalizedAngleTo(a2 % d2), TestTolerance);
			AssertToleranceEquals(expectation, (a2 % d2).NormalizedAngleTo(a1 % d1), TestTolerance);
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

		AssertEquivalence(Rotation.None, NinetyAroundUp * 0f, TestTolerance);
		AssertEquivalence(Rotation.None, NegativeNinetyAroundUp * 0f, TestTolerance);
		AssertEquivalence(Rotation.None, NinetyAroundDown * 0f, TestTolerance);
		AssertEquivalence(Rotation.None, NegativeNinetyAroundDown * 0f, TestTolerance);

		AssertToleranceEquals(Rotation.None, Rotation.None * 0.5f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 1f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 0f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * -0.5f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * -1f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 100f, TestTolerance);

		AssertEquivalence(180f % Up, NinetyAroundUp * 2f, TestTolerance);
		AssertEquivalence(180f % Up, NinetyAroundDown * -2f, TestTolerance);
		AssertEquivalence(180f % Down, NegativeNinetyAroundUp * 2f, TestTolerance);
		AssertEquivalence(180f % Down, NegativeNinetyAroundDown * -2f, TestTolerance);

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

		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, 1f)) * 0f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, 1f)) * -2f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, 1f)) * -1f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, 1f)) * -0.5f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, 1f)) * 0.5f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, 1f)) * 1f);
		Assert.AreEqual(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, 1f)) * 2f);

		AssertEquivalence(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, -1f)) * 0f, 0f);
		AssertEquivalence(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, -1f)) * -2f, 0f);
		AssertEquivalence(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, -1f)) * -1f, 0f);
		AssertEquivalence(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, -1f)) * -0.5f, 0f);
		AssertEquivalence(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, -1f)) * 0.5f, 0f);
		AssertEquivalence(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, -1f)) * 1f, 0f);
		AssertEquivalence(Rotation.None, Rotation.FromQuaternionPreNormalized(new(0f, 0f, 0f, -1f)) * 2f, 0f);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		// Some examples from external sources
		var a = Rotation.None;
		var b = new Rotation(-Angle.HalfCircle, Up);
		var c = new Rotation(Angle.FromRadians(-((3.1415f * 3f) / 2f)), Forward);

		AssertToleranceEquals(new Quaternion(0f, 0.58777f, 0f, 0.809028f), Rotation.AccuratelyInterpolate(a, b, 0.4f).ToQuaternion(), TestTolerance);
		AssertToleranceEquals(new Quaternion(0f, -0.233f, -0.688f, -0.688f), Rotation.AccuratelyInterpolate(b, c, 0.85f).ToQuaternion(), TestTolerance);
		AssertToleranceEquals(Quaternion.Identity, Rotation.AccuratelyInterpolate(c, a, 1f).ToQuaternion(), TestTolerance);

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

				var distance = start.NormalizedAngleTo(end);
				if (distance > Angle.QuarterCircle) continue; // Don't try this with rotations too far apart
				for (var f = -0.05f; f <= 1.05f; f += 0.05f) {
					try {
						AssertToleranceEquals(
							Rotation.AccuratelyInterpolate(start, end, f).ToQuaternion(),
							Rotation.ApproximatelyInterpolate(start, end, f).ToQuaternion(),
							0.01f
						);
					}
					catch (AssertionException) {
						Console.WriteLine(start + " -> " + end + " x " + f);
						Console.WriteLine("Distance " + distance);
						Console.WriteLine("\t" + Rotation.AccuratelyInterpolate(start, end, f) + " / " + Rotation.AccuratelyInterpolate(start, end, f).ToQuaternion());
						Console.WriteLine("\t" + Rotation.ApproximatelyInterpolate(start, end, f) + " / " + Rotation.ApproximatelyInterpolate(start, end, f).ToQuaternion());
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

	[Test]
	public void ShouldCorrectlyCalculateAngleAroundAxis() {
		AssertToleranceEquals(Angle.QuarterCircle, (90f % Down).AngleAroundAxis(Down), TestTolerance);
		AssertToleranceEquals(Angle.Zero, (0f % Down).AngleAroundAxis(Down), TestTolerance);
		AssertToleranceEquals(-Angle.QuarterCircle, (-90f % Down).AngleAroundAxis(Down), TestTolerance);

		AssertToleranceEquals(-Angle.QuarterCircle, (90f % Down).AngleAroundAxis(Up), TestTolerance);
		AssertToleranceEquals(Angle.Zero, (0f % Down).AngleAroundAxis(Up), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, (-90f % Down).AngleAroundAxis(Up), TestTolerance);

		AssertToleranceEquals(Angle.QuarterCircle, (90f % Up).AngleAroundAxis(Up), TestTolerance);
		AssertToleranceEquals(Angle.Zero, (0f % Up).AngleAroundAxis(Up), TestTolerance);
		AssertToleranceEquals(-Angle.QuarterCircle, (-90f % Up).AngleAroundAxis(Up), TestTolerance);

		AssertToleranceEquals(-Angle.QuarterCircle, (90f % Up).AngleAroundAxis(Down), TestTolerance);
		AssertToleranceEquals(Angle.Zero, (0f % Down).AngleAroundAxis(Up), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, (-90f % Up).AngleAroundAxis(Down), TestTolerance);
	}

	[Test]
	public void ShouldBePossibleToScaleRotationsThatProduceIdentityQuaternions() {
		// I created this test when I converted the Rotation type from being quaternion-backed to being truly axis/angle.
		// This test is designed to prove out one of the biggest advantages of this structure, which is that
		// you can specify something like "360 degrees around Down" and it would actually have meaning when scaled.
		// If Rotations are just quaternions under the hood, this can not work.
		var rotation = 360f % Down;
		AssertToleranceEquals(Backward, rotation * 0.5f * Forward, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyScaleQuaternions() {
		AssertEquivalence(NinetyAroundDown * 0.5f, Rotation.FromQuaternionPreNormalized(Rotation.ScaleQuaternion(NinetyAroundDown.ToQuaternion(), 0.5f)), TestTolerance);
		AssertEquivalence(NinetyAroundDown * -0.5f, Rotation.FromQuaternionPreNormalized(Rotation.ScaleQuaternion(NinetyAroundDown.ToQuaternion(), -0.5f)), TestTolerance);
		AssertEquivalence(NinetyAroundDown * 1.5f, Rotation.FromQuaternionPreNormalized(Rotation.ScaleQuaternion(NinetyAroundDown.ToQuaternion(), 1.5f)), TestTolerance);
		AssertEquivalence(NinetyAroundDown * 2.5f, Rotation.FromQuaternionPreNormalized(Rotation.ScaleQuaternion(NinetyAroundDown.ToQuaternion(), 2.5f)), TestTolerance);
		AssertEquivalence(NinetyAroundDown * 0f, Rotation.FromQuaternionPreNormalized(Rotation.ScaleQuaternion(NinetyAroundDown.ToQuaternion(), 0f)), TestTolerance);
	}
}