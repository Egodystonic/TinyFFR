// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

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
		AssertToleranceEquals(new(3.6f, 6.8f, 0.7f), TestCuboid.GetCorner(DiagonalOrientation3D.LeftUpForward), TestTolerance);
		AssertToleranceEquals(new(3.6f, 6.8f, -0.7f), TestCuboid.GetCorner(DiagonalOrientation3D.LeftUpBackward), TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, 0.7f), TestCuboid.GetCorner(DiagonalOrientation3D.LeftDownForward), TestTolerance);
		AssertToleranceEquals(new(3.6f, -6.8f, -0.7f), TestCuboid.GetCorner(DiagonalOrientation3D.LeftDownBackward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, 0.7f), TestCuboid.GetCorner(DiagonalOrientation3D.RightUpForward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, 6.8f, -0.7f), TestCuboid.GetCorner(DiagonalOrientation3D.RightUpBackward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, 0.7f), TestCuboid.GetCorner(DiagonalOrientation3D.RightDownForward), TestTolerance);
		AssertToleranceEquals(new(-3.6f, -6.8f, -0.7f), TestCuboid.GetCorner(DiagonalOrientation3D.RightDownBackward), TestTolerance);

		Assert.Throws<ArgumentOutOfRangeException>(() => TestCuboid.GetCorner(DiagonalOrientation3D.None));
	}

	[Test]
	public void ShouldCorrectlyCalculateSurfacePlanes() {
		AssertToleranceEquals(new Plane(Direction.Left, new(3.6f, 0f, 0f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Left), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Right, new(-3.6f, 0f, 0f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Right), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Up, new(0f, 6.8f, 0f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Up), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Down, new(0f, -6.8f, 0f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Down), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Forward, new(0f, 0f, 0.7f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Forward), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Backward, new(0f, 0f, -0.7f)), TestCuboid.GetSideSurfacePlane(CardinalOrientation3D.Backward), TestTolerance);

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
		Assert.AreEqual(7.2f, TestCuboid.GetDimension(Axis.X), TestTolerance);
		Assert.AreEqual(13.6f, TestCuboid.GetDimension(Axis.Y), TestTolerance);
		Assert.AreEqual(1.4f, TestCuboid.GetDimension(Axis.Z), TestTolerance);
		Assert.Throws<ArgumentException>(() => _ = TestCuboid.GetDimension(Axis.None));

		Assert.AreEqual(0.5f * 7.2f, TestCuboid.GetHalfDimension(Axis.X), TestTolerance);
		Assert.AreEqual(0.5f * 13.6f, TestCuboid.GetHalfDimension(Axis.Y), TestTolerance);
		Assert.AreEqual(0.5f * 1.4f, TestCuboid.GetHalfDimension(Axis.Z), TestTolerance);
		Assert.Throws<ArgumentException>(() => _ = TestCuboid.GetHalfDimension(Axis.None));
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
		AssertToleranceEquals((0f, 0f, 0f), TestCuboid.ClosestPointTo((0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.ClosestPointTo((3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.ClosestPointTo((0f, 6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.ClosestPointTo((0f, 0f, 0.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.ClosestPointTo((-3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.ClosestPointTo((0f, -6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.ClosestPointTo((0f, 0f, -0.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.ClosestPointTo((4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.ClosestPointTo((0, 7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.ClosestPointTo((0, 0f, 1.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.ClosestPointTo((-4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.ClosestPointTo((0, -7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.ClosestPointTo((0, 0f, -1.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, -6.8f, 0.7f), TestCuboid.ClosestPointTo((4.6f, -7.8f, 0.7f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.ClosestPointTo((0f, 100f, -100f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestSurfacePointToLocations() {
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo((0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.ClosestPointOnSurfaceTo((3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.ClosestPointOnSurfaceTo((0f, 6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo((0f, 0f, 0.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.ClosestPointOnSurfaceTo((-3.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.ClosestPointOnSurfaceTo((0f, -6.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo((0f, 0f, -0.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, 0f, 0f), TestCuboid.ClosestPointOnSurfaceTo((4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, 0f), TestCuboid.ClosestPointOnSurfaceTo((0, 7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo((0, 0f, 1.7f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0f), TestCuboid.ClosestPointOnSurfaceTo((-4.6f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, -6.8f, 0f), TestCuboid.ClosestPointOnSurfaceTo((0, -7.8f, 0f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo((0, 0f, -1.7f)), TestTolerance);

		AssertToleranceEquals((3.6f, -6.8f, 0.7f), TestCuboid.ClosestPointOnSurfaceTo((4.6f, -7.8f, 0.7f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.ClosestPointOnSurfaceTo((0f, 100f, -100f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineLineIntersection() { // TODO
		Assert.AreEqual(
			new ConvexShapeLineIntersection((-3.6f, 0f, 0f), (3.6f, 0f, 0f)),
			TestCuboid.IntersectionWith(new Line(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f)))
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointToLines() { // TODO check this after fixing intersection
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
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfDimension(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfDimension(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, -TestCuboid.GetHalfDimension(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestCuboid.GetHalfDimension(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfDimension(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfDimension(Axis.Z));

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
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfDimension(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfDimension(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, -TestCuboid.GetHalfDimension(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, TestCuboid.GetHalfDimension(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfDimension(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfDimension(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, TestCuboid.GetHalfDimension(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, TestCuboid.GetHalfDimension(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, TestCuboid.GetHalfDimension(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, -TestCuboid.GetHalfDimension(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, -TestCuboid.GetHalfDimension(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, -TestCuboid.GetHalfDimension(Axis.Z));

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

		// BoundedLine
		AssertToleranceEquals((3.6f, -6.8f, 0f), TestCuboid.ClosestPointTo(new BoundedLine(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -0.7f), TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointTo(new BoundedLine(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-3.6f, 0f, 0.7f), TestCuboid.ClosestPointTo(new BoundedLine(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, -TestCuboid.GetHalfDimension(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, TestCuboid.GetHalfDimension(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, -TestCuboid.GetHalfDimension(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, TestCuboid.GetHalfDimension(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, -TestCuboid.GetHalfDimension(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, TestCuboid.GetHalfDimension(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, TestCuboid.GetHalfDimension(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, TestCuboid.GetHalfDimension(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, TestCuboid.GetHalfDimension(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, -TestCuboid.GetHalfDimension(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, -TestCuboid.GetHalfDimension(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, -TestCuboid.GetHalfDimension(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, TestCuboid.GetHalfDimension(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, TestCuboid.GetHalfDimension(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, TestCuboid.GetHalfDimension(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, -TestCuboid.GetHalfDimension(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, -TestCuboid.GetHalfDimension(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, -TestCuboid.GetHalfDimension(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, -0.5f);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, -0.5f);

		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Y);
		Assert.AreEqual(-TestCuboid.HalfWidth, TestCuboid.ClosestPointTo(new BoundedLine(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfWidth, TestCuboid.ClosestPointTo(new BoundedLine(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfHeight, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfHeight, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfDepth, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfDepth, TestCuboid.ClosestPointTo(new BoundedLine(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Z, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointOnLines() { // TODO this seems wrong
		// Line
		AssertToleranceEquals((94.2f, -6.8f, 0f), TestCuboid.ClosestPointOn(new Line(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -73.2f), TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-219.3f, 0f, 0.7f), TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((19.3f, 0f, 0.7f), TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(100f, 0f, 0f), new Direction(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, 100f), new Direction(0f, 0f, 1f))).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfDimension(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfDimension(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, -TestCuboid.GetHalfDimension(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 100f, 0f), new Direction(0f, 1f, 0f))).Y, TestCuboid.GetHalfDimension(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfDimension(Axis.Z) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Line(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfDimension(Axis.Z) + TestTolerance);

		// Ray
		AssertToleranceEquals((94.2f, -6.8f, 0f), TestCuboid.ClosestPointOn(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -73.2f), TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f))), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f))), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f))), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f))).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, -TestCuboid.GetHalfDimension(Axis.X) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f))).X, TestCuboid.GetHalfDimension(Axis.X) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, -TestCuboid.GetHalfDimension(Axis.Y) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f))).Y, TestCuboid.GetHalfDimension(Axis.Y) + TestTolerance);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, -TestCuboid.GetHalfDimension(Axis.Z) - TestTolerance);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f))).Z, TestCuboid.GetHalfDimension(Axis.Z) + TestTolerance);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).X, TestCuboid.GetHalfDimension(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Y, TestCuboid.GetHalfDimension(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f))).Z, TestCuboid.GetHalfDimension(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).X, -TestCuboid.GetHalfDimension(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Y, -TestCuboid.GetHalfDimension(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new Ray(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f))).Z, -TestCuboid.GetHalfDimension(Axis.Z));

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

		// BoundedLine
		Console.WriteLine(
			TestCuboid.ClosestPointOn(new Ray(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f))).DistanceFrom(new Location(1f, -100f, 0f))
		);
		Console.WriteLine(
			TestCuboid.ClosestPointOn(new BoundedLine(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)).DistanceFrom(new Location(1f, -100f, 0f))
		);
		AssertToleranceEquals((94.2f, -6.8f, 0f), TestCuboid.ClosestPointOn(new BoundedLine(new Location(1f, -100f, 0f), new Direction(1f, 1f, 0f) * 1000f)), TestTolerance);
		AssertToleranceEquals((0f, 6.8f, -73.2f), TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 100f, 20f), new Direction(0f, -1f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointOn(new BoundedLine(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, -1f) * 1000f)), TestTolerance);
		AssertToleranceEquals((-100f, 0f, 120f), TestCuboid.ClosestPointOn(new BoundedLine(new Location(-100f, 0f, 120f), new Direction(-1f, 0f, 1f) * 1000f)), TestTolerance);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, -100f, 0f), new Direction(0f, 1f, 0f) * 1000f)).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 100f), new Direction(0f, 0f, -1f) * 1000f)).Y);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, -TestCuboid.GetHalfDimension(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0f) * 1000f)).X, TestCuboid.GetHalfDimension(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, -TestCuboid.GetHalfDimension(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 100f, 0f), new Direction(0f, -1f, 0f) * 1000f)).Y, TestCuboid.GetHalfDimension(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, -TestCuboid.GetHalfDimension(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, -100f), new Direction(0f, 0f, 1f) * 1000f)).Z, TestCuboid.GetHalfDimension(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).X, TestCuboid.GetHalfDimension(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Y, TestCuboid.GetHalfDimension(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f)).Z, TestCuboid.GetHalfDimension(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).X, -TestCuboid.GetHalfDimension(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Y, -TestCuboid.GetHalfDimension(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f)).Z, -TestCuboid.GetHalfDimension(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).X, TestCuboid.GetHalfDimension(Axis.X));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Y, TestCuboid.GetHalfDimension(Axis.Y));
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 100f).Flipped).Z, TestCuboid.GetHalfDimension(Axis.Z));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).X, -TestCuboid.GetHalfDimension(Axis.X));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Y, -TestCuboid.GetHalfDimension(Axis.Y));
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 100f).Flipped).Z, -TestCuboid.GetHalfDimension(Axis.Z));

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f)).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f)).Z, -0.5f);

		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).X, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Y, 0.5f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(1f, 1f, 1f) * 0.5f).Flipped).Z, 0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).X, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Y, -0.5f);
		Assert.LessOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, 0f);
		Assert.GreaterOrEqual(TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 0f), new Direction(-1f, -1f, -1f) * 0.5f).Flipped).Z, -0.5f);

		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).Y);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Z);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).X);
		Assert.AreEqual(0f, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Y);
		Assert.AreEqual(-TestCuboid.HalfWidth, TestCuboid.ClosestPointOn(new BoundedLine(new Location(-100f, 0f, 0f), new Vect(-1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfWidth, TestCuboid.ClosestPointOn(new BoundedLine(new Location(100f, 0f, 0f), new Vect(1f, 0f, 0f))).X, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfHeight, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, -100f, 0f), new Vect(0f, -1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfHeight, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 100f, 0f), new Vect(0f, 1f, 0f))).Y, TestTolerance);
		Assert.AreEqual(-TestCuboid.HalfDepth, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, -100f), new Vect(0f, 0f, -1f))).Z, TestTolerance);
		Assert.AreEqual(TestCuboid.HalfDepth, TestCuboid.ClosestPointOn(new BoundedLine(new Location(0f, 0f, 100f), new Vect(0f, 0f, 1f))).Z, TestTolerance);
	}
}