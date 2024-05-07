// Created on 2023-09-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class AngleTest {
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
			if (f < -1f || f > 1f) Assert.Throws<ArgumentOutOfRangeException>(() => Angle.FromSine(f));
			else Assert.AreEqual(Angle.FromRadians(MathF.Asin(f)), Angle.FromSine(f));
		}

		// cosine
		for (var f = -2f; f < 2.05f; f += 0.05f) {
			if (f < -1f || f > 1f) Assert.Throws<ArgumentOutOfRangeException>(() => Angle.FromCosine(f));
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

		Assert.Throws<ArgumentOutOfRangeException>(() => Angle.FromAngleBetweenDirections(Direction.None, Direction.Left));
		Assert.Throws<ArgumentOutOfRangeException>(() => Angle.FromAngleBetweenDirections(Direction.Left, Direction.None));
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
	}

	[Test]
	public void ShouldConsiderEquivalentAnglesEqualWhenNormalized() {
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.IsTrue(new Angle(360f + f).Equals(new Angle(f), TestTolerance, normalizeAngles: true));
			Assert.IsTrue(new Angle(720f + f).Equals(new Angle(f), TestTolerance, normalizeAngles: true));
			Assert.IsTrue(new Angle(-360f + f).Equals(new Angle(f), TestTolerance, normalizeAngles: true));
			Assert.IsTrue(new Angle(-720f + f).Equals(new Angle(f), TestTolerance, normalizeAngles: true));

			Assert.IsFalse(new Angle(360f + f).Equals(new Angle(f + 1f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(720f + f).Equals(new Angle(f + 1f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(-360f + f).Equals(new Angle(f + 1f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(-720f + f).Equals(new Angle(f + 1f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(360f + f).Equals(new Angle(f - 1f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(720f + f).Equals(new Angle(f - 1f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(-360f + f).Equals(new Angle(f - 1f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(-720f + f).Equals(new Angle(f - 1f), TestTolerance, normalizeAngles: true));

			Assert.IsFalse(new Angle(360f + f).Equals(new Angle(f + 180f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(720f + f).Equals(new Angle(f + 180f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(-360f + f).Equals(new Angle(f + 180f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(-720f + f).Equals(new Angle(f + 180f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(360f + f).Equals(new Angle(f - 180f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(720f + f).Equals(new Angle(f - 180f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(-360f + f).Equals(new Angle(f - 180f), TestTolerance, normalizeAngles: true));
			Assert.IsFalse(new Angle(-720f + f).Equals(new Angle(f - 180f), TestTolerance, normalizeAngles: true));
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
}