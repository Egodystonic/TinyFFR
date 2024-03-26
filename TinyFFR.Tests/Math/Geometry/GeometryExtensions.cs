// Created on 2024-03-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using NSubstitute;

namespace Egodystonic.TinyFFR;

[TestFixture]
class GeometryExtensionsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyImplementMirrorMethods() {
		var geometryInteractable = Substitute.For<IGeometryInteractable>();

		var signedDistanceMeasurable = Substitute.For<ISignedDistanceMeasurable<IGeometryInteractable>>();
		_ = geometryInteractable.SignedDistanceFrom(signedDistanceMeasurable);
		_ = signedDistanceMeasurable.Received(1).SignedDistanceFrom(geometryInteractable);
		_ = signedDistanceMeasurable.Received(0).DistanceFrom(geometryInteractable);
		_ = geometryInteractable.DistanceFrom(signedDistanceMeasurable);
		_ = signedDistanceMeasurable.Received(1).SignedDistanceFrom(geometryInteractable);
		_ = signedDistanceMeasurable.Received(1).DistanceFrom(geometryInteractable);

		var containmentTestable = Substitute.For<IContainmentTestable<IGeometryInteractable>>();
		_ = geometryInteractable.IsContainedWithin(containmentTestable);
		_ = containmentTestable.Received(1).Contains(geometryInteractable);

		var closestPointDiscoverable = Substitute.For<IClosestPointDiscoverable<IGeometryInteractable>>();
		_ = geometryInteractable.ClosestPointOn(closestPointDiscoverable);
		_ = closestPointDiscoverable.Received(1).ClosestPointTo(geometryInteractable);
		_ = closestPointDiscoverable.Received(0).ClosestPointOn(geometryInteractable);
		_ = geometryInteractable.ClosestPointTo(closestPointDiscoverable);
		_ = closestPointDiscoverable.Received(1).ClosestPointTo(geometryInteractable);
		_ = closestPointDiscoverable.Received(1).ClosestPointOn(geometryInteractable);

		var signedSurfaceDistanceMeasurable = Substitute.For<ISignedSurfaceDistanceMeasurable<IGeometryInteractable>>();
		_ = geometryInteractable.SignedDistanceFromSurfaceOf(signedSurfaceDistanceMeasurable);
		_ = signedSurfaceDistanceMeasurable.Received(1).SignedSurfaceDistanceFrom(geometryInteractable);
		_ = signedSurfaceDistanceMeasurable.Received(0).SurfaceDistanceFrom(geometryInteractable);
		_ = geometryInteractable.DistanceFromSurfaceOf(signedSurfaceDistanceMeasurable);
		_ = signedSurfaceDistanceMeasurable.Received(1).SignedSurfaceDistanceFrom(geometryInteractable);
		_ = signedSurfaceDistanceMeasurable.Received(1).SurfaceDistanceFrom(geometryInteractable);

		var closestSurfacePointDiscoverable = Substitute.For<IClosestSurfacePointDiscoverable<IGeometryInteractable>>();
		_ = geometryInteractable.ClosestPointOnSurfaceOf(closestSurfacePointDiscoverable);
		_ = closestSurfacePointDiscoverable.Received(1).ClosestPointOnSurfaceTo(geometryInteractable);
		_ = closestSurfacePointDiscoverable.Received(0).ClosestPointToSurfaceOn(geometryInteractable);
		_ = geometryInteractable.ClosestPointToSurfaceOf(closestSurfacePointDiscoverable);
		_ = closestSurfacePointDiscoverable.Received(1).ClosestPointOnSurfaceTo(geometryInteractable);
		_ = closestSurfacePointDiscoverable.Received(1).ClosestPointToSurfaceOn(geometryInteractable);
	}
}