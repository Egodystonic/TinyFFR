// Created on 2024-03-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

[TestFixture]
class LineExtensionsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	// Test is a pass if it compiles; this is essentially making sure it's possible to make all these function calls.
	// Some of these are arguably more tests of GeometryExtensions, but improperly implemented LineExtension methods can
	// interfere with those (and those are much easier to make compile correctly), so it's really actually a test of LineExtensions.
	[Test]
	public void ShouldBeAbleToCompileAllOfTheFollowing() {
		static void Test<TLine, TLine2>(TLine line, TLine2 line2) where TLine : ILine where TLine2 : ILine {
			line.DistanceFrom(new Sphere());
			new Plane().IntersectionWith(line);
			line.ClosestPointTo(new Ray());
			line.ClosestPointTo(line2);
			line.ClosestPointTo(new Sphere());
			line.ClosestPointTo(Location.Origin);
			line.ClosestPointTo(new Plane());
			new Ray().ClosestPointTo(new Sphere());

			new Ray().ClosestPointTo(line);
			new Sphere().ClosestPointTo(line);
			Location.Origin.ClosestPointOnSurfaceOf(new Sphere());
			new Plane().ClosestPointTo(line);

			new Plane().DistanceFrom(line);

			new Sphere().IntersectionWith(line);
			line.ClosestPointToSurfaceOf(new Sphere());
			line.ClosestPointOnSurfaceOf(new Sphere());

			new Sphere().IntersectionWith(new Ray());
			new Ray().ClosestPointToSurfaceOf(new Sphere());
			new Ray().ClosestPointOnSurfaceOf(new Sphere());

			new Plane().DistanceFrom(new Sphere());
			new Sphere().ClosestPointOnSurfaceTo(new Plane());
			new Sphere().ClosestPointToSurfaceOn(new Plane());
			new Plane().ClosestPointOnSurfaceOf(new Sphere());
			new Plane().ClosestPointToSurfaceOf(new Sphere());
			new Sphere().SurfaceDistanceFrom(new Plane());
			new Plane().DistanceFromSurfaceOf(new Sphere());
			new Sphere().SignedSurfaceDistanceFrom(new Plane());
			new Plane().SignedDistanceFromSurfaceOf(new Sphere());

			new Ray().ClosestPointToSurfaceOf(new Sphere());

			line.IntersectionWith(line2);
			line.IntersectionWith(line2, 0f);
			line.IntersectionWith(new Plane());
			line.IntersectionWith(new Sphere());
			new Ray().IntersectionWith(line2);
			new Ray().IntersectionWith(line2, 0f);
			new Ray().IntersectionWith(new Plane());
			new Ray().IntersectionWith(new Sphere());

			line.RelationshipTo(new Plane());
			new Ray().RelationshipTo(new Plane());
			new Plane().RelationshipTo(line);
			new Plane().RelationshipTo(new Ray());
			new Ray().IntersectionWith(new Sphere());

			new BoundedLine().IntersectionWith(line);
			new BoundedLine().IntersectionWith(line, 0f);
			new BoundedLine().IntersectionWith(new Ray());
			new BoundedLine().IntersectionWith(new Ray(), 0f);
			line.IntersectionWith(new BoundedLine());
			line.IntersectionWith(new BoundedLine(), 0f);

			Location.Origin.IsContainedWithin(new Sphere());
			Location.Origin.IsContainedWithin(new Ray());
			Location.Origin.IsContainedWithin(line);
			Location.Origin.IsContainedWithin(new Ray(), 0f);
			Location.Origin.IsContainedWithin(line, 0f);
			Location.Origin.DistanceFromSurfaceOf(new Sphere());
			Location.Origin.ClosestPointOnSurfaceOf(new Sphere());
			Location.Origin.DistanceFrom(new Sphere());

			line.AngleTo(new Plane());
			line.ParallelismWith(new Plane());
			new Ray().AngleTo(new Plane());
			new Ray().ParallelismWith(new Plane());

			new Plane().AngleTo(line);
			new Plane().ParallelismWith(line);
			new Plane().AngleTo(new Ray());
			new Plane().ParallelismWith(new Ray());
		}

		try {
			Test(new Ray(), new BoundedLine());
		}
		catch { /* Don't care about any actual parameter validation exceptions or errors from executing the functions. */}
		Assert.Pass("Compiling test is successful test.");
	}
}