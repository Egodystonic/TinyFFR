// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class BoundedRayTest {
	const float TestTolerance = 0.001f;
	static readonly BoundedRay TestRay = new(new Location(1f, 2f, -3f), new Location(-1f, -2f, 3f));

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<BoundedRay>();

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		Assert.AreEqual(new Location(1f, 2f, -3f), TestRay.StartPoint);
		Assert.AreEqual(new Direction(-1f, -2f, 3f), TestRay.Direction);
		Assert.AreEqual(MathF.Sqrt(4f + 16f + 36f), TestRay.Length, TestTolerance);
		Assert.AreEqual(4f + 16f + 36f, TestRay.LengthSquared, TestTolerance);
		Assert.AreEqual(new Vect(-2f, -4f, 6f), TestRay.StartToEndVect);
		Assert.AreEqual(new Location(-1f, -2f, 3f), TestRay.EndPoint);
		Assert.AreEqual(new Location(0f, 0f, 0f), TestRay.MiddlePoint);
		Assert.AreEqual(false, ((ILineLike) TestRay).IsUnboundedInBothDirections);
		Assert.AreEqual(TestRay.Length, ((ILineLike) TestRay).Length);
		Assert.AreEqual(TestRay.LengthSquared, ((ILineLike) TestRay).LengthSquared);
		Assert.AreEqual(TestRay.StartToEndVect, ((ILineLike) TestRay).StartToEndVect);
		Assert.AreEqual(TestRay.EndPoint, ((ILineLike) TestRay).EndPoint);
		Assert.AreEqual(true, ((ILineLike) TestRay).IsFiniteLength);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "BoundedRay[StartPoint <1.0, 2.0, -3.0> | EndPoint <-1.0, -2.0, 3.0>]";
		Assert.AreEqual(Expectation, TestRay.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestRay.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "BoundedRay[StartPoint <1.0, 2.0, -3.0> | EndPoint <-1.0, -2.0, 3.0>]";
		Assert.AreEqual(TestRay, BoundedRay.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, BoundedRay.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestRay, result);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength(TestRay);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestRay);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestRay, TestRay.StartPoint.X, TestRay.StartPoint.Y, TestRay.StartPoint.Z, TestRay.EndPoint.X, TestRay.EndPoint.Y, TestRay.EndPoint.Z);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = new BoundedRay(new Location(0f, 5f, 0f), new Location(5f, 0f, 0f));
		var end = new BoundedRay(new Location(0f, 0f, 5f), new Location(0f, 0f, -5f));
		var startPointVec = start.StartPoint >> end.StartPoint;
		var endPointVec = start.EndPoint >> end.EndPoint;

		Assert.AreEqual(new BoundedRay(start.StartPoint + startPointVec * -0.5f, start.EndPoint + (endPointVec * -0.5f)), BoundedRay.Interpolate(start, end, -0.5f));
		Assert.AreEqual(new BoundedRay(start.StartPoint + startPointVec * 0.5f, start.EndPoint + (endPointVec * 0.5f)), BoundedRay.Interpolate(start, end, 0.5f));
		Assert.AreEqual(new BoundedRay(start.StartPoint + startPointVec * 1.5f, start.EndPoint + (endPointVec * 1.5f)), BoundedRay.Interpolate(start, end, 1.5f));
		Assert.AreEqual(new BoundedRay(start.StartPoint, start.EndPoint), BoundedRay.Interpolate(start, end, 0f));
		Assert.AreEqual(new BoundedRay(end.StartPoint, end.EndPoint), BoundedRay.Interpolate(start, end, 1f));
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;

		var start = new BoundedRay(new Location(5f, 5f, 5f), new Location(0f, 0f, 0f));
		var end = new BoundedRay(new Location(15f, 15f, 15f), new Location(1f, 1f, 1f));

		for (var i = 0; i < NumIterations; ++i) {
			var val = BoundedRay.Random(start, end);
			Assert.GreaterOrEqual(val.StartPoint.X, start.StartPoint.X);
			Assert.GreaterOrEqual(val.StartPoint.Y, start.StartPoint.Y);
			Assert.GreaterOrEqual(val.StartPoint.Z, start.StartPoint.Z);
			Assert.Less(val.StartPoint.X, end.StartPoint.X);
			Assert.Less(val.StartPoint.Y, end.StartPoint.Y);
			Assert.Less(val.StartPoint.Z, end.StartPoint.Z);

			Assert.GreaterOrEqual(val.EndPoint.X, start.EndPoint.X);
			Assert.GreaterOrEqual(val.EndPoint.Y, start.EndPoint.Y);
			Assert.GreaterOrEqual(val.EndPoint.Z, start.EndPoint.Z);
			Assert.Less(val.EndPoint.X, end.EndPoint.X);
			Assert.Less(val.EndPoint.Y, end.EndPoint.Y);
			Assert.Less(val.EndPoint.Z, end.EndPoint.Z);
		}
	}

	[Test]
	public void ShouldCorrectlyImplementEquality() {
		Assert.AreEqual(
			new BoundedRay(new Location(100f, 0f, 0f), new Location(0f, 0f, 0f)),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(-100f, 0f, 0f))
		);

		AssertToleranceEquals(
			new BoundedRay(new Location(100f, 0f, 0f), new Location(99.9f, 0f, 0f)),
			new BoundedRay(new Location(100f, 0f, 0.1f), new Location(100f, 0f, 0f)),
			0.2f
		);

		AssertToleranceNotEquals(
			new BoundedRay(new Location(100f, 0f, 0f), new Location(99.9f, 0f, 0f)),
			new BoundedRay(new Location(100f, 0f, 0.1f), new Location(100f, 0f, 0f)),
			0.05f
		);

		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(99.9f, 0f, 0f)),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0.1f), new Vect(100f, 0f, 0f)),
			0.2f
		);

		AssertToleranceNotEquals(
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(99.9f, 0f, 0f)),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0.1f), new Vect(100f, 0f, 0f)),
			0.05f
		);

		Assert.AreEqual(
			true,
			TestRay.EqualsDisregardingDirection(new BoundedRay(TestRay.EndPoint, TestRay.StartPoint))
		);
		Assert.AreEqual(
			false,
			TestRay.EqualsDisregardingDirection(new BoundedRay(TestRay.EndPoint + (0.1f, 0f, 0f), TestRay.StartPoint + (-0.1f, 0f, 0f)))
		);
		Assert.AreEqual(
			true,
			TestRay.EqualsDisregardingDirection(new BoundedRay(TestRay.EndPoint + (0.1f, 0f, 0f), TestRay.StartPoint + (-0.1f, 0f, 0f)), 0.2f)
		);
		Assert.AreEqual(
			false,
			TestRay.EqualsDisregardingDirection(new BoundedRay(TestRay.EndPoint + (0.1f, 0f, 0f), TestRay.StartPoint + (-0.1f, 0f, 0f)), 0.05f)
		);
	}

	[Test]
	public void ShouldCorrectlyConvertToLine() {
		Assert.AreEqual(new Line(TestRay.StartPoint, TestRay.Direction), TestRay.ToLine());
	}

	[Test]
	public void ShouldCorrectlyConvertToRay() {
		AssertToleranceEquals(new Ray(TestRay.StartPoint, TestRay.Direction), TestRay.ToRayFromStart(), TestTolerance);
		AssertToleranceEquals(new Ray(TestRay.EndPoint, -TestRay.Direction), TestRay.ToRayFromEnd(), TestTolerance);
		AssertToleranceEquals(new Ray((2f, 2f, 2f), (1f, 1f, 1f)), new BoundedRay((0f, 0f, 0f), (4f, 4f, 4f)).ToRay(MathF.Sqrt(12f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFlip() {
		Assert.AreEqual(
			new BoundedRay(TestRay.EndPoint, TestRay.StartPoint),
			-TestRay
		);
	}

	[Test]
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(TestRay.StartPoint, TestRay.StartToEndVect * 2f),
			TestRay.ScaledFromStartBy(2f),
			TestTolerance
		);
		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(TestRay.StartPoint, TestRay.StartToEndVect * -2f),
			TestRay.ScaledFromStartBy(-2f),
			TestTolerance
		);

		AssertToleranceEquals(
			new BoundedRay(new Location(-5f, -5f, -5f), new Location(15f, 15f, 15f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledFromMiddleBy(2f),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(-5f, -5f, -5f), new Location(15f, 15f, 15f)).Flipped,
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledFromMiddleBy(-2f),
			TestTolerance
		);

		AssertToleranceEquals(
			new BoundedRay(TestRay.EndPoint - TestRay.StartToEndVect * 2f, TestRay.EndPoint),
			TestRay.ScaledFromEndBy(2f),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(TestRay.EndPoint + TestRay.StartToEndVect * 2f, TestRay.EndPoint),
			TestRay.ScaledFromEndBy(-2f),
			TestTolerance
		);

		AssertToleranceEquals(
			new BoundedRay(new Location(-7.5f, -7.5f, -7.5f), new Location(12.5f, 12.5f, 12.5f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledBy(2f, 0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(22.5f, 22.5f, 22.5f), new Location(2.5f, 2.5f, 2.5f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledBy(-2f, 0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(7.5f, 7.5f, 7.5f), new Location(27.5f, 27.5f, 27.5f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledBy(2f, -0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(-22.5f, -22.5f, -22.5f), new Location(-7.5f + 17.5f * -2f, -7.5f + 17.5f * -2f, -7.5f + 17.5f * -2f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledBy(-2f, -0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(-15f, -15f, -15f), new Location(5f, 5f, 5f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledBy(2f, 1.5f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(45f, 45f, 45f), new Location(25f, 25f, 25f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledBy(-2f, 1.5f * MathF.Sqrt(300f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyRotate() {
		var rotation = 90f % Direction.Down;
		var xzLine = new BoundedRay(new Location(3f, 0f, -3f), new Location(-3f, 0f, 3f));

		AssertToleranceEquals(
			new BoundedRay(new Location(3f, 0f, -3f), new Location(-3f, 0f, -9f)),
			xzLine * rotation,
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(3f, 0f, 3f), new Location(-3f, 0f, -3f)),
			xzLine.RotatedAroundMiddleBy(rotation),
			TestTolerance
		);
		AssertToleranceEquals(
			rotation * xzLine,
			xzLine.RotatedAroundStartBy(rotation),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(3f, 0f, 9f), new Location(-3f, 0f, 3f)),
			xzLine.RotatedAroundEndBy(rotation),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(3f, 0f, 6f), new Location(-3f, 0f, 0f)),
			xzLine.RotatedBy(rotation, xzLine.UnboundedLocationAtDistance(xzLine.Length * 0.75f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(3f, 0f, 0f), new Location(-3f, 0f, -6f)),
			xzLine.RotatedBy(rotation, xzLine.UnboundedLocationAtDistance(xzLine.Length * 0.25f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(-3f, 0f, 3f), new Location(-9f, 0f, -3f)),
			xzLine.RotatedBy(rotation, (-3f, 0f, -3f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyRotateAroundPoints() {
		void AssertCombination(BoundedRay expectation, BoundedRay input, Location pivotPoint, Rotation rotation) {
			AssertToleranceEquals(expectation, input.RotatedBy(rotation, pivotPoint), TestTolerance);
			Assert.AreEqual(input.RotatedBy(rotation, pivotPoint), input * (pivotPoint, rotation));
			Assert.AreEqual(input.RotatedBy(rotation, pivotPoint), input * (rotation, pivotPoint));
			Assert.AreEqual(input.RotatedBy(rotation, pivotPoint), (pivotPoint, rotation) * input);
			Assert.AreEqual(input.RotatedBy(rotation, pivotPoint), (rotation, pivotPoint) * input);
		}

		AssertCombination(new BoundedRay((0f, 0f, 10f), Location.Origin), new BoundedRay(Location.Origin, (0f, 0f, 10f)), (0f, 0f, 5f), Direction.Down % 180f);
		AssertCombination(new BoundedRay((-30f, 30f, 0f), (-30f, 10f, 0f)), new BoundedRay((10f, 10f, 0f), (-10f, 10f, 0f)), (-20f, 0f, 0f), Direction.Forward % 90f);
	}

	[Test]
	public void ShouldCorrectlyMove() {
		var vect = new Vect(5f, -3f, 12f);

		AssertToleranceEquals(
			new BoundedRay(TestRay.StartPoint + vect, TestRay.EndPoint + vect),
			TestRay + vect,
			TestTolerance
		);
		AssertToleranceEquals(
			TestRay.Direction,
			(vect + TestRay).Direction,
			TestTolerance
		);
		AssertToleranceEquals(
			TestRay.StartToEndVect,
			(vect + TestRay).StartToEndVect,
			TestTolerance
		);
		Assert.AreEqual(
			TestRay.Length,
			(vect + TestRay).Length,
			TestTolerance
		);
		Assert.AreEqual(
			TestRay.LengthSquared,
			(vect + TestRay).LengthSquared,
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToLocation() {
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), Direction.Left * 200f).PointClosestTo(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(-100f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 200f).PointClosestTo(new Location(-100f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).PointClosestTo(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 200f).PointClosestTo(new Location(-100f, 1f, 0f))
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToOrigin() {
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).PointClosestToOrigin()
		);
		Assert.AreEqual(
			new Location(0f, -1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -1f, 0f), Direction.Right * 200f).PointClosestToOrigin()
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).PointClosestToOrigin()
		);
		Assert.AreEqual(
			new Location(100f, -1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -1f, 0f), Direction.Left * 200f).PointClosestToOrigin()
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLocation() {
		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			0f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			MathF.Sqrt(2f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			MathF.Sqrt(10001f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			MathF.Sqrt(10001f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			200f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			MathF.Sqrt(40002f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			10f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(310f, 0f, 0f)),
			TestTolerance
		);
		Assert.AreEqual(
			10f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(90f, 0f, 0f)),
			TestTolerance
		);

		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			0f,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 100f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(1f, 0f, 0f), Direction.Left * 100f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			0f,
			BoundedRay.FromStartPointAndVect(new Location(-1f, 0f, 0f), Direction.Left * 100f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 9f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			0f,
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 11f).DistanceFromOrigin()
		);

		// Squared
		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceSquaredFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceSquaredFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			0f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceSquaredFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			2f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceSquaredFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			10001f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceSquaredFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			10001f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceSquaredFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			40_000f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceSquaredFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			40002f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceSquaredFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			100f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceSquaredFrom(new Location(310f, 0f, 0f)),
			TestTolerance
		);
		Assert.AreEqual(
			100f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceSquaredFrom(new Location(90f, 0f, 0f)),
			TestTolerance
		);

		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			0f,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 100f).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(1f, 0f, 0f), Direction.Left * 100f).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			0f,
			BoundedRay.FromStartPointAndVect(new Location(-1f, 0f, 0f), Direction.Left * 100f).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 9f).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			0f,
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 11f).DistanceSquaredFromOrigin()
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineContainmentOfLocation() {
		Assert.AreEqual(
			false,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).Contains(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			true,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).Contains(new Location(0f, 1f, 0f), 1.1f)
		);
		Assert.AreEqual(
			false,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).Contains(new Location(0f, 1f, 0f), 0.9f)
		);
		Assert.AreEqual(
			true,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).Contains(new Location(0f, -1f, 0f), 1.1f)
		);
		Assert.AreEqual(
			false,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Right * 200f).Contains(new Location(0f, -1f, 0f), 0.9f)
		);
		Assert.AreEqual(
			false,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(99f, 0f, 0f), 0.9f)
		);
		Assert.AreEqual(
			true,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(99f, 0f, 0f), 1.1f)
		);
		Assert.AreEqual(
			true,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(100f, 0f, 0f))
		);
		Assert.AreEqual(
			true,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(110f, 0f, 0f))
		);
		Assert.AreEqual(
			false,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(310f, 0f, 0f))
		);
		Assert.AreEqual(
			false,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(90f, 0f, 0f))
		);
		Assert.AreEqual(
			false,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(310f, 0f, 0f), 9.9f)
		);
		Assert.AreEqual(
			false,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(90f, 0f, 0f), 9.9f)
		);
		Assert.AreEqual(
			true,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(310f, 0f, 0f), 10.1f)
		);
		Assert.AreEqual(
			true,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(90f, 0f, 0f), 10.1f)
		);
	}

	[Test]
	public void ShouldCorrectlyReturnClosestPointToOtherLine() {
		void AssertPair<TLine>(Location expectedResult, BoundedRay ray, TLine other) where TLine : ILineLike {
			switch (other) {
				case Line l:
					AssertToleranceEquals(expectedResult, ray.PointClosestTo(l), TestTolerance);
					Assert.AreEqual(ray.PointClosestTo(l), other.ClosestPointOn(ray));
					break;
				case Ray r:
					AssertToleranceEquals(expectedResult, ray.PointClosestTo(r), TestTolerance);
					Assert.AreEqual(ray.PointClosestTo(r), other.ClosestPointOn(ray));
					break;
				case BoundedRay b:
					AssertToleranceEquals(expectedResult, ray.PointClosestTo(b), TestTolerance);
					Assert.AreEqual(ray.PointClosestTo(b), other.ClosestPointOn(ray));
					break;
				default:
					Assert.Fail("Unknown line type");
					break;
			}
		}

		// Line
		AssertPair(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 1f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Line(new Location(100f, 10f, 0f), Direction.Left)
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Line(new Location(100f, -10f, 0f), Direction.Left)
		);
		AssertPair(
			new Location(0f, 100f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Line(new Location(100f, 110f, 0f), Direction.Left)
		);

		// Ray
		AssertPair(
			new Location(0f, 20f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 1f, 1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -1f, 1f))
		);
		AssertPair(
			new Location(0f, 30f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 2f, 1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -1.5f, 1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -2.5f, 1f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -2.5f, -1f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 1f, -1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, -1f, -10f), Direction.Forward)
		);
		AssertPair(
			new Location(0f, 0f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 1f, -1f), new Direction(0f, -100f, 0.1f))
		);
		AssertPair(
			new Location(0f, 15f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 15f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 1f, 1f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 15f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -1f, -1f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 15f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -1f, -1f))
		);
		AssertPair(
			new Location(0f, 15f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 15f),
			new Ray(new Location(0f, 20f, -10f), new Direction(0f, -1f, -1f))
		);

		// BoundedRay
		AssertPair(
			new Location(0f, 20f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 20f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, 30f, 10f), new Location(0f, 10f, -10f))
		);
		AssertPair(
			new Location(0f, 30f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, 30f, 10f), new Location(0f, 10f, 30f))
		);
		AssertPair(
			new Location(0f, 30f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, 10f, 30f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, 0f, 10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, 0f, 10f), new Location(0f, -10f, -10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, -10f, 0f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, -10f, 0f), new Location(0f, -10f, -10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, -50f, -10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, -50f, -10f), new Location(0f, -10f, -10f))
		);
		AssertPair(
			new Location(0f, 20f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f),
			new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 10f),
			new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 10f),
			new BoundedRay(new Location(0f, 30f, 10f), new Location(0f, 10f, -10f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 10f),
			new BoundedRay(new Location(0f, 30f, 10f), new Location(0f, 10f, 30f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 10f),
			new BoundedRay(new Location(0f, 10f, 30f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 10f),
			new BoundedRay(new Location(0f, -10f, 10f), new Location(0f, -20f, 10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 10f),
			new BoundedRay(new Location(0f, -20f, 10f), new Location(0f, -10f, 10f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 10f),
			new BoundedRay(new Location(0f, 30f, 10f), new Location(0f, 20f, 10f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 10f),
			new BoundedRay(new Location(0f, 20f, 10f), new Location(0f, 30f, 10f))
		);
		Assert.GreaterOrEqual(BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).Y, 0f);
		Assert.LessOrEqual(BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).Y, 50f);
		Assert.AreEqual(0f, BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).X);
		Assert.AreEqual(0f, BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).Z);
		Assert.GreaterOrEqual(BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).Y, 0f);
		Assert.LessOrEqual(BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).Y, 50f);
		Assert.AreEqual(0f, BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).X);
		Assert.AreEqual(0f, BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).Z);

		Assert.GreaterOrEqual(BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).Y, 10f);
		Assert.LessOrEqual(BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).Y, 50f);
		Assert.AreEqual(0f, BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).X);
		Assert.AreEqual(0f, BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).Z);
		Assert.GreaterOrEqual(BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).Y, 10f);
		Assert.LessOrEqual(BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).Y, 50f);
		Assert.AreEqual(0f, BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).X);
		Assert.AreEqual(0f, BoundedRay.FromStartPointAndVect(Location.Origin, Direction.Up * 100f).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).Z);
	}

	[Test]
	public void ShouldCorrectlyCalculateDistanceFromLines() { // These are regression tests
		Assert.AreEqual(
			16.738178f,
			TestRay.DistanceFrom(new Line(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			18.3847770f,
			TestRay.DistanceFrom(new Ray(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			17.34369f,
			TestRay.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f) * -4f)),
			TestTolerance
		);

		Assert.AreEqual(
			0f,
			TestRay.DistanceFrom(TestRay.ToLine()),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestRay.DistanceFrom(TestRay.ToRayFromStart()),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestRay.DistanceFrom(TestRay),
			TestTolerance
		);

		// Squared
		Assert.AreEqual(
			16.738178f * 16.738178f,
			TestRay.DistanceSquaredFrom(new Line(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			18.3847770f * 18.3847770f,
			TestRay.DistanceSquaredFrom(new Ray(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			17.34369f * 17.34369f,
			TestRay.DistanceSquaredFrom(BoundedRay.FromStartPointAndVect(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f) * -4f)),
			1f
		);

		Assert.AreEqual(
			0f,
			TestRay.DistanceSquaredFrom(TestRay.ToLine()),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestRay.DistanceSquaredFrom(TestRay.ToRayFromStart()),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestRay.DistanceSquaredFrom(TestRay),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyImplementLocationAtDistanceFunctions() {
		var line = BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Right * 100f);

		Assert.AreEqual(false, line.DistanceIsWithinLineBounds(-30000f));
		Assert.AreEqual(false, line.DistanceIsWithinLineBounds(30000f));
		Assert.AreEqual(true, line.DistanceIsWithinLineBounds(0f));
		Assert.AreEqual(true, line.DistanceIsWithinLineBounds(50f));
		Assert.AreEqual(true, line.DistanceIsWithinLineBounds(99f));
		Assert.AreEqual(false, line.DistanceIsWithinLineBounds(101f));
		Assert.AreEqual(false, line.DistanceIsWithinLineBounds(-1f));

		Assert.AreEqual(0f, line.BindDistance(-30000f));
		Assert.AreEqual(100f, line.BindDistance(30000f));
		Assert.AreEqual(0f, line.BindDistance(0f));
		Assert.AreEqual(0f, line.BindDistance(-1f));
		Assert.AreEqual(100f, line.BindDistance(100f));
		Assert.AreEqual(100f, line.BindDistance(101f));

		Assert.AreEqual(new Location(0f, 1f, 0f), line.BoundedLocationAtDistance(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), line.BoundedLocationAtDistance(3f));
		Assert.AreEqual(new Location(-100f, 1f, 0f), line.BoundedLocationAtDistance(101f));
		Assert.AreEqual(new Location(0f, 1f, 0f), line.BoundedLocationAtDistance(-3f));

		Assert.AreEqual(new Location(0f, 1f, 0f), line.UnboundedLocationAtDistance(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), line.UnboundedLocationAtDistance(3f));
		Assert.AreEqual(new Location(-101f, 1f, 0f), line.UnboundedLocationAtDistance(101f));
		Assert.AreEqual(new Location(3f, 1f, 0f), line.UnboundedLocationAtDistance(-3f));

		Assert.AreEqual(new Location(0f, 1f, 0f), line.LocationAtDistanceOrNull(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), line.LocationAtDistanceOrNull(3f));
		Assert.AreEqual(null, line.LocationAtDistanceOrNull(-3f));
		Assert.AreEqual(null, line.LocationAtDistanceOrNull(101f));
	}

	[Test]
	public void ShouldCorrectlyDetectLineIntersections() {
		// Line
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(-1f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 0.99f
			)
		);
		Assert.AreEqual(
			new Location(101f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 1.01f
			)
		);

		// Ray
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(-1f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(101f, 1f, 0f), Direction.Left),
				lineThickness: 0.99f
			)
		);
		Assert.AreEqual(
			new Location(101f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(101f, 1f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 0.99f
			)
		);
		Assert.AreEqual(
			new Location(101f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 1.01f
			)
		);

		// BoundedRay
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(0f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedRay(new Location(0f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.AreEqual(
			new Location(0f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedRay(new Location(-1f, 1f, 0f), new Location(0f, 1f, 0f))
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedRay(new Location(-1f, 1f, 0f), new Location(-2f, 1f, 0f))
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedRay(new Location(-2f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.AreEqual(
			new Location(10f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedRay(new Location(10f, 2f, 0f), new Location(10f, 0f, 0f))
			)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedRay(new Location(10f, 2f, 0f), new Location(10f, 4f, 0f))
			)
		);



		// Line, Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(101f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				new Line(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 1.01f
			)
		);

		// Ray, Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(-1f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(101f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				new Ray(new Location(101f, 1f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(101f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				new Ray(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 1.01f
			)
		);

		// BoundedRay
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(0f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				new BoundedRay(new Location(0f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.AreEqual(
			new Location(0f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				new BoundedRay(new Location(-1f, 1f, 0f), new Location(0f, 1f, 0f))
			)
		);
		Assert.AreEqual(
			new Location(10f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).FastIntersectionWith(
				new BoundedRay(new Location(10f, 2f, 0f), new Location(10f, 0f, 0f))
			)
		);
	}

	[Test]
	public void ShouldCorrectlyTestForLineIntersections() {
		// Line
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(-1f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 0.99f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 1.01f
			)
		);

		// Ray
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(101f, 1f, 0f), Direction.Left),
				lineThickness: 0.99f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(101f, 1f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 0.99f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 1.01f
			)
		);

		// BoundedRay
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedRay(new Location(0f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedRay(new Location(-1f, 1f, 0f), new Location(0f, 1f, 0f))
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedRay(new Location(-1f, 1f, 0f), new Location(-2f, 1f, 0f))
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedRay(new Location(-2f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedRay(new Location(10f, 2f, 0f), new Location(10f, 0f, 0f))
			)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedRay(new Location(10f, 2f, 0f), new Location(10f, 4f, 0f))
			)
		);
	}

	[Test]
	public void ShouldCorrectlyReflectOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Down * 100f).ReflectedBy(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Up * 100f).ReflectedBy(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 100f).ReflectedBy(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 100f).ReflectedBy(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Up * 100f).ReflectedBy(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Down * 100f).ReflectedBy(plane)
		);

		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), new Direction(0f, 1f, -1f) * MathF.Sqrt(50f) * 2f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).ReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), new Direction(0f, -1f, 1f) * MathF.Sqrt(50f) * 1f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.ReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), new Direction(1f, 1f, -1f) * (10f - MathF.Sqrt(3f))),
			BoundedRay.FromStartPointAndVect(new Location(-1f, 2f, 1f), new Direction(1f, -1f, -1f) * 10f).ReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), new Direction(-2f, -1f, 2f) * (10f - MathF.Sqrt(9f))),
			BoundedRay.FromStartPointAndVect(new Location(2f, 0f, -2f), new Direction(-2f, 1f, 2f) * 10f).ReflectedBy(plane),
			TestTolerance
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).ReflectedBy(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).Flipped.ReflectedBy(plane)
		);

		// Fast
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Down * 100f).FastReflectedBy(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Up * 100f).FastReflectedBy(plane)
		);

		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), new Direction(0f, 1f, -1f) * MathF.Sqrt(50f) * 2f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).FastReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), new Direction(0f, -1f, 1f) * MathF.Sqrt(50f) * 1f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.FastReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), new Direction(1f, 1f, -1f) * (10f - MathF.Sqrt(3f))),
			BoundedRay.FromStartPointAndVect(new Location(-1f, 2f, 1f), new Direction(1f, -1f, -1f) * 10f).FastReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), new Direction(-2f, -1f, 2f) * (10f - MathF.Sqrt(9f))),
			BoundedRay.FromStartPointAndVect(new Location(2f, 0f, -2f), new Direction(-2f, 1f, 2f) * 10f).FastReflectedBy(plane),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineIncidentAngleOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			Angle.Zero,
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Down * 100f).IncidentAngleWith(plane)
		);
		Assert.AreEqual(
			Angle.Zero,
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Up * 100f).IncidentAngleWith(plane)
		);

		AssertToleranceEquals(
			Angle.EighthCircle,
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).IncidentAngleWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			Angle.EighthCircle,
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.IncidentAngleWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			Angle.FromRadians(MathF.Acos(1f / 3f)),
			BoundedRay.FromStartPointAndVect(new Location(2f, 0f, -2f), new Direction(-2f, 1f, 2f) * 10f).IncidentAngleWith(plane),
			TestTolerance
		);

		// Fast
		Assert.AreEqual(
			Angle.Zero,
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Down * 100f).FastIncidentAngleWith(plane)
		);
		Assert.AreEqual(
			Angle.Zero,
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Up * 100f).FastIncidentAngleWith(plane)
		);

		AssertToleranceEquals(
			Angle.EighthCircle,
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).FastIncidentAngleWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			Angle.EighthCircle,
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.FastIncidentAngleWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			Angle.FromRadians(MathF.Acos(1f / 3f)),
			BoundedRay.FromStartPointAndVect(new Location(2f, 0f, -2f), new Direction(-2f, 1f, 2f) * 10f).FastIncidentAngleWith(plane),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyFindIntersectionPointWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 101f, 0f), Direction.Down * 100f).IntersectionWith(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -99f, 0f), Direction.Up * 100f).IntersectionWith(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 100f).IntersectionWith(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 100f).IntersectionWith(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Up * 100f).IntersectionWith(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Down * 100f).IntersectionWith(plane)
		);

		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).IntersectionWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.IntersectionWith(plane),
			TestTolerance
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).IntersectionWith(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).Flipped.IntersectionWith(plane)
		);

		// Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 101f, 0f), Direction.Down * 100f).FastIntersectionWith(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -99f, 0f), Direction.Up * 100f).FastIntersectionWith(plane)
		);

		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).FastIntersectionWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.FastIntersectionWith(plane),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyTestForIntersectionWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(100f, 101f, 0f), Direction.Down * 100f).IsIntersectedBy(plane)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(100f, -99f, 0f), Direction.Up * 100f).IsIntersectedBy(plane)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 100f).IsIntersectedBy(plane)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 100f).IsIntersectedBy(plane)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Up * 100f).IsIntersectedBy(plane)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Down * 100f).IsIntersectedBy(plane)
		);

		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).IsIntersectedBy(plane)
		);
		Assert.True(
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.IsIntersectedBy(plane)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).IsIntersectedBy(plane)
		);
		Assert.False(
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).Flipped.IsIntersectedBy(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineSignedDistanceFromPlane() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		void AssertDistance(float expectedSignedDistance, BoundedRay line) {
			Assert.AreEqual(expectedSignedDistance, line.SignedDistanceFrom(plane));
			Assert.AreEqual(MathF.Abs(expectedSignedDistance), line.DistanceFrom(plane));
			Assert.AreEqual(expectedSignedDistance, line.Flipped.SignedDistanceFrom(plane));
			Assert.AreEqual(MathF.Abs(expectedSignedDistance), line.Flipped.DistanceFrom(plane));
		}

		AssertDistance(
			0f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 101f, 0f), Direction.Down * 100f)
		);
		AssertDistance(
			0f,
			BoundedRay.FromStartPointAndVect(new Location(100f, -99f, 0f), Direction.Up * 100f)
		);
		AssertDistance(
			1f,
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 100f)
		);
		AssertDistance(
			-1f,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 100f)
		);
		AssertDistance(
			99f,
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Up * 100f)
		);
		AssertDistance(
			-101f,
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Down * 100f)
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Down * 100f).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Down * 100f).Flipped.PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Up * 100f).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Up * 100f).Flipped.PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(0f, 2f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 100f).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(-100f, 2f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 100f).Flipped.PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 100f).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(-100f, 0f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 100f).Flipped.PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 100f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Up * 100f).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 100f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Up * 100f).Flipped.PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, -100f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Down * 100f).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, -100f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Down * 100f).Flipped.PointClosestTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Down * 200f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Up * 200f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Down * 50f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Up * 50f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			plane.PointClosestToOrigin,
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 200f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			plane.PointClosestToOrigin,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 200f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Up * 200f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Down * 200f).ClosestPointOn(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineRelationshipWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Down * 200f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Up * 200f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Down * 50f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Up * 50f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Down * 200f).Flipped.RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Up * 200f).Flipped.RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Down * 50f).Flipped.RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Up * 50f).Flipped.RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 200f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 200f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Up * 200f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Down * 200f).RelationshipTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyProjectOnToPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various projections from behind the plane
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Left * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Left * 100f / MathF.Sqrt(2f)),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Right * 100f / MathF.Sqrt(2f)),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Left * 100f / MathF.Sqrt(2f)),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Right * 100f / MathF.Sqrt(2f)),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f) * 100f).ProjectedOnTo(plane)
		);

		// Various projections from in front the plane
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Left * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Right * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Left * 100f / MathF.Sqrt(2f)),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Right * 100f / MathF.Sqrt(2f)),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Left * 100f / MathF.Sqrt(2f)),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Direction.Right * 100f / MathF.Sqrt(2f)),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f) * 100f).ProjectedOnTo(plane)
		);

		// Projections from perpendicular directions
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Vect.Zero),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Up * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Vect.Zero),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Down * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Vect.Zero),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Up * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 1f, 0f), Vect.Zero),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Down * 100f).ProjectedOnTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various parallelizations from behind the plane
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Left * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Left * 100f).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 100f).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f) * 100f).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f) * 100f).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f) * 100f).FastParallelizedWith(plane)
		);

		// Various parallelizations from in front the plane
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Left * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Right * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Left * 100f).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Right * 100f).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f) * 100f).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f) * 100f).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Left * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Right * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f) * 100f).FastParallelizedWith(plane)
		);

		// Projections from parallelizations directions
		Assert.AreEqual(
			null,
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Up * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			null,
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Down * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			null,
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Up * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			null,
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Down * 100f).ParallelizedWith(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various orthogonalizations from behind the plane
		Assert.AreEqual(
			null,
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Left * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			null,
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Right * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f) * 100f).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f) * 100f).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f) * 100f).FastOrthogonalizedAgainst(plane)
		);

		// Various orthogonalizations from in front the plane
		Assert.AreEqual(
			null,
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Left * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			null,
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Right * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f) * 100f).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f) * 100f).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f) * 100f).FastOrthogonalizedAgainst(plane)
		);

		// Orthogonalizations from perpendicular directions
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Up * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Down * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Up * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Down * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Up * 100f).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 2f, 0f), Direction.Down * 100f).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Up * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Up * 100f).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Down * 100f),
			BoundedRay.FromStartPointAndVect(new Location(10f, 0f, 0f), Direction.Down * 100f).FastOrthogonalizedAgainst(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyBeSplitByPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		void AssertSplit(BoundedRay? expectedToPlane, BoundedRay? expectedFromPlane, BoundedRay ray) {
			if (expectedToPlane == null) {
				Assert.AreEqual(null, ray.SplitBy(plane));
				Assert.AreEqual(false, ray.IsIntersectedBy(plane));
			}
			else {
				Assert.AreEqual(true, ray.IsIntersectedBy(plane));
				var (actualToPlane, actualFromPlane) = ray.SplitBy(plane)!.Value;
				AssertToleranceEquals(expectedToPlane, actualToPlane, TestTolerance);
				AssertToleranceEquals(expectedFromPlane, actualFromPlane, TestTolerance);
				(actualToPlane, actualFromPlane) = ray.FastSplitBy(plane);
				AssertToleranceEquals(expectedToPlane, actualToPlane, TestTolerance);
				AssertToleranceEquals(expectedFromPlane, actualFromPlane, TestTolerance);
			}
		}

		AssertSplit(
			null,
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 100f)
		);
		AssertSplit(
			null,
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 100f)
		);
		AssertSplit(
			null,
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 100f)
		);
		AssertSplit(
			null,
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 100f)
		);
		AssertSplit(
			null,
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Right * 100f)
		);
		AssertSplit(
			null,
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Direction.Left * 100f)
		);

		AssertSplit(
			new BoundedRay(new Location(100f, 2f, 0f), new Location(100f, 1f, 0f)),
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Down * 99f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Down * 100f)
		);
		AssertSplit(
			new BoundedRay(new Location(100f, 0f, 0f), new Location(100f, 1f, 0f)),
			BoundedRay.FromStartPointAndVect(new Location(100f, 1f, 0f), Direction.Up * 99f),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Up * 100f)
		);
		AssertSplit(
			null,
			null,
			BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 100f)
		);
		AssertSplit(
			null,
			null,
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), Direction.Down * 100f)
		);
		AssertSplit(
			new BoundedRay(new Location(100f, 2f, 0f), new Location(101f, 1f, 0f)),
			BoundedRay.FromStartPointAndVect(new Location(101f, 1f, 0f), new Direction(1f, -1f, 0f) * (100f - MathF.Sqrt(2f))),
			BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f)
		);
		AssertSplit(
			new BoundedRay(new Location(100f, 0f, 0f), new Location(101f, 1f, 0f)),
			BoundedRay.FromStartPointAndVect(new Location(101f, 1f, 0f), new Direction(1f, 1f, 0f) * (100f - MathF.Sqrt(2f))),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f)
		);

		AssertSplit(
			null,
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 10f, 0f), Direction.Down * 5f)
		);
		AssertSplit(
			null,
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, -10f, 0f), Direction.Up * 5f)
		);
		AssertSplit(
			BoundedRay.FromStartPointAndVect(new Location(0f, 10f, 0f), Direction.Down * 9f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Vect.Zero),
			BoundedRay.FromStartPointAndVect(new Location(0f, 10f, 0f), Direction.Down * 9f)
		);
		AssertSplit(
			BoundedRay.FromStartPointAndVect(new Location(0f, -10f, 0f), Direction.Up * 11f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 1f, 0f), Vect.Zero),
			BoundedRay.FromStartPointAndVect(new Location(0f, -10f, 0f), Direction.Up * 11f)
		);

		// Some older tests from previous iteration
		Assert.AreEqual(
			new Pair<BoundedRay, BoundedRay>(new BoundedRay(new Location(100f, 101f, 0f), new Location(100f, 1f, 0f)), new BoundedRay(new Location(100f, 1f, 0f), new Location(100f, 1f, 0f))),
			BoundedRay.FromStartPointAndVect(new Location(100f, 101f, 0f), Direction.Down * 100f).SplitBy(plane)
		);
		Assert.AreEqual(
			new Pair<BoundedRay, BoundedRay>(new BoundedRay(new Location(100f, -99f, 0f), new Location(100f, 1f, 0f)), new BoundedRay(new Location(100f, 1f, 0f), new Location(100f, 1f, 0f))),
			BoundedRay.FromStartPointAndVect(new Location(100f, -99f, 0f), Direction.Up * 100f).SplitBy(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 100f).SplitBy(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 100f).SplitBy(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 0f), Direction.Up * 100f).SplitBy(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(100f, -100f, 0f), Direction.Down * 100f).SplitBy(plane)
		);

		var split = BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).SplitBy(plane)!.Value;
		AssertToleranceEquals(new BoundedRay(new Location(0f, 6f, 5f), new Location(0f, 1f, 0f)), split.First, TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, 1f, 0f), new Location(0f, -9f, -10f)), split.Second, TestTolerance);
		split = BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.SplitBy(plane)!.Value;
		AssertToleranceEquals(new BoundedRay(new Location(0f, -9f, -10f), new Location(0f, 1f, 0f)), split.First, TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, 1f, 0f), new Location(0f, 6f, 5f)), split.Second, TestTolerance);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).SplitBy(plane)
		);
		Assert.Null(
			BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).Flipped.SplitBy(plane)
		);

		// Fast
		Assert.AreEqual(
			new Pair<BoundedRay, BoundedRay>(new BoundedRay(new Location(100f, 101f, 0f), new Location(100f, 1f, 0f)), new BoundedRay(new Location(100f, 1f, 0f), new Location(100f, 1f, 0f))),
			BoundedRay.FromStartPointAndVect(new Location(100f, 101f, 0f), Direction.Down * 100f).FastSplitBy(plane)
		);
		Assert.AreEqual(
			new Pair<BoundedRay, BoundedRay>(new BoundedRay(new Location(100f, -99f, 0f), new Location(100f, 1f, 0f)), new BoundedRay(new Location(100f, 1f, 0f), new Location(100f, 1f, 0f))),
			BoundedRay.FromStartPointAndVect(new Location(100f, -99f, 0f), Direction.Up * 100f).FastSplitBy(plane)
		);

		split = BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).FastSplitBy(plane);
		AssertToleranceEquals(new BoundedRay(new Location(0f, 6f, 5f), new Location(0f, 1f, 0f)), split.First, TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, 1f, 0f), new Location(0f, -9f, -10f)), split.Second, TestTolerance);
		split = BoundedRay.FromStartPointAndVect(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.FastSplitBy(plane);
		AssertToleranceEquals(new BoundedRay(new Location(0f, -9f, -10f), new Location(0f, 1f, 0f)), split.First, TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, 1f, 0f), new Location(0f, 6f, 5f)), split.Second, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = new BoundedRay((0f, 10f, 0f), (0f, 10f, 10f));
		var max = new BoundedRay((0f, 20f, 0f), (0f, 20f, 20f));

		AssertToleranceEquals(
			new BoundedRay((0f, 15f, 0f), (0f, 15f, 15f)),
			new BoundedRay((0f, 15f, 0f), (0f, 15f, 15f)).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay((0f, 10f, 0f), (0f, 10f, 10f)),
			new BoundedRay((0f, 05f, 0f), (0f, 05f, 05f)).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay((0f, 20f, 0f), (0f, 20f, 20f)),
			new BoundedRay((0f, 25f, 0f), (0f, 25f, 25f)).Clamp(min, max),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceAtPoints() {
		Assert.AreEqual(0f, TestRay.UnboundedDistanceAtPointClosestTo((1f, 2f, -3f)), TestTolerance);
		Assert.AreEqual(10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f), TestTolerance);
		Assert.AreEqual(-10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f), TestTolerance);

		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo((1f, 2f, -3f)), TestTolerance);
		Assert.AreEqual(TestRay.Length, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f), TestTolerance);
		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f), TestTolerance);


		Assert.AreEqual(0f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);
		Assert.AreEqual(10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);
		Assert.AreEqual(-10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);

		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);
		Assert.AreEqual(TestRay.Length, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);
		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineColinearityWithOtherLineLikes() {
		void AssertPair(bool expectation, BoundedRay ray, Ray other, float? lineThickness, Angle? tolerance) {
			var flippedRay = ray.Flipped;
			var otherAsLine = other.ToLine();
			var otherAsFlippedLine = new Line(other.StartPoint, other.Direction.Flipped);
			var otherAsBoundedRay = other.ToBoundedRay(100f);

			// Line
			Assert.AreEqual(expectation, ray.IsApproximatelyColinearWith(otherAsLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsLine.IsApproximatelyColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, ray.IsApproximatelyColinearWith(otherAsFlippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsLine.IsApproximatelyColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedRay.IsApproximatelyColinearWith(otherAsLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsFlippedLine.IsApproximatelyColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedRay.IsApproximatelyColinearWith(otherAsFlippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsFlippedLine.IsApproximatelyColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			// Ray
			Assert.AreEqual(expectation, ray.IsApproximatelyColinearWith(other, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, other.IsApproximatelyColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, ray.IsApproximatelyColinearWith(other.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, other.IsApproximatelyColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedRay.IsApproximatelyColinearWith(other, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, other.Flipped.IsApproximatelyColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedRay.IsApproximatelyColinearWith(other.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, other.Flipped.IsApproximatelyColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			// BoundedRay
			Assert.AreEqual(expectation, ray.IsApproximatelyColinearWith(otherAsBoundedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.IsApproximatelyColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, ray.IsApproximatelyColinearWith(otherAsBoundedRay.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.IsApproximatelyColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedRay.IsApproximatelyColinearWith(otherAsBoundedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.Flipped.IsApproximatelyColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedRay.IsApproximatelyColinearWith(otherAsBoundedRay.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.Flipped.IsApproximatelyColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
		}

		AssertPair(true, TestRay, TestRay.ToRayFromStart(), null, null);
		AssertPair(false, TestRay.MovedBy(TestRay.Direction.AnyOrthogonal() * 1f), TestRay.ToRayFromStart(), 0.45f, null);
		AssertPair(true, TestRay.MovedBy(TestRay.Direction.AnyOrthogonal() * 1f), TestRay.ToRayFromStart(), 0.55f, null);
		AssertPair(false, TestRay.RotatedAroundStartBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)), TestRay.ToRayFromStart(), null, 0.9f);
		AssertPair(true, TestRay.RotatedAroundStartBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)), TestRay.ToRayFromStart(), null, 1.1f);
		AssertPair(false, TestRay.MovedBy(TestRay.Direction.AnyOrthogonal() * 1f).RotatedAroundStartBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)), TestRay.ToRayFromStart(), 0.45f, 0.9f);
		AssertPair(true, TestRay.MovedBy(TestRay.Direction.AnyOrthogonal() * 1f).RotatedAroundStartBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)), TestRay.ToRayFromStart(), 0.55f, 1.1f);
		AssertPair(false, TestRay.RotatedAroundStartBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)).MovedBy(TestRay.Direction.AnyOrthogonal() * 1f), TestRay.ToRayFromStart(), 0.45f, 0.9f);
		AssertPair(true, TestRay.RotatedAroundStartBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)).MovedBy(TestRay.Direction.AnyOrthogonal() * 1f), TestRay.ToRayFromStart(), 0.55f, 1.1f);
	}

	[Test]
	public void ShouldCorrectlyDetermineParallelismWithOtherElements() {
		void AssertCombination(bool expectation, BoundedRay ray, Direction dir, Angle? tolerance) {
			var flippedRay = ray.Flipped;
			var plane = new Plane(dir.AnyOrthogonal(), Location.Origin);
			var dirLine = new Line(Location.Origin, dir);
			var dirRay = new Ray(Location.Origin, dir);
			var dirRayBounded = BoundedRay.FromStartPointAndVect(Location.Origin, dir * 10f);

			if (tolerance == null) {
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dir));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dir));
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dir.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dir.Flipped));

				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(plane));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(plane));
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(plane.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(plane.Flipped));

				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dirLine));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dirLine));
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));

				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dirRay));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dirRay));
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dirRay.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dirRay.Flipped));

				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dirRayBounded));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dirRayBounded));
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dirRayBounded.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dirRayBounded.Flipped));

				Assert.AreEqual(expectation, ray.IsParallelTo(dir));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dir));
				Assert.AreEqual(expectation, ray.IsParallelTo(dir.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dir.Flipped));

				Assert.AreEqual(expectation, ray.IsParallelTo(plane));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(plane));
				Assert.AreEqual(expectation, ray.IsParallelTo(plane.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(plane.Flipped));

				Assert.AreEqual(expectation, ray.IsParallelTo(dirLine));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dirLine));
				Assert.AreEqual(expectation, ray.IsParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));

				Assert.AreEqual(expectation, ray.IsParallelTo(dirRay));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dirRay));
				Assert.AreEqual(expectation, ray.IsParallelTo(dirRay.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dirRay.Flipped));

				Assert.AreEqual(expectation, ray.IsParallelTo(dirRayBounded));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dirRayBounded));
				Assert.AreEqual(expectation, ray.IsParallelTo(dirRayBounded.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dirRayBounded.Flipped));
			}
			else {
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dir.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dir.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(plane.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(plane.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));

				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dirRay.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dirRay.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsApproximatelyParallelTo(dirRayBounded.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyParallelTo(dirRayBounded.Flipped, tolerance.Value));
			}
		}

		AssertCombination(true, new BoundedRay(Location.Origin, (0f, 1f, 0f)), Direction.Up, null);
		AssertCombination(false, new BoundedRay(Location.Origin, (0f, 1f, 0f)), Direction.Left, null);
		AssertCombination(false, new BoundedRay(Location.Origin, (0f, 1f, 0f)), (1f, 1f, 0f), 44f);
		AssertCombination(true, new BoundedRay(Location.Origin, (0f, 1f, 0f)), (1f, 1f, 0f), 46f);

		Assert.AreEqual(false, TestRay.IsApproximatelyParallelTo(Direction.None));
		Assert.AreEqual(false, TestRay.IsApproximatelyParallelTo(new BoundedRay(Location.Origin, Location.Origin)));
		Assert.AreEqual(false, TestRay.IsParallelTo(Direction.None));
		Assert.AreEqual(false, TestRay.IsParallelTo(new BoundedRay(Location.Origin, Location.Origin)));

		Assert.AreEqual(false, new BoundedRay(Location.Origin, Location.Origin).IsApproximatelyParallelTo(Direction.Up));
		Assert.AreEqual(false, new BoundedRay(Location.Origin, Location.Origin).IsParallelTo(Direction.Up));
		Assert.AreEqual(false, new BoundedRay(Location.Origin, Location.Origin).IsApproximatelyParallelTo(Direction.None));
		Assert.AreEqual(false, new BoundedRay(Location.Origin, Location.Origin).IsParallelTo(Direction.None));
	}

	[Test]
	public void ShouldCorrectlyDetermineOrthogonalityWithOtherElements() {
		void AssertCombination(bool expectation, BoundedRay ray, Direction dir, Angle? tolerance) {
			var flippedRay = ray.Flipped;
			var plane = new Plane(dir.AnyOrthogonal(), Location.Origin);
			var dirLine = new Line(Location.Origin, dir);
			var dirRay = new Ray(Location.Origin, dir);
			var dirRayBounded = BoundedRay.FromStartPointAndVect(Location.Origin, dir * 10f);

			if (tolerance == null) {
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dir));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dir));
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dir.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dir.Flipped));

				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(plane));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(plane));
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(plane.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(plane.Flipped));

				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dirLine));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dirLine));
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));

				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dirRay));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dirRay));
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dirRay.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dirRay.Flipped));

				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dirRayBounded));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dirRayBounded));
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dirRayBounded.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dirRayBounded.Flipped));


				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dir));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dir));
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dir.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dir.Flipped));

				Assert.AreEqual(expectation, ray.IsOrthogonalTo(plane));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(plane));
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(plane.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(plane.Flipped));

				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dirLine));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dirLine));
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));

				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dirRay));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dirRay));
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dirRay.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dirRay.Flipped));

				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dirRayBounded));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dirRayBounded));
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dirRayBounded.Flipped));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dirRayBounded.Flipped));
			}
			else {
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dir.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dir.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(plane.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(plane.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));

				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dirRay.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dirRay.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsApproximatelyOrthogonalTo(dirRayBounded.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsApproximatelyOrthogonalTo(dirRayBounded.Flipped, tolerance.Value));
			}
		}

		AssertCombination(true, new BoundedRay(Location.Origin, (0f, 1f, 0f)), Direction.Left, null);
		AssertCombination(false, new BoundedRay(Location.Origin, (0f, 1f, 0f)), Direction.Up, null);
		AssertCombination(false, new BoundedRay(Location.Origin, (0f, 1f, 0f)), (1f, 1f, 0f), 44f);
		AssertCombination(true, new BoundedRay(Location.Origin, (0f, 1f, 0f)), (1f, 1f, 0f), 46f);

		Assert.AreEqual(false, TestRay.IsApproximatelyOrthogonalTo(Direction.None));
		Assert.AreEqual(false, TestRay.IsApproximatelyOrthogonalTo(new BoundedRay(Location.Origin, Location.Origin)));
		Assert.AreEqual(false, TestRay.IsOrthogonalTo(Direction.None));
		Assert.AreEqual(false, TestRay.IsOrthogonalTo(new BoundedRay(Location.Origin, Location.Origin)));

		Assert.AreEqual(false, new BoundedRay(Location.Origin, Location.Origin).IsApproximatelyOrthogonalTo(Direction.Up));
		Assert.AreEqual(false, new BoundedRay(Location.Origin, Location.Origin).IsOrthogonalTo(Direction.Up));
		Assert.AreEqual(false, new BoundedRay(Location.Origin, Location.Origin).IsApproximatelyOrthogonalTo(Direction.None));
		Assert.AreEqual(false, new BoundedRay(Location.Origin, Location.Origin).IsOrthogonalTo(Direction.None));
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithDirectionsAndLineLikes() {
		void AssertAgainstLeft(BoundedRay? expectation, BoundedRay input) {
			Assert.AreEqual(expectation, input.ParallelizedWith(Direction.Left));
			Assert.AreEqual(expectation, input.ParallelizedWith(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.ParallelizedWith(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.ParallelizedWith(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}
		void AssertFastAgainstLeft(BoundedRay expectation, BoundedRay input) {
			Assert.AreEqual(expectation, input.FastParallelizedWith(Direction.Left));
			Assert.AreEqual(expectation, input.FastParallelizedWith(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastParallelizedWith(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastParallelizedWith(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}

		// Various parallelizations from behind the plane
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f) * 10f)
		);

		// Various parallelizations from in front the dir
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f) * 10f)
		);

		// Parallelizations from perpendicular directions
		AssertAgainstLeft(
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Up * 10f)
		);
		AssertAgainstLeft(
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Down * 10f)
		);
		AssertAgainstLeft(
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Up * 10f)
		);
		AssertAgainstLeft(
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Down * 10f)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstDirectionsAndLineLikes() {
		void AssertAgainstLeft(BoundedRay? expectation, BoundedRay input) {
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(Direction.Left));
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}
		void AssertFastAgainstLeft(BoundedRay expectation, BoundedRay input) {
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(Direction.Left));
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}

		// Various orthogonalizations from behind the plane
		AssertAgainstLeft(
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Left * 10f)
		);
		AssertAgainstLeft(
			null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Right * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Up * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Up * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Down * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Down * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Up * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Up * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Down * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Down * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f) * 10f)
		);

		// Various orthogonalizations from in front the plane
		AssertAgainstLeft(
		null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 10f)
		);
		AssertAgainstLeft(
		null,
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Right * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Up * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Up * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Down * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f) * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Down * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Up * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Up * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Down * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f) * 10f)
		);
		AssertFastAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Down * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f) * 10f)
		);

		// Orthogonalizations from perpendicular directions
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Up * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Up * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Down * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Down * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Up * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Up * 10f)
		);
		AssertAgainstLeft(
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Down * 10f),
			BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), Direction.Down * 10f)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAndParallelizeAroundPoints() {
		const float TestRayLength = 10f;
		const float TestPivotDistance = 3f;

		var testList = new List<Direction>();
		for (var x = -3f; x <= 3f; x += 1f) {
			for (var y = -3f; y <= 3f; y += 1f) {
				for (var z = -3f; z <= 3f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var rayDir = testList[i];
			var ray = BoundedRay.FromStartPointAndVect(Location.Origin, rayDir * TestRayLength);
			for (var j = i; j < testList.Count; ++j) {
				var targetDir = testList[j];
				var targetRayBounded = new BoundedRay(Location.Origin, Location.Origin + targetDir * TestRayLength);

				if (targetDir == Direction.None) {
					AssertToleranceEquals(ray, ray.ParallelizedWith(targetDir), TestTolerance);
					AssertToleranceEquals(ray, ray.ParallelizedAroundStartWith(targetDir), TestTolerance);
					AssertToleranceEquals(ray, ray.ParallelizedAroundMiddleWith(targetDir), TestTolerance);
					AssertToleranceEquals(ray, ray.ParallelizedAroundEndWith(targetDir), TestTolerance);
					AssertToleranceEquals(ray, ray.ParallelizedWith(targetDir, TestPivotDistance), TestTolerance);
					AssertToleranceEquals(ray, ray.OrthogonalizedAgainst(targetDir), TestTolerance);
					AssertToleranceEquals(ray, ray.OrthogonalizedAroundStartAgainst(targetDir), TestTolerance);
					AssertToleranceEquals(ray, ray.OrthogonalizedAroundMiddleAgainst(targetDir), TestTolerance);
					AssertToleranceEquals(ray, ray.OrthogonalizedAroundEndAgainst(targetDir), TestTolerance);
					AssertToleranceEquals(ray, ray.OrthogonalizedAgainst(targetDir, TestPivotDistance), TestTolerance);

					AssertToleranceEquals(ray, ray.ParallelizedWith(targetRayBounded), TestTolerance);
					AssertToleranceEquals(ray, ray.ParallelizedAroundStartWith(targetRayBounded), TestTolerance);
					AssertToleranceEquals(ray, ray.ParallelizedAroundMiddleWith(targetRayBounded), TestTolerance);
					AssertToleranceEquals(ray, ray.ParallelizedAroundEndWith(targetRayBounded), TestTolerance);
					AssertToleranceEquals(ray, ray.ParallelizedWith(targetRayBounded, TestPivotDistance), TestTolerance);
					AssertToleranceEquals(ray, ray.OrthogonalizedAgainst(targetRayBounded), TestTolerance);
					AssertToleranceEquals(ray, ray.OrthogonalizedAroundStartAgainst(targetRayBounded), TestTolerance);
					AssertToleranceEquals(ray, ray.OrthogonalizedAroundMiddleAgainst(targetRayBounded), TestTolerance);
					AssertToleranceEquals(ray, ray.OrthogonalizedAroundEndAgainst(targetRayBounded), TestTolerance);
					AssertToleranceEquals(ray, ray.OrthogonalizedAgainst(targetRayBounded, TestPivotDistance), TestTolerance);
					continue;
				}

				var targetLine = new Line(Location.Origin, targetDir);
				var targetRay = new Ray(Location.Origin, targetDir);
				var targetPlane = new Plane(targetDir.AnyOrthogonal(), 0f);
				var allTargets = new object[] { targetDir, targetLine, targetRay, targetRayBounded, targetPlane };

				void AssertAllTrue(Func<BoundedRay?, bool> assertionPredicate,
					bool includeParallelizations = true, bool includeOrthogonalizations = true,
					bool includeStandardFuncs = true, bool includeStartFuncs = true, bool includeMiddleFuncs = true,
					bool includeEndFuncs = true, bool includePivotFuncs = true) => AssertAll(result => Assert.IsTrue(assertionPredicate(result)), includeParallelizations, includeOrthogonalizations, includeStandardFuncs, includeStartFuncs, includeMiddleFuncs, includeEndFuncs, includePivotFuncs);
				void AssertAllNullOr(Action<BoundedRay> assertionAction,
					bool includeParallelizations = true, bool includeOrthogonalizations = true,
					bool includeStandardFuncs = true, bool includeStartFuncs = true, bool includeMiddleFuncs = true,
					bool includeEndFuncs = true, bool includePivotFuncs = true) => AssertAll(result => { if (result == null) return; assertionAction(result.Value); }, includeParallelizations, includeOrthogonalizations, includeStandardFuncs, includeStartFuncs, includeMiddleFuncs, includeEndFuncs, includePivotFuncs);
				void AssertAll(Action<BoundedRay?> assertionAction,
					bool includeParallelizations = true, bool includeOrthogonalizations = true,
					bool includeStandardFuncs = true, bool includeStartFuncs = true, bool includeMiddleFuncs = true,
					bool includeEndFuncs = true, bool includePivotFuncs = true
				) {
					void TestMethod(string nonFastMethodName, params object[] args) {
						BoundedRay? result = null;
						try {
							result = (BoundedRay?) typeof(BoundedRay).GetMethod(nonFastMethodName, args.Select(o => o.GetType()).ToArray())!.Invoke(ray, args);
							assertionAction(result);
							if (result != null && ray.StartToEndVect != Vect.Zero) {
								nonFastMethodName = "Fast" + nonFastMethodName;
								result = ((BoundedRay?) typeof(BoundedRay).GetMethod(nonFastMethodName, args.Select(o => o.GetType()).ToArray())!.Invoke(ray, args))!.Value;
								assertionAction(result);
							}
						}
						catch {
							Console.WriteLine($"Failure details:");
							Console.WriteLine("\tInput: " + ray.ToStringDescriptive());
							Console.WriteLine("\tFunc: " + nonFastMethodName);
							Console.WriteLine("\tTarget: " + args[0]);
							Console.WriteLine("\tResult: " + (result?.ToStringDescriptive() ?? "<null>"));
							Console.WriteLine("\tRay dir: " + rayDir);
							Console.WriteLine("\tTarget dir: " + targetDir);
							Console.WriteLine("\tRay/Target angle: " + (rayDir ^ targetDir));
							Console.WriteLine("\tTargets: " + System.Environment.NewLine + "\t\t" + String.Join(System.Environment.NewLine + "\t\t", allTargets));
							throw;
						}
					}

					foreach (var target in allTargets!) {
						if (includeParallelizations) {
							if (includeStandardFuncs) TestMethod(nameof(BoundedRay.ParallelizedWith), target);
							if (includeStartFuncs) TestMethod(nameof(BoundedRay.ParallelizedAroundStartWith), target);
							if (includeMiddleFuncs) TestMethod(nameof(BoundedRay.ParallelizedAroundMiddleWith), target);
							if (includeEndFuncs) TestMethod(nameof(BoundedRay.ParallelizedAroundEndWith), target);
							if (includePivotFuncs) TestMethod(nameof(BoundedRay.ParallelizedWith), target, TestPivotDistance);
						}
						if (includeOrthogonalizations) {
							if (includeStandardFuncs) TestMethod(nameof(BoundedRay.OrthogonalizedAgainst), target);
							if (includeStartFuncs) TestMethod(nameof(BoundedRay.OrthogonalizedAroundStartAgainst), target);
							if (includeMiddleFuncs) TestMethod(nameof(BoundedRay.OrthogonalizedAroundMiddleAgainst), target);
							if (includeEndFuncs) TestMethod(nameof(BoundedRay.OrthogonalizedAroundEndAgainst), target);
							if (includePivotFuncs) TestMethod(nameof(BoundedRay.OrthogonalizedAgainst), target, TestPivotDistance);
						}
					}
				}

				if (rayDir == Direction.None) {
					AssertAllTrue(
						result => result == ray
					);
				}
				else {
					AssertAllNullOr(
						result => Assert.AreEqual(TestRayLength, result.Length, TestTolerance)
					);
					AssertAllNullOr(
						result => AssertToleranceEquals(ray.StartPoint, result.StartPoint, TestTolerance),
						includeMiddleFuncs: false, includeEndFuncs: false, includePivotFuncs: false
					);
					AssertAllNullOr(
						result => AssertToleranceEquals(ray.EndPoint, result.EndPoint, TestTolerance),
						includeStandardFuncs: false, includeStartFuncs: false, includeMiddleFuncs: false, includePivotFuncs: false
					);
					AssertAllNullOr(
						result => AssertToleranceEquals(ray.LocationAtDistanceOrNull(TestRayLength * 0.5f), result.LocationAtDistanceOrNull(TestRayLength * 0.5f), TestTolerance),
						includeStandardFuncs: false, includeStartFuncs: false, includeEndFuncs: false, includePivotFuncs: false
					);
					AssertAllNullOr(
						result => AssertToleranceEquals(ray.LocationAtDistanceOrNull(TestPivotDistance), result.LocationAtDistanceOrNull(TestPivotDistance), TestTolerance),
						includeStandardFuncs: false, includeStartFuncs: false, includeMiddleFuncs: false, includeEndFuncs: false
					);

					Assert.IsTrue(allTargets[^1].Equals(targetPlane));
					allTargets = allTargets[..^1]; // Exclude the testPlane from these two tests

					if (rayDir.IsParallelTo(targetDir)) {
						AssertAllTrue(
							result => result == null,
							includeParallelizations: false
						);
						AssertAllTrue(
							result => result != null,
							includeOrthogonalizations: false
						);
					}
					if (rayDir.IsOrthogonalTo(targetDir)) {
						AssertAllTrue(
							result => result == null,
							includeOrthogonalizations: false
						);
						AssertAllTrue(
							result => result != null,
							includeParallelizations: false
						);
					}
				}
			}
		}
	}
}