// Created on 2024-03-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

[TestFixture]
class LineLikeInterfaceTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCalculateUnboundedIntersectionDistances() {
		const float TestTolerance = 0.0001f;

		void AssertPair(Location l1, Direction d1, Location l2, Direction d2, (float First, float Second)? expectation) {
			var thisLine = new Line(l1, d1);
			var otherLine = new Line(l2, d2);

			if (expectation == null) {
				Assert.IsNull(ILineLike.CalculateUnboundedIntersectionDistanceOnThisLine(thisLine, otherLine));
				Assert.IsNull(ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(thisLine, otherLine));
				Assert.IsNull(ILineLike.CalculateUnboundedIntersectionDistanceOnThisLine(otherLine, thisLine));
				Assert.IsNull(ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(otherLine, thisLine));
			}
			else {
				Assert.AreEqual(
					expectation.Value.First,
					ILineLike.CalculateUnboundedIntersectionDistanceOnThisLine(thisLine, otherLine)!.Value,
					TestTolerance
				);
				Assert.AreEqual(
					expectation.Value.Second,
					ILineLike.CalculateUnboundedIntersectionDistanceOnThisLine(otherLine, thisLine)!.Value,
					TestTolerance
				);
				var bothActual = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(thisLine, otherLine)!;
				Assert.AreEqual(
					expectation.Value.First,
					bothActual.Value.ThisDistance,
					TestTolerance
				);
				Assert.AreEqual(
					expectation.Value.Second,
					bothActual.Value.OtherDistance,
					TestTolerance
				);
				bothActual = ILineLike.CalculateUnboundedIntersectionDistancesOnBothLines(otherLine, thisLine)!;
				Assert.AreEqual(
					expectation.Value.Second,
					bothActual.Value.ThisDistance,
					TestTolerance
				);
				Assert.AreEqual(
					expectation.Value.First,
					bothActual.Value.OtherDistance,
					TestTolerance
				);
			}

			
		}

