// Created on 2024-08-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

[TestFixture]
class ShapeInterfaceTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyImplementMirrorMethods() {
		void AssertForConvexShape<TShape>() where TShape : IConvexShape<TShape> {
			AssertMirrorMethod<TShape, Location>((s, l) => s.PointClosestTo(l), (l, s) => l.ClosestPointInsideOf(s));
			AssertMirrorMethod<TShape, Location>((s, l) => s.SurfacePointClosestTo(l), (l, s) => l.ClosestPointOnSurfaceOf(s));
			Assert.AreEqual(new Location(1f, 2f, 3f), ((IClosestConvexShapePointsDiscoverable) new Location(1f, 2f, 3f)).PointClosestTo(TShape.Random()));
			Assert.AreEqual(new Location(1f, 2f, 3f), ((IClosestConvexShapePointsDiscoverable) new Location(1f, 2f, 3f)).PointClosestToSurfaceOf(TShape.Random()));
			AssertMirrorMethod<TShape, Location>((s, l) => s.DistanceFrom(l), (l, s) => l.DistanceFrom(s));
			AssertMirrorMethod<TShape, Location>((s, l) => s.SurfaceDistanceFrom(l), (l, s) => l.DistanceFromSurfaceOf(s));
			AssertMirrorMethod<TShape, Location>((s, l) => s.DistanceSquaredFrom(l), (l, s) => l.DistanceSquaredFrom(s));
			AssertMirrorMethod<TShape, Location>((s, l) => s.SurfaceDistanceSquaredFrom(l), (l, s) => l.DistanceSquaredFromSurfaceOf(s));

			AssertMirrorMethod<TShape, Plane>((s, p) => s.PointClosestTo(p), (p, s) => p.ClosestPointInsideOf(s));
			AssertMirrorMethod<TShape, Plane>((s, p) => s.SurfacePointClosestTo(p), (p, s) => p.ClosestPointOnSurfaceOf(s));
			AssertMirrorMethod<TShape, Plane>((s, p) => s.ClosestPointOn(p), (p, s) => p.PointClosestTo(s));
			AssertMirrorMethod<TShape, Plane>((s, p) => s.ClosestPointToSurfaceOn(p), (p, s) => p.PointClosestToSurfaceOf(s));
			AssertMirrorMethod<TShape, Plane>((s, p) => s.DistanceFrom(p), (p, s) => p.DistanceFrom(s));
			AssertMirrorMethod<TShape, Plane>((s, p) => s.SurfaceDistanceFrom(p), (p, s) => p.DistanceFromSurfaceOf(s));
			AssertMirrorMethod<TShape, Plane>((s, p) => s.DistanceSquaredFrom(p), (p, s) => p.DistanceSquaredFrom(s));
			AssertMirrorMethod<TShape, Plane>((s, p) => s.SurfaceDistanceSquaredFrom(p), (p, s) => p.DistanceSquaredFromSurfaceOf(s));

			AssertMirrorMethod<TShape, Line>((s, l) => s.PointClosestTo(l), (l, s) => l.ClosestPointInsideOf(s));
			AssertMirrorMethod<TShape, Line>((s, l) => s.SurfacePointClosestTo(l), (l, s) => l.ClosestPointOnSurfaceOf(s));
			AssertMirrorMethod<TShape, Line>((s, l) => s.ClosestPointOn(l), (l, s) => l.PointClosestTo(s));
			AssertMirrorMethod<TShape, Line>((s, l) => s.ClosestPointToSurfaceOn(l), (l, s) => l.PointClosestToSurfaceOf(s));
			AssertMirrorMethod<TShape, Line>((s, l) => s.DistanceFrom(l), (l, s) => l.DistanceFrom(s));
			AssertMirrorMethod<TShape, Line>((s, l) => s.SurfaceDistanceFrom(l), (l, s) => l.DistanceFromSurfaceOf(s));
			AssertMirrorMethod<TShape, Line>((s, l) => s.DistanceSquaredFrom(l), (l, s) => l.DistanceSquaredFrom(s));
			AssertMirrorMethod<TShape, Line>((s, l) => s.SurfaceDistanceSquaredFrom(l), (l, s) => l.DistanceSquaredFromSurfaceOf(s));
			AssertMirrorMethod<TShape, Line>((a, b) => a.IncidentAngleWith(b));
			AssertMirrorMethod<TShape, Line>((a, b) => a.FastIncidentAngleWith(b));
			AssertMirrorMethod<TShape, Line>((a, b) => a.IntersectionWith(b));
			AssertMirrorMethod<TShape, Line>((a, b) => a.FastIntersectionWith(b));

			AssertMirrorMethod<TShape, Ray>((s, l) => s.PointClosestTo(l), (l, s) => l.ClosestPointInsideOf(s));
			AssertMirrorMethod<TShape, Ray>((s, l) => s.SurfacePointClosestTo(l), (l, s) => l.ClosestPointOnSurfaceOf(s));
			AssertMirrorMethod<TShape, Ray>((s, l) => s.ClosestPointOn(l), (l, s) => l.PointClosestTo(s));
			AssertMirrorMethod<TShape, Ray>((s, l) => s.ClosestPointToSurfaceOn(l), (l, s) => l.PointClosestToSurfaceOf(s));
			AssertMirrorMethod<TShape, Ray>((s, l) => s.DistanceFrom(l), (l, s) => l.DistanceFrom(s));
			AssertMirrorMethod<TShape, Ray>((s, l) => s.SurfaceDistanceFrom(l), (l, s) => l.DistanceFromSurfaceOf(s));
			AssertMirrorMethod<TShape, Ray>((s, l) => s.DistanceSquaredFrom(l), (l, s) => l.DistanceSquaredFrom(s));
			AssertMirrorMethod<TShape, Ray>((s, l) => s.SurfaceDistanceSquaredFrom(l), (l, s) => l.DistanceSquaredFromSurfaceOf(s));
			AssertMirrorMethod<TShape, Ray>((a, b) => a.IncidentAngleWith(b));
			AssertMirrorMethod<TShape, Ray>((a, b) => a.FastIncidentAngleWith(b));
			AssertMirrorMethod<TShape, Ray>((a, b) => a.IntersectionWith(b));
			AssertMirrorMethod<TShape, Ray>((a, b) => a.FastIntersectionWith(b));

			AssertMirrorMethod<TShape, BoundedRay>((s, l) => s.PointClosestTo(l), (l, s) => l.ClosestPointInsideOf(s));
			AssertMirrorMethod<TShape, BoundedRay>((s, l) => s.SurfacePointClosestTo(l), (l, s) => l.ClosestPointOnSurfaceOf(s));
			AssertMirrorMethod<TShape, BoundedRay>((s, l) => s.ClosestPointOn(l), (l, s) => l.PointClosestTo(s));
			AssertMirrorMethod<TShape, BoundedRay>((s, l) => s.ClosestPointToSurfaceOn(l), (l, s) => l.PointClosestToSurfaceOf(s));
			AssertMirrorMethod<TShape, BoundedRay>((s, l) => s.DistanceFrom(l), (l, s) => l.DistanceFrom(s));
			AssertMirrorMethod<TShape, BoundedRay>((s, l) => s.SurfaceDistanceFrom(l), (l, s) => l.DistanceFromSurfaceOf(s));
			AssertMirrorMethod<TShape, BoundedRay>((s, l) => s.DistanceSquaredFrom(l), (l, s) => l.DistanceSquaredFrom(s));
			AssertMirrorMethod<TShape, BoundedRay>((s, l) => s.SurfaceDistanceSquaredFrom(l), (l, s) => l.DistanceSquaredFromSurfaceOf(s));
			AssertMirrorMethod<TShape, BoundedRay>((a, b) => a.IncidentAngleWith(b));
			AssertMirrorMethod<TShape, BoundedRay>((a, b) => a.FastIncidentAngleWith(b));
			AssertMirrorMethod<TShape, BoundedRay>((a, b) => a.IntersectionWith(b));
			AssertMirrorMethod<TShape, BoundedRay>((a, b) => a.FastIntersectionWith(b));
		}

		AssertForConvexShape<SphereDescriptor>();
		AssertForConvexShape<CuboidDescriptor>();
	}
}