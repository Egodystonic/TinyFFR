// Created on 2024-02-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class XYPairTest {
	const float TestTolerance = 0.001f;
	readonly XYPair<float> ThreeFourFloat = (3f, 4f);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyInstantiateStaticMembers() {
		Assert.AreEqual(0, XYPair<int>.Zero.X);
		Assert.AreEqual(0, XYPair<int>.Zero.Y);
		Assert.AreEqual(1, XYPair<int>.One.X);
		Assert.AreEqual(1, XYPair<int>.One.Y);
	}

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
					Assert.IsTrue(Angle.FromDegrees(f).EqualsWithinCircle(result.PolarAngle!.Value, toleranceDegrees: TestTolerance));
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
					Assert.IsTrue(orientation.ToPolarAngle()!.Value.EqualsWithinCircle(result.PolarAngle!.Value, toleranceDegrees: TestTolerance));
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

	[Test]
	public void ShouldCorrectlyAddAndSubtract() {
		Assert.AreEqual(XYPair<float>.Zero, XYPair<float>.Zero + XYPair<float>.Zero);
		Assert.AreEqual(XYPair<float>.Zero, XYPair<float>.Zero - XYPair<float>.Zero);
		Assert.AreEqual(ThreeFourFloat * 2f, ThreeFourFloat + ThreeFourFloat);
		Assert.AreEqual(ThreeFourFloat * 2, ThreeFourFloat + ThreeFourFloat);
		Assert.AreEqual(XYPair<float>.Zero, ThreeFourFloat - ThreeFourFloat);
		Assert.AreEqual(new XYPair<float>(2f, 4f), new XYPair<float>(-1f, -2f) + new XYPair<float>(3f, 6f));
		Assert.AreEqual(new XYPair<float>(-4f, -8f), new XYPair<float>(-1f, -2f) - new XYPair<float>(3f, 6f));
	}

	[Test]
	public void ShouldCorrectlyReverse() {
		Assert.AreEqual(XYPair<float>.Zero, -XYPair<float>.Zero);
		Assert.AreEqual(new XYPair<float>(-3f, -4f), -ThreeFourFloat);
		Assert.AreEqual(new XYPair<float>(-1f, -1f), new XYPair<float>(1f, 1f).Negated);
	}

	[Test]
	public void ShouldCorrectlyReciprocate() {
		Assert.AreEqual(null, XYPair<float>.Zero.Reciprocal);
		Assert.AreEqual(null, new XYPair<int>(0, 1).Reciprocal);
		Assert.AreEqual(null, new XYPair<int>(1, 0).Reciprocal);
		Assert.AreEqual(new XYPair<float>(1f / 3f, 1f / 4f), ThreeFourFloat.Reciprocal);
		Assert.AreEqual(new XYPair<float>(-1f / 3f, -1f / 4f), -ThreeFourFloat.Reciprocal);
	}

	[Test]
	public void ShouldCorrectlyCalculatePolarAngle() {
		for (var x = -1f; x <= 1.05f; x += 0.05f) {
			for (var y = -1f; y <= 1.05f; y += 0.05f) {
				Assert.AreEqual(new XYPair<float>(x, y).PolarAngle, Angle.From2DPolarAngle(x, y));
			}
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineAngleBetweenPairs() {
		void AssertPair(Angle expectation, Angle leftAngle, Angle rightAngle) {
			var left = XYPair<float>.FromPolarAngle(leftAngle);
			var right = XYPair<float>.FromPolarAngle(rightAngle);
			AssertToleranceEquals(expectation, left.AngleTo(right), TestTolerance);
			AssertToleranceEquals(expectation, left ^ right, TestTolerance);
			AssertToleranceEquals(expectation, right.AngleTo(left), TestTolerance);
			AssertToleranceEquals(expectation, right ^ left, TestTolerance);
		}

		AssertPair(0f, 0f, 0f);
		AssertPair(0f, 90f, 90f);
		AssertPair(0f, -90f, -90f);
		AssertPair(90f, 0f, 90f);
		AssertPair(90f, 0f, -90f);
		AssertPair(90f, 180f, 270f);
		AssertPair(90f, 270f, 180f);
		AssertPair(90f, -180f, -270f);
		AssertPair(90f, -270f, -180f);
		AssertPair(10f, 0f, 350f);
		AssertPair(10f, -350f, 0f);
		AssertPair(20f, -350f, 350f);
	}

	[Test]
	public void ShouldCorrectlyCalculateAbsolute() {
		Assert.AreEqual(3f, ThreeFourFloat.Absolute.X);
		Assert.AreEqual(4f, ThreeFourFloat.Absolute.Y);

		Assert.AreEqual(3f, new XYPair<float>(-3f, -4f).Absolute.X);
		Assert.AreEqual(4f, new XYPair<float>(-3f, -4f).Absolute.Y);
	}

	[Test]
	public void ShouldCorrectlyScale() {
		void AssertForType<T>() where T : unmanaged, INumber<T> {
			Assert.AreEqual(XYPair<T>.Zero, XYPair<T>.Zero.ScaledByReal(-10f));
			Assert.AreEqual(XYPair<T>.Zero, XYPair<T>.Zero.ScaledByReal(0f));
			Assert.AreEqual(XYPair<T>.Zero, XYPair<T>.Zero.ScaledByReal(10f));
			Assert.AreEqual(XYPair<T>.Zero, new XYPair<T>(T.CreateChecked(1), T.CreateChecked(2)).ScaledByReal(0f));
			AssertToleranceEquals(new XYPair<T>(T.CreateChecked(2), T.CreateChecked(4)), new XYPair<T>(T.CreateChecked(1), T.CreateChecked(2)).ScaledByReal(2f), TestTolerance);
			AssertToleranceEquals(new XYPair<T>(T.CreateChecked(-2), T.CreateChecked(-4)), new XYPair<T>(T.CreateChecked(1), T.CreateChecked(2)).ScaledByReal(-2f), TestTolerance);

			for (var x = -5; x <= 5; x += 1) {
				for (var y = -5; y <= 5; y += 1) {
					var v = new XYPair<T>(T.CreateChecked(x), T.CreateChecked(y));

					AssertToleranceEquals((v.Cast<float>() * x).Cast<T>(), v.ScaledByReal(x), TestTolerance);

					if (x == 0) continue;
					AssertToleranceEquals(new XYPair<T>(v.X / T.CreateChecked(x), v.Y / T.CreateChecked(x)), v / T.CreateChecked(x), TestTolerance);
					AssertToleranceEquals((v.Cast<float>() / x).CastWithRoundingIfNecessary<float, T>(), v.ScaledByReal(1f / x), TestTolerance);
				}
			}
		}

		AssertForType<int>();
		AssertForType<float>();
		AssertForType<long>();
		AssertForType<double>();
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		void AssertForType<T>() where T : unmanaged, INumber<T> {
			var nonZeroTestInput = new XYPair<T>(T.CreateSaturating(-200f), T.CreateSaturating(400f));

			AssertToleranceEquals(nonZeroTestInput, XYPair<T>.Interpolate(nonZeroTestInput, XYPair<T>.Zero, 0f), TestTolerance);
			AssertToleranceEquals(XYPair<T>.Zero, XYPair<T>.Interpolate(nonZeroTestInput, XYPair<T>.Zero, 1f), TestTolerance);
			AssertToleranceEquals(XYPair<T>.FromVector2(nonZeroTestInput.ToVector2() * 0.5f), XYPair<T>.Interpolate(nonZeroTestInput, XYPair<T>.Zero, 0.5f), TestTolerance);
			AssertToleranceEquals(XYPair<T>.FromVector2(nonZeroTestInput.ToVector2() * 2f), XYPair<T>.Interpolate(nonZeroTestInput, XYPair<T>.Zero, -1f), TestTolerance);
			AssertToleranceEquals(XYPair<T>.FromVector2(nonZeroTestInput.ToVector2() * -1f), XYPair<T>.Interpolate(nonZeroTestInput, XYPair<T>.Zero, 2f), TestTolerance);

			var testList = new List<XYPair<T>>();
			for (var x = -5f; x <= 5f; x += 1f) {
				for (var y = -5f; y <= 5f; y += 1f) {
					testList.Add(new(T.CreateSaturating(x * 100f), T.CreateSaturating(y * 100f)));
				}
			}
			for (var i = 0; i < testList.Count; ++i) {
				for (var j = i; j < testList.Count; ++j) {
					var start = testList[i];
					var end = testList[j];

					for (var f = -1f; f <= 2f; f += 0.1f) {
						AssertToleranceEquals(
							new(
								T.CreateSaturating(Single.CreateSaturating(end.X - start.X) * f + Single.CreateSaturating(start.X)),
								T.CreateSaturating(Single.CreateSaturating(end.Y - start.Y) * f + Single.CreateSaturating(start.Y))
							),
							XYPair<T>.Interpolate(start, end, f),
							TestTolerance + 1
						);
					}
				}
			}
		}

		AssertForType<int>();
		AssertForType<float>();
		AssertForType<long>();
		AssertForType<double>();
	}

	[Test]
	public void ShouldCorrectlyCreateNonBoundedRandomValues() {
		const int NumIterations = 10_000;

		void AssertForType<T>() where T : unmanaged, INumber<T> {
			for (var i = 0; i < NumIterations; ++i) {
				var val = XYPair<T>.Random();
				Assert.GreaterOrEqual(val.X, T.CreateChecked(-XYPair<T>.DefaultRandomRange));
				Assert.GreaterOrEqual(val.Y, T.CreateChecked(-XYPair<T>.DefaultRandomRange));
				Assert.LessOrEqual(val.X, T.CreateChecked(XYPair<T>.DefaultRandomRange));
				Assert.LessOrEqual(val.Y, T.CreateChecked(XYPair<T>.DefaultRandomRange));
			}
		}

		AssertForType<int>();
		AssertForType<float>();
		AssertForType<long>();
		AssertForType<double>();
	}

	[Test]
	public void ShouldCorrectlyCreateBoundedRandomValues() {
		const int NumIterations = 10_000;

		void AssertForType<T>() where T : unmanaged, INumber<T> {
			for (var i = 0; i < NumIterations; ++i) {
				var val = XYPair<T>.Random(T.CreateChecked(-1000), T.CreateChecked(1000));
				Assert.GreaterOrEqual(val.X, T.CreateChecked(-1000));
				Assert.GreaterOrEqual(val.Y, T.CreateChecked(-1000));
				Assert.LessOrEqual(val.X, T.CreateChecked(1000));
				Assert.LessOrEqual(val.Y, T.CreateChecked(1000));

				val = XYPair<T>.Random((T.CreateChecked(-500), T.CreateChecked(-200)), (T.CreateChecked(500), T.CreateChecked(200)));
				Assert.GreaterOrEqual(val.X, T.CreateChecked(-500));
				Assert.GreaterOrEqual(val.Y, T.CreateChecked(-200));
				Assert.LessOrEqual(val.X, T.CreateChecked(500));
				Assert.LessOrEqual(val.Y, T.CreateChecked(200));
			}
		}

		AssertForType<int>();
		AssertForType<float>();
		AssertForType<long>();
		AssertForType<double>();
	}

	[Test]
	public void ShouldCorrectlyCalculateLength() {
		Assert.AreEqual(MathF.Sqrt(9f + 16f), ThreeFourFloat.Length, TestTolerance);
		Assert.AreEqual(9f + 16f, ThreeFourFloat.LengthSquared, TestTolerance);

		Assert.AreEqual(MathF.Sqrt(9f + 16f), ThreeFourFloat.Cast<int>().Length);
		Assert.AreEqual(9f + 16f, ThreeFourFloat.Cast<int>().LengthSquared);
	}

	[Test]
	public void ShouldCorrectlyCalculateDistance() {
		Assert.AreEqual(10f, ThreeFourFloat.DistanceFrom(-ThreeFourFloat), TestTolerance);
		Assert.AreEqual(5f, ThreeFourFloat.DistanceFrom(default(XYPair<float>)), TestTolerance);
		Assert.AreEqual(5f, ThreeFourFloat.Negated.DistanceFrom(default(XYPair<float>)), TestTolerance);

		Assert.AreEqual(100f, ThreeFourFloat.DistanceSquaredFrom(-ThreeFourFloat), TestTolerance);
		Assert.AreEqual(25f, ThreeFourFloat.DistanceSquaredFrom(default(XYPair<float>)), TestTolerance);
		Assert.AreEqual(25f, ThreeFourFloat.Negated.DistanceSquaredFrom(default(XYPair<float>)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCast() {
		Assert.AreEqual(3, ThreeFourFloat.Cast<int>().X);
		Assert.AreEqual(4, ThreeFourFloat.Cast<int>().Y);
		Assert.AreEqual(3f, new XYPair<int>(3, 4).Cast<float>().X);
		Assert.AreEqual(4f, new XYPair<int>(3, 4).Cast<float>().Y);
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		Assert.AreEqual(ThreeFourFloat, ThreeFourFloat.Clamp((2f, 3f), (4f, 5f)));
		Assert.AreEqual(ThreeFourFloat, ThreeFourFloat.Clamp((4f, 5f), (2f, 3f)));
		Assert.AreEqual(ThreeFourFloat, ThreeFourFloat.Clamp((3f, 4f), (3f, 4f)));

		Assert.AreEqual(new XYPair<float>(2f, 5f), new XYPair<float>(1f, 6f).Clamp((2f, 3f), (4f, 5f)));
		Assert.AreEqual(new XYPair<float>(4f, 3f), new XYPair<float>(5f, 2f).Clamp((2f, 3f), (4f, 5f)));
		Assert.AreEqual(new XYPair<float>(2f, 5f), new XYPair<float>(1f, 6f).Clamp((4f, 3f), (2f, 5f)));
		Assert.AreEqual(new XYPair<float>(4f, 3f), new XYPair<float>(5f, 2f).Clamp((4f, 3f), (2f, 5f)));
	}

	[Test]
	public void ShouldCorrectlyNormalize() {
		Assert.AreEqual(1f, ThreeFourFloat.WithLengthOne().Length, TestTolerance);
		Assert.AreEqual(1f, (-ThreeFourFloat).WithLengthOne().Length, TestTolerance);
		Assert.AreEqual(XYPair<float>.Zero, XYPair<float>.Zero.WithLengthOne());
	}

	[Test]
	public void ShouldCorrectlyImplementLineGeometry() {
		AssertToleranceEquals((1f, 0f), new XYPair<float>(0f, 0f).ClosestPointOn2DLine((1f, 0f), (0f, 1f)), TestTolerance);
		AssertToleranceEquals((-1f, 0f), new XYPair<float>(0f, 0f).ClosestPointOn2DLine((-1f, 0f), (0f, 1f)), TestTolerance);
		AssertToleranceEquals((1f, 9f), new XYPair<float>(-10f, 9f).ClosestPointOn2DLine((1f, 0f), (0f, 1f)), TestTolerance);

		AssertToleranceEquals((1f, 0f), new XYPair<float>(0f, 0f).ClosestPointOn2DBoundedRay((1f, -100f), (1f, 100f)), TestTolerance);
		AssertToleranceEquals((-1f, 0f), new XYPair<float>(0f, 0f).ClosestPointOn2DBoundedRay((-1f, 100f), (-1f, -100f)), TestTolerance);
		AssertToleranceEquals((1f, 9f), new XYPair<float>(-10f, 9f).ClosestPointOn2DBoundedRay((1f, -100f), (1f, 100f)), TestTolerance);
		
		AssertToleranceEquals((1f, -50f), new XYPair<float>(0f, 0f).ClosestPointOn2DBoundedRay((1f, -100f), (1f, -50f)), TestTolerance);
		AssertToleranceEquals((-1f, 50f), new XYPair<float>(0f, 0f).ClosestPointOn2DBoundedRay((-1f, 100f), (-1f, 50f)), TestTolerance);
		AssertToleranceEquals((0f, 0f), new XYPair<float>(0f, 0f).ClosestPointOn2DBoundedRay((-1f, -1f), (1f, 1f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyRound() {
		Assert.AreEqual(new XYPair<float>(2f, 0f), new XYPair<float>(1.5f, -0.5f).Round<float, float>());
		Assert.AreEqual(new XYPair<float>(2f, 0f), new XYPair<float>(1.5f, -0.5f).Round<float, float>(0, MidpointRounding.ToEven));
		Assert.AreEqual(new XYPair<float>(2f, -1f), new XYPair<float>(1.5f, -0.5f).Round<float, float>(0, MidpointRounding.AwayFromZero));
		Assert.AreEqual(new XYPair<float>(1.5f, -0.5f), new XYPair<float>(1.5f, -0.5f).Round<float, float>(1));
		Assert.AreEqual(new XYPair<float>(1.2f, -0.7f), new XYPair<float>(1.234f, -0.678f).Round<float, float>(1));
		Assert.AreEqual(new XYPair<float>(1.23f, -0.68f), new XYPair<float>(1.234f, -0.678f).Round<float, float>(2));
		Assert.AreEqual(new XYPair<float>(1.2f, -0.0f), new XYPair<float>(1.15f, -0.05f).Round<float, float>(1, MidpointRounding.ToEven));
		Assert.AreEqual(new XYPair<float>(1.2f, -0.1f), new XYPair<float>(1.15f, -0.05f).Round<float, float>(1, MidpointRounding.AwayFromZero));

		Assert.AreEqual(new XYPair<int>(2, 0), new XYPair<float>(1.5f, -0.5f).Round<float, int>());
		Assert.AreEqual(new XYPair<int>(2, 0), new XYPair<float>(1.5f, -0.5f).Round<float, int>(MidpointRounding.ToEven));
		Assert.AreEqual(new XYPair<int>(2, -1), new XYPair<float>(1.5f, -0.5f).Round<float, int>(MidpointRounding.AwayFromZero));

		Assert.AreEqual(new XYPair<int>(2, 0), new XYPair<float>(1.5f, -0.5f).CastWithRoundingIfNecessary<float, int>());
		Assert.AreEqual(new XYPair<int>(2, 0), new XYPair<float>(1.5f, -0.5f).CastWithRoundingIfNecessary<float, int>(MidpointRounding.ToEven));
		Assert.AreEqual(new XYPair<int>(2, -1), new XYPair<float>(1.5f, -0.5f).CastWithRoundingIfNecessary<float, int>(MidpointRounding.AwayFromZero));
		Assert.AreEqual(new XYPair<float>(1.5f, -0.5f), new XYPair<float>(1.5f, -0.5f).CastWithRoundingIfNecessary<float, float>());
		Assert.AreEqual(new XYPair<float>(1.5f, -0.5f), new XYPair<float>(1.5f, -0.5f).CastWithRoundingIfNecessary<float, float>(MidpointRounding.ToEven));
		Assert.AreEqual(new XYPair<float>(1.5f, -0.5f), new XYPair<float>(1.5f, -0.5f).CastWithRoundingIfNecessary<float, float>(MidpointRounding.AwayFromZero));
	}
}