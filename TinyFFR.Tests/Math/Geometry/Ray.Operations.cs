// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;
using System.Reflection;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class RayTest {
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
			AssertToleranceEquals(expectation, input.RotatedAroundPoint(rotation, pivotPoint), TestTolerance);
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), input * (pivotPoint, rotation));
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), input * (rotation, pivotPoint));
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), (pivotPoint, rotation) * input);
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), (rotation, pivotPoint) * input);
		}

		AssertCombination(new Ray((0f, 0f, 10f), Direction.Backward), new Ray(Location.Origin, Direction.Forward), (0f, 0f, 5f), Direction.Down % 180f);
		AssertCombination(new Ray(Location.Origin, Direction.Forward), new Ray(Location.Origin, Direction.Forward), (0f, 0f, 5f), Direction.Forward % 180f);
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


		Assert.AreEqual(0f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);
		Assert.AreEqual(10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);
		Assert.AreEqual(-10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);

		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);
		Assert.AreEqual(10f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);
		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineColinearityWithOtherLineLikes() {
		void AssertPair(bool expectation, Ray ray, Ray other, float? lineThickness, Angle? tolerance) {
			var flippedRay = ray.Flipped;
			var otherAsLine = other.ToLine();
			var otherAsFlippedLine = new Line(other.StartPoint, other.Direction.Flipped);
			var otherAsBoundedRay = other.ToBoundedRay(100f);

			// Line
			Assert.AreEqual(expectation, ray.IsColinearWith(otherAsLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, otherAsLine.IsColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			Assert.AreEqual(expectation, ray.IsColinearWith(otherAsFlippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, otherAsLine.IsColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			Assert.AreEqual(expectation, flippedRay.IsColinearWith(otherAsLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, otherAsFlippedLine.IsColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			Assert.AreEqual(expectation, flippedRay.IsColinearWith(otherAsFlippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, otherAsFlippedLine.IsColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			// Ray
			Assert.AreEqual(expectation, ray.IsColinearWith(other, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, other.IsColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			Assert.AreEqual(expectation, ray.IsColinearWith(other.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, other.IsColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			Assert.AreEqual(expectation, flippedRay.IsColinearWith(other, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, other.Flipped.IsColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			Assert.AreEqual(expectation, flippedRay.IsColinearWith(other.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, other.Flipped.IsColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			// BoundedRay
			Assert.AreEqual(expectation, ray.IsColinearWith(otherAsBoundedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.IsColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			Assert.AreEqual(expectation, ray.IsColinearWith(otherAsBoundedRay.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.IsColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			Assert.AreEqual(expectation, flippedRay.IsColinearWith(otherAsBoundedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.Flipped.IsColinearWith(ray, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));

			Assert.AreEqual(expectation, flippedRay.IsColinearWith(otherAsBoundedRay.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.Flipped.IsColinearWith(flippedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultAngularToleranceDegrees));
		}

		AssertPair(true, TestRay, TestRay, null, null);
		AssertPair(false, TestRay.MovedBy(TestRay.Direction.AnyPerpendicular() * 1f), TestRay, 0.45f, null);
		AssertPair(true, TestRay.MovedBy(TestRay.Direction.AnyPerpendicular() * 1f), TestRay, 0.55f, null);
		AssertPair(false, TestRay.RotatedBy((TestRay.Direction >> TestRay.Direction.AnyPerpendicular()).WithAngle(1f)), TestRay, null, 0.9f);
		AssertPair(true, TestRay.RotatedBy((TestRay.Direction >> TestRay.Direction.AnyPerpendicular()).WithAngle(1f)), TestRay, null, 1.1f);
		AssertPair(false, TestRay.MovedBy(TestRay.Direction.AnyPerpendicular() * 1f).RotatedBy((TestRay.Direction >> TestRay.Direction.AnyPerpendicular()).WithAngle(1f)), TestRay, 0.45f, 0.9f);
		AssertPair(true, TestRay.MovedBy(TestRay.Direction.AnyPerpendicular() * 1f).RotatedBy((TestRay.Direction >> TestRay.Direction.AnyPerpendicular()).WithAngle(1f)), TestRay, 0.55f, 1.1f);
		AssertPair(false, TestRay.RotatedBy((TestRay.Direction >> TestRay.Direction.AnyPerpendicular()).WithAngle(1f)).MovedBy(TestRay.Direction.AnyPerpendicular() * 1f), TestRay, 0.45f, 0.9f);
		AssertPair(true, TestRay.RotatedBy((TestRay.Direction >> TestRay.Direction.AnyPerpendicular()).WithAngle(1f)).MovedBy(TestRay.Direction.AnyPerpendicular() * 1f), TestRay, 0.55f, 1.1f);
	}

	[Test]
	public void ShouldCorrectlyDetermineParallelismWithOtherElements() {
		void AssertCombination(bool expectation, Ray ray, Direction dir, Angle? tolerance) {
			var flippedRay = ray.Flipped;
			var plane = new Plane(dir.AnyPerpendicular(), Location.Origin);
			var dirLine = new Line(Location.Origin, dir);
			var dirRay = new Ray(Location.Origin, dir);
			var dirRayBounded = BoundedRay.FromStartPointAndVect(Location.Origin, dir * 10f);

			if (tolerance == null) {
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
				Assert.AreEqual(expectation, ray.IsParallelTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsParallelTo(dir.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dir.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsParallelTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsParallelTo(plane.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(plane.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsParallelTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));

				Assert.AreEqual(expectation, ray.IsParallelTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsParallelTo(dirRay.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dirRay.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsParallelTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsParallelTo(dirRayBounded.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsParallelTo(dirRayBounded.Flipped, tolerance.Value));
			}
		}

		AssertCombination(true, new Ray(Location.Origin, Direction.Up), Direction.Up, null);
		AssertCombination(false, new Ray(Location.Origin, Direction.Up), Direction.Left, null);
		AssertCombination(false, new Ray(Location.Origin, Direction.Up), (1f, 1f, 0f), 44f);
		AssertCombination(true, new Ray(Location.Origin, Direction.Up), (1f, 1f, 0f), 46f);

		Assert.AreEqual(false, TestRay.IsParallelTo(Direction.None));
		Assert.AreEqual(false, TestRay.IsParallelTo(new BoundedRay(Location.Origin, Location.Origin)));
	}

	[Test]
	public void ShouldCorrectlyDetermineOrthogonalityWithOtherElements() {
		void AssertCombination(bool expectation, Ray ray, Direction dir, Angle? tolerance) {
			var flippedRay = ray.Flipped;
			var plane = new Plane(dir.AnyPerpendicular(), Location.Origin);
			var dirLine = new Line(Location.Origin, dir);
			var dirRay = new Ray(Location.Origin, dir);
			var dirRayBounded = BoundedRay.FromStartPointAndVect(Location.Origin, dir * 10f);

			if (tolerance == null) {
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
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dir.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dir.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsOrthogonalTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(plane.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(plane.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));

				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dirRay.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dirRay.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, ray.IsOrthogonalTo(dirRayBounded.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedRay.IsOrthogonalTo(dirRayBounded.Flipped, tolerance.Value));
			}
		}

		AssertCombination(true, new Ray(Location.Origin, Direction.Up), Direction.Left, null);
		AssertCombination(false, new Ray(Location.Origin, Direction.Up), Direction.Up, null);
		AssertCombination(false, new Ray(Location.Origin, Direction.Up), (1f, 1f, 0f), 44f);
		AssertCombination(true, new Ray(Location.Origin, Direction.Up), (1f, 1f, 0f), 46f);

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
					Assert.AreEqual(null, ray.ParallelizedWith(targetDir));
					Assert.AreEqual(null, ray.ParallelizedAroundStartWith(targetDir));
					Assert.AreEqual(null, ray.ParallelizedAroundMiddleWith(targetDir));
					Assert.AreEqual(null, ray.ParallelizedAroundEndWith(targetDir));
					Assert.AreEqual(null, ray.ParallelizedAroundPivotDistanceWith(targetDir, TestPivotDistance));
					Assert.AreEqual(null, ray.OrthogonalizedAgainst(targetDir));
					Assert.AreEqual(null, ray.OrthogonalizedAroundStartAgainst(targetDir));
					Assert.AreEqual(null, ray.OrthogonalizedAroundMiddleAgainst(targetDir));
					Assert.AreEqual(null, ray.OrthogonalizedAroundEndAgainst(targetDir));
					Assert.AreEqual(null, ray.OrthogonalizedAroundPivotDistanceAgainst(targetDir, TestPivotDistance));

					Assert.AreEqual(null, ray.ParallelizedWith(targetRayBounded));
					Assert.AreEqual(null, ray.ParallelizedAroundStartWith(targetRayBounded));
					Assert.AreEqual(null, ray.ParallelizedAroundMiddleWith(targetRayBounded));
					Assert.AreEqual(null, ray.ParallelizedAroundEndWith(targetRayBounded));
					Assert.AreEqual(null, ray.ParallelizedAroundPivotDistanceWith(targetRayBounded, TestPivotDistance));
					Assert.AreEqual(null, ray.OrthogonalizedAgainst(targetRayBounded));
					Assert.AreEqual(null, ray.OrthogonalizedAroundStartAgainst(targetRayBounded));
					Assert.AreEqual(null, ray.OrthogonalizedAroundMiddleAgainst(targetRayBounded));
					Assert.AreEqual(null, ray.OrthogonalizedAroundEndAgainst(targetRayBounded));
					Assert.AreEqual(null, ray.OrthogonalizedAroundPivotDistanceAgainst(targetRayBounded, TestPivotDistance));
					continue;
				}

				var targetLine = new Line(Location.Origin, targetDir);
				var targetRay = new Ray(Location.Origin, targetDir);
				var targetPlane = new Plane(targetDir.AnyPerpendicular(), 0f);
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
							if (result != null) {
								result = ((BoundedRay?) typeof(BoundedRay).GetMethod("Fast" + nonFastMethodName, args.Select(o => o.GetType()).ToArray())!.Invoke(ray, args))!.Value;
								assertionAction(result);
							}
						}
						catch {
							Console.WriteLine($"Failure details:");
							Console.WriteLine("\tInput: " + ray.ToStringDescriptive());
							Console.WriteLine("\tFunc: " + nonFastMethodName);
							Console.WriteLine("\tTarget: " + args[0]);
							Console.WriteLine("\tResult: " + (result?.ToStringDescriptive() ?? "<null>"));
							throw;
						}
					}

					foreach (var target in allTargets!) {
						if (includeParallelizations) {
							if (includeStandardFuncs) TestMethod(nameof(BoundedRay.ParallelizedWith), target);
							if (includeStartFuncs) TestMethod(nameof(BoundedRay.ParallelizedAroundStartWith), target);
							if (includeMiddleFuncs) TestMethod(nameof(BoundedRay.ParallelizedAroundMiddleWith), target);
							if (includeEndFuncs) TestMethod(nameof(BoundedRay.ParallelizedAroundEndWith), target);
							if (includePivotFuncs) TestMethod(nameof(BoundedRay.ParallelizedAroundPivotDistanceWith), target, TestPivotDistance);
						}
						if (includeOrthogonalizations) {
							if (includeStandardFuncs) TestMethod(nameof(BoundedRay.OrthogonalizedAgainst), target);
							if (includeStartFuncs) TestMethod(nameof(BoundedRay.OrthogonalizedAroundStartAgainst), target);
							if (includeMiddleFuncs) TestMethod(nameof(BoundedRay.OrthogonalizedAroundMiddleAgainst), target);
							if (includeEndFuncs) TestMethod(nameof(BoundedRay.OrthogonalizedAroundEndAgainst), target);
							if (includePivotFuncs) TestMethod(nameof(BoundedRay.OrthogonalizedAroundPivotDistanceAgainst), target, TestPivotDistance);
						}
					}
				}
				
				if (rayDir == Direction.None) {
					AssertAllTrue(
						result => result == null
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