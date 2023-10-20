// Created on 2023-09-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class AngleTest {
	const float TestTolerance = 0.001f;

	[Test]
	public void StaticReadonlyMembersShouldBeCorrectlyInitialized() {
		AssertToleranceEquals(Angle.FromRadians(0f), Angle.Zero, 0f);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI * 0.5f), Angle.QuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI), Angle.HalfCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI * 1.5f), Angle.ThreeQuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI * 2f), Angle.FullCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI * 0.5f), -Angle.QuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI), -Angle.HalfCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI * 1.5f), -Angle.ThreeQuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI * 2f), -Angle.FullCircle, TestTolerance);
	}

	[Test]
	public void PropertiesShouldCorrectlyConvertToAndFromRadians() {
		Assert.AreEqual(0f, Angle.Zero.Radians, 0f);
		Assert.AreEqual(0f, Angle.Zero.Degrees, 0f);
		Assert.AreEqual(0f, Angle.Zero.FullCircleFraction, 0f);

		Assert.AreEqual(MathF.PI * 0.5f, Angle.QuarterCircle.Radians, 0f);
		Assert.AreEqual(90f, Angle.QuarterCircle.Degrees, TestTolerance);
		Assert.AreEqual(0.25f, Angle.QuarterCircle.FullCircleFraction, TestTolerance);
		Assert.AreEqual(-MathF.PI * 0.5f, -Angle.QuarterCircle.Radians, 0f);
		Assert.AreEqual(-90f, -Angle.QuarterCircle.Degrees, TestTolerance);
		Assert.AreEqual(-0.25f, -Angle.QuarterCircle.FullCircleFraction, TestTolerance);

		Assert.AreEqual(MathF.PI, Angle.HalfCircle.Radians, 0f);
		Assert.AreEqual(180f, Angle.HalfCircle.Degrees, TestTolerance);
		Assert.AreEqual(0.5f, Angle.HalfCircle.FullCircleFraction, TestTolerance);
		Assert.AreEqual(-MathF.PI, -Angle.HalfCircle.Radians, 0f);
		Assert.AreEqual(-180f, -Angle.HalfCircle.Degrees, TestTolerance);
		Assert.AreEqual(-0.5f, -Angle.HalfCircle.FullCircleFraction, TestTolerance);

		Assert.AreEqual(MathF.PI * 1.5f, Angle.ThreeQuarterCircle.Radians, 0f);
		Assert.AreEqual(270f, Angle.ThreeQuarterCircle.Degrees, TestTolerance);
		Assert.AreEqual(0.75f, Angle.ThreeQuarterCircle.FullCircleFraction, TestTolerance);
		Assert.AreEqual(-MathF.PI * 1.5f, -Angle.ThreeQuarterCircle.Radians, 0f);
		Assert.AreEqual(-270f, -Angle.ThreeQuarterCircle.Degrees, TestTolerance);
		Assert.AreEqual(-0.75f, -Angle.ThreeQuarterCircle.FullCircleFraction, TestTolerance);

		Assert.AreEqual(MathF.PI * 2f, Angle.FullCircle.Radians, 0f);
		Assert.AreEqual(360f, Angle.FullCircle.Degrees, TestTolerance);
		Assert.AreEqual(1f, Angle.FullCircle.FullCircleFraction, TestTolerance);
		Assert.AreEqual(-MathF.PI * 2f, -Angle.FullCircle.Radians, 0f);
		Assert.AreEqual(-360f, -Angle.FullCircle.Degrees, TestTolerance);
		Assert.AreEqual(-1f, -Angle.FullCircle.FullCircleFraction, TestTolerance);

		for (var f = -720f; f <= 720f + 36f; f += 36f) {
			Assert.AreEqual((MathF.Tau / 360f) * f, new Angle(f).Radians, TestTolerance);
			Assert.AreEqual(f, new Angle(f).Degrees, TestTolerance);
			Assert.AreEqual(f / 360f, new Angle(f).FullCircleFraction, TestTolerance);
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
			Assert.AreEqual(f, Angle.FromRadians(f).Radians);
		}

		// degrees
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.AreEqual(f, Angle.FromDegrees(f).Degrees, TestTolerance);
		}

		// circle fraction
		for (var f = -2f; f < 2.05f; f += 0.05f) {
			Assert.AreEqual(f, Angle.FromFullCircleFraction(f).FullCircleFraction, TestTolerance);
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

		var rot45AroundUp = Rotation.FromAngleAroundAxis(45f, Direction.Up);
		var rotNeg45AroundUp = Rotation.FromAngleAroundAxis(-45f, Direction.Up);

		AssertToleranceEquals(new Angle(45f), Angle.FromAngleBetweenDirections(Direction.Forward * rot45AroundUp, Direction.Forward), TestTolerance);
		AssertToleranceEquals(new Angle(90f), Angle.FromAngleBetweenDirections(Direction.Forward * rot45AroundUp, Direction.Forward * rotNeg45AroundUp), TestTolerance);

		// One using Wolfram Alpha as a sanity check (randomish vectors): https://www.wolframalpha.com/input?i=angle+between+%280.251398%2C+-0.967884%2C+0%29+and+%280.733632%2C+0.507899%2C+-0.451466%29
		var d1 = new Direction(0.733632f, 0.507899f, -0.451466f);
		var d2 = new Direction(0.251398f, -0.967884f, 0f);
		AssertToleranceEquals(new Angle(107.888f), Angle.FromAngleBetweenDirections(d1, d2), TestTolerance);
		AssertToleranceEquals(new Angle(107.888f), Angle.FromAngleBetweenDirections(d2, d1), TestTolerance);

		// Check that the order of arguments makes no difference
		for (var i = 0; i < Direction.AllCardinals.Count; ++i) {
			for (var j = i; j < Direction.AllCardinals.Count; ++j) {
				d1 = Direction.AllCardinals.ElementAt(i);
				d2 = Direction.AllCardinals.ElementAt(j);
				Assert.AreEqual(Angle.FromAngleBetweenDirections(d1, d2), Angle.FromAngleBetweenDirections(d2, d1));
			}
		}

		Assert.Throws<ArgumentOutOfRangeException>(() => Angle.FromAngleBetweenDirections(Direction.None, Direction.Left));
		Assert.Throws<ArgumentOutOfRangeException>(() => Angle.FromAngleBetweenDirections(Direction.Left, Direction.None));
		Assert.Throws<ArgumentOutOfRangeException>(() => Angle.FromAngleBetweenDirections(Direction.FromVector3PreNormalized(1f, 1f, 1f), Direction.Left));
		Assert.Throws<ArgumentOutOfRangeException>(() => Angle.FromAngleBetweenDirections(Direction.Left, Direction.FromVector3PreNormalized(1f, 1f, 1f)));
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		void AssertIteration(Angle input) {
			var span = Angle.ConvertToSpan(input);
			Assert.AreEqual(1, span.Length);
			Assert.AreEqual(input.Radians, span[0]);
			Assert.AreEqual(input, Angle.ConvertFromSpan(span));
		}

		for (var f = -2f; f < 2.05f; f += 0.05f) AssertIteration(Angle.FromFullCircleFraction(f));

		var noneSpan = Angle.ConvertToSpan(Angle.Zero);
		var quarterSpan = Angle.ConvertToSpan(Angle.QuarterCircle);
		var halfSpan = Angle.ConvertToSpan(Angle.HalfCircle);
		var threeQuarterSpan = Angle.ConvertToSpan(Angle.ThreeQuarterCircle);
		var fullSpan = Angle.ConvertToSpan(Angle.FullCircle);
		
		Assert.AreEqual(0f, noneSpan[0]);
		Assert.AreEqual(MathF.PI * 0.5f, quarterSpan[0]);
		Assert.AreEqual(MathF.PI, halfSpan[0]);
		Assert.AreEqual(MathF.PI * 1.5f, threeQuarterSpan[0]);
		Assert.AreEqual(MathF.PI * 2f, fullSpan[0]);

		Assert.AreEqual(Angle.Zero, Angle.ConvertFromSpan(new ReadOnlySpan<float>(0f)));
		Assert.AreEqual(Angle.QuarterCircle, Angle.ConvertFromSpan(new ReadOnlySpan<float>(MathF.PI * 0.5f)));
		Assert.AreEqual(Angle.HalfCircle, Angle.ConvertFromSpan(new ReadOnlySpan<float>(MathF.PI)));
		Assert.AreEqual(Angle.ThreeQuarterCircle, Angle.ConvertFromSpan(new ReadOnlySpan<float>(MathF.PI * 1.5f)));
		Assert.AreEqual(Angle.FullCircle, Angle.ConvertFromSpan(new ReadOnlySpan<float>(MathF.PI * 2f)));
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
}