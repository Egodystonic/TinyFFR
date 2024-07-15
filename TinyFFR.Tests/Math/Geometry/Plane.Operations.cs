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

		Assert.AreEqual(MathF.Cos(Angle.EighthCircle.AsRadians), TestPlane.PerpendicularityWith((1f, 1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineAngleToDirections() {
		Assert.AreEqual(Angle.Zero, TestPlane.AngleTo(Direction.Forward));
		Assert.AreEqual(Angle.Zero, TestPlane.AngleTo(Direction.Backward));
		Assert.AreEqual(Angle.Zero, TestPlane.AngleTo(Direction.Right));
		Assert.AreEqual(Angle.Zero, TestPlane.AngleTo(Direction.Left));
		Assert.AreEqual(Angle.QuarterCircle, TestPlane.AngleTo(Direction.Up));
		Assert.AreEqual(Angle.QuarterCircle, TestPlane.AngleTo(Direction.Down));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo(new Direction(1f, 1f, 0f)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo(new Direction(-1f, 1f, 0f)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo(new Direction(-1f, -1f, 0f)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.AngleTo(new Direction(1f, -1f, 0f)));
		Assert.AreEqual(Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.AngleTo(new Direction(2f, -1f, 0f)));

		Assert.AreEqual(Angle.Zero, TestPlane.SignedAngleTo(Direction.Forward));
		Assert.AreEqual(Angle.Zero, TestPlane.SignedAngleTo(Direction.Backward));
		Assert.AreEqual(Angle.Zero, TestPlane.SignedAngleTo(Direction.Right));
		Assert.AreEqual(Angle.Zero, TestPlane.SignedAngleTo(Direction.Left));
		Assert.AreEqual(Angle.QuarterCircle, TestPlane.SignedAngleTo(Direction.Up));
		Assert.AreEqual(-Angle.QuarterCircle, TestPlane.SignedAngleTo(Direction.Down));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.SignedAngleTo(new Direction(1f, 1f, 0f)));
		Assert.AreEqual(Angle.EighthCircle, TestPlane.SignedAngleTo(new Direction(-1f, 1f, 0f)));
		Assert.AreEqual(-Angle.EighthCircle, TestPlane.SignedAngleTo(new Direction(-1f, -1f, 0f)));
		Assert.AreEqual(-Angle.EighthCircle, TestPlane.SignedAngleTo(new Direction(1f, -1f, 0f)));
		Assert.AreEqual(-Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.SignedAngleTo(new Direction(2f, -1f, 0f)));
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
		Assert.AreEqual(Direction.Up, TestPlane.ReflectionOf(Direction.Down));
		Assert.AreEqual(Direction.Down, TestPlane.ReflectionOf(Direction.Up));
		Assert.AreEqual(Direction.Left, TestPlane.ReflectionOf(Direction.Left));
		Assert.AreEqual(Direction.Right, TestPlane.ReflectionOf(Direction.Right));
		Assert.AreEqual(Direction.Forward, TestPlane.ReflectionOf(Direction.Forward));
		Assert.AreEqual(Direction.Backward, TestPlane.ReflectionOf(Direction.Backward));
		AssertToleranceEquals(new Direction(1f, -1f, 0f), TestPlane.ReflectionOf(new Direction(1f, 1f, 0f)), TestTolerance);

		Assert.AreEqual(Direction.Up, TestPlane.Flipped.ReflectionOf(Direction.Down));
		Assert.AreEqual(Direction.Down, TestPlane.Flipped.ReflectionOf(Direction.Up));
		Assert.AreEqual(Direction.Left, TestPlane.Flipped.ReflectionOf(Direction.Left));
		Assert.AreEqual(Direction.Right, TestPlane.Flipped.ReflectionOf(Direction.Right));
		Assert.AreEqual(Direction.Forward, TestPlane.Flipped.ReflectionOf(Direction.Forward));
		Assert.AreEqual(Direction.Backward, TestPlane.Flipped.ReflectionOf(Direction.Backward));
		AssertToleranceEquals(new Direction(1f, -1f, 0f), TestPlane.Flipped.ReflectionOf(new Direction(1f, 1f, 0f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToGivenLocation() {
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.PointClosestTo((0f, -1f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.PointClosestTo((0f, -1000f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.PointClosestTo((0f, 1000f, 0f)));
		Assert.AreEqual(new Location(100f, -1f, -100f), TestPlane.PointClosestTo((100f, -1000f, -100f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.Flipped.PointClosestTo((0f, -1f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.Flipped.PointClosestTo((0f, -1000f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.Flipped.PointClosestTo((0f, 1000f, 0f)));
		Assert.AreEqual(new Location(100f, -1f, -100f), TestPlane.Flipped.PointClosestTo((100f, -1000f, -100f)));
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
	public void ShouldCorrectlyTestForIntersectionWithOtherPlanes() {
		Assert.False(TestPlane.IsIntersectedBy(new Plane(Direction.Down, Location.Origin)));
		Assert.False(TestPlane.IsIntersectedBy(new Plane(Direction.Down, TestPlane.PointClosestToOrigin)));
		Assert.False(TestPlane.IsIntersectedBy(new Plane(Direction.Up, TestPlane.PointClosestToOrigin)));

		Assert.True(
			TestPlane.IsIntersectedBy(new Plane(Direction.Right, (-1f, 0f, 0f)))
		);
		Assert.True(
			TestPlane.IsIntersectedBy(new Plane((0f, 1f, 1f), (0f, 99f, 0f)))
		);
		Assert.True(
			new Plane((1f, 0f, 1f), Location.Origin).IsIntersectedBy(new Plane((-1f, 0f, 1f), Location.Origin))
		);
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
		Assert.AreEqual(null, TestPlane.IntersectionWith(new Plane(Direction.Down, TestPlane.PointClosestToOrigin)));
		Assert.AreEqual(null, TestPlane.IntersectionWith(new Plane(Direction.Up, TestPlane.PointClosestToOrigin)));

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
		Assert.AreEqual(new Vect(10f, 0f, -10f), TestPlane.FastProjectionOf(new Vect(10f, 0f, -10f)));
		Assert.AreEqual(new Vect(10f, 0f, -10f), TestPlane.FastProjectionOf(new Vect(10f, -20f, -10f)));
		Assert.AreEqual(new Vect(10f, 0f, -10f), TestPlane.FastProjectionOf(new Vect(10f, 20f, -10f)));
		Assert.AreEqual(null, TestPlane.ProjectionOf(Vect.Zero));
		Assert.AreEqual(null, TestPlane.ProjectionOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(null, TestPlane.ProjectionOf(new Vect(0f, -1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyParallelizeVectors() {
		Assert.AreEqual(new Vect(10f, 0f, 10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(10f, 10f, 10f)));
		Assert.AreEqual(new Vect(10f, 0f, 10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(10f, -10f, 10f)));
		Assert.AreEqual(new Vect(-10f, 0f, -10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(-10f, 10f, -10f)));
		Assert.AreEqual(new Vect(-10f, 0f, -10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(-10f, -10f, -10f)));
		Assert.AreEqual(new Vect(10f, 0f, 10f).WithLength(MathF.Sqrt(300f)), TestPlane.FastParallelizationOf(new Vect(10f, 10f, 10f)));
		Assert.AreEqual(new Vect(10f, 0f, 10f).WithLength(MathF.Sqrt(300f)), TestPlane.FastParallelizationOf(new Vect(10f, -10f, 10f)));
		Assert.AreEqual(new Vect(-10f, 0f, -10f).WithLength(MathF.Sqrt(300f)), TestPlane.FastParallelizationOf(new Vect(-10f, 10f, -10f)));
		Assert.AreEqual(new Vect(-10f, 0f, -10f).WithLength(MathF.Sqrt(300f)), TestPlane.FastParallelizationOf(new Vect(-10f, -10f, -10f)));
		Assert.AreEqual(null, TestPlane.ParallelizationOf(Vect.Zero));
		Assert.AreEqual(null, TestPlane.ParallelizationOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(null, TestPlane.ParallelizationOf(new Vect(0f, -1f, 0f)));
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

		AssertToleranceEquals(Direction.Left, TestPlane.FastProjectionOf(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Left, TestPlane.FastProjectionOf(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Right, TestPlane.FastProjectionOf(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Right, TestPlane.FastProjectionOf(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Forward, TestPlane.FastProjectionOf(new Direction(0f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Forward, TestPlane.FastProjectionOf(new Direction(0f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Backward, TestPlane.FastProjectionOf(new Direction(0f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Backward, TestPlane.FastProjectionOf(new Direction(0f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.ProjectionOf(new Direction(1f, 0f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.ProjectionOf(new Direction(1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.ProjectionOf(new Direction(1f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.ProjectionOf(new Direction(-1f, 0f, -1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.ProjectionOf(new Direction(-1f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.ProjectionOf(new Direction(-1f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.FastProjectionOf(new Direction(1f, 0f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.FastProjectionOf(new Direction(1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.FastProjectionOf(new Direction(1f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.FastProjectionOf(new Direction(-1f, 0f, -1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.FastProjectionOf(new Direction(-1f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.FastProjectionOf(new Direction(-1f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(null, TestPlane.ProjectionOf(Direction.Up), TestTolerance);
		AssertToleranceEquals(null, TestPlane.ProjectionOf(Direction.Down), TestTolerance);
		AssertToleranceEquals(null, TestPlane.ProjectionOf(Direction.None), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeVectors() {
		Assert.AreEqual(new Vect(0f, 10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(10f, 10f, 10f)));
		Assert.AreEqual(new Vect(0f, -10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(10f, -10f, 10f)));
		Assert.AreEqual(new Vect(0f, 10, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(-10f, 10f, -10f)));
		Assert.AreEqual(new Vect(0f, -10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(-10f, -10f, -10f)));
		Assert.AreEqual(new Vect(0f, 1f, 0f), TestPlane.OrthogonalizationOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(new Vect(0f, -1f, 0f), TestPlane.OrthogonalizationOf(new Vect(0f, -1f, 0f)));

		Assert.AreEqual(new Vect(0f, 10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.FastOrthogonalizationOf(new Vect(10f, 10f, 10f)));
		Assert.AreEqual(new Vect(0f, -10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.FastOrthogonalizationOf(new Vect(10f, -10f, 10f)));
		Assert.AreEqual(new Vect(0f, 10, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.FastOrthogonalizationOf(new Vect(-10f, 10f, -10f)));
		Assert.AreEqual(new Vect(0f, -10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.FastOrthogonalizationOf(new Vect(-10f, -10f, -10f)));
		Assert.AreEqual(new Vect(0f, 1f, 0f), TestPlane.FastOrthogonalizationOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(new Vect(0f, -1f, 0f), TestPlane.FastOrthogonalizationOf(new Vect(0f, -1f, 0f)));

		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(Vect.Zero));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(1f, 0f, 0f)));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(0f, 0f, 1f)));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(-1f, 0f, 0f)));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(0f, 0f, -1f)));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(1f, 0f, -1f)));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(-1f, 0f, 1f)));
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

		AssertToleranceEquals(null, TestPlane.OrthogonalizationOf(new Direction(1f, 0f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(1f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(null, TestPlane.OrthogonalizationOf(new Direction(-1f, 0f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(-1f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(-1f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(0f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(0f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(0f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(0f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(1f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(-1f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(-1f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(Direction.Up), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(Direction.Down), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(Direction.Up), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(Direction.Down), TestTolerance);
		AssertToleranceEquals(null, TestPlane.OrthogonalizationOf(Direction.None), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyConvertBetween3DAnd2D() {
		// 3D -> 2D
		AssertToleranceEquals(
			new XYPair<float>(1f, -1f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(1f, 0f, 0f), new(0f, 0f, 1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-2f, 2f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(-1f, 0f, 0f), new(0f, 0f, -1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-1f, 1f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(-1f, 0f, 0f), new(0f, 0f, -1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(2f, -2f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(1f, 0f, 0f), new(0f, 0f, 1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new XYPair<float>(1f, -1f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(1f, 1f, 0f), new(0f, -1f, 1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-2f, 2f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(-1f, 1f, 0f), new(0f, -1f, -1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-1f, 1f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(-1f, -1f, 0f), new(0f, 1f, -1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(2f, -2f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(1f, -1f, 0f), new(0f, 1f, 1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new XYPair<float>(-2f, -4f),
			TestPlane.CreateDimensionConverter(new Location(3f, 0f, 3f), new(1f, 0f, 0f), new(0f, 0f, 1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-5f, -1f),
			TestPlane.CreateDimensionConverter(new Location(-3f, 0f, -3f), new(-1f, 0f, 0f), new(0f, 0f, -1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(2f, 4f),
			TestPlane.CreateDimensionConverter(new Location(3f, 0f, 3f), new(-1f, 0f, 0f), new(0f, 0f, -1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(5f, 1f),
			TestPlane.CreateDimensionConverter(new Location(-3f, 0f, -3f), new(1f, 0f, 0f), new(0f, 0f, 1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new XYPair<float>(-2f, -4f),
			TestPlane.CreateDimensionConverter(new Location(3f, 10f, 3f), new(1f, 1f, 0f), new(0f, -1f, 1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-5f, -1f),
			TestPlane.CreateDimensionConverter(new Location(-3f, 10f, -3f), new(-1f, 1f, 0f), new(0f, -1f, -1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(2f, 4f),
			TestPlane.CreateDimensionConverter(new Location(3f, -10f, 3f), new(-1f, -1f, 0f), new(0f, 1f, -1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(5f, 1f),
			TestPlane.CreateDimensionConverter(new Location(-3f, -10f, -3f), new(1f, -1f, 0f), new(0f, 1f, 1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new XYPair<float>(0f, 0f),
			new Plane(new Direction(1f, 1f, 1f), 10f).CreateDimensionConverter(new Vect(1f, 1f, 1f).WithLength(10f).AsLocation(), new Direction(1f, -1f, 0f), new Direction(-1f, 0f, 1f)).Convert((20f, 20f, 20f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(1f, 0f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation(),
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(1f, -1f, 0f).WithLength(1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(0f, 1f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation(),
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(-0.408f, -0.408f, 0.816f).WithLength(1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-3f, 0f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation(),
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(1f, -1f, 0f).WithLength(-3f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(0f, -3f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation(),
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(-0.408f, -0.408f, 0.816f).WithLength(-3f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(0f, 0f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation() + new Direction(1f, -1f, 0f) * -3f,
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(1f, -1f, 0f).WithLength(-3f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(3f, -3f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation() + new Direction(1f, -1f, 0f) * -3f,
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(-0.408f, -0.408f, 0.816f).WithLength(-3f)),
			TestTolerance
		);

		// 2D -> 3D
		AssertToleranceEquals(
			new Location(1f, -1f, -1f),
			TestPlane.CreateDimensionConverter(new Location(3f, 10f, 3f), new(1f, 1f, 0f), new(0f, -1f, 1f)).Convert((-2f, -4f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(2f, -1f, -2f),
			TestPlane.CreateDimensionConverter(new Location(-3f, 10f, -3f), new(-1f, 1f, 0f), new(0f, -1f, -1f)).Convert((-5f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(1f, -1f, -1f),
			TestPlane.CreateDimensionConverter(new Location(3f, -10f, 3f), new(-1f, -1f, 0f), new(0f, 1f, -1f)).Convert((2f, 4f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(2f, -1f, -2f),
			TestPlane.CreateDimensionConverter(new Location(-3f, -10f, -3f), new(1f, -1f, 0f), new(0f, 1f, 1f)).Convert((5f, 1f)),
			TestTolerance
		);
	}

	[Test]
	public void ConvenienceMethodsShouldCorrectlyConvertBetween2DAnd3D() {
		Assert.AreEqual(MathF.Sqrt(200f), new Location(10f, 10f, 10f).ProjectedTo2DOn(TestPlane).ToVector2().Length(), TestTolerance);
		Assert.AreEqual(MathF.Sqrt(200f), new Location(-10f, -10f, -10f).ProjectedTo2DOn(TestPlane).ToVector2().Length(), TestTolerance);
		Assert.AreEqual(new Location(10f, 10f, 10f).ProjectedTo2DOn(TestPlane).X, -new Location(-10f, -10f, -10f).ProjectedTo2DOn(TestPlane).X, TestTolerance);
		Assert.AreEqual(new Location(10f, 10f, 10f).ProjectedTo2DOn(TestPlane).Y, -new Location(-10f, -10f, -10f).ProjectedTo2DOn(TestPlane).Y, TestTolerance);

		AssertToleranceEquals(
			new Location(10f, -1f, 10f),
			new Location(10f, 10f, 10f).ProjectedTo2DOn(TestPlane).ExpandedTo3DOn(TestPlane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-10f, -1f, -10f),
			new Location(-10f, -10f, -10f).ProjectedTo2DOn(TestPlane).ExpandedTo3DOn(TestPlane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(10f, 10f, 10f),
			new Location(10f, 10f, 10f).ProjectedTo2DOn(TestPlane).ExpandedTo3DOn(TestPlane, 11f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-10f, -10f, -10f),
			new Location(-10f, -10f, -10f).ProjectedTo2DOn(TestPlane).ExpandedTo3DOn(TestPlane, -9f),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyConstructDimensionConverters() {
		var converter = TestPlane.CreateDimensionConverter();
		Assert.AreEqual(TestPlane.PointClosestToOrigin, converter.Origin);
		Assert.AreEqual(TestPlane.Normal, converter.PlaneNormal);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ converter.YBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ TestPlane.Normal, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.YBasis ^ TestPlane.Normal, TestTolerance);

		converter = TestPlane.CreateDimensionConverter(new Location(-100f, 1000f, 33f));
		Assert.AreEqual(new Location(-100f, -1f, 33f), converter.Origin);
		Assert.AreEqual(TestPlane.Normal, converter.PlaneNormal);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ converter.YBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ TestPlane.Normal, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.YBasis ^ TestPlane.Normal, TestTolerance);

		converter = TestPlane.CreateDimensionConverter(new Location(-100f, 1000f, 33f), new Direction(1f, 1f, -1f));
		Assert.AreEqual(new Location(-100f, -1f, 33f), converter.Origin);
		Assert.AreEqual(TestPlane.Normal, converter.PlaneNormal);
		AssertToleranceEquals(new Direction(1f, 0f, -1f), converter.XBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ converter.YBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ TestPlane.Normal, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.YBasis ^ TestPlane.Normal, TestTolerance);

		converter = TestPlane.CreateDimensionConverter(new Location(-100f, 1000f, 33f), new Direction(1f, 1f, -1f), new Direction(1f, -1f, 0.8f));
		Assert.AreEqual(new Location(-100f, -1f, 33f), converter.Origin);
		Assert.AreEqual(TestPlane.Normal, converter.PlaneNormal);
		AssertToleranceEquals(new Direction(1f, 0f, -1f), converter.XBasis, TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), converter.YBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ converter.YBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ TestPlane.Normal, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.YBasis ^ TestPlane.Normal, TestTolerance);

		var testList = new List<Direction>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					if (x == 0f && y == 0f && z == 0f) continue;
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var x = testList[i];
			if (x.Equals(Direction.None, TestTolerance)) continue;
			for (var j = i + 1; j < testList.Count; ++j) {
				var y = testList[j];
				if (y.Equals(Direction.None, TestTolerance)) continue;
				if (x.AngleTo(TestPlane).Equals(90f, TestTolerance)) continue;
				if (y.AngleTo(TestPlane).Equals(90f, TestTolerance)) continue;
				if (1f - MathF.Abs(x.ProjectedOnTo(TestPlane)!.Value.Dot(y.ProjectedOnTo(TestPlane)!.Value)) < TestTolerance) continue;

				try {
					converter = TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, x, y);
					AssertToleranceEquals(new Angle(90f), converter.XBasis ^ converter.YBasis, TestTolerance);
					AssertToleranceEquals(new Angle(90f), converter.XBasis ^ TestPlane.Normal, TestTolerance);
					AssertToleranceEquals(new Angle(90f), converter.YBasis ^ TestPlane.Normal, TestTolerance);
				}
				catch {
					Console.WriteLine("X: " + x.ToStringDescriptive() + " | Y: " + y.ToStringDescriptive());
					throw;
				}
			}
		}

		Assert.Throws<ArgumentException>(() => TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Up));
		Assert.Throws<ArgumentException>(() => TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Down));
		Assert.Throws<ArgumentException>(() => TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Up, Direction.Right));
		Assert.Throws<ArgumentException>(() => TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Down, Direction.Right));
		Assert.Throws<ArgumentException>(() => TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Right, Direction.Right));
		Assert.Throws<ArgumentException>(() => TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Left, Direction.Right));
		Assert.Throws<ArgumentException>(() => TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Left, Direction.Up));
		Assert.Throws<ArgumentException>(() => TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Left, Direction.Down));
	}
}