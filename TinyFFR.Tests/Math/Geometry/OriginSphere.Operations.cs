// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class OriginSphereTest {
	[Test]
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(new OriginSphere(7.4f * 3f), new OriginSphere(7.4f).ScaledBy(3f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineCircleRadiusAtDistanceFromCentre() {
		// https://github.com/Egodystonic/EscapeLizards/blob/master/LosgapTests/Core/Math/Sphere.cs#L94

		Assert.AreEqual(7.4f, TestSphere.GetCircleRadiusAtDistanceFromCenter(0f));
		Assert.AreEqual(0f, TestSphere.GetCircleRadiusAtDistanceFromCenter(7.4f));
		Assert.AreEqual(0f, TestSphere.GetCircleRadiusAtDistanceFromCenter(10f));
		Assert.AreEqual(8.66025448f, new OriginSphere(10f).GetCircleRadiusAtDistanceFromCenter(5f), TestTolerance);
		Assert.AreEqual(0.1410673f, new OriginSphere(1f).GetCircleRadiusAtDistanceFromCenter(0.99f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLocations() {
		Assert.AreEqual(0f, TestSphere.DistanceFrom((0f, 0f, 0f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom((0f, 1f, 0f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom((0f, -1f, 0f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom((0f, 7.4f, 0f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom((0f, -7.4f, 0f)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom((0f, 17.4f, 0f)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom((0f, -17.4f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLocations() {
		Assert.AreEqual(7.4f, TestSphere.SurfaceDistanceFrom((0f, 0f, 0f)));
		Assert.AreEqual(6.4f, TestSphere.SurfaceDistanceFrom((0f, 1f, 0f)));
		Assert.AreEqual(6.4f, TestSphere.SurfaceDistanceFrom((0f, -1f, 0f)));
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom((0f, 7.4f, 0f)));
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom((0f, -7.4f, 0f)));
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom((0f, 17.4f, 0f)));
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom((0f, -17.4f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLines() {
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Line((0f, 0f, 0f), Direction.Backward)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Line((0f, 7.4f, 0f), Direction.Backward)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Line((0f, -7.4f, 0f), Direction.Backward)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(new Line((0f, 17.4f, 0f), Direction.Backward)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(new Line((0f, -17.4f, 0f), Direction.Backward)));

		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, 0f, 0f), Direction.Down)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, 7.4f, 0f), Direction.Down)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, -7.4f, 0f), Direction.Up)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, 17.4f, 0f), Direction.Down)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, -17.4f, 0f), Direction.Up)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, 7.4f, 0f), Direction.Up)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, -7.4f, 0f), Direction.Down)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(new Ray((0f, 17.4f, 0f), Direction.Up)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(new Ray((0f, -17.4f, 0f), Direction.Down)));

		Assert.AreEqual(0f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, 0f, 0f), Direction.Down * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Down * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Up * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Down * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Up * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Up * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Down * 100f)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Up * 100f)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Down * 100f)));
		Assert.AreEqual(11f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, 27.4f, 0f), Direction.Down * 9f)));
		Assert.AreEqual(9f, TestSphere.DistanceFrom(BoundedLine.FromStartPointAndVect((0f, 27.4f, 0f), Direction.Down * 11f)));

		Assert.AreEqual(0f, TestSphere.DistanceFrom(new BoundedLine(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))));
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLines() {
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Line((0f, 0f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Line((0f, 7.4f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Line((0f, -7.4f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(new Line((0f, 17.4f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(new Line((0f, -17.4f, 0f), Direction.Backward)), TestTolerance);

		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, 7.4f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, -7.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, 17.4f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, -17.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, 7.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, -7.4f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(new Ray((0f, 17.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(new Ray((0f, -17.4f, 0f), Direction.Down)), TestTolerance);

		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, 0f, 0f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(11f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, 27.4f, 0f), Direction.Down * 9f)), TestTolerance);
		Assert.AreEqual(9f, TestSphere.SurfaceDistanceFrom(BoundedLine.FromStartPointAndVect((0f, 27.4f, 0f), Direction.Down * 11f)), TestTolerance);

		Assert.AreEqual(6.4f, TestSphere.SurfaceDistanceFrom(new BoundedLine(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineWhetherLocationIsContained() {
		Assert.AreEqual(true, TestSphere.Contains(new Location(0f, 0f, 0f)));
		Assert.AreEqual(true, TestSphere.Contains(new Location(0f, 7.4f, 0f)));
		Assert.AreEqual(true, TestSphere.Contains(new Location(0f, -7.4f, 0f)));
		Assert.AreEqual(false, TestSphere.Contains(new Location(0f, 7.5f, 0f)));
		Assert.AreEqual(false, TestSphere.Contains(new Location(0f, -7.5f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToGivenLocation() {
		AssertToleranceEquals(new Location(0f, 0f, 0f), TestSphere.ClosestPointTo(new Location(0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, 7.4f, 0f), TestSphere.ClosestPointTo(new Location(0f, 7.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -7.4f, 0f), TestSphere.ClosestPointTo(new Location(0f, -7.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, 7.4f, 0f), TestSphere.ClosestPointTo(new Location(0f, 17.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -7.4f, 0f), TestSphere.ClosestPointTo(new Location(0f, -17.4f, 0f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnSurfaceToGivenLocation() {
		AssertToleranceEquals(new Location(0f, 0f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Location(0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Location(0f, 7.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Location(0f, -7.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Location(0f, 17.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Location(0f, -17.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Location(0f, 2f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Location(0f, -2f, 0f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToLine() {
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(new Line((0f, 0f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 2f, 0f), TestSphere.ClosestPointTo(new Line((0f, 2f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -2f, 0f), TestSphere.ClosestPointTo(new Line((0f, -2f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointTo(new Line((0f, 7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointTo(new Line((0f, -7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointTo(new Line((0f, 17.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointTo(new Line((0f, -17.4f, 0f), Direction.Backward)), TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(new Ray((0f, 0f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(new Ray((0f, 7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(new Ray((0f, -7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(new Ray((0f, 17.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(new Ray((0f, -17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, 2f, 0f), TestSphere.ClosestPointTo(new Ray((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -2f, 0f), TestSphere.ClosestPointTo(new Ray((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointTo(new Ray((0f, 7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointTo(new Ray((0f, -7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointTo(new Ray((0f, 17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointTo(new Ray((0f, -17.4f, 0f), Direction.Down)), TestTolerance);
		
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(BoundedLine.FromStartPointAndVect((0f, 0f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointTo(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointTo(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointTo(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointTo(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointTo(BoundedLine.FromStartPointAndVect((0f, 27.4f, 0f), Direction.Down * 9f)), TestTolerance);
		
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(new BoundedLine(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-2f, 0f, 0f), TestSphere.ClosestPointTo(new BoundedLine(new Location(-5f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-2f, 0f, 0f), TestSphere.ClosestPointTo(new BoundedLine(new Location(-15f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnLine() {
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Line((0f, 0f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 2f, 0f), TestSphere.ClosestPointOn(new Line((0f, 2f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -2f, 0f), TestSphere.ClosestPointOn(new Line((0f, -2f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOn(new Line((0f, 7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOn(new Line((0f, -7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointOn(new Line((0f, 17.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointOn(new Line((0f, -17.4f, 0f), Direction.Backward)), TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 0f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Ray((0f, -7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 17.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Ray((0f, -17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, 2f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -2f, 0f), TestSphere.ClosestPointOn(new Ray((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOn(new Ray((0f, -7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointOn(new Ray((0f, -17.4f, 0f), Direction.Down)), TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(BoundedLine.FromStartPointAndVect((0f, 0f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOn(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOn(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointOn(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointOn(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 18.4f, 0f), TestSphere.ClosestPointOn(BoundedLine.FromStartPointAndVect((0f, 27.4f, 0f), Direction.Down * 9f)), TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedLine(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-2f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedLine(new Location(-5f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-2f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedLine(new Location(-15f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnSurfaceToLine() {
		Assert.AreEqual(7.4f, TestSphere.ClosestPointOnSurfaceTo(new Line((0f, 0f, 0f), Direction.Backward)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Line((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Line((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Line((0f, 7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Line((0f, -7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Line((0f, 17.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Line((0f, -17.4f, 0f), Direction.Backward)), TestTolerance);

		Assert.AreEqual(7.4f, TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, 0f, 0f), Direction.Down)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, 6.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, -6.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, 17.4f, 0f), Direction.Down)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, -17.4f, 0f), Direction.Up)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, 7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, -7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, 17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(new Ray((0f, -17.4f, 0f), Direction.Down)), TestTolerance);

		Assert.AreEqual(7.4f, TestSphere.ClosestPointOnSurfaceTo(BoundedLine.FromStartPointAndVect((0f, 0f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointOnSurfaceTo(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointOnSurfaceTo(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Up * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointOnSurfaceTo(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointOnSurfaceTo(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Up * 100f)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOnSurfaceTo(BoundedLine.FromStartPointAndVect((0f, 27.4f, 0f), Direction.Down * 9f)), TestTolerance);

		Assert.AreEqual(7.4f, TestSphere.ClosestPointOnSurfaceTo(new BoundedLine(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((-7.4f, 0f, 0f), TestSphere.ClosestPointOnSurfaceTo(new BoundedLine(new Location(-5f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-7.4f, 0f, 0f), TestSphere.ClosestPointOnSurfaceTo(new BoundedLine(new Location(-15f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnLineToSurface() {
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new Line((0f, 0f, 0f), Direction.Backward)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, 7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, -7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, 17.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, -17.4f, 0f), Direction.Backward)), TestTolerance);

		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 0f, 0f), Direction.Down)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 6.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, -6.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 17.4f, 0f), Direction.Down)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new Ray((0f, -17.4f, 0f), Direction.Up)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, -7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, -17.4f, 0f), Direction.Down)), TestTolerance);

		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(BoundedLine.FromStartPointAndVect((0f, 0f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Up * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Up * 100f)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(BoundedLine.FromStartPointAndVect((0f, 7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(BoundedLine.FromStartPointAndVect((0f, -7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(BoundedLine.FromStartPointAndVect((0f, 17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(BoundedLine.FromStartPointAndVect((0f, -17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 18.4f, 0f), TestSphere.ClosestPointToSurfaceOn(BoundedLine.FromStartPointAndVect((0f, 27.4f, 0f), Direction.Down * 9f)), TestTolerance);

		Assert.AreEqual(1f, TestSphere.ClosestPointToSurfaceOn(new BoundedLine(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((-5f, 0f, 0f), TestSphere.ClosestPointToSurfaceOn(new BoundedLine(new Location(-5f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-7.4f, 0f, 0f), TestSphere.ClosestPointToSurfaceOn(new BoundedLine(new Location(-15f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindLineIntersections() {
		ConvexShapeLineIntersection intersection;
		
		// Line
		Assert.AreEqual(null, TestSphere.IntersectionWith(new Line(new Location(0f, 10f, 0f), Direction.Right)));

		intersection = TestSphere.IntersectionWith(new Line(new Location(0f, 6f, 0f), Direction.Right))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(-intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestSphere.IntersectionWith(new Line(new Location(0f, 7.4f, 0f), Direction.Right))!.Value;
		AssertToleranceEquals((0f, 7.4f, 0f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);


		// Ray
		Assert.AreEqual(null, TestSphere.IntersectionWith(new Ray(new Location(0f, 10f, 0f), Direction.Right)));
		Assert.AreEqual(null, TestSphere.IntersectionWith(new Ray(new Location(10f, 10f, 0f), Direction.Right)));
		Assert.AreEqual(null, TestSphere.IntersectionWith(new Ray(new Location(-10f, 0f, 0f), Direction.Right)));

		intersection = TestSphere.IntersectionWith(new Ray(new Location(10f, 6f, 0f), Direction.Right))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(-intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestSphere.IntersectionWith(new Ray(new Location(0f, 6f, 0f), Direction.Right))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestSphere.IntersectionWith(new Ray(new Location(0f, 7.4f, 0f), Direction.Right))!.Value;
		AssertToleranceEquals((0f, 7.4f, 0f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);

		
		// BoundedLine
		Assert.AreEqual(null, TestSphere.IntersectionWith(BoundedLine.FromStartPointAndVect(new Location(0f, 10f, 0f), Direction.Right * 100f)));
		Assert.AreEqual(null, TestSphere.IntersectionWith(BoundedLine.FromStartPointAndVect(new Location(10f, 10f, 0f), Direction.Right * 100f)));
		Assert.AreEqual(null, TestSphere.IntersectionWith(BoundedLine.FromStartPointAndVect(new Location(-10f, 0f, 0f), Direction.Right * 100f)));
		Assert.AreEqual(null, TestSphere.IntersectionWith(BoundedLine.FromStartPointAndVect(new Location(-10f, 0f, 0f), Direction.Left * 2.5f)));

		intersection = TestSphere.IntersectionWith(BoundedLine.FromStartPointAndVect(new Location(10f, 6f, 0f), Direction.Right * 100f))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(-intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestSphere.IntersectionWith(BoundedLine.FromStartPointAndVect(new Location(0f, 6f, 0f), Direction.Right * 100f))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestSphere.IntersectionWith(BoundedLine.FromStartPointAndVect(new Location(10f, 6f, 0f), Direction.Right * 10f))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestSphere.IntersectionWith(BoundedLine.FromStartPointAndVect(new Location(0f, 7.4f, 0f), Direction.Right * 100f))!.Value;
		AssertToleranceEquals((0f, 7.4f, 0f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);
	}

	[Test]
	public void ShouldCorrectlyTestForLineIntersections() {
		// Line
		Assert.False(TestSphere.IsIntersectedBy(new Line(new Location(0f, 10f, 0f), Direction.Right)));
		Assert.True(TestSphere.IsIntersectedBy(new Line(new Location(0f, 6f, 0f), Direction.Right)));
		Assert.True(TestSphere.IsIntersectedBy(new Line(new Location(0f, 7.4f, 0f), Direction.Right)));


		// Ray
		Assert.False(TestSphere.IsIntersectedBy(new Ray(new Location(0f, 10f, 0f), Direction.Right)));
		Assert.False(TestSphere.IsIntersectedBy(new Ray(new Location(10f, 10f, 0f), Direction.Right)));
		Assert.False(TestSphere.IsIntersectedBy(new Ray(new Location(-10f, 0f, 0f), Direction.Right)));
		Assert.True(TestSphere.IsIntersectedBy(new Ray(new Location(10f, 6f, 0f), Direction.Right)));
		Assert.True(TestSphere.IsIntersectedBy(new Ray(new Location(0f, 6f, 0f), Direction.Right)));
		Assert.True(TestSphere.IsIntersectedBy(new Ray(new Location(0f, 7.4f, 0f), Direction.Right)));


		// BoundedLine
		Assert.False(TestSphere.IsIntersectedBy(BoundedLine.FromStartPointAndVect(new Location(0f, 10f, 0f), Direction.Right * 100f)));
		Assert.False(TestSphere.IsIntersectedBy(BoundedLine.FromStartPointAndVect(new Location(10f, 10f, 0f), Direction.Right * 100f)));
		Assert.False(TestSphere.IsIntersectedBy(BoundedLine.FromStartPointAndVect(new Location(-10f, 0f, 0f), Direction.Right * 100f)));
		Assert.False(TestSphere.IsIntersectedBy(BoundedLine.FromStartPointAndVect(new Location(-10f, 0f, 0f), Direction.Left * 2.5f)));
		Assert.True(TestSphere.IsIntersectedBy(BoundedLine.FromStartPointAndVect(new Location(10f, 6f, 0f), Direction.Right * 100f)));
		Assert.True(TestSphere.IsIntersectedBy(BoundedLine.FromStartPointAndVect(new Location(0f, 6f, 0f), Direction.Right * 100f)));
		Assert.True(TestSphere.IsIntersectedBy(BoundedLine.FromStartPointAndVect(new Location(10f, 6f, 0f), Direction.Right * 10f)));
		Assert.True(TestSphere.IsIntersectedBy(BoundedLine.FromStartPointAndVect(new Location(0f, 7.4f, 0f), Direction.Right * 100f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointToPlanes() {
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointTo(new Plane(Direction.Up, (0f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 1f, 0f), TestSphere.ClosestPointTo(new Plane(Direction.Up, (0f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointTo(new Plane(Direction.Up, (0f, 7.4f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointTo(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointOnPlanes() {
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Plane(Direction.Up, (0f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 1f, 0f), TestSphere.ClosestPointOn(new Plane(Direction.Up, (0f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOn(new Plane(Direction.Up, (0f, 7.4f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 10f, 0f), TestSphere.ClosestPointOn(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromPlanes() {
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Plane(Direction.Up, (0f, 0f, 0f))));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Plane(Direction.Up, (0f, 7.4f, 0f))));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Plane(Direction.Up, (0f, -7.4f, 0f))));
		Assert.AreEqual(2.6f, TestSphere.DistanceFrom(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
		Assert.AreEqual(2.6f, TestSphere.DistanceFrom(new Plane(Direction.Up, (0f, -10f, 0f))), TestTolerance);

		Assert.AreEqual(0f, TestSphere.SignedDistanceFrom(new Plane(Direction.Up, (0f, 0f, 0f))));
		Assert.AreEqual(0f, TestSphere.SignedDistanceFrom(new Plane(Direction.Up, (0f, 7.4f, 0f))));
		Assert.AreEqual(0f, TestSphere.SignedDistanceFrom(new Plane(Direction.Up, (0f, -7.4f, 0f))));
		Assert.AreEqual(-2.6f, TestSphere.SignedDistanceFrom(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
		Assert.AreEqual(2.6f, TestSphere.SignedDistanceFrom(new Plane(Direction.Up, (0f, -10f, 0f))), TestTolerance);


		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Plane(Direction.Up, (0f, 0f, 0f))));
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Plane(Direction.Up, (0f, 7.4f, 0f))));
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Plane(Direction.Up, (0f, -7.4f, 0f))));
		Assert.AreEqual(2.6f, TestSphere.SurfaceDistanceFrom(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
		Assert.AreEqual(2.6f, TestSphere.SurfaceDistanceFrom(new Plane(Direction.Up, (0f, -10f, 0f))), TestTolerance);

		Assert.AreEqual(0f, TestSphere.SignedSurfaceDistanceFrom(new Plane(Direction.Up, (0f, 0f, 0f))));
		Assert.AreEqual(0f, TestSphere.SignedSurfaceDistanceFrom(new Plane(Direction.Up, (0f, 7.4f, 0f))));
		Assert.AreEqual(0f, TestSphere.SignedSurfaceDistanceFrom(new Plane(Direction.Up, (0f, -7.4f, 0f))));
		Assert.AreEqual(-2.6f, TestSphere.SignedSurfaceDistanceFrom(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
		Assert.AreEqual(2.6f, TestSphere.SignedSurfaceDistanceFrom(new Plane(Direction.Up, (0f, -10f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineRelationshipToPlanes() {
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestSphere.RelationshipTo(new Plane(Direction.Up, (0f, 0f, 0f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestSphere.RelationshipTo(new Plane(Direction.Up, (0f, 7.4f, 0f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestSphere.RelationshipTo(new Plane(Direction.Up, (0f, -7.4f, 0f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestSphere.RelationshipTo(new Plane(Direction.Up, (0f, 10f, 0f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestSphere.RelationshipTo(new Plane(Direction.Up, (0f, -10f, 0f))));
	}

	[Test]
	public void ShouldCorrectlyBeSplitByPlanes() {
		Assert.AreEqual(false, TestSphere.TrySplit(new Plane(Direction.Up, (0f, 10f, 0f)), out _, out _));
		
		Assert.AreEqual(true, new OriginSphere(10f).TrySplit(new Plane(Direction.Up, (0f, 5f, 0f)), out var circleCentrePoint, out var circleRadius));
		Assert.AreEqual(8.66025448f, circleRadius, TestTolerance);
		Assert.AreEqual(new Location(0f, 5f, 0f), circleCentrePoint);

		Assert.AreEqual(true, TestSphere.TrySplit(new Plane(Direction.Up, (0f, 0f, 0f)), out circleCentrePoint, out circleRadius));
		Assert.AreEqual(7.4f, circleRadius, TestTolerance);
		Assert.AreEqual(new Location(0f, 0f, 0f), circleCentrePoint);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointToPlanes() {
		var closestPoint = TestSphere.ClosestPointOnSurfaceTo(new Plane(Direction.Up, (0f, 1f, 0f)));

		Assert.AreEqual(1f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFromOrigin(), TestTolerance);

		closestPoint = TestSphere.ClosestPointOnSurfaceTo(new Plane(Direction.Up, (0f, 0f, 0f)));

		Assert.AreEqual(0f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(true, MathF.Abs(closestPoint.X - 7.4f) < TestTolerance || MathF.Abs(closestPoint.Z - 7.4f) < TestTolerance);

		closestPoint = TestSphere.ClosestPointOnSurfaceTo(new Plane(Direction.Up, (0f, 10f, 0f)));
		AssertToleranceEquals((0f, 7.4f, 0f), closestPoint, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointOnPlanes() {
		var closestPoint = TestSphere.ClosestPointToSurfaceOn(new Plane(Direction.Up, (0f, 1f, 0f)));

		Assert.AreEqual(1f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFromOrigin(), TestTolerance);

		closestPoint = TestSphere.ClosestPointToSurfaceOn(new Plane(Direction.Up, (0f, 0f, 0f)));

		Assert.AreEqual(0f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(true, MathF.Abs(closestPoint.X - 7.4f) < TestTolerance || MathF.Abs(closestPoint.Z - 7.4f) < TestTolerance);

		closestPoint = TestSphere.ClosestPointToSurfaceOn(new Plane(Direction.Up, (0f, 10f, 0f)));
		AssertToleranceEquals((0f, 10f, 0f), closestPoint, TestTolerance);
	}
}