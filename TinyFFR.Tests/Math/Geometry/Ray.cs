// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class RayTest {
	const float TestTolerance = 0.001f;
	static readonly Ray TestRay = new(new Location(1f, 2f, -3f), new Direction(-1f, -2f, 3f));

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<Ray>();

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		Assert.AreEqual(new Location(1f, 2f, -3f), TestRay.StartPoint);
		Assert.AreEqual(new Direction(-1f, -2f, 3f), TestRay.Direction);
		Assert.AreEqual(false, ((ILineLike) TestRay).IsUnboundedInBothDirections);
		Assert.AreEqual(null, ((ILineLike) TestRay).Length);
		Assert.AreEqual(null, ((ILineLike) TestRay).LengthSquared);
		Assert.AreEqual(null, ((ILineLike) TestRay).StartToEndVect);
		Assert.AreEqual(null, ((ILineLike) TestRay).EndPoint);
		Assert.AreEqual(false, ((ILineLike) TestRay).IsFiniteLength);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "Ray[StartPoint <1.0, 2.0, -3.0> | Direction <-0.3, -0.5, 0.8>]";
		Assert.AreEqual(Expectation, TestRay.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestRay.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

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
			var val = Ray.Random(start, end);
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

	[Test]
	public void ShouldCorrectlyConvertToLine() {
		Assert.AreEqual(new Line(TestRay.StartPoint, TestRay.Direction), TestRay.ToLine());
	}

	[Test]
	public void ShouldCorrectlyConvertToBoundedLine() {
		AssertToleranceEquals(new BoundedRay(TestRay.StartPoint, TestRay.StartPoint + TestRay.Direction * 10f), TestRay.ToBoundedRay(10f), TestTolerance);
		AssertToleranceEquals(TestRay.Direction, TestRay.ToBoundedRay(10f).Direction, TestTolerance);
		AssertToleranceEquals(new BoundedRay(TestRay.StartPoint, TestRay.StartPoint + TestRay.Direction * -10f), TestRay.ToBoundedRay(-10f), TestTolerance);
		AssertToleranceEquals(-TestRay.Direction, TestRay.ToBoundedRay(-10f).Direction, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFlip() {
		Assert.AreEqual(
			new Ray(new Location(10f, -20f, 0f), Direction.Up),
			-new Ray(new Location(10f, -20f, 0f), Direction.Down)
		);
	}

	[Test]
	public void ShouldCorrectlyRotate() {
		var rotation = 70f % Direction.Down;

		AssertToleranceEquals(
			new Ray(TestRay.StartPoint, TestRay.Direction * rotation),
			TestRay * rotation,
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray(Location.Origin, Direction.Right),
			new Ray(Location.Origin, Direction.Left) * (Direction.Up % 180f),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyRotateAroundPoints() {
		void AssertCombination(Ray expectation, Ray input, Location pivotPoint, Rotation rotation) {
			AssertToleranceEquals(expectation, input.RotatedBy(rotation, pivotPoint), TestTolerance);
			Assert.AreEqual(input.RotatedBy(rotation, pivotPoint), input * (pivotPoint, rotation));
			Assert.AreEqual(input.RotatedBy(rotation, pivotPoint), input * (rotation, pivotPoint));
			Assert.AreEqual(input.RotatedBy(rotation, pivotPoint), (pivotPoint, rotation) * input);
			Assert.AreEqual(input.RotatedBy(rotation, pivotPoint), (rotation, pivotPoint) * input);
		}

		AssertCombination(new Ray((0f, 0f, 10f), Direction.Backward), new Ray(Location.Origin, Direction.Forward), (0f, 0f, 5f), Direction.Down % 180f);
		AssertCombination(new Ray(Location.Origin, Direction.Forward), new Ray(Location.Origin, Direction.Forward), (0f, 0f, 5f), Direction.Forward % 180f);

		AssertToleranceEquals(
			new Ray(Location.Origin, Direction.Right),
			new Ray(Location.Origin, Direction.Up).RotatedBy(90f % Direction.Forward, 0f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray(new(5f, 5f, 0f), Direction.Right),
			new Ray(Location.Origin, Direction.Up).RotatedBy(90f % Direction.Forward, 5f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray((-5f, -5f, 0f), Direction.Right),
			new Ray(Location.Origin, Direction.Up).RotatedBy(90f % Direction.Forward, -5f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray(new(10f, 10f, 0f), Direction.Right),
			new Ray(Location.Origin, Direction.Up).RotatedBy(90f % Direction.Forward, 10f),
			TestTolerance
		);

		AssertToleranceEquals(
			TestRay.RotatedBy(15f % Direction.Down, Location.Origin),
			TestRay.RotatedAroundOriginBy(15f % Direction.Down),
			TestTolerance
		);
		AssertToleranceEquals(
			TestRay.RotatedBy(-87f % Direction.Right, Location.Origin),
			TestRay.RotatedAroundOriginBy(-87f % Direction.Right),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyMove() {
		var vect = new Vect(5f, -3f, 12f);

		AssertToleranceEquals(
			new Ray(TestRay.StartPoint + vect, TestRay.Direction),
			TestRay + vect,
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray(TestRay.StartPoint + vect, TestRay.Direction),
			vect + TestRay,
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToLocation() {
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Ray(new Location(-100f, 0f, 0f), Direction.Left).PointClosestTo(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(-100f, 0f, 0f),
			new Ray(new Location(0f, 0f, 0f), Direction.Right).PointClosestTo(new Location(-100f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			new Ray(new Location(100f, 0f, 0f), Direction.Left).PointClosestTo(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Ray(new Location(0f, 0f, 0f), Direction.Left).PointClosestTo(new Location(-100f, 1f, 0f))
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToOrigin() {
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Ray(new Location(100f, 0f, 0f), Direction.Right).PointClosestToOrigin()
		);
		Assert.AreEqual(
			new Location(0f, -1f, 0f),
			new Ray(new Location(100f, -1f, 0f), Direction.Right).PointClosestToOrigin()
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			new Ray(new Location(100f, 0f, 0f), Direction.Left).PointClosestToOrigin()
		);
		Assert.AreEqual(
			new Location(100f, -1f, 0f),
			new Ray(new Location(100f, -1f, 0f), Direction.Left).PointClosestToOrigin()
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLocation() {
		Assert.AreEqual(
			1f,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).DistanceFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			1f,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).DistanceFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			0f,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).DistanceFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			MathF.Sqrt(2f),
			new Ray(new Location(100f, 0f, 0f), Direction.Right).DistanceFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			MathF.Sqrt(10001f),
			new Ray(new Location(100f, 0f, 0f), Direction.Left).DistanceFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			MathF.Sqrt(10001f),
			new Ray(new Location(100f, 0f, 0f), Direction.Left).DistanceFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			200f,
			new Ray(new Location(100f, 0f, 0f), Direction.Left).DistanceFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			MathF.Sqrt(40002f),
			new Ray(new Location(100f, 0f, 0f), Direction.Left).DistanceFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			1f,
			new Ray(new Location(0f, 1f, 0f), Direction.Left).DistanceFromOrigin()
		);
		Assert.AreEqual(
			1f,
			new Ray(new Location(0f, 1f, 0f), Direction.Left).DistanceFromOrigin()
		);
		Assert.AreEqual(
			0f,
			new Ray(new Location(0f, 0f, 0f), Direction.Left).DistanceFromOrigin()
		);
		Assert.AreEqual(
			1f,
			new Ray(new Location(1f, 0f, 0f), Direction.Left).DistanceFromOrigin()
		);
		Assert.AreEqual(
			0f,
			new Ray(new Location(-1f, 0f, 0f), Direction.Left).DistanceFromOrigin()
		);

		// Squared
		Assert.AreEqual(
			1f,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).DistanceSquaredFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			1f,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).DistanceSquaredFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			0f,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).DistanceSquaredFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			2f,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).DistanceSquaredFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			10001f,
			new Ray(new Location(100f, 0f, 0f), Direction.Left).DistanceSquaredFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			10001f,
			new Ray(new Location(100f, 0f, 0f), Direction.Left).DistanceSquaredFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			40_000f,
			new Ray(new Location(100f, 0f, 0f), Direction.Left).DistanceSquaredFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			40002f,
			new Ray(new Location(100f, 0f, 0f), Direction.Left).DistanceSquaredFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			1f,
			new Ray(new Location(0f, 1f, 0f), Direction.Left).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			1f,
			new Ray(new Location(0f, 1f, 0f), Direction.Left).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			0f,
			new Ray(new Location(0f, 0f, 0f), Direction.Left).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			1f,
			new Ray(new Location(1f, 0f, 0f), Direction.Left).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			0f,
			new Ray(new Location(-1f, 0f, 0f), Direction.Left).DistanceSquaredFromOrigin()
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineContainmentOfLocation() {
		Assert.AreEqual(
			false,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).Contains(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			true,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).Contains(new Location(0f, 1f, 0f), 1.1f)
		);
		Assert.AreEqual(
			false,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).Contains(new Location(0f, 1f, 0f), 0.9f)
		);
		Assert.AreEqual(
			true,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).Contains(new Location(0f, -1f, 0f), 1.1f)
		);
		Assert.AreEqual(
			false,
			new Ray(new Location(100f, 0f, 0f), Direction.Right).Contains(new Location(0f, -1f, 0f), 0.9f)
		);
		Assert.AreEqual(
			false,
			new Ray(new Location(100f, 0f, 0f), Direction.Left).Contains(new Location(99f, 0f, 0f), 0.9f)
		);
		Assert.AreEqual(
			true,
			new Ray(new Location(100f, 0f, 0f), Direction.Left).Contains(new Location(99f, 0f, 0f), 1.1f)
		);
		Assert.AreEqual(
			true,
			new Ray(new Location(100f, 0f, 0f), Direction.Left).Contains(new Location(100f, 0f, 0f))
		);
		Assert.AreEqual(
			true,
			new Ray(new Location(100f, 0f, 0f), Direction.Left).Contains(new Location(110f, 0f, 0f))
		);
	}

	[Test]
	public void ShouldCorrectlyReturnClosestPointToOtherLine() {
		void AssertPair<TLine>(Location expectedResult, Ray ray, TLine other) where TLine : ILineLike {
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
			new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 1f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			new Ray(Location.Origin, Direction.Up),
			new Line(new Location(100f, 10f, 0f), Direction.Left)
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new Ray(Location.Origin, Direction.Up),
			new Line(new Location(100f, -10f, 0f), Direction.Left)
		);

		// Ray
		AssertPair(
			new Location(0f, 20f, 0),
			new Ray(Location.Origin, Direction.Up),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 1f, 1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			new Ray(Location.Origin, Direction.Up),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -1f, 1f))
		);
		AssertPair(
			new Location(0f, 30f, 0),
			new Ray(Location.Origin, Direction.Up),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 2f, 1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			new Ray(Location.Origin, Direction.Up),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -1.5f, 1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			new Ray(Location.Origin, Direction.Up),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -2.5f, 1f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			new Ray(Location.Origin, Direction.Up),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -2.5f, -1f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			new Ray(Location.Origin, Direction.Up),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 1f, -1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			new Ray(Location.Origin, Direction.Up),
			new Ray(new Location(0f, -1f, -10f), Direction.Forward)
		);
		AssertPair(
			new Location(0f, 0f, 0),
			new Ray(Location.Origin, Direction.Up),
			new Ray(new Location(0f, 1f, -1f), new Direction(0f, -100f, 0.1f))
		);

		// BoundedRay
		AssertPair(
			new Location(0f, 20f, 0),
			new Ray(Location.Origin, Direction.Up),
			new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 20f, 0),
			new Ray(Location.Origin, Direction.Up),
			new BoundedRay(new Location(0f, 30f, 10f), new Location(0f, 10f, -10f))
		);
		AssertPair(
			new Location(0f, 30f, 0f),
			new Ray(Location.Origin, Direction.Up),
			new BoundedRay(new Location(0f, 30f, 10f), new Location(0f, 10f, 30f))
		);
		AssertPair(
			new Location(0f, 30f, 0f),
			new Ray(Location.Origin, Direction.Up),
			new BoundedRay(new Location(0f, 10f, 30f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new Ray(Location.Origin, Direction.Up),
			new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, 0f, 10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new Ray(Location.Origin, Direction.Up),
			new BoundedRay(new Location(0f, 0f, 10f), new Location(0f, -10f, -10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new Ray(Location.Origin, Direction.Up),
			new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, -10f, 0f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new Ray(Location.Origin, Direction.Up),
			new BoundedRay(new Location(0f, -10f, 0f), new Location(0f, -10f, -10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new Ray(Location.Origin, Direction.Up),
			new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, -50f, -10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new Ray(Location.Origin, Direction.Up),
			new BoundedRay(new Location(0f, -50f, -10f), new Location(0f, -10f, -10f))
		);
		Assert.GreaterOrEqual(new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).Y, 0f);
		Assert.LessOrEqual(new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).Y, 50f);
		Assert.AreEqual(0f, new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).X);
		Assert.AreEqual(0f, new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).Z);
		Assert.GreaterOrEqual(new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).Y, 0f);
		Assert.LessOrEqual(new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).Y, 50f);
		Assert.AreEqual(0f, new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).X);
		Assert.AreEqual(0f, new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).Z);

		Assert.GreaterOrEqual(new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).Y, 10f);
		Assert.LessOrEqual(new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).Y, 50f);
		Assert.AreEqual(0f, new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).X);
		Assert.AreEqual(0f, new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).Z);
		Assert.GreaterOrEqual(new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).Y, 10f);
		Assert.LessOrEqual(new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).Y, 50f);
		Assert.AreEqual(0f, new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).X);
		Assert.AreEqual(0f, new Ray(Location.Origin, Direction.Up).PointClosestTo(new BoundedRay(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).Z);
	}

	[Test]
	public void ShouldCorrectlyCalculateDistanceFromLines() { // These are regression tests
		Assert.AreEqual(
			16.738178f,
			TestRay.DistanceFrom(new Line(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			18.053491f,
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
			TestRay.DistanceFrom(TestRay),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestRay.DistanceFrom(TestRay.ToBoundedRay(1f)),
			TestTolerance
		);

		// Squared
		Assert.AreEqual(
			16.738178f * 16.738178f,
			TestRay.DistanceSquaredFrom(new Line(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			18.053491f * 18.053491f,
			TestRay.DistanceSquaredFrom(new Ray(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			17.34369f * 17.34369f,
			TestRay.DistanceSquaredFrom(BoundedRay.FromStartPointAndVect(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f) * -4f)),
			TestTolerance
		);

		Assert.AreEqual(
			0f,
			TestRay.DistanceSquaredFrom(TestRay.ToLine()),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestRay.DistanceSquaredFrom(TestRay),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestRay.DistanceSquaredFrom(TestRay.ToBoundedRay(1f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyImplementLocationAtDistanceFunctions() {
		var ray = new Ray(new Location(0f, 1f, 0f), Direction.Right);

		Assert.AreEqual(false, ray.DistanceIsWithinLineBounds(-30000f));
		Assert.AreEqual(true, ray.DistanceIsWithinLineBounds(30000f));
		Assert.AreEqual(true, ray.DistanceIsWithinLineBounds(0f));

		Assert.AreEqual(0f, ray.BindDistance(-30000f));
		Assert.AreEqual(30000f, ray.BindDistance(30000f));
		Assert.AreEqual(0f, ray.BindDistance(0f));

		Assert.AreEqual(new Location(0f, 1f, 0f), ray.BoundedLocationAtDistance(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), ray.BoundedLocationAtDistance(3f));
		Assert.AreEqual(new Location(0f, 1f, 0f), ray.BoundedLocationAtDistance(-3f));

		Assert.AreEqual(new Location(0f, 1f, 0f), ray.UnboundedLocationAtDistance(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), ray.UnboundedLocationAtDistance(3f));
		Assert.AreEqual(new Location(3f, 1f, 0f), ray.UnboundedLocationAtDistance(-3f));

		Assert.AreEqual(new Location(0f, 1f, 0f), ray.LocationAtDistanceOrNull(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), ray.LocationAtDistanceOrNull(3f));
		Assert.AreEqual(null, ray.LocationAtDistanceOrNull(-3f));
	}

	[Test]
	public void ShouldCorrectlyDetectLineIntersections() {
		// Line
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Line(new Location(-1f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);

		// Ray
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(-1f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 1.01f
			)
		);

		// BoundedRay
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(0f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedRay(new Location(0f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.AreEqual(
			new Location(0f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedRay(new Location(-1f, 1f, 0f), new Location(0f, 1f, 0f))
			)
		);
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedRay(new Location(-1f, 1f, 0f), new Location(-2f, 1f, 0f))
			)
		);
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedRay(new Location(-2f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.AreEqual(
			new Location(10f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedRay(new Location(10f, 2f, 0f), new Location(10f, 0f, 0f))
			)
		);
		Assert.Null(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedRay(new Location(10f, 2f, 0f), new Location(10f, 4f, 0f))
			)
		);



		// Line, Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);

		// Ray, Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(-1f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 1.01f
			)
		);

		// BoundedRay, Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(0f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				new BoundedRay(new Location(0f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.AreEqual(
			new Location(0f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				new BoundedRay(new Location(-1f, 1f, 0f), new Location(0f, 1f, 0f))
			)
		);
		Assert.AreEqual(
			new Location(10f, 1f, 0f),
			new Ray(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				new BoundedRay(new Location(10f, 2f, 0f), new Location(10f, 0f, 0f))
			)
		);
	}

	[Test]
	public void ShouldCorrectlyTestForLineIntersections() {
		// Line
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Line(new Location(-1f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);

		// Ray
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 1.01f
			)
		);

		// BoundedRay
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new BoundedRay(new Location(0f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new BoundedRay(new Location(-1f, 1f, 0f), new Location(0f, 1f, 0f))
			)
		);
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new BoundedRay(new Location(-1f, 1f, 0f), new Location(-2f, 1f, 0f))
			)
		);
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new BoundedRay(new Location(-2f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.True(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new BoundedRay(new Location(10f, 2f, 0f), new Location(10f, 0f, 0f))
			)
		);
		Assert.False(
			new Ray(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new BoundedRay(new Location(10f, 2f, 0f), new Location(10f, 4f, 0f))
			)
		);
	}

	[Test]
	public void ShouldCorrectlyReflectOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Ray(new Location(100f, 1f, 0f), Direction.Up),
			new Ray(new Location(100f, 100f, 0f), Direction.Down).ReflectedBy(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(100f, 1f, 0f), Direction.Down),
			new Ray(new Location(100f, -100f, 0f), Direction.Up).ReflectedBy(plane)
		);
		AssertToleranceEquals(
			new Ray(new Location(0f, 1f, 0f), new Direction(1f, 1f, -1f)),
			new Ray(new Location(-1f, 2f, 1f), new Direction(1f, -1f, -1f)).ReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray(new Location(0f, 1f, 0f), new Direction(-2f, -1f, 2f)),
			new Ray(new Location(2f, 0f, -2f), new Direction(-2f, 1f, 2f)).ReflectedBy(plane),
			TestTolerance
		);
		Assert.Null(
			new Ray(new Location(0f, 2f, 0f), Direction.Right).ReflectedBy(plane)
		);
		Assert.Null(
			new Ray(new Location(0f, 0f, 0f), Direction.Right).ReflectedBy(plane)
		);
		Assert.Null(
			new Ray(new Location(100f, 100f, 0f), Direction.Up).ReflectedBy(plane)
		);
		Assert.Null(
			new Ray(new Location(100f, -100f, 0f), Direction.Down).ReflectedBy(plane)
		);

		// Fast
		Assert.AreEqual(
			new Ray(new Location(100f, 1f, 0f), Direction.Up),
			new Ray(new Location(100f, 100f, 0f), Direction.Down).FastReflectedBy(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(100f, 1f, 0f), Direction.Down),
			new Ray(new Location(100f, -100f, 0f), Direction.Up).FastReflectedBy(plane)
		);
		AssertToleranceEquals(
			new Ray(new Location(0f, 1f, 0f), new Direction(1f, 1f, -1f)),
			new Ray(new Location(-1f, 2f, 1f), new Direction(1f, -1f, -1f)).FastReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray(new Location(0f, 1f, 0f), new Direction(-2f, -1f, 2f)),
			new Ray(new Location(2f, 0f, -2f), new Direction(-2f, 1f, 2f)).FastReflectedBy(plane),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineIncidentAngleOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			Angle.Zero,
			new Ray(new Location(100f, 100f, 0f), Direction.Down).IncidentAngleWith(plane)
		);
		Assert.AreEqual(
			Angle.Zero,
			new Ray(new Location(100f, -100f, 0f), Direction.Up).IncidentAngleWith(plane)
		);
		AssertToleranceEquals(
			Angle.EighthCircle,
			new Ray(new Location(-1f, 2f, 1f), new Direction(1f, -1f, 0f)).IncidentAngleWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			Angle.FromRadians(MathF.Acos(1f / 3f)),
			new Ray(new Location(2f, 0f, -2f), new Direction(-2f, 1f, 2f)).IncidentAngleWith(plane),
			TestTolerance
		);
		Assert.Null(
			new Ray(new Location(0f, 2f, 0f), Direction.Right).IncidentAngleWith(plane)
		);
		Assert.Null(
			new Ray(new Location(0f, 0f, 0f), Direction.Right).IncidentAngleWith(plane)
		);
		Assert.Null(
			new Ray(new Location(100f, 100f, 0f), Direction.Up).IncidentAngleWith(plane)
		);
		Assert.Null(
			new Ray(new Location(100f, -100f, 0f), Direction.Down).IncidentAngleWith(plane)
		);

		// Fast
		Assert.AreEqual(
			Angle.Zero,
			new Ray(new Location(100f, 100f, 0f), Direction.Down).FastIncidentAngleWith(plane)
		);
		Assert.AreEqual(
			Angle.Zero,
			new Ray(new Location(100f, -100f, 0f), Direction.Up).FastIncidentAngleWith(plane)
		);
		AssertToleranceEquals(
			Angle.EighthCircle,
			new Ray(new Location(-1f, 2f, 1f), new Direction(1f, -1f, 0f)).FastIncidentAngleWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			Angle.FromRadians(MathF.Acos(1f / 3f)),
			new Ray(new Location(2f, 0f, -2f), new Direction(-2f, 1f, 2f)).FastIncidentAngleWith(plane),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineIntersectionPointWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(100f, 100f, 0f), Direction.Down).IntersectionWith(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(100f, -100f, 0f), Direction.Up).IntersectionWith(plane)
		);
		Assert.Null(
			new Ray(new Location(0f, 2f, 0f), Direction.Right).IntersectionWith(plane)
		);
		Assert.Null(
			new Ray(new Location(0f, 0f, 0f), Direction.Right).IntersectionWith(plane)
		);
		Assert.Null(
			new Ray(new Location(100f, 100f, 0f), Direction.Up).IntersectionWith(plane)
		);
		Assert.Null(
			new Ray(new Location(100f, -100f, 0f), Direction.Down).IntersectionWith(plane)
		);

		// Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(100f, 100f, 0f), Direction.Down).FastIntersectionWith(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(100f, -100f, 0f), Direction.Up).FastIntersectionWith(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyTestForIntersectionWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.True(
			new Ray(new Location(100f, 100f, 0f), Direction.Down).IsIntersectedBy(plane)
		);
		Assert.True(
			new Ray(new Location(100f, -100f, 0f), Direction.Up).IsIntersectedBy(plane)
		);
		Assert.False(
			new Ray(new Location(0f, 2f, 0f), Direction.Right).IsIntersectedBy(plane)
		);
		Assert.False(
			new Ray(new Location(0f, 0f, 0f), Direction.Right).IsIntersectedBy(plane)
		);
		Assert.False(
			new Ray(new Location(100f, 100f, 0f), Direction.Up).IsIntersectedBy(plane)
		);
		Assert.False(
			new Ray(new Location(100f, -100f, 0f), Direction.Down).IsIntersectedBy(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineSignedDistanceFromPlane() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		void AssertDistance(float expectedSignedDistance, Ray ray) {
			Assert.AreEqual(expectedSignedDistance, ray.SignedDistanceFrom(plane));
			Assert.AreEqual(MathF.Abs(expectedSignedDistance), ray.DistanceFrom(plane));
		}

		AssertDistance(
			0f,
			new Ray(new Location(100f, 100f, 0f), Direction.Down)
		);
		AssertDistance(
			0f,
			new Ray(new Location(100f, -100f, 0f), Direction.Up)
		);
		AssertDistance(
			1f,
			new Ray(new Location(0f, 2f, 0f), Direction.Right)
		);
		AssertDistance(
			-1f,
			new Ray(new Location(0f, 0f, 0f), Direction.Right)
		);
		AssertDistance(
			99f,
			new Ray(new Location(100f, 100f, 0f), Direction.Up)
		);
		AssertDistance(
			-101f,
			new Ray(new Location(100f, -100f, 0f), Direction.Down)
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(100f, 100f, 0f), Direction.Down).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(100f, -100f, 0f), Direction.Up).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(0f, 2f, 0f),
			new Ray(new Location(0f, 2f, 0f), Direction.Right).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Ray(new Location(0f, 0f, 0f), Direction.Right).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 100f, 0f),
			new Ray(new Location(100f, 100f, 0f), Direction.Up).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, -100f, 0f),
			new Ray(new Location(100f, -100f, 0f), Direction.Down).PointClosestTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(100f, 100f, 0f), Direction.Down).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(100f, -100f, 0f), Direction.Up).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			plane.PointClosestToOrigin,
			new Ray(new Location(0f, 2f, 0f), Direction.Right).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			plane.PointClosestToOrigin,
			new Ray(new Location(0f, 0f, 0f), Direction.Right).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(100f, 100f, 0f), Direction.Up).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Ray(new Location(100f, -100f, 0f), Direction.Down).ClosestPointOn(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineRelationshipWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			new Ray(new Location(100f, 100f, 0f), Direction.Down).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			new Ray(new Location(100f, -100f, 0f), Direction.Up).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			new Ray(new Location(0f, 2f, 0f), Direction.Right).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			new Ray(new Location(0f, 0f, 0f), Direction.Right).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			new Ray(new Location(100f, 100f, 0f), Direction.Up).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			new Ray(new Location(100f, -100f, 0f), Direction.Down).RelationshipTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyProjectOnToPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various projections from behind the plane
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), Direction.Left).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), Direction.Right).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), Direction.Left).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), Direction.Right).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f)).FastProjectedOnTo(plane)
		);

		// Various projections from in front the plane
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), Direction.Left).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), Direction.Right).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), Direction.Left).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), Direction.Right).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f)).FastProjectedOnTo(plane)
		);

		// Projections from perpendicular directions
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 2f, 0f), Direction.Up).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 2f, 0f), Direction.Down).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 0f, 0f), Direction.Up).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 0f, 0f), Direction.Down).ProjectedOnTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various parallelizations from behind the plane
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), Direction.Left).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), Direction.Right).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), Direction.Left).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), Direction.Right).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Left),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Right),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f)).FastParallelizedWith(plane)
		);

		// Various parallelizations from in front the plane
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), Direction.Left).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), Direction.Right).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), Direction.Left).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), Direction.Right).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Left),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Right),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f)).FastParallelizedWith(plane)
		);

		// Parallelizations from perpendicular directions
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 2f, 0f), Direction.Up).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 2f, 0f), Direction.Down).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 0f, 0f), Direction.Up).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 0f, 0f), Direction.Down).ParallelizedWith(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various orthogonalizations from behind the plane
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 0f, 0f), Direction.Left).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 0f, 0f), Direction.Right).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Up),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Up),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Down),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Down),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Up),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Up),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Down),
			new Ray(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Down),
			new Ray(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f)).FastOrthogonalizedAgainst(plane)
		);

		// Various orthogonalizations from in front the plane
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 2f, 0f), Direction.Left).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			null,
			new Ray(new Location(10f, 2f, 0f), Direction.Right).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Up),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Up),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Down),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Down),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Up),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Up),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Down),
			new Ray(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Down),
			new Ray(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f)).FastOrthogonalizedAgainst(plane)
		);

		// Orthogonalizations from perpendicular directions
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Up),
			new Ray(new Location(10f, 2f, 0f), Direction.Up).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Down),
			new Ray(new Location(10f, 2f, 0f), Direction.Down).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Up),
			new Ray(new Location(10f, 0f, 0f), Direction.Up).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Down),
			new Ray(new Location(10f, 0f, 0f), Direction.Down).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Up),
			new Ray(new Location(10f, 2f, 0f), Direction.Up).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Down),
			new Ray(new Location(10f, 2f, 0f), Direction.Down).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Up),
			new Ray(new Location(10f, 0f, 0f), Direction.Up).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Down),
			new Ray(new Location(10f, 0f, 0f), Direction.Down).FastOrthogonalizedAgainst(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyBeSplitByPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		void AssertSplit(BoundedRay? expectedToPlane, Ray? expectedFromPlane, Ray ray) {
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
			new Ray(new Location(0f, 2f, 0f), Direction.Right)
		);
		AssertSplit(
			null,
			null,
			new Ray(new Location(0f, 2f, 0f), Direction.Left)
		);
		AssertSplit(
			null,
			null,
			new Ray(new Location(0f, 0f, 0f), Direction.Right)
		);
		AssertSplit(
			null,
			null,
			new Ray(new Location(0f, 0f, 0f), Direction.Left)
		);
		AssertSplit(
			null,
			null,
			new Ray(new Location(0f, 1f, 0f), Direction.Right)
		);
		AssertSplit(
			null,
			null,
			new Ray(new Location(0f, 1f, 0f), Direction.Left)
		);

		AssertSplit(
			new BoundedRay(new Location(100f, 2f, 0f), new Location(100f, 1f, 0f)),
			new Ray(new Location(100f, 1f, 0f), Direction.Down),
			new Ray(new Location(100f, 2f, 0f), Direction.Down)
		);
		AssertSplit(
			new BoundedRay(new Location(100f, 0f, 0f), new Location(100f, 1f, 0f)),
			new Ray(new Location(100f, 1f, 0f), Direction.Up),
			new Ray(new Location(100f, 0f, 0f), Direction.Up)
		);
		AssertSplit(
			null,
			null,
			new Ray(new Location(100f, 2f, 0f), Direction.Up)
		);
		AssertSplit(
			null,
			null,
			new Ray(new Location(100f, 0f, 0f), Direction.Down)
		);
		AssertSplit(
			new BoundedRay(new Location(100f, 2f, 0f), new Location(101f, 1f, 0f)),
			new Ray(new Location(101f, 1f, 0f), new Direction(1f, -1f, 0f)),
			new Ray(new Location(100f, 2f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertSplit(
			new BoundedRay(new Location(100f, 0f, 0f), new Location(101f, 1f, 0f)),
			new Ray(new Location(101f, 1f, 0f), new Direction(1f, 1f, 0f)),
			new Ray(new Location(100f, 0f, 0f), new Direction(1f, 1f, 0f))
		);

		// Some older tests from previous iteration
		Assert.AreEqual(
			new Pair<BoundedRay, Ray>(new BoundedRay(new Location(100f, 100f, 0f), new Location(100f, 1f, 0f)), new Ray(new Location(100f, 1f, 0f), Direction.Down)),
			new Ray(new Location(100f, 100f, 0f), Direction.Down).SplitBy(plane)
		);
		Assert.AreEqual(
			new Pair<BoundedRay, Ray>(new BoundedRay(new Location(100f, -100f, 0f), new Location(100f, 1f, 0f)), new Ray(new Location(100f, 1f, 0f), Direction.Up)),
			new Ray(new Location(100f, -100f, 0f), Direction.Up).SplitBy(plane)
		);
		Assert.Null(
			new Ray(new Location(0f, 2f, 0f), Direction.Right).SplitBy(plane)
		);
		Assert.Null(
			new Ray(new Location(0f, 0f, 0f), Direction.Right).SplitBy(plane)
		);
		Assert.Null(
			new Ray(new Location(100f, 100f, 0f), Direction.Up).SplitBy(plane)
		);
		Assert.Null(
			new Ray(new Location(100f, -100f, 0f), Direction.Down).SplitBy(plane)
		);

		// Fast
		Assert.AreEqual(
			new Pair<BoundedRay, Ray>(new BoundedRay(new Location(100f, 100f, 0f), new Location(100f, 1f, 0f)), new Ray(new Location(100f, 1f, 0f), Direction.Down)),
			new Ray(new Location(100f, 100f, 0f), Direction.Down).FastSplitBy(plane)
		);
		Assert.AreEqual(
			new Pair<BoundedRay, Ray>(new BoundedRay(new Location(100f, -100f, 0f), new Location(100f, 1f, 0f)), new Ray(new Location(100f, 1f, 0f), Direction.Up)),
			new Ray(new Location(100f, -100f, 0f), Direction.Up).FastSplitBy(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = new Ray((0f, 10f, 0f), Direction.Forward);
		var max = new Ray((0f, 20f, 0f), Direction.Right);

		AssertToleranceEquals(
			new Ray((0f, 15f, 0f), (-1f, 0f, 1f)),
			new Ray((0f, 15f, 0f), (-1f, 0f, 1f)).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray((0f, 20f, 0f), (-1f, 0f, 0f)),
			new Ray((0f, 25f, 0f), (-1f, 0f, -1f)).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Ray((0f, 10f, 0f), (0f, 0f, 1f)),
			new Ray((0f, 05f, 0f), (1f, 0f, 1f)).Clamp(min, max),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceAtPoints() {
		Assert.AreEqual(0f, TestRay.UnboundedDistanceAtPointClosestTo((1f, 2f, -3f)), TestTolerance);
		Assert.AreEqual(10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f), TestTolerance);
		Assert.AreEqual(-10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f), TestTolerance);

		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo((1f, 2f, -3f)), TestTolerance);
		Assert.AreEqual(10f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f), TestTolerance);
		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f), TestTolerance);


		Assert.AreEqual(0f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);
		Assert.AreEqual(10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);
		Assert.AreEqual(-10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);

		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);
		Assert.AreEqual(10f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);
		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f + TestRay.Direction.AnyOrthogonal() * 10f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineColinearityWithOtherLineLikes() {
		void AssertPair(bool expectation, Ray ray, Ray other, float? lineThickness, Angle? tolerance) {
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

		AssertPair(true, TestRay, TestRay, null, null);
		AssertPair(false, TestRay.MovedBy(TestRay.Direction.AnyOrthogonal() * 1f), TestRay, 0.45f, null);
		AssertPair(true, TestRay.MovedBy(TestRay.Direction.AnyOrthogonal() * 1f), TestRay, 0.55f, null);
		AssertPair(false, TestRay.RotatedBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)), TestRay, null, 0.9f);
		AssertPair(true, TestRay.RotatedBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)), TestRay, null, 1.1f);
		AssertPair(false, TestRay.MovedBy(TestRay.Direction.AnyOrthogonal() * 1f).RotatedBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)), TestRay, 0.45f, 0.9f);
		AssertPair(true, TestRay.MovedBy(TestRay.Direction.AnyOrthogonal() * 1f).RotatedBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)), TestRay, 0.55f, 1.1f);
		AssertPair(false, TestRay.RotatedBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)).MovedBy(TestRay.Direction.AnyOrthogonal() * 1f), TestRay, 0.45f, 0.9f);
		AssertPair(true, TestRay.RotatedBy((TestRay.Direction >> TestRay.Direction.AnyOrthogonal()).WithAngle(1f)).MovedBy(TestRay.Direction.AnyOrthogonal() * 1f), TestRay, 0.55f, 1.1f);
	}

	[Test]
	public void ShouldCorrectlyDetermineParallelismWithOtherElements() {
		void AssertCombination(bool expectation, Ray ray, Direction dir, Angle? tolerance) {
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

		AssertCombination(true, new Ray(Location.Origin, Direction.Up), Direction.Up, null);
		AssertCombination(false, new Ray(Location.Origin, Direction.Up), Direction.Left, null);
		AssertCombination(false, new Ray(Location.Origin, Direction.Up), (1f, 1f, 0f), 44f);
		AssertCombination(true, new Ray(Location.Origin, Direction.Up), (1f, 1f, 0f), 46f);

		Assert.AreEqual(false, TestRay.IsApproximatelyParallelTo(Direction.None));
		Assert.AreEqual(false, TestRay.IsApproximatelyParallelTo(new BoundedRay(Location.Origin, Location.Origin)));
		Assert.AreEqual(false, TestRay.IsParallelTo(Direction.None));
		Assert.AreEqual(false, TestRay.IsParallelTo(new BoundedRay(Location.Origin, Location.Origin)));
	}

	[Test]
	public void ShouldCorrectlyDetermineOrthogonalityWithOtherElements() {
		void AssertCombination(bool expectation, Ray ray, Direction dir, Angle? tolerance) {
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

		AssertCombination(true, new Ray(Location.Origin, Direction.Up), Direction.Left, null);
		AssertCombination(false, new Ray(Location.Origin, Direction.Up), Direction.Up, null);
		AssertCombination(false, new Ray(Location.Origin, Direction.Up), (1f, 1f, 0f), 44f);
		AssertCombination(true, new Ray(Location.Origin, Direction.Up), (1f, 1f, 0f), 46f);

		Assert.AreEqual(false, TestRay.IsApproximatelyOrthogonalTo(Direction.None));
		Assert.AreEqual(false, TestRay.IsApproximatelyOrthogonalTo(new BoundedRay(Location.Origin, Location.Origin)));
		Assert.AreEqual(false, TestRay.IsOrthogonalTo(Direction.None));
		Assert.AreEqual(false, TestRay.IsOrthogonalTo(new BoundedRay(Location.Origin, Location.Origin)));
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithDirectionsAndLineLikes() {
		void AssertAgainstLeft(Ray? expectation, Ray input) {
			Assert.AreEqual(expectation, input.ParallelizedWith(Direction.Left));
			Assert.AreEqual(expectation, input.ParallelizedWith(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.ParallelizedWith(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.ParallelizedWith(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}
		void AssertFastAgainstLeft(Ray expectation, Ray input) {
			Assert.AreEqual(expectation, input.FastParallelizedWith(Direction.Left));
			Assert.AreEqual(expectation, input.FastParallelizedWith(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastParallelizedWith(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastParallelizedWith(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}

		// Various parallelizations from behind the plane
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Left),
			new Ray(new Location(0f, 0f, 0f), Direction.Left)
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Right),
			new Ray(new Location(0f, 0f, 0f), Direction.Right)
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Left),
			new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Right),
			new Ray(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Left),
			new Ray(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Right),
			new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Left),
			new Ray(new Location(0f, 0f, 0f), Direction.Left)
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Right),
			new Ray(new Location(0f, 0f, 0f), Direction.Right)
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Left),
			new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Right),
			new Ray(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Left),
			new Ray(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Right),
			new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f))
		);

		// Various parallelizations from in front the dir
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Left),
			new Ray(new Location(0f, 2f, 0f), Direction.Left)
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Right),
			new Ray(new Location(0f, 2f, 0f), Direction.Right)
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Left),
			new Ray(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Right),
			new Ray(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Left),
			new Ray(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Right),
			new Ray(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Left),
			new Ray(new Location(0f, 2f, 0f), Direction.Left)
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Right),
			new Ray(new Location(0f, 2f, 0f), Direction.Right)
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Left),
			new Ray(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Right),
			new Ray(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Left),
			new Ray(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Right),
			new Ray(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))
		);

		// Parallelizations from perpendicular directions
		AssertAgainstLeft(
			null,
			new Ray(new Location(0f, 2f, 0f), Direction.Up)
		);
		AssertAgainstLeft(
			null,
			new Ray(new Location(0f, 2f, 0f), Direction.Down)
		);
		AssertAgainstLeft(
			null,
			new Ray(new Location(0f, 0f, 0f), Direction.Up)
		);
		AssertAgainstLeft(
			null,
			new Ray(new Location(0f, 0f, 0f), Direction.Down)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstDirectionsAndLineLikes() {
		void AssertAgainstLeft(Ray? expectation, Ray input) {
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(Direction.Left));
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}
		void AssertFastAgainstLeft(Ray expectation, Ray input) {
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(Direction.Left));
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}

		// Various orthogonalizations from behind the plane
		AssertAgainstLeft(
			null,
			new Ray(new Location(0f, 0f, 0f), Direction.Left)
		);
		AssertAgainstLeft(
			null,
			new Ray(new Location(0f, 0f, 0f), Direction.Right)
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Up),
			new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Up),
			new Ray(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Down),
			new Ray(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Down),
			new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Up),
			new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Up),
			new Ray(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Down),
			new Ray(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Down),
			new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f))
		);

		// Various orthogonalizations from in front the plane
		AssertAgainstLeft(
		null,
			new Ray(new Location(0f, 2f, 0f), Direction.Left)
		);
		AssertAgainstLeft(
		null,
			new Ray(new Location(0f, 2f, 0f), Direction.Right)
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Up),
			new Ray(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Up),
			new Ray(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Down),
			new Ray(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Down),
			new Ray(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Up),
			new Ray(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Up),
			new Ray(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Down),
			new Ray(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Down),
			new Ray(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))
		);

		// Orthogonalizations from perpendicular directions
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Up),
			new Ray(new Location(0f, 2f, 0f), Direction.Up)
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 2f, 0f), Direction.Down),
			new Ray(new Location(0f, 2f, 0f), Direction.Down)
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Up),
			new Ray(new Location(0f, 0f, 0f), Direction.Up)
		);
		AssertAgainstLeft(
			new Ray(new Location(0f, 0f, 0f), Direction.Down),
			new Ray(new Location(0f, 0f, 0f), Direction.Down)
		);
	}
}