		AssertPair(
			(0f, 0f, 0f), Direction.Up,
			(0f, 0f, 0f), Direction.Right,
			(0f, 0f)
		);
		AssertPair(
			(0f, 10f, 0f), Direction.Up,
			(10f, 0f, 0f), Direction.Right,
			(-10f, 10f)
		);
		AssertPair(
			(0f, -10f, 0f), Direction.Up,
			(-10f, 0f, 0f), Direction.Right,
			(10f, -10f)
		);
		AssertPair(
			(1f, 1f, 0f), (-1f, -1f, 0f),
			(-1f, 1f, 0f), (1f, -1f, 0f),
			(MathF.Sqrt(2f), MathF.Sqrt(2f))
		);
		AssertPair(
			(4f, 4f, 0f), (-1f, -1f, 0f),
			(2f, -2f, 0f), (1f, -1f, 0f),
			(MathF.Sqrt(32f), -MathF.Sqrt(8f))
		);
		AssertPair(
			(0f, 0f, 0f), Direction.Right,
			(0f, 1f, 0f), Direction.Left,
			null
		);
		AssertPair(
			(1f, 1f, 1f), (1f, 1f, 1f),
			(3f, 3f, 3f), (-1f, -1f, -1f),
			null
		);
		AssertPair(
			Location.Origin, (1f, 1f, 1f),
			Location.Origin, (-1f, -1f, -1f),
			null
		);
		AssertPair(
			(1f, 1f, 1f), (1f, 1f, 1f),
			(1f, 3f, 1f), (-1f, -1f, -1f),
			null
		);
	}

	[Test]
	public void ShouldCorrectlyImplementMirrorMethodsForLineLikes() {
		const float TestTolerance = 0.5f;

		void AssertForLineType<TSelf>() where TSelf : struct, ILineLike<TSelf> {
			AssertMirrorMethodWithTolerance<TSelf, Line>((a, b) => a.AngleTo(b), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, Ray>((a, b) => a.AngleTo(b), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, BoundedRay>((a, b) => a.AngleTo(b), TestTolerance);

			AssertMirrorMethodWithTolerance<TSelf, Line>((a, b) => a.DistanceSquaredFrom(b), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, Ray>((a, b) => a.DistanceSquaredFrom(b), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, BoundedRay>((a, b) => a.DistanceSquaredFrom(b), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, Plane>((a, b) => a.DistanceSquaredFrom(b), TestTolerance);
			
			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsIntersectedBy(b));
			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsIntersectedBy(b, 0.5f));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsIntersectedBy(b));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsIntersectedBy(b, 0.5f));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsIntersectedBy(b));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsIntersectedBy(b, 0.5f));
			AssertMirrorMethodWithTolerance<TSelf, Line>((a, b) => a.IntersectionWith(b), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, Line>((a, b) => a.IntersectionWith(b, 0.5f), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, Ray>((a, b) => a.IntersectionWith(b), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, Ray>((a, b) => a.IntersectionWith(b, 0.5f), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, BoundedRay>((a, b) => a.IntersectionWith(b), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, BoundedRay>((a, b) => a.IntersectionWith(b, 0.5f), TestTolerance);

			AssertMirrorMethodWithTolerance<TSelf, Line>((a, b) => a.DistanceFrom(b), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, Ray>((a, b) => a.DistanceFrom(b), TestTolerance);
			AssertMirrorMethodWithTolerance<TSelf, BoundedRay>((a, b) => a.DistanceFrom(b), TestTolerance);

			AssertMirrorMethod<TSelf, Direction>((a, b) => a.IsParallelTo(b));
			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsParallelTo(b));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsParallelTo(b));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsParallelTo(b));

			AssertMirrorMethod<TSelf, Direction>((a, b) => a.IsApproximatelyParallelTo(b));
			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsApproximatelyParallelTo(b));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsApproximatelyParallelTo(b));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsApproximatelyParallelTo(b));
			AssertMirrorMethod<TSelf, Direction>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));
			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));

			AssertMirrorMethod<TSelf, Direction>((a, b) => a.IsOrthogonalTo(b));
			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsOrthogonalTo(b));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsOrthogonalTo(b));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsOrthogonalTo(b));

			AssertMirrorMethod<TSelf, Direction>((a, b) => a.IsApproximatelyOrthogonalTo(b));
			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsApproximatelyOrthogonalTo(b));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsApproximatelyOrthogonalTo(b));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsApproximatelyOrthogonalTo(b));
			AssertMirrorMethod<TSelf, Direction>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));
			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));

			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsExactlyColinearWith(b, 0.5f));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsExactlyColinearWith(b, 0.5f));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsExactlyColinearWith(b, 0.5f));

			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsApproximatelyColinearWith(b));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsApproximatelyColinearWith(b));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsApproximatelyColinearWith(b));
			AssertMirrorMethod<TSelf, Line>((a, b) => a.IsApproximatelyColinearWith(b, 0.5f, new Angle(10f)));
			AssertMirrorMethod<TSelf, Ray>((a, b) => a.IsApproximatelyColinearWith(b, 0.5f, new Angle(10f)));
			AssertMirrorMethod<TSelf, BoundedRay>((a, b) => a.IsApproximatelyColinearWith(b, 0.5f, new Angle(10f)));

			AssertMirrorMethodWithTolerance<TSelf, Plane>((a, b) => a.AngleTo(b), TestTolerance);
			AssertMirrorMethod<TSelf, Plane>((a, b) => a.IsParallelTo(b));
			AssertMirrorMethod<TSelf, Plane>((a, b) => a.IsApproximatelyParallelTo(b));
			AssertMirrorMethod<TSelf, Plane>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));
			AssertMirrorMethod<TSelf, Plane>((a, b) => a.IsOrthogonalTo(b));
			AssertMirrorMethod<TSelf, Plane>((a, b) => a.IsApproximatelyOrthogonalTo(b));
			AssertMirrorMethod<TSelf, Plane>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));
		}

		AssertForLineType<Line>();
		AssertForLineType<Ray>();
		AssertForLineType<BoundedRay>();

		AssertMirrorMethod<Line, Line>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<Line, Ray>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<Line, BoundedRay>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<Ray, Line>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<Ray, Ray>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<Ray, BoundedRay>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<BoundedRay, Line>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<BoundedRay, Ray>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<BoundedRay, BoundedRay>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));

		AssertMirrorMethod<Line, Line>((a, b) => a.ParallelizationOf(b), (b, a) => b.ParallelizedWith(a));
		AssertMirrorMethod<Line, Ray>((a, b) => a.ParallelizationOf(b), (b, a) => b.ParallelizedWith(a));
		AssertMirrorMethod<Line, BoundedRay>((a, b) => a.ParallelizationOf(b), (b, a) => b.ParallelizedWith(a));
		AssertMirrorMethod<Ray, Line>((a, b) => a.ParallelizationOf(b), (b, a) => b.ParallelizedWith(a));
		AssertMirrorMethod<Ray, Ray>((a, b) => a.ParallelizationOf(b), (b, a) => b.ParallelizedWith(a));
		AssertMirrorMethod<Ray, BoundedRay>((a, b) => a.ParallelizationOf(b), (b, a) => b.ParallelizedWith(a));
		AssertMirrorMethod<BoundedRay, Line>((a, b) => a.ParallelizationOf(b), (b, a) => b.ParallelizedWith(a));
		AssertMirrorMethod<BoundedRay, Ray>((a, b) => a.ParallelizationOf(b), (b, a) => b.ParallelizedWith(a));
		AssertMirrorMethod<BoundedRay, BoundedRay>((a, b) => a.ParallelizationOf(b), (b, a) => b.ParallelizedWith(a));
		AssertMirrorMethod<Line, Line>((a, b) => a.OrthogonalizationOf(b), (b, a) => b.OrthogonalizedAgainst(a));
		AssertMirrorMethod<Line, Ray>((a, b) => a.OrthogonalizationOf(b), (b, a) => b.OrthogonalizedAgainst(a));
		AssertMirrorMethod<Line, BoundedRay>((a, b) => a.OrthogonalizationOf(b), (b, a) => b.OrthogonalizedAgainst(a));
		AssertMirrorMethod<Ray, Line>((a, b) => a.OrthogonalizationOf(b), (b, a) => b.OrthogonalizedAgainst(a));
		AssertMirrorMethod<Ray, Ray>((a, b) => a.OrthogonalizationOf(b), (b, a) => b.OrthogonalizedAgainst(a));
		AssertMirrorMethod<Ray, BoundedRay>((a, b) => a.OrthogonalizationOf(b), (b, a) => b.OrthogonalizedAgainst(a));
		AssertMirrorMethod<BoundedRay, Line>((a, b) => a.OrthogonalizationOf(b), (b, a) => b.OrthogonalizedAgainst(a));
		AssertMirrorMethod<BoundedRay, Ray>((a, b) => a.OrthogonalizationOf(b), (b, a) => b.OrthogonalizedAgainst(a));
		AssertMirrorMethod<BoundedRay, BoundedRay>((a, b) => a.OrthogonalizationOf(b), (b, a) => b.OrthogonalizedAgainst(a));

		for (var i = 0; i < 100; ++i) {
			var d = Direction.Random();
			AssertMirrorMethod<Line, Line>((a, b) => a.SignedAngleTo(b, d), (b, a) => {
				var s = a.SignedAngleTo(b, d);
				return s.Radians switch {
					MathF.PI => b.SignedAngleTo(a, d),
					_ => -b.SignedAngleTo(a, d)
				};
			});
			AssertMirrorMethod<Line, Ray>((a, b) => a.SignedAngleTo(b, d), (b, a) => {
				var s = a.SignedAngleTo(b, d);
				return s.Radians switch {
					MathF.PI => b.SignedAngleTo(a, d),
					_ => -b.SignedAngleTo(a, d)
				};
			});
			AssertMirrorMethod<Line, BoundedRay>((a, b) => a.SignedAngleTo(b, d), (b, a) => {
				var s = a.SignedAngleTo(b, d);
				return s.Radians switch {
					MathF.PI => b.SignedAngleTo(a, d),
					_ => -b.SignedAngleTo(a, d)
				};
			});

			AssertMirrorMethod<Ray, Line>((a, b) => a.SignedAngleTo(b, d), (b, a) => {
				var s = a.SignedAngleTo(b, d);
				return s.Radians switch {
					MathF.PI => b.SignedAngleTo(a, d),
					_ => -b.SignedAngleTo(a, d)
				};
			});
			AssertMirrorMethod<Ray, Ray>((a, b) => a.SignedAngleTo(b, d), (b, a) => {
				var s = a.SignedAngleTo(b, d);
				return s.Radians switch {
					MathF.PI => b.SignedAngleTo(a, d),
					_ => -b.SignedAngleTo(a, d)
				};
			});
			AssertMirrorMethod<Ray, BoundedRay>((a, b) => a.SignedAngleTo(b, d), (b, a) => {
				var s = a.SignedAngleTo(b, d);
				return s.Radians switch {
					MathF.PI => b.SignedAngleTo(a, d),
					_ => -b.SignedAngleTo(a, d)
				};
			});

			AssertMirrorMethod<BoundedRay, Line>((a, b) => a.SignedAngleTo(b, d), (b, a) => {
				var s = a.SignedAngleTo(b, d);
				return s.Radians switch {
					MathF.PI => b.SignedAngleTo(a, d),
					_ => -b.SignedAngleTo(a, d)
				};
			});
			AssertMirrorMethod<BoundedRay, Ray>((a, b) => a.SignedAngleTo(b, d), (b, a) => {
				var s = a.SignedAngleTo(b, d);
				return s.Radians switch {
					MathF.PI => b.SignedAngleTo(a, d),
					_ => -b.SignedAngleTo(a, d)
				};
			});
			AssertMirrorMethod<BoundedRay, BoundedRay>((a, b) => a.SignedAngleTo(b, d), (b, a) => {
				var s = a.SignedAngleTo(b, d);
				return s.Radians switch {
					MathF.PI => b.SignedAngleTo(a, d),
					_ => -b.SignedAngleTo(a, d)
				};
			});
		}
	}

	[Test]
	public void ShouldCorrectlyImplementMirrorMethodsForLocations() {
		void AssertForLineType<TLineLike>() where TLineLike : struct, ILineLike<TLineLike> {
			AssertMirrorMethod<TLineLike, Location>((a, b) => a.DistanceFrom(b));
			AssertMirrorMethod<TLineLike, Location>((a, b) => a.DistanceSquaredFrom(b));
		}

		AssertForLineType<Line>();
		AssertForLineType<Ray>();
		AssertForLineType<BoundedRay>();

		AssertMirrorMethod<Line, Location>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<Ray, Location>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<BoundedRay, Location>((a, b) => a.PointClosestTo(b), (b, a) => b.ClosestPointOn(a));
		AssertMirrorMethod<Line, Location>((a, b) => a.Contains(b), (b, a) => b.IsContainedWithin(a));
		AssertMirrorMethod<Ray, Location>((a, b) => a.Contains(b), (b, a) => b.IsContainedWithin(a));
		AssertMirrorMethod<BoundedRay, Location>((a, b) => a.Contains(b), (b, a) => b.IsContainedWithin(a));
	}

	[Test]
	public void ShouldCorrectlyImplementMirrorMethodsForDirections() {
		void AssertForLineType<TLineLike>() where TLineLike : struct, ILineLike<TLineLike> {
			AssertMirrorMethod<TLineLike, Direction>((a, b) => a.IsOrthogonalTo(b));
			AssertMirrorMethod<TLineLike, Direction>((a, b) => a.IsParallelTo(b));
			AssertMirrorMethod<TLineLike, Direction>((a, b) => a.IsApproximatelyOrthogonalTo(b));
			AssertMirrorMethod<TLineLike, Direction>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));
			AssertMirrorMethod<TLineLike, Direction>((a, b) => a.IsApproximatelyParallelTo(b));
			AssertMirrorMethod<TLineLike, Direction>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));
		}

		AssertForLineType<Line>();
		AssertForLineType<Ray>();
		AssertForLineType<BoundedRay>();

		AssertMirrorMethod<Line, Direction>((a, b) => a.OrthogonalizedAgainst(b), (b, a) => b.OrthogonalizationOf(a));
		AssertMirrorMethod<Ray, Direction>((a, b) => a.OrthogonalizedAgainst(b), (b, a) => b.OrthogonalizationOf(a));
		AssertMirrorMethod<BoundedRay, Direction>((a, b) => a.OrthogonalizedAgainst(b), (b, a) => b.OrthogonalizationOf(a));
		AssertMirrorMethod<Line, Direction>((a, b) => a.FastOrthogonalizedAgainst(b), (b, a) => b.FastOrthogonalizationOf(a));
		AssertMirrorMethod<Ray, Direction>((a, b) => a.FastOrthogonalizedAgainst(b), (b, a) => b.FastOrthogonalizationOf(a));
		AssertMirrorMethod<BoundedRay, Direction>((a, b) => a.FastOrthogonalizedAgainst(b), (b, a) => b.FastOrthogonalizationOf(a));
		AssertMirrorMethod<Line, Direction>((a, b) => a.ParallelizedWith(b), (b, a) => b.ParallelizationOf(a));
		AssertMirrorMethod<Ray, Direction>((a, b) => a.ParallelizedWith(b), (b, a) => b.ParallelizationOf(a));
		AssertMirrorMethod<BoundedRay, Direction>((a, b) => a.ParallelizedWith(b), (b, a) => b.ParallelizationOf(a));
		AssertMirrorMethod<Line, Direction>((a, b) => a.FastParallelizedWith(b), (b, a) => b.FastParallelizationOf(a));
		AssertMirrorMethod<Ray, Direction>((a, b) => a.FastParallelizedWith(b), (b, a) => b.FastParallelizationOf(a));
		AssertMirrorMethod<BoundedRay, Direction>((a, b) => a.FastParallelizedWith(b), (b, a) => b.FastParallelizationOf(a));
	}

	[Test]
	public void ShouldCorrectlyImplementMirrorMethodsForPlanes() {
		AssertMirrorMethod<Line, Plane>((l, p) => l.ProjectedOnTo(p), (p, l) => p.ProjectionOf(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.ProjectedOnTo(p), (p, l) => p.ProjectionOf(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.ProjectedOnTo(p), (p, l) => p.ProjectionOf(l));
		AssertMirrorMethod<Line, Plane>((l, p) => l.FastProjectedOnTo(p), (p, l) => p.FastProjectionOf(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.FastProjectedOnTo(p), (p, l) => p.FastProjectionOf(l));
		AssertMirrorMethod<BoundedRay, Plane, IProjectable<BoundedRay, Plane>, IProjectionTarget<BoundedRay>>((l, p) => l.ProjectedOnTo(p), (p, l) => p.ProjectionOf(l));
		AssertMirrorMethod<BoundedRay, Plane, IProjectable<BoundedRay, Plane>, IProjectionTarget<BoundedRay>>((l, p) => l.FastProjectedOnTo(p), (p, l) => p.FastProjectionOf(l));

		AssertMirrorMethod<Line, Plane>((l, p) => l.OrthogonalizedAgainst(p), (p, l) => p.OrthogonalizationOf(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.OrthogonalizedAgainst(p), (p, l) => p.OrthogonalizationOf(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.OrthogonalizedAgainst(p), (p, l) => p.OrthogonalizationOf(l));
		AssertMirrorMethod<Line, Plane>((l, p) => l.FastOrthogonalizedAgainst(p), (p, l) => p.FastOrthogonalizationOf(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.FastOrthogonalizedAgainst(p), (p, l) => p.FastOrthogonalizationOf(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.FastOrthogonalizedAgainst(p), (p, l) => p.FastOrthogonalizationOf(l));

		AssertMirrorMethod<Line, Plane>((l, p) => l.ParallelizedWith(p), (p, l) => p.ParallelizationOf(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.ParallelizedWith(p), (p, l) => p.ParallelizationOf(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.ParallelizedWith(p), (p, l) => p.ParallelizationOf(l));
		AssertMirrorMethod<Line, Plane>((l, p) => l.FastParallelizedWith(p), (p, l) => p.FastParallelizationOf(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.FastParallelizedWith(p), (p, l) => p.FastParallelizationOf(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.FastParallelizedWith(p), (p, l) => p.FastParallelizationOf(l));

		AssertMirrorMethod<Line, Plane>((l, p) => l.ReflectedBy(p), (p, l) => p.ReflectionOf(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.ReflectedBy(p), (p, l) => p.ReflectionOf(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.ReflectedBy(p), (p, l) => p.ReflectionOf(l));
		AssertMirrorMethod<Line, Plane>((l, p) => l.FastReflectedBy(p), (p, l) => p.FastReflectionOf(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.FastReflectedBy(p), (p, l) => p.FastReflectionOf(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.FastReflectedBy(p), (p, l) => p.FastReflectionOf(l));

		AssertMirrorMethod<Line, Plane>((a, b) => a.IncidentAngleWith(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.IncidentAngleWith(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.IncidentAngleWith(b));
		AssertMirrorMethod<Line, Plane>((a, b) => a.FastIncidentAngleWith(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.FastIncidentAngleWith(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.FastIncidentAngleWith(b));

		AssertMirrorMethod<Line, Plane>((a, b) => a.IsIntersectedBy(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.IsIntersectedBy(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.IsIntersectedBy(b));

		AssertMirrorMethod<Line, Plane>((l, p) => l.SplitBy(p), (p, l) => p.Split(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.SplitBy(p), (p, l) => p.Split(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.SplitBy(p), (p, l) => p.Split(l));
		AssertMirrorMethod<Line, Plane>((l, p) => l.FastSplitBy(p), (p, l) => p.FastSplit(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.FastSplitBy(p), (p, l) => p.FastSplit(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.FastSplitBy(p), (p, l) => p.FastSplit(l));

		AssertMirrorMethod<Line, Plane>((a, b) => a.IntersectionWith(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.IntersectionWith(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.IntersectionWith(b));
		AssertMirrorMethod<Line, Plane>((a, b) => a.FastIntersectionWith(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.FastIntersectionWith(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.FastIntersectionWith(b));

		AssertMirrorMethod<Line, Plane>((a, b) => a.DistanceFrom(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.DistanceFrom(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.DistanceFrom(b));
		AssertMirrorMethod<Line, Plane>((a, b) => a.SignedDistanceFrom(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.SignedDistanceFrom(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.SignedDistanceFrom(b));
		AssertMirrorMethod<Line, Plane>((a, b) => a.DistanceSquaredFrom(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.DistanceSquaredFrom(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.DistanceSquaredFrom(b));

		AssertMirrorMethod<Line, Plane>((l, p) => l.ClosestPointOn(p), (p, l) => p.PointClosestTo(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.ClosestPointOn(p), (p, l) => p.PointClosestTo(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.ClosestPointOn(p), (p, l) => p.PointClosestTo(l));
		AssertMirrorMethod<Line, Plane>((l, p) => l.PointClosestTo(p), (p, l) => p.ClosestPointOn(l));
		AssertMirrorMethod<Ray, Plane>((l, p) => l.PointClosestTo(p), (p, l) => p.ClosestPointOn(l));
		AssertMirrorMethod<BoundedRay, Plane>((l, p) => l.PointClosestTo(p), (p, l) => p.ClosestPointOn(l));

		AssertMirrorMethod<Line, Plane>((a, b) => a.RelationshipTo(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.RelationshipTo(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.RelationshipTo(b));

		AssertMirrorMethod<Line, Plane>((a, b) => a.IsOrthogonalTo(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.IsOrthogonalTo(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.IsOrthogonalTo(b));
		AssertMirrorMethod<Line, Plane>((a, b) => a.IsApproximatelyOrthogonalTo(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.IsApproximatelyOrthogonalTo(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.IsApproximatelyOrthogonalTo(b));
		AssertMirrorMethod<Line, Plane>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));

		AssertMirrorMethod<Line, Plane>((a, b) => a.IsParallelTo(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.IsParallelTo(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.IsParallelTo(b));
		AssertMirrorMethod<Line, Plane>((a, b) => a.IsApproximatelyParallelTo(b));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.IsApproximatelyParallelTo(b));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.IsApproximatelyParallelTo(b));
		AssertMirrorMethod<Line, Plane>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));
		AssertMirrorMethod<Ray, Plane>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));
		AssertMirrorMethod<BoundedRay, Plane>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));
	}
}