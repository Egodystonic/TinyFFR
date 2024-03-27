// Created on 2024-03-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

// This file hosts all the trait interfaces for geometric primitives

namespace Egodystonic.TinyFFR;

#region Primitive building block trait interfaces
public interface IGeometryInteractable; // Tag interface for extension methods

public interface IDistanceMeasurable<in T> {
	float DistanceFrom(T element);
}
public interface ISignedDistanceMeasurable<in T> : IDistanceMeasurable<T> {
	float SignedDistanceFrom(T element);
}
public interface IContainmentTestable<in T> {
	bool Contains(T element);
}

public interface IClosestEndogenousPointDiscoverable<in T> {
	Location ClosestPointTo(T element);
}
public interface IClosestExogenousPointDiscoverable<in T> {
	Location ClosestPointOn(T element);
}
public interface IClosestPointDiscoverable<in T> : IClosestEndogenousPointDiscoverable<T>, IClosestExogenousPointDiscoverable<T>;

public interface IIntersectable<in T, TIntersection> where TIntersection : struct {
	TIntersection? IntersectionWith(T element);
}
public interface IRelationshipDeterminable<in T, out TRelationship> {
	TRelationship RelationshipTo(T element);
}

public interface ISurfaceDistanceMeasurable<in T> : IDistanceMeasurable<T> {
	float SurfaceDistanceFrom(T element);
}
public interface ISignedSurfaceDistanceMeasurable<in T> : ISignedDistanceMeasurable<T>, ISurfaceDistanceMeasurable<T> {
	float SignedSurfaceDistanceFrom(T element);
}
public interface IClosestEndogenousSurfacePointDiscoverable<in T> : IClosestEndogenousPointDiscoverable<T> {
	Location ClosestPointOnSurfaceTo(T element);
}
public interface IClosestExogenousSurfacePointDiscoverable<in T> : IClosestExogenousPointDiscoverable<T> {
	Location ClosestPointToSurfaceOn(T element);
}
public interface IClosestSurfacePointDiscoverable<in T> : IClosestPointDiscoverable<T>, IClosestEndogenousSurfacePointDiscoverable<T>, IClosestExogenousSurfacePointDiscoverable<T>;
#endregion

