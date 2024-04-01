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
		AssertToleranceEquals(new BoundedLine(TestRay.StartPoint, TestRay.StartPoint + TestRay.Direction * 10f), TestRay.ToBoundedLine(10f), TestTolerance);
		AssertToleranceEquals(TestRay.Direction, TestRay.ToBoundedLine(10f).Direction, TestTolerance);
		AssertToleranceEquals(new BoundedLine(TestRay.StartPoint, TestRay.StartPoint + TestRay.Direction * -10f), TestRay.ToBoundedLine(-10f), TestTolerance);
		AssertToleranceEquals(-TestRay.Direction, TestRay.ToBoundedLine(-10f).Direction, TestTolerance);
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
		Assert.AreEqual(
			new Ray(Location.Origin, Direction.Right),
			new Ray(Location.Origin, Direction.Left) * (Direction.Up % 180f)
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
			new Ray(new Location(-100f, 0f, 0f), Direction.Left).ClosestPointTo(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(-100f, 0f, 0f),
			new Ray(new Location(0f, 0f, 0f), Direction.Right).ClosestPointTo(new Location(-100f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			new Ray(new Location(100f, 0f, 0f), Direction.Left).ClosestPointTo(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Ray(new Location(0f, 0f, 0f), Direction.Left).ClosestPointTo(new Location(-100f, 1f, 0f))
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToOrigin() {
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Ray(new Location(100f, 0f, 0f), Direction.Right).ClosestPointToOrigin()
		);
		Assert.AreEqual(
			new Location(0f, -1f, 0f),
			new Ray(new Location(100f, -1f, 0f), Direction.Right).ClosestPointToOrigin()
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			new Ray(new Location(100f, 0f, 0f), Direction.Left).ClosestPointToOrigin()
		);
		Assert.AreEqual(
			new Location(100f, -1f, 0f),
			new Ray(new Location(100f, -1f, 0f), Direction.Left).ClosestPointToOrigin()
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
		void AssertPair<TLine>(Location expectedResult, Ray ray, TLine other) where TLine : ILine {
			AssertToleranceEquals(expectedResult, ray.ClosestPointTo(other), TestTolerance);
			//Assert.AreEqual(ray.ClosestPointTo(other), other.ClosestPointOn(ray));
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

		// BoundedLine
		AssertPair(
			new Location(0f, 20f, 0),
			new Ray(Location.Origin, Direction.Up),
			new BoundedLine(new Location(0f, 10f, -10f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 20f, 0),
			new Ray(Location.Origin, Direction.Up),
			new BoundedLine(new Location(0f, 30f, 10f), new Location(0f, 10, -10f))
		);
		// TODO continue here
		
	}

	[Test]
	public void ShouldCorrectlyCalculateDistanceFromLines() { // TODO add the right answers in later, this is just a regression test
		Assert.Fail("Todo");
	}

	[Test]
	public void ShouldCorrectlyImplementLocationAtDistanceFunctions() {
		ILine line = new Line(new Location(0f, 1f, 0f), Direction.Right);

		Assert.AreEqual(true, line.DistanceIsWithinLineBounds(-30000f));
		Assert.AreEqual(true, line.DistanceIsWithinLineBounds(30000f));
		Assert.AreEqual(true, line.DistanceIsWithinLineBounds(0f));
		
		Assert.AreEqual(-30000f, line.BindDistance(-30000f));
		Assert.AreEqual(30000f, line.BindDistance(30000f));
		Assert.AreEqual(0f, line.BindDistance(0f));

		Assert.AreEqual(new Location(0f, 1f, 0f), ((Line) line).LocationAtDistance(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), ((Line) line).LocationAtDistance(3f));
		Assert.AreEqual(new Location(3f, 1f, 0f), ((Line) line).LocationAtDistance(-3f));

		Assert.AreEqual(new Location(0f, 1f, 0f), line.BoundedLocationAtDistance(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), line.BoundedLocationAtDistance(3f));
		Assert.AreEqual(new Location(3f, 1f, 0f), line.BoundedLocationAtDistance(-3f));

		Assert.AreEqual(new Location(0f, 1f, 0f), line.UnboundedLocationAtDistance(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), line.UnboundedLocationAtDistance(3f));
		Assert.AreEqual(new Location(3f, 1f, 0f), line.UnboundedLocationAtDistance(-3f));

		Assert.AreEqual(new Location(0f, 1f, 0f), line.LocationAtDistanceOrNull(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), line.LocationAtDistanceOrNull(3f));
		Assert.AreEqual(new Location(3f, 1f, 0f), line.LocationAtDistanceOrNull(-3f));
	}

	[Test]
	public void ShouldCorrectlyDetectLineIntersections() {
		// Line
		Assert.Null(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);

		// Ray
		Assert.Null(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);

		// BoundedLine
		Assert.Null(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedLine(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedLine(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedLine(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedLine(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedLine(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedLine(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				new BoundedLine(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 1.01f
			)
		);
	}

	[Test]
	public void ShouldCorrectlyReflectOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Ray(new Location(100f, 1f, 0f), Direction.Up),
			new Line(new Location(100f, 100f, 0f), Direction.Down).ReflectedBy(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(100f, 1f, 0f), Direction.Down),
			new Line(new Location(100f, -100f, 0f), Direction.Up).ReflectedBy(plane)
		);
		Assert.Null(
			new Line(new Location(0f, 2f, 0f), Direction.Right).ReflectedBy(plane)
		);
		Assert.Null(
			new Line(new Location(0f, 0f, 0f), Direction.Right).ReflectedBy(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyIntersectWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(100f, 100f, 0f), Direction.Down).IntersectionWith(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(100f, -100f, 0f), Direction.Up).IntersectionWith(plane)
		);
		Assert.Null(
			new Line(new Location(0f, 2f, 0f), Direction.Right).IntersectionWith(plane)
		);
		Assert.Null(
			new Line(new Location(0f, 0f, 0f), Direction.Right).IntersectionWith(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineSignedDistanceFromPlane() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		void AssertDistance(float expectedSignedDistance, Line line) {
			Assert.AreEqual(expectedSignedDistance, line.SignedDistanceFrom(plane));
			Assert.AreEqual(MathF.Abs(expectedSignedDistance), line.DistanceFrom(plane));
		}

		AssertDistance(
			0f,
			new Line(new Location(100f, 100f, 0f), Direction.Down)
		);
		AssertDistance(
			0f,
			new Line(new Location(100f, -100f, 0f), Direction.Up)
		);
		AssertDistance(
			1f,
			new Line(new Location(0f, 2f, 0f), Direction.Right)
		);
		AssertDistance(
			-1f,
			new Line(new Location(0f, 0f, 0f), Direction.Right)
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(100f, 100f, 0f), Direction.Down).ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(100f, -100f, 0f), Direction.Up).ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(0f, 2f, 0f),
			new Line(new Location(0f, 2f, 0f), Direction.Right).ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), Direction.Right).ClosestPointTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(100f, 100f, 0f), Direction.Down).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(100f, -100f, 0f), Direction.Up).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			plane.ClosestPointToOrigin,
			new Line(new Location(0f, 2f, 0f), Direction.Right).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			plane.ClosestPointToOrigin,
			new Line(new Location(0f, 0f, 0f), Direction.Right).ClosestPointOn(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineRelationshipWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			new Line(new Location(100f, 100f, 0f), Direction.Down).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			new Line(new Location(100f, -100f, 0f), Direction.Up).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			new Line(new Location(0f, 2f, 0f), Direction.Right).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			new Line(new Location(0f, 0f, 0f), Direction.Right).RelationshipTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyProjectOnToPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various projections from behind the plane
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), Direction.Left).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), Direction.Right).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f)).ProjectedOnTo(plane)
		);
	
		// Various projections from in front the plane
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), Direction.Left).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), Direction.Right).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f)).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f)).ProjectedOnTo(plane)
		);

		// Projections from perpendicular directions
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Up).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), Direction.Down).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Up).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), Direction.Down).ProjectedOnTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various parallelizations from behind the plane
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), Direction.Left).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), Direction.Right).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f)).ParallelizedWith(plane)
		);

		// Various parallelizations from in front the plane
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), Direction.Left).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), Direction.Right).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f)).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f)).ParallelizedWith(plane)
		);

		// Parallelizations from perpendicular directions
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Up).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), Direction.Down).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Up).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), Direction.Down).ParallelizedWith(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithPlanesUsingSpecifiedPivotPoint() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various parallelizations from behind the plane
		Assert.AreEqual(
			new Line(new Location(100f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), Direction.Left).ParallelizedWith(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(100f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), Direction.Right).ParallelizedWith(plane, -100f)
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).ParallelizedWith(plane, 100f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f), 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f)).ParallelizedWith(plane, -100f),
			TestTolerance
		);

		// Various parallelizations from in front the plane
		Assert.AreEqual(
			new Line(new Location(100f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), Direction.Left).ParallelizedWith(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(100f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), Direction.Right).ParallelizedWith(plane, -100f)
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f)).ParallelizedWith(plane, 100f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)).ParallelizedWith(plane, -100f),
			TestTolerance
		);

		// Parallelizations from perpendicular directions
		Assert.AreEqual(
			new Line(new Location(0f, 102f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Up).ParallelizedWith(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(0f, -98f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), Direction.Down).ParallelizedWith(plane, -100f)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 100f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Up).ParallelizedWith(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(0f, -100f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), Direction.Down).ParallelizedWith(plane, -100f)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various orthogonalizations from behind the plane
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Left).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Right).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f)).OrthogonalizedAgainst(plane)
		);

		// Various orthogonalizations from in front the plane
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Left).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Right).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f)).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f)).OrthogonalizedAgainst(plane)
		);

		// Orthogonalizations from perpendicular directions
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Up).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), Direction.Down).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Up).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), Direction.Down).OrthogonalizedAgainst(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstPlanesUsingSpecifiedPivotPoint() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various orthogonalizations from behind the plane
		Assert.AreEqual(
			new Line(new Location(100f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Left).OrthogonalizedAgainst(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(100f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Right).OrthogonalizedAgainst(plane, -100f)
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).OrthogonalizedAgainst(plane, 100f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f)).OrthogonalizedAgainst(plane, -100f),
			TestTolerance
		);

		// Various orthogonalizations from in front the plane
		Assert.AreEqual(
			new Line(new Location(100f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Left).OrthogonalizedAgainst(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(100f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Right).OrthogonalizedAgainst(plane, -100f)
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f)).OrthogonalizedAgainst(plane, 100f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)).OrthogonalizedAgainst(plane, -100f),
			TestTolerance
		);

		// Orthogonalizations from perpendicular directions
		Assert.AreEqual(
			new Line(new Location(0f, 102f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Up).OrthogonalizedAgainst(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(0f, -98f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), Direction.Down).OrthogonalizedAgainst(plane, -100f)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 100f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Up).OrthogonalizedAgainst(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(0f, -100f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), Direction.Down).OrthogonalizedAgainst(plane, -100f)
		);
	}

	[Test]
	public void ShouldCorrectlyBeSplitByPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		void AssertSplit(Ray? expectedWithLineDir, Ray? expectedOpposingLineDir, Line line) {
			Assert.AreEqual(expectedOpposingLineDir, line.SplitBy(plane));
			var trySplitResult = line.TrySplit(plane, out var actualWithLineDir, out var actualOpposingLineDir);
			if (expectedWithLineDir == null) Assert.AreEqual(false, trySplitResult);
			else {
				Assert.AreEqual(true, trySplitResult);
				Assert.AreEqual(expectedWithLineDir, actualWithLineDir);
				Assert.AreEqual(expectedOpposingLineDir, actualOpposingLineDir);
			}
		}

		AssertSplit(
			null,
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Right)
		);
		AssertSplit(
			null,
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Left)
		);
		AssertSplit(
			null,
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Right)
		);
		AssertSplit(
			null,
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Left)
		);
		AssertSplit(
			null,
			null,
			new Line(new Location(0f, 1f, 0f), Direction.Right)
		);
		AssertSplit(
			null,
			null,
			new Line(new Location(0f, 1f, 0f), Direction.Left)
		);

		AssertSplit(
			new Ray(new Location(100f, 1f, 0f), Direction.Up),
			new Ray(new Location(100f, 1f, 0f), Direction.Down),
			new Line(new Location(100f, 2f, 0f), Direction.Up)
		);
		AssertSplit(
			new Ray(new Location(100f, 1f, 0f), Direction.Down),
			new Ray(new Location(100f, 1f, 0f), Direction.Up),
			new Line(new Location(100f, 0f, 0f), Direction.Down)
		);
	}
}