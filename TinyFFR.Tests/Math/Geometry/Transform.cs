// Created on 2024-11-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

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

	}
}