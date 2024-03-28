// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class LineTest {
	[Test]
	public void ShouldCorrectlyConvertToRay() {
		AssertToleranceEquals(new Ray(new Location(1f, 2f, -3f) + TestLineDirection * 10f, TestLineDirection), TestLine.ToRay(10f, false), TestTolerance);
		AssertToleranceEquals(new Ray(new Location(1f, 2f, -3f) + TestLineDirection * -10f, TestLineDirection), TestLine.ToRay(-10f, false), TestTolerance);
		AssertToleranceEquals(new Ray(new Location(1f, 2f, -3f) + TestLineDirection * 10f, -TestLineDirection), TestLine.ToRay(10f, true), TestTolerance);
		AssertToleranceEquals(new Ray(new Location(1f, 2f, -3f) + TestLineDirection * -10f, -TestLineDirection), TestLine.ToRay(-10f, true), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyConvertToBoundedLine() {
		AssertToleranceEquals(new BoundedLine(new Location(1f, 2f, -3f) + TestLineDirection * 10f, new Location(1f, 2f, -3f) + TestLineDirection * -10f), TestLine.ToBoundedLine(10f, -10f), TestTolerance);
		AssertToleranceEquals(new BoundedLine(new Location(1f, 2f, -3f) + TestLineDirection * -10f, new Location(1f, 2f, -3f) + TestLineDirection * 10f), TestLine.ToBoundedLine(-10f, 10f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyRotate() {
		var rotation = 70f % Direction.Down;

		AssertToleranceEquals(
			new Line(TestLine.PointOnLine, TestLineDirection * rotation),
			TestLine * rotation,
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(TestLine.PointOnLine, TestLineDirection * rotation),
			rotation * TestLine,
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyMove() {
		var vect = new Vect(5f, -3f, 12f);

		AssertToleranceEquals(
			new Line(TestLine.PointOnLine + vect, TestLineDirection),
			TestLine + vect,
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(TestLine.PointOnLine + vect, TestLineDirection),
			vect + TestLine,
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToLocation() {
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Line(new Location(100f, 0f, 0f), Direction.Left).ClosestPointTo(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(-100f, 0f, 0f),
			new Line(new Location(100f, 0f, 0f), Direction.Left).ClosestPointTo(new Location(-100f, 1f, 0f))
		);
		AssertToleranceEquals(
			new Location(2f, 5f, 2f),
			new Line(new Location(0f, 3f, 0f), new Direction(1f, 1f, 1f)).ClosestPointTo(new Direction(1f, 1f, 1f).GetAnyPerpendicular() * 10f + new Location(2f, 5f, 2f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToOrigin() {
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Line(new Location(100f, 0f, 0f), Direction.Left).ClosestPointToOrigin()
		);
		Assert.AreEqual(
			new Location(0f, -1f, 0f),
			new Line(new Location(100f, -1f, 0f), Direction.Left).ClosestPointToOrigin()
		);
		AssertToleranceEquals(
			new Location(-1f, 2f, -1f),
			new Line(new Location(0f, 3f, 0f), new Direction(1f, 1f, 1f)).ClosestPointToOrigin(),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLocation() {
		Assert.AreEqual(
			1f,
			new Line(new Location(100f, 0f, 0f), Direction.Left).DistanceFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			1f,
			new Line(new Location(100f, 0f, 0f), Direction.Left).DistanceFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(100f, 0f, 0f), Direction.Left).DistanceFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			MathF.Sqrt(2f),
			new Line(new Location(100f, 0f, 0f), Direction.Left).DistanceFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineContainmentOfLocation() {
		Assert.AreEqual(
			false,
			new Line(new Location(100f, 0f, 0f), Direction.Left).Contains(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			true,
			new Line(new Location(100f, 0f, 0f), Direction.Left).Contains(new Location(0f, 1f, 0f), 1.1f)
		);
		Assert.AreEqual(
			false,
			new Line(new Location(100f, 0f, 0f), Direction.Left).Contains(new Location(0f, 1f, 0f), 0.9f)
		);
		Assert.AreEqual(
			true,
			new Line(new Location(100f, 0f, 0f), Direction.Left).Contains(new Location(0f, -1f, 0f), 1.1f)
		);
		Assert.AreEqual(
			false,
			new Line(new Location(100f, 0f, 0f), Direction.Left).Contains(new Location(0f, -1f, 0f), 0.9f)
		);
	}

	[Test]
	public void ShouldCorrectlyReturnClosestPointToOtherLine() {
		// Line
		AssertToleranceEquals(
			new Location(0f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)).ClosestPointTo(new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 1f))),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 0f, 0f),
			new Line(new Location(1f, 1f, 1f), new Direction(1f, 1f, 1f)).ClosestPointTo(new Line(new Location(-1f, -1f, 1f), new Direction(-1f, -1f, 1f))),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			new Line(new Location(1f, 2f, 1f), new Direction(1f, 1f, 1f)).ClosestPointTo(new Line(new Location(-1f, 0f, 1f), new Direction(-1f, -1f, 1f))),
			TestTolerance
		);

		// Ray
		AssertToleranceEquals(
			new Location(0f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)).ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 1f))),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-1f, -1f, -1f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)).ClosestPointTo(new Ray(new Location(-1f, -1f, -1f), new Direction(-1f, -1f, -1f))),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(1f, 1f, 0f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).ClosestPointTo(new Ray(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right).ClosestPointTo(new Ray(new Location(0f, 2f, 0f), Direction.Up)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right).ClosestPointTo(new Ray(new Location(100f, 2f, 0f), Direction.Up)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right).ClosestPointTo(new Ray(new Location(0f, 2f, 0f), Direction.Down)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right).ClosestPointTo(new Ray(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-1f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right).ClosestPointTo(new Ray(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right).ClosestPointTo(new Ray(new Location(100f, 2000f, 0f), Direction.Left)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right).ClosestPointTo(new Ray(new Location(100f, 2000f, 0f), Direction.Right)),
			TestTolerance
		);

		// BoundedLine
		AssertToleranceEquals(
			new Location(0f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)).ClosestPointTo(
				new BoundedLine(new Location(-1f, -1f, 1f), new Location(1f, 1f, -1f))
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right).ClosestPointTo(
				new BoundedLine(new Location(0f, 2f, 0f), new Location(0f, 4f, 0f))
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			new Line(new Location(1000f, 1f, 0f), Direction.Right).ClosestPointTo(
				new BoundedLine(new Location(0f, 4f, 0f), new Location(0f, 2f, 0f))
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-3f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), Direction.Left).ClosestPointTo(
				new BoundedLine(new Location(-3f, 1f, 0f), new Location(3f, 2f, 0f))
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-5f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right).ClosestPointTo(
				new BoundedLine(new Location(-10f, -4f, 0f), new Location(0f, 6f, 0f))
			),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyCalculateDistanceFromLines() { // TODO add the right answers in later, this is just a regression test
		// Line
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)).DistanceFrom(new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 1f))),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(1f, 1f, 1f), new Direction(1f, 1f, 1f)).DistanceFrom(new Line(new Location(-1f, -1f, 1f), new Direction(-1f, -1f, 1f))),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(1f, 2f, 1f), new Direction(1f, 1f, 1f)).DistanceFrom(new Line(new Location(-1f, 0f, 1f), new Direction(-1f, -1f, 1f))),
			TestTolerance
		);

		// Ray
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)).DistanceFrom(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 1f))),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)).DistanceFrom(new Ray(new Location(-1f, -1f, -1f), new Direction(-1f, -1f, -1f))),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).DistanceFrom(new Ray(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 1f, 0f), Direction.Right).DistanceFrom(new Ray(new Location(0f, 2f, 0f), Direction.Up)),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 1f, 0f), Direction.Right).DistanceFrom(new Ray(new Location(100f, 2f, 0f), Direction.Up)),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 1f, 0f), Direction.Right).DistanceFrom(new Ray(new Location(0f, 2f, 0f), Direction.Down)),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 1f, 0f), Direction.Right).DistanceFrom(new Ray(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 1f, 0f), Direction.Right).DistanceFrom(new Ray(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 1f, 0f), Direction.Right).DistanceFrom(new Ray(new Location(100f, 2000f, 0f), Direction.Left)),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 1f, 0f), Direction.Right).DistanceFrom(new Ray(new Location(100f, 2000f, 0f), Direction.Right)),
			TestTolerance
		);

		// BoundedLine
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)).DistanceFrom(
				new BoundedLine(new Location(-1f, -1f, 1f), new Location(1f, 1f, -1f))
			),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 1f, 0f), Direction.Right).DistanceFrom(
				new BoundedLine(new Location(0f, 2f, 0f), new Location(0f, 4f, 0f))
			),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(1000f, 1f, 0f), Direction.Right).DistanceFrom(
				new BoundedLine(new Location(0f, 4f, 0f), new Location(0f, 2f, 0f))
			),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 0f, 0f), Direction.Left).DistanceFrom(
				new BoundedLine(new Location(-3f, 1f, 0f), new Location(3f, 2f, 0f))
			),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 1f, 0f), Direction.Right).DistanceFrom(
				new BoundedLine(new Location(-10f, -4f, 0f), new Location(0f, 6f, 0f))
			),
			TestTolerance
		);
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
}