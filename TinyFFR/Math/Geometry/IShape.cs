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
	IClosestEndogenousSurfacePointDiscoverable<Location>,
	IEndogenousSurfaceDistanceMeasurable<Location>,
	IContainer<Location>,

	ILineReflectionTarget, 
	ILineClosestExogenousPointDiscoverable,
	ILineClosestEndogenousSurfacePointDiscoverable,
	ILineEndogenousSurfaceDistanceMeasurable, 
	IContainer<BoundedRay>,
	ILineIntersectionDeterminable<ConvexShapeLineIntersection>,

	ISignedEndogenousSurfaceDistanceMeasurable<Plane>,
	IClosestEndogenousSurfacePointDiscoverable<Plane>,
	IClosestExogenousPointDiscoverable<Plane>,
	IRelatable<Plane, PlaneObjectRelationship> {
	Angle? IncidentAngleTo(Line line);
	Angle? IncidentAngleTo(Ray ray);
	Angle? IncidentAngleTo(BoundedRay ray);
}

public interface IConvexShape<TSelf> : IConvexShape, IShape<TSelf> where TSelf : IConvexShape<TSelf>;