#region Line composites
/*  The reason these interfaces exist is to make it possible to implement the same algorithm for all line types generically,
	and also declare the requisite traits for those line types all in one go. Making the generic method protected
	means it doesn't interfere with some of the generic methods with the same name on other interfaces. However, it's also needed for
	certain cases where we want/need to actually work with a TLine instead of a specific type (and the compiler of course
	can't know that Ray/Line/BoundedLine are the only three options), so we still expose that protected method via a public
	static on the interface itself, which is in turn used by more generic extension methods that are public. It's all a bit
	nasty and if it causes too much trouble in the future it might be better to just dump these interfaces entirely, but then
	you will have to find another way to let types/methods automatically work for any line type (or maybe it doesn't matter...).
	Also, on types that I implement in this library that implement these interfaces I re-declare the generic method as public so
	the end-user shouldn't really know or care about any of this.

	Or maybe we'll get HKT, traits, or macros proper in C#. Or maybe I did this all in a completely dumb way, I can't see the wood
	for the trees, and someone will look at this one day and be like "why did this dumb fucker do this like this" and fix it all.
*/
public interface ILineDistanceMeasurable : IDistanceMeasurable<Line>, IDistanceMeasurable<Ray>, IDistanceMeasurable<BoundedLine> {
	protected float DistanceFrom<TLine>(TLine line) where TLine : ILine;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IDistanceMeasurable<Line>.DistanceFrom(Line line) => DistanceFrom(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IDistanceMeasurable<Ray>.DistanceFrom(Ray line) => DistanceFrom(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IDistanceMeasurable<BoundedLine>.DistanceFrom(BoundedLine line) => DistanceFrom(line);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetDistanceFromGenericLine<T, TLine>(T lineDistanceMeasurable, TLine line) where TLine : ILine where T : ILineDistanceMeasurable => lineDistanceMeasurable.DistanceFrom(line);
}
public interface ILineSurfaceDistanceMeasurable : ILineDistanceMeasurable, ISurfaceDistanceMeasurable<Line>, ISurfaceDistanceMeasurable<Ray>, ISurfaceDistanceMeasurable<BoundedLine> {
	protected float SurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISurfaceDistanceMeasurable<Line>.SurfaceDistanceFrom(Line line) => SurfaceDistanceFrom(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISurfaceDistanceMeasurable<Ray>.SurfaceDistanceFrom(Ray line) => SurfaceDistanceFrom(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISurfaceDistanceMeasurable<BoundedLine>.SurfaceDistanceFrom(BoundedLine line) => SurfaceDistanceFrom(line);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetSurfaceDistanceFromGenericLine<T, TLine>(T lineSurfaceDistanceMeasurable, TLine line) where TLine : ILine where T : ILineSurfaceDistanceMeasurable => lineSurfaceDistanceMeasurable.SurfaceDistanceFrom(line);
}
public interface ILineClosestPointDiscoverable : IClosestPointDiscoverable<Line>, IClosestPointDiscoverable<Ray>, IClosestPointDiscoverable<BoundedLine> {
	protected Location ClosestPointTo<TLine>(TLine line) where TLine : ILine;
	protected Location ClosestPointOn<TLine>(TLine line) where TLine : ILine;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousPointDiscoverable<Line>.ClosestPointTo(Line line) => ClosestPointTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousPointDiscoverable<Line>.ClosestPointOn(Line line) => ClosestPointOn(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousPointDiscoverable<Ray>.ClosestPointTo(Ray line) => ClosestPointTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousPointDiscoverable<Ray>.ClosestPointOn(Ray line) => ClosestPointOn(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousPointDiscoverable<BoundedLine>.ClosestPointTo(BoundedLine line) => ClosestPointTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousPointDiscoverable<BoundedLine>.ClosestPointOn(BoundedLine line) => ClosestPointOn(line);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location GetClosestPointToGenericLine<T, TLine>(T lineClosestPointDiscoverable, TLine line) where TLine : ILine where T : ILineClosestPointDiscoverable => lineClosestPointDiscoverable.ClosestPointTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location GetClosestPointOnGenericLine<T, TLine>(T lineClosestPointDiscoverable, TLine line) where TLine : ILine where T : ILineClosestPointDiscoverable => lineClosestPointDiscoverable.ClosestPointOn(line);
}
public interface ILineClosestSurfacePointDiscoverable : ILineClosestPointDiscoverable, IClosestSurfacePointDiscoverable<Line>, IClosestSurfacePointDiscoverable<Ray>, IClosestSurfacePointDiscoverable<BoundedLine> {
	protected Location ClosestPointOnSurfaceTo<TLine>(TLine line) where TLine : ILine;
	protected Location ClosestPointToSurfaceOn<TLine>(TLine line) where TLine : ILine;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousSurfacePointDiscoverable<Line>.ClosestPointOnSurfaceTo(Line line) => ClosestPointOnSurfaceTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousSurfacePointDiscoverable<Line>.ClosestPointToSurfaceOn(Line line) => ClosestPointToSurfaceOn(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousSurfacePointDiscoverable<Ray>.ClosestPointOnSurfaceTo(Ray line) => ClosestPointOnSurfaceTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousSurfacePointDiscoverable<Ray>.ClosestPointToSurfaceOn(Ray line) => ClosestPointToSurfaceOn(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousSurfacePointDiscoverable<BoundedLine>.ClosestPointOnSurfaceTo(BoundedLine line) => ClosestPointOnSurfaceTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousSurfacePointDiscoverable<BoundedLine>.ClosestPointToSurfaceOn(BoundedLine line) => ClosestPointToSurfaceOn(line);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location GetClosestPointOnSurfaceToGenericLine<T, TLine>(T lineClosestSurfacePointDiscoverable, TLine line) where TLine : ILine where T : ILineClosestSurfacePointDiscoverable => lineClosestSurfacePointDiscoverable.ClosestPointOnSurfaceTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location GetClosestPointToSurfaceOnGenericLine<T, TLine>(T lineClosestSurfacePointDiscoverable, TLine line) where TLine : ILine where T : ILineClosestSurfacePointDiscoverable => lineClosestSurfacePointDiscoverable.ClosestPointToSurfaceOn(line);
}
public interface ILineIntersectable<TIntersection> : IIntersectable<Line, TIntersection>, IIntersectable<Ray, TIntersection>, IIntersectable<BoundedLine, TIntersection> where TIntersection : struct {
	protected TIntersection? IntersectionWith<TLine>(TLine line) where TLine : ILine;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	TIntersection? IIntersectable<Line, TIntersection>.IntersectionWith(Line line) => IntersectionWith(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	TIntersection? IIntersectable<Ray, TIntersection>.IntersectionWith(Ray line) => IntersectionWith(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	TIntersection? IIntersectable<BoundedLine, TIntersection>.IntersectionWith(BoundedLine line) => IntersectionWith(line);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TIntersection? GetIntersectionWithGenericLine<T, TLine>(T lineIntersectable, TLine line) where TLine : ILine where T : ILineIntersectable<TIntersection> => lineIntersectable.IntersectionWith(line);
}
#endregion

// These extensions automatically implement the "reverse"/mirror implementation between types
// (e.g. if T implements IDistanceMeasurable<TGeo>, we now get TGeo.DistanceFrom(T) for free as well as the existing T.DistanceFrom(TGeo)).
public static class GeometryExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFrom<TGeo, T>(this TGeo @this, T geometricPrimitive) where TGeo : IGeometryInteractable where T : IDistanceMeasurable<TGeo> => geometricPrimitive.DistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float SignedDistanceFrom<TGeo, T>(this TGeo @this, T geometricPrimitive) where TGeo : IGeometryInteractable where T : ISignedDistanceMeasurable<TGeo> => geometricPrimitive.SignedDistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsContainedWithin<TGeo, T>(this TGeo @this, T geometricPrimitive) where TGeo : IGeometryInteractable where T : IContainmentTestable<TGeo> => geometricPrimitive.Contains(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOn<TGeo, T>(this TGeo @this, T geometricPrimitive) where TGeo : IGeometryInteractable where T : IClosestEndogenousPointDiscoverable<TGeo> => geometricPrimitive.ClosestPointTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointTo<TGeo, T>(this TGeo @this, T geometricPrimitive) where TGeo : IGeometryInteractable where T : IClosestExogenousPointDiscoverable<TGeo> => geometricPrimitive.ClosestPointOn(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFromSurfaceOf<TGeo, T>(this TGeo @this, T geometricPrimitive) where TGeo : IGeometryInteractable where T : ISurfaceDistanceMeasurable<TGeo> => geometricPrimitive.SurfaceDistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float SignedDistanceFromSurfaceOf<TGeo, T>(this TGeo @this, T geometricPrimitive) where TGeo : IGeometryInteractable where T : ISignedSurfaceDistanceMeasurable<TGeo> => geometricPrimitive.SignedSurfaceDistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOnSurfaceOf<TGeo, T>(this TGeo @this, T geometricPrimitive) where TGeo : IGeometryInteractable where T : IClosestEndogenousSurfacePointDiscoverable<TGeo> => geometricPrimitive.ClosestPointOnSurfaceTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointToSurfaceOf<TGeo, T>(this TGeo @this, T geometricPrimitive) where TGeo : IGeometryInteractable where T : IClosestExogenousSurfacePointDiscoverable<TGeo> => geometricPrimitive.ClosestPointToSurfaceOn(@this);
}