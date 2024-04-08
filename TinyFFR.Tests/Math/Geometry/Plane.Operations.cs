// Created on 2024-04-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class PlaneTest {
	[Test]
	public void ShouldCorrectlyFlipPlanes() {
		Assert.AreEqual(new Plane(Direction.Down, (0f, -1f, 0f)), TestPlane.Flipped);
		Assert.AreEqual(TestPlane.Flipped, -TestPlane);
	}

	[Test]
	public void ShouldCorrectlyMovePlanes() {
		Assert.AreEqual(new Plane(Direction.Up, (0f, -1f, 0f)), TestPlane + new Vect(100f, 0f, -100f));
		Assert.AreEqual(new Plane(Direction.Up, (0f, -11f, 0f)), TestPlane + new Vect(100f, -10f, -100f));
		Assert.AreEqual(new Plane(Direction.Up, (0f, 9f, 0f)), TestPlane + new Vect(100f, 10f, -100f));
	}

	[Test]
	public void ShouldCorrectlyRotateAroundPivot() {
		AssertToleranceEquals(new Plane(Direction.Down, (0f, 1f, 0f)), TestPlane * (180f % Direction.Left, Location.Origin), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Down, (0f, -3f, 0f)), TestPlane * (180f % Direction.Left, (0f, -2f, 0f)), TestTolerance);
		
		AssertToleranceEquals(TestPlane, TestPlane * (90f % Direction.Up, (43f, -123f, 0.9f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDeterminePerpendicularityWithDirections() {
		Assert.AreEqual(1f, TestPlane.PerpendicularityWith(Direction.Up));
		Assert.AreEqual(1f, TestPlane.PerpendicularityWith(Direction.Down));
		Assert.AreEqual(0f, TestPlane.PerpendicularityWith(Direction.Backward));
		Assert.AreEqual(0f, TestPlane.PerpendicularityWith(Direction.Left));
		Assert.AreEqual(0f, TestPlane.PerpendicularityWith(Direction.Right));
		Assert.AreEqual(0f, TestPlane.PerpendicularityWith(Direction.Forward));

		Assert.AreEqual(MathF.Cos(Angle.EighthCircle.Radians), TestPlane.PerpendicularityWith((1f, 1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineAngleToDirections() {
		Assert.AreEqual(Angle.Zero, TestPlane.AngleTo(Direction.Forward));
		Assert.AreEqual(Angle.Zero, TestPlane.AngleTo(Direction.Backward));
		Assert.AreEqual(Angle.Zero, TestPlane.AngleTo(Direction.Right));
		Assert.AreEqual(Angle.Zero, TestPlane.AngleTo(Direction.Left));
		Assert.AreEqual(Angle.QuarterCircle, TestPlane.AngleTo(Direction.Up));
		Assert.AreEqual(Angle.QuarterCircle, TestPlane.AngleTo(Direction.Down));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo((1f, 1f, 0f)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo((-1f, 1f, 0f)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo((-1f, -1f, 0f)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo((1f, -1f, 0f)));
		Assert.AreEqual(Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.AngleTo((2f, -1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineAngleToPlanes() {
		Assert.AreEqual(Angle.Zero, TestPlane.AngleTo(TestPlane));
		Assert.AreEqual(Angle.Zero, TestPlane.AngleTo(-TestPlane));
		Assert.AreEqual(Angle.QuarterCircle, TestPlane.AngleTo(new Plane(Direction.Backward, Location.Origin)));
		Assert.AreEqual(Angle.QuarterCircle, TestPlane.AngleTo(new Plane(Direction.Forward, Location.Origin)));
		Assert.AreEqual(Angle.QuarterCircle, TestPlane.AngleTo(new Plane(Direction.Left, Location.Origin)));
		Assert.AreEqual(Angle.QuarterCircle, TestPlane.AngleTo(new Plane(Direction.Right, Location.Origin)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo(new Plane((1f, 1f, 0f), Location.Origin)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo(new Plane((-1f, -1f, 0f), Location.Origin)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo(new Plane((1f, -1f, 0f), Location.Origin)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo(new Plane((-1f, 1f, 0f), Location.Origin)));
		Assert.AreEqual(Angle.FromRadians(MathF.Atan(2f)), TestPlane.AngleTo(new Plane((2f, -1f, 0f), Location.Origin)));
	}

	[Test]
	public void ShouldCorrectlyReflectDirections() {
		Assert.AreEqual(Direction.Up, TestPlane.Reflect(Direction.Down));
		Assert.AreEqual(Direction.Down, TestPlane.Reflect(Direction.Up));
		Assert.AreEqual(Direction.Left, TestPlane.Reflect(Direction.Left));
		Assert.AreEqual(Direction.Right, TestPlane.Reflect(Direction.Right));
		Assert.AreEqual(Direction.Forward, TestPlane.Reflect(Direction.Forward));
		Assert.AreEqual(Direction.Backward, TestPlane.Reflect(Direction.Backward));
		AssertToleranceEquals(new Direction(1f, -1f, 0f), TestPlane.Reflect((1f, 1f, 0f)), TestTolerance);

		Assert.AreEqual(Direction.Up, TestPlane.Flipped.Reflect(Direction.Down));
		Assert.AreEqual(Direction.Down, TestPlane.Flipped.Reflect(Direction.Up));
		Assert.AreEqual(Direction.Left, TestPlane.Flipped.Reflect(Direction.Left));
		Assert.AreEqual(Direction.Right, TestPlane.Flipped.Reflect(Direction.Right));
		Assert.AreEqual(Direction.Forward, TestPlane.Flipped.Reflect(Direction.Forward));
		Assert.AreEqual(Direction.Backward, TestPlane.Flipped.Reflect(Direction.Backward));
		AssertToleranceEquals(new Direction(1f, -1f, 0f), TestPlane.Flipped.Reflect((1f, 1f, 0f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToGivenLocation() {
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.ClosestPointTo((0f, -1f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.ClosestPointTo((0f, -1000f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.ClosestPointTo((0f, 1000f, 0f)));
		Assert.AreEqual(new Location(100f, -1f, -100f), TestPlane.ClosestPointTo((100f, -1000f, -100f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.Flipped.ClosestPointTo((0f, -1f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.Flipped.ClosestPointTo((0f, -1000f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.Flipped.ClosestPointTo((0f, 1000f, 0f)));
		Assert.AreEqual(new Location(100f, -1f, -100f), TestPlane.Flipped.ClosestPointTo((100f, -1000f, -100f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromGivenLocation() {
		void AssertDistanceFromTestPlane(float expectedSignedDistance, Location location) {
			Assert.AreEqual(expectedSignedDistance, TestPlane.SignedDistanceFrom(location), TestTolerance);
			Assert.AreEqual(-expectedSignedDistance, TestPlane.Flipped.SignedDistanceFrom(location), TestTolerance);
			Assert.AreEqual(MathF.Abs(expectedSignedDistance), TestPlane.DistanceFrom(location), TestTolerance);
			Assert.AreEqual(MathF.Abs(expectedSignedDistance), TestPlane.Flipped.DistanceFrom(location), TestTolerance);
		}

		AssertDistanceFromTestPlane(0f, (0f, -1f, 0f));
		AssertDistanceFromTestPlane(2f, (0f, 1f, 0f));
		AssertDistanceFromTestPlane(-2f, (0f, -3f, 0f));

		AssertDistanceFromTestPlane(0f, (100f, -1f, 0f));
		AssertDistanceFromTestPlane(2f, (-100f, 1f, 100f));
		AssertDistanceFromTestPlane(-2f, (0f, -3f, -100f));

		Assert.AreEqual(1f, TestPlane.DistanceFromOrigin());
		Assert.AreEqual(1f, TestPlane.SignedDistanceFromOrigin());
		Assert.AreEqual(1f, TestPlane.Flipped.DistanceFromOrigin());
		Assert.AreEqual(-1f, TestPlane.Flipped.SignedDistanceFromOrigin());
	}

	[Test]
	public void ShouldCorrectlyDetermineWhetherPlaneFacesTowardsOrAwayFromLocations() {
		Assert.AreEqual(true, TestPlane.FacesTowards((100f, 1f, -100f)));
		Assert.AreEqual(true, TestPlane.FacesTowards((100f, 0f, -100f)));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -1f, -100f)));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -2f, -100f)));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -3f, -100f)));

		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, 1f, -100f)));
		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, 0f, -100f)));
		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, -1f, -100f)));
		Assert.AreEqual(true, TestPlane.FacesAwayFrom((100f, -2f, -100f)));
		Assert.AreEqual(true, TestPlane.FacesAwayFrom((100f, -3f, -100f)));

		Assert.AreEqual(true, TestPlane.FacesTowards((100f, 1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, 0f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -2f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -3f, -100f), planeThickness: 1.5f));

		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, 1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, 0f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, -1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, -2f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(true, TestPlane.FacesAwayFrom((100f, -3f, -100f), planeThickness: 1.5f));

		Assert.AreEqual(false, TestPlane.FacesAwayFromOrigin());
		Assert.AreEqual(true, TestPlane.FacesTowardsOrigin());
		Assert.AreEqual(false, TestPlane.FacesAwayFromOrigin(planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesTowardsOrigin(planeThickness: 1.5f));
	}

	[Test]
	public void ShouldCorrectlyDetermineWhetherAPointIsOnThePlane() {
		Assert.AreEqual(false, TestPlane.Contains((100f, 1f, -100f)));
		Assert.AreEqual(false, TestPlane.Contains((100f, 0f, -100f)));
		Assert.AreEqual(true, TestPlane.Contains((100f, -1f, -100f)));
		Assert.AreEqual(false, TestPlane.Contains((100f, -2f, -100f)));
		Assert.AreEqual(false, TestPlane.Contains((100f, -3f, -100f)));

		Assert.AreEqual(false, TestPlane.Contains((100f, 1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(true, TestPlane.Contains((100f, 0f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(true, TestPlane.Contains((100f, -1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(true, TestPlane.Contains((100f, -2f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.Contains((100f, -3f, -100f), planeThickness: 1.5f));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromOtherPlanes() {
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Backward, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Forward, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Left, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Right, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Up, (0f, -1f, 0f))));
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Down, (0f, -1f, 0f))));

		Assert.AreEqual(10f, TestPlane.DistanceFrom(new Plane(Direction.Up, (0f, -11f, 0f))));
		Assert.AreEqual(10f, TestPlane.DistanceFrom(new Plane(Direction.Down, (0f, 9f, 0f))));
		Assert.AreEqual(10f, TestPlane.DistanceFrom(new Plane(Direction.Down, (0f, -11f, 0f))));
		Assert.AreEqual(10f, TestPlane.DistanceFrom(new Plane(Direction.Up, (0f, 9f, 0f))));

		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane((0.001f, 1f, 0f), Location.Origin)));
	}

	[Test]
	public void ShouldCorrectlyCalculateIntersectionWithOtherPlanes() {
		void AssertIntersection(Line expectedLine, Plane planeA, Plane planeB) {
			AssertToleranceEquals(expectedLine, planeA.IntersectionWith(planeB), TestTolerance);
			AssertToleranceEquals(expectedLine, planeA.Flipped.IntersectionWith(planeB), TestTolerance);
			AssertToleranceEquals(expectedLine, planeB.IntersectionWith(planeA), TestTolerance);
			AssertToleranceEquals(expectedLine, planeB.Flipped.IntersectionWith(planeA), TestTolerance);
		}

		Assert.AreEqual(null, TestPlane.IntersectionWith(new Plane(Direction.Down, Location.Origin)));
		Assert.AreEqual(null, TestPlane.IntersectionWith(new Plane(Direction.Down, TestPlane.ClosestPointToOrigin)));
		Assert.AreEqual(null, TestPlane.IntersectionWith(new Plane(Direction.Up, TestPlane.ClosestPointToOrigin)));

		AssertIntersection(
			new Line((-1f, -1f, 0f), Direction.Forward),
			TestPlane,
			new Plane(Direction.Right, (-1f, 0f, 0f))
		);
		AssertIntersection(
			new Line((0f, -1f, 100f), Direction.Left),
			TestPlane,
			new Plane((0f, 1f, 1f), (0f, 99f, 0f))
		);
		AssertIntersection(
			new Line(Location.Origin, Direction.Down),
			new Plane((1f, 0f, 1f), Location.Origin),
			new Plane((-1f, 0f, 1f), Location.Origin)
		);
	}

	[Test]
	public void ShouldCorrectlyProjectVectors() {
		Assert.AreEqual(new Vect(10f, 0f, -10f), TestPlane.ProjectionOf(new Vect(10f, 0f, -10f)));
		Assert.AreEqual(new Vect(10f, 0f, -10f), TestPlane.ProjectionOf(new Vect(10f, -20f, -10f)));
		Assert.AreEqual(new Vect(10f, 0f, -10f), TestPlane.ProjectionOf(new Vect(10f, 20f, -10f)));
		Assert.AreEqual(Vect.Zero, TestPlane.ProjectionOf(Vect.Zero));
		Assert.AreEqual(Vect.Zero, TestPlane.ProjectionOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(Vect.Zero, TestPlane.ProjectionOf(new Vect(0f, -1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyParallelizeVectors() {
		Assert.AreEqual(new Vect(10f, 0f, 10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(10f, 10f, 10f)));
		Assert.AreEqual(new Vect(10f, 0f, 10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(10f, -10f, 10f)));
		Assert.AreEqual(new Vect(-10f, 0f, -10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(-10f, 10f, -10f)));
		Assert.AreEqual(new Vect(-10f, 0f, -10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(-10f, -10f, -10f)));
		Assert.AreEqual(Vect.Zero, TestPlane.ParallelizationOf(Vect.Zero));
		Assert.AreEqual(Vect.Zero, TestPlane.ParallelizationOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(Vect.Zero, TestPlane.ParallelizationOf(new Vect(0f, -1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyProjectDirections() {
		AssertToleranceEquals(Direction.Left, TestPlane.ProjectionOf(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Left, TestPlane.ProjectionOf(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Right, TestPlane.ProjectionOf(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Right, TestPlane.ProjectionOf(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Forward, TestPlane.ProjectionOf(new Direction(0f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Forward, TestPlane.ProjectionOf(new Direction(0f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Backward, TestPlane.ProjectionOf(new Direction(0f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Backward, TestPlane.ProjectionOf(new Direction(0f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.ProjectionOf(new Direction(1f, 0f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.ProjectionOf(new Direction(1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.ProjectionOf(new Direction(1f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.ProjectionOf(new Direction(-1f, 0f, -1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.ProjectionOf(new Direction(-1f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.ProjectionOf(new Direction(-1f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(Direction.None, TestPlane.ProjectionOf(Direction.Up), TestTolerance);
		AssertToleranceEquals(Direction.None, TestPlane.ProjectionOf(Direction.Down), TestTolerance);
		AssertToleranceEquals(Direction.None, TestPlane.ProjectionOf(Direction.None), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeVectors() {
		Assert.AreEqual(new Vect(0f, 10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(10f, 10f, 10f)));
		Assert.AreEqual(new Vect(0f, -10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(10f, -10f, 10f)));
		Assert.AreEqual(new Vect(0f, 10, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(-10f, 10f, -10f)));
		Assert.AreEqual(new Vect(0f, -10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(-10f, -10f, -10f)));
		Assert.AreEqual(Vect.Zero, TestPlane.OrthogonalizationOf(Vect.Zero));
		Assert.AreEqual(new Vect(0f, 1f, 0f), TestPlane.OrthogonalizationOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(new Vect(0f, -1f, 0f), TestPlane.OrthogonalizationOf(new Vect(0f, -1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeDirections() {
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(0f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(0f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(0f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(0f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(1f, 0f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(1f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(-1f, 0f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(-1f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(-1f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(Direction.Up), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(Direction.Down), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(Direction.None), TestTolerance);
	}
}