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
		AssertToleranceEquals(new BoundedRay(new Location(1f, 2f, -3f) + TestLineDirection * 10f, new Location(1f, 2f, -3f) + TestLineDirection * -10f), TestLine.ToBoundedRay(10f, -10f), TestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(1f, 2f, -3f) + TestLineDirection * -10f, new Location(1f, 2f, -3f) + TestLineDirection * 10f), TestLine.ToBoundedRay(-10f, 10f), TestTolerance);
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
	public void ShouldCorrectlyRotateAroundPoints() {
		void AssertCombination(Line expectation, Line input, Location pivotPoint, Rotation rotation) {
			AssertToleranceEquals(expectation, input.RotatedAroundPoint(rotation, pivotPoint), TestTolerance);
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), input * (pivotPoint, rotation));
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), input * (rotation, pivotPoint));
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), (pivotPoint, rotation) * input);
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), (rotation, pivotPoint) * input);
		}

		AssertCombination(new Line(Location.Origin, Direction.Forward), new Line(Location.Origin, Direction.Forward), (0f, 0f, 5f), Direction.Down % 180f);
		AssertCombination(new Line((0f, 0f, 10f), Direction.Right), new Line(Location.Origin, Direction.Right), (0f, 0f, 5f), Direction.Down % 180f);
		AssertCombination(new Line((0f, 10f, 0f), Direction.Right), new Line((0f, 10f, 0f), Direction.Down), (0f, 10f, 0f), Direction.Forward % 90f);
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
			new Line(new Location(100f, 0f, 0f), Direction.Left).PointClosestTo(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(-100f, 0f, 0f),
			new Line(new Location(100f, 0f, 0f), Direction.Left).PointClosestTo(new Location(-100f, 1f, 0f))
		);
		AssertToleranceEquals(
			new Location(2f, 5f, 2f),
			new Line(new Location(0f, 3f, 0f), new Direction(1f, 1f, 1f)).PointClosestTo(new Direction(1f, 1f, 1f).AnyPerpendicular() * 10f + new Location(2f, 5f, 2f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToOrigin() {
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Line(new Location(100f, 0f, 0f), Direction.Left).PointClosestToOrigin()
		);
		Assert.AreEqual(
			new Location(0f, -1f, 0f),
			new Line(new Location(100f, -1f, 0f), Direction.Left).PointClosestToOrigin()
		);
		AssertToleranceEquals(
			new Location(-1f, 2f, -1f),
			new Line(new Location(0f, 3f, 0f), new Direction(1f, 1f, 1f)).PointClosestToOrigin(),
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

		Assert.AreEqual(
			1f,
			new Line(new Location(0f, 1f, 0f), Direction.Left).DistanceFromOrigin()
		);
		Assert.AreEqual(
			1f,
			new Line(new Location(0f, 1f, 0f), Direction.Left).DistanceFromOrigin()
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 0f, 0f), Direction.Left).DistanceFromOrigin()
		);

		// Squared
		Assert.AreEqual(
			1f,
			new Line(new Location(100f, 0f, 0f), Direction.Left).DistanceSquaredFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			1f,
			new Line(new Location(100f, 0f, 0f), Direction.Left).DistanceSquaredFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(100f, 0f, 0f), Direction.Left).DistanceSquaredFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			2f,
			new Line(new Location(100f, 0f, 0f), Direction.Left).DistanceSquaredFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			1f,
			new Line(new Location(0f, 1f, 0f), Direction.Left).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			1f,
			new Line(new Location(0f, 1f, 0f), Direction.Left).DistanceSquaredFromOrigin()
		);
		Assert.AreEqual(
			0f,
			new Line(new Location(0f, 0f, 0f), Direction.Left).DistanceSquaredFromOrigin()
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
	public void ShouldCorrectlyDetermineClosestPointBetweenLines() {
		void AssertPair<TLine>(Location expectedResult, Line line, TLine other) where TLine : ILineLike {
			switch (other) {
				case Line l:
					AssertToleranceEquals(expectedResult, line.PointClosestTo(l), TestTolerance);
					Assert.AreEqual(line.PointClosestTo(l), other.ClosestPointOn(line));
					break;
				case Ray r:
					AssertToleranceEquals(expectedResult, line.PointClosestTo(r), TestTolerance);
					Assert.AreEqual(line.PointClosestTo(r), other.ClosestPointOn(line));
					break;
				case BoundedRay b:
					AssertToleranceEquals(expectedResult, line.PointClosestTo(b), TestTolerance);
					Assert.AreEqual(line.PointClosestTo(b), other.ClosestPointOn(line));
					break;
				default:
					Assert.Fail("Unknown line type");
					break;
			}
		}

		// Line
		AssertPair(
			new Location(0f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 1f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new Line(new Location(1f, 1f, 1f), new Direction(1f, 1f, 1f)),
			new Line(new Location(-1f, -1f, 1f), new Direction(-1f, -1f, 1f))
		);
		AssertPair(
			new Location(0f, 1f, 0f),
			new Line(new Location(1f, 2f, 1f), new Direction(1f, 1f, 1f)),
			new Line(new Location(-1f, 0f, 1f), new Direction(-1f, -1f, 1f))
		);

		// Ray
		AssertPair(
			new Location(0f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)), 
			new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 1f))
		);
		AssertPair(
			new Location(-1f, -1f, -1f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)),
			new Ray(new Location(-1f, -1f, -1f), new Direction(-1f, -1f, -1f))
		);
		AssertPair(
			new Location(1f, 1f, 0f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)),
			new Ray(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertPair(
			new Location(0f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Ray(new Location(0f, 2f, 0f), Direction.Up)
		);
		AssertPair(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right), 
			new Ray(new Location(100f, 2f, 0f), Direction.Up)
		);
		AssertPair(
			new Location(0f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Ray(new Location(0f, 2f, 0f), Direction.Down)
		);
		AssertPair(
			new Location(0f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Ray(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertPair(
			new Location(-1f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Ray(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))
		);
		AssertPair(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Ray(new Location(100f, 2000f, 0f), Direction.Left)
		);
		AssertPair(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Ray(new Location(100f, 2000f, 0f), Direction.Right)
		);

		// BoundedRay
		AssertPair(
			new Location(0f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f)),
			new BoundedRay(new Location(-1f, -1f, 1f), new Location(1f, 1f, -1f))
		);
		AssertPair(
			new Location(0f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new BoundedRay(new Location(0f, 2f, 0f), new Location(0f, 4f, 0f))
		);
		AssertPair(
			new Location(0f, 1f, 0f),
			new Line(new Location(1000f, 1f, 0f), Direction.Right),
			new BoundedRay(new Location(0f, 4f, 0f), new Location(0f, 2f, 0f))
		);
		AssertPair(
			new Location(-3f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new BoundedRay(new Location(-3f, 1f, 0f), new Location(3f, 2f, 0f))
		);
		AssertPair(
			new Location(-5f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new BoundedRay(new Location(-10f, -4f, 0f), new Location(0f, 6f, 0f))
		);
	}

	[Test]
	public void ShouldCorrectlyCalculateDistanceFromLines() { // These are regression tests
		Assert.AreEqual(
			16.738178f,
			TestLine.DistanceFrom(new Line(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			18.053492f,
			TestLine.DistanceFrom(new Ray(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			17.34369f,
			TestLine.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f) * -4f)),
			TestTolerance
		);

		Assert.AreEqual(
			0f,
			TestLine.DistanceFrom(TestLine),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestLine.DistanceFrom(TestLine.ToRay(0f, false)),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestLine.DistanceFrom(TestLine.ToBoundedRay(-1f, 1f)),
			TestTolerance
		);

		// Squared
		Assert.AreEqual(
			16.738178f * 16.738178f,
			TestLine.DistanceSquaredFrom(new Line(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			18.053492f * 18.053492f,
			TestLine.DistanceSquaredFrom(new Ray(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			17.34369f * 17.34369f,
			TestLine.DistanceSquaredFrom(BoundedRay.FromStartPointAndVect(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f) * -4f)),
			TestTolerance
		);

		Assert.AreEqual(
			0f,
			TestLine.DistanceSquaredFrom(TestLine),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestLine.DistanceSquaredFrom(TestLine.ToRay(0f, false)),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestLine.DistanceSquaredFrom(TestLine.ToBoundedRay(-1f, 1f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyImplementLocationAtDistanceFunctions() {
		ILineLike lineLike = new Line(new Location(0f, 1f, 0f), Direction.Right);

		Assert.AreEqual(true, lineLike.DistanceIsWithinLineBounds(-30000f));
		Assert.AreEqual(true, lineLike.DistanceIsWithinLineBounds(30000f));
		Assert.AreEqual(true, lineLike.DistanceIsWithinLineBounds(0f));
		
		Assert.AreEqual(-30000f, lineLike.BindDistance(-30000f));
		Assert.AreEqual(30000f, lineLike.BindDistance(30000f));
		Assert.AreEqual(0f, lineLike.BindDistance(0f));

		Assert.AreEqual(new Location(0f, 1f, 0f), ((Line) lineLike).LocationAtDistance(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), ((Line) lineLike).LocationAtDistance(3f));
		Assert.AreEqual(new Location(3f, 1f, 0f), ((Line) lineLike).LocationAtDistance(-3f));

		Assert.AreEqual(new Location(0f, 1f, 0f), lineLike.BoundedLocationAtDistance(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), lineLike.BoundedLocationAtDistance(3f));
		Assert.AreEqual(new Location(3f, 1f, 0f), lineLike.BoundedLocationAtDistance(-3f));

		Assert.AreEqual(new Location(0f, 1f, 0f), lineLike.UnboundedLocationAtDistance(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), lineLike.UnboundedLocationAtDistance(3f));
		Assert.AreEqual(new Location(3f, 1f, 0f), lineLike.UnboundedLocationAtDistance(-3f));

		Assert.AreEqual(new Location(0f, 1f, 0f), lineLike.LocationAtDistanceOrNull(0f));
		Assert.AreEqual(new Location(-3f, 1f, 0f), lineLike.LocationAtDistanceOrNull(3f));
		Assert.AreEqual(new Location(3f, 1f, 0f), lineLike.LocationAtDistanceOrNull(-3f));
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

		// BoundedRay
		Assert.Null(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).IntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 1.01f
			)
		);
	}

	[Test]
	public void ShouldCorrectlyTestForLineIntersections() {
		// Line
		Assert.False(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);

		// Ray
		Assert.False(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);

		// BoundedRay
		Assert.False(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new Line(new Location(0f, 1f, 0f), Direction.Left).IsIntersectedBy(
				BoundedRay.FromStartPointAndVect(new Location(100f, 6f, 0f), Direction.Down * 4f),
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
			new Ray(new Location(100f, 1f, 0f), Direction.Down),
			new Line(new Location(100f, 100f, 0f), Direction.Down).IntersectionWith(plane)
		);
		Assert.AreEqual(
			new Ray(new Location(100f, 1f, 0f), Direction.Up),
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
	public void ShouldCorrectlyTestForIntersectionWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.True(
			new Line(new Location(100f, 100f, 0f), Direction.Down).IsIntersectedBy(plane)
		);
		Assert.True(
			new Line(new Location(100f, -100f, 0f), Direction.Up).IsIntersectedBy(plane)
		);
		Assert.False(
			new Line(new Location(0f, 2f, 0f), Direction.Right).IsIntersectedBy(plane)
		);
		Assert.False(
			new Line(new Location(0f, 0f, 0f), Direction.Right).IsIntersectedBy(plane)
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
			new Line(new Location(100f, 100f, 0f), Direction.Down).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(100f, -100f, 0f), Direction.Up).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(0f, 2f, 0f),
			new Line(new Location(0f, 2f, 0f), Direction.Right).PointClosestTo(plane)
		);
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new Line(new Location(0f, 0f, 0f), Direction.Right).PointClosestTo(plane)
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
			plane.PointClosestToOrigin,
			new Line(new Location(0f, 2f, 0f), Direction.Right).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			plane.PointClosestToOrigin,
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
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), Direction.Left).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), Direction.Right).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f)).FastProjectedOnTo(plane)
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
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), Direction.Left).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), Direction.Right).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f)).FastProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 1f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f)).FastProjectedOnTo(plane)
		);

		// Projections from perpendicular directions
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Up).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Down).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Up).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			null,
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
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), Direction.Left).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), Direction.Right).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f)).FastParallelizedWith(plane)
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
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), Direction.Left).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), Direction.Right).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f)).FastParallelizedWith(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f)).FastParallelizedWith(plane)
		);

		// Parallelizations from perpendicular directions
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Up).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Down).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Up).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			null,
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
		Assert.AreEqual(
			new Line(new Location(100f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), Direction.Left).FastParallelizedWith(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(100f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), Direction.Right).FastParallelizedWith(plane, -100f)
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).FastParallelizedWith(plane, 100f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f), 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f)).FastParallelizedWith(plane, -100f),
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
		Assert.AreEqual(
			new Line(new Location(100f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), Direction.Left).FastParallelizedWith(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(100f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), Direction.Right).FastParallelizedWith(plane, -100f)
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f)).FastParallelizedWith(plane, 100f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)).FastParallelizedWith(plane, -100f),
			TestTolerance
		);

		// Parallelizations from perpendicular directions
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Up).ParallelizedWith(plane, 100f)
		);
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Down).ParallelizedWith(plane, -100f)
		);
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Up).ParallelizedWith(plane, 100f)
		);
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Down).ParallelizedWith(plane, -100f)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various orthogonalizations from behind the plane
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Left).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			null,
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
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f)).FastOrthogonalizedAgainst(plane)
		);

		// Various orthogonalizations from in front the plane
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Left).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			null,
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
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f)).FastOrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f)).FastOrthogonalizedAgainst(plane)
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
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Left).OrthogonalizedAgainst(plane, 100f)
		);
		Assert.AreEqual(
			null,
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
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)).FastOrthogonalizedAgainst(plane, 100f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f)).FastOrthogonalizedAgainst(plane, -100f),
			TestTolerance
		);

		// Various orthogonalizations from in front the plane
		Assert.AreEqual(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Left).OrthogonalizedAgainst(plane, 100f)
		);
		Assert.AreEqual(
			null,
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
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f)).FastOrthogonalizedAgainst(plane, 100f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)).FastOrthogonalizedAgainst(plane, -100f),
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
		Assert.AreEqual(
			new Line(new Location(0f, 102f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Up).FastOrthogonalizedAgainst(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(0f, -98f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), Direction.Down).FastOrthogonalizedAgainst(plane, -100f)
		);
		Assert.AreEqual(
			new Line(new Location(0f, 100f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Up).FastOrthogonalizedAgainst(plane, 100f)
		);
		Assert.AreEqual(
			new Line(new Location(0f, -100f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), Direction.Down).FastOrthogonalizedAgainst(plane, -100f)
		);
	}

	[Test]
	public void ShouldCorrectlyBeSplitByPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		void AssertSplit(Ray? expectedWithLineDir, Ray? expectedOpposingLineDir, Line line) {
			Assert.AreEqual(expectedWithLineDir, line.IntersectionWith(plane));
			var trySplitResult = line.TrySplit(plane, out var actualWithLineDir, out var actualOpposingLineDir);
			if (expectedWithLineDir == null) {
				Assert.AreEqual(false, trySplitResult);
				Assert.AreEqual(false, line.IsIntersectedBy(plane));
			}
			else {
				Assert.AreEqual(true, trySplitResult);
				Assert.AreEqual(true, line.IsIntersectedBy(plane));
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

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = new Line((0f, 10f, 0f), Direction.Forward);
		var max = new Line((0f, 20f, 0f), Direction.Right);

		AssertToleranceEquals(
			new Line((0f, 15f, 0f), (-1f, 0f, 1f)),
			new Line((0f, 15f, 0f), (-1f, 0f, 1f)).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line((0f, 20f, 0f), (-1f, 0f, 0f)),
			new Line((0f, 25f, 0f), (-1f, 0f, -1f)).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line((0f, 10f, 0f), (0f, 0f, 1f)),
			new Line((0f, 05f, 0f), (1f, 0f, 1f)).Clamp(min, max),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceAtPoints() {
		Assert.AreEqual(0f, TestLine.DistanceAtPointClosestTo((1f, 2f, -3f)), TestTolerance);
		Assert.AreEqual(10f, TestLine.DistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestLineDirection * 10f), TestTolerance);
		Assert.AreEqual(-10f, TestLine.DistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestLineDirection * -10f), TestTolerance);

		Assert.AreEqual(0f, TestLine.DistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestLineDirection.AnyPerpendicular() * 10f), TestTolerance);
		Assert.AreEqual(10f, TestLine.DistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestLineDirection * 10f + TestLineDirection.AnyPerpendicular() * 10f), TestTolerance);
		Assert.AreEqual(-10f, TestLine.DistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestLineDirection * -10f + TestLineDirection.AnyPerpendicular() * 10f), TestTolerance);
	}
}