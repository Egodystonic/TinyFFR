// Created on 2023-10-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class VectExtensionsTest {
	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(VectTestStubType input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			VectTestStubType input,
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
			Assert.AreEqual(new String(expectedDestSpanValue), input.ToString(new String(format), provider));
		}

		var testVect = new VectTestStubType { X = 1.123f, Y = 2.123f, Z = -3.123f };

		AssertFail(testVect, Array.Empty<char>(), "", null);
		AssertFail(testVect, Array.Empty<char>(), "", null);
		AssertFail(testVect, new char[9], "N0", CultureInfo.InvariantCulture);
		AssertSuccess(testVect, new char[10], "N0", CultureInfo.InvariantCulture, "<1, 2, -3>");
		AssertFail(testVect, new char[15], "N1", CultureInfo.InvariantCulture);
		AssertSuccess(testVect, new char[16], "N1", CultureInfo.InvariantCulture, "<1.1, 2.1, -3.1>");
		AssertSuccess(testVect, new char[16], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "<1,1. 2,1. -3,1>");
	}
}