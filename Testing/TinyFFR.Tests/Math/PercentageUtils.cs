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

	[Test]
	public void ShouldCorrectlyConvertVectorsToString() {
		var testCulture = CultureInfo.InvariantCulture;
		var testFormat = "N0";

		void AssertPairIteration(XYPair<float> input, string expectedValue) {
			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(PercentageUtils.TryFormatFractionToPercentageString(input, formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(expectedValue.Length, charsWritten);

			Assert.AreEqual(expectedValue, PercentageUtils.ConvertFractionToPercentageString(input, testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		void AssertVectIteration<TVect>(TVect input, string expectedValue) where TVect : IVect {
			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(PercentageUtils.TryFormatFractionToPercentageString(input, formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(expectedValue.Length, charsWritten);

			Assert.AreEqual(expectedValue, PercentageUtils.ConvertFractionToPercentageString(input, testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertPairIteration(new XYPair<float>(0f, 0f), "<0%, 0%>");
		AssertPairIteration(new XYPair<float>(0.5f, 1f), "<50%, 100%>");
		AssertPairIteration(new XYPair<float>(-0.5f, -1f), "<-50%, -100%>");
		AssertPairIteration(new XYPair<float>(10f, -10f), "<1,000%, -1,000%>");
		AssertPairIteration(new XYPair<float>(0.123f, 0.5f), "<12%, 50%>");

		AssertVectIteration(new Vect(0f, 0f, 0f), "<0%, 0%, 0%>");
		AssertVectIteration(new Vect(0.5f, 1f, -0.5f), "<50%, 100%, -50%>");
		AssertVectIteration(new Vect(10f, -10f, 0.123f), "<1,000%, -1,000%, 12%>");
		AssertVectIteration(new Location(0.5f, 1f, -0.5f), "<50%, 100%, -50%>");
		AssertVectIteration(Direction.Left, "<100%, 0%, 0%>");

		var germanCulture = CultureInfo.CreateSpecificCulture("de-DE");
		var germanSeparator = NumberFormatInfo.GetInstance(germanCulture).NumberGroupSeparator;
		Assert.AreEqual(
			$"<12,3%{germanSeparator} 50,0%>",
			PercentageUtils.ConvertFractionToPercentageString(new XYPair<float>(0.123f, 0.5f), "N1", germanCulture)
		);
		Assert.AreEqual(
			$"<12,3%{germanSeparator} 50,0%{germanSeparator} -100,0%>",
			PercentageUtils.ConvertFractionToPercentageString(new Vect(0.123f, 0.5f, -1f), "N1", germanCulture)
		);
	}

	[Test]
	public void ShouldCorrectlyFormatVectorsToString() {
		var testCulture = CultureInfo.InvariantCulture;

		void AssertPairFail(XYPair<float> input, int destinationLength) {
			Assert.AreEqual(false, PercentageUtils.TryFormatFractionToPercentageString(input, new char[destinationLength], out _, "N0", testCulture));
		}

		void AssertPairSuccess(XYPair<float> input, int destinationLength, string expectedDestSpanValue) {
			var destination = new char[destinationLength];
			Assert.AreEqual(true, PercentageUtils.TryFormatFractionToPercentageString(input, destination, out var numCharsWritten, "N0", testCulture));
			Assert.AreEqual(expectedDestSpanValue.Length, numCharsWritten);
			Assert.AreEqual(expectedDestSpanValue, new String(destination[..expectedDestSpanValue.Length]));
		}

		void AssertVectFail<TVect>(TVect input, int destinationLength) where TVect : IVect {
			Assert.AreEqual(false, PercentageUtils.TryFormatFractionToPercentageString(input, new char[destinationLength], out _, "N0", testCulture));
		}

		void AssertVectSuccess<TVect>(TVect input, int destinationLength, string expectedDestSpanValue) where TVect : IVect {
			var destination = new char[destinationLength];
			Assert.AreEqual(true, PercentageUtils.TryFormatFractionToPercentageString(input, destination, out var numCharsWritten, "N0", testCulture));
			Assert.AreEqual(expectedDestSpanValue.Length, numCharsWritten);
			Assert.AreEqual(expectedDestSpanValue, new String(destination[..expectedDestSpanValue.Length]));
		}

		var testPair = new XYPair<float>(0.5f, 1f);
		AssertPairFail(testPair, 0);
		AssertPairFail(testPair, 1);
		AssertPairFail(testPair, 3);
		AssertPairFail(testPair, 4);
		AssertPairFail(testPair, 5);
		AssertPairFail(testPair, 6);
		AssertPairFail(testPair, 10);
		AssertPairSuccess(testPair, 11, "<50%, 100%>");
		AssertPairSuccess(testPair, 20, "<50%, 100%>");

		var testVect = new Vect(0.5f, 1f, -0.5f);
		AssertVectFail(testVect, 0);
		AssertVectFail(testVect, 1);
		AssertVectFail(testVect, 4);
		AssertVectFail(testVect, 6);
		AssertVectFail(testVect, 10);
		AssertVectFail(testVect, 12);
		AssertVectFail(testVect, 16);
		AssertVectSuccess(testVect, 17, "<50%, 100%, -50%>");
		AssertVectSuccess(testVect, 30, "<50%, 100%, -50%>");
	}

	[Test]
	public void ShouldCorrectlyParseVectorsFromString() {
		var testCulture = CultureInfo.InvariantCulture;

		void AssertPairSuccess(string input, XYPair<float> expectedResult) {
			Assert.IsTrue(expectedResult.Equals(PercentageUtils.ParsePercentageStringToFractionXYPair(input, testCulture), TestTolerance));
			Assert.IsTrue(expectedResult.Equals(PercentageUtils.ParsePercentageStringToFractionXYPair(input.AsSpan(), testCulture), TestTolerance));
			Assert.IsTrue(PercentageUtils.TryParsePercentageStringToFractionXYPair(input, testCulture, out var parseResult));
			Assert.IsTrue(expectedResult.Equals(parseResult, TestTolerance));
			Assert.IsTrue(PercentageUtils.TryParsePercentageStringToFractionXYPair(input.AsSpan(), testCulture, out parseResult));
			Assert.IsTrue(expectedResult.Equals(parseResult, TestTolerance));
		}

		void AssertVectSuccess<TVect>(string input, TVect expectedResult) where TVect : IVect<TVect> {
			Assert.IsTrue(expectedResult.Equals(PercentageUtils.ParsePercentageStringToFractionVect<TVect>(input, testCulture), TestTolerance));
			Assert.IsTrue(expectedResult.Equals(PercentageUtils.ParsePercentageStringToFractionVect<TVect>(input.AsSpan(), testCulture), TestTolerance));
			Assert.IsTrue(PercentageUtils.TryParsePercentageStringToFractionVect<TVect>(input, testCulture, out var parseResult));
			Assert.IsTrue(expectedResult.Equals(parseResult, TestTolerance));
			Assert.IsTrue(PercentageUtils.TryParsePercentageStringToFractionVect<TVect>(input.AsSpan(), testCulture, out parseResult));
			Assert.IsTrue(expectedResult.Equals(parseResult, TestTolerance));
		}

		void AssertPairFailure(string input) {
			Assert.Catch(() => PercentageUtils.ParsePercentageStringToFractionXYPair(input, testCulture));
			Assert.Catch(() => _ = PercentageUtils.ParsePercentageStringToFractionXYPair(input.AsSpan(), testCulture));
			AssertPairTryFailure(input);
		}

		void AssertVectFailure(string input) {
			Assert.Catch(() => PercentageUtils.ParsePercentageStringToFractionVect<Vect>(input, testCulture));
			Assert.Catch(() => _ = PercentageUtils.ParsePercentageStringToFractionVect<Vect>(input.AsSpan(), testCulture));
			AssertVectTryFailure(input);
		}

		void AssertPairTryFailure(string input) {
			Assert.False(PercentageUtils.TryParsePercentageStringToFractionXYPair(input, testCulture, out _));
			Assert.False(PercentageUtils.TryParsePercentageStringToFractionXYPair(input.AsSpan(), testCulture, out _));
		}

		void AssertVectTryFailure(string input) {
			Assert.False(PercentageUtils.TryParsePercentageStringToFractionVect<Vect>(input, testCulture, out _));
			Assert.False(PercentageUtils.TryParsePercentageStringToFractionVect<Vect>(input.AsSpan(), testCulture, out _));
		}

		AssertPairSuccess("<50%, 100%>", new XYPair<float>(0.5f, 1f));
		AssertPairSuccess("<50%,100%>", new XYPair<float>(0.5f, 1f));
		AssertPairSuccess("<50, 100>", new XYPair<float>(0.5f, 1f));
		AssertPairSuccess("<-50%, -100%>", new XYPair<float>(-0.5f, -1f));
		AssertPairSuccess("<0%, 0%>", new XYPair<float>(0f, 0f));
		AssertPairSuccess("<1,000%, -1,000%>", new XYPair<float>(10f, -10f));
		AssertPairSuccess("<123.456%, -123.456%>", new XYPair<float>(1.23456f, -1.23456f));

		AssertVectSuccess("<50%, 100%, -50%>", new Vect(0.5f, 1f, -0.5f));
		AssertVectSuccess("<50, 100, -50>", new Vect(0.5f, 1f, -0.5f));
		AssertVectSuccess("<1,000%, -1,000%, 0%>", new Vect(10f, -10f, 0f));
		AssertVectSuccess("<50%, 100%, -50%>", new Location(0.5f, 1f, -0.5f));
		AssertVectSuccess("<100%, 0%, 0%>", Direction.Left);
		AssertVectSuccess("<50%, 0%, 0%>", Direction.Left);

		AssertPairFailure("");
		AssertPairFailure("<>");
		AssertPairFailure("<50%>");
		AssertPairFailure("<abc, def>");
		AssertPairFailure("<50%, >");
		AssertPairTryFailure("50%, 100%");
		AssertPairTryFailure("<50%, 100%");
		AssertPairTryFailure("50%, 100%>");
		AssertPairTryFailure("<50%, 100%, 150%>");

		AssertVectFailure("");
		AssertVectFailure("<>");
		AssertVectFailure("<50%, 100%>");
		AssertVectFailure("<abc, def, ghi>");
		AssertVectTryFailure("50%, 100%, 150%");
		AssertVectTryFailure("<50%, 100%, 150%, 200%>");

		var germanCulture = CultureInfo.CreateSpecificCulture("de-DE");
		var germanSeparator = NumberFormatInfo.GetInstance(germanCulture).NumberGroupSeparator;
		Assert.IsTrue(
			new XYPair<float>(0.123f, 0.5f).Equals(
				PercentageUtils.ParsePercentageStringToFractionXYPair($"<12,3%{germanSeparator} 50,0%>", germanCulture),
				TestTolerance
			)
		);
		Assert.IsTrue(
			new Vect(0.123f, 0.5f, -1f).Equals(
				PercentageUtils.ParsePercentageStringToFractionVect<Vect>($"<12,3%{germanSeparator} 50,0%{germanSeparator} -100,0%>", germanCulture),
				TestTolerance
			)
		);

		void AssertPairRoundTrip(XYPair<float> input, string? format, IFormatProvider? provider) {
			var asString = PercentageUtils.ConvertFractionToPercentageString(input, format, provider);
			Assert.IsTrue(input.Equals(PercentageUtils.ParsePercentageStringToFractionXYPair(asString, provider), TestTolerance), asString);
			Assert.IsTrue(PercentageUtils.TryParsePercentageStringToFractionXYPair(asString, provider, out var parseResult), asString);
			Assert.IsTrue(input.Equals(parseResult, TestTolerance), asString);
		}

		void AssertVectRoundTrip<TVect>(TVect input, string? format, IFormatProvider? provider) where TVect : IVect<TVect> {
			var asString = PercentageUtils.ConvertFractionToPercentageString(input, format, provider);
			Assert.IsTrue(input.Equals(PercentageUtils.ParsePercentageStringToFractionVect<TVect>(asString, provider), TestTolerance), asString);
			Assert.IsTrue(PercentageUtils.TryParsePercentageStringToFractionVect<TVect>(asString, provider, out var parseResult), asString);
			Assert.IsTrue(input.Equals(parseResult, TestTolerance), asString);
		}

		foreach (var culture in new[] { CultureInfo.InvariantCulture, germanCulture }) {
			AssertPairRoundTrip(new XYPair<float>(0.5f, 1f), "N3", culture);
			AssertPairRoundTrip(new XYPair<float>(-0.5f, 10f), "N3", culture);
			AssertVectRoundTrip(new Vect(0.5f, 1f, -0.5f), "N3", culture);
			AssertVectRoundTrip(new Vect(-10f, 10f, 0.123f), "N3", culture);
			AssertVectRoundTrip(new Location(0.5f, 1f, -0.5f), "N3", culture);
			AssertVectRoundTrip(Direction.Left, "N3", culture);
		}
	}
}