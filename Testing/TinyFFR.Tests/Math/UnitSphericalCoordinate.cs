// Created on 2023-09-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class UnitSphericalCoordinateTest {
	const float TestTolerance = 0.1f;
	static readonly UnitSphericalCoordinate TestCoord = new(135f, 20f);

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<UnitSphericalCoordinate>(8);

	[Test]
	public void ConstructorsAndPropertiesShouldCorrectlyInitializeValues() {
		Assert.AreEqual(new Angle(135f), TestCoord.AzimuthalOffset);
		Assert.AreEqual(new Angle(20f), TestCoord.PolarOffset);
		Assert.AreEqual(new Angle(200f), (TestCoord with { AzimuthalOffset = 200f }).AzimuthalOffset);
		Assert.AreEqual(new Angle(100f), (TestCoord with { PolarOffset = 100f }).PolarOffset);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<UnitSphericalCoordinate>();
		var anglesToTest = new List<Angle>();
		for (var f = -2f; f < 2.05f; f += 0.05f) anglesToTest.Add(f);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(anglesToTest.Select(a => new UnitSphericalCoordinate(a, a)).ToArray());
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(new UnitSphericalCoordinate(), 0f, 0f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestCoord, new Angle(135f).Radians, new Angle(20f).Radians);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(-TestCoord, TestCoord.Inverted.AzimuthalOffset.Radians, TestCoord.Inverted.PolarOffset.Radians);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = $"UnitSphericalCoordinate[AzimuthalOffset 135.0{Angle.ToStringSuffix} | PolarOffset 20.0{Angle.ToStringSuffix}]";
		Assert.AreEqual(Expectation, TestCoord.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestCoord.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = $"UnitSphericalCoordinate[AzimuthalOffset 135.0{Angle.ToStringSuffix} | PolarOffset 20.0{Angle.ToStringSuffix}]";
		Assert.AreEqual(TestCoord, UnitSphericalCoordinate.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, UnitSphericalCoordinate.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestCoord, result);
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreNotEqual(default(UnitSphericalCoordinate), TestCoord);
		Assert.IsTrue(TestCoord.Equals(TestCoord));
		Assert.IsFalse(TestCoord.Equals(-TestCoord));
		Assert.IsTrue(TestCoord == new UnitSphericalCoordinate(135f, 20f));
		Assert.IsFalse(TestCoord == default);
		Assert.IsFalse(TestCoord != new UnitSphericalCoordinate(135f, 20f));
		Assert.IsTrue(TestCoord != default);

		Assert.IsTrue(default(UnitSphericalCoordinate).Equals(default, 0f));
		Assert.IsTrue(default(UnitSphericalCoordinate).Equals(default, 0f));
		Assert.IsTrue(new UnitSphericalCoordinate(0.5f, 0.5f).Equals(new UnitSphericalCoordinate(0.4f, 0.4f), 0.11f));
		Assert.IsFalse(new UnitSphericalCoordinate(0.5f, 0.5f).Equals(new UnitSphericalCoordinate(0.5f, 0.4f), 0.09f));
		Assert.IsFalse(new UnitSphericalCoordinate(0.5f, 0.5f).Equals(new UnitSphericalCoordinate(0.4f, 0.5f), 0.09f));
		Assert.IsTrue(new UnitSphericalCoordinate(-0.5f, -0.5f).Equals(new UnitSphericalCoordinate(-0.4f, -0.4f), 0.11f));
		Assert.IsFalse(new UnitSphericalCoordinate(-0.5f, -0.5f).Equals(new UnitSphericalCoordinate(-0.5f, -0.4f), 0.09f));
		Assert.IsFalse(new UnitSphericalCoordinate(-0.5f, -0.5f).Equals(new UnitSphericalCoordinate(-0.4f, -0.5f), 0.09f));

		Assert.AreEqual(new Angle(180f), new Angle(180f));
		Assert.AreNotEqual(new Angle(180f), new Angle(180f + 360f));
		Assert.AreNotEqual(new Angle(180f), new Angle(180f - 360f));
	}

	[Test]
	public void ShouldConsiderEquivalentCoordsEqualWhenEqualWithinSphere() {
		Assert.IsTrue(new UnitSphericalCoordinate(135f + 360f, 20f + 360f).IsEquivalentWithinSphereTo(TestCoord));

		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.IsTrue(new UnitSphericalCoordinate(360f + f, 360 + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f), TestTolerance));
			Assert.IsTrue(new UnitSphericalCoordinate(720f + f, 720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f), TestTolerance));
			Assert.IsTrue(new UnitSphericalCoordinate(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f), TestTolerance));
			Assert.IsTrue(new UnitSphericalCoordinate(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f), TestTolerance));

			Assert.IsFalse(new UnitSphericalCoordinate(360f + f, 360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f + 1f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(720f + f, 720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f + 1f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f + 1f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f + 1f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(360f + f, 360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f - 1f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(720f + f, 720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f - 1f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f - 1f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f - 1f), TestTolerance));

			Assert.IsFalse(new UnitSphericalCoordinate(360f + f, 360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f + 1f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(720f + f, 720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f + 1f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f + 1f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f + 1f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(360f + f, 360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f - 1f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(720f + f, 720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f - 1f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f - 1f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f - 1f, f), TestTolerance));

			Assert.IsFalse(new UnitSphericalCoordinate(360f + f, 360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f + 180f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(720f + f, 720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f + 180f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f + 180f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f + 180f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(360f + f, 360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f - 180f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(720f + f, 720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f - 180f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f - 180f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f, f - 180f), TestTolerance));

			Assert.IsFalse(new UnitSphericalCoordinate(360f + f, 360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f + 180f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(720f + f, 720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f + 180f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f + 180f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f + 180f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(360f + f, 360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f - 180f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(720f + f, 720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f - 180f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f - 180f, f), TestTolerance));
			Assert.IsFalse(new UnitSphericalCoordinate(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new UnitSphericalCoordinate(f - 180f, f), TestTolerance));
		}
	}

	[Test]
	public void ShouldCorrectlyCalculateDirection() {
		AssertToleranceEquals(
			Direction.Up * (Direction.Up >> ((135f % Direction.Up) * Direction.Forward)) with { Angle = 20f },
			TestCoord.ToDirection(Direction.Forward, Direction.Up),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Forward,
			new UnitSphericalCoordinate(0f, 90f).ToDirection(Direction.Forward, Direction.Up),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Left,
			new UnitSphericalCoordinate(90f, 90f).ToDirection(Direction.Forward, Direction.Up),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Right,
			new UnitSphericalCoordinate(-90f, 90f).ToDirection(Direction.Forward, Direction.Up),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Left,
			new UnitSphericalCoordinate(90f, 90f).ToDirection(Direction.Backward, Direction.Down),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Right,
			new UnitSphericalCoordinate(-90f, 90f).ToDirection(Direction.Backward, Direction.Down),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Up,
			new UnitSphericalCoordinate(0f, 0f).ToDirection(Direction.Forward, Direction.Up),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Up,
			new UnitSphericalCoordinate(0f, 0f).ToDirection(Direction.Backward, Direction.Up),
			TestTolerance
		);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var azimuthalOffset = Angle.Random(-360f, 360f);
					var polarOffset = Angle.Random(-360f, 360f);

					var azimuthZero = new Direction(x, y, z);
					var polarZero = azimuthZero.AnyOrthogonal();

					if (azimuthZero == Direction.None) {
						Assert.AreEqual(Direction.None, new UnitSphericalCoordinate(azimuthalOffset, 90f).ToDirection(azimuthZero, polarZero));
						Assert.AreEqual(Direction.None, new UnitSphericalCoordinate(0f, polarOffset).ToDirection(azimuthZero, polarZero));
						continue;
					}
					
					AssertToleranceEquals(
						azimuthalOffset.TriangularizeRectified(180f),
						new UnitSphericalCoordinate(azimuthalOffset, 90f).ToDirection(azimuthZero, polarZero).AngleTo(azimuthZero),
						TestTolerance
					);

					AssertToleranceEquals(
						polarOffset.TriangularizeRectified(180f),
						new UnitSphericalCoordinate(0f, polarOffset).ToDirection(azimuthZero, polarZero).AngleTo(polarZero),
						TestTolerance
					);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyInvert() {
		Assert.AreEqual(-TestCoord, TestCoord.Inverted);
		AssertToleranceEquals(new UnitSphericalCoordinate(315f, 160f), TestCoord.Inverted, TestTolerance);

		AssertToleranceEquals(
			Direction.Backward,
			new UnitSphericalCoordinate(0f, 90f).Inverted.ToDirection(Direction.Forward, Direction.Up),
			TestTolerance
		);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var azimuthZero = new Direction(x, y, z);
					var polarZero = azimuthZero.AnyOrthogonal();

					for (var a = -720f; a < 720f + 36f; a += 36f) {
						var coord = new UnitSphericalCoordinate(a, a);

						if (azimuthZero == Direction.None) {
							Assert.AreEqual(Direction.None, coord.ToDirection(azimuthZero, polarZero));
							continue;
						}

						AssertToleranceEquals(
							180f,
							coord.ToDirection(azimuthZero, polarZero).AngleTo(
								coord.Inverted.ToDirection(azimuthZero, polarZero)
							),
							TestTolerance
						);
					}
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyNormalize() {
		AssertToleranceEquals(TestCoord, TestCoord.Normalized, TestTolerance);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var azimuthZero = new Direction(x, y, z);
					var polarZero = azimuthZero.AnyOrthogonal();

					for (var a = -720f; a < 720f + 36f; a += 36f) {
						var coord = new UnitSphericalCoordinate(a, a);

						Assert.IsTrue(
							coord.AzimuthalOffset.IsEquivalentWithinCircleTo(coord.Normalized.AzimuthalOffset, TestTolerance)
						);
						Assert.IsTrue(
							coord.PolarOffset.IsEquivalentWithinCircleTo(coord.Normalized.PolarOffset, TestTolerance)
						);

						AssertToleranceEquals(
							coord.ToDirection(azimuthZero, polarZero),
							coord.Normalized.ToDirection(azimuthZero, polarZero),
							TestTolerance
						);
					}
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		void AssertZeroToHalf(Angle input, Angle expectedOutput) {
			var i = new UnitSphericalCoordinate(input, input);
			var e = new UnitSphericalCoordinate(expectedOutput, expectedOutput);

			Assert.AreEqual(e, i.Clamp(new(Angle.Zero, Angle.Zero), new(Angle.HalfCircle, Angle.HalfCircle)));
			Assert.AreEqual(e, i.Clamp(new(Angle.HalfCircle, Angle.HalfCircle), new(Angle.Zero, Angle.Zero)));
		}

		void AssertZeroToFull(Angle input, Angle expectedOutput) {
			var i = new UnitSphericalCoordinate(input, input);
			var e = new UnitSphericalCoordinate(expectedOutput, expectedOutput);

			Assert.AreEqual(e, i.Clamp(new(Angle.Zero, Angle.Zero), new(Angle.FullCircle, Angle.FullCircle)));
			Assert.AreEqual(e, i.Clamp(new(Angle.FullCircle, Angle.FullCircle), new(Angle.Zero, Angle.Zero)));
		}

		void AssertNegFullToFull(Angle input, Angle expectedOutput) {
			var i = new UnitSphericalCoordinate(input, input);
			var e = new UnitSphericalCoordinate(expectedOutput, expectedOutput);

			Assert.AreEqual(e, i.Clamp(new(-Angle.FullCircle, -Angle.FullCircle), new(Angle.FullCircle, Angle.FullCircle)));
			Assert.AreEqual(e, i.Clamp(new(Angle.FullCircle, Angle.FullCircle), new(-Angle.FullCircle, -Angle.FullCircle)));
		}

		void AssertNegHalfToHalf(Angle input, Angle expectedOutput) {
			var i = new UnitSphericalCoordinate(input, input);
			var e = new UnitSphericalCoordinate(expectedOutput, expectedOutput);

			Assert.AreEqual(e, i.Clamp(new(-Angle.HalfCircle, -Angle.HalfCircle), new(Angle.HalfCircle, Angle.HalfCircle)));
			Assert.AreEqual(e, i.Clamp(new(Angle.HalfCircle, Angle.HalfCircle), new(-Angle.HalfCircle, -Angle.HalfCircle)));
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

		Assert.AreEqual(new UnitSphericalCoordinate(100f, 100f), new UnitSphericalCoordinate(0f, 0f).Clamp(new UnitSphericalCoordinate(100f, 100f), new UnitSphericalCoordinate(100f, 100f)));
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		AssertToleranceEquals(new(315f, 315f), UnitSphericalCoordinate.InterpolateGeometrically(new(270f, 270f), new(0f, 0f), 0.5f), TestTolerance);

		AssertToleranceEquals(new(180f, 180f), UnitSphericalCoordinate.InterpolateGeometrically(new(-100f, -100f), new(100f, 100f), 0.5f), TestTolerance);
		AssertToleranceEquals(new(260f, 260f), UnitSphericalCoordinate.InterpolateGeometrically(new(-100f, -100f), new(100f, 100f), 0f), TestTolerance);
		AssertToleranceEquals(new(100f, 100f), UnitSphericalCoordinate.InterpolateGeometrically(new(-100f, -100f), new(100f, 100f), 1f), TestTolerance);
		AssertToleranceEquals(new(340f, 340f), UnitSphericalCoordinate.InterpolateGeometrically(new(-100f, -100f), new(100f, 100f), -0.5f), TestTolerance);
		AssertToleranceEquals(new(20f, 20f), UnitSphericalCoordinate.InterpolateGeometrically(new(-100f, -100f), new(100f, 100f), 1.5f), TestTolerance);

		AssertToleranceEquals(new(30f, 30f), UnitSphericalCoordinate.InterpolateGeometrically(new(30f, 30f), new(30f, 30f), -1f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), UnitSphericalCoordinate.InterpolateGeometrically(new(30f, 30f), new(30f, 30f), 0f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), UnitSphericalCoordinate.InterpolateGeometrically(new(30f, 30f), new(30f, 30f), 0.5f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), UnitSphericalCoordinate.InterpolateGeometrically(new(30f, 30f), new(30f, 30f), 1f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), UnitSphericalCoordinate.InterpolateGeometrically(new(30f, 30f), new(30f, 30f), 2f), TestTolerance);

		AssertToleranceEquals(new(135f, 135f), UnitSphericalCoordinate.InterpolateArithmetically(new(270f, 270f), new(0f, 0f), 0.5f), TestTolerance);

		AssertToleranceEquals(new(0f, 0f), UnitSphericalCoordinate.InterpolateArithmetically(new(-100f, -100f), new(100f, 100f), 0.5f), TestTolerance);
		AssertToleranceEquals(new(-100f, -100f), UnitSphericalCoordinate.InterpolateArithmetically(new(-100f, -100f), new(100f, 100f), 0f), TestTolerance);
		AssertToleranceEquals(new(100f, 100f), UnitSphericalCoordinate.InterpolateArithmetically(new(-100f, -100f), new(100f, 100f), 1f), TestTolerance);
		AssertToleranceEquals(new(-200f, -200f), UnitSphericalCoordinate.InterpolateArithmetically(new(-100f, -100f), new(100f, 100f), -0.5f), TestTolerance);
		AssertToleranceEquals(new(200f, 200f), UnitSphericalCoordinate.InterpolateArithmetically(new(-100f, -100f), new(100f, 100f), 1.5f), TestTolerance);

		AssertToleranceEquals(new(30f, 30f), UnitSphericalCoordinate.InterpolateArithmetically(new(30f, 30f), new(30f, 30f), -1f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), UnitSphericalCoordinate.InterpolateArithmetically(new(30f, 30f), new(30f, 30f), 0f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), UnitSphericalCoordinate.InterpolateArithmetically(new(30f, 30f), new(30f, 30f), 0.5f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), UnitSphericalCoordinate.InterpolateArithmetically(new(30f, 30f), new(30f, 30f), 1f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), UnitSphericalCoordinate.InterpolateArithmetically(new(30f, 30f), new(30f, 30f), 2f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCreateNonBoundedRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = UnitSphericalCoordinate.Random();
			Assert.GreaterOrEqual(val.AzimuthalOffset.Degrees, 0f);
			Assert.Less(val.AzimuthalOffset.Degrees, 360f);
			Assert.GreaterOrEqual(val.PolarOffset.Degrees, 0f);
			Assert.Less(val.AzimuthalOffset.Degrees, 360f);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateBoundedRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = UnitSphericalCoordinate.Random(new(-720f, -720f), new(720f, 720f));
			Assert.GreaterOrEqual(val.AzimuthalOffset.Degrees, -720f);
			Assert.Less(val.AzimuthalOffset.Degrees, 720f);
			Assert.GreaterOrEqual(val.PolarOffset.Degrees, -720f);
			Assert.Less(val.AzimuthalOffset.Degrees, 720f);
		}
	}
}