// Created on 2023-09-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class AngleTest {
	const float TestTolerance = 0.1f;

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<Angle>();

	[Test]
	public void StaticReadonlyMembersShouldBeCorrectlyInitialized() {
		AssertToleranceEquals(Angle.FromRadians(0f), Angle.Zero, 0f);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI * 0.25f), Angle.EighthCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI * 0.5f), Angle.QuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI), Angle.HalfCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI * 1.5f), Angle.ThreeQuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI * 2f), Angle.FullCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI * 0.25f), -Angle.EighthCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI * 0.5f), -Angle.QuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI), -Angle.HalfCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI * 1.5f), -Angle.ThreeQuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI * 2f), -Angle.FullCircle, TestTolerance);
	}

	[Test]
	public void PropertiesShouldCorrectlyConvertToAndFromRadians() {
		Assert.AreEqual(0f, Angle.Zero.AsRadians, 0f);
		Assert.AreEqual(0f, Angle.Zero.AsDegrees, 0f);
		Assert.AreEqual(0f, Angle.Zero.AsFullCircleFraction, 0f);

		Assert.AreEqual(MathF.PI * 0.5f, Angle.QuarterCircle.AsRadians, 0f);
		Assert.AreEqual(90f, Angle.QuarterCircle.AsDegrees, TestTolerance);
		Assert.AreEqual(0.25f, Angle.QuarterCircle.AsFullCircleFraction, TestTolerance);
		Assert.AreEqual(-MathF.PI * 0.5f, -Angle.QuarterCircle.AsRadians, 0f);
		Assert.AreEqual(-90f, -Angle.QuarterCircle.AsDegrees, TestTolerance);
		Assert.AreEqual(-0.25f, -Angle.QuarterCircle.AsFullCircleFraction, TestTolerance);

		Assert.AreEqual(MathF.PI, Angle.HalfCircle.AsRadians, 0f);
		Assert.AreEqual(180f, Angle.HalfCircle.AsDegrees, TestTolerance);
		Assert.AreEqual(0.5f, Angle.HalfCircle.AsFullCircleFraction, TestTolerance);
		Assert.AreEqual(-MathF.PI, -Angle.HalfCircle.AsRadians, 0f);
		Assert.AreEqual(-180f, -Angle.HalfCircle.AsDegrees, TestTolerance);
		Assert.AreEqual(-0.5f, -Angle.HalfCircle.AsFullCircleFraction, TestTolerance);

		Assert.AreEqual(MathF.PI * 1.5f, Angle.ThreeQuarterCircle.AsRadians, 0f);
		Assert.AreEqual(270f, Angle.ThreeQuarterCircle.AsDegrees, TestTolerance);
		Assert.AreEqual(0.75f, Angle.ThreeQuarterCircle.AsFullCircleFraction, TestTolerance);
		Assert.AreEqual(-MathF.PI * 1.5f, -Angle.ThreeQuarterCircle.AsRadians, 0f);
		Assert.AreEqual(-270f, -Angle.ThreeQuarterCircle.AsDegrees, TestTolerance);
		Assert.AreEqual(-0.75f, -Angle.ThreeQuarterCircle.AsFullCircleFraction, TestTolerance);

		Assert.AreEqual(MathF.PI * 2f, Angle.FullCircle.AsRadians, 0f);
		Assert.AreEqual(360f, Angle.FullCircle.AsDegrees, TestTolerance);
		Assert.AreEqual(1f, Angle.FullCircle.AsFullCircleFraction, TestTolerance);
		Assert.AreEqual(-MathF.PI * 2f, -Angle.FullCircle.AsRadians, 0f);
		Assert.AreEqual(-360f, -Angle.FullCircle.AsDegrees, TestTolerance);
		Assert.AreEqual(-1f, -Angle.FullCircle.AsFullCircleFraction, TestTolerance);

		for (var f = -720f; f <= 720f + 36f; f += 36f) {
			Assert.AreEqual((MathF.Tau / 360f) * f, new Angle(f).AsRadians, TestTolerance);
			Assert.AreEqual(f, new Angle(f).AsDegrees, TestTolerance);
			Assert.AreEqual(f / 360f, new Angle(f).AsFullCircleFraction, TestTolerance);
		}		
	}

	[Test]
	public void ConstructorsAndConversionsShouldCorrectlyInitializeValue() {
		for (var f = -720f; f <= 720f + 36f; f += 36f) {
			AssertToleranceEquals(Angle.FromDegrees(f), new Angle(f), 0f);
			AssertToleranceEquals(Angle.FromDegrees(f), f, 0f);
		}
	}

	[Test]
	public void BasicFactoryMethodsShouldCorrectlyInitializeValue() {
		// radians
		for (var f = -MathF.Tau * 2f; f < MathF.Tau * 2.05f; f += MathF.Tau * 0.05f) {
			Assert.AreEqual(f, Angle.FromRadians(f).AsRadians);
		}

		// degrees
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.AreEqual(f, Angle.FromDegrees(f).AsDegrees, TestTolerance);
		}

		// circle fraction
		for (var f = -2f; f < 2.05f; f += 0.05f) {
			Assert.AreEqual(f, Angle.FromFullCircleFraction(f).AsFullCircleFraction, TestTolerance);
		}

		// sine
		for (var f = -2f; f < 2.05f; f += 0.05f) {
			if (f < -1f) Assert.AreEqual(-Angle.QuarterCircle, Angle.FromSine(f));
			else if (f > 1f) Assert.AreEqual(Angle.QuarterCircle, Angle.FromSine(f));
			else Assert.AreEqual(Angle.FromRadians(MathF.Asin(f)), Angle.FromSine(f));
		}

		// cosine
		for (var f = -2f; f < 2.05f; f += 0.05f) {
			if (f < -1f) Assert.AreEqual(Angle.HalfCircle, Angle.FromCosine(f));
			else if (f > 1f) Assert.AreEqual(Angle.Zero, Angle.FromCosine(f));
			else Assert.AreEqual(Angle.FromRadians(MathF.Acos(f)), Angle.FromCosine(f));
		}
	}

	[Test]
	public void ShouldCorrectlyCalculateAngleBetweenDirections() {
		Assert.AreEqual(Angle.Zero, Angle.FromAngleBetweenDirections(Direction.Forward, Direction.Forward));
		Assert.AreEqual(Angle.HalfCircle, Angle.FromAngleBetweenDirections(Direction.Forward, Direction.Backward));
		Assert.AreEqual(Angle.HalfCircle, Angle.FromAngleBetweenDirections(Direction.Right, Direction.Left));
		Assert.AreEqual(Angle.HalfCircle, Angle.FromAngleBetweenDirections(Direction.Up, Direction.Down));
		Assert.AreEqual(Angle.QuarterCircle, Angle.FromAngleBetweenDirections(Direction.Forward, Direction.Left));
		Assert.AreEqual(Angle.QuarterCircle, Angle.FromAngleBetweenDirections(Direction.Forward, Direction.Right));
		Assert.AreEqual(Angle.QuarterCircle, Angle.FromAngleBetweenDirections(Direction.Backward, Direction.Left));
		Assert.AreEqual(Angle.QuarterCircle, Angle.FromAngleBetweenDirections(Direction.Backward, Direction.Right));

		var rot45AroundUp = new Rotation(45f, Direction.Up);
		var rotNeg45AroundUp = new Rotation(-45f, Direction.Up);

		AssertToleranceEquals(new Angle(45f), Angle.FromAngleBetweenDirections(Direction.Forward * rot45AroundUp, Direction.Forward), TestTolerance);
		AssertToleranceEquals(new Angle(90f), Angle.FromAngleBetweenDirections(Direction.Forward * rot45AroundUp, Direction.Forward * rotNeg45AroundUp), TestTolerance);

		// One using Wolfram Alpha as a sanity check (randomish vectors): https://www.wolframalpha.com/input?i=angle+between+%280.251398%2C+-0.967884%2C+0%29+and+%280.733632%2C+0.507899%2C+-0.451466%29
		var d1 = new Direction(0.733632f, 0.507899f, -0.451466f);
		var d2 = new Direction(0.251398f, -0.967884f, 0f);
		AssertToleranceEquals(new Angle(107.888f), Angle.FromAngleBetweenDirections(d1, d2), TestTolerance);
		AssertToleranceEquals(new Angle(107.888f), Angle.FromAngleBetweenDirections(d2, d1), TestTolerance);

		// Check that the order of arguments makes no difference
		for (var i = 0; i < Direction.AllCardinals.Length; ++i) {
			for (var j = i; j < Direction.AllCardinals.Length; ++j) {
				d1 = Direction.AllCardinals[i];
				d2 = Direction.AllCardinals[j];
				Assert.AreEqual(Angle.FromAngleBetweenDirections(d1, d2), Angle.FromAngleBetweenDirections(d2, d1));
			}
		}

		Assert.AreEqual(Angle.Zero, Angle.FromAngleBetweenDirections(Direction.None, Direction.Left));
		Assert.AreEqual(Angle.Zero, Angle.FromAngleBetweenDirections(Direction.Left, Direction.None));
	}

	[Test]
	public void AngleBetweenDirectionsShouldUseAppropriateErrorMargin() {
		Assert.AreEqual(Angle.Zero, Angle.FromAngleBetweenDirections((1f, 0f, 0f), (1f, 0f, 0f)));
		Assert.AreEqual(Angle.Zero, Angle.FromAngleBetweenDirections((1f, 0f, 0f), (0.999f, 0f, 0.001f)));
		Assert.AreNotEqual(Angle.Zero, Angle.FromAngleBetweenDirections((1f, 0f, 0f), (0.99f, 0f, 0.01f)));

		Assert.AreEqual(Angle.QuarterCircle, Angle.FromAngleBetweenDirections((1f, 0f, 0f), (0f, 1f, 0f)));
		Assert.AreEqual(Angle.QuarterCircle, Angle.FromAngleBetweenDirections((1f, 0f, 0f), (0f, 0.999f, 0.001f)));
		Assert.AreNotEqual(Angle.QuarterCircle, Angle.FromAngleBetweenDirections((1f, 0f, 0f), (0.01f, 0.99f, 0f)));

		Assert.AreEqual(Angle.HalfCircle, Angle.FromAngleBetweenDirections((1f, 0f, 0f), (-1f, 0f, 0f)));
		Assert.AreEqual(Angle.HalfCircle, Angle.FromAngleBetweenDirections((1f, 0f, 0f), (-0.999f, 0f, -0.001f)));
		Assert.AreNotEqual(Angle.HalfCircle, Angle.FromAngleBetweenDirections((1f, 0f, 0f), (-0.99f, 0f, -0.01f)));
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<Angle>();
		var anglesToTest = new List<Angle>();
		for (var f = -2f; f < 2.05f; f += 0.05f) anglesToTest.Add(f);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(anglesToTest.ToArray());
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(Angle.Zero, 0f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(Angle.EighthCircle, Angle.EighthCircle.AsRadians);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(Angle.ThreeQuarterCircle, Angle.ThreeQuarterCircle.AsRadians);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(-Angle.ThreeQuarterCircle, -Angle.ThreeQuarterCircle.AsRadians);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(Angle input, string expectedStringValuePart) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N0";
			var expectedValue = $"{expectedStringValuePart}{Angle.ToStringSuffix}";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);
			
			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(Angle.Zero, "0");
		AssertIteration(Angle.QuarterCircle, "90");
		AssertIteration(Angle.HalfCircle, "180");
		AssertIteration(Angle.ThreeQuarterCircle, "270");
		AssertIteration(Angle.FullCircle, "360");
		AssertIteration(Angle.FullCircle * 2f, "720");
		AssertIteration(-Angle.QuarterCircle, "-90");
		AssertIteration(-Angle.HalfCircle, "-180");
		AssertIteration(-Angle.ThreeQuarterCircle, "-270");
		AssertIteration(-Angle.FullCircle, "-360");
		AssertIteration(-Angle.FullCircle * 2f, "-720");
		AssertIteration(1080f, "1,080");
		AssertIteration(-1080f, "-1,080");
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(Angle input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			Angle input, 
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

		var fractionalAngle = Angle.FromDegrees(12.345f);

		AssertFail(Angle.Zero, Array.Empty<char>(), "", null);
		AssertFail(Angle.Zero, new char[1], "", null);
		AssertSuccess(Angle.Zero, new char[2], "N0", null, "0" + Angle.ToStringSuffix);
		AssertFail(fractionalAngle, new char[2], "N0", null);
		AssertSuccess(fractionalAngle, new char[3], "N0", null, "12" + Angle.ToStringSuffix);
		AssertFail(fractionalAngle, new char[4], "N1", null);
		AssertSuccess(fractionalAngle, new char[5], "N1", null, "12.3" + Angle.ToStringSuffix);
		AssertSuccess(fractionalAngle, new char[5], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "12,3" + Angle.ToStringSuffix);
		AssertSuccess(fractionalAngle, new char[20], "N5", null, "12.34500" + Angle.ToStringSuffix);
	}

	[Test]
	public void ShouldCorrectlyParseFromString() {
		var testCulture = CultureInfo.InvariantCulture;

		void AssertSuccess(string input, Angle expectedResult) {
			AssertToleranceEquals(expectedResult, Angle.Parse(input, testCulture), TestTolerance);
			AssertToleranceEquals(expectedResult, Angle.Parse(input.AsSpan(), testCulture), TestTolerance);
			Assert.IsTrue(Angle.TryParse(input, testCulture, out var parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
			Assert.IsTrue(Angle.TryParse(input.AsSpan(), testCulture, out parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
		}

		void AssertFailure(string input) {
			Assert.Catch(() => Angle.Parse(input, testCulture));
			Assert.Catch(() => Angle.Parse(input.AsSpan(), testCulture));
			Assert.False(Angle.TryParse(input, testCulture, out _));
			Assert.False(Angle.TryParse(input.AsSpan(), testCulture, out _));
		}

		AssertSuccess("180", Angle.HalfCircle);
		AssertSuccess("180.000", Angle.HalfCircle);
		AssertSuccess("180" + Angle.ToStringSuffix, Angle.HalfCircle);
		AssertSuccess("180 " + Angle.ToStringSuffix, Angle.HalfCircle);
		AssertSuccess("-180", -Angle.HalfCircle);
		AssertSuccess("-180.000", -Angle.HalfCircle);
		AssertSuccess("-180" + Angle.ToStringSuffix, -Angle.HalfCircle);
		AssertSuccess("123.456", Angle.FromDegrees(123.456f));
		AssertSuccess("-123.456" + Angle.ToStringSuffix, Angle.FromDegrees(-123.456f));

		AssertFailure("");
		AssertFailure("abc");
		AssertFailure(Angle.ToStringSuffix);
		AssertFailure(Angle.ToStringSuffix + "123");
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreEqual(Angle.Zero, -Angle.Zero);
		Assert.AreNotEqual(Angle.Zero, Angle.QuarterCircle);
		Assert.IsTrue(Angle.HalfCircle.Equals(Angle.HalfCircle));
		Assert.IsFalse(Angle.FullCircle.Equals(Angle.HalfCircle));
		Assert.IsTrue(Angle.HalfCircle == 180f);
		Assert.IsFalse(Angle.FullCircle == Angle.HalfCircle);
		Assert.IsFalse(Angle.HalfCircle != 180f);
		Assert.IsTrue(Angle.FullCircle != Angle.HalfCircle);

		Assert.IsTrue(Angle.Zero.Equals(Angle.Zero, 0f));
		Assert.IsTrue(Angle.HalfCircle.Equals(Angle.HalfCircle, 0f));
		Assert.IsTrue(new Angle(0.5f).Equals(new Angle(0.4f), 0.11f));
		Assert.IsFalse(new Angle(0.5f).Equals(new Angle(0.4f), 0.09f));
		Assert.IsTrue(new Angle(-0.5f).Equals(new Angle(-0.4f), 0.11f));
		Assert.IsFalse(new Angle(-0.5f).Equals(new Angle(-0.4f), 0.09f));
		Assert.IsFalse(new Angle(-0.5f).Equals(new Angle(0.4f), 0.11f));

		Assert.AreEqual(new Angle(180f), new Angle(180f));
		Assert.AreNotEqual(new Angle(180f), new Angle(180f + 360f));
		Assert.AreNotEqual(new Angle(180f), new Angle(180f - 360f));
	}

	[Test]
	public void ShouldConsiderEquivalentAnglesEqualWhenEqualWithinCircle() {
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.IsTrue(new Angle(360f + f).EqualsWithinCircle(new Angle(f), TestTolerance));
			Assert.IsTrue(new Angle(720f + f).EqualsWithinCircle(new Angle(f), TestTolerance));
			Assert.IsTrue(new Angle(-360f + f).EqualsWithinCircle(new Angle(f), TestTolerance));
			Assert.IsTrue(new Angle(-720f + f).EqualsWithinCircle(new Angle(f), TestTolerance));

			Assert.IsFalse(new Angle(360f + f).EqualsWithinCircle(new Angle(f + 1f), TestTolerance));
			Assert.IsFalse(new Angle(720f + f).EqualsWithinCircle(new Angle(f + 1f), TestTolerance));
			Assert.IsFalse(new Angle(-360f + f).EqualsWithinCircle(new Angle(f + 1f), TestTolerance));
			Assert.IsFalse(new Angle(-720f + f).EqualsWithinCircle(new Angle(f + 1f), TestTolerance));
			Assert.IsFalse(new Angle(360f + f).EqualsWithinCircle(new Angle(f - 1f), TestTolerance));
			Assert.IsFalse(new Angle(720f + f).EqualsWithinCircle(new Angle(f - 1f), TestTolerance));
			Assert.IsFalse(new Angle(-360f + f).EqualsWithinCircle(new Angle(f - 1f), TestTolerance));
			Assert.IsFalse(new Angle(-720f + f).EqualsWithinCircle(new Angle(f - 1f), TestTolerance));

			Assert.IsFalse(new Angle(360f + f).EqualsWithinCircle(new Angle(f + 180f), TestTolerance));
			Assert.IsFalse(new Angle(720f + f).EqualsWithinCircle(new Angle(f + 180f), TestTolerance));
			Assert.IsFalse(new Angle(-360f + f).EqualsWithinCircle(new Angle(f + 180f), TestTolerance));
			Assert.IsFalse(new Angle(-720f + f).EqualsWithinCircle(new Angle(f + 180f), TestTolerance));
			Assert.IsFalse(new Angle(360f + f).EqualsWithinCircle(new Angle(f - 180f), TestTolerance));
			Assert.IsFalse(new Angle(720f + f).EqualsWithinCircle(new Angle(f - 180f), TestTolerance));
			Assert.IsFalse(new Angle(-360f + f).EqualsWithinCircle(new Angle(f - 180f), TestTolerance));
			Assert.IsFalse(new Angle(-720f + f).EqualsWithinCircle(new Angle(f - 180f), TestTolerance));
		}
	}

	[Test]
	public void ShouldCorrectlyCalculatePolarAngle() {
		Assert.AreEqual(null, Angle.From2DPolarAngle(0f, 0f));
		AssertToleranceEquals(0f, Angle.From2DPolarAngle(1f, 0f)!.Value, TestTolerance);
		AssertToleranceEquals(45f, Angle.From2DPolarAngle(1f, 1f)!.Value, TestTolerance);
		AssertToleranceEquals(90f, Angle.From2DPolarAngle(0f, 1f)!.Value, TestTolerance);
		AssertToleranceEquals(135f, Angle.From2DPolarAngle(-1f, 1f)!.Value, TestTolerance);
		AssertToleranceEquals(180f, Angle.From2DPolarAngle(-1f, 0f)!.Value, TestTolerance);
		AssertToleranceEquals(225f, Angle.From2DPolarAngle(-1f, -1f)!.Value, TestTolerance);
		AssertToleranceEquals(270f, Angle.From2DPolarAngle(0f, -1f)!.Value, TestTolerance);
		AssertToleranceEquals(315f, Angle.From2DPolarAngle(1f, -1f)!.Value, TestTolerance);

		Assert.AreEqual(null, Angle.From2DPolarAngle(Orientation2D.None));
		AssertToleranceEquals(0f, Angle.From2DPolarAngle(Orientation2D.Right)!.Value, TestTolerance);
		AssertToleranceEquals(45f, Angle.From2DPolarAngle(Orientation2D.UpRight)!.Value, TestTolerance);
		AssertToleranceEquals(90f, Angle.From2DPolarAngle(Orientation2D.Up)!.Value, TestTolerance);
		AssertToleranceEquals(135f, Angle.From2DPolarAngle(Orientation2D.UpLeft)!.Value, TestTolerance);
		AssertToleranceEquals(180f, Angle.From2DPolarAngle(Orientation2D.Left)!.Value, TestTolerance);
		AssertToleranceEquals(225f, Angle.From2DPolarAngle(Orientation2D.DownLeft)!.Value, TestTolerance);
		AssertToleranceEquals(270f, Angle.From2DPolarAngle(Orientation2D.Down)!.Value, TestTolerance);
		AssertToleranceEquals(315f, Angle.From2DPolarAngle(Orientation2D.DownRight)!.Value, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyNegateAngle() {
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.AreEqual(new Angle(-f), new Angle(f).Negated);
			Assert.AreEqual(new Angle(-f), -new Angle(f));
		}
	}

	[Test]
	public void ShouldCorrectlyReturnAbsoluteAngle() {
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.AreEqual(new Angle(MathF.Abs(f)), new Angle(f).Absolute);
		}
	}

	[Test]
	public void ShouldCorrectlyReturnNormalizedAngle() {
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			AssertToleranceEquals(new Angle(MathUtils.TrueModulus(f, 360f)), new Angle(f).Normalized, TestTolerance);
			Assert.AreEqual(new Angle(f).Sine, new Angle(f).Normalized.Sine, TestTolerance);
			Assert.GreaterOrEqual(new Angle(f).Normalized.AsDegrees, 0f);
			Assert.LessOrEqual(new Angle(f).Normalized.AsDegrees, 360f);
		}
	}

	[Test]
	public void ShouldCorrectlyCalculateNormalizedDifferences() {
		AssertToleranceEquals(0f, Angle.Zero.AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, Angle.Zero.AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, Angle.Zero.AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.Zero.AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.Zero.AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(180f, Angle.HalfCircle.AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(180f, Angle.HalfCircle.AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(0f, Angle.HalfCircle.AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.HalfCircle.AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.HalfCircle.AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(180f, new Angle(-180f).AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(180f, new Angle(-180f).AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(0f, new Angle(-180f).AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(-180f).AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(-180f).AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, Angle.FullCircle.AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, Angle.FullCircle.AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, Angle.FullCircle.AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.FullCircle.AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.FullCircle.AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, new Angle(720f).AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, new Angle(720f).AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, new Angle(720f).AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(720f).AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(720f).AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, new Angle(360f).AbsoluteDifferenceTo(Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, new Angle(360f).AbsoluteDifferenceTo(Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, new Angle(360f).AbsoluteDifferenceTo(Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(360f).AbsoluteDifferenceTo(Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(360f).AbsoluteDifferenceTo(Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, Angle.Zero.AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, Angle.Zero.AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, Angle.Zero.AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.Zero.AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.Zero.AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(180f, Angle.HalfCircle.AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(180f, Angle.HalfCircle.AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(0f, Angle.HalfCircle.AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.HalfCircle.AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.HalfCircle.AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(180f, new Angle(-180f).AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(180f, new Angle(-180f).AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(0f, new Angle(-180f).AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(-180f).AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(-180f).AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, Angle.FullCircle.AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, Angle.FullCircle.AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, Angle.FullCircle.AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.FullCircle.AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, Angle.FullCircle.AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, new Angle(720f).AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, new Angle(720f).AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, new Angle(720f).AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(720f).AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(720f).AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);

		AssertToleranceEquals(0f, new Angle(360f).AbsoluteDifferenceTo(-Angle.Zero), TestTolerance);
		AssertToleranceEquals(0f, new Angle(360f).AbsoluteDifferenceTo(-Angle.FullCircle), TestTolerance);
		AssertToleranceEquals(180f, new Angle(360f).AbsoluteDifferenceTo(-Angle.HalfCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(360f).AbsoluteDifferenceTo(-Angle.QuarterCircle), TestTolerance);
		AssertToleranceEquals(90f, new Angle(360f).AbsoluteDifferenceTo(-Angle.ThreeQuarterCircle), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyMultiplyAndDivide() {
		for (var f = -MathF.Tau * 2f; f < MathF.Tau * 2.05f; f += MathF.Tau * 0.05f) {
			for (var s = -2f; s < 2.1f; s += 0.1f) {
				Assert.AreEqual(Angle.FromRadians(f * s), Angle.FromRadians(f) * s);
				Assert.AreEqual(Angle.FromRadians(f * s), s * Angle.FromRadians(f));
				Assert.AreEqual(Angle.FromRadians(f * s), Angle.FromRadians(f).ScaledBy(s));
				if (MathF.Abs(s) < 0.001f) continue;
				Assert.AreEqual(Angle.FromRadians(f / s), Angle.FromRadians(f) / s);
				AssertToleranceEquals(Angle.FromRadians(f / s), Angle.FromRadians(f).ScaledBy(1f / s), 0.01f);
			}
		}
	}

	[Test]
	public void ShouldCorrectlyAddAndSubtract() {
		for (var f = -MathF.Tau * 2f; f < MathF.Tau * 2.05f; f += MathF.Tau * 0.05f) {
			Assert.AreEqual(new Angle(f + f), new Angle(f) + new Angle(f));
			Assert.AreEqual(new Angle(f + f), new Angle(f).Plus(new Angle(f)));
			Assert.AreEqual(new Angle(f - f), new Angle(f) - new Angle(f));
			Assert.AreEqual(new Angle(f - f), new Angle(f).Minus(new Angle(f)));
		}
	}

	[Test]
	public void SineAndCosinePropertiesShouldBeCorrect() {
		foreach (var f in new[] { -1f, -0.75f, -0.5f, -0.25f, 0f, 0.25f, 0.5f, 0.75f, 1f }) {
			Assert.AreEqual(f, Angle.FromSine(f).Sine, TestTolerance);
			Assert.AreEqual(f, Angle.FromCosine(f).Cosine, TestTolerance);
		}
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		void AssertZeroToHalf(Angle input, Angle expectedOutput) {
			Assert.AreEqual(expectedOutput, input.ClampZeroToHalfCircle());
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.Zero, Angle.HalfCircle));
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.HalfCircle, Angle.Zero));
		}

		void AssertZeroToFull(Angle input, Angle expectedOutput) {
			Assert.AreEqual(expectedOutput, input.ClampZeroToFullCircle());
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.Zero, Angle.FullCircle));
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.FullCircle, Angle.Zero));
		}

		void AssertNegFullToFull(Angle input, Angle expectedOutput) {
			Assert.AreEqual(expectedOutput, input.ClampNegativeFullCircleToFullCircle());
			Assert.AreEqual(expectedOutput, input.Clamp(-Angle.FullCircle, Angle.FullCircle));
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.FullCircle, -Angle.FullCircle));
		}

		void AssertNegHalfToHalf(Angle input, Angle expectedOutput) {
			Assert.AreEqual(expectedOutput, input.ClampNegativeHalfCircleToHalfCircle());
			Assert.AreEqual(expectedOutput, input.Clamp(-Angle.HalfCircle, Angle.HalfCircle));
			Assert.AreEqual(expectedOutput, input.Clamp(Angle.HalfCircle, -Angle.HalfCircle));
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

		Assert.AreEqual(new Angle(100f), new Angle(0f).Clamp(new Angle(100f), new Angle(100f)));
	}

	[Test]
	public void ShouldCorrectlyImplementComparisonOperators() {
		var angleList = new[] { -Angle.FullCircle, -Angle.HalfCircle, Angle.Zero, Angle.HalfCircle, Angle.FullCircle };

		for (var i = 0; i < angleList.Length; ++i) {
			for (var j = i; j < angleList.Length; ++j) {
				var lhs = angleList[i];
				var rhs = angleList[j];

				Assert.AreEqual(lhs.AsRadians > rhs.AsRadians, lhs > rhs);
				Assert.AreEqual(lhs.AsRadians >= rhs.AsRadians, lhs >= rhs);
				Assert.AreEqual(lhs.AsRadians < rhs.AsRadians, lhs < rhs);
				Assert.AreEqual(lhs.AsRadians <= rhs.AsRadians, lhs <= rhs);
				Assert.AreEqual(lhs.AsRadians.CompareTo(rhs.AsRadians), lhs.CompareTo(rhs));
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCalculatePolarDirection() {
		Assert.AreEqual(Orientation2D.Right, Angle.From2DPolarAngle(1f, 0f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.UpRight, Angle.From2DPolarAngle(1f, 1f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.Up, Angle.From2DPolarAngle(0f, 1f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.UpLeft, Angle.From2DPolarAngle(-1f, 1f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.Left, Angle.From2DPolarAngle(-1f, 0f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.DownLeft, Angle.From2DPolarAngle(-1f, -1f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.Down, Angle.From2DPolarAngle(0f, -1f)!.Value.PolarOrientation);
		Assert.AreEqual(Orientation2D.DownRight, Angle.From2DPolarAngle(1f, -1f)!.Value.PolarOrientation);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		AssertToleranceEquals(0f, Angle.Interpolate(-100f, 100f, 0.5f), TestTolerance);
		AssertToleranceEquals(-100f, Angle.Interpolate(-100f, 100f, 0f), TestTolerance);
		AssertToleranceEquals(100f, Angle.Interpolate(-100f, 100f, 1f), TestTolerance);
		AssertToleranceEquals(-200f, Angle.Interpolate(-100f, 100f, -0.5f), TestTolerance);
		AssertToleranceEquals(200f, Angle.Interpolate(-100f, 100f, 1.5f), TestTolerance);

		AssertToleranceEquals(30f, Angle.Interpolate(30f, 30f, -1f), TestTolerance);
		AssertToleranceEquals(30f, Angle.Interpolate(30f, 30f, 0f), TestTolerance);
		AssertToleranceEquals(30f, Angle.Interpolate(30f, 30f, 0.5f), TestTolerance);
		AssertToleranceEquals(30f, Angle.Interpolate(30f, 30f, 1f), TestTolerance);
		AssertToleranceEquals(30f, Angle.Interpolate(30f, 30f, 2f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCreateNonBoundedRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = Angle.Random();
			Assert.GreaterOrEqual(val.AsDegrees, 0f);
			Assert.Less(val.AsDegrees, 360f);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateBoundedRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = Angle.Random(-720f, 720f);
			Assert.GreaterOrEqual(val.AsDegrees, -720f);
			Assert.Less(val.AsDegrees, 720f);
		}
	}
}