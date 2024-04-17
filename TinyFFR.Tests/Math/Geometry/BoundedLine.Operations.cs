// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class BoundedLineTest {
	[Test]
	public void ShouldCorrectlyConvertToLine() {
		Assert.AreEqual(new Line(TestLine.StartPoint, TestLine.Direction), TestLine.ToLine());
	}

	[Test]
	public void ShouldCorrectlyConvertToRay() {
		AssertToleranceEquals(new Ray(TestLine.StartPoint, TestLine.Direction), TestLine.ToRayFromStart(), TestTolerance);
		AssertToleranceEquals(new Ray(TestLine.EndPoint, -TestLine.Direction), TestLine.ToRayFromEnd(), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFlip() {
		Assert.AreEqual(
			new BoundedLine(TestLine.EndPoint, TestLine.StartPoint),
			-TestLine
		);
	}

	[Test]
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(
			new BoundedLine(TestLine.StartPoint, TestLine.StartToEndVect * 2f),
			TestLine.ScaledFromStartBy(2f),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(TestLine.StartPoint, TestLine.StartToEndVect * -2f),
			TestLine.ScaledFromStartBy(-2f),
			TestTolerance
		);

		AssertToleranceEquals(
			new BoundedLine(new Location(-5f, -5f, -5f), new Location(15f, 15f, 15f)),
			new BoundedLine(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)) * 2f,
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(-5f, -5f, -5f), new Location(15f, 15f, 15f)).Flipped,
			-2f * new BoundedLine(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new BoundedLine(TestLine.EndPoint - TestLine.StartToEndVect * 2f, TestLine.EndPoint),
			TestLine.ScaledFromEndBy(2f),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(TestLine.EndPoint + TestLine.StartToEndVect * 2f, TestLine.EndPoint),
			TestLine.ScaledFromEndBy(-2f),
			TestTolerance
		);

		AssertToleranceEquals(
			new BoundedLine(new Location(-7.5f, -7.5f, -7.5f), new Location(12.5f, 12.5f, 12.5f)),
			new BoundedLine(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(2f, 0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(22.5f, 22.5f, 22.5f), new Location(2.5f, 2.5f, 2.5f)),
			new BoundedLine(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(-2f, 0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(7.5f, 7.5f, 7.5f), new Location(27.5f, 27.5f, 27.5f)),
			new BoundedLine(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(2f, -0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(-22.5f, -22.5f, -22.5f), new Location(-7.5f + 17.5f * -2f, -7.5f + 17.5f * -2f, -7.5f + 17.5f * -2f)),
			new BoundedLine(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(-2f, -0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(-15f, -15f, -15f), new Location(5f, 5f, 5f)),
			new BoundedLine(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(2f, 1.5f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(45f, 45f, 45f), new Location(25f, 25f, 25f)),
			new BoundedLine(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(-2f, 1.5f * MathF.Sqrt(300f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyRotate() {
		var rotation = 90f % Direction.Down;
		var xzLine = new BoundedLine(new Location(3f, 0f, -3f), new Location(-3f, 0f, 3f));

		AssertToleranceEquals(
			new BoundedLine(new Location(3f, 0f, 3f), new Location(-3f, 0f, -3f)),
			xzLine * rotation,
			TestTolerance
		);
		AssertToleranceEquals(
			xzLine.RotatedAroundMiddleBy(rotation),
			rotation * xzLine,
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(3f, 0f, -3f), new Location(-3f, 0f, -9f)),
			xzLine.RotatedAroundStartBy(rotation),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(3f, 0f, 9f), new Location(-3f, 0f, 3f)),
			xzLine.RotatedAroundEndBy(rotation),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(3f, 0f, 6f), new Location(-3f, 0f, 0f)),
			xzLine.RotatedAroundPivotDistance(rotation, xzLine.Length * 0.75f),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(3f, 0f, 0f), new Location(-3f, 0f, -6f)),
			xzLine.RotatedAroundPivotDistance(rotation, xzLine.Length * 0.25f),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(-3f, 0f, 3f), new Location(-9f, 0f, -3f)),
			xzLine.RotatedAroundPoint(rotation, (-3f, 0f, -3f)),
			TestTolerance
		);
	}
	
	[Test]
	public void ShouldCorrectlyMove() {
		var vect = new Vect(5f, -3f, 12f);
	
		AssertToleranceEquals(
			new BoundedLine(TestLine.StartPoint + vect, TestLine.EndPoint + vect),
			TestLine + vect,
			TestTolerance
		);
		AssertToleranceEquals(
			TestLine.Direction,
			(vect + TestLine).Direction,
			TestTolerance
		);
		AssertToleranceEquals(
			TestLine.StartToEndVect,
			(vect + TestLine).StartToEndVect,
			TestTolerance
		);
		Assert.AreEqual(
			TestLine.Length,
			(vect + TestLine).Length,
			TestTolerance
		);
		Assert.AreEqual(
			TestLine.LengthSquared,
			(vect + TestLine).LengthSquared,
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToLocation() {
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new BoundedLine(new Location(-100f, 0f, 0f), Direction.Left * 200f).ClosestPointTo(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(-100f, 0f, 0f),
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Right * 200f).ClosestPointTo(new Location(-100f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).ClosestPointTo(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Left * 200f).ClosestPointTo(new Location(-100f, 1f, 0f))
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToOrigin() {
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Right * 200f).ClosestPointToOrigin()
		);
		Assert.AreEqual(
			new Location(0f, -1f, 0f),
			new BoundedLine(new Location(100f, -1f, 0f), Direction.Right * 200f).ClosestPointToOrigin()
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).ClosestPointToOrigin()
		);
		Assert.AreEqual(
			new Location(100f, -1f, 0f),
			new BoundedLine(new Location(100f, -1f, 0f), Direction.Left * 200f).ClosestPointToOrigin()
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLocation() {
		Assert.AreEqual(
			1f,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			1f,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			0f,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			MathF.Sqrt(2f),
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Right * 200f).DistanceFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			MathF.Sqrt(10001f),
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			MathF.Sqrt(10001f),
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(0f, -1f, 0f))
		);
		Assert.AreEqual(
			200f,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(-100f, 0f, 0f))
		);
		Assert.AreEqual(
			MathF.Sqrt(40002f),
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(-100f, 1f, -1f)),
			TestTolerance
		);

		Assert.AreEqual(
			10f,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(310f, 0f, 0f)),
			TestTolerance
		);
		Assert.AreEqual(
			10f,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).DistanceFrom(new Location(90f, 0f, 0f)),
			TestTolerance
		);

		Assert.AreEqual(
			1f,
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			1f,
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			0f,
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Left * 100f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			1f,
			new BoundedLine(new Location(1f, 0f, 0f), Direction.Left * 100f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			0f,
			new BoundedLine(new Location(-1f, 0f, 0f), Direction.Left * 100f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			1f,
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Right * 9f).DistanceFromOrigin()
		);
		Assert.AreEqual(
			0f,
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Right * 11f).DistanceFromOrigin()
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineContainmentOfLocation() {
		Assert.AreEqual(
			false,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Right * 200f).Contains(new Location(0f, 1f, 0f))
		);
		Assert.AreEqual(
			true,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Right * 200f).Contains(new Location(0f, 1f, 0f), 1.1f)
		);
		Assert.AreEqual(
			false,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Right * 200f).Contains(new Location(0f, 1f, 0f), 0.9f)
		);
		Assert.AreEqual(
			true,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Right * 200f).Contains(new Location(0f, -1f, 0f), 1.1f)
		);
		Assert.AreEqual(
			false,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Right * 200f).Contains(new Location(0f, -1f, 0f), 0.9f)
		);
		Assert.AreEqual(
			false,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(99f, 0f, 0f), 0.9f)
		);
		Assert.AreEqual(
			true,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(99f, 0f, 0f), 1.1f)
		);
		Assert.AreEqual(
			true,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(100f, 0f, 0f))
		);
		Assert.AreEqual(
			true,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(110f, 0f, 0f))
		);
		Assert.AreEqual(
			false,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(310f, 0f, 0f))
		);
		Assert.AreEqual(
			false,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(90f, 0f, 0f))
		);
		Assert.AreEqual(
			false,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(310f, 0f, 0f), 9.9f)
		);
		Assert.AreEqual(
			false,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(90f, 0f, 0f), 9.9f)
		);
		Assert.AreEqual(
			true,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(310f, 0f, 0f), 10.1f)
		);
		Assert.AreEqual(
			true,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Left * 200f).Contains(new Location(90f, 0f, 0f), 10.1f)
		);
	}

	[Test]
	public void ShouldCorrectlyReturnClosestPointToOtherLine() {
		void AssertPair<TLine>(Location expectedResult, BoundedLine line, TLine other) where TLine : ILine {
			AssertToleranceEquals(expectedResult, line.ClosestPointTo(other), TestTolerance);
			Assert.AreEqual(line.ClosestPointTo(other), other.ClosestPointOn(line));
		}

		// Line
		AssertPair(
			new Location(0f, 0f, 0f),
			new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f),
			new Line(new Location(0f, 0f, 0f), new Direction(-1f, -1f, 1f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Line(new Location(100f, 10f, 0f), Direction.Left)
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Line(new Location(100f, -10f, 0f), Direction.Left)
		);
		AssertPair(
			new Location(0f, 100f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Line(new Location(100f, 110f, 0f), Direction.Left)
		);

		// Ray
		AssertPair(
			new Location(0f, 20f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 1f, 1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -1f, 1f))
		);
		AssertPair(
			new Location(0f, 30f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 2f, 1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -1.5f, 1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -2.5f, 1f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -2.5f, -1f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 1f, -1f))
		);
		AssertPair(
			new Location(0f, 0f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, -1f, -10f), Direction.Forward)
		);
		AssertPair(
			new Location(0f, 0f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new Ray(new Location(0f, 1f, -1f), new Direction(0f, -100f, 0.1f))
		);
		AssertPair(
			new Location(0f, 15f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 15f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, 1f, 1f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 15f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -1f, -1f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 15f),
			new Ray(new Location(0f, 10f, -10f), new Direction(0f, -1f, -1f))
		);
		AssertPair(
			new Location(0f, 15f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 15f),
			new Ray(new Location(0f, 20f, -10f), new Direction(0f, -1f, -1f))
		);

		// BoundedLine
		AssertPair(
			new Location(0f, 20f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, 10f, -10f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 20f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, 30f, 10f), new Location(0f, 10f, -10f))
		);
		AssertPair(
			new Location(0f, 30f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, 30f, 10f), new Location(0f, 10f, 30f))
		);
		AssertPair(
			new Location(0f, 30f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, 10f, 30f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, -10f, -10f), new Location(0f, 0f, 10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, 0f, 10f), new Location(0f, -10f, -10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, -10f, -10f), new Location(0f, -10f, 0f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, -10f, 0f), new Location(0f, -10f, -10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, -10f, -10f), new Location(0f, -50f, -10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, -50f, -10f), new Location(0f, -10f, -10f))
		);
		AssertPair(
			new Location(0f, 20f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 100f),
			new BoundedLine(new Location(0f, 10f, -10f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 10f),
			new BoundedLine(new Location(0f, 10f, -10f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 10f, 0),
			new BoundedLine(Location.Origin, Direction.Up * 10f),
			new BoundedLine(new Location(0f, 30f, 10f), new Location(0f, 10f, -10f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 10f),
			new BoundedLine(new Location(0f, 30f, 10f), new Location(0f, 10f, 30f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 10f),
			new BoundedLine(new Location(0f, 10f, 30f), new Location(0f, 30f, 10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 10f),
			new BoundedLine(new Location(0f, -10f, 10f), new Location(0f, -20f, 10f))
		);
		AssertPair(
			new Location(0f, 0f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 10f),
			new BoundedLine(new Location(0f, -20f, 10f), new Location(0f, -10f, 10f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 10f),
			new BoundedLine(new Location(0f, 30f, 10f), new Location(0f, 20f, 10f))
		);
		AssertPair(
			new Location(0f, 10f, 0f),
			new BoundedLine(Location.Origin, Direction.Up * 10f),
			new BoundedLine(new Location(0f, 20f, 10f), new Location(0f, 30f, 10f))
		);
		Assert.GreaterOrEqual(new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).Y, 0f);
		Assert.LessOrEqual(new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).Y, 50f);
		Assert.AreEqual(0f, new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).X);
		Assert.AreEqual(0f, new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, -10f, -10f), new Location(0f, 50f, -10f))).Z);
		Assert.GreaterOrEqual(new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).Y, 0f);
		Assert.LessOrEqual(new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).Y, 50f);
		Assert.AreEqual(0f, new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).X);
		Assert.AreEqual(0f, new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 50f, -10f), new Location(0f, -10f, -10f))).Z);
		
		Assert.GreaterOrEqual(new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).Y, 10f);
		Assert.LessOrEqual(new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).Y, 50f);
		Assert.AreEqual(0f, new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).X);
		Assert.AreEqual(0f, new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 10f, -10f), new Location(0f, 50f, -10f))).Z);
		Assert.GreaterOrEqual(new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).Y, 10f);
		Assert.LessOrEqual(new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).Y, 50f);
		Assert.AreEqual(0f, new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).X);
		Assert.AreEqual(0f, new BoundedLine(Location.Origin, Direction.Up * 100f).ClosestPointTo(new BoundedLine(new Location(0f, 50f, -10f), new Location(0f, 10f, -10f))).Z);
	}

	[Test]
	public void ShouldCorrectlyCalculateDistanceFromLines() { // These are regression tests
		Assert.AreEqual(
			16.738178f,
			TestLine.DistanceFrom(new Line(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			18.3847770f,
			TestLine.DistanceFrom(new Ray(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f))),
			TestTolerance
		);
		Assert.AreEqual(
			17.34369f,
			TestLine.DistanceFrom(new BoundedLine(new Location(15f, -3f, 12f), new Direction(-2f, 0f, 14f) * -4f)),
			TestTolerance
		);

		Assert.AreEqual(
			0f,
			TestLine.DistanceFrom(TestLine.ToLine()),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestLine.DistanceFrom(TestLine.ToRayFromStart()),
			TestTolerance
		);
		Assert.AreEqual(
			0f,
			TestLine.DistanceFrom(TestLine),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyImplementLocationAtDistanceFunctions() {
		var line = new BoundedLine(new Location(0f, 1f, 0f), Direction.Right * 100f);

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
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(-1f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 0.99f
			)
		);
		Assert.AreEqual(
			new Location(101f, 1f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Line(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 1.01f
			)
		);

		// Ray
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(-1f, 1f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(101f, 1f, 0f), Direction.Left),
				lineThickness: 0.99f
			)
		);
		Assert.AreEqual(
			new Location(101f, 1f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(101f, 1f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 0.99f
			)
		);
		Assert.AreEqual(
			new Location(101f, 1f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new Ray(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 1.01f
			)
		);

		// BoundedLine
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 0.01f
			)
		);
		Assert.NotNull(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.AreEqual(
			new Location(100f, 2f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.AreEqual(
			new Location(0f, 1f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(0f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.AreEqual(
			new Location(0f, 1f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(-1f, 1f, 0f), new Location(0f, 1f, 0f))
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(-1f, 1f, 0f), new Location(-2f, 1f, 0f))
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(-2f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.AreEqual(
			new Location(10f, 1f, 0f),
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(10f, 2f, 0f), new Location(10f, 0f, 0f))
			)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IntersectionWith(
				new BoundedLine(new Location(10f, 2f, 0f), new Location(10f, 4f, 0f))
			)
		);
	}

	[Test]
	public void ShouldCorrectlyTestForLineIntersections() {
		// Line
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(-1f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 0.99f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Line(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 1.01f
			)
		);

		// Ray
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(0f, 2f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Down),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(100f, 2f, 0f), Direction.Up),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(-1f, 1f, 0f), Direction.Right),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(101f, 1f, 0f), Direction.Left),
				lineThickness: 0.99f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(101f, 1f, 0f), Direction.Left),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 0.99f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new Ray(new Location(101f, 2f, 0f), Direction.Down),
				lineThickness: 1.01f
			)
		);

		// BoundedLine
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(0f, 2f, 0f), Direction.Left * 1f),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(100f, 2f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(100f, 2f, 0f), Direction.Up * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 0.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(100f, 6f, 0f), Direction.Down * 4f),
				lineThickness: 1.01f
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(0f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(-1f, 1f, 0f), new Location(0f, 1f, 0f))
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(-1f, 1f, 0f), new Location(-2f, 1f, 0f))
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(-2f, 1f, 0f), new Location(-1f, 1f, 0f))
			)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(10f, 2f, 0f), new Location(10f, 0f, 0f))
			)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f).IsIntersectedBy(
				new BoundedLine(new Location(10f, 2f, 0f), new Location(10f, 4f, 0f))
			)
		);
	}

	[Test]
	public void ShouldCorrectlyReflectOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new BoundedLine(new Location(100f, 1f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(100f, 1f, 0f), Direction.Down * 100f).ReflectedBy(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(100f, 1f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(100f, 1f, 0f), Direction.Up * 100f).ReflectedBy(plane)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 2f, 0f), Direction.Right * 100f).ReflectedBy(plane)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Right * 100f).ReflectedBy(plane)
		);
		Assert.Null(
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Up * 100f).ReflectedBy(plane)
		);
		Assert.Null(
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Down * 100f).ReflectedBy(plane)
		);

		AssertToleranceEquals(
			new BoundedLine(new Location(0f, 1f, 0f), new Direction(0f, 1f, -1f) * MathF.Sqrt(50f) * 2f),
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).ReflectedBy(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedLine(new Location(0f, 1f, 0f), new Direction(0f, -1f, 1f) * MathF.Sqrt(50f) * 1f),
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.ReflectedBy(plane),
			TestTolerance
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).ReflectedBy(plane)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).Flipped.ReflectedBy(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyIntersectWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(100f, 101f, 0f), Direction.Down * 100f).IntersectionWith(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(100f, -99f, 0f), Direction.Up * 100f).IntersectionWith(plane)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 2f, 0f), Direction.Right * 100f).IntersectionWith(plane)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Right * 100f).IntersectionWith(plane)
		);
		Assert.Null(
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Up * 100f).IntersectionWith(plane)
		);
		Assert.Null(
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Down * 100f).IntersectionWith(plane)
		);

		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).IntersectionWith(plane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0f, 1f, 0f),
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.IntersectionWith(plane),
			TestTolerance
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).IntersectionWith(plane)
		);
		Assert.Null(
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).Flipped.IntersectionWith(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyTestForIntersectionWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.True(
			new BoundedLine(new Location(100f, 101f, 0f), Direction.Down * 100f).IsIntersectedBy(plane)
		);
		Assert.True(
			new BoundedLine(new Location(100f, -99f, 0f), Direction.Up * 100f).IsIntersectedBy(plane)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 2f, 0f), Direction.Right * 100f).IsIntersectedBy(plane)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Right * 100f).IsIntersectedBy(plane)
		);
		Assert.False(
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Up * 100f).IsIntersectedBy(plane)
		);
		Assert.False(
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Down * 100f).IsIntersectedBy(plane)
		);

		Assert.True(
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).IsIntersectedBy(plane)
		);
		Assert.True(
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 3f).Flipped.IsIntersectedBy(plane)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).IsIntersectedBy(plane)
		);
		Assert.False(
			new BoundedLine(new Location(0f, 6f, 5f), new Direction(0f, -1f, -1f) * MathF.Sqrt(50f) * 0.5f).Flipped.IsIntersectedBy(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineSignedDistanceFromPlane() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		void AssertDistance(float expectedSignedDistance, BoundedLine line) {
			Assert.AreEqual(expectedSignedDistance, line.SignedDistanceFrom(plane));
			Assert.AreEqual(MathF.Abs(expectedSignedDistance), line.DistanceFrom(plane));
			Assert.AreEqual(expectedSignedDistance, line.Flipped.SignedDistanceFrom(plane));
			Assert.AreEqual(MathF.Abs(expectedSignedDistance), line.Flipped.DistanceFrom(plane));
		}

		AssertDistance(
			0f,
			new BoundedLine(new Location(100f, 101f, 0f), Direction.Down * 100f)
		);
		AssertDistance(
			0f,
			new BoundedLine(new Location(100f, -99f, 0f), Direction.Up * 100f)
		);
		AssertDistance(
			1f,
			new BoundedLine(new Location(0f, 2f, 0f), Direction.Right * 100f)
		);
		AssertDistance(
			-1f,
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Right * 100f)
		);
		AssertDistance(
			99f,
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Up * 100f)
		);
		AssertDistance(
			-101f,
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Down * 100f)
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Down * 100f).ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Down * 100f).Flipped.ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Up * 100f).ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 0f, 0f),
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Up * 100f).Flipped.ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(0f, 2f, 0f),
			new BoundedLine(new Location(0f, 2f, 0f), Direction.Right * 100f).ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(-100f, 2f, 0f),
			new BoundedLine(new Location(0f, 2f, 0f), Direction.Right * 100f).Flipped.ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(0f, 0f, 0f),
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Right * 100f).ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(-100f, 0f, 0f),
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Right * 100f).Flipped.ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 100f, 0f),
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Up * 100f).ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, 100f, 0f),
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Up * 100f).Flipped.ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, -100f, 0f),
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Down * 100f).ClosestPointTo(plane)
		);
		Assert.AreEqual(
			new Location(100f, -100f, 0f),
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Down * 100f).Flipped.ClosestPointTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Down * 200f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Up * 200f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Down * 50f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Up * 50f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			plane.ClosestPointToOrigin,
			new BoundedLine(new Location(0f, 2f, 0f), Direction.Right * 200f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			plane.ClosestPointToOrigin,
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Right * 200f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Up * 200f).ClosestPointOn(plane)
		);
		Assert.AreEqual(
			new Location(100f, 1f, 0f),
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Down * 200f).ClosestPointOn(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineRelationshipWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Down * 200f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Up * 200f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Down * 50f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Up * 50f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Down * 200f).Flipped.RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneIntersectsObject,
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Up * 200f).Flipped.RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Down * 50f).Flipped.RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Up * 50f).Flipped.RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			new BoundedLine(new Location(0f, 2f, 0f), Direction.Right * 200f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Right * 200f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesTowardsObject,
			new BoundedLine(new Location(100f, 100f, 0f), Direction.Up * 200f).RelationshipTo(plane)
		);
		Assert.AreEqual(
			PlaneObjectRelationship.PlaneFacesAwayFromObject,
			new BoundedLine(new Location(100f, -100f, 0f), Direction.Down * 200f).RelationshipTo(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyProjectOnToPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various projections from behind the plane
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Left * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Right * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Left * 100f / MathF.Sqrt(2f)),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Right * 100f / MathF.Sqrt(2f)),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Left * 100f / MathF.Sqrt(2f)),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Right * 100f / MathF.Sqrt(2f)),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f).ProjectedOnTo(plane, preserveLength: true)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f) * 100f).ProjectedOnTo(plane, preserveLength: true)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f) * 100f).ProjectedOnTo(plane, preserveLength: true)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f) * 100f).ProjectedOnTo(plane, preserveLength: true)
		);

		// Various projections from in front the plane
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Left * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Right * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Left * 100f / MathF.Sqrt(2f)),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Right * 100f / MathF.Sqrt(2f)),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Left * 100f / MathF.Sqrt(2f)),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Right * 100f / MathF.Sqrt(2f)),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f) * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f) * 100f).ProjectedOnTo(plane, preserveLength: true)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f) * 100f).ProjectedOnTo(plane, preserveLength: true)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f).ProjectedOnTo(plane, preserveLength: true)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f) * 100f).ProjectedOnTo(plane, preserveLength: true)
		);

		// Projections from perpendicular directions
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Vect.Zero),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Up * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Vect.Zero),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Down * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Vect.Zero),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Up * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Vect.Zero),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Down * 100f).ProjectedOnTo(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Up * 100f).ProjectedOnTo(plane, preserveLength: true)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Down * 100f).ProjectedOnTo(plane, preserveLength: true)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Up * 100f).ProjectedOnTo(plane, preserveLength: true)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 1f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Down * 100f).ProjectedOnTo(plane, preserveLength: true)
		);
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various parallelizations from behind the plane
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Left * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Right * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f) * 100f).ParallelizedWith(plane)
		);

		// Various parallelizations from in front the plane
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Left * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Right * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Left * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Right * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f) * 100f).ParallelizedWith(plane)
		);

		// Projections from parallelizations directions
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Up * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Down * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Up * 100f).ParallelizedWith(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Down * 100f).ParallelizedWith(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		// Various orthogonalizations from behind the plane
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Left * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Right * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(-1f, 1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(1f, -1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), new Direction(-1f, -1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);

		// Various orthogonalizations from in front the plane
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Left * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Right * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(1f, 1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(-1f, 1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), new Direction(-1f, -1f, 0f) * 100f).OrthogonalizedAgainst(plane)
		);

		// Orthogonalizations from perpendicular directions
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Up * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(10f, 2f, 0f), Direction.Down * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Up * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Up * 100f).OrthogonalizedAgainst(plane)
		);
		Assert.AreEqual(
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Down * 100f),
			new BoundedLine(new Location(10f, 0f, 0f), Direction.Down * 100f).OrthogonalizedAgainst(plane)
		);
	}

	[Test]
	public void ShouldCorrectlyBeSplitByPlanes() {
		var plane = new Plane(Direction.Up, new Location(0f, 1f, 0f));

		void AssertSplit(BoundedLine? expectedToPlane, BoundedLine? expectedFromPlane, BoundedLine line) {
			AssertToleranceEquals(expectedFromPlane, line.SlicedBy(plane), TestTolerance);
			var trySplitResult = line.TrySplit(plane, out var actualToPlane, out var actualFromPlane);
			if (expectedToPlane == null) Assert.AreEqual(false, trySplitResult);
			else {
				Assert.AreEqual(true, trySplitResult);
				AssertToleranceEquals(expectedToPlane, actualToPlane, TestTolerance);
				AssertToleranceEquals(expectedFromPlane, actualFromPlane, TestTolerance);
			}
		}

		AssertSplit(
			null,
			null,
			new BoundedLine(new Location(0f, 2f, 0f), Direction.Right * 100f)
		);
		AssertSplit(
			null,
			null,
			new BoundedLine(new Location(0f, 2f, 0f), Direction.Left * 100f)
		);
		AssertSplit(
			null,
			null,
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Right * 100f)
		);
		AssertSplit(
			null,
			null,
			new BoundedLine(new Location(0f, 0f, 0f), Direction.Left * 100f)
		);
		AssertSplit(
			null,
			null,
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Right * 100f)
		);
		AssertSplit(
			null,
			null,
			new BoundedLine(new Location(0f, 1f, 0f), Direction.Left * 100f)
		);

		AssertSplit(
			new BoundedLine(new Location(100f, 2f, 0f), new Location(100f, 1f, 0f)),
			new BoundedLine(new Location(100f, 1f, 0f), Direction.Down * 99f),
			new BoundedLine(new Location(100f, 2f, 0f), Direction.Down * 100f)
		);
		AssertSplit(
			new BoundedLine(new Location(100f, 0f, 0f), new Location(100f, 1f, 0f)),
			new BoundedLine(new Location(100f, 1f, 0f), Direction.Up * 99f),
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Up * 100f)
		);
		AssertSplit(
			null,
			null,
			new BoundedLine(new Location(100f, 2f, 0f), Direction.Up * 100f)
		);
		AssertSplit(
			null,
			null,
			new BoundedLine(new Location(100f, 0f, 0f), Direction.Down * 100f)
		);
		AssertSplit(
			new BoundedLine(new Location(100f, 2f, 0f), new Location(101f, 1f, 0f)),
			new BoundedLine(new Location(101f, 1f, 0f), new Direction(1f, -1f, 0f) * (100f - MathF.Sqrt(2f))),
			new BoundedLine(new Location(100f, 2f, 0f), new Direction(1f, -1f, 0f) * 100f)
		);
		AssertSplit(
			new BoundedLine(new Location(100f, 0f, 0f), new Location(101f, 1f, 0f)),
			new BoundedLine(new Location(101f, 1f, 0f), new Direction(1f, 1f, 0f) * (100f - MathF.Sqrt(2f))),
			new BoundedLine(new Location(100f, 0f, 0f), new Direction(1f, 1f, 0f) * 100f)
		);

		AssertSplit(
			null,
			null,
			new BoundedLine(new Location(0f, 10f, 0f), Direction.Down * 5f)
		);
		AssertSplit(
			null,
			null,
			new BoundedLine(new Location(0f, -10f, 0f), Direction.Up * 5f)
		);
		AssertSplit(
			new BoundedLine(new Location(0f, 10f, 0f), Direction.Down * 9f),
			new BoundedLine(new Location(0f, 1f, 0f), Vect.Zero),
			new BoundedLine(new Location(0f, 10f, 0f), Direction.Down * 9f)
		);
		AssertSplit(
			new BoundedLine(new Location(0f, -10f, 0f), Direction.Up * 11f),
			new BoundedLine(new Location(0f, 1f, 0f), Vect.Zero),
			new BoundedLine(new Location(0f, -10f, 0f), Direction.Up * 11f)
		);
	}
}