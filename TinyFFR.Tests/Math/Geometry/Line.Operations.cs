// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;
using System.Numerics;

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
			new Line(new Location(0f, 3f, 0f), new Direction(1f, 1f, 1f)).PointClosestTo(new Direction(1f, 1f, 1f).AnyOrthogonal() * 10f + new Location(2f, 5f, 2f)),
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



		// Line, Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);

		// Ray, Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);

		// BoundedRay, Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
				BoundedRay.FromStartPointAndVect(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new Line(new Location(0f, 1f, 0f), Direction.Left).FastIntersectionWith(
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
			new Line(new Location(100f, 1f, 0f), Direction.Up),
			new Line(new Location(100f, 100f, 0f), Direction.Down).ReflectedBy(plane)
		);
		Assert.AreEqual(
			new Line(new Location(100f, 1f, 0f), Direction.Down),
			new Line(new Location(100f, -100f, 0f), Direction.Up).ReflectedBy(plane)
		);
		AssertToleranceEquals(
			new Line(new Location(0f, 1f, 0f), new Direction(1f, 1f, -1f)),
			new Line(new Location(0f, 1f, 0f), new Direction(1f, -1f, -1f)).ReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(0f, 1f, 0f), new Direction(-2f, -1f, 2f)),
			new Line(new Location(0f, 1f, 0f), new Direction(-2f, 1f, 2f)).ReflectedBy(plane),
			TestTolerance
		);
		Assert.Null(
			new Line(new Location(0f, 2f, 0f), Direction.Right).ReflectedBy(plane)
		);
		Assert.Null(
			new Line(new Location(0f, 0f, 0f), Direction.Right).ReflectedBy(plane)
		);

		// Fast
		Assert.AreEqual(
			new Line(new Location(100f, 1f, 0f), Direction.Up),
			new Line(new Location(100f, 100f, 0f), Direction.Down).FastReflectedBy(plane)
		);
		Assert.AreEqual(
			new Line(new Location(100f, 1f, 0f), Direction.Down),
			new Line(new Location(100f, -100f, 0f), Direction.Up).FastReflectedBy(plane)
		);
		AssertToleranceEquals(
			new Line(new Location(0f, 1f, 0f), new Direction(1f, 1f, -1f)),
			new Line(new Location(0f, 1f, 0f), new Direction(1f, -1f, -1f)).FastReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(0f, 1f, 0f), new Direction(-2f, -1f, 2f)),
			new Line(new Location(0f, 1f, 0f), new Direction(-2f, 1f, 2f)).FastReflectedBy(plane),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineIncidentAngleOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			Angle.Zero,
			new Line(new Location(100f, 100f, 0f), Direction.Down).IncidentAngleWith(plane)
		);
		Assert.AreEqual(
			Angle.Zero,
			new Line(new Location(100f, -100f, 0f), Direction.Up).IncidentAngleWith(plane)
		);
		AssertToleranceEquals(
			Angle.EighthCircle,
			new Line(new Location(0f, 1f, 0f), new Direction(1f, -1f, 0f)).IncidentAngleWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			Angle.FromRadians(MathF.Acos(1f / 3f)),
			new Line(new Location(0f, 1f, 0f), new Direction(-2f, 1f, 2f)).IncidentAngleWith(plane),
			TestTolerance
		);
		Assert.Null(
			new Line(new Location(0f, 2f, 0f), Direction.Right).IncidentAngleWith(plane)
		);
		Assert.Null(
			new Line(new Location(0f, 0f, 0f), Direction.Right).IncidentAngleWith(plane)
		);

		// Fast
		Assert.AreEqual(
			Angle.Zero,
			new Line(new Location(100f, 100f, 0f), Direction.Down).FastIncidentAngleWith(plane)
		);
		Assert.AreEqual(
			Angle.Zero,
			new Line(new Location(100f, -100f, 0f), Direction.Up).FastIncidentAngleWith(plane)
		);
		AssertToleranceEquals(
			Angle.EighthCircle,
			new Line(new Location(0f, 1f, 0f), new Direction(1f, -1f, 0f)).FastIncidentAngleWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			Angle.FromRadians(MathF.Acos(1f / 3f)),
			new Line(new Location(0f, 1f, 0f), new Direction(-2f, 1f, 2f)).FastIncidentAngleWith(plane),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyGetIntersectionPointWithPlanes() {
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

		// Fast
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(100f, 100f, 0f), Direction.Down).FastIntersectionWith(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new Line(new Location(100f, -100f, 0f), Direction.Up).FastIntersectionWith(plane)
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
			if (expectedWithLineDir == null) {
				Assert.AreEqual(null, line.SplitBy(plane));
				Assert.AreEqual(false, line.IsIntersectedBy(plane));
			}
			else {
				Assert.AreEqual(true, line.IsIntersectedBy(plane));
				var (actualWithLineDir, actualOpposingLineDir) = line.SplitBy(plane)!.Value;
				Assert.AreEqual(expectedWithLineDir, actualWithLineDir);
				Assert.AreEqual(expectedOpposingLineDir, actualOpposingLineDir);
				(actualWithLineDir, actualOpposingLineDir) = line.FastSplitBy(plane);
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

		// Some older tests from previous iteration
		Assert.AreEqual(
			new Pair<Ray, Ray>(new Ray(new Location(100f, 1f, 0f), Direction.Down), new Ray(new Location(100f, 1f, 0f), Direction.Up)),
			new Line(new Location(100f, 100f, 0f), Direction.Down).SplitBy(plane)
		);
		Assert.AreEqual(
			new Pair<Ray, Ray>(new Ray(new Location(100f, 1f, 0f), Direction.Up), new Ray(new Location(100f, 1f, 0f), Direction.Down)),
			new Line(new Location(100f, -100f, 0f), Direction.Up).SplitBy(plane)
		);
		Assert.Null(
			new Line(new Location(0f, 2f, 0f), Direction.Right).SplitBy(plane)
		);
		Assert.Null(
			new Line(new Location(0f, 0f, 0f), Direction.Right).SplitBy(plane)
		);

		// Fast
		Assert.AreEqual(
			new Pair<Ray, Ray>(new Ray(new Location(100f, 1f, 0f), Direction.Down), new Ray(new Location(100f, 1f, 0f), Direction.Up)),
			new Line(new Location(100f, 100f, 0f), Direction.Down).FastSplitBy(plane)
		);
		Assert.AreEqual(
			new Pair<Ray, Ray>(new Ray(new Location(100f, 1f, 0f), Direction.Up), new Ray(new Location(100f, 1f, 0f), Direction.Down)),
			new Line(new Location(100f, -100f, 0f), Direction.Up).FastSplitBy(plane)
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

		Assert.AreEqual(0f, TestLine.DistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestLineDirection.AnyOrthogonal() * 10f), TestTolerance);
		Assert.AreEqual(10f, TestLine.DistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestLineDirection * 10f + TestLineDirection.AnyOrthogonal() * 10f), TestTolerance);
		Assert.AreEqual(-10f, TestLine.DistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestLineDirection * -10f + TestLineDirection.AnyOrthogonal() * 10f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineColinearityWithOtherLineLikes() {
		void AssertPair(bool expectation, Line line, Ray other, float? lineThickness, Angle? tolerance) {
			var flippedLine = new Line(line.PointOnLine, line.Direction.Flipped);
			var otherAsLine = other.ToLine();
			var otherAsFlippedLine = new Line(other.StartPoint, other.Direction.Flipped);
			var otherAsBoundedRay = other.ToBoundedRay(100f);

			// Line
			Assert.AreEqual(expectation, line.IsApproximatelyColinearWith(otherAsLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsLine.IsApproximatelyColinearWith(line, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, line.IsApproximatelyColinearWith(otherAsFlippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsLine.IsApproximatelyColinearWith(flippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedLine.IsApproximatelyColinearWith(otherAsLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsFlippedLine.IsApproximatelyColinearWith(line, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedLine.IsApproximatelyColinearWith(otherAsFlippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsFlippedLine.IsApproximatelyColinearWith(flippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			// Ray
			Assert.AreEqual(expectation, line.IsApproximatelyColinearWith(other, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, other.IsApproximatelyColinearWith(line, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, line.IsApproximatelyColinearWith(other.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, other.IsApproximatelyColinearWith(flippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedLine.IsApproximatelyColinearWith(other, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, other.Flipped.IsApproximatelyColinearWith(line, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedLine.IsApproximatelyColinearWith(other.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, other.Flipped.IsApproximatelyColinearWith(flippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			// BoundedRay
			Assert.AreEqual(expectation, line.IsApproximatelyColinearWith(otherAsBoundedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.IsApproximatelyColinearWith(line, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, line.IsApproximatelyColinearWith(otherAsBoundedRay.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.IsApproximatelyColinearWith(flippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedLine.IsApproximatelyColinearWith(otherAsBoundedRay, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.Flipped.IsApproximatelyColinearWith(line, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));

			Assert.AreEqual(expectation, flippedLine.IsApproximatelyColinearWith(otherAsBoundedRay.Flipped, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
			Assert.AreEqual(expectation, otherAsBoundedRay.Flipped.IsApproximatelyColinearWith(flippedLine, lineThickness ?? ILineLike.DefaultLineThickness, tolerance ?? ILineLike.DefaultParallelOrthogonalColinearTestApproximationDegrees));
		}

		AssertPair(true, TestLine, TestLine.ToRay(0f, false), null, null);
		AssertPair(false, TestLine.MovedBy(TestLine.Direction.AnyOrthogonal() * 1f), TestLine.ToRay(0f, false), 0.45f, null);
		AssertPair(true, TestLine.MovedBy(TestLine.Direction.AnyOrthogonal() * 1f), TestLine.ToRay(0f, false), 0.55f, null);
		AssertPair(false, TestLine.RotatedBy((TestLine.Direction >> TestLine.Direction.AnyOrthogonal()).WithAngle(1f)), TestLine.ToRay(0f, false), null, 0.9f);
		AssertPair(true, TestLine.RotatedBy((TestLine.Direction >> TestLine.Direction.AnyOrthogonal()).WithAngle(1f)), TestLine.ToRay(0f, false), null, 1.1f);
		AssertPair(false, TestLine.MovedBy(TestLine.Direction.AnyOrthogonal() * 1f).RotatedBy((TestLine.Direction >> TestLine.Direction.AnyOrthogonal()).WithAngle(1f)), TestLine.ToRay(0f, false), 0.45f, 0.9f);
		AssertPair(true, TestLine.MovedBy(TestLine.Direction.AnyOrthogonal() * 1f).RotatedBy((TestLine.Direction >> TestLine.Direction.AnyOrthogonal()).WithAngle(1f)), TestLine.ToRay(0f, false), 0.55f, 1.1f);
		AssertPair(false, TestLine.RotatedBy((TestLine.Direction >> TestLine.Direction.AnyOrthogonal()).WithAngle(1f)).MovedBy(TestLine.Direction.AnyOrthogonal() * 1f), TestLine.ToRay(0f, false), 0.45f, 0.9f);
		AssertPair(true, TestLine.RotatedBy((TestLine.Direction >> TestLine.Direction.AnyOrthogonal()).WithAngle(1f)).MovedBy(TestLine.Direction.AnyOrthogonal() * 1f), TestLine.ToRay(0f, false), 0.55f, 1.1f);
	}

	[Test]
	public void ShouldCorrectlyDetermineParallelismWithOtherElements() {
		void AssertCombination(bool expectation, Line line, Direction dir, Angle? tolerance) {
			var flippedLine = new Line(line.PointOnLine, line.Direction.Flipped);
			var plane = new Plane(dir.AnyOrthogonal(), Location.Origin);
			var dirLine = new Line(Location.Origin, dir);
			var dirRay = new Ray(Location.Origin, dir);
			var dirRayBounded = BoundedRay.FromStartPointAndVect(Location.Origin, dir * 10f);

			if (tolerance == null) {
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dir));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dir));
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dir.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dir.Flipped));

				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(plane));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(plane));
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(plane.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(plane.Flipped));

				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dirLine));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dirLine));
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));

				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dirRay));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dirRay));
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dirRay.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dirRay.Flipped));

				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dirRayBounded));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dirRayBounded));
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dirRayBounded.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dirRayBounded.Flipped));


				Assert.AreEqual(expectation, line.IsParallelTo(dir));
				Assert.AreEqual(expectation, flippedLine.IsParallelTo(dir));
				Assert.AreEqual(expectation, line.IsParallelTo(dir.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsParallelTo(dir.Flipped));

				Assert.AreEqual(expectation, line.IsParallelTo(plane));
				Assert.AreEqual(expectation, flippedLine.IsParallelTo(plane));
				Assert.AreEqual(expectation, line.IsParallelTo(plane.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsParallelTo(plane.Flipped));

				Assert.AreEqual(expectation, line.IsParallelTo(dirLine));
				Assert.AreEqual(expectation, flippedLine.IsParallelTo(dirLine));
				Assert.AreEqual(expectation, line.IsParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));
				Assert.AreEqual(expectation, flippedLine.IsParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));

				Assert.AreEqual(expectation, line.IsParallelTo(dirRay));
				Assert.AreEqual(expectation, flippedLine.IsParallelTo(dirRay));
				Assert.AreEqual(expectation, line.IsParallelTo(dirRay.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsParallelTo(dirRay.Flipped));

				Assert.AreEqual(expectation, line.IsParallelTo(dirRayBounded));
				Assert.AreEqual(expectation, flippedLine.IsParallelTo(dirRayBounded));
				Assert.AreEqual(expectation, line.IsParallelTo(dirRayBounded.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsParallelTo(dirRayBounded.Flipped));
			}
			else {
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dir.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dir.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(plane.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(plane.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));

				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dirRay.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dirRay.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, line.IsApproximatelyParallelTo(dirRayBounded.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyParallelTo(dirRayBounded.Flipped, tolerance.Value));
			}
		}

		AssertCombination(true, new Line(Location.Origin, Direction.Up), Direction.Up, null);
		AssertCombination(false, new Line(Location.Origin, Direction.Up), Direction.Left, null);
		AssertCombination(false, new Line(Location.Origin, Direction.Up), (1f, 1f, 0f), 44f);
		AssertCombination(true, new Line(Location.Origin, Direction.Up), (1f, 1f, 0f), 46f);

		Assert.AreEqual(false, TestLine.IsApproximatelyParallelTo(Direction.None));
		Assert.AreEqual(false, TestLine.IsApproximatelyParallelTo(new BoundedRay(Location.Origin, Location.Origin)));
		Assert.AreEqual(false, TestLine.IsParallelTo(Direction.None));
		Assert.AreEqual(false, TestLine.IsParallelTo(new BoundedRay(Location.Origin, Location.Origin)));
	}

	[Test]
	public void ShouldCorrectlyDetermineOrthogonalityWithOtherElements() {
		void AssertCombination(bool expectation, Line line, Direction dir, Angle? tolerance) {
			var flippedLine = new Line(line.PointOnLine, line.Direction.Flipped);
			var plane = new Plane(dir.AnyOrthogonal(), Location.Origin);
			var dirLine = new Line(Location.Origin, dir);
			var dirRay = new Ray(Location.Origin, dir);
			var dirRayBounded = BoundedRay.FromStartPointAndVect(Location.Origin, dir * 10f);

			if (tolerance == null) {
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dir));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dir));
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dir.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dir.Flipped));

				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(plane));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(plane));
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(plane.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(plane.Flipped));

				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dirLine));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dirLine));
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));

				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dirRay));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dirRay));
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dirRay.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dirRay.Flipped));

				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dirRayBounded));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dirRayBounded));
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dirRayBounded.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dirRayBounded.Flipped));


				Assert.AreEqual(expectation, line.IsOrthogonalTo(dir));
				Assert.AreEqual(expectation, flippedLine.IsOrthogonalTo(dir));
				Assert.AreEqual(expectation, line.IsOrthogonalTo(dir.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsOrthogonalTo(dir.Flipped));

				Assert.AreEqual(expectation, line.IsOrthogonalTo(plane));
				Assert.AreEqual(expectation, flippedLine.IsOrthogonalTo(plane));
				Assert.AreEqual(expectation, line.IsOrthogonalTo(plane.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsOrthogonalTo(plane.Flipped));

				Assert.AreEqual(expectation, line.IsOrthogonalTo(dirLine));
				Assert.AreEqual(expectation, flippedLine.IsOrthogonalTo(dirLine));
				Assert.AreEqual(expectation, line.IsOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));
				Assert.AreEqual(expectation, flippedLine.IsOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped)));

				Assert.AreEqual(expectation, line.IsOrthogonalTo(dirRay));
				Assert.AreEqual(expectation, flippedLine.IsOrthogonalTo(dirRay));
				Assert.AreEqual(expectation, line.IsOrthogonalTo(dirRay.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsOrthogonalTo(dirRay.Flipped));

				Assert.AreEqual(expectation, line.IsOrthogonalTo(dirRayBounded));
				Assert.AreEqual(expectation, flippedLine.IsOrthogonalTo(dirRayBounded));
				Assert.AreEqual(expectation, line.IsOrthogonalTo(dirRayBounded.Flipped));
				Assert.AreEqual(expectation, flippedLine.IsOrthogonalTo(dirRayBounded.Flipped));
			}
			else {
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dir, tolerance.Value));
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dir.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dir.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(plane, tolerance.Value));
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(plane.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(plane.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dirLine, tolerance.Value));
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(new Line(dirLine.PointOnLine, dirLine.Direction.Flipped), tolerance.Value));

				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dirRay, tolerance.Value));
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dirRay.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dirRay.Flipped, tolerance.Value));

				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dirRayBounded, tolerance.Value));
				Assert.AreEqual(expectation, line.IsApproximatelyOrthogonalTo(dirRayBounded.Flipped, tolerance.Value));
				Assert.AreEqual(expectation, flippedLine.IsApproximatelyOrthogonalTo(dirRayBounded.Flipped, tolerance.Value));
			}
		}

		AssertCombination(true, new Line(Location.Origin, Direction.Up), Direction.Left, null);
		AssertCombination(false, new Line(Location.Origin, Direction.Up), Direction.Up, null);
		AssertCombination(false, new Line(Location.Origin, Direction.Up), (1f, 1f, 0f), 44f);
		AssertCombination(true, new Line(Location.Origin, Direction.Up), (1f, 1f, 0f), 46f);

		Assert.AreEqual(false, TestLine.IsApproximatelyOrthogonalTo(Direction.None));
		Assert.AreEqual(false, TestLine.IsApproximatelyOrthogonalTo(new BoundedRay(Location.Origin, Location.Origin)));
		Assert.AreEqual(false, TestLine.IsOrthogonalTo(Direction.None));
		Assert.AreEqual(false, TestLine.IsOrthogonalTo(new BoundedRay(Location.Origin, Location.Origin)));
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithDirectionsAndLineLikes() {
		void AssertAgainstLeft(Line? expectation, Line input) {
			Assert.AreEqual(expectation, input.ParallelizedWith(Direction.Left));
			Assert.AreEqual(expectation, input.ParallelizedWith(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.ParallelizedWith(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.ParallelizedWith(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}
		void AssertFastAgainstLeft(Line expectation, Line input) {
			Assert.AreEqual(expectation, input.FastParallelizedWith(Direction.Left));
			Assert.AreEqual(expectation, input.FastParallelizedWith(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastParallelizedWith(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastParallelizedWith(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}

		// Various parallelizations from behind the plane
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), Direction.Left)
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), Direction.Right)
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), Direction.Left)
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), Direction.Right)
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f))
		);

		// Various parallelizations from in front the dir
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), Direction.Left)
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), Direction.Right)
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), Direction.Left)
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), Direction.Right)
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))
		);

		// Parallelizations from perpendicular directions
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Up)
		);
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Down)
		);
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Up)
		);
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Down)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstDirectionsAndLineLikes() {
		void AssertAgainstLeft(Line? expectation, Line input) {
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(Direction.Left));
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.OrthogonalizedAgainst(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}
		void AssertFastAgainstLeft(Line expectation, Line input) {
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(Direction.Left));
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(new Line(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(new Ray(Location.Origin, Direction.Left)));
			Assert.AreEqual(expectation, input.FastOrthogonalizedAgainst(new BoundedRay(Location.Origin, (1f, 0f, 0f))));
		}

		// Various orthogonalizations from behind the plane
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Left)
		);
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Right)
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f))
		);

		// Various orthogonalizations from in front the plane
		AssertAgainstLeft(
		null,
			new Line(new Location(0f, 2f, 0f), Direction.Left)
		);
		AssertAgainstLeft(
		null,
			new Line(new Location(0f, 2f, 0f), Direction.Right)
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f))
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, -1f, 0f))
		);

		// Orthogonalizations from perpendicular directions
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Up)
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), Direction.Down)
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Up)
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 0f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), Direction.Down)
		);
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithDirectionsAndLineLikesUsingSpecifiedPivotPoint() {
		void AssertAgainstLeft(Line? expectation, Line input, float pivotPointDistance) {
			AssertToleranceEquals(expectation, input.ParallelizedWith(Direction.Left, pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.ParallelizedWith(new Line(Location.Origin, Direction.Left), pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.ParallelizedWith(new Ray(Location.Origin, Direction.Left), pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.ParallelizedWith(new BoundedRay(Location.Origin, (1f, 0f, 0f)), pivotPointDistance), TestTolerance);
		}
		void AssertFastAgainstLeft(Line expectation, Line input, float pivotPointDistance) {
			AssertToleranceEquals(expectation, input.FastParallelizedWith(Direction.Left, pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.FastParallelizedWith(new Line(Location.Origin, Direction.Left), pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.FastParallelizedWith(new Ray(Location.Origin, Direction.Left), pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.FastParallelizedWith(new BoundedRay(Location.Origin, (1f, 0f, 0f)), pivotPointDistance), TestTolerance);
		}

		// Various parallelizations from behind the plane
		AssertAgainstLeft(
			new Line(new Location(100f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), Direction.Left), 100f
		);
		AssertAgainstLeft(
			new Line(new Location(100f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), Direction.Right), -100f
		);
		AssertAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)), 100f
		);
		AssertAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f), 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f)), -100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f, 0f, 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), Direction.Left), 100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f, 0f, 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), Direction.Right), -100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Left),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)), 100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f), 0f), Direction.Right),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, 1f, 0f)), -100f
		);

		// Various parallelizations from in front the plane
		AssertAgainstLeft(
			new Line(new Location(100f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), Direction.Left), 100f
		);
		AssertAgainstLeft(
			new Line(new Location(100f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), Direction.Right), -100f
		);
		AssertAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f)), 100f
		);
		AssertAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)), -100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f, 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), Direction.Left), 100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f, 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), Direction.Right), -100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Left),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, 1f, 0f)), 100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Right),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)), -100f
		);

		// Parallelizations from perpendicular directions
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Up), 100f
		);
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Down), -100f
		);
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Up), 100f
		);
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Down), -100f
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstDirectionsAndLineLikesUsingSpecifiedPivotPoint() {
		void AssertAgainstLeft(Line? expectation, Line input, float pivotPointDistance) {
			AssertToleranceEquals(expectation, input.OrthogonalizedAgainst(Direction.Left, pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.OrthogonalizedAgainst(new Line(Location.Origin, Direction.Left), pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.OrthogonalizedAgainst(new Ray(Location.Origin, Direction.Left), pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.OrthogonalizedAgainst(new BoundedRay(Location.Origin, (1f, 0f, 0f)), pivotPointDistance), TestTolerance);
		}
		void AssertFastAgainstLeft(Line expectation, Line input, float pivotPointDistance) {
			AssertToleranceEquals(expectation, input.FastOrthogonalizedAgainst(Direction.Left, pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.FastOrthogonalizedAgainst(new Line(Location.Origin, Direction.Left), pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.FastOrthogonalizedAgainst(new Ray(Location.Origin, Direction.Left), pivotPointDistance), TestTolerance);
			AssertToleranceEquals(expectation, input.FastOrthogonalizedAgainst(new BoundedRay(Location.Origin, (1f, 0f, 0f)), pivotPointDistance), TestTolerance);
		}

		// Various orthogonalizations from behind the plane
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Left), 100f
		);
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 0f, 0f), Direction.Right), -100f
		);
		AssertAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)), 100f
		);
		AssertAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f)), -100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), new Direction(1f, 1f, 0f)), 100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), 100f / MathF.Sqrt(2f), 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 0f)), -100f
		);

		// Various orthogonalizations from in front the plane
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Left), 100f
		);
		AssertAgainstLeft(
			null,
			new Line(new Location(0f, 2f, 0f), Direction.Right), -100f
		);
		AssertAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f)), 100f
		);
		AssertAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)), -100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), new Direction(1f, -1f, 0f)), 100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(100f / MathF.Sqrt(2f), -100f / MathF.Sqrt(2f) + 2f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), new Direction(-1f, 1f, 0f)), -100f
		);

		// Orthogonalizations from perpendicular directions
		AssertAgainstLeft(
			new Line(new Location(0f, 102f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Up), 100f
		);
		AssertAgainstLeft(
			new Line(new Location(0f, -98f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), Direction.Down), -100f
		);
		AssertAgainstLeft(
			new Line(new Location(0f, 100f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Up), 100f
		);
		AssertAgainstLeft(
			new Line(new Location(0f, -100f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), Direction.Down), -100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 102f, 0f), Direction.Up),
			new Line(new Location(0f, 2f, 0f), Direction.Up), 100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, -98f, 0f), Direction.Down),
			new Line(new Location(0f, 2f, 0f), Direction.Down), -100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, 100f, 0f), Direction.Up),
			new Line(new Location(0f, 0f, 0f), Direction.Up), 100f
		);
		AssertFastAgainstLeft(
			new Line(new Location(0f, -100f, 0f), Direction.Down),
			new Line(new Location(0f, 0f, 0f), Direction.Down), -100f
		);
	}
}