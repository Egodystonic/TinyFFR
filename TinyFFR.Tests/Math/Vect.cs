// Created on 2023-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class VectTest {
	const float TestTolerance = 0.001f;
	static readonly Vect OneTwoNegThree = new(1f, 2f, -3f);

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<Vect>();

	[Test]
	public void ShouldCorrectlyInitializeStaticReadonlyMembers() {
		Assert.AreEqual(new Vect(0f, 0f, 0f), Vect.Zero);
		Assert.AreEqual(new Vect(1f, 1f, 1f), Vect.One);
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

		Assert.AreEqual(Vect.Zero, new Vect(0f));
		Assert.AreEqual(Vect.One, new Vect(1f));
		Assert.AreEqual(-Vect.One, new Vect(-1f));
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
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<Vect>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(Vect.Zero, OneTwoNegThree, new(-0.001f, 0f, 100000f));
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(Vect.Zero, 0f, 0f, 0f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(OneTwoNegThree, 1f, 2f, -3f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(new Vect(-0.001f, 0f, 100000f), -0.001f, 0f, 100000f);
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

	[Test]
	public void ShouldCorrectlyCalculateLengthAndLengthSquared() {
		Assert.AreEqual(0f, Vect.Zero.Length);
		Assert.AreEqual(0f, Vect.Zero.LengthSquared);
		Assert.AreEqual(MathF.Sqrt(1f + 4f + 9f), OneTwoNegThree.Length);
		Assert.AreEqual(1f + 4f + 9f, OneTwoNegThree.LengthSquared);

		Assert.AreEqual(OneTwoNegThree.Length, new Vect(-1f, -2f, 3f).Length);
		Assert.AreEqual(OneTwoNegThree.LengthSquared, new Vect(-1f, -2f, 3f).LengthSquared);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var v = new Vect(x, y, z);

					Assert.AreEqual(Math.Sqrt(x * x + y * y + z * z), v.Length, TestTolerance);

					Assert.AreEqual(v.Length, (-v).Length);
					Assert.AreEqual(v.Length * v.Length, v.LengthSquared, TestTolerance);
					Assert.AreEqual(v.Length * 2f, (v * 2f).Length);
					Assert.IsTrue(v.Length >= 0f);
					Assert.IsTrue((v.Length > 1f && v.LengthSquared > v.Length) || (v.Length <= 1f && v.LengthSquared <= v.Length));
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineIfIsNormalized() {
		Assert.AreEqual(false, Vect.Zero.IsUnitLength);
		Assert.AreEqual(false, Vect.Zero.AsUnitLength.IsUnitLength);
		Assert.AreEqual(false, OneTwoNegThree.IsUnitLength);
		Assert.AreEqual(true, OneTwoNegThree.AsUnitLength.IsUnitLength);
		Assert.AreEqual(true, new Vect(1f, 0f, 0f).IsUnitLength);
		Assert.AreEqual(true, new Vect(0f, -1f, 0f).IsUnitLength);
		Assert.AreEqual(true, new Vect(0.707f, 0f, 0.707f).IsUnitLength);
		Assert.AreEqual(true, new Vect(0f, 0.707f, -0.707f).IsUnitLength);
	}

	[Test]
	public void UnitLengthTestShouldUseAppropriateErrorMargin() {
		const int NumNonNormalizedRotations = 200;

		Assert.AreEqual(true, new Vect(1f, 0f, 0f).IsUnitLength);
		Assert.AreEqual(true, new Vect(0.9999f, 0f, 0f).IsUnitLength);
		Assert.AreEqual(false, new Vect(0.999f, 0f, 0f).IsUnitLength);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var v = new Vect(x, y, z).AsUnitLength;
					if (v == Vect.Zero) {
						Assert.IsFalse(v.IsUnitLength);
						continue;
					}

					var rot = (v.Direction >> v.Direction.AnyOrthogonal()) * 0.1f;
					for (var i = 0; i < NumNonNormalizedRotations; ++i) v = rot.Rotate(v);
					Assert.IsTrue(v.IsUnitLength);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyReverse() {
		Assert.AreEqual(Vect.Zero, -Vect.Zero);
		Assert.AreEqual(new Vect(-1f, -2f, 3f), -OneTwoNegThree);
		Assert.AreEqual(new Vect(-1f, -1f, -1f), new Vect(1f, 1f, 1f).Reversed);
	}

	[Test]
	public void ShouldCorrectlyReciprocate() {
		Assert.AreEqual(new Vect(1f, 0.5f, 1f / -3f), OneTwoNegThree.Reciprocal);
		Assert.AreEqual(null, Vect.Zero.Reciprocal);
		Assert.AreEqual(null, new Vect(0f, 1f, 1f).Reciprocal);
		Assert.AreEqual(null, new Vect(1f, 0f, 1f).Reciprocal);
		Assert.AreEqual(null, new Vect(1f, 1f, 0f).Reciprocal);
	}

	[Test]
	public void ShouldCorrectlyAddAndSubtract() {
		Assert.AreEqual(Vect.Zero, Vect.Zero + Vect.Zero);
		Assert.AreEqual(Vect.Zero, Vect.Zero - Vect.Zero);
		Assert.AreEqual(OneTwoNegThree * 2f, OneTwoNegThree + OneTwoNegThree);
		Assert.AreEqual(Vect.Zero, OneTwoNegThree - OneTwoNegThree);
		Assert.AreEqual(new Vect(2f, 4f, 6f), new Vect(-1f, -2f, -3f) + new Vect(3f, 6f, 9f));
		Assert.AreEqual(new Vect(-4f, -8f, 12f), new Vect(-1f, -2f, 9f) - new Vect(3f, 6f, -3f));
	}

	[Test]
	public void ShouldCorrectlyMultiplyAndDivide() {
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree / 1f).AsVector4.W);
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree / -1f).AsVector4.W);
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree / 0f).AsVector4.W);
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree * 1f).AsVector4.W);
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree * -1f).AsVector4.W);
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree * 0f).AsVector4.W);

		Assert.AreEqual(Vect.WValue, (OneTwoNegThree * Vect.Zero).AsVector4.W);
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree * OneTwoNegThree).AsVector4.W);
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree * -OneTwoNegThree).AsVector4.W);
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree / Vect.Zero).AsVector4.W);
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree / OneTwoNegThree).AsVector4.W);
		Assert.AreEqual(Vect.WValue, (OneTwoNegThree / -OneTwoNegThree).AsVector4.W);

		AssertToleranceEquals(
			OneTwoNegThree,
			OneTwoNegThree * 1f,
			TestTolerance
		);
		AssertToleranceEquals(
			OneTwoNegThree,
			OneTwoNegThree / 1f,
			TestTolerance
		);
		AssertToleranceEquals(
			Vect.Zero,
			OneTwoNegThree * 0f,
			TestTolerance
		);
		AssertToleranceEquals(
			Vect.Zero,
			OneTwoNegThree / 0f,
			TestTolerance
		);
		AssertToleranceEquals(
			-OneTwoNegThree,
			OneTwoNegThree * -1f,
			TestTolerance
		);
		AssertToleranceEquals(
			-OneTwoNegThree,
			OneTwoNegThree / -1f,
			TestTolerance
		);

		AssertToleranceEquals(
			OneTwoNegThree,
			OneTwoNegThree * new Vect(1f, 1f, 1f),
			TestTolerance
		);
		AssertToleranceEquals(
			-OneTwoNegThree,
			OneTwoNegThree * new Vect(-1f, -1f, -1f),
			TestTolerance
		);
		AssertToleranceEquals(
			OneTwoNegThree,
			OneTwoNegThree / new Vect(1f, 1f, 1f),
			TestTolerance
		);
		AssertToleranceEquals(
			-OneTwoNegThree,
			OneTwoNegThree / new Vect(-1f, -1f, -1f),
			TestTolerance
		);
		AssertToleranceEquals(
			Vect.Zero,
			OneTwoNegThree * Vect.Zero,
			TestTolerance
		);
		AssertToleranceEquals(
			Vect.Zero,
			OneTwoNegThree / Vect.Zero,
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(1f, 4f, 9f),
			OneTwoNegThree * OneTwoNegThree,
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(1f, 1f, 1f),
			OneTwoNegThree / OneTwoNegThree,
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(-1f, -4f, -9f),
			OneTwoNegThree * -OneTwoNegThree,
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(-1f, -1f, -1f),
			OneTwoNegThree / -OneTwoNegThree,
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyProvideDirection() {
		Assert.AreEqual(Direction.None, Vect.Zero.Direction);
		Assert.AreEqual(new Direction(1f, 0f, 0f), new Vect(40f, 0f, 0f).Direction);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var v = new Vect(x, y, z);
					var vNorm = v.AsUnitLength;

					AssertToleranceEquals(new Direction(vNorm.X, vNorm.Y, vNorm.Z), v.Direction, TestTolerance);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyNormalize() {
		AssertToleranceEquals(new Vect(0.707f, 0f, -0.707f), new Vect(1f, 0f, -1f).AsUnitLength, TestTolerance);
		Assert.AreEqual(Vect.Zero, Vect.Zero.AsUnitLength);
		Assert.AreEqual(Vect.Zero, (-Vect.Zero).AsUnitLength);
		Assert.AreEqual(new Vect(0f, 1f, 0f), new Vect(0f, 0.0001f, 0f).AsUnitLength);
	}

	[Test]
	public void ShouldCorrectlyProjectOnToDirectionAndVect() {
		Assert.AreEqual(new Vect(1f, 0f, 0f), new Vect(1f, 1f, 0f).ProjectedOnTo(new Direction(1f, 0f, 0f)));
		Assert.AreEqual(new Vect(1f, 0f, 0f), new Vect(1f, 1f, 0f).ProjectedOnTo(new Direction(1f, 0f, 0f)));

		// https://www.wolframalpha.com/input?i=project+%5B14.2%2C+-7.1%2C+8.9%5D+on+to+%5B0.967%2C+0.137%2C+-0.216%5D
		AssertToleranceEquals(new Vect(10.473f, 1.484f, -2.339f), new Vect(14.2f, -7.1f, 8.9f).ProjectedOnTo(new Direction(0.967f, 0.137f, -0.216f)), TestTolerance);

		Assert.AreEqual(Vect.Zero, new Vect(1f, 0f, 0f).ProjectedOnTo(new Direction(0f, 1f, 0f)));
		Assert.AreEqual(Vect.Zero, new Vect(1f, 0f, 0f).ProjectedOnTo(new Direction(0f, 1f, 0f)));

		Assert.AreEqual(new Vect(1f, 0f, 0f), new Vect(1f, 1f, 0f).ProjectedOnTo(new Vect(1f, 0f, 0f)));
		Assert.AreEqual(new Vect(1f, 0f, 0f), new Vect(1f, 1f, 0f).ProjectedOnTo(new Vect(1f, 0f, 0f)));

		// https://www.wolframalpha.com/input?i=project+%5B14.2%2C+-7.1%2C+8.9%5D+on+to+%5B0.967%2C+0.137%2C+-0.216%5D
		AssertToleranceEquals(new Vect(10.473f, 1.484f, -2.339f), new Vect(14.2f, -7.1f, 8.9f).ProjectedOnTo(new Vect(0.967f, 0.137f, -0.216f)), TestTolerance);

		Assert.AreEqual(Vect.Zero, new Vect(1f, 0f, 0f).ProjectedOnTo(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(Vect.Zero, new Vect(1f, 0f, 0f).ProjectedOnTo(new Vect(0f, 1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstDirectionAndVect() {
		Assert.AreEqual(Vect.Zero, Vect.Zero.OrthogonalizedAgainst(Direction.Up));
		Assert.AreEqual(null, (Direction.Up * 100f).OrthogonalizedAgainst(Direction.Up));
		Assert.AreEqual(OneTwoNegThree, OneTwoNegThree.OrthogonalizedAgainst(Direction.None));
		Assert.AreEqual(Vect.Zero, Vect.Zero.OrthogonalizedAgainst(Direction.None));

		Assert.AreEqual(Vect.Zero, Vect.Zero.OrthogonalizedAgainst(Direction.Up * 10f));
		Assert.AreEqual(null, (Direction.Up * 100f).OrthogonalizedAgainst(Direction.Up * 10f));
		Assert.AreEqual(OneTwoNegThree, OneTwoNegThree.OrthogonalizedAgainst(Direction.None * 10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.OrthogonalizedAgainst(Direction.None * 10f));

		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).OrthogonalizedAgainst(new Direction(0f, 1f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).FastOrthogonalizedAgainst(new Direction(0f, 1f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).OrthogonalizedAgainst(new Direction(-1f, 0f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).FastOrthogonalizedAgainst(new Direction(-1f, 0f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).OrthogonalizedAgainst(new Direction(1f, 0f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).FastOrthogonalizedAgainst(new Direction(1f, 0f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).OrthogonalizedAgainst(new Vect(0f, 1f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).FastOrthogonalizedAgainst(new Vect(0f, 1f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).OrthogonalizedAgainst(new Vect(-1f, 0f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).FastOrthogonalizedAgainst(new Vect(-1f, 0f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).OrthogonalizedAgainst(new Vect(1f, 0f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).FastOrthogonalizedAgainst(new Vect(1f, 0f, 0f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithDirectionAndVect() {
		Assert.AreEqual(Vect.Zero, Vect.Zero.ParallelizedWith(Direction.Up));
		Assert.AreEqual(null, (Direction.Up * 100f).ParallelizedWith(Direction.Right));
		Assert.AreEqual(OneTwoNegThree, OneTwoNegThree.ParallelizedWith(Direction.None));
		Assert.AreEqual(Vect.Zero, Vect.Zero.ParallelizedWith(Direction.None));

		Assert.AreEqual(Vect.Zero, Vect.Zero.ParallelizedWith(Direction.Up * 10f));
		Assert.AreEqual(null, (Direction.Up * 100f).ParallelizedWith(Direction.Right * 10f));
		Assert.AreEqual(OneTwoNegThree, OneTwoNegThree.ParallelizedWith(Direction.None * 10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.ParallelizedWith(Direction.None * 10f));

		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).ParallelizedWith(new Direction(1f, 0f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).FastParallelizedWith(new Direction(1f, 0f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).ParallelizedWith(new Direction(0f, -1f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).FastParallelizedWith(new Direction(0f, -1f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).ParallelizedWith(new Direction(0f, 1f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).FastParallelizedWith(new Direction(0f, 1f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).ParallelizedWith(new Vect(1f, 0f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(1f, 0f, 0f),
			new Vect(0.8f, 0.2f, 0f).WithLength(1f).FastParallelizedWith(new Vect(1f, 0f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).ParallelizedWith(new Vect(0f, -1f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).FastParallelizedWith(new Vect(0f, -1f, 0f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).ParallelizedWith(new Vect(0f, 1f, 0f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0f, -10f, 0f),
			new Vect(1f, -5f, 0f).WithLength(10f).FastParallelizedWith(new Vect(0f, 1f, 0f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyRescale() {
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLength(-10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLength(0f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLength(10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.WithLengthOne());
		Assert.AreEqual(OneTwoNegThree, OneTwoNegThree.WithLength(OneTwoNegThree.Length));

		// https://www.wolframalpha.com/input?i=normalize+%5B1%2C+2%2C+-3%5D
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f), OneTwoNegThree.WithLength(1f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f), OneTwoNegThree.WithLengthOne(), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * 2f, OneTwoNegThree.WithLength(2f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * -1f, OneTwoNegThree.WithLength(-1f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * -1f, OneTwoNegThree.WithLengthOne().Reversed, TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * -2f, OneTwoNegThree.WithLength(-2f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * 0.5f, OneTwoNegThree.WithLength(0.5f), TestTolerance);
		AssertToleranceEquals(new Vect(0.267f, 0.535f, -0.802f) * -0.5f, OneTwoNegThree.WithLength(-0.5f), TestTolerance);
		Assert.AreEqual(Vect.Zero, OneTwoNegThree.WithLength(0f));
	}

	[Test]
	public void ShouldCorrectlyScale() {
		Assert.AreEqual(Vect.Zero, Vect.Zero * -10f);
		Assert.AreEqual(Vect.Zero, Vect.Zero * 0f);
		Assert.AreEqual(Vect.Zero, Vect.Zero * 10f);
		Assert.AreEqual(new Vect(2f, 4f, -6f), OneTwoNegThree * 2f);
		Assert.AreEqual(new Vect(0.5f, 1f, -1.5f), OneTwoNegThree * 0.5f);
		Assert.AreEqual(new Vect(-3f, -6f, 9f), OneTwoNegThree * -3f);
		Assert.AreEqual(Vect.Zero, OneTwoNegThree * 0f);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var v = new Vect(x, y, z);

					Assert.AreEqual(v * x, x * v);
					Assert.AreEqual(x == 0f ? Vect.Zero : v * (1f / x), v / x);

					AssertToleranceEquals(new Vect(x, 2f * y, -3f * z), OneTwoNegThree.ScaledBy(v), TestTolerance);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyShortenAndLengthen() {
		Assert.AreEqual(Vect.Zero, Vect.Zero.LengthenedBy(10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.LengthenedBy(0f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.ShortenedBy(10f));
		Assert.AreEqual(Vect.Zero, Vect.Zero.ShortenedBy(0f));

		AssertToleranceEquals(OneTwoNegThree.WithLength(10f), OneTwoNegThree.WithLength(7f).LengthenedBy(3f), TestTolerance);
		AssertToleranceEquals(OneTwoNegThree.WithLength(-10f), OneTwoNegThree.WithLength(7f).LengthenedBy(-17f), TestTolerance);
		AssertToleranceEquals(OneTwoNegThree.WithLength(-10f), OneTwoNegThree.WithLength(-7f).LengthenedBy(3f), TestTolerance);
		AssertToleranceEquals(OneTwoNegThree.WithLength(10f), OneTwoNegThree.WithLength(-7f).LengthenedBy(-17f), TestTolerance);
		AssertToleranceEquals(Vect.Zero, OneTwoNegThree.WithLength(7f).LengthenedBy(-7f), TestTolerance);
		AssertToleranceEquals(Vect.Zero, OneTwoNegThree.WithLength(-7f).LengthenedBy(-7f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		AssertToleranceEquals(OneTwoNegThree, Vect.Interpolate(OneTwoNegThree, Vect.Zero, 0f), TestTolerance);
		AssertToleranceEquals(Vect.Zero, Vect.Interpolate(OneTwoNegThree, Vect.Zero, 1f), TestTolerance);
		AssertToleranceEquals(Vect.FromVector3(OneTwoNegThree.ToVector3() * 0.5f), Vect.Interpolate(OneTwoNegThree, Vect.Zero, 0.5f), TestTolerance);
		AssertToleranceEquals(Vect.FromVector3(OneTwoNegThree.ToVector3() * 2f), Vect.Interpolate(OneTwoNegThree, Vect.Zero, -1f), TestTolerance);
		AssertToleranceEquals(Vect.FromVector3(OneTwoNegThree.ToVector3() * -1f), Vect.Interpolate(OneTwoNegThree, Vect.Zero, 2f), TestTolerance);

		var testList = new List<Vect>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}
		for (var i = 0; i < testList.Count; ++i) {
			for (var j = i; j < testList.Count; ++j) {
				var start = testList[i];
				var end = testList[j];

				for (var f = -1f; f <= 2f; f += 0.1f) {
					AssertToleranceEquals(new(Single.Lerp(start.X, end.X, f), Single.Lerp(start.Y, end.Y, f), Single.Lerp(start.Z, end.Z, f)), Vect.Interpolate(start, end, f), TestTolerance);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCreateNonBoundedRandomValues() {
		const int NumIterations = 50_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = Vect.Random();
			Assert.GreaterOrEqual(val.X, -Vect.DefaultRandomRange);
			Assert.LessOrEqual(val.X, Vect.DefaultRandomRange);
			Assert.GreaterOrEqual(val.Y, -Vect.DefaultRandomRange);
			Assert.LessOrEqual(val.Y, Vect.DefaultRandomRange);
			Assert.GreaterOrEqual(val.Z, -Vect.DefaultRandomRange);
			Assert.LessOrEqual(val.Z, Vect.DefaultRandomRange);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateBoundedRandomValues() {
		const int NumIterations = 50_000;

		for (var i = 0; i < NumIterations; ++i) {
			var a = Vect.Random();
			var b = a + new Vect(3f, 3f, 3f);
			var val = Vect.Random(a, b);
			Assert.GreaterOrEqual(val.X, a.X);
			Assert.LessOrEqual(val.X, b.X);
			Assert.GreaterOrEqual(val.Y, a.Y);
			Assert.LessOrEqual(val.Y, b.Y);
			Assert.GreaterOrEqual(val.Z, a.Z);
			Assert.LessOrEqual(val.Z, b.Z);
		}
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = new Vect(-3f, 1f, 3f);
		var max = new Vect(3f, -1f, -3f);

		AssertToleranceEquals(
			new Vect(0f, 0f, 0f),
			new Vect(0f, 0f, 0f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(-3f, 1f, 3f),
			new Vect(-3f, 1f, 3f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(3f, -1f, -3f),
			new Vect(3f, -1f, -3f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(-3f, 1f, 3f),
			new Vect(-4f, 2f, 4f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(3f, -1f, -3f),
			new Vect(4f, -2f, -4f).Clamp(min, max),
			TestTolerance
		);


		AssertToleranceEquals(
			new Vect(-0.158f, 0.0526f, 0.158f),
			new Vect(0f, 1f, 0f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0.158f, -0.0526f, -0.158f),
			new Vect(0f, -1f, 0f).Clamp(min, max),
			TestTolerance
		);


		(min, max) = (max, min);
		AssertToleranceEquals(
			new Vect(0f, 0f, 0f),
			new Vect(0f, 0f, 0f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(-3f, 1f, 3f),
			new Vect(-3f, 1f, 3f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(3f, -1f, -3f),
			new Vect(3f, -1f, -3f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(-3f, 1f, 3f),
			new Vect(-4f, 2f, 4f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(3f, -1f, -3f),
			new Vect(4f, -2f, -4f).Clamp(min, max),
			TestTolerance
		);


		AssertToleranceEquals(
			new Vect(-0.158f, 0.0526f, 0.158f),
			new Vect(0f, 1f, 0f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Vect(0.158f, -0.0526f, -0.158f),
			new Vect(0f, -1f, 0f).Clamp(min, max),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineOrthogonalityToOtherDirectionsAndVects() {
		void AssertCombinationExactly(bool expectation, Direction d1, Direction d2) {
			var v1 = d1 * 10f;
			var v2 = d2 * 10f;

			Assert.AreEqual(expectation, v1.IsOrthogonalTo(d2));
			Assert.AreEqual(expectation, v2.IsOrthogonalTo(d1));
			Assert.AreEqual(expectation, v1.IsOrthogonalTo(-d2));
			Assert.AreEqual(expectation, v2.IsOrthogonalTo(-d1));
			Assert.AreEqual(expectation, (-v1).IsOrthogonalTo(d2));
			Assert.AreEqual(expectation, (-v2).IsOrthogonalTo(d1));
			Assert.AreEqual(expectation, (-v1).IsOrthogonalTo(-d2));
			Assert.AreEqual(expectation, (-v2).IsOrthogonalTo(-d1));

			Assert.AreEqual(expectation, v1.IsOrthogonalTo(v2));
			Assert.AreEqual(expectation, v2.IsOrthogonalTo(v1));
			Assert.AreEqual(expectation, v1.IsOrthogonalTo(-v2));
			Assert.AreEqual(expectation, v2.IsOrthogonalTo(-v1));
			Assert.AreEqual(expectation, (-v1).IsOrthogonalTo(v2));
			Assert.AreEqual(expectation, (-v2).IsOrthogonalTo(v1));
			Assert.AreEqual(expectation, (-v1).IsOrthogonalTo(-v2));
			Assert.AreEqual(expectation, (-v2).IsOrthogonalTo(-v1));
		}
		void AssertCombination(bool expectation, Direction d1, Direction d2, Angle? tolerance) {
			var v1 = d1 * 10f;
			var v2 = d2 * 10f;

			if (tolerance == null) {
				Assert.AreEqual(expectation, v1.IsApproximatelyOrthogonalTo(d2));
				Assert.AreEqual(expectation, v2.IsApproximatelyOrthogonalTo(d1));
				Assert.AreEqual(expectation, v1.IsApproximatelyOrthogonalTo(-d2));
				Assert.AreEqual(expectation, v2.IsApproximatelyOrthogonalTo(-d1));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyOrthogonalTo(d2));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyOrthogonalTo(d1));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyOrthogonalTo(-d2));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyOrthogonalTo(-d1));

				Assert.AreEqual(expectation, v1.IsApproximatelyOrthogonalTo(v2));
				Assert.AreEqual(expectation, v2.IsApproximatelyOrthogonalTo(v1));
				Assert.AreEqual(expectation, v1.IsApproximatelyOrthogonalTo(-v2));
				Assert.AreEqual(expectation, v2.IsApproximatelyOrthogonalTo(-v1));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyOrthogonalTo(v2));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyOrthogonalTo(v1));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyOrthogonalTo(-v2));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyOrthogonalTo(-v1));
			}
			else {
				Assert.AreEqual(expectation, v1.IsApproximatelyOrthogonalTo(d2, tolerance.Value));
				Assert.AreEqual(expectation, v2.IsApproximatelyOrthogonalTo(d1, tolerance.Value));
				Assert.AreEqual(expectation, v1.IsApproximatelyOrthogonalTo(-d2, tolerance.Value));
				Assert.AreEqual(expectation, v2.IsApproximatelyOrthogonalTo(-d1, tolerance.Value));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyOrthogonalTo(d2, tolerance.Value));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyOrthogonalTo(d1, tolerance.Value));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyOrthogonalTo(-d2, tolerance.Value));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyOrthogonalTo(-d1, tolerance.Value));

				Assert.AreEqual(expectation, v1.IsApproximatelyOrthogonalTo(v2, tolerance.Value));
				Assert.AreEqual(expectation, v2.IsApproximatelyOrthogonalTo(v1, tolerance.Value));
				Assert.AreEqual(expectation, v1.IsApproximatelyOrthogonalTo(-v2, tolerance.Value));
				Assert.AreEqual(expectation, v2.IsApproximatelyOrthogonalTo(-v1, tolerance.Value));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyOrthogonalTo(v2, tolerance.Value));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyOrthogonalTo(v1, tolerance.Value));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyOrthogonalTo(-v2, tolerance.Value));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyOrthogonalTo(-v1, tolerance.Value));
			}
		}

		AssertCombination(true, Direction.Left, Direction.Down, null);
		AssertCombination(true, Direction.Left, Direction.Up, null);
		AssertCombination(false, Direction.Left, Direction.Right, null);
		AssertCombination(false, Direction.Forward, Direction.Backward, null);
		AssertCombination(false, Direction.Forward, Direction.None, null);
		AssertCombination(false, Direction.None, Direction.Right, null);
		AssertCombination(false, Direction.None, Direction.None, null);

		AssertCombination(true, Direction.Left, Direction.Down, 0f);
		AssertCombination(true, Direction.Left, Direction.Up, 0f);
		AssertCombination(false, Direction.Left, Direction.Right, 89f);
		AssertCombination(false, Direction.Forward, Direction.Backward, 89f);

		AssertCombination(true, Direction.Left, Direction.Down, 90f);

		AssertCombinationExactly(false, Direction.Left, Direction.Right);
		AssertCombinationExactly(false, Direction.Left, Direction.None);

		var testList = new List<Direction>();
		for (var x = -3f; x <= 3f; x += 1f) {
			for (var y = -3f; y <= 3f; y += 1f) {
				for (var z = -3f; z <= 3f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			AssertCombinationExactly(false, testList[i], testList[i]);
			if (testList[i] != Direction.None) AssertCombinationExactly(true, testList[i], testList[i].AnyOrthogonal());
			for (var j = i; j < testList.Count; ++j) {
				var a = testList[i];
				var b = testList[j];
				if (a == Direction.None || b == Direction.None) {
					AssertCombination(false, a, b, null);
					AssertCombination(false, a, b, 0f);
					AssertCombination(false, a, b, 45f);
					AssertCombination(false, a, b, 90f);
					AssertCombination(false, a, b, 180f);
					AssertCombinationExactly(false, a, b);
					continue;
				}

				var angle = a ^ b;
				var diffToOrthogonality = (angle - Angle.QuarterCircle).Absolute;
				if (diffToOrthogonality == Angle.Zero) {
					AssertCombination(true, a, b, null);
					AssertCombination(true, a, b, 0f);
				}
				else {
					AssertCombination(true, a, b, diffToOrthogonality + 0.1f);
					AssertCombination(false, a, b, diffToOrthogonality - 0.1f);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineParallelismToOtherDirectionsAndVects() {
		void AssertCombinationExactly(bool expectation, Direction d1, Direction d2) {
			var v1 = d1 * 10f;
			var v2 = d2 * 10f;

			Assert.AreEqual(expectation, v1.IsParallelTo(d2));
			Assert.AreEqual(expectation, v2.IsParallelTo(d1));
			Assert.AreEqual(expectation, v1.IsParallelTo(-d2));
			Assert.AreEqual(expectation, v2.IsParallelTo(-d1));
			Assert.AreEqual(expectation, (-v1).IsParallelTo(d2));
			Assert.AreEqual(expectation, (-v2).IsParallelTo(d1));
			Assert.AreEqual(expectation, (-v1).IsParallelTo(-d2));
			Assert.AreEqual(expectation, (-v2).IsParallelTo(-d1));

			Assert.AreEqual(expectation, v1.IsParallelTo(v2));
			Assert.AreEqual(expectation, v2.IsParallelTo(v1));
			Assert.AreEqual(expectation, v1.IsParallelTo(-v2));
			Assert.AreEqual(expectation, v2.IsParallelTo(-v1));
			Assert.AreEqual(expectation, (-v1).IsParallelTo(v2));
			Assert.AreEqual(expectation, (-v2).IsParallelTo(v1));
			Assert.AreEqual(expectation, (-v1).IsParallelTo(-v2));
			Assert.AreEqual(expectation, (-v2).IsParallelTo(-v1));
		}
		void AssertCombination(bool expectation, Direction d1, Direction d2, Angle? tolerance) {
			var v1 = d1 * 10f;
			var v2 = d2 * 10f;

			if (tolerance == null) {
				Assert.AreEqual(expectation, v1.IsApproximatelyParallelTo(d2));
				Assert.AreEqual(expectation, v2.IsApproximatelyParallelTo(d1));
				Assert.AreEqual(expectation, v1.IsApproximatelyParallelTo(-d2));
				Assert.AreEqual(expectation, v2.IsApproximatelyParallelTo(-d1));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyParallelTo(d2));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyParallelTo(d1));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyParallelTo(-d2));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyParallelTo(-d1));

				Assert.AreEqual(expectation, v1.IsApproximatelyParallelTo(v2));
				Assert.AreEqual(expectation, v2.IsApproximatelyParallelTo(v1));
				Assert.AreEqual(expectation, v1.IsApproximatelyParallelTo(-v2));
				Assert.AreEqual(expectation, v2.IsApproximatelyParallelTo(-v1));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyParallelTo(v2));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyParallelTo(v1));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyParallelTo(-v2));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyParallelTo(-v1));
			}
			else {
				Assert.AreEqual(expectation, v1.IsApproximatelyParallelTo(d2, tolerance.Value));
				Assert.AreEqual(expectation, v2.IsApproximatelyParallelTo(d1, tolerance.Value));
				Assert.AreEqual(expectation, v1.IsApproximatelyParallelTo(-d2, tolerance.Value));
				Assert.AreEqual(expectation, v2.IsApproximatelyParallelTo(-d1, tolerance.Value));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyParallelTo(d2, tolerance.Value));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyParallelTo(d1, tolerance.Value));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyParallelTo(-d2, tolerance.Value));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyParallelTo(-d1, tolerance.Value));

				Assert.AreEqual(expectation, v1.IsApproximatelyParallelTo(v2, tolerance.Value));
				Assert.AreEqual(expectation, v2.IsApproximatelyParallelTo(v1, tolerance.Value));
				Assert.AreEqual(expectation, v1.IsApproximatelyParallelTo(-v2, tolerance.Value));
				Assert.AreEqual(expectation, v2.IsApproximatelyParallelTo(-v1, tolerance.Value));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyParallelTo(v2, tolerance.Value));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyParallelTo(v1, tolerance.Value));
				Assert.AreEqual(expectation, (-v1).IsApproximatelyParallelTo(-v2, tolerance.Value));
				Assert.AreEqual(expectation, (-v2).IsApproximatelyParallelTo(-v1, tolerance.Value));
			}
		}

		AssertCombination(false, Direction.Left, Direction.Down, null);
		AssertCombination(false, Direction.Left, Direction.Up, null);
		AssertCombination(true, Direction.Left, Direction.Right, null);
		AssertCombination(true, Direction.Forward, Direction.Backward, null);
		AssertCombination(true, Direction.Up, Direction.Up, null);
		AssertCombination(true, Direction.Down, Direction.Down, null);
		AssertCombination(false, Direction.Forward, Direction.None, null);
		AssertCombination(false, Direction.None, Direction.Right, null);
		AssertCombination(false, Direction.None, Direction.None, null);

		AssertCombination(false, Direction.Left, Direction.Down, 89f);
		AssertCombination(false, Direction.Left, Direction.Up, 89f);
		AssertCombination(true, Direction.Left, Direction.Down, 90f);
		AssertCombination(true, Direction.Left, Direction.Up, 90f);
		AssertCombination(true, Direction.Up, Direction.Up, 0f);
		AssertCombination(true, Direction.Down, Direction.Down, 0f);

		AssertCombinationExactly(false, Direction.Left, Direction.Down);
		AssertCombinationExactly(false, Direction.Left, Direction.None);

		var testList = new List<Direction>();
		for (var x = -3f; x <= 3f; x += 1f) {
			for (var y = -3f; y <= 3f; y += 1f) {
				for (var z = -3f; z <= 3f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			if (testList[i] != Direction.None) AssertCombinationExactly(true, testList[i], testList[i]);
			for (var j = i; j < testList.Count; ++j) {
				var a = testList[i];
				var b = testList[j];
				if (a == Direction.None || b == Direction.None) {
					AssertCombination(false, a, b, null);
					AssertCombination(false, a, b, 0f);
					AssertCombination(false, a, b, 45f);
					AssertCombination(false, a, b, 90f);
					AssertCombination(false, a, b, 180f);
					AssertCombinationExactly(false, a, b);
					continue;
				}

				var angle = (a ^ b) < (a ^ -b) ? (a ^ b) : (a ^ -b);
				if (angle == Angle.Zero) {
					AssertCombination(true, a, b, null);
					AssertCombination(true, a, b, 0f);
				}
				else {
					AssertCombination(true, a, b, angle + 0.1f);
					AssertCombination(false, a, b, angle - 0.1f);
				}
			}
		}
	}
}