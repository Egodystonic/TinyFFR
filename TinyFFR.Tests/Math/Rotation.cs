// Created on 2023-10-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;
using static Egodystonic.TinyFFR.Direction;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class RotationTest {
	const float TestTolerance = 0.001f;
	static readonly Rotation NinetyAroundDown = 90f % Down;
	static readonly Rotation NinetyAroundUp = 90f % Up;
	static readonly Rotation NegativeNinetyAroundDown = -90f % Down;
	static readonly Rotation NegativeNinetyAroundUp = -90f % Up;

	[Test]
	public void ShouldCorrectlyInitializeStaticMembers() {
		Assert.AreEqual(new Rotation(Quaternion.Identity), Rotation.None);
	}

	[Test]
	public void AxisAndAnglePropertiesShouldBeImplementedCorrectly() {
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

				Assert.AreEqual(r, rot.Angle.Radians, TestTolerance);
				var expectedAxis = r == 0f ? None : cardinal;
				AssertToleranceEquals(expectedAxis, rot.Axis, TestTolerance);

				Assert.AreEqual(Rotation.None, rot with { Angle = Angle.Zero });
				Assert.AreEqual(Rotation.None, rot with { Axis = None });

				if (r == 0f) {
					Assert.AreEqual(Rotation.None, rot with { Axis = Left });
					Assert.AreEqual(Rotation.None, rot with { Angle = 270f });
					continue;
				}

				var anyPerp = cardinal.GetAnyPerpendicularDirection();
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
		void AssertInitConstructedQuatIsUnit(Angle angle, Direction axis) {
			Assert.AreEqual(1f, new Rotation { Angle = angle, Axis = axis }.AsQuaternion.Length(), TestTolerance);
			Assert.AreEqual(1f, new Rotation { Axis = axis, Angle = angle }.AsQuaternion.Length(), TestTolerance);
		}
		AssertInitConstructedQuatIsUnit(-360f, None);
		AssertInitConstructedQuatIsUnit(-180f, None);
		AssertInitConstructedQuatIsUnit(-90f, None);
		AssertInitConstructedQuatIsUnit(0f, None);
		AssertInitConstructedQuatIsUnit(90f, None);
		AssertInitConstructedQuatIsUnit(180f, None);
		AssertInitConstructedQuatIsUnit(360f, None);
		foreach (var cardinal in AllCardinals) {
			AssertInitConstructedQuatIsUnit(-360f, cardinal);
			AssertInitConstructedQuatIsUnit(-180f, cardinal);
			AssertInitConstructedQuatIsUnit(-90f, cardinal);
			AssertInitConstructedQuatIsUnit(0f, cardinal);
			AssertInitConstructedQuatIsUnit(90f, cardinal);
			AssertInitConstructedQuatIsUnit(180f, cardinal);
			AssertInitConstructedQuatIsUnit(360f, cardinal);
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
				Assert.AreEqual(Rotation.FromAngleAroundAxis(Angle.FromRadians(r), cardinal), new Rotation(Angle.FromRadians(r), cardinal));
			}
		}

		Assert.AreEqual(Rotation.None, Rotation.FromAngleAroundAxis(0f, None));
		Assert.AreEqual(Rotation.None, Rotation.FromAngleAroundAxis(0f, Up));
		Assert.AreEqual(Rotation.None, Rotation.FromAngleAroundAxis(90f, None));

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
	public void ShouldCorrectlyExtractAngleAndAxis() {
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

				rot.ExtractAngleAndAxis(out var angle, out var axis);

				Assert.AreEqual(r, angle.Radians, TestTolerance);
				var expectedAxis = r == 0f ? None : cardinal;
				AssertToleranceEquals(expectedAxis, axis, TestTolerance);
			}
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		foreach (var cardinal in AllCardinals) {
			for (var angle = -360f; angle <= 360f; angle += 36f) {
				var expected = Rotation.FromAngleAroundAxis(angle, cardinal);
				var span = Rotation.ConvertToSpan(expected);
				Assert.AreEqual(expected.AsQuaternion.X, span[0]);
				Assert.AreEqual(expected.AsQuaternion.Y, span[1]);
				Assert.AreEqual(expected.AsQuaternion.Z, span[2]);
				Assert.AreEqual(expected.AsQuaternion.W, span[3]);
				Assert.AreEqual(expected, Rotation.ConvertFromSpan(span));
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
}