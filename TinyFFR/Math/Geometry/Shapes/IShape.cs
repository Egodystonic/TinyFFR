// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public enum PlaneObjectRelationship {
	PlaneIntersectsObject,
	PlaneFacesTowardsObject,
	PlaneFacesAwayFromObject
}

public readonly record struct ConvexShapeLineIntersection(Location First, Location? Second) {
	public Location First { get; } = First;
	public Location? Second { get; } = Second;
	
	public static ConvexShapeLineIntersection? FromTwoPotentiallyNullArgs(Location? a, Location? b) {
		return (a, b) switch {
			(not null, _) => new(a.Value, b),
			(null, not null) => new(b.Value, a),
			_ => null
		};
	}
}

public interface IShape : IMathPrimitive;
public interface IShape<TSelf> :
	IShape,
	IMathPrimitive<TSelf>,
	IScalable<TSelf>
	where TSelf : IShape<TSelf>;
public interface IConvexShape : IShape,
	IClosestEndogenousPointDiscoverable<Location>,
	IDistanceMeasurable<Location>,
	IContainer<Location>,

	ILineReflectionTarget, 
	ILineClosestExogenousPointDiscoverable,
	ILineClosestEndogenousPointDiscoverable,
	ILineDistanceMeasurable, 
	IContainer<BoundedRay>,
	ILineIntersectionDeterminable<ConvexShapeLineIntersection>,

	ISignedDistanceMeasurable<Plane>,
	IClosestEndogenousPointDiscoverable<Plane>,
	IClosestExogenousPointDiscoverable<Plane>,
	IRelatable<Plane, PlaneObjectRelationship> {
	Angle? IncidentAngleTo(Line line);
	Angle? IncidentAngleTo(Ray ray);
	Angle? IncidentAngleTo(BoundedRay ray);

	Location SurfacePointClosestTo(Location point);
	float SurfaceDistanceFrom(Location point);
	float SurfaceDistanceSquaredFrom(Location point);

	Location SurfacePointClosestTo(Line line);
	Location ClosestPointToSurfaceOn(Line line);
	float SurfaceDistanceFrom(Line line);
	float SurfaceDistanceSquaredFrom(Line line);

	Location SurfacePointClosestTo(Ray ray);
	Location ClosestPointToSurfaceOn(Ray ray);
	float SurfaceDistanceFrom(Ray ray);
	float SurfaceDistanceSquaredFrom(Ray ray);

	Location SurfacePointClosestTo(BoundedRay ray);
	Location ClosestPointToSurfaceOn(BoundedRay ray);
	float SurfaceDistanceFrom(BoundedRay ray);
	float SurfaceDistanceSquaredFrom(BoundedRay ray);

	Location SurfacePointClosestTo(Plane plane);
	Location ClosestPointToSurfaceOn(Plane plane);
	float SurfaceDistanceFrom(Plane plane);
	float SurfaceDistanceSquaredFrom(Plane plane);
}

public interface IConvexShape<TSelf> : IConvexShape, IShape<TSelf> where TSelf : IConvexShape<TSelf>;