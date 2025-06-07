// Created on 2023-10-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class PercentageUtilsTest {
	const float TestTolerance = 0.001f;

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(float input, string expectedStringValuePart) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N0";
			var expectedValue = $"{expectedStringValuePart}{PercentageUtils.StringSuffix}";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(PercentageUtils.TryFormatFractionToPercentageString(input, formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);

			Assert.AreEqual(expectedValue, PercentageUtils.ConvertFractionToPercentageString(input, testFormat, testCulture));
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
		void AssertFail(float input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, PercentageUtils.TryFormatFractionToPercentageString(input, destination, out _, format, provider));
		}

		void AssertSuccess(
			float input,
			Span<char> destination,
			ReadOnlySpan<char> format,
			IFormatProvider? provider,
			ReadOnlySpan<char> expectedDestSpanValue
		) {
			var actualReturnValue = PercentageUtils.TryFormatFractionToPercentageString(input, destination, out var numCharsWritten, format, provider);
			Assert.AreEqual(true, actualReturnValue);
			Assert.AreEqual(expectedDestSpanValue.Length, numCharsWritten);
			Assert.IsTrue(
				expectedDestSpanValue.SequenceEqual(destination[..expectedDestSpanValue.Length]),
				$"Destination as string was {new String(destination)}"
			);
		}

		var testFraction = 0.12345f;

		AssertFail(0f, Array.Empty<char>(), "", null);
		AssertFail(0f, new char[1], "", null);
		AssertFail(0f, new char[2], "N1", null);
		AssertSuccess(0f, new char[3], "N0", null, "0" + PercentageUtils.StringSuffix);
		AssertSuccess(testFraction, new char[4], "N0", null, "12" + PercentageUtils.StringSuffix);
		AssertFail(testFraction, new char[3], "N1", null);
		AssertSuccess(testFraction, new char[6], "N1", null, "12.3" + PercentageUtils.StringSuffix);
		AssertSuccess(testFraction, new char[6], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "12,3" + PercentageUtils.StringSuffix);
		AssertSuccess(testFraction, new char[20], "N5", null, "12.34500" + PercentageUtils.StringSuffix);
	}

	[Test]
	public void ShouldCorrectlyParseFromString() {
		var testCulture = CultureInfo.InvariantCulture;

		void AssertSuccess(string input, float expectedResult) {
			Assert.AreEqual(expectedResult, PercentageUtils.ParsePercentageStringToFraction(input, testCulture), TestTolerance);
			Assert.AreEqual(expectedResult, PercentageUtils.ParsePercentageStringToFraction(input.AsSpan(), testCulture), TestTolerance);
			Assert.IsTrue(PercentageUtils.TryParsePercentageStringToFraction(input, testCulture, out var parseResult));
			Assert.AreEqual(expectedResult, parseResult, TestTolerance);
			Assert.IsTrue(PercentageUtils.TryParsePercentageStringToFraction(input.AsSpan(), testCulture, out parseResult));
			Assert.AreEqual(expectedResult, parseResult, TestTolerance);
		}

		void AssertFailure(string input) {
			Assert.Catch(() => PercentageUtils.ParsePercentageStringToFraction(input, testCulture));
			Assert.Catch(() => PercentageUtils.ParsePercentageStringToFraction(input.AsSpan(), testCulture));
			Assert.False(PercentageUtils.TryParsePercentageStringToFraction(input, testCulture, out _));
			Assert.False(PercentageUtils.TryParsePercentageStringToFraction(input.AsSpan(), testCulture, out _));
		}

		AssertSuccess("180", 1.8f);
		AssertSuccess("180.000", 1.8f);
		AssertSuccess("180" + PercentageUtils.StringSuffix, 1.8f);
		AssertSuccess("180 " + PercentageUtils.StringSuffix, 1.8f);
		AssertSuccess("-180", -1.8f);
		AssertSuccess("-180.000", -1.8f);
		AssertSuccess("-180" + PercentageUtils.StringSuffix, -1.8f);
		AssertSuccess("123.456", 1.23456f);
		AssertSuccess("-123.456" + PercentageUtils.StringSuffix, -1.23456f);

		AssertFailure("");
		AssertFailure("abc");
		AssertFailure(PercentageUtils.StringSuffix);
		AssertFailure(PercentageUtils.StringSuffix + "123");
	}
}