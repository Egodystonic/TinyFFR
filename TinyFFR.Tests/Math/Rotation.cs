// Created on 2023-10-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class RotationTest {
	const float TestTolerance = 0.001f;
	static readonly Rotation NinetyAroundDown = 90f ^ Direction.Down;
	static readonly Rotation NinetyAroundUp = 90f ^ Direction.Up;
	static readonly Rotation NegativeNinetyAroundDown = -90f ^ Direction.Down;
	static readonly Rotation NegativeNinetyAroundUp = -90f ^ Direction.Up;

	[Test]
	public void ShouldCorrectlyInitializeStaticMembers() {
		Assert.AreEqual(new Rotation(Quaternion.Identity), Rotation.None);
	}

	[Test]
	public void AxisAndAnglePropertiesShouldBeImplementedCorrectly() {
		foreach (var cardinal in Direction.AllCardinals) {
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
				var expectedAxis = r == 0f ? Direction.None : cardinal;
				AssertToleranceEquals(expectedAxis, rot.Axis, TestTolerance);

				Assert.AreEqual(Rotation.None, rot with { Angle = Angle.Zero });
				Assert.AreEqual(Rotation.None, rot with { Axis = Direction.None });

				if (r == 0f) {
					Assert.AreEqual(Rotation.None, rot with { Axis = Direction.Left });
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
		AssertInitConstructedQuatIsUnit(-360f, Direction.None);
		AssertInitConstructedQuatIsUnit(-180f, Direction.None);
		AssertInitConstructedQuatIsUnit(-90f, Direction.None);
		AssertInitConstructedQuatIsUnit(0f, Direction.None);
		AssertInitConstructedQuatIsUnit(90f, Direction.None);
		AssertInitConstructedQuatIsUnit(180f, Direction.None);
		AssertInitConstructedQuatIsUnit(360f, Direction.None);
		foreach (var cardinal in Direction.AllCardinals) {
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

		foreach (var cardinal in Direction.AllCardinals) {
			for (var r = 0f; r < MathF.Tau * 0.95f; r += MathF.Tau * 0.1f) {
				Assert.AreEqual(new Rotation(Quaternion.CreateFromAxisAngle(cardinal.ToVector3(), r)), new Rotation(Angle.FromRadians(r), cardinal));
			}
		}
	}

	[Test]
	public void StaticFactoryMethodsShouldCorrectlyConstruct() {
		foreach (var cardinal in Direction.AllCardinals) {
			for (var r = 0f; r < MathF.Tau * 0.95f; r += MathF.Tau * 0.1f) {
				Assert.AreEqual(Rotation.FromAngleAroundAxis(Angle.FromRadians(r), cardinal), new Rotation(Angle.FromRadians(r), cardinal));
			}
		}

		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Direction.Forward, Direction.Right));
		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Direction.Right, Direction.Backward));
		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Direction.Backward, Direction.Left));
		Assert.AreEqual(NinetyAroundDown, Rotation.FromStartAndEndDirection(Direction.Left, Direction.Forward));

		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Direction.Right, Direction.Forward));
		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Direction.Backward, Direction.Right));
		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Direction.Left, Direction.Backward));
		Assert.AreEqual(NinetyAroundUp, Rotation.FromStartAndEndDirection(Direction.Forward, Direction.Left));

		foreach (var cardinal in Direction.AllCardinals) {
			Assert.AreEqual(Rotation.None, Rotation.FromStartAndEndDirection(cardinal, cardinal));
		}

		for (var i = 0; i < Direction.AllCardinals.Count; ++i) {
			for (var j = i; j < Direction.AllCardinals.Count; ++j) {
				var dirA = Direction.AllCardinals.ElementAt(i);
				var dirB = Direction.AllCardinals.ElementAt(j);

				AssertToleranceEquals(dirB, Rotation.FromStartAndEndDirection(dirA, dirB) * dirA, TestTolerance);
				AssertToleranceEquals(dirA, Rotation.FromStartAndEndDirection(dirB, dirA) * dirB, TestTolerance);

				if (dirA % dirB == 180f) continue;

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
						Assert.AreEqual(Quaternion.Normalize(q), Rotation.FromQuaternion(q).AsQuaternion);
					}
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyExtractAngleAndAxis() {
		foreach (var cardinal in Direction.AllCardinals) {
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
				var expectedAxis = r == 0f ? Direction.None : cardinal;
				AssertToleranceEquals(expectedAxis, axis, TestTolerance);
			}
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		foreach (var cardinal in Direction.AllCardinals) {
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

		foreach (var cardinal in Direction.AllCardinals) {
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

		var testRot = 12.345f ^ new Direction(1f, 1f, 1f);

		AssertFail(Rotation.None, Array.Empty<char>(), "", null);
		AssertFail(Rotation.None, new char[GetExpectedStrLen(Rotation.None, "", null) - 1], "", null);
		AssertSuccess(Rotation.None, new char[GetExpectedStrLen(Rotation.None, "N0", null)], "N0", null, "0" + Angle.ToStringSuffix + Rotation.ToStringMiddleSection + "<0, 0, 0>");
		AssertFail(testRot, new char[GetExpectedStrLen(testRot, "N0", null) - 1], "N0", null);
		AssertSuccess(testRot, new char[GetExpectedStrLen(testRot, "N0", null)], "N0", null, "12" + Angle.ToStringSuffix + Rotation.ToStringMiddleSection + "<1, 1, 1>");
		AssertSuccess(testRot, new char[GetExpectedStrLen(testRot, "N3", null)], "N3", null, "12.345" + Angle.ToStringSuffix + Rotation.ToStringMiddleSection + "<0.577, 0.577, 0.577>");
	}
}