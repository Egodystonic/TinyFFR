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

		AssertToleranceEquals(
			new Location(0f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)).ClosestPointTo(
				new BoundedLine(new Location(-1f, -1f, 1f), new Location(1f, 1f, -1f))
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)).ClosestPointTo(
				new BoundedLine(new Location(-1f, -1f, 1f), new Location(-3f, -3f, 3f))
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-3f, 0f, 0f), // TODO this is giving me the intersection point, not the closest point
			new Line(new Location(0f, 0f, 0f), Direction.Left).ClosestPointTo(
				new BoundedLine(new Location(-3f, 1f, 0f), new Location(3f, 2f, 0f))
			),
			TestTolerance
		);
	}
}