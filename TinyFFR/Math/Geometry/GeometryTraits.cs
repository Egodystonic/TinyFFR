// Created on 2024-03-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

// This file hosts all the trait interfaces for geometric primitives

using System.Numerics;

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
	means it doesn't interfere with some of the extension methods declared further down below. However, it's also needed for
	certain cases where we want/need to actually work with a TLine instead of a specific type (and the compiler of course
	can't know that Ray/Line/BoundedLine are the only three options), so we still expose that protected method via an internal
	static on the interface itself, which is in turn used by more generic extension methods that are public. It's all a bit
	nasty and if it causes too much trouble in the future it might be better to just dump these interfaces entirely, but then
	you will have to find another way to let types/methods automatically work for any line type (or maybe it doesn't matter...).
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
	internal static float InvokeProtectedDistanceFrom<T, TLine>(T @this, TLine line) where TLine : ILine where T : ILineDistanceMeasurable => @this.DistanceFrom(line);
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
	internal static float InvokeProtectedSurfaceDistanceFrom<T, TLine>(T @this, TLine line) where TLine : ILine where T : ILineSurfaceDistanceMeasurable => @this.SurfaceDistanceFrom(line);
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
	internal static Location InvokeProtectedClosestPointTo<T, TLine>(T @this, TLine line) where TLine : ILine where T : ILineClosestPointDiscoverable => @this.ClosestPointTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Location InvokeProtectedClosestPointOn<T, TLine>(T @this, TLine line) where TLine : ILine where T : ILineClosestPointDiscoverable => @this.ClosestPointOn(line);
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
	internal static Location InvokeProtectedClosestPointOnSurfaceTo<T, TLine>(T @this, TLine line) where TLine : ILine where T : ILineClosestSurfacePointDiscoverable => @this.ClosestPointOnSurfaceTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Location InvokeProtectedClosestPointToSurfaceOn<T, TLine>(T @this, TLine line) where TLine : ILine where T : ILineClosestSurfacePointDiscoverable => @this.ClosestPointToSurfaceOn(line);
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
	internal static TIntersection? InvokeProtectedIntersectionWith<T, TLine>(T @this, TLine line) where TLine : ILine where T : ILineIntersectable<TIntersection> => @this.IntersectionWith(line);
}
#endregion

public static class GeometryExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFrom<TGeo, T>(this TGeo @this, T element) where TGeo : IGeometryInteractable where T : IDistanceMeasurable<TGeo> => element.DistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float SignedDistanceFrom<TGeo, T>(this TGeo @this, T element) where TGeo : IGeometryInteractable where T : ISignedDistanceMeasurable<TGeo> => element.SignedDistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsContainedWithin<TGeo, T>(this TGeo @this, T element) where TGeo : IGeometryInteractable where T : IContainmentTestable<TGeo> => element.Contains(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOn<TGeo, T>(this TGeo @this, T element) where TGeo : IGeometryInteractable where T : IClosestEndogenousPointDiscoverable<TGeo> => element.ClosestPointTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointTo<TGeo, T>(this TGeo @this, T element) where TGeo : IGeometryInteractable where T : IClosestExogenousPointDiscoverable<TGeo> => element.ClosestPointOn(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFromSurfaceOf<TGeo, T>(this TGeo @this, T element) where TGeo : IGeometryInteractable where T : ISurfaceDistanceMeasurable<TGeo> => element.SurfaceDistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointToSurfaceOn<TGeo, T>(this TGeo @this, T element) where TGeo : IGeometryInteractable where T : IClosestEndogenousSurfacePointDiscoverable<TGeo> => element.ClosestPointOnSurfaceTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOnSurfaceTo<TGeo, T>(this TGeo @this, T element) where TGeo : IGeometryInteractable where T : IClosestExogenousSurfacePointDiscoverable<TGeo> => element.ClosestPointToSurfaceOn(@this);
}
// TODO partial additions for intersectable, relationship, etc. -- probably at the classes themselves