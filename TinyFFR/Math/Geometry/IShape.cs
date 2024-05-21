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

public interface IShape : IGeometryPrimitive;
public interface IShape<TSelf> :
	IShape,
	IGeometryPrimitive<TSelf>,
	IScalable<TSelf>
	where TSelf : IShape<TSelf>;
public interface IConvexShape : IShape,
	ISurfaceDistanceMeasurable<Location>, IContainer<Location>, IClosestEndogenousSurfacePointDiscoverable<Location>,
	ILineSurfaceDistanceMeasurable, ILineClosestSurfacePointDiscoverable,
	ISignedSurfaceDistanceMeasurable<Plane>, IClosestSurfacePointDiscoverable<Plane>, IRelatable<Plane, PlaneObjectRelationship>,
	ILineIntersectable<ConvexShapeLineIntersection>;
public interface IConvexShape<TSelf> : 
	IConvexShape, IShape<TSelf>
	where TSelf : IConvexShape<TSelf>;