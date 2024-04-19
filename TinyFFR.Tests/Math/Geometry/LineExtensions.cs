// Created on 2024-03-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using NSubstitute;
using NSubstitute.Core;

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
	public void ShouldBeAbleToCompileAllExamples() {
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
			new Line().ClosestPointTo(line);
			new Line().ClosestPointTo(new Line());
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
			line.PerpendicularityWith(new Plane());
			new Ray().AngleTo(new Plane());
			new Ray().PerpendicularityWith(new Plane());

			new Plane().AngleTo(line);
			new Plane().PerpendicularityWith(line);
			new Plane().AngleTo(new Ray());
			new Plane().PerpendicularityWith(new Ray());

			new OriginSphere().DistanceFrom(line);

			new Line().IsIntersectedBy(new Plane());
			new Plane().IsIntersectedBy(new Line());
			new Line().IsIntersectedBy(new OriginCuboid());
			new OriginCuboid().IsIntersectedBy(new Line());

			line.IsIntersectedBy(new Plane());
			line.IntersectionWith(new Plane());
			new Plane().IsIntersectedBy(line);
			line.IsIntersectedBy(new OriginCuboid());
			new OriginCuboid().IsIntersectedBy(line);
		}

		try {
			Test(new Ray(), new BoundedLine());
		}
		catch { /* Don't care about any actual parameter validation exceptions or errors from executing the functions. */ }
		Assert.Pass("Compiling test is successful test.");
	}

	[Test]
	public void ShouldCorrectlyImplementMirrorMethods() {
		void AssertGenericMethodInvoked(object sub, string methodName, object genericLine) {
			Assert.AreEqual(
				genericLine,
				sub.ReceivedCalls().Single(c => c.GetMethodInfo().Name == methodName && c.GetMethodInfo().IsGenericMethod).GetArguments()[0]
			);
			sub.ClearReceivedCalls();
		}

		void ExecuteGenericLineTests<TLine>(TLine genericLine) where TLine : ILine {
			var lineSurfaceDistanceMeasurable = Substitute.For<ILineSurfaceDistanceMeasurable>();
			_ = genericLine.DistanceFrom(lineSurfaceDistanceMeasurable);
			AssertGenericMethodInvoked(lineSurfaceDistanceMeasurable, nameof(ILineDistanceMeasurable.DistanceFrom), genericLine);
			_ = genericLine.DistanceFromSurfaceOf(lineSurfaceDistanceMeasurable);
			AssertGenericMethodInvoked(lineSurfaceDistanceMeasurable, nameof(ILineSurfaceDistanceMeasurable.SurfaceDistanceFrom), genericLine);

			var lineClosestSurfacePointDiscoverable = Substitute.For<ILineClosestSurfacePointDiscoverable>();
			_ = genericLine.ClosestPointOn(lineClosestSurfacePointDiscoverable);
			AssertGenericMethodInvoked(lineClosestSurfacePointDiscoverable, nameof(ILineClosestPointDiscoverable.ClosestPointTo), genericLine);
			_ = genericLine.ClosestPointTo(lineClosestSurfacePointDiscoverable);
			AssertGenericMethodInvoked(lineClosestSurfacePointDiscoverable, nameof(ILineClosestPointDiscoverable.ClosestPointOn), genericLine);
			_ = genericLine.ClosestPointOnSurfaceOf(lineClosestSurfacePointDiscoverable);
			AssertGenericMethodInvoked(lineClosestSurfacePointDiscoverable, nameof(ILineClosestSurfacePointDiscoverable.ClosestPointOnSurfaceTo), genericLine);
			_ = genericLine.ClosestPointToSurfaceOf(lineClosestSurfacePointDiscoverable);
			AssertGenericMethodInvoked(lineClosestSurfacePointDiscoverable, nameof(ILineClosestSurfacePointDiscoverable.ClosestPointToSurfaceOn), genericLine);
		}

		ExecuteGenericLineTests(new Line(Location.Origin, Direction.Forward));

		var lineSurfaceDistanceMeasurable = Substitute.For<ILineSurfaceDistanceMeasurable>();
		var lineClosestSurfacePointDiscoverable = Substitute.For<ILineClosestSurfacePointDiscoverable>();
		var line = new Line(Location.Origin, Direction.Forward);
		var ray = new Ray(Location.Origin, Direction.Backward);
		var boundedLine = BoundedLine.FromStartPointAndVect(Location.Origin, Direction.Forward * 3f);

		_ = line.DistanceFrom(lineSurfaceDistanceMeasurable);
		_ = lineSurfaceDistanceMeasurable.Received(1).DistanceFrom(line);
		_ = lineSurfaceDistanceMeasurable.Received(0).SurfaceDistanceFrom(line);
		_ = line.DistanceFromSurfaceOf(lineSurfaceDistanceMeasurable);
		_ = lineSurfaceDistanceMeasurable.Received(1).DistanceFrom(line);
		_ = lineSurfaceDistanceMeasurable.Received(1).SurfaceDistanceFrom(line);

		_ = ray.DistanceFrom(lineSurfaceDistanceMeasurable);
		_ = lineSurfaceDistanceMeasurable.Received(1).DistanceFrom(ray);
		_ = lineSurfaceDistanceMeasurable.Received(0).SurfaceDistanceFrom(ray);
		_ = ray.DistanceFromSurfaceOf(lineSurfaceDistanceMeasurable);
		_ = lineSurfaceDistanceMeasurable.Received(1).DistanceFrom(ray);
		_ = lineSurfaceDistanceMeasurable.Received(1).SurfaceDistanceFrom(ray);

		_ = boundedLine.DistanceFrom(lineSurfaceDistanceMeasurable);
		_ = lineSurfaceDistanceMeasurable.Received(1).DistanceFrom(boundedLine);
		_ = lineSurfaceDistanceMeasurable.Received(0).SurfaceDistanceFrom(boundedLine);
		_ = boundedLine.DistanceFromSurfaceOf(lineSurfaceDistanceMeasurable);
		_ = lineSurfaceDistanceMeasurable.Received(1).DistanceFrom(boundedLine);
		_ = lineSurfaceDistanceMeasurable.Received(1).SurfaceDistanceFrom(boundedLine);

		_ = line.ClosestPointOn(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(line);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointOn(line);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointOnSurfaceTo(line);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointToSurfaceOn(line);
		_ = line.ClosestPointTo(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(line);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOn(line);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointOnSurfaceTo(line);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointToSurfaceOn(line);
		_ = line.ClosestPointOnSurfaceOf(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(line);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOn(line);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOnSurfaceTo(line);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointToSurfaceOn(line);
		_ = line.ClosestPointToSurfaceOf(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(line);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOn(line);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOnSurfaceTo(line);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointToSurfaceOn(line);

		_ = ray.ClosestPointOn(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointOn(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointOnSurfaceTo(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointToSurfaceOn(ray);
		_ = ray.ClosestPointTo(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOn(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointOnSurfaceTo(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointToSurfaceOn(ray);
		_ = ray.ClosestPointOnSurfaceOf(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOn(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOnSurfaceTo(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointToSurfaceOn(ray);
		_ = ray.ClosestPointToSurfaceOf(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOn(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOnSurfaceTo(ray);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointToSurfaceOn(ray);

		_ = boundedLine.ClosestPointOn(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointOn(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointOnSurfaceTo(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointToSurfaceOn(boundedLine);
		_ = boundedLine.ClosestPointTo(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOn(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointOnSurfaceTo(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointToSurfaceOn(boundedLine);
		_ = boundedLine.ClosestPointOnSurfaceOf(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOn(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOnSurfaceTo(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(0).ClosestPointToSurfaceOn(boundedLine);
		_ = boundedLine.ClosestPointToSurfaceOf(lineClosestSurfacePointDiscoverable);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointTo(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOn(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointOnSurfaceTo(boundedLine);
		_ = lineClosestSurfacePointDiscoverable.Received(1).ClosestPointToSurfaceOn(boundedLine);
	}
}