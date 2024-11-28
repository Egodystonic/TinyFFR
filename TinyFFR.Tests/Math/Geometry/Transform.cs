// Created on 2024-11-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class TransformTest {
	const float TestTolerance = 0.001f;
	static readonly Transform TestTransform = new(
		translation: new(1f, 2f, 3f),
		rotation: 90f % Direction.Down,
		scaling: new(0.75f, 0.5f, 0.25f)
	);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlySetPublicStaticMembers() {
		Assert.AreEqual(Vect.Zero, Transform.None.Translation);
		Assert.AreEqual(Rotation.None, Transform.None.Rotation);
		Assert.AreEqual(Vect.One, Transform.None.Scaling);
	}

	[Test]
	public void ShouldCorrectlyImplementProperties() {
		Assert.AreEqual(new Vect(1f, 2f, 3f), TestTransform.Translation);
		Assert.AreEqual(new Rotation(90f, Direction.Down), TestTransform.Rotation);
		Assert.AreEqual(new Vect(0.75f, 0.5f, 0.25f), TestTransform.Scaling);

		var transform = TestTransform with {
			Translation = new(2f, 3f, 4f)
		};

		Assert.AreEqual(new Vect(2f, 3f, 4f), transform.Translation);
		Assert.AreEqual(new Rotation(90f, Direction.Down), transform.Rotation);
		Assert.AreEqual(new Vect(0.75f, 0.5f, 0.25f), transform.Scaling);

		transform = transform with {
			Rotation = new Rotation(30f, Direction.Left)
		};

		Assert.AreEqual(new Vect(2f, 3f, 4f), transform.Translation);
		Assert.AreEqual(new Rotation(30f, Direction.Left), transform.Rotation);
		Assert.AreEqual(new Vect(0.75f, 0.5f, 0.25f), transform.Scaling);

		transform = transform with {
			Scaling = new Vect(1.1f, 1.2f, 1.3f)
		};

		Assert.AreEqual(new Vect(2f, 3f, 4f), transform.Translation);
		Assert.AreEqual(new Rotation(30f, Direction.Left), transform.Rotation);
		Assert.AreEqual(new Vect(1.1f, 1.2f, 1.3f), transform.Scaling);
	}

	[Test]
	public void ShouldCorrectlyConstruct() {
		Assert.AreEqual(Transform.None, new Transform());

		Assert.AreEqual(
			Transform.None with { Translation = new(1f, 2f, 3f) },
			new Transform(1f, 2f, 3f)
		);

		Assert.AreEqual(
			Transform.None with { Translation = new(1f, 2f, 3f) },
			new Transform(translation: new(1f, 2f, 3f))
		);
		Assert.AreEqual(
			Transform.None with { Rotation = 45f % Direction.Right },
			new Transform(rotation: 45f % Direction.Right)
		);
		Assert.AreEqual(
			Transform.None with { Scaling = new(2f) },
			new Transform(scaling: new(2f))
		);

		Assert.AreEqual(
			TestTransform, 
			new Transform(new(1f, 2f, 3f), 90f % Direction.Down, new(0.75f, 0.5f, 0.25f))
		);
	}

	[Test]
	public void ShouldCorrectlyConvertToMatrix() {
		void AssertMat(Matrix4x4 expectation, Transform transform) {
			AssertToleranceEquals(expectation, transform.ToMatrix(), TestTolerance);
			transform.ToMatrix(out var actual);
			AssertToleranceEquals(expectation, actual, TestTolerance);
		}

		AssertMat(Matrix4x4.Identity, Transform.None);
		
		AssertMat(
			new Matrix4x4(
				1f,		0f,		0f,		0f,
				0f,		1f,		0f,		0f,
				0f,		0f,		1f,		0f,
				1f,		2f,		3f,		1f
			),
			new Transform(translation: new(1f, 2f, 3f))
		);

		var rotVect = (90f % Direction.Down).AsVector4;
		var rotVectSquared = rotVect * rotVect;
		AssertMat(
			new Matrix4x4(
				1f - 2f * rotVectSquared.Y - 2f * rotVectSquared.Z,
				2f * rotVect.X * rotVect.Y + 2f * rotVect.Z * rotVect.W,
				2f * rotVect.X * rotVect.Z - 2f * rotVect.Y * rotVect.W,
				0f,

				2f * rotVect.X * rotVect.Y - 2f * rotVect.Z * rotVect.W,
				1f - 2f * rotVectSquared.X - 2f * rotVectSquared.Z,
				2f * rotVect.Y * rotVect.Z + 2f * rotVect.X * rotVect.W,
				0f,

				2f * rotVect.X * rotVect.Z + 2f * rotVect.Y * rotVect.W,
				2f * rotVect.Y * rotVect.Z - 2f * rotVect.X * rotVect.W,
				1f - 2f * rotVectSquared.X - 2f * rotVectSquared.Y,
				0f,

				0f, 0f, 0f, 1f
			),
			new Transform(rotation: 90f % Direction.Down)
		);

		AssertMat(
			new Matrix4x4(
				2f, 0f, 0f, 0f,
				0f, 3f, 0f, 0f,
				0f, 0f, 4f, 0f,
				0f, 0f, 0f, 1f
			),
			new Transform(scaling: new(2f, 3f, 4f))
		);

		AssertMat(
			new Matrix4x4(
				(1f - 2f * rotVectSquared.Y - 2f * rotVectSquared.Z) * 2f,
				(2f * rotVect.X * rotVect.Y + 2f * rotVect.Z * rotVect.W) * 2f,
				(2f * rotVect.X * rotVect.Z - 2f * rotVect.Y * rotVect.W) * 2f,
				0f,

				(2f * rotVect.X * rotVect.Y - 2f * rotVect.Z * rotVect.W) * 3f,
				(1f - 2f * rotVectSquared.X - 2f * rotVectSquared.Z) * 3f,
				(2f * rotVect.Y * rotVect.Z + 2f * rotVect.X * rotVect.W) * 3f,
				0f,

				(2f * rotVect.X * rotVect.Z + 2f * rotVect.Y * rotVect.W) * 4f,
				(2f * rotVect.Y * rotVect.Z - 2f * rotVect.X * rotVect.W) * 4f,
				(1f - 2f * rotVectSquared.X - 2f * rotVectSquared.Y) * 4f,
				0f,

				1f, 2f, 3f, 1f
			),
			new Transform(
				translation: new(1f, 2f, 3f),
				rotation: 90f % Direction.Down,
				scaling: new(2f, 3f, 4f)
			)
		);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromTuple() {
		var (t, r, s) = TestTransform;

		Assert.AreEqual(new Vect(1f, 2f, 3f), t);
		Assert.AreEqual(90f % Direction.Down, r);
		Assert.AreEqual(new Vect(0.75f, 0.5f, 0.25f), s);

		Assert.AreEqual(new Transform(t), (Transform) t);
		Assert.AreEqual(new Transform(t, r), (Transform) (t, r));
		Assert.AreEqual(TestTransform, (Transform) (t, r, s));
	}

	[Test]
	public void ShouldCorrectlyGenerateRandomTransforms() {
		const int NumIterations = 10_000;

		// TODO check for rotation
		static void AssertTranslationAndScaling(Transform min, Transform max, Transform actual) {
			Assert.GreaterOrEqual(actual.Translation.X, min.Translation.X);
			Assert.GreaterOrEqual(actual.Translation.Y, min.Translation.Y);
			Assert.GreaterOrEqual(actual.Translation.Z, min.Translation.Z);
			Assert.LessOrEqual(actual.Translation.X, max.Translation.X);
			Assert.LessOrEqual(actual.Translation.Y, max.Translation.Y);
			Assert.LessOrEqual(actual.Translation.Z, max.Translation.Z);

			Assert.GreaterOrEqual(actual.Scaling.X, min.Scaling.X);
			Assert.GreaterOrEqual(actual.Scaling.Y, min.Scaling.Y);
			Assert.GreaterOrEqual(actual.Scaling.Z, min.Scaling.Z);
			Assert.LessOrEqual(actual.Scaling.X, max.Scaling.X);
			Assert.LessOrEqual(actual.Scaling.Y, max.Scaling.Y);
			Assert.LessOrEqual(actual.Scaling.Z, max.Scaling.Z);
		}

		var min = new Transform(
			new Vect(-5f, -3f, -1f),
			90f % Direction.Left,
			new Vect(0.5f, 0.7f, 0.9f)
		);
		var max = new Transform(
			new Vect(5f, 3f, 1f),
			45f % Direction.Left,
			new Vect(1.1f, 1.3f, 1.5f)
		);

		for (var i = 0; i < NumIterations; ++i) {
			AssertTranslationAndScaling(
				(new Vect(-Vect.DefaultRandomRange), Rotation.None, new Vect(-1f)),
				(new Vect(Vect.DefaultRandomRange), Rotation.None, new Vect(1f)),
				Transform.Random()
			);
			AssertTranslationAndScaling(
				min,
				max,
				Transform.Random(min, max)
			);
			AssertTranslationAndScaling(
				min,
				max,
				Transform.Random(max, min)
			);
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<Transform>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(Transform.None, TestTransform);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(Transform.None, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestTransform, 1f, 2f, 3f, 0f, -0.70710677f, 0f, 0.70710677f, 0.75f, 0.5f, 0.25f);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(Transform input, string expectedValue) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N2";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);

			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(Transform.None, "Transform[Translation <0.00, 0.00, 0.00> | Rotation 0.00° around <0.00, 0.00, 0.00> | Scaling <1.00, 1.00, 1.00>]");
		AssertIteration(TestTransform, "Transform[Translation <1.00, 2.00, 3.00> | Rotation 90.00° around <0.00, -1.00, 0.00> | Scaling <0.75, 0.50, 0.25>]");
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(Transform input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			Transform input,
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

		AssertFail(Transform.None, Array.Empty<char>(), "N0", null);
		AssertFail(Transform.None, new char[112], "N2", null);
		AssertSuccess(Transform.None, new char[113], "N2", null, "Transform[Translation <0.00, 0.00, 0.00> | Rotation 0.00° around <0.00, 0.00, 0.00> | Scaling <1.00, 1.00, 1.00>]");
		AssertFail(TestTransform, new char[84], "N0", null);
		AssertSuccess(TestTransform, new char[85], "N0", null, "Transform[Translation <1, 2, 3> | Rotation 90° around <0, -1, 0> | Scaling <1, 0, 0>]");
		AssertFail(TestTransform, new char[104], "N1", null);
		AssertSuccess(TestTransform, new char[105], "N1", null, "Transform[Translation <1.0, 2.0, 3.0> | Rotation 90.0° around <0.0, -1.0, 0.0> | Scaling <0.8, 0.5, 0.2>]");
		AssertSuccess(TestTransform, new char[105], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "Transform[Translation <1,0. 2,0. 3,0> | Rotation 90,0° around <0,0. -1,0. 0,0> | Scaling <0,8. 0,5. 0,2>]");
		AssertSuccess(TestTransform, new char[125], "N3", null, "Transform[Translation <1.000, 2.000, 3.000> | Rotation 90.000° around <0.000, -1.000, 0.000> | Scaling <0.750, 0.500, 0.250>]");
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Transform[Translation <1.00, 2.00, 3.00> | Rotation 90.00° around <0.00, -1.00, 0.00> | Scaling <0.75, 0.50, 0.25>]";

		Assert.AreEqual(TestTransform, Transform.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, Transform.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestTransform, result);
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreEqual(Transform.None, new Transform(-0f, -0f, -0f));
		Assert.AreNotEqual(Transform.None, TestTransform);
		Assert.IsTrue(TestTransform.Equals(TestTransform));
		Assert.IsFalse(TestTransform.Equals(Transform.None));
		Assert.IsTrue(TestTransform == new Transform(new(1f, 2f, 3f), 90f % Direction.Down, new(0.75f, 0.5f, 0.25f)));
		Assert.IsFalse(Transform.None == TestTransform);
		Assert.IsFalse(Transform.None != new Transform(0f, 0f, 0f));
		Assert.IsTrue(TestTransform != Transform.None);
		Assert.IsTrue(TestTransform != TestTransform with { Translation = new(1f) });
		Assert.IsTrue(TestTransform != TestTransform with { Scaling = new(1f) });
		Assert.IsTrue(TestTransform != TestTransform with { Rotation = Rotation.None });

		Assert.IsTrue(Transform.None.Equals(Transform.None, 0f));
		Assert.IsTrue(TestTransform.Equals(TestTransform, 0f));
		Assert.IsTrue(
			new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
			.Equals(
				new Transform(new Vect(0.51f, 0.61f, 0.71f), new Rotation(90.01f, Direction.Left), new Vect(1.11f, 1.21f, 1.31f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
			.Equals(
				new Transform(new Vect(0.6f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
			.Equals(
				new Transform(new Vect(0.5f, 0.7f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
			.Equals(
				new Transform(new Vect(0.5f, 0.6f, 0.8f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
			.Equals(
				new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(80f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
			.Equals(
				new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.2f, 1.2f, 1.3f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
			.Equals(
				new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.3f, 1.3f))
				, 0.05f
			)
		);
		Assert.IsFalse(
			new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.3f))
			.Equals(
				new Transform(new Vect(0.5f, 0.6f, 0.7f), new Rotation(90f, Direction.Left), new Vect(1.1f, 1.2f, 1.4f))
				, 0.05f
			)
		);
	}

	[Test]
	public void ShouldCorrectlyCombineScaling() {
		Assert.AreEqual(new Vect(1.75f, 1.5f, 1.25f), TestTransform.WithScalingAdjustedBy(1f).Scaling);
		Assert.AreEqual(new Vect(-0.25f, -0.5f, -0.75f), TestTransform.WithScalingAdjustedBy(-1f).Scaling);

		Assert.AreEqual(new Vect(1.75f, 2.5f, 3.25f), TestTransform.WithScalingAdjustedBy((1f, 2f, 3f)).Scaling);
		Assert.AreEqual(new Vect(-0.25f, -1.5f, -2.75f), TestTransform.WithScalingAdjustedBy((-1f, -2f, -3f)).Scaling);

		Assert.AreEqual(new Vect(0.75f, 0.5f, 0.25f), TestTransform.WithScalingMultipliedBy(1f).Scaling);
		Assert.AreEqual(new Vect(-0.75f, -0.5f, -0.25f), TestTransform.WithScalingMultipliedBy(-1f).Scaling);
		Assert.AreEqual(new Vect(1.5f, 1f, 0.5f), TestTransform.WithScalingMultipliedBy(2f).Scaling);
		Assert.AreEqual(new Vect(0.375f, 0.25f, 0.125f), TestTransform.WithScalingMultipliedBy(0.5f).Scaling);

		Assert.AreEqual(new Vect(0.75f, 1f, 0.125f), TestTransform.WithScalingMultipliedBy((1f, 2f, 0.5f)).Scaling);
		Assert.AreEqual(new Vect(-0.75f, -1f, -0.125f), TestTransform.WithScalingMultipliedBy((-1f, -2f, -0.5f)).Scaling);
	}

	[Test]
	public void ShouldCorrectlyCombineRotations() {
		AssertToleranceEquals(90f % Direction.Down, TestTransform.WithAdditionalRotation(Rotation.None).Rotation, TestTolerance);
		AssertToleranceEquals(100f % Direction.Down, TestTransform.WithAdditionalRotation(10f % Direction.Down).Rotation, TestTolerance);
		AssertToleranceEquals(80f % Direction.Down, TestTransform.WithAdditionalRotation(10f % Direction.Up).Rotation, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCombineTranslations() {
		Assert.AreEqual(TestTransform.Translation, TestTransform.WithAdditionalTranslation(Vect.Zero).Translation);
		Assert.AreEqual(new Vect(2f, 0f, 3.5f), TestTransform.WithAdditionalTranslation(new(1f, -2f, 0.5f)).Translation);
	}

	[Test]
	public void ShouldCorrectlyCombineTransforms() {
		AssertToleranceEquals(TestTransform, TestTransform.WithComponentsCombinedWith(Transform.None), TestTolerance);
		AssertToleranceEquals(
			new Transform(
				new Vect(2f, 0f, 3.5f),
				80f % Direction.Down,
				new Vect(-0.75f, -1f, -0.125f)
			), 
			TestTransform.WithComponentsCombinedWith(new Transform(
				new Vect(1f, -2f, 0.5f),
				10f % Direction.Up,
				new Vect(-1f, -2f, -0.5f)
			)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = TestTransform;
		var end = new Transform(
			new Vect(2f, 4f, 6f),
			50f % Direction.Down,
			new Vect(0.55f, 1f, 0.75f)
		);

		AssertToleranceEquals(
			start,
			Transform.Interpolate(start, end, 0f),
			TestTolerance
		);
		AssertToleranceEquals(
			end,
			Transform.Interpolate(start, end, 1f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Transform(
				new Vect(1.5f, 3f, 4.5f),
				70f % Direction.Down,
				new Vect(0.65f, 0.75f, 0.5f)
			),
			Transform.Interpolate(start, end, 0.5f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Transform(
				new Vect(3f, 6f, 9f),
				10f % Direction.Down,
				new Vect(0.35f, 1.5f, 1.25f)
			),
			Transform.Interpolate(start, end, 2f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Transform(
				new Vect(0f, 0f, 0f),
				130f % Direction.Down,
				new Vect(0.95f, 0f, -0.25f)
			),
			Transform.Interpolate(start, end, -1f),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = TestTransform;
		var max = TestTransform
			.WithAdditionalTranslation((3f, -4f, 2f))
			.WithAdditionalRotation(30f % Direction.Up)
			.WithScalingAdjustedBy(2f);

		void AssertInput(Transform expectation, Transform input) {
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
			Transform.Interpolate(min, max, 0.5f),
			Transform.Interpolate(min, max, 0.5f)
		);
		AssertInput(
			min,
			Transform.Interpolate(min, max, -1f)
		);
		AssertInput(
			max,
			Transform.Interpolate(min, max, 1.5f)
		);
	}
}