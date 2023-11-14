// Created on 2023-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class VectTest {
	const float TestTolerance = 0.001f;
	static readonly Vect OneTwoNegThree = new(1f, 2f, -3f);
	
	[Test]
	public void ShouldCorrectlyInitializeStaticReadonlyMembers() {
		Assert.AreEqual(new Vect(0f, 0f, 0f), Vect.Zero);
	}

	[Test]
	public void ShouldCorrectlyImplementProperties() {
		Assert.AreEqual(1f, OneTwoNegThree.X);
		Assert.AreEqual(2f, OneTwoNegThree.Y);
		Assert.AreEqual(-3f, OneTwoNegThree.Z);

		Assert.AreEqual(1.5f, (OneTwoNegThree with { X = 1.5f }).X);
		Assert.AreEqual(2.5f, (OneTwoNegThree with { Y = 2.5f }).Y);
		Assert.AreEqual(-3.5f, (OneTwoNegThree with { Z = -3.5f }).Z);

		Assert.AreEqual(new Vect(4f, 5f, -6f), OneTwoNegThree with { X = 4f, Y = 5f, Z = -6f });
	}

	[Test]
	public void ConstructorsShouldCorrectlyConstruct() {
		Assert.AreEqual(Vect.Zero, new Vect());
		Assert.AreEqual(Vect.WValue, new Vect().AsVector4.W);

		Assert.AreEqual(new Vect(new Vector4(0.1f, 0.2f, 0.3f, Vect.WValue)), new Vect(0.1f, 0.2f, 0.3f));
		Assert.AreEqual(Vect.WValue, new Vect(0.1f, 0.2f, 0.3f).AsVector4.W);
	}

	[Test]
	public void StaticFactoryMethodsShouldCorrectlyConstruct() {
		Assert.AreEqual(new Vect(10f, 0f, 0f), Vect.FromDirectionAndDistance(new Direction(1f, 0f, 0f), 10f));
		Assert.AreEqual(new Vect(-10f, 0f, 0f), Vect.FromDirectionAndDistance(new Direction(1f, 0f, 0f), -10f));
		Assert.AreEqual(new Vect(-10f, 0f, 0f), Vect.FromDirectionAndDistance(new Direction(-1f, 0f, 0f), 10f));
		Assert.AreEqual(new Vect(10f, 0f, 0f), Vect.FromDirectionAndDistance(new Direction(-1f, 0f, 0f), -10f));
		Assert.AreEqual(new Vect(0f, 0f, 0f), Vect.FromDirectionAndDistance(Direction.None, 0f));
		Assert.AreEqual(new Vect(0f, 0f, 0f), Vect.FromDirectionAndDistance(Direction.None, 10f));
		Assert.AreEqual(new Vect(0f, 0f, 0f), Vect.FromDirectionAndDistance(Direction.Up, 0f));

		Assert.AreEqual(new Vect(-1.2f, 2.4f, 0f), Vect.FromVector3(new(-1.2f, 2.4f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyConvertToVector3() {
		Assert.AreEqual(new Vector3(1f, 2f, -3f), OneTwoNegThree.ToVector3());
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromValueTuple() {
		Assert.AreEqual(OneTwoNegThree, (Vect) (1, 2, -3));
		var (x, y, z) = OneTwoNegThree;
		Assert.AreEqual(1f, x);
		Assert.AreEqual(2f, y);
		Assert.AreEqual(-3f, z);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		void AssertIteration(Vect input) {
			var span = Vect.ConvertToSpan(input);
			Assert.AreEqual(3, span.Length);
			Assert.AreEqual(input.X, span[0]);
			Assert.AreEqual(input.Y, span[1]);
			Assert.AreEqual(input.Z, span[2]);
			Assert.AreEqual(input, Vect.ConvertFromSpan(span));
		}

		AssertIteration(Vect.Zero);
		AssertIteration(OneTwoNegThree);
		AssertIteration(new Vect(-0.001f, 0f, 100000f));
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(Vect input, string expectedValue) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N1";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);

			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(Vect.Zero, "<0.0, 0.0, 0.0>");
		AssertIteration(OneTwoNegThree, "<1.0, 2.0, -3.0>");
		AssertIteration(new Vect(0.5f, 0f, -1.6f), "<0.5, 0.0, -1.6>");
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(Vect input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			Vect input,
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

		var fractionalVect = new Vect(1.211f, 3.422f, -5.633f);

		AssertFail(Vect.Zero, Array.Empty<char>(), "N0", null);
		AssertFail(Vect.Zero, new char[8], "N0", null);
		AssertSuccess(Vect.Zero, new char[9], "N0", null, "<0, 0, 0>");
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

		void AssertSuccess(string input, Vect expectedResult) {
			AssertToleranceEquals(expectedResult, Vect.Parse(input, testCulture), TestTolerance);
			AssertToleranceEquals(expectedResult, Vect.Parse(input.AsSpan(), testCulture), TestTolerance);
			Assert.IsTrue(Vect.TryParse(input, testCulture, out var parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
			Assert.IsTrue(Vect.TryParse(input.AsSpan(), testCulture, out parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
		}

		void AssertFail(string input) {
			Assert.Catch(() => Vect.Parse(input, testCulture));
			Assert.Catch(() => Vect.Parse(input.AsSpan(), testCulture));
			Assert.False(Vect.TryParse(input, testCulture, out _));
			Assert.False(Vect.TryParse(input.AsSpan(), testCulture, out _));
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
		Assert.AreEqual(Vect.Zero, -Vect.Zero);
		Assert.AreNotEqual(Vect.Zero, OneTwoNegThree);
		Assert.IsTrue(OneTwoNegThree.Equals(OneTwoNegThree));
		Assert.IsFalse(OneTwoNegThree.Equals(Vect.Zero));
		Assert.IsTrue(OneTwoNegThree == new Vect(1f, 2f, -3f));
		Assert.IsFalse(Vect.Zero == OneTwoNegThree);
		Assert.IsFalse(Vect.Zero != new Vect(0f, 0f, 0f));
		Assert.IsTrue(OneTwoNegThree != Vect.Zero);
		Assert.IsTrue(new Vect(1f, 2f, 3f) != new Vect(0f, 2f, 3f));
		Assert.IsTrue(new Vect(1f, 2f, 3f) != new Vect(1f, 0f, 3f));
		Assert.IsTrue(new Vect(1f, 2f, 3f) != new Vect(1f, 2f, 0f));

		Assert.IsTrue(Vect.Zero.Equals(Vect.Zero, 0f));
		Assert.IsTrue(OneTwoNegThree.Equals(OneTwoNegThree, 0f));
		Assert.IsTrue(new Vect(0.5f, 0.6f, 0.7f).Equals(new Vect(0.4f, 0.5f, 0.6f), 0.11f));
		Assert.IsFalse(new Vect(0.5f, 0.6f, 0.7f).Equals(new Vect(0.4f, 0.5f, 0.6f), 0.09f));
		Assert.IsTrue(new Vect(-0.5f, -0.5f, -0.5f).Equals(new Vect(-0.4f, -0.4f, -0.4f), 0.11f));
		Assert.IsFalse(new Vect(-0.5f, -0.5f, -0.5f).Equals(new Vect(-0.4f, -0.4f, -0.4f), 0.09f));
		Assert.IsFalse(new Vect(-0.5f, -0.5f, -0.5f).Equals(new Vect(0.4f, -0.4f, -0.4f), 0.11f));
	}

	[Test]
	public void ShouldCorrectlyCastFromDirectionAndLocation() {
		Assert.AreEqual(new Vect(1f, 0f, 0f), (Vect) new Direction(1f, 0f, 0f));
		Assert.AreEqual(Vect.WValue, ((Vect) new Direction(1f, 0f, 0f)).AsVector4.W);

		Assert.AreEqual(new Vect(1f, 0f, 0f), (Vect) new Location(1f, 0f, 0f));
		Assert.AreEqual(Vect.WValue, ((Vect) new Location(1f, 0f, 0f)).AsVector4.W);
	}
}