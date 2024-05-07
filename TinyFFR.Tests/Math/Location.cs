// Created on 2023-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class LocationTest {
	const float TestTolerance = 0.001f;
	static readonly Location OneTwoNegThree = new(1f, 2f, -3f);

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<Location>();

	[Test]
	public void ShouldCorrectlyInitializeStaticReadonlyMembers() {
		Assert.AreEqual(new Location(0f, 0f, 0f), Location.Origin);
	}

	[Test]
	public void ShouldCorrectlyImplementProperties() {
		Assert.AreEqual(1f, OneTwoNegThree.X);
		Assert.AreEqual(2f, OneTwoNegThree.Y);
		Assert.AreEqual(-3f, OneTwoNegThree.Z);

		Assert.AreEqual(1.5f, (OneTwoNegThree with { X = 1.5f }).X);
		Assert.AreEqual(2.5f, (OneTwoNegThree with { Y = 2.5f }).Y);
		Assert.AreEqual(-3.5f, (OneTwoNegThree with { Z = -3.5f }).Z);

		Assert.AreEqual(new Location(4f, 5f, -6f), OneTwoNegThree with { X = 4f, Y = 5f, Z = -6f });
	}

	[Test]
	public void ConstructorsShouldCorrectlyConstruct() {
		Assert.AreEqual(Location.Origin, new Location());
		Assert.AreEqual(Location.WValue, new Location().AsVector4.W);

		Assert.AreEqual(new Location(new Vector4(0.1f, 0.2f, 0.3f, Location.WValue)), new Location(0.1f, 0.2f, 0.3f));
		Assert.AreEqual(Location.WValue, new Location(0.1f, 0.2f, 0.3f).AsVector4.W);
	}

	[Test]
	public void StaticFactoryMethodsShouldCorrectlyConstruct() {
		Assert.AreEqual(new Location(-1.2f, 2.4f, 0f), Location.FromVector3(new(-1.2f, 2.4f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyConvertToVector3() {
		Assert.AreEqual(new Vector3(1f, 2f, -3f), OneTwoNegThree.ToVector3());
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromValueTuple() {
		Assert.AreEqual(OneTwoNegThree, (Location) (1, 2, -3));
		var (x, y, z) = OneTwoNegThree;
		Assert.AreEqual(1f, x);
		Assert.AreEqual(2f, y);
		Assert.AreEqual(-3f, z);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength(OneTwoNegThree);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(Location.Origin, OneTwoNegThree, new(-0.001f, 0f, 100000f));
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(Location.Origin, 0f, 0f, 0f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(OneTwoNegThree, 1f, 2f, -3f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(new Location(-0.001f, 0f, 100000f), -0.001f, 0f, 100000f);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(Location input, string expectedValue) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N1";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);

			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(Location.Origin, "<0.0, 0.0, 0.0>");
		AssertIteration(OneTwoNegThree, "<1.0, 2.0, -3.0>");
		AssertIteration(new Location(0.5f, 0f, -1.6f), "<0.5, 0.0, -1.6>");
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(Location input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			Location input,
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

		var fractionalVect = new Location(1.211f, 3.422f, -5.633f);

		AssertFail(Location.Origin, Array.Empty<char>(), "N0", null);
		AssertFail(Location.Origin, new char[8], "N0", null);
		AssertSuccess(Location.Origin, new char[9], "N0", null, "<0, 0, 0>");
		AssertFail(fractionalVect, new char[8], "N0", null);
		AssertSuccess(fractionalVect, new char[10], "N0", null, "<1, 3, -6>");
		AssertFail(fractionalVect, new char[10], "N1", null);
		AssertSuccess(fractionalVect, new char[16], "N1", null, "<1.2, 3.4, -5.6>");
		AssertSuccess(fractionalVect, new char[16], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "<1,2. 3,4. -5,6>");
		AssertSuccess(fractionalVect, new char[22], "N3", null, "<1.211, 3.422, -5.633>");
	}

	[Test]
	public void ShouldCorrectlyParseFromString() {
		var testCulture = CultureInfo.InvariantCulture;

		void AssertSuccess(string input, Location expectedResult) {
			AssertToleranceEquals(expectedResult, Location.Parse(input, testCulture), TestTolerance);
			AssertToleranceEquals(expectedResult, Location.Parse(input.AsSpan(), testCulture), TestTolerance);
			Assert.IsTrue(Location.TryParse(input, testCulture, out var parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
			Assert.IsTrue(Location.TryParse(input.AsSpan(), testCulture, out parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
		}

		void AssertFail(string input) {
			Assert.Catch(() => Location.Parse(input, testCulture));
			Assert.Catch(() => Location.Parse(input.AsSpan(), testCulture));
			Assert.False(Location.TryParse(input, testCulture, out _));
			Assert.False(Location.TryParse(input.AsSpan(), testCulture, out _));
		}

		AssertFail("");
		AssertFail("<>");
		AssertFail("1, 2, 3");
		AssertFail("<1, 2, 3");
		AssertFail("1, 2, 3>");
		AssertFail("<1, 2>");
		AssertFail("<1, 2,>");
		AssertFail("<1, 2, >");
		AssertFail("<1 2 3>");
		AssertFail("<a, 1, 2>");
		AssertFail("<, 1, 2>");
		AssertFail("<1, c, 2>");
		AssertFail("<1, 2, ->");
		AssertSuccess("<1, 2, 3>", new(1f, 2f, 3f));
		AssertSuccess("<1,2,3>", new(1f, 2f, 3f));
		AssertSuccess("<1.1, 2.2, 3.3>", new(1.1f, 2.2f, 3.3f));
		AssertSuccess("<1,2,3>", new(1f, 2f, 3f));
		AssertSuccess("<-1.1, 2.2,3.3>", new(-1.1f, 2.2f, 3.3f));
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreEqual(Location.Origin, new Location(-0f, -0f, -0f));
		Assert.AreNotEqual(Location.Origin, OneTwoNegThree);
		Assert.IsTrue(OneTwoNegThree.Equals(OneTwoNegThree));
		Assert.IsFalse(OneTwoNegThree.Equals(Location.Origin));
		Assert.IsTrue(OneTwoNegThree == new Location(1f, 2f, -3f));
		Assert.IsFalse(Location.Origin == OneTwoNegThree);
		Assert.IsFalse(Location.Origin != new Location(0f, 0f, 0f));
		Assert.IsTrue(OneTwoNegThree != Location.Origin);
		Assert.IsTrue(new Location(1f, 2f, 3f) != new Location(0f, 2f, 3f));
		Assert.IsTrue(new Location(1f, 2f, 3f) != new Location(1f, 0f, 3f));
		Assert.IsTrue(new Location(1f, 2f, 3f) != new Location(1f, 2f, 0f));

		Assert.IsTrue(Location.Origin.Equals(Location.Origin, 0f));
		Assert.IsTrue(OneTwoNegThree.Equals(OneTwoNegThree, 0f));
		Assert.IsTrue(new Location(0.5f, 0.6f, 0.7f).Equals(new Location(0.4f, 0.5f, 0.6f), 0.11f));
		Assert.IsFalse(new Location(0.5f, 0.6f, 0.7f).Equals(new Location(0.4f, 0.5f, 0.6f), 0.09f));
		Assert.IsTrue(new Location(-0.5f, -0.5f, -0.5f).Equals(new Location(-0.4f, -0.4f, -0.4f), 0.11f));
		Assert.IsFalse(new Location(-0.5f, -0.5f, -0.5f).Equals(new Location(-0.4f, -0.4f, -0.4f), 0.09f));
		Assert.IsFalse(new Location(-0.5f, -0.5f, -0.5f).Equals(new Location(0.4f, -0.4f, -0.4f), 0.11f));
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityWithDistanceTolerance() {
		Assert.AreEqual(true, OneTwoNegThree.EqualsWithinDistance(OneTwoNegThree, 0f));
		Assert.AreEqual(true, OneTwoNegThree.EqualsWithinDistance(new(1f, 2f, -2f), 1f + TestTolerance));
		Assert.AreEqual(false, OneTwoNegThree.EqualsWithinDistance(new(1f, 2f, -1.5f), 1f + TestTolerance));
		Assert.AreEqual(true, Location.Origin.EqualsWithinDistance(new(1f, 1f, 0f), 1.42f));
		Assert.AreEqual(false, Location.Origin.EqualsWithinDistance(new(1f, 1f, 0f), 1.40f));
	}
}