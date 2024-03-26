// Created on 2024-03-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using NSubstitute;

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
			line.DistanceFrom(new OriginSphere());
			new Plane().IntersectionWith(line);
			line.ClosestPointTo(new Ray());
			line.ClosestPointTo(line2);
			line.ClosestPointTo(new OriginSphere());
			line.ClosestPointTo(Location.Origin);
			line.ClosestPointTo(new Plane());
			new Ray().ClosestPointTo(new OriginSphere());

			new Ray().ClosestPointTo(line);
			new OriginSphere().ClosestPointTo(line);
			Location.Origin.ClosestPointOnSurfaceOf(new OriginSphere());
			new Plane().ClosestPointTo(line);

			new Plane().DistanceFrom(line);

			new OriginSphere().IntersectionWith(line);
			line.ClosestPointToSurfaceOf(new OriginSphere());
			line.ClosestPointOnSurfaceOf(new OriginSphere());

			new OriginSphere().IntersectionWith(new Ray());
			new Ray().ClosestPointToSurfaceOf(new OriginSphere());
			new Ray().ClosestPointOnSurfaceOf(new OriginSphere());

			new Plane().DistanceFrom(new OriginSphere());
			new OriginSphere().ClosestPointOnSurfaceTo(new Plane());
			new OriginSphere().ClosestPointToSurfaceOn(new Plane());
			new Plane().ClosestPointOnSurfaceOf(new OriginSphere());
			new Plane().ClosestPointToSurfaceOf(new OriginSphere());
			new OriginSphere().SurfaceDistanceFrom(new Plane());
			new Plane().DistanceFromSurfaceOf(new OriginSphere());
			new OriginSphere().SignedSurfaceDistanceFrom(new Plane());
			new Plane().SignedDistanceFromSurfaceOf(new OriginSphere());

			new Ray().ClosestPointToSurfaceOf(new OriginSphere());

			line.IntersectionWith(line2);
			line.IntersectionWith(line2, 0f);
			line.IntersectionWith(new Plane());
			line.IntersectionWith(new OriginSphere());
			new Ray().IntersectionWith(line2);
			new Ray().IntersectionWith(line2, 0f);
			new Ray().IntersectionWith(new Plane());
			new Ray().IntersectionWith(new OriginSphere());

			line.RelationshipTo(new Plane());
			new Ray().RelationshipTo(new Plane());
			new Plane().RelationshipTo(line);
			new Plane().RelationshipTo(new Ray());
			new Ray().IntersectionWith(new OriginSphere());

			new BoundedLine().IntersectionWith(line);
			new BoundedLine().IntersectionWith(line, 0f);
			new BoundedLine().IntersectionWith(new Ray());
			new BoundedLine().IntersectionWith(new Ray(), 0f);
			line.IntersectionWith(new BoundedLine());
			line.IntersectionWith(new BoundedLine(), 0f);

			Location.Origin.IsContainedWithin(new OriginSphere());
			Location.Origin.IsContainedWithin(new Ray());
			Location.Origin.IsContainedWithin(line);
			Location.Origin.IsContainedWithin(new Ray(), 0f);
			Location.Origin.IsContainedWithin(line, 0f);
			Location.Origin.DistanceFromSurfaceOf(new OriginSphere());
			Location.Origin.ClosestPointOnSurfaceOf(new OriginSphere());
			Location.Origin.DistanceFrom(new OriginSphere());

			line.AngleTo(new Plane());
			line.ParallelismWith(new Plane());
			new Ray().AngleTo(new Plane());
			new Ray().ParallelismWith(new Plane());

			new Plane().AngleTo(line);
			new Plane().ParallelismWith(line);
			new Plane().AngleTo(new Ray());
			new Plane().ParallelismWith(new Ray());

			new OriginSphere().DistanceFrom(line);
		}

		try {
			Test(new Ray(), new BoundedLine());
		}
		catch { /* Don't care about any actual parameter validation exceptions or errors from executing the functions. */ }
		Assert.Pass("Compiling test is successful test.");
	}

	[Test]
	public void ShouldCorrectlyImplementMirrorMethods() {
		var genericLine = Substitute.For<ILine>();

		var lineSurfaceDistanceMeasurable = Substitute.For<ILineSurfaceDistanceMeasurable>();
		_ = genericLine.DistanceFrom(lineSurfaceDistanceMeasurable);
		_ = lineSurfaceDistanceMeasurable.Received(1).DistanceFrom(
	}
}