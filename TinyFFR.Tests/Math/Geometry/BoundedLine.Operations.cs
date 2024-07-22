// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class BoundedRayTest {
	[Test]
	public void ShouldCorrectlyConvertToLine() {
		Assert.AreEqual(new Line(TestRay.StartPoint, TestRay.Direction), TestRay.ToLine());
	}

	[Test]
	public void ShouldCorrectlyConvertToRay() {
		AssertToleranceEquals(new Ray(TestRay.StartPoint, TestRay.Direction), TestRay.ToRayFromStart(), TestTolerance);
		AssertToleranceEquals(new Ray(TestRay.EndPoint, -TestRay.Direction), TestRay.ToRayFromEnd(), TestTolerance);
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
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)) * 2f,
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(-5f, -5f, -5f), new Location(15f, 15f, 15f)).Flipped,
			-2f * new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)),
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
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(2f, 0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(22.5f, 22.5f, 22.5f), new Location(2.5f, 2.5f, 2.5f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(-2f, 0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(7.5f, 7.5f, 7.5f), new Location(27.5f, 27.5f, 27.5f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(2f, -0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(-22.5f, -22.5f, -22.5f), new Location(-7.5f + 17.5f * -2f, -7.5f + 17.5f * -2f, -7.5f + 17.5f * -2f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(-2f, -0.75f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(-15f, -15f, -15f), new Location(5f, 5f, 5f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(2f, 1.5f * MathF.Sqrt(300f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(45f, 45f, 45f), new Location(25f, 25f, 25f)),
			new BoundedRay(new Location(0f, 0f, 0f), new Location(10f, 10f, 10f)).ScaledAroundPivotDistanceBy(-2f, 1.5f * MathF.Sqrt(300f)),
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
			xzLine.RotatedAroundPoint(rotation, xzLine.UnboundedLocationAtDistance(xzLine.Length * 0.75f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(3f, 0f, 0f), new Location(-3f, 0f, -6f)),
			xzLine.RotatedAroundPoint(rotation, xzLine.UnboundedLocationAtDistance(xzLine.Length * 0.25f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new BoundedRay(new Location(-3f, 0f, 3f), new Location(-9f, 0f, -3f)),
			xzLine.RotatedAroundPoint(rotation, (-3f, 0f, -3f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyRotateAroundPoints() {
		void AssertCombination(BoundedRay expectation, BoundedRay input, Location pivotPoint, Rotation rotation) {
			AssertToleranceEquals(expectation, input.RotatedAroundPoint(rotation, pivotPoint), TestTolerance);
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), input * (pivotPoint, rotation));
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), input * (rotation, pivotPoint));
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), (pivotPoint, rotation) * input);
			Assert.AreEqual(input.RotatedAroundPoint(rotation, pivotPoint), (rotation, pivotPoint) * input);
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


		Assert.AreEqual(0f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);
		Assert.AreEqual(10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);
		Assert.AreEqual(-10f, TestRay.UnboundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);

		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);
		Assert.AreEqual(TestRay.Length, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * 10f + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);
		Assert.AreEqual(0f, TestRay.BoundedDistanceAtPointClosestTo(new Location(1f, 2f, -3f) + TestRay.Direction * -10f + TestRay.Direction.AnyPerpendicular() * 10f), TestTolerance);
	}
}