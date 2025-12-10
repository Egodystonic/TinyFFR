// Created on 2024-11-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class Transform2DTest {
	const float TestTolerance = 0.001f;
	static readonly Transform2D TestTransform = new(
		translation: new(1f, 2f),
		rotation: 90f,
		scaling: new(0.75f, 0.5f)
	);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlySetPublicStaticMembers() {
		Assert.AreEqual(XYPair<float>.Zero, Transform2D.None.Translation);
		Assert.AreEqual(Angle.Zero, Transform2D.None.Rotation);
		Assert.AreEqual(XYPair<float>.One, Transform2D.None.Scaling);
	}

	[Test]
	public void ShouldCorrectlyImplementProperties() {
		Assert.AreEqual(new XYPair<float>(1f, 2f), TestTransform.Translation);
		Assert.AreEqual(new Angle(90f), TestTransform.Rotation);
		Assert.AreEqual(new XYPair<float>(0.75f, 0.5f), TestTransform.Scaling);

		var transform = TestTransform with {
			Translation = new(2f, 3f)
		};

		Assert.AreEqual(new XYPair<float>(2f, 3f), transform.Translation);
		Assert.AreEqual(new Angle(90f), TestTransform.Rotation);
		Assert.AreEqual(new XYPair<float>(0.75f, 0.5f), TestTransform.Scaling);

		transform = transform with {
			Rotation = 30f
		};

		Assert.AreEqual(new XYPair<float>(2f, 3f), transform.Translation);
		Assert.AreEqual(new Angle(30f), transform.Rotation);
		Assert.AreEqual(new XYPair<float>(0.75f, 0.5f), TestTransform.Scaling);

		transform = transform with {
			Scaling = new(1.1f, 1.2f)
		};

		Assert.AreEqual(new XYPair<float>(2f, 3f), transform.Translation);
		Assert.AreEqual(new Angle(30f), transform.Rotation);
		Assert.AreEqual(new XYPair<float>(1.1f, 1.2f), transform.Scaling);
	}

	[Test]
	public void ShouldCorrectlyConstruct() {
		Assert.AreEqual(Transform2D.None, new Transform2D());

		Assert.AreEqual(
			Transform2D.None with { Translation = new(1f, 2f) },
			new Transform2D(1f, 2f)
		);

		Assert.AreEqual(
			Transform2D.None with { Translation = new(1f, 2f) },
			new Transform2D(translation: new(1f, 2f))
		);
		Assert.AreEqual(
			Transform2D.None with { Rotation = 45f },
			new Transform2D(rotation: 45f)
		);
		Assert.AreEqual(
			Transform2D.None with { Scaling = new(2f) },
			new Transform2D(scaling: new(2f))
		);

		Assert.AreEqual(
			TestTransform, 
			new Transform2D(new(1f, 2f), 90f, new(0.75f, 0.5f))
		);
	}

	[Test]
	public void ShouldCorrectlyConvertToMatrix() {
		void AssertMat(Matrix3x2 expectation, Transform2D transform) {
			AssertToleranceEquals(expectation, transform.ToMatrix(), TestTolerance);
			Matrix3x2 actual = new();
			transform.ToMatrix(ref actual);
			AssertToleranceEquals(expectation, actual, TestTolerance);
		}

		AssertMat(Matrix3x2.Identity, Transform2D.None);

		AssertMat(
			new Matrix3x2(
				1f, 0f,
				0f, 1f,
				1f, 2f
			),
			new Transform2D(translation: new(1f, 2f))
		);

		var rotAngle = new Angle(60f);
		var (sin, cos) = MathF.SinCos(rotAngle.Radians);
		AssertMat(
			new Matrix3x2(
				cos, -sin, 
				sin, cos, 
				0f, 0f
			),
			new Transform2D(rotation: rotAngle)
		);

		AssertMat(
			new Matrix3x2(
				2f, 0f,
				0f, 3f,
				0f, 0f
			),
			new Transform2D(scaling: new(2f, 3f))
		);

		AssertMat(
			new Matrix3x2(
				cos * 2f, -sin * 3f,
				sin * 2f, cos * 3f,
				1f, 2f
			),
			new Transform2D(
				translation: new(1f, 2f),
				rotation: 60f,
				scaling: new(2f, 3f)
			)
		);
	}

	[Test]
	public void ShouldCorrectlyConvertToTuple() {
		var (t, r, s) = TestTransform;

		Assert.AreEqual(new XYPair<float>(1f, 2f), t);
		Assert.AreEqual((Angle) 90f, r);
		Assert.AreEqual(new XYPair<float>(0.75f, 0.5f), s);
	}

	[Test]
	public void ShouldCorrectlyGenerateRandomTransforms() {
		const int NumIterations = 10_000;

		static void AssertTranslationAndScaling(Transform2D min, Transform2D max, Transform2D actual) {
			Assert.GreaterOrEqual(actual.Translation.X, min.Translation.X);
			Assert.GreaterOrEqual(actual.Translation.Y, min.Translation.Y);
			Assert.LessOrEqual(actual.Translation.X, max.Translation.X);
			Assert.LessOrEqual(actual.Translation.Y, max.Translation.Y);
			
			Assert.GreaterOrEqual(actual.Scaling.X, min.Scaling.X);
			Assert.GreaterOrEqual(actual.Scaling.Y, min.Scaling.Y);
			Assert.LessOrEqual(actual.Scaling.X, max.Scaling.X);
			Assert.LessOrEqual(actual.Scaling.Y, max.Scaling.Y);
			}

		var min = new Transform2D(
			new XYPair<float>(-5f, -3f),
			90f,
			new XYPair<float>(0.5f, 0.7f)
		);
		var max = new Transform2D(
			new XYPair<float>(5f, 3f),
			45f,
			new XYPair<float>(1.1f, 1.3f)
		);

		for (var i = 0; i < NumIterations; ++i) {
			AssertTranslationAndScaling(
				new(new XYPair<float>(-XYPair<float>.DefaultRandomRange), Angle.Zero, new XYPair<float>(-1f)),
				new(new XYPair<float>(XYPair<float>.DefaultRandomRange), Angle.Zero, new XYPair<float>(1f)),
				Transform2D.Random()
			);
			AssertTranslationAndScaling(
				min,
				max,
				Transform2D.Random(min, max)
			);
			AssertTranslationAndScaling(
				min,
				max,
				Transform2D.Random(max, min)
			);
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<Transform2D>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(Transform2D.None, TestTransform);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(Transform2D.None, 0f, 0f, 0f, 1f, 1f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestTransform, 1f, 2f, Angle.QuarterCircle.Radians, 0.75f, 0.5f);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(Transform2D input, string expectedValue) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N2";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);

			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(Transform2D.None, "Transform2D[Translation <0.00, 0.00> | Rotation 0.00° | Scaling <1.00, 1.00>]");
		AssertIteration(TestTransform, "Transform2D[Translation <1.00, 2.00> | Rotation 90.00° | Scaling <0.75, 0.50>]");
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(Transform2D input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			Transform2D input,
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

		AssertFail(Transform2D.None, Array.Empty<char>(), "N0", null);
		AssertFail(Transform2D.None, new char[76], "N2", null);
		AssertSuccess(Transform2D.None, new char[77], "N2", null, "Transform2D[Translation <0.00, 0.00> | Rotation 0.00° | Scaling <1.00, 1.00>]");
		AssertFail(TestTransform, new char[76 - 14], "N0", null);
		AssertSuccess(TestTransform, new char[77 - 14], "N0", null, "Transform2D[Translation <1, 2> | Rotation 90° | Scaling <1, 0>]");
		AssertFail(TestTransform, new char[76 - 4], "N1", null);
		AssertSuccess(TestTransform, new char[77 - 4], "N1", null, "Transform2D[Translation <1.0, 2.0> | Rotation 90.0° | Scaling <0.8, 0.5>]");
		AssertSuccess(TestTransform, new char[77 - 4], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "Transform2D[Translation <1,0. 2,0> | Rotation 90,0° | Scaling <0,8. 0,5>]");
		AssertSuccess(TestTransform, new char[77 + 6], "N3", null, "Transform2D[Translation <1.000, 2.000> | Rotation 90.000° | Scaling <0.750, 0.500>]");
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Transform2D[Translation <1.00, 2.00> | Rotation 90.00° | Scaling <0.75, 0.50>]";

		Assert.AreEqual(TestTransform, Transform2D.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, Transform2D.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestTransform, result);
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreEqual(Transform2D.None, new Transform2D(-0f, -0f));
		Assert.AreNotEqual(Transform2D.None, TestTransform);
		Assert.IsTrue(TestTransform.Equals(TestTransform));
		Assert.IsFalse(TestTransform.Equals(Transform2D.None));
		Assert.IsTrue(TestTransform == new Transform2D(new(1f, 2f), 90f, new(0.75f, 0.5f)));
		Assert.IsFalse(Transform2D.None == TestTransform);
		Assert.IsFalse(Transform2D.None != new Transform2D(0f, 0f));
		Assert.IsTrue(TestTransform != Transform2D.None);
		Assert.IsTrue(TestTransform != TestTransform with { Translation = new(1f) });
		Assert.IsTrue(TestTransform != TestTransform with { Scaling = new(1f) });
		Assert.IsTrue(TestTransform != TestTransform with { Rotation = Angle.Zero });

		Assert.IsTrue(Transform2D.None.Equals(Transform2D.None, 0f));
		Assert.IsTrue(TestTransform.Equals(TestTransform, 0f));
		Assert.IsTrue(
			new Transform2D(new(0.5f, 0.6f), 90f, new(1.1f, 1.2f))
			.Equals(
				new Transform2D(new(0.51f, 0.61f), 90.01f, new(1.11f, 1.21f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform2D(new(0.5f, 0.6f), 90f, new(1.1f, 1.2f))
			.Equals(
				new Transform2D(new(0.6f, 0.6f), 90f, new(1.1f, 1.2f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform2D(new(0.5f, 0.6f), 90f, new(1.1f, 1.2f))
			.Equals(
				new Transform2D(new(0.5f, 0.7f), 90f, new(1.1f, 1.2f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform2D(new(0.5f, 0.6f), 90f, new(1.1f, 1.2f))
			.Equals(
				new Transform2D(new(0.5f, 0.7f), 90f, new(1.1f, 1.2f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform2D(new(0.5f, 0.6f), 90f, new(1.1f, 1.2f))
			.Equals(
				new Transform2D(new(0.5f, 0.6f), 80f, new(1.1f, 1.2f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform2D(new(0.5f, 0.6f), 90f, new(1.1f, 1.2f))
			.Equals(
				new Transform2D(new(0.5f, 0.6f), 90f, new(1.2f, 1.2f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform2D(new(0.5f, 0.6f), 90f, new(1.1f, 1.2f))
			.Equals(
				new Transform2D(new(0.5f, 0.6f), 90f, new(1.1f, 1.3f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform2D(new(0.5f, 0.6f), 90f, new(1.1f, 1.2f))
			.Equals(
				new Transform2D(new(0.5f, 0.6f), 90f, new(1.1f, 1.3f))
				, 0.05f
			)
		);
	}

	[Test]
	public void ShouldCorrectlyInvert() {
		Assert.AreEqual(Transform2D.None, Transform2D.None.Inverse);
		AssertToleranceEquals(new(-1f, -2f), TestTransform.Inverse.Translation, TestTolerance);
		AssertToleranceEquals(-90f, TestTransform.Inverse.Rotation, TestTolerance);
		AssertToleranceEquals(new(1f / 0.75f, 2f), TestTransform.Inverse.Scaling, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCombineScaling() {
		Assert.AreEqual(new XYPair<float>(1.75f, 1.5f), TestTransform.WithScalingAdjustedBy(1f).Scaling);
		Assert.AreEqual(new XYPair<float>(-0.25f, -0.5f), TestTransform.WithScalingAdjustedBy(-1f).Scaling);

		Assert.AreEqual(new XYPair<float>(1.75f, 2.5f), TestTransform.WithScalingAdjustedBy((1f, 2f)).Scaling);
		Assert.AreEqual(new XYPair<float>(-0.25f, -1.5f), TestTransform.WithScalingAdjustedBy((-1f, -2f)).Scaling);

		Assert.AreEqual(new XYPair<float>(0.75f, 0.5f), TestTransform.WithScalingMultipliedBy(1f).Scaling);
		Assert.AreEqual(new XYPair<float>(-0.75f, -0.5f), TestTransform.WithScalingMultipliedBy(-1f).Scaling);
		Assert.AreEqual(new XYPair<float>(1.5f, 1f), TestTransform.WithScalingMultipliedBy(2f).Scaling);
		Assert.AreEqual(new XYPair<float>(0.375f, 0.25f), TestTransform.WithScalingMultipliedBy(0.5f).Scaling);

		Assert.AreEqual(new XYPair<float>(0.75f, 1f), TestTransform.WithScalingMultipliedBy((1f, 2f)).Scaling);
		Assert.AreEqual(new XYPair<float>(-0.75f, -1f), TestTransform.WithScalingMultipliedBy((-1f, -2f)).Scaling);
	}

	[Test]
	public void ShouldCorrectlyCombineRotations() {
		AssertToleranceEquals(90f, TestTransform.WithAdditionalRotation(0f).Rotation, TestTolerance);
		AssertToleranceEquals(100f, TestTransform.WithAdditionalRotation(10f).Rotation, TestTolerance);
		AssertToleranceEquals(80f, TestTransform.WithAdditionalRotation(-10f).Rotation, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCombineTranslations() {
		Assert.AreEqual(TestTransform.Translation, TestTransform.WithAdditionalTranslation(XYPair<float>.Zero).Translation);
		Assert.AreEqual(new XYPair<float>(2f, 0f), TestTransform.WithAdditionalTranslation(new(1f, -2f)).Translation);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = TestTransform;
		var end = new Transform2D(
			new XYPair<float>(2f, 4f),
			50f,
			new XYPair<float>(0.55f, 1f)
		);

		AssertToleranceEquals(
			start,
			Transform2D.Interpolate(start, end, 0f),
			TestTolerance
		);
		AssertToleranceEquals(
			end,
			Transform2D.Interpolate(start, end, 1f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Transform2D(
				new XYPair<float>(1.5f, 3f),
				70f,
				new XYPair<float>(0.65f, 0.75f)
			),
			Transform2D.Interpolate(start, end, 0.5f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Transform2D(
				new XYPair<float>(3f, 6f),
				10f,
				new XYPair<float>(0.35f, 1.5f)
			),
			Transform2D.Interpolate(start, end, 2f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Transform2D(
				new XYPair<float>(0f, 0f),
				130f,
				new XYPair<float>(0.95f, 0f)
			),
			Transform2D.Interpolate(start, end, -1f),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = TestTransform;
		var max = TestTransform
			.WithAdditionalTranslation((3f, -4f))
			.WithAdditionalRotation(30f)
			.WithScalingAdjustedBy(2f);

		void AssertInput(Transform2D expectation, Transform2D input) {
			AssertToleranceEquals(expectation, input.Clamp(min, max), TestTolerance);
			AssertToleranceEquals(expectation, input.Clamp(max, min), TestTolerance);
		}

		AssertInput(
			min, 
			min
		);
		AssertInput(
			max,
			max
		);
		AssertInput(
			Transform2D.Interpolate(min, max, 0.5f),
			Transform2D.Interpolate(min, max, 0.5f)
		);
		AssertInput(
			min,
			Transform2D.Interpolate(min, max, -1f)
		);
		AssertInput(
			max,
			Transform2D.Interpolate(min, max, 1.5f)
		);
	}

	[Test]
	public void ShouldCorrectlyConvertTo3DTransform() {
		AssertToleranceEquals(
			new Transform(
				translation: (1f, 2f, 0f),
				rotation: 90f % Direction.Forward,
				scaling: (0.75f, 0.5f, 1f)
			),
			TestTransform.To3D(new DimensionConverter((1f, 0f, 0f), (0f, 1f, 0f), (0f, 0f, 1f), Location.Origin)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyApply() {
		for (var i = 0; i < 100; ++i) {
			var v = XYPair<float>.Random();
			Assert.AreEqual(
				v.TransformedBy(TestTransform),
				TestTransform.AppliedTo(v)
			);
		}
	}
}