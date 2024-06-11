// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.Metrics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class OriginCuboidTest {
	[Test]
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(
			new OriginCuboid(width: 7.2f * 3f, height: 13.6f * 3f, depth: 1.4f * 3f), 
			TestCuboid.ScaledBy(3f), 
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyCalculateCornerLocations() {
		AssertToleranceEquals(new(3.6f, 6.8f, 0.7f), TestCuboid.GetCornerLocation(DiagonalOrientation3D.LeftUpForward), TestTolerance);
		AssertToleranceEquals(new(3.6f, 6.8f, -0.7f), TestCuboid.GetCornerLocation(DiagonalOrientation3D.LeftUpBackward), TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, 0.7f), TestCuboid.GetCornerLocation(DiagonalOrientation3D.LeftDownForward), TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, -0.7f), TestCuboid.GetCornerLocation(DiagonalOrientation3D.LeftDownBackward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, 0.7f), TestCuboid.GetCornerLocation(DiagonalOrientation3D.RightUpForward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, -0.7f), TestCuboid.GetCornerLocation(DiagonalOrientation3D.RightUpBackward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, 0.7f), TestCuboid.GetCornerLocation(DiagonalOrientation3D.RightDownForward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, -0.7f), TestCuboid.GetCornerLocation(DiagonalOrientation3D.RightDownBackward), TestTolerance);

		Assert.Throws<ArgumentOutOfRangeException>(() => TestCuboid.GetCornerLocation(DiagonalOrientation3D.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateSurfacePlanes() {
		AssertToleranceEquals(new Plane(Direction.Left, (3.6f, 0f, 0f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Left), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Right, (-3.6f, 0f, 0f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Right), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Up, (0f, 6.8f, 0f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Up), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Down, (0f, -6.8f, 0f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Down), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Forward, (0f, 0f, 0.7f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Forward), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Backward, (0f, 0f, -0.7f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Backward), TestTolerance);

		Assert.Throws<ArgumentOutOfRangeException>(() => TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateEdges() {
		const float W = 0.5f * 7.2f;
		const float H = 0.5f * 13.6f;
		const float D = 0.5f * 1.4f;
		var cuboid = new OriginCuboid(W * 2f, H * 2f, D * 2f);

		void AssertOrientation(IntercardinalOrientation3D orientation, Location expectedLinePointA, Location expectedLinePointB) {
			Assert.IsTrue(cuboid.GetEdge(orientation).EqualsDisregardingDirection(new(expectedLinePointA, expectedLinePointB), TestTolerance));
		}

		AssertOrientation(IntercardinalOrientation3D.UpForward, new(W, H, D), new(-W, H, D));
		AssertOrientation(IntercardinalOrientation3D.UpBackward, new(W, H, -D), new(-W, H, -D));
		AssertOrientation(IntercardinalOrientation3D.DownForward, new(W, -H, D), new(-W, -H, D));
		AssertOrientation(IntercardinalOrientation3D.DownBackward, new(W, -H, -D), new(-W, -H, -D));

		AssertOrientation(IntercardinalOrientation3D.LeftForward, new(W, H, D), new(W, -H, D));
		AssertOrientation(IntercardinalOrientation3D.LeftBackward, new(W, H, -D), new(W, -H, -D));
		AssertOrientation(IntercardinalOrientation3D.RightForward, new(-W, H, D), new(-W, -H, D));
		AssertOrientation(IntercardinalOrientation3D.RightBackward, new(-W, H, -D), new(-W, -H, -D));

		AssertOrientation(IntercardinalOrientation3D.LeftUp, new(W, H, D), new(W, H, -D));
		AssertOrientation(IntercardinalOrientation3D.LeftDown, new(W, -H, D), new(W, -H, -D));
		AssertOrientation(IntercardinalOrientation3D.RightUp, new(-W, H, D), new(-W, H, -D));
		AssertOrientation(IntercardinalOrientation3D.RightDown, new(-W, -H, D), new(-W, -H, -D));

		Assert.Throws<ArgumentOutOfRangeException>(() => cuboid.GetEdge(IntercardinalOrientation3D.None));
	}

	[Test]
	public void ShouldCorrectlyReturnDimensionOfGivenAxis() {
		Assert.AreEqual(7.2f, TestCuboid.GetExtent(Axis.X), TestTolerance);
		Assert.AreEqual(13.6f, TestCuboid.GetExtent(Axis.Y), TestTolerance);
		Assert.AreEqual(1.4f, TestCuboid.GetExtent(Axis.Z), TestTolerance);
		Assert.Throws<ArgumentException>(() => _ = TestCuboid.GetExtent(Axis.None));

		Assert.AreEqual(0.5f * 7.2f, TestCuboid.GetHalfExtent(Axis.X), TestTolerance);
		Assert.AreEqual(0.5f * 13.6f, TestCuboid.GetHalfExtent(Axis.Y), TestTolerance);
		Assert.AreEqual(0.5f * 1.4f, TestCuboid.GetHalfExtent(Axis.Z), TestTolerance);
		Assert.Throws<ArgumentException>(() => _ = TestCuboid.GetHalfExtent(Axis.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateSideSurfaceAreas() {
		Assert.AreEqual(13.6f * 1.4f, TestCuboid.GetSideSurfaceArea(CardinalOrientation3D.Left), TestTolerance);
		Assert.AreEqual(13.6f * 1.4f, TestCuboid.GetSideSurfaceArea(CardinalOrientation3D.Right), TestTolerance);
		Assert.AreEqual(7.2f * 1.4f, TestCuboid.GetSideSurfaceArea(CardinalOrientation3D.Up), TestTolerance);
		Assert.AreEqual(7.2f * 1.4f, TestCuboid.GetSideSurfaceArea(CardinalOrientation3D.Down), TestTolerance);
		Assert.AreEqual(7.2f * 13.6f, TestCuboid.GetSideSurfaceArea(CardinalOrientation3D.Forward), TestTolerance);
		Assert.AreEqual(7.2f * 13.6f, TestCuboid.GetSideSurfaceArea(CardinalOrientation3D.Backward), TestTolerance);
		Assert.Throws<ArgumentException>(() => _ = TestCuboid.GetSideSurfaceArea(CardinalOrientation3D.None));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLocations() {
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((0f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((0f, 6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((0f, 0f, 0.7f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((-3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((0f, -6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.DistanceFrom((0f, 0f, -0.7f)));

		Assert.AreEqual(1f, TestCuboid.DistanceFrom((4.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.DistanceFrom((0, 7.8f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.DistanceFrom((0, 0f, 1.7f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.DistanceFrom((-4.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.DistanceFrom((0, -7.8f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.DistanceFrom((0, 0f, -1.7f)), TestTolerance);

		Assert.AreEqual(MathF.Sqrt(2f), TestCuboid.DistanceFrom((4.6f, -7.8f, 0.7f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLocations() {
		Assert.AreEqual(0.7f, TestCuboid.SurfaceDistanceFrom((0f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((0f, 6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((0f, 0f, 0.7f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((-3.6f, 0f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((0f, -6.8f, 0f)));
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom((0f, 0f, -0.7f)));

		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((4.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((0, 7.8f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((0, 0f, 1.7f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((-4.6f, 0f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((0, -7.8f, 0f)), TestTolerance);
		Assert.AreEqual(1f, TestCuboid.SurfaceDistanceFrom((0, 0f, -1.7f)), TestTolerance);

		Assert.AreEqual(MathF.Sqrt(2f), TestCuboid.SurfaceDistanceFrom((4.6f, -7.8f, 0.7f)), TestTolerance);

		Assert.AreEqual(1f, new OriginCuboid(20f, 40f, 60f).SurfaceDistanceFrom((9f, 9f, 9f)), TestTolerance);
		Assert.AreEqual(1f, new OriginCuboid(20f, 40f, 60f).SurfaceDistanceFrom((-9f, -9f, -9f)), TestTolerance);
		Assert.AreEqual(11f, new OriginCuboid(200f, 40f, 60f).SurfaceDistanceFrom((9f, 9f, 9f)), TestTolerance);
		Assert.AreEqual(11f, new OriginCuboid(200f, 40f, 60f).SurfaceDistanceFrom((-9f, -9f, -9f)), TestTolerance);
		Assert.AreEqual(21f, new OriginCuboid(200f, 400f, 60f).SurfaceDistanceFrom((9f, 9f, 9f)), TestTolerance);
		Assert.AreEqual(21f, new OriginCuboid(200f, 400f, 60f).SurfaceDistanceFrom((-9f, -9f, -9f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineContainmentOfLocations() {
		Assert.AreEqual(true, TestCuboid.Contains((0f, 0f, 0f)));
		Assert.AreEqual(true, TestCuboid.Contains((3.6f, 0f, 0f)));
		Assert.AreEqual(true, TestCuboid.Contains((0f, 6.8f, 0f)));
		Assert.AreEqual(true, TestCuboid.Contains((0f, 0f, 0.7f)));
		Assert.AreEqual(true, TestCuboid.Contains((-3.6f, 0f, 0f)));
		Assert.AreEqual(true, TestCuboid.Contains((0f, -6.8f, 0f)));
		Assert.AreEqual(true, TestCuboid.Contains((0f, 0f, -0.7f)));

		Assert.AreEqual(true, TestCuboid.Contains((-3.6f, -6.8f, -0.7f)));
		Assert.AreEqual(true, TestCuboid.Contains((3.6f, 6.8f, 0.7f)));

		Assert.AreEqual(false, TestCuboid.Contains((4.6f, 0f, 0f)));
		Assert.AreEqual(false, TestCuboid.Contains((0, 7.8f, 0f)));
		Assert.AreEqual(false, TestCuboid.Contains((0, 0f, 1.7f)));
		Assert.AreEqual(false, TestCuboid.Contains((-4.6f, 0f, 0f)));
		Assert.AreEqual(false, TestCuboid.Contains((0, -7.8f, 0f)));
		Assert.AreEqual(false, TestCuboid.Contains((0, 0f, -1.7f)));

		Assert.AreEqual(false, TestCuboid.Contains((4.6f, -7.8f, 0.7f)));
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToLocations() {
		AssertToleranceEquals((0f, 0f, 0f), TestCuboid.PointClosestTo((0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.PointClosestTo((3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.PointClosestTo((0f, 6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.PointClosestTo((0f, 0f, 0.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.PointClosestTo((-3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.PointClosestTo((0f, -6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.PointClosestTo((0f, 0f, -0.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.PointClosestTo((4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.PointClosestTo((0, 7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.PointClosestTo((0, 0f, 1.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.PointClosestTo((-4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.PointClosestTo((0, -7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.PointClosestTo((0, 0f, -1.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, -6.8f, 0.7f), TestCuboid.PointClosestTo((4.6f, -7.8f, 0.7f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.PointClosestTo((0f, 100f, -100f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestSurfacePointToLocations() {
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo((0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.SurfacePointClosestTo((3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.SurfacePointClosestTo((0f, 6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo((0f, 0f, 0.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.SurfacePointClosestTo((-3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.SurfacePointClosestTo((0f, -6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.SurfacePointClosestTo((0f, 0f, -0.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.SurfacePointClosestTo((4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.SurfacePointClosestTo((0, 7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.SurfacePointClosestTo((0, 0f, 1.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.SurfacePointClosestTo((-4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.SurfacePointClosestTo((0, -7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.SurfacePointClosestTo((0, 0f, -1.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, -6.8f, 0.7f), TestCuboid.SurfacePointClosestTo((4.6f, -7.8f, 0.7f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.SurfacePointClosestTo((0f, 100f, -100f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineLineIntersection() {
		ConvexShapeLineIntersection? intersection;

		// Line
		intersection = TestCuboid.IntersectionWith(new Line(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		
		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f)));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f)));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f)));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f)));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.Second, TestTolerance);


		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f)));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f)));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f)));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f)));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.Second, TestTolerance);


		intersection = TestCuboid.IntersectionWith(new Line(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f)));
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f)));
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Line(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f)));
		Assert.IsNull(intersection);




		// Ray
		intersection = TestCuboid.IntersectionWith(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f)));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f)));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f)));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f)));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f)));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f)));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f)));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(new Ray(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f)));
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f)));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(new Ray(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f)));
		Assert.IsNull(intersection);



		// BoundedRay
		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f));
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f));
		AssertToleranceEquals((-3.6f, 0f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 10f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 10f));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f));
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f));
		AssertToleranceEquals((0f, -6.8f, 0f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 10f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 10f));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f));
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f));
		AssertToleranceEquals((0f, 0f, -0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 10f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 10f));
		Assert.IsNull(intersection);


		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f) * 1000f));
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), intersection!.Value.First, TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), intersection!.Value.Second, TestTolerance);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f) * 10f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f));
		Assert.IsNull(intersection);

		intersection = TestCuboid.IntersectionWith(BoundedRay.FromStartPointAndVect(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f) * 1000f));
		Assert.IsNull(intersection);
	}

	[Test]
	public void ShouldCorrectlyTestForLineIntersection() {
		// Line
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Line(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Line(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Line(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Line(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f))));


		// Ray
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))));
		Assert.True(TestCuboid.IsIntersectedBy(new Ray(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f))));
		Assert.False(TestCuboid.IsIntersectedBy(new Ray(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f))));


		// BoundedRay
		Assert.True(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)));
		Assert.True(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 10f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 10f)));
		Assert.True(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)));
		Assert.True(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 10f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 10f)));
		Assert.True(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)));
		Assert.True(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 10f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 10f)));
		Assert.True(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 100f), new Direction(-1f, -1f, -1f) * 10f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(100f, 100f, 100f), new Direction(1f, 1f, 1f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(100f, 20f, 0f), new Direction(-1f, 0f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(20f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)));
		Assert.False(TestCuboid.IsIntersectedBy(BoundedRay.FromStartPointAndVect(new Location(0f, 20f, 100f), new Direction(0f, 0f, -1f) * 1000f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointToLines() {
		// Line
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.ClosestPointTo(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.ClosestPointTo(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointTo(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0.7f), TestCuboid.ClosestPointTo(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z));

		// Ray
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.ClosestPointTo(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.ClosestPointTo(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointTo(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointTo(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z));

		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(-TestCuboid.HalfWidth, TestCuboid.ClosestPointTo(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfWidth, TestCuboid.ClosestPointTo(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfHeight, TestCuboid.ClosestPointTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfHeight, TestCuboid.ClosestPointTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfDepth, TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfDepth, TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Z, TestTolerance);

		// BoundedRay
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, -TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, TestCuboid.GetHalfExtent(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, -TestCuboid.GetHalfExtent(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, -TestCuboid.GetHalfExtent(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, -0.5f);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, -0.5f);

		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Y);
		Assert.AreEqual(-TestCuboid.HalfWidth, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfWidth, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfHeight, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfHeight, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfDepth, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfDepth, TestCuboid.ClosestPointTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Z, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointOnLines() {
		// Line
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointOn(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-111.45f, 0f, 108.55f), TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((11.45f, 0, 8.55f), TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfExtent(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfExtent(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, -TestCuboid.GetHalfExtent(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestCuboid.GetHalfExtent(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z) + TestTolerance);

		// Ray
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointOn(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-111.45f, 0, 108.55f), TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfExtent(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfExtent(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, -TestCuboid.GetHalfExtent(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, TestCuboid.GetHalfExtent(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z) + TestTolerance);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, TestCuboid.GetHalfExtent(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, TestCuboid.GetHalfExtent(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, TestCuboid.GetHalfExtent(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, -TestCuboid.GetHalfExtent(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, -TestCuboid.GetHalfExtent(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, -TestCuboid.GetHalfExtent(Axis.Z));

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Z, TestTolerance);

		// BoundedRay
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-111.45f, 0, 108.55f), TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, -TestCuboid.GetHalfExtent(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, TestCuboid.GetHalfExtent(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, -TestCuboid.GetHalfExtent(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, TestCuboid.GetHalfExtent(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, -TestCuboid.GetHalfExtent(Axis.Z) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, TestCuboid.GetHalfExtent(Axis.Z) + TestTolerance);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, TestCuboid.GetHalfExtent(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, TestCuboid.GetHalfExtent(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, TestCuboid.GetHalfExtent(Axis.Z) + TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, -TestCuboid.GetHalfExtent(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, -TestCuboid.GetHalfExtent(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, -TestCuboid.GetHalfExtent(Axis.Z) - TestTolerance);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, TestCuboid.GetHalfExtent(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, TestCuboid.GetHalfExtent(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, TestCuboid.GetHalfExtent(Axis.Z) + TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, -TestCuboid.GetHalfExtent(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, -TestCuboid.GetHalfExtent(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, -TestCuboid.GetHalfExtent(Axis.Z) - TestTolerance);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, -0.5f);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, -0.5f);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Y);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Z, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLines() {
		// Line
		Assert.AreEqual(64.0638f, TestCuboid.DistanceFrom(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.DistanceFrom(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.DistanceFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(11.1016f, TestCuboid.DistanceFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Line(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// Ray
		Assert.AreEqual(64.0638f, TestCuboid.DistanceFrom(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.DistanceFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(153.3801f, TestCuboid.DistanceFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))), TestTolerance);

		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(new Ray(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// BoundedRay
		Assert.AreEqual(64.0638f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(153.3801f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f).Flipped), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped), TestTolerance);

		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f)).Flipped));

		Assert.AreEqual(10f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(13.6f, 0f, 0f), Direction.Up * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 16.8f, 0f), Direction.Forward * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 10.7f), Direction.Left * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-13.6f, 0f, 0f), Direction.Down * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, -16.8f, 0f), Direction.Backward * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.DistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -10.7f), Direction.Right * 1000f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointToLines() {
		// Line
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.ClosestPointOnSurfaceTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z), TestTolerance);

		// Ray
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z), TestTolerance);

		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(-TestCuboid.HalfWidth, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfWidth, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfHeight, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfHeight, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfDepth, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfDepth, TestCuboid.ClosestPointOnSurfaceTo(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Z, TestTolerance);

		// BoundedRay
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z), TestTolerance);

		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 1000f).Flipped), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 1000f).Flipped), TestTolerance);

		AssertToleranceEquals((0.2887f, 0.2887f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)), TestTolerance);
		AssertToleranceEquals((-0.2887f, -0.2887f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)), TestTolerance);
		AssertToleranceEquals((0.2887f, 0.2887f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped), TestTolerance);
		AssertToleranceEquals((-0.2887f, -0.2887f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Y);
		Assert.AreEqual(-TestCuboid.HalfWidth, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfWidth, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfHeight, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfHeight, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfDepth, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfDepth, TestCuboid.ClosestPointOnSurfaceTo(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Z, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointOnLines() {
		// Line
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-111.45f, 0f, 108.55f), TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((11.45f, 0, 8.55f), TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z), TestTolerance);

		// Ray
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-111.45f, 0, 108.55f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z), TestTolerance);

		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Z, TestTolerance);

		// BoundedRay
		AssertToleranceEquals((48.9f, -52.1f, 0f), TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0f, 43.05f, -36.95f), TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-111.45f, 0, 108.55f), TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)).Y);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.X), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Y), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y), TestTolerance);
		Assert.AreEqual(TestCuboid.GetHalfExtent(Axis.Z), MathF.Abs(TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z), TestTolerance);

		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0.7f, 0.7f, 0.7f), TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 1000f).Flipped), TestTolerance);
		AssertToleranceEquals((-0.7f, -0.7f, -0.7f), TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 1000f).Flipped), TestTolerance);

		AssertToleranceEquals(Location.Origin + new Direction(1f, 1f, 1f) * 0.5f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)), TestTolerance);
		AssertToleranceEquals(Location.Origin + new Direction(1f, 1f, 1f) * -0.5f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)), TestTolerance);
		AssertToleranceEquals(Location.Origin + new Direction(1f, 1f, 1f) * 0.5f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped), TestTolerance);
		AssertToleranceEquals(Location.Origin + new Direction(1f, 1f, 1f) * -0.5f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Y);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-100f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(100f, TestCuboid.ClosestPointToSurfaceOn(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Z, TestTolerance);

		var longCuboid = new OriginCuboid(1000f, 10f, 1f);
		var line = BoundedRay.FromStartPointAndVect(new Location(-485f, -4f, 0f), new Vect(980f, -0.9f, 0f));
		AssertToleranceEquals(line.EndPoint, longCuboid.ClosestPointToSurfaceOn(line), TestTolerance);
		AssertToleranceEquals(line.EndPoint, longCuboid.ClosestPointToSurfaceOn(line.Flipped), TestTolerance);
		line = BoundedRay.FromStartPointAndVect(new Location(-495f, -4f, 0f), new Vect(980f, -0.9f, 0f));
		AssertToleranceEquals(line.EndPoint, longCuboid.ClosestPointToSurfaceOn(line), TestTolerance);
		AssertToleranceEquals(line.EndPoint, longCuboid.ClosestPointToSurfaceOn(line.Flipped), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLines() {
		// Line
		Assert.AreEqual(64.0638f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(11.1016f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Line(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// Ray
		Assert.AreEqual(64.0638f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(153.3801f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))), TestTolerance);

		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(-100f, 0f, 0f), new Direction(-1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, -100f, 0f), new Direction(0f, -1f, 0f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, -1f))), TestTolerance);
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(13.6f, 0f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 16.8f, 0f), Direction.Forward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, 10.7f), Direction.Left)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(-13.6f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, -16.8f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(new Ray(new Location(0f, 0f, -10.7f), Direction.Right)), TestTolerance);

		// BoundedRay
		Assert.AreEqual(64.0638f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(51.2652f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(152.5229f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		Assert.AreEqual(153.3801f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f).Flipped), TestTolerance);
		Assert.AreEqual(0f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f).Flipped), TestTolerance);

		Assert.AreEqual(0.7f - 0.2887f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)), TestTolerance);
		Assert.AreEqual(0.7f - 0.2887f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)), TestTolerance);

		Assert.AreEqual(0.7f - 0.2887f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped), TestTolerance);
		Assert.AreEqual(0.7f - 0.2887f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped), TestTolerance);

		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfWidth, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfHeight, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f)).Flipped));
		Assert.AreEqual(100f - TestCuboid.HalfDepth, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f)).Flipped));

		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(13.6f, 0f, 0f), Direction.Up * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 16.8f, 0f), Direction.Forward * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, 10.7f), Direction.Left * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(-13.6f, 0f, 0f), Direction.Down * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, -16.8f, 0f), Direction.Backward * 1000f)), TestTolerance);
		Assert.AreEqual(10f, TestCuboid.SurfaceDistanceFrom(BoundedRay.FromStartPointAndVect(new Location(0f, 0f, -10.7f), Direction.Right * 1000f)), TestTolerance);

		var longCuboid = new OriginCuboid(1000f, 10f, 1f);
		var line = BoundedRay.FromStartPointAndVect(new Location(-485f, -4f, 0f), new Vect(980f, -0.9f, 0f));
		Assert.AreEqual(0.1f, longCuboid.SurfaceDistanceFrom(line), TestTolerance);
		Assert.AreEqual(0.1f, longCuboid.SurfaceDistanceFrom(line.Flipped), TestTolerance);
		line = BoundedRay.FromStartPointAndVect(new Location(-495f, -4f, 0f), new Vect(980f, -0.9f, 0f));
		Assert.AreEqual(0.1f, longCuboid.SurfaceDistanceFrom(line), TestTolerance);
		Assert.AreEqual(0.1f, longCuboid.SurfaceDistanceFrom(line.Flipped), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointToPlane() {
		foreach (var orientation in OrientationUtils.AllDiagonals) {
			AssertToleranceEquals(
				TestCuboid.GetCornerLocation(orientation),
				TestCuboid.PointClosestTo(new Plane(orientation.ToDirection(), 1000f)),
				TestTolerance
			);
			AssertToleranceEquals(
				TestCuboid.GetCornerLocation(orientation),
				TestCuboid.ClosestPointOnSurfaceTo(new Plane(orientation.ToDirection(), 1000f)),
				TestTolerance
			);
		}

		foreach (var orientation in OrientationUtils.AllIntercardinals) {
			var edge = TestCuboid.GetEdge(orientation);
			Assert.AreEqual(
				0f,
				edge.DistanceFrom(TestCuboid.PointClosestTo(new Plane(orientation.ToDirection(), 1000f))),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				edge.DistanceFrom(TestCuboid.ClosestPointOnSurfaceTo(new Plane(orientation.ToDirection(), 1000f))),
				TestTolerance
			);
		}

		foreach (var orientation in OrientationUtils.AllCardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);

			foreach (var axis in OrientationUtils.AllAxes) {
				if (axis == orientation.GetAxis()) {
					Assert.AreEqual(
						TestCuboid.GetHalfExtent(axis) * orientation.GetAxisSign(),
						TestCuboid.PointClosestTo(plane)[axis],
						TestTolerance
					);
					Assert.AreEqual(
						TestCuboid.GetHalfExtent(axis) * orientation.GetAxisSign(),
						TestCuboid.ClosestPointOnSurfaceTo(plane)[axis],
						TestTolerance
					);
				}
				else {
					Assert.GreaterOrEqual(
						TestCuboid.PointClosestTo(plane)[axis],
						-TestCuboid.GetHalfExtent(axis) - TestTolerance
					);
					Assert.LessOrEqual(
						TestCuboid.PointClosestTo(plane)[axis],
						TestCuboid.GetHalfExtent(axis) + TestTolerance
					);
					Assert.GreaterOrEqual(
						TestCuboid.ClosestPointOnSurfaceTo(plane)[axis],
						-TestCuboid.GetHalfExtent(axis) - TestTolerance
					);
					Assert.LessOrEqual(
						TestCuboid.ClosestPointOnSurfaceTo(plane)[axis],
						TestCuboid.GetHalfExtent(axis) + TestTolerance
					);
				}
			}
		}

		foreach (var orientation in OrientationUtils.All3DOrientations) {
			var plane = new Plane(orientation.ToDirection(), 0f);

			Assert.AreEqual(
				0f,
				TestCuboid.DistanceFrom(TestCuboid.PointClosestTo(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				TestCuboid.DistanceFrom(TestCuboid.ClosestPointOnSurfaceTo(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				plane.DistanceFrom(TestCuboid.PointClosestTo(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				plane.DistanceFrom(TestCuboid.ClosestPointOnSurfaceTo(plane)),
				TestTolerance
			);
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointOnPlane() {
		foreach (var orientation in OrientationUtils.AllDiagonals) {
			var corner = TestCuboid.GetCornerLocation(orientation);
			var plane = new Plane(orientation.ToDirection(), 1000f);
			AssertToleranceEquals(
				orientation.ToDirection() * plane.DistanceFrom(corner) + corner,
				TestCuboid.ClosestPointOn(plane),
				TestTolerance
			);
			AssertToleranceEquals(
				orientation.ToDirection() * plane.DistanceFrom(corner) + corner,
				TestCuboid.ClosestPointToSurfaceOn(plane),
				TestTolerance
			);
		}

		foreach (var orientation in OrientationUtils.AllIntercardinals) {
			var edge = TestCuboid.GetEdge(orientation);
			var plane = new Plane(orientation.ToDirection(), 1000f);
			var projectedEdge = edge.ProjectedOnTo(plane);
			Assert.AreEqual(
				0f,
				projectedEdge.DistanceFrom(TestCuboid.ClosestPointOn(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				projectedEdge.DistanceFrom(TestCuboid.ClosestPointToSurfaceOn(plane)),
				TestTolerance
			);
		}

		foreach (var orientation in OrientationUtils.AllCardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);

			foreach (var axis in OrientationUtils.AllAxes) {
				if (axis == orientation.GetAxis()) {
					Assert.AreEqual(
						(orientation.ToDirection() * 1000f)[axis],
						TestCuboid.ClosestPointOn(plane)[axis],
						TestTolerance
					);
					Assert.AreEqual(
						(orientation.ToDirection() * 1000f)[axis],
						TestCuboid.ClosestPointToSurfaceOn(plane)[axis],
						TestTolerance
					);
				}
				else {
					Assert.GreaterOrEqual(
						TestCuboid.ClosestPointOn(plane)[axis],
						-TestCuboid.GetHalfExtent(axis) - TestTolerance
					);
					Assert.LessOrEqual(
						TestCuboid.ClosestPointOn(plane)[axis],
						TestCuboid.GetHalfExtent(axis) + TestTolerance
					);
					Assert.GreaterOrEqual(
						TestCuboid.ClosestPointToSurfaceOn(plane)[axis],
						-TestCuboid.GetHalfExtent(axis) - TestTolerance
					);
					Assert.LessOrEqual(
						TestCuboid.ClosestPointToSurfaceOn(plane)[axis],
						TestCuboid.GetHalfExtent(axis) + TestTolerance
					);
				}
			}
		}

		foreach (var orientation in OrientationUtils.All3DOrientations) {
			var plane = new Plane(orientation.ToDirection(), 0f);

			Assert.AreEqual(
				0f,
				TestCuboid.DistanceFrom(TestCuboid.ClosestPointOn(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				TestCuboid.DistanceFrom(TestCuboid.ClosestPointToSurfaceOn(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				plane.DistanceFrom(TestCuboid.ClosestPointOn(plane)),
				TestTolerance
			);
			Assert.AreEqual(
				0f,
				plane.DistanceFrom(TestCuboid.ClosestPointToSurfaceOn(plane)),
				TestTolerance
			);
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromPlanes() {
		void AssertDistance(float expectedSignedDistance, Plane plane) {
			Assert.AreEqual(
				expectedSignedDistance,
				TestCuboid.SignedDistanceFrom(plane),
				TestTolerance
			);
			Assert.AreEqual(
				expectedSignedDistance,
				TestCuboid.SignedSurfaceDistanceFrom(plane),
				TestTolerance
			);
			Assert.AreEqual(
				MathF.Abs(expectedSignedDistance),
				TestCuboid.DistanceFrom(plane),
				TestTolerance
			);
			Assert.AreEqual(
				MathF.Abs(expectedSignedDistance),
				TestCuboid.SurfaceDistanceFrom(plane),
				TestTolerance
			);
		}

		foreach (var orientation in OrientationUtils.AllDiagonals) {
			var corner = TestCuboid.GetCornerLocation(orientation);
			var plane = new Plane(orientation.ToDirection(), 1000f);
			AssertDistance(-plane.DistanceFrom(corner), plane);
			AssertDistance(plane.DistanceFrom(corner), plane.Flipped);
		}

		foreach (var orientation in OrientationUtils.AllIntercardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);
			var edge = TestCuboid.GetEdge(orientation);
			AssertDistance(-plane.DistanceFrom(edge), plane);
			AssertDistance(plane.DistanceFrom(edge), plane.Flipped);
		}

		foreach (var orientation in OrientationUtils.AllCardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);
			var expectedSignedDistance = -(1000f - TestCuboid.GetHalfExtent(orientation.GetAxis()));
			AssertDistance(expectedSignedDistance, plane);
			AssertDistance(-expectedSignedDistance, plane.Flipped);
		}

		foreach (var orientation in OrientationUtils.All3DOrientations) {
			var plane = new Plane(orientation.ToDirection(), 0f);
			AssertDistance(0f, plane);
			AssertDistance(0f, plane.Flipped);
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineRelationshipToPlane() {
		foreach (var orientation in OrientationUtils.AllDiagonals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestCuboid.RelationshipTo(plane));
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestCuboid.RelationshipTo(plane.Flipped));
		}

		foreach (var orientation in OrientationUtils.AllIntercardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestCuboid.RelationshipTo(plane));
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestCuboid.RelationshipTo(plane.Flipped));
		}

		foreach (var orientation in OrientationUtils.AllCardinals) {
			var plane = new Plane(orientation.ToDirection(), 1000f);
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestCuboid.RelationshipTo(plane));
			Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestCuboid.RelationshipTo(plane.Flipped));
		}

		foreach (var orientation in OrientationUtils.All3DOrientations) {
			var plane = new Plane(orientation.ToDirection(), 0f);
			Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestCuboid.RelationshipTo(plane));
			Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestCuboid.RelationshipTo(plane.Flipped));
		}
	}
}