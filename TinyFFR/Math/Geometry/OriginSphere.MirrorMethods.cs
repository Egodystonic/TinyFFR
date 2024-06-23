// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

partial struct Location : IClosestExogenousSurfacePointDiscoverable<Location, OriginSphere>, IExogenousSurfaceDistanceMeasurable<Location, OriginSphere>, IContainable<Location, OriginSphere> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(OriginSphere sphere) => sphere.ClosestPointTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointToSurfaceOf(OriginSphere sphere) => 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(OriginSphere sphere) => sphere.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(OriginSphere sphere) => sphere.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf(OriginSphere sphere) => sphere.SurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf(OriginSphere sphere) => sphere.SurfaceDistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(OriginSphere sphere) => sphere.Contains(this);
}

partial struct Plane : ISignedExogenousSurfaceDistanceMeasurable<Plane, OriginSphere>, IClosestExogenousSurfacePointDiscoverable<Plane, OriginSphere>, IClosestEndogenousPointDiscoverable<Plane, OriginSphere>, IRelatable<OriginSphere, PlaneObjectRelationship> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(OriginSphere sphere) => sphere.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(OriginSphere sphere) => sphere.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(OriginSphere sphere) => sphere.SignedDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf(OriginSphere sphere) => sphere.SurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf(OriginSphere sphere) => sphere.SurfaceDistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFromSurfaceOf(OriginSphere sphere) => sphere.SignedSurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(OriginSphere sphere) => sphere.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointToSurfaceOf(OriginSphere sphere) => 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(OriginSphere sphere) => sphere.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo(OriginSphere sphere) => sphere.RelationshipTo(this);
}

partial struct Line : IReflectable<OriginSphere, Ray>, IClosestEndogenousPointDiscoverable<Line, OriginSphere>, IClosestExogenousSurfacePointDiscoverable<Line, OriginSphere>, IExogenousSurfaceDistanceMeasurable<Line, OriginSphere>, IIntersectionDeterminable<Line, OriginSphere, ConvexShapeLineIntersection> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ReflectedBy(OriginSphere sphere) => sphere.ReflectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(OriginSphere sphere) => sphere.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(OriginSphere sphere) => sphere.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointToSurfaceOf(OriginSphere sphere) => 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(OriginSphere sphere) => sphere.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(OriginSphere sphere) => sphere.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf(OriginSphere sphere) => sphere.SurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf(OriginSphere sphere) => sphere.SurfaceDistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(OriginSphere sphere) => sphere.IsIntersectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith(OriginSphere sphere) => sphere.IntersectionWith(this);
}

partial struct Ray : IReflectable<OriginSphere, Ray>, IClosestEndogenousPointDiscoverable<Ray, OriginSphere>, IClosestExogenousSurfacePointDiscoverable<Ray, OriginSphere>, IExogenousSurfaceDistanceMeasurable<Ray, OriginSphere>, IIntersectionDeterminable<Ray, OriginSphere, ConvexShapeLineIntersection> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? ReflectedBy(OriginSphere sphere) => sphere.ReflectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(OriginSphere sphere) => sphere.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(OriginSphere sphere) => sphere.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointToSurfaceOf(OriginSphere sphere) => 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(OriginSphere sphere) => sphere.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(OriginSphere sphere) => sphere.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf(OriginSphere sphere) => sphere.SurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf(OriginSphere sphere) => sphere.SurfaceDistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(OriginSphere sphere) => sphere.IsIntersectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith(OriginSphere sphere) => sphere.IntersectionWith(this);
}

partial struct BoundedRay : IReflectable<OriginSphere, BoundedRay>, IClosestEndogenousPointDiscoverable<BoundedRay, OriginSphere>, IClosestExogenousSurfacePointDiscoverable<BoundedRay, OriginSphere>, IExogenousSurfaceDistanceMeasurable<BoundedRay, OriginSphere>, IIntersectionDeterminable<BoundedRay, OriginSphere, ConvexShapeLineIntersection>, IContainable<OriginSphere> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? ReflectedBy(OriginSphere sphere) => sphere.ReflectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(OriginSphere sphere) => sphere.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(OriginSphere sphere) => sphere.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointToSurfaceOf(OriginSphere sphere) => 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(OriginSphere sphere) => sphere.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(OriginSphere sphere) => sphere.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFromSurfaceOf(OriginSphere sphere) => sphere.SurfaceDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFromSurfaceOf(OriginSphere sphere) => sphere.SurfaceDistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(OriginSphere sphere) => sphere.IsIntersectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith(OriginSphere sphere) => sphere.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(OriginSphere sphere) => sphere.Contains(this);
}