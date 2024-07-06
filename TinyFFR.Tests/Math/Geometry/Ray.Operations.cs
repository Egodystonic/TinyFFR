// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

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
	}

	[Test]
	public void ShouldCorrectlyIntersectWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Ray(new Location(100f, 1f, 0f), Direction.Down),
			new Ray(new Location(100f, 100f, 0f), Direction.Down).IntersectionWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(100f, 1f, 0f), Direction.Up),
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

		// Projections from perpendicular directions
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Up),
			new Ray(new Location(10f, 2f, 0f), Direction.Up).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Down),
			new Ray(new Location(10f, 2f, 0f), Direction.Down).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Up),
			new Ray(new Location(10f, 0f, 0f), Direction.Up).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 1f, 0f), Direction.Down),
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

		// Parallelizations from perpendicular directions
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Up),
			new Ray(new Location(10f, 2f, 0f), Direction.Up).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Down),
			new Ray(new Location(10f, 2f, 0f), Direction.Down).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Up),
			new Ray(new Location(10f, 0f, 0f), Direction.Up).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Down),
			new Ray(new Location(10f, 0f, 0f), Direction.Down).ParallelizedWith(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various orthogonalizations from behind the plane
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Up),
			new Ray(new Location(10f, 0f, 0f), Direction.Left).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 0f, 0f), Direction.Up),
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

		// Various orthogonalizations from in front the plane
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Up),
			new Ray(new Location(10f, 2f, 0f), Direction.Left).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(10f, 2f, 0f), Direction.Up),
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
	}

	[Test]
	public void ShouldCorrectlyBeSplitByPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		void AssertSplit(BoundedRay? expectedToPlane, Ray? expectedFromPlane, Ray ray) {
			AssertToleranceEquals(expectedFromPlane, ray.IntersectionWith(plane), TestTolerance);
			var trySplitResult = ray.TrySplit(plane, out var actualToPlane, out var actualFromPlane);
			if (expectedToPlane == null) {
				Assert.AreEqual(false, trySplitResult);
				Assert.AreEqual(false, ray.IsIntersectedBy(plane));
			}
			else {
				Assert.AreEqual(true, trySplitResult);
				Assert.AreEqual(true, ray.IsIntersectedBy(plane));
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
	}
}