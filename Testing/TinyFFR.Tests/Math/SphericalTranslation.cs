// Created on 2023-09-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class SphericalTranslationTest {
	const float TestTolerance = 0.1f;
	static readonly SphericalTranslation TestTranslation = new(135f, 20f);

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<SphericalTranslation>(8);

	[Test]
	public void ConstructorsAndPropertiesShouldCorrectlyInitializeValues() {
		Assert.AreEqual(new Angle(135f), TestTranslation.AzimuthalOffset);
		Assert.AreEqual(new Angle(20f), TestTranslation.PolarOffset);
		Assert.AreEqual(new Angle(200f), (TestTranslation with { AzimuthalOffset = 200f }).AzimuthalOffset);
		Assert.AreEqual(new Angle(100f), (TestTranslation with { PolarOffset = 100f }).PolarOffset);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<SphericalTranslation>();
		var anglesToTest = new List<Angle>();
		for (var f = -2f; f < 2.05f; f += 0.05f) anglesToTest.Add(f);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(anglesToTest.Select(a => new SphericalTranslation(a, a)).ToArray());
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(new SphericalTranslation(), 0f, 0f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestTranslation, new Angle(135f).Radians, new Angle(20f).Radians);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(-TestTranslation, TestTranslation.Inverted.AzimuthalOffset.Radians, TestTranslation.Inverted.PolarOffset.Radians);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = $"SphericalTranslation[AzimuthalOffset 135.0{Angle.ToStringSuffix} | PolarOffset 20.0{Angle.ToStringSuffix}]";
		Assert.AreEqual(Expectation, TestTranslation.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestTranslation.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = $"SphericalTranslation[AzimuthalOffset 135.0{Angle.ToStringSuffix} | PolarOffset 20.0{Angle.ToStringSuffix}]";
		Assert.AreEqual(TestTranslation, SphericalTranslation.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, SphericalTranslation.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestTranslation, result);
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreNotEqual(default(SphericalTranslation), TestTranslation);
		Assert.IsTrue(TestTranslation.Equals(TestTranslation));
		Assert.IsFalse(TestTranslation.Equals(-TestTranslation));
		Assert.IsTrue(TestTranslation == new SphericalTranslation(135f, 20f));
		Assert.IsFalse(TestTranslation == default);
		Assert.IsFalse(TestTranslation != new SphericalTranslation(135f, 20f));
		Assert.IsTrue(TestTranslation != default);

		Assert.IsTrue(default(SphericalTranslation).Equals(default, 0f));
		Assert.IsTrue(default(SphericalTranslation).Equals(default, 0f));
		Assert.IsTrue(new SphericalTranslation(0.5f, 0.5f).Equals(new SphericalTranslation(0.4f, 0.4f), 0.11f));
		Assert.IsFalse(new SphericalTranslation(0.5f, 0.5f).Equals(new SphericalTranslation(0.5f, 0.4f), 0.09f));
		Assert.IsFalse(new SphericalTranslation(0.5f, 0.5f).Equals(new SphericalTranslation(0.4f, 0.5f), 0.09f));
		Assert.IsTrue(new SphericalTranslation(-0.5f, -0.5f).Equals(new SphericalTranslation(-0.4f, -0.4f), 0.11f));
		Assert.IsFalse(new SphericalTranslation(-0.5f, -0.5f).Equals(new SphericalTranslation(-0.5f, -0.4f), 0.09f));
		Assert.IsFalse(new SphericalTranslation(-0.5f, -0.5f).Equals(new SphericalTranslation(-0.4f, -0.5f), 0.09f));

		Assert.AreEqual(new Angle(180f), new Angle(180f));
		Assert.AreNotEqual(new Angle(180f), new Angle(180f + 360f));
		Assert.AreNotEqual(new Angle(180f), new Angle(180f - 360f));
	}

	[Test]
	public void ShouldConsiderEquivalentCoordsEqualWhenEqualWithinSphere() {
		Assert.IsTrue(new SphericalTranslation(135f + 360f, 20f + 360f).IsEquivalentWithinSphereTo(TestTranslation));

		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.IsTrue(new SphericalTranslation(360f + f, 360 + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f), TestTolerance));
			Assert.IsTrue(new SphericalTranslation(720f + f, 720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f), TestTolerance));
			Assert.IsTrue(new SphericalTranslation(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f), TestTolerance));
			Assert.IsTrue(new SphericalTranslation(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f), TestTolerance));

			Assert.IsFalse(new SphericalTranslation(360f + f, 360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f + 1f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(720f + f, 720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f + 1f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f + 1f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f + 1f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(360f + f, 360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f - 1f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(720f + f, 720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f - 1f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f - 1f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f - 1f), TestTolerance));

			Assert.IsFalse(new SphericalTranslation(360f + f, 360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f + 1f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(720f + f, 720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f + 1f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f + 1f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f + 1f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(360f + f, 360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f - 1f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(720f + f, 720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f - 1f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f - 1f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f - 1f, f), TestTolerance));

			Assert.IsFalse(new SphericalTranslation(360f + f, 360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f + 180f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(720f + f, 720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f + 180f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f + 180f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f + 180f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(360f + f, 360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f - 180f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(720f + f, 720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f - 180f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f - 180f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f, f - 180f), TestTolerance));

			Assert.IsFalse(new SphericalTranslation(360f + f, 360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f + 180f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(720f + f, 720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f + 180f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f + 180f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f + 180f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(360f + f, 360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f - 180f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(720f + f, 720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f - 180f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-360f + f, -360f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f - 180f, f), TestTolerance));
			Assert.IsFalse(new SphericalTranslation(-720f + f, -720f + f).IsEquivalentWithinSphereTo(new SphericalTranslation(f - 180f, f), TestTolerance));
		}
	}

	[Test]
	public void ShouldCorrectlyTranslateDirection() {
		AssertToleranceEquals(
			Direction.Up * (Direction.Up >> ((135f % Direction.Up) * Direction.Forward)) with { Angle = 20f },
			TestTranslation.Translate(Direction.Forward, Direction.Up),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Forward,
			new SphericalTranslation(0f, 90f).Translate(Direction.Forward, Direction.Up),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Left,
			new SphericalTranslation(90f, 90f).Translate(Direction.Forward, Direction.Up),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Right,
			new SphericalTranslation(-90f, 90f).Translate(Direction.Forward, Direction.Up),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Left,
			new SphericalTranslation(90f, 90f).Translate(Direction.Backward, Direction.Down),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Right,
			new SphericalTranslation(-90f, 90f).Translate(Direction.Backward, Direction.Down),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Up,
			new SphericalTranslation(0f, 0f).Translate(Direction.Forward, Direction.Up),
			TestTolerance
		);

		AssertToleranceEquals(
			Direction.Up,
			new SphericalTranslation(0f, 0f).Translate(Direction.Backward, Direction.Up),
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
						Assert.AreEqual(Direction.None, new SphericalTranslation(azimuthalOffset, 90f).Translate(azimuthZero, polarZero));
						Assert.AreEqual(Direction.None, new SphericalTranslation(0f, polarOffset).Translate(azimuthZero, polarZero));
						continue;
					}
					
					AssertToleranceEquals(
						azimuthalOffset.TriangularizeRectified(180f),
						new SphericalTranslation(azimuthalOffset, 90f).Translate(azimuthZero, polarZero).AngleTo(azimuthZero),
						TestTolerance
					);

					AssertToleranceEquals(
						polarOffset.TriangularizeRectified(180f),
						new SphericalTranslation(0f, polarOffset).Translate(azimuthZero, polarZero).AngleTo(polarZero),
						TestTolerance
					);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyInvert() {
		Assert.AreEqual(-TestTranslation, TestTranslation.Inverted);
		AssertToleranceEquals(new SphericalTranslation(315f, 160f), TestTranslation.Inverted, TestTolerance);

		AssertToleranceEquals(
			Direction.Backward,
			new SphericalTranslation(0f, 90f).Inverted.Translate(Direction.Forward, Direction.Up),
			TestTolerance
		);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var azimuthZero = new Direction(x, y, z);
					var polarZero = azimuthZero.AnyOrthogonal();

					for (var a = -720f; a < 720f + 36f; a += 36f) {
						var coord = new SphericalTranslation(a, a);

						if (azimuthZero == Direction.None) {
							Assert.AreEqual(Direction.None, coord.Translate(azimuthZero, polarZero));
							continue;
						}

						AssertToleranceEquals(
							180f,
							coord.Translate(azimuthZero, polarZero).AngleTo(
								coord.Inverted.Translate(azimuthZero, polarZero)
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
		AssertToleranceEquals(TestTranslation, TestTranslation.Normalized, TestTolerance);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var azimuthZero = new Direction(x, y, z);
					var polarZero = azimuthZero.AnyOrthogonal();

					for (var a = -720f; a < 720f + 36f; a += 36f) {
						var coord = new SphericalTranslation(a, a);

						Assert.IsTrue(
							coord.AzimuthalOffset.IsEquivalentWithinCircleTo(coord.Normalized.AzimuthalOffset, TestTolerance)
						);
						Assert.IsTrue(
							coord.PolarOffset.IsEquivalentWithinCircleTo(coord.Normalized.PolarOffset, TestTolerance)
						);

						AssertToleranceEquals(
							coord.Translate(azimuthZero, polarZero),
							coord.Normalized.Translate(azimuthZero, polarZero),
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
			var i = new SphericalTranslation(input, input);
			var e = new SphericalTranslation(expectedOutput, expectedOutput);

			Assert.AreEqual(e, i.Clamp(new(Angle.Zero, Angle.Zero), new(Angle.HalfCircle, Angle.HalfCircle)));
			Assert.AreEqual(e, i.Clamp(new(Angle.HalfCircle, Angle.HalfCircle), new(Angle.Zero, Angle.Zero)));
		}

		void AssertZeroToFull(Angle input, Angle expectedOutput) {
			var i = new SphericalTranslation(input, input);
			var e = new SphericalTranslation(expectedOutput, expectedOutput);

			Assert.AreEqual(e, i.Clamp(new(Angle.Zero, Angle.Zero), new(Angle.FullCircle, Angle.FullCircle)));
			Assert.AreEqual(e, i.Clamp(new(Angle.FullCircle, Angle.FullCircle), new(Angle.Zero, Angle.Zero)));
		}

		void AssertNegFullToFull(Angle input, Angle expectedOutput) {
			var i = new SphericalTranslation(input, input);
			var e = new SphericalTranslation(expectedOutput, expectedOutput);

			Assert.AreEqual(e, i.Clamp(new(-Angle.FullCircle, -Angle.FullCircle), new(Angle.FullCircle, Angle.FullCircle)));
			Assert.AreEqual(e, i.Clamp(new(Angle.FullCircle, Angle.FullCircle), new(-Angle.FullCircle, -Angle.FullCircle)));
		}

		void AssertNegHalfToHalf(Angle input, Angle expectedOutput) {
			var i = new SphericalTranslation(input, input);
			var e = new SphericalTranslation(expectedOutput, expectedOutput);

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

		Assert.AreEqual(new SphericalTranslation(100f, 100f), new SphericalTranslation(0f, 0f).Clamp(new SphericalTranslation(100f, 100f), new SphericalTranslation(100f, 100f)));
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		AssertToleranceEquals(new(315f, 315f), SphericalTranslation.InterpolateGeometrically(new(270f, 270f), new(0f, 0f), 0.5f), TestTolerance);

		AssertToleranceEquals(new(180f, 180f), SphericalTranslation.InterpolateGeometrically(new(-100f, -100f), new(100f, 100f), 0.5f), TestTolerance);
		AssertToleranceEquals(new(260f, 260f), SphericalTranslation.InterpolateGeometrically(new(-100f, -100f), new(100f, 100f), 0f), TestTolerance);
		AssertToleranceEquals(new(100f, 100f), SphericalTranslation.InterpolateGeometrically(new(-100f, -100f), new(100f, 100f), 1f), TestTolerance);
		AssertToleranceEquals(new(340f, 340f), SphericalTranslation.InterpolateGeometrically(new(-100f, -100f), new(100f, 100f), -0.5f), TestTolerance);
		AssertToleranceEquals(new(20f, 20f), SphericalTranslation.InterpolateGeometrically(new(-100f, -100f), new(100f, 100f), 1.5f), TestTolerance);

		AssertToleranceEquals(new(30f, 30f), SphericalTranslation.InterpolateGeometrically(new(30f, 30f), new(30f, 30f), -1f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), SphericalTranslation.InterpolateGeometrically(new(30f, 30f), new(30f, 30f), 0f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), SphericalTranslation.InterpolateGeometrically(new(30f, 30f), new(30f, 30f), 0.5f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), SphericalTranslation.InterpolateGeometrically(new(30f, 30f), new(30f, 30f), 1f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), SphericalTranslation.InterpolateGeometrically(new(30f, 30f), new(30f, 30f), 2f), TestTolerance);

		AssertToleranceEquals(new(135f, 135f), SphericalTranslation.InterpolateArithmetically(new(270f, 270f), new(0f, 0f), 0.5f), TestTolerance);

		AssertToleranceEquals(new(0f, 0f), SphericalTranslation.InterpolateArithmetically(new(-100f, -100f), new(100f, 100f), 0.5f), TestTolerance);
		AssertToleranceEquals(new(-100f, -100f), SphericalTranslation.InterpolateArithmetically(new(-100f, -100f), new(100f, 100f), 0f), TestTolerance);
		AssertToleranceEquals(new(100f, 100f), SphericalTranslation.InterpolateArithmetically(new(-100f, -100f), new(100f, 100f), 1f), TestTolerance);
		AssertToleranceEquals(new(-200f, -200f), SphericalTranslation.InterpolateArithmetically(new(-100f, -100f), new(100f, 100f), -0.5f), TestTolerance);
		AssertToleranceEquals(new(200f, 200f), SphericalTranslation.InterpolateArithmetically(new(-100f, -100f), new(100f, 100f), 1.5f), TestTolerance);

		AssertToleranceEquals(new(30f, 30f), SphericalTranslation.InterpolateArithmetically(new(30f, 30f), new(30f, 30f), -1f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), SphericalTranslation.InterpolateArithmetically(new(30f, 30f), new(30f, 30f), 0f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), SphericalTranslation.InterpolateArithmetically(new(30f, 30f), new(30f, 30f), 0.5f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), SphericalTranslation.InterpolateArithmetically(new(30f, 30f), new(30f, 30f), 1f), TestTolerance);
		AssertToleranceEquals(new(30f, 30f), SphericalTranslation.InterpolateArithmetically(new(30f, 30f), new(30f, 30f), 2f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCreateNonBoundedRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = SphericalTranslation.Random();
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
			var val = SphericalTranslation.Random(new(-720f, -720f), new(720f, 720f));
			Assert.GreaterOrEqual(val.AzimuthalOffset.Degrees, -720f);
			Assert.Less(val.AzimuthalOffset.Degrees, 720f);
			Assert.GreaterOrEqual(val.PolarOffset.Degrees, -720f);
			Assert.Less(val.AzimuthalOffset.Degrees, 720f);
		}
	}
}