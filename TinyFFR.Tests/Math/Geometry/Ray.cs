// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class RayTest {
	const float TestTolerance = 0.001f;
	static readonly Ray TestRay = new(new Location(1f, 2f, -3f), new Direction(-1f, -2f, 3f));

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		Assert.AreEqual(new Location(1f, 2f, -3f), TestRay.StartPoint);
		Assert.AreEqual(new Direction(-1f, -2f, 3f), TestRay.Direction);
		Assert.AreEqual(false, ((ILine) TestRay).IsUnboundedInBothDirections);
		Assert.AreEqual(null, ((ILine) TestRay).Length);
		Assert.AreEqual(null, ((ILine) TestRay).LengthSquared);
		Assert.AreEqual(null, ((ILine) TestRay).StartToEndVect);
		Assert.AreEqual(null, ((ILine) TestRay).EndPoint);
	}

	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "Ray[StartPoint <1.0, 2.0, -3.0> | Direction <-0.3, -0.5, 0.8>]";
		Assert.AreEqual(Expectation, TestRay.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestRay.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Ray[PointOnLine <1.0, 2.0, -3.0> | Direction <-0.3, -0.5, 0.8>]";
		Assert.AreEqual(new Ray(new Location(1f, 2f, -3f), new Direction(-0.3f, -0.5f, 0.8f)), Ray.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, Ray.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(new Ray(new Location(1f, 2f, -3f), new Direction(-0.3f, -0.5f, 0.8f)), result);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength(TestRay);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestRay);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestRay, TestRay.StartPoint.X, TestRay.StartPoint.Y, TestRay.StartPoint.Z, TestRay.Direction.X, TestRay.Direction.Y, TestRay.Direction.Z);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = new Ray(new Location(5f, 5f, 5f), Direction.Forward);
		var end = new Ray(new Location(15f, 15f, 15f), Direction.Right);
		var startToEndVec = start.StartPoint >> end.StartPoint;
		var startToEndDir = Direction.Forward >> Direction.Right;

		Assert.AreEqual(new Ray(start.StartPoint + startToEndVec * -0.5f, Direction.Forward * (startToEndDir * -0.5f)), Ray.Interpolate(start, end, -0.5f));
		Assert.AreEqual(new Ray(start.StartPoint + startToEndVec * 0.5f, Direction.Forward * (startToEndDir * 0.5f)), Ray.Interpolate(start, end, 0.5f));
		Assert.AreEqual(new Ray(start.StartPoint + startToEndVec * 1.5f, Direction.Forward * (startToEndDir * 1.5f)), Ray.Interpolate(start, end, 1.5f));

		Assert.AreEqual(Direction.Forward, Ray.Interpolate(start, new Ray(new Location(5f, 5f, 15f), Direction.Forward), 1f).Direction);
		Assert.AreEqual(Direction.Forward, Ray.Interpolate(start, new Ray(new Location(5f, 5f, 15f), Direction.Forward), 0f).Direction);
		Assert.AreEqual(Direction.Forward, Ray.Interpolate(start, new Ray(new Location(5f, 5f, 15f), Direction.Forward), 0.5f).Direction);
		Assert.AreEqual(Direction.Forward, Ray.Interpolate(start, new Ray(new Location(5f, 5f, 15f), Direction.Forward), -0.5f).Direction);

		AssertToleranceEquals(
			new Ray(new Location(1f, 0f, 0f), Direction.Right),
			Ray.Interpolate(
				new Ray(new Location(1f, 0f, 0f), Direction.Right),
				new Ray(new Location(1f, 0f, 0f), Direction.Forward),
				0f
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray(new Location(1f, 0f, 0f), Direction.Forward),
			Ray.Interpolate(
				new Ray(new Location(1f, 0f, 0f), Direction.Right),
				new Ray(new Location(1f, 0f, 0f), Direction.Forward),
				1f
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray(new Location(1f, 0f, 0f), new Direction(-1f, 0f, 1f)),
			Ray.Interpolate(
				new Ray(new Location(1f, 0f, 0f), Direction.Right),
				new Ray(new Location(1f, 0f, 0f), Direction.Forward),
				0.5f
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray(new Location(1f, 0f, 0.5f), new Direction(-1f, 0f, 1f)),
			Ray.Interpolate(
				new Ray(new Location(1f, 0f, 1f), Direction.Right),
				new Ray(new Location(1f, 0f, 0f), Direction.Forward),
				0.5f
			),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;

		var start = new Ray(new Location(5f, 5f, 5f), Direction.Forward);
		var end = new Ray(new Location(15f, 15f, 15f), Direction.Right);

		for (var i = 0; i < NumIterations; ++i) {
			var val = Ray.CreateNewRandom(start, end);
			Assert.GreaterOrEqual(val.StartPoint.X, start.StartPoint.X);
			Assert.GreaterOrEqual(val.StartPoint.Y, start.StartPoint.Y);
			Assert.GreaterOrEqual(val.StartPoint.Z, start.StartPoint.Z);
			Assert.Less(val.StartPoint.X, end.StartPoint.X);
			Assert.Less(val.StartPoint.Y, end.StartPoint.Y);
			Assert.Less(val.StartPoint.Z, end.StartPoint.Z);
			Assert.AreEqual(0f, val.Direction.Y);
			Assert.GreaterOrEqual(val.Direction.Z, 0f);
			Assert.LessOrEqual(val.Direction.Z, 1f);
			Assert.GreaterOrEqual(val.Direction.X, -1f);
			Assert.LessOrEqual(val.Direction.X, 0f);
		}
	}

	[Test]
	public void ShouldCorrectlyImplementEquality() {
		Assert.AreEqual(
			new Ray(new Location(100f, 0f, 0f), Direction.Right),
			new Ray(new Location(100f, 0f, 0f), new Direction(-2f, 0f, 0f))
		);

		AssertToleranceEquals(
			new Ray(new Location(100f, 0f, 0f), Direction.Right),
			new Ray(new Location(100f, 0f, 0.1f), Direction.Right),
			0.2f
		);

		AssertToleranceNotEquals(
			new Ray(new Location(100f, 0f, 0f), Direction.Right),
			new Ray(new Location(-100f, 0f, 0.1f), Direction.Right),
			0.05f
		);

		AssertToleranceEquals(
			new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0.05f)),
			new Ray(new Location(100f, 0f, 0f), Direction.Left),
			0.05f
		);

		AssertToleranceNotEquals(
			new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0.05f)),
			new Ray(new Location(100f, 0f, 0f), Direction.Left),
			0.03f
		);

		Assert.IsTrue(
			new Ray(new Location(100f, 0f, 0f), Direction.Right).EqualsWithinDistanceAndAngle(
				new Ray(new Location(100f, 0f, 0f), Direction.Right),
				0f,
				Angle.Zero
			)
		);
		Assert.IsFalse(
			new Ray(new Location(100f, 0f, 0f), Direction.Right).EqualsWithinDistanceAndAngle(
				new Ray(new Location(100f, 0f, 0.1f), Direction.Right),
				0f,
				Angle.Zero
			)
		);
		Assert.IsTrue(
			new Ray(new Location(100f, 0f, 0f), Direction.Right).EqualsWithinDistanceAndAngle(
				new Ray(new Location(100f, 0f, 0.1f), Direction.Right),
				0.2f,
				Angle.Zero
			)
		);
		Assert.IsFalse(
			new Ray(new Location(100f, 0f, 0f), Direction.Right).EqualsWithinDistanceAndAngle(
				new Ray(new Location(100f, 0f, 0.1f), Direction.Right),
				0.05f,
				Angle.Zero
			)
		);
		Assert.IsTrue(
			new Ray(new Location(100f, 0f, 0f), Direction.Left).EqualsWithinDistanceAndAngle(
				new Ray(new Location(100f, 0f, 0.1f), new Direction(1f, 0f, 0.1f)),
				0.2f,
				(Direction.Left ^ new Direction(1f, 0f, 0.1f)) * 1.1f
			)
		);
		Assert.IsFalse(
			new Ray(new Location(100f, 0f, 0f), Direction.Left).EqualsWithinDistanceAndAngle(
				new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0.1f)),
				0f,
				(Direction.Left ^ new Direction(1f, 0f, 0.1f)) * 0.9f
			)
		);
	}
}