// Created on 2024-02-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class XYPairTest {
	const float TestTolerance = 0.001f;
	readonly XYPair<float> ThreeFourFloat = (3f, 4f);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyAssignConstructorParameters() {
		Assert.AreEqual(3f, ThreeFourFloat.X);
		Assert.AreEqual(4f, ThreeFourFloat.Y);
	}

	[Test]
	public void ShouldCorrectlyConvertToVector2() {
		Assert.AreEqual(3f, ThreeFourFloat.ToVector2().X);
		Assert.AreEqual(4f, ThreeFourFloat.ToVector2().Y);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<XYPair<float>>();
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<XYPair<int>>();
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<XYPair<byte>>();
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<XYPair<ushort>>();
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<XYPair<double>>();
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<XYPair<long>>();
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<XYPair<decimal>>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(XYPair<float>.Zero, ThreeFourFloat, -ThreeFourFloat);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(XYPair<int>.Zero, (3, 4), (-3, -4));
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(XYPair<float>.Zero, 0f, 0f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(ThreeFourFloat, 3f, 4f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(-ThreeFourFloat, -3f, -4f);
		ByteSpanSerializationTestUtils.AssertLittleEndianInt32s(XYPair<int>.Zero, 0, 0);
		ByteSpanSerializationTestUtils.AssertLittleEndianInt32s(new XYPair<int>(3, 4), 3, 4);
		ByteSpanSerializationTestUtils.AssertLittleEndianInt32s(-new XYPair<int>(3, 4), -3, -4);
	}

	[Test]
	public void ShouldCorrectlyConvertFromAngleAndLength() {
		for (var f = -720f; f <= 720f; f += 36f) {
			for (var l = 0f; l <= 3f; ++l) {
				var result = XYPair<float>.FromPolarAngleAndLength(f, l);
				if (l == 0f) {
					Assert.AreEqual(null, result.PolarAngle);
					Assert.AreEqual(0, result.ToVector2().Length());
				}
				else {
					Assert.IsTrue(Angle.FromDegrees(f).Equals(result.PolarAngle!.Value, normalizeAngles: true, tolerance: TestTolerance));
					Assert.AreEqual(l, result.ToVector2().Length(), TestTolerance);
				}
			}
		}

		foreach (var orientation in Enum.GetValues<Orientation2D>()) {
			for (var l = 0f; l <= 3f; ++l) {
				var result = XYPair<float>.FromOrientationAndLength(orientation, l);
				if (l == 0f || orientation == Orientation2D.None) {
					Assert.AreEqual(null, result.PolarAngle);
					Assert.AreEqual(0, result.ToVector2().Length());
				}
				else {
					Assert.IsTrue(orientation.ToPolarAngle()!.Value.Equals(result.PolarAngle!.Value, normalizeAngles: true, tolerance: TestTolerance));
					Assert.AreEqual(l, result.ToVector2().Length(), TestTolerance);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(XYPair<float> input, string expectedValue) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N1";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);

			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(XYPair<float>.Zero, "<0.0, 0.0>");
		AssertIteration(ThreeFourFloat, "<3.0, 4.0>");
		AssertIteration(new XYPair<float>(0.5f, -1.6f), "<0.5, -1.6>");
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(XYPair<float> input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			XYPair<float> input,
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

		var fractionalPair = new XYPair<float>(1.211f, -5.633f);

		AssertFail(XYPair<float>.Zero, Array.Empty<char>(), "N0", null);
		AssertFail(XYPair<float>.Zero, new char[5], "N0", null);
		AssertSuccess(XYPair<float>.Zero, new char[6], "N0", null, "<0, 0>");
		AssertFail(fractionalPair, new char[6], "N0", null);
		AssertSuccess(fractionalPair, new char[7], "N0", null, "<1, -6>");
		AssertFail(fractionalPair, new char[10], "N1", null);
		AssertSuccess(fractionalPair, new char[11], "N1", null, "<1.2, -5.6>");
		AssertSuccess(fractionalPair, new char[11], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "<1,2. -5,6>");
		AssertSuccess(fractionalPair, new char[16], "N3", null, "<1.211, -5.633>");
	}

	[Test]
	public void ShouldCorrectlyParseFromString() {
		var testCulture = CultureInfo.InvariantCulture;

		void AssertSuccess(string input, XYPair<float> expectedResult) {
			AssertToleranceEquals(expectedResult, XYPair<float>.Parse(input, testCulture), TestTolerance);
			AssertToleranceEquals(expectedResult, XYPair<float>.Parse(input.AsSpan(), testCulture), TestTolerance);
			Assert.IsTrue(XYPair<float>.TryParse(input, testCulture, out var parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
			Assert.IsTrue(XYPair<float>.TryParse(input.AsSpan(), testCulture, out parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
		}

		void AssertFail(string input) {
			Assert.Catch(() => XYPair<float>.Parse(input, testCulture));
			Assert.Catch(() => XYPair<float>.Parse(input.AsSpan(), testCulture));
			Assert.False(XYPair<float>.TryParse(input, testCulture, out _));
			Assert.False(XYPair<float>.TryParse(input.AsSpan(), testCulture, out _));
		}

		AssertSuccess("<1, 2>", new(1f, 2f));
		AssertSuccess("<1,2>", new(1f, 2f));
		AssertSuccess("<1.1, 2.2>", new(1.1f, 2.2f));
		AssertSuccess("<1,2>", new(1f, 2f));
		AssertSuccess("<-1.1, 2.2>", new(-1.1f, 2.2f));
		AssertFail("");
		AssertFail("<>");
		AssertFail("1, 2");
		AssertFail("<1, 2");
		AssertFail("1, 2>");
		AssertFail("<1 2>");
		AssertFail("<a, 1>");
		AssertFail("<, 1>");
		AssertFail("<1, c>");
		AssertFail("<1, ->");
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreEqual(XYPair<float>.Zero, -XYPair<float>.Zero);
		Assert.AreNotEqual(XYPair<float>.Zero, ThreeFourFloat);
		Assert.IsTrue(ThreeFourFloat.Equals(ThreeFourFloat));
		Assert.IsFalse(ThreeFourFloat.Equals(XYPair<float>.Zero));
		Assert.IsTrue(ThreeFourFloat == new XYPair<float>(3f, 4f));
		Assert.IsFalse(XYPair<float>.Zero == ThreeFourFloat);
		Assert.IsFalse(XYPair<float>.Zero != new XYPair<float>(0f, 0f));
		Assert.IsTrue(ThreeFourFloat != XYPair<float>.Zero);
		Assert.IsTrue(new XYPair<float>(1f, 2f) != new XYPair<float>(0f, 3f));
		Assert.IsTrue(new XYPair<float>(1f, 2f) != new XYPair<float>(1f, 3f));
		Assert.IsTrue(new XYPair<float>(1f, 2f) != new XYPair<float>(0f, 2f));

		Assert.IsTrue(XYPair<float>.Zero.Equals(XYPair<float>.Zero, 0f));
		Assert.IsTrue(ThreeFourFloat.Equals(ThreeFourFloat, 0f));
		Assert.IsTrue(new XYPair<float>(0.5f, 0.6f).Equals(new XYPair<float>(0.4f, 0.5f), 0.11f));
		Assert.IsFalse(new XYPair<float>(0.5f, 0.6f).Equals(new XYPair<float>(0.4f, 0.5f), 0.09f));
		Assert.IsTrue(new XYPair<float>(-0.5f, -0.5f).Equals(new XYPair<float>(-0.4f, -0.4f), 0.11f));
		Assert.IsFalse(new XYPair<float>(-0.5f, -0.5f).Equals(new XYPair<float>(-0.4f, -0.4f), 0.09f));
		Assert.IsFalse(new XYPair<float>(-0.5f, -0.5f).Equals(new XYPair<float>(0.4f, -0.4f), 0.11f));
	}
}