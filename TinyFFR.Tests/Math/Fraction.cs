// Created on 2023-10-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class FractionTest {
	const float TestTolerance = 0.001f;

	[Test]
	public void StaticReadonlyMembersShouldBeCorrectlyInitialized() {
		Assert.AreEqual(new Fraction(0f), Fraction.Zero);
		Assert.AreEqual(new Fraction(1f), Fraction.Full);
		Assert.AreEqual(new Fraction(-1f), Fraction.FullNegative);
	}

	[Test]
	public void PropertiesShouldCorrectlyConvertToAndFromDecimal() {
		Assert.AreEqual(0f, Fraction.Zero.AsDecimal);
		Assert.AreEqual(0f, Fraction.Zero.AsPercentage);
		Assert.AreEqual(1f, Fraction.Full.AsDecimal);
		Assert.AreEqual(100f, Fraction.Full.AsPercentage);
		Assert.AreEqual(-1f, Fraction.FullNegative.AsDecimal);
		Assert.AreEqual(-100f, Fraction.FullNegative.AsPercentage);

		Assert.AreEqual(50f, Fraction.FromDecimal(0.5f).AsPercentage);
		Assert.AreEqual(-50f, Fraction.FromDecimal(-0.5f).AsPercentage);
	}

	[Test]
	public void ConstructorsAndConversionsShouldCorrectlyInitializeValue() {
		Assert.AreEqual(0.3f, new Fraction(0.3f).AsDecimal);
		Assert.AreEqual(-0.3f, new Fraction(-0.3f).AsDecimal);

		Assert.AreEqual(0.3f, ((Fraction) 0.3f).AsDecimal);
		Assert.AreEqual(-0.3f, ((Fraction) (-0.3f)).AsDecimal);

		Assert.AreEqual((Fraction) 0.3f, new Fraction(0.3f));
		Assert.AreEqual((Fraction) (-0.3f), new Fraction(-0.3f));
	}

	[Test]
	public void BasicFactoryMethodsShouldCorrectlyInitializeValue() {
		Assert.AreEqual(0.3f, Fraction.FromDecimal(0.3f).AsDecimal, TestTolerance);
		Assert.AreEqual(0.3f, Fraction.FromPercentage(30f).AsDecimal, TestTolerance);
		Assert.AreEqual(0.3f, Fraction.FromRatio(36f, 120f).AsDecimal, TestTolerance);

		Assert.AreEqual(-3f, Fraction.FromDecimal(-3f).AsDecimal, TestTolerance);
		Assert.AreEqual(-3f, Fraction.FromPercentage(-300f).AsDecimal, TestTolerance);
		Assert.AreEqual(-3f, Fraction.FromRatio(120f, -40f).AsDecimal, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		void AssertIteration(Fraction input) {
			var span = Fraction.ConvertToSpan(input);
			Assert.AreEqual(1, span.Length);
			Assert.AreEqual(input.AsDecimal, span[0]);
			Assert.AreEqual(input, Fraction.ConvertFromSpan(span));
		}

		for (var f = -2f; f < 2.05f; f += 0.05f) AssertIteration(f);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(Fraction input, string expectedStringValuePart) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N0";
			var expectedValue = $"{expectedStringValuePart}{Fraction.StringSuffix}";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);

			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(0f, "0");
		AssertIteration(0.5f, "50");
		AssertIteration(1f, "100");
		AssertIteration(0.123f, "12");
		AssertIteration(10f, "1,000");
		AssertIteration(-0.5f, "-50");
		AssertIteration(-1f, "-100");
		AssertIteration(-10f, "-1,000");
		AssertIteration(-0.123f, "-12");
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(Fraction input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			Fraction input,
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

		var testFraction = Fraction.FromPercentage(12.345f);

		AssertFail(Fraction.Zero, Array.Empty<char>(), "", null);
		AssertFail(Fraction.Zero, new char[1], "", null);
		AssertFail(Fraction.Zero, new char[2], "N0", null);
		AssertSuccess(Fraction.Zero, new char[3], "N0", null, "0" + Fraction.StringSuffix);
		AssertFail(testFraction, new char[3], "N0", null);
		AssertSuccess(testFraction, new char[4], "N0", null, "12" + Fraction.StringSuffix);
		AssertFail(testFraction, new char[5], "N1", null);
		AssertSuccess(testFraction, new char[6], "N1", null, "12.3" + Fraction.StringSuffix);
		AssertSuccess(testFraction, new char[6], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "12,3" + Fraction.StringSuffix);
		AssertSuccess(testFraction, new char[20], "N5", null, "12.34500" + Fraction.StringSuffix);
	}

	[Test]
	public void ShouldCorrectlyParseFromString() {
		void AssertIteration(string input, Fraction expectedResult) {
			var testCulture = CultureInfo.InvariantCulture;

			AssertToleranceEquals(expectedResult, Fraction.Parse(input, testCulture), TestTolerance);
			AssertToleranceEquals(expectedResult, Fraction.Parse(input.AsSpan(), testCulture), TestTolerance);
			Assert.IsTrue(Fraction.TryParse(input, testCulture, out var parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
			Assert.IsTrue(Fraction.TryParse(input.AsSpan(), testCulture, out parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
		}

		AssertIteration("180", Fraction.FromPercentage(180f));
		AssertIteration("180.000", Fraction.FromPercentage(180f));
		AssertIteration("180" + Fraction.StringSuffix, Fraction.FromPercentage(180f));
		AssertIteration("-180", -Fraction.FromPercentage(180f));
		AssertIteration("-180.000", -Fraction.FromPercentage(180f));
		AssertIteration("-180" + Fraction.StringSuffix, -Fraction.FromPercentage(180f));
		AssertIteration("123.456", Fraction.FromPercentage(123.456f));
		AssertIteration("-123.456" + Fraction.StringSuffix, Fraction.FromPercentage(-123.456f));
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreEqual(Fraction.Zero, -Fraction.Zero);
		Assert.AreNotEqual(Fraction.Zero, new Fraction(0.1f));
		Assert.IsTrue(Fraction.Full.Equals(Fraction.Full));
		Assert.IsFalse(Fraction.Full.Equals(Fraction.FullNegative));
		Assert.IsTrue(Fraction.Full == 1f);
		Assert.IsFalse(Fraction.Full == Fraction.FullNegative);
		Assert.IsFalse(Fraction.FullNegative != -1f);
		Assert.IsTrue(Fraction.Full != Fraction.FullNegative);

		Assert.IsTrue(Fraction.Zero.Equals(Fraction.Zero, 0f));
		Assert.IsTrue(Fraction.Full.Equals(Fraction.Full, 0f));
		Assert.IsTrue(new Fraction(0.5f).Equals(new Fraction(0.4f), 0.11f));
		Assert.IsFalse(new Fraction(0.5f).Equals(new Fraction(0.4f), 0.09f));
		Assert.IsTrue(new Fraction(-0.5f).Equals(new Fraction(-0.4f), 0.11f));
		Assert.IsFalse(new Fraction(-0.5f).Equals(new Fraction(-0.4f), 0.09f));
		Assert.IsFalse(new Fraction(-0.5f).Equals(new Fraction(0.4f), 0.11f));
	}
}