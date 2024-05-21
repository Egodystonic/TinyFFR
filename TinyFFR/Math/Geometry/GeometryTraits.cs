// Created on 2024-03-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

// This file hosts all the trait interfaces for geometric primitives

namespace Egodystonic.TinyFFR;

#region Scale/Rotate/Translate
public interface IScalable<TSelf> :
	IMultiplicative<TSelf, float, TSelf>
	where TSelf : IScalable<TSelf> {
	TSelf IMultiplicative<TSelf, float, TSelf>.MultipliedBy(float scalar) => ScaledBy(scalar);
	TSelf IMultiplicative<TSelf, float, TSelf>.DividedBy(float scalar) => ScaledBy(1f / scalar);
	TSelf ScaledBy(float scalar);
}

public interface IIndependentAxisScalable<TSelf> :
	IScalable<TSelf>,
	IMultiplicative<TSelf, Vect, TSelf>
	where TSelf : IIndependentAxisScalable<TSelf> {
	TSelf IMultiplicative<TSelf, Vect, TSelf>.MultipliedBy(Vect vect) => ScaledBy(vect);
	TSelf IMultiplicative<TSelf, Vect, TSelf>.DividedBy(Vect vect) => ScaledBy(vect.Reciprocal);
	TSelf ScaledBy(Vect vect);
}

public interface ITranslatable<TSelf> :
	IAdditive<TSelf, Vect, TSelf>
	where TSelf : ITranslatable<TSelf> {
	TSelf IAdditive<TSelf, Vect, TSelf>.Plus(Vect v) => MovedBy(v);
	TSelf IAdditive<TSelf, Vect, TSelf>.Minus(Vect v) => MovedBy(-v);
	TSelf MovedBy(Vect v);
}

public interface IRotatable<TSelf> :
	IMultiplyOperators<TSelf, Rotation, TSelf>
	where TSelf : IRotatable<TSelf> {
	static abstract TSelf operator *(Rotation left, TSelf right);
	TSelf RotatedBy(Rotation rot);
}

public interface IPointRotatable<TSelf> :
	IMultiplyOperators<TSelf, (Rotation Rotation, Location Pivot), TSelf>,
	IMultiplyOperators<TSelf, (Location Pivot, Rotation Rotation), TSelf>
	where TSelf : IPointRotatable<TSelf> {
	static abstract TSelf operator *((Rotation Rotation, Location Pivot) left, TSelf right);
	static abstract TSelf operator *((Location Pivot, Rotation Rotation) left, TSelf right);
	TSelf RotatedAroundPoint(Rotation rot, Location pivot);
}
#endregion

#region Angle/projection/parallelization/orthogonalization
public interface IAngleMeasurable<in TOther> {
	Angle AngleTo(TOther other);
}
public interface ILineAngleMeasurable : IAngleMeasurable<Line>, IAngleMeasurable<Ray>, IAngleMeasurable<BoundedLine> {
	Angle AngleTo<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Angle IAngleMeasurable<Line>.AngleTo(Line other) => AngleTo(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Angle IAngleMeasurable<Ray>.AngleTo(Ray other) => AngleTo(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Angle IAngleMeasurable<BoundedLine>.AngleTo(BoundedLine other) => AngleTo(other);
}
public interface IAngleMeasurable<in TSelf, in TOther> : IAngleMeasurable<TOther> where TSelf : IAngleMeasurable<TSelf, TOther> where TOther : IAngleMeasurable<TSelf> {
	static abstract Angle operator ^(TSelf self, TOther other);
	static abstract Angle operator ^(TOther other, TSelf self);
}


public interface IProjectable<out TSelf, in TOther> {
	TSelf ProjectedOnTo(TOther element);
}
public interface IProjectionTarget<TOther> {
	TOther ProjectionOf(TOther element);
}
public interface ILineProjectionTarget : IProjectionTarget<Line>, IProjectionTarget<Ray>, IProjectionTarget<BoundedLine> {
	TLine ProjectionOf<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Line IProjectionTarget<Line>.ProjectionOf(Line element) => ProjectionOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Ray IProjectionTarget<Ray>.ProjectionOf(Ray element) => ProjectionOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	BoundedLine IProjectionTarget<BoundedLine>.ProjectionOf(BoundedLine element) => ProjectionOf(element);
}
interface IProjectionTarget<in TSelf, TOther> : IProjectionTarget<TOther> where TOther : IProjectable<TOther, TSelf> where TSelf : IProjectionTarget<TOther>;


public interface IParallelizable<out TSelf, in TOther> {
	TSelf ParallelizedWith(TOther element);
}
public interface IParallelizationTarget<TOther> {
	TOther ParallelizationOf(TOther element);
}
public interface ILineParallelizationTarget : IParallelizationTarget<Line>, IParallelizationTarget<Ray>, IParallelizationTarget<BoundedLine> {
	TLine ParallelizationOf<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Line IParallelizationTarget<Line>.ParallelizationOf(Line element) => ParallelizationOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Ray IParallelizationTarget<Ray>.ParallelizationOf(Ray element) => ParallelizationOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	BoundedLine IParallelizationTarget<BoundedLine>.ParallelizationOf(BoundedLine element) => ParallelizationOf(element);
}
interface IParallelizationTarget<in TSelf, TOther> : IParallelizationTarget<TOther> where TOther : IParallelizable<TOther, TSelf> where TSelf : IParallelizationTarget<TOther>;


public interface IOrthogonalizable<out TSelf, in TOther> {
	TSelf OrthogonalizedAgainst(TOther element);
}
public interface IOrthogonalizationTarget<TOther> {
	TOther OrthogonalizationOf(TOther element);
}
public interface ILineOrthogonalizationTarget : IOrthogonalizationTarget<Line>, IOrthogonalizationTarget<Ray>, IOrthogonalizationTarget<BoundedLine> {
	TLine OrthogonalizationOf<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Line IOrthogonalizationTarget<Line>.OrthogonalizationOf(Line element) => OrthogonalizationOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Ray IOrthogonalizationTarget<Ray>.OrthogonalizationOf(Ray element) => OrthogonalizationOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	BoundedLine IOrthogonalizationTarget<BoundedLine>.OrthogonalizationOf(BoundedLine element) => OrthogonalizationOf(element);
}
interface IOrthogonalizationTarget<in TSelf, TOther> : IOrthogonalizationTarget<TOther> where TOther : IOrthogonalizable<TOther, TSelf> where TSelf : IOrthogonalizationTarget<TOther>;
#endregion

#region Distance measurable
public interface IDistanceMeasurable<in T> {
	float DistanceFrom(T element);
	float DistanceSquaredFrom(T element);
}
public interface ILineDistanceMeasurable : IDistanceMeasurable<Line>, IDistanceMeasurable<Ray>, IDistanceMeasurable<BoundedLine> {
	float DistanceFrom<TLine>(TLine line) where TLine : ILine<TLine>;
	float DistanceSquaredFrom<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IDistanceMeasurable<Line>.DistanceFrom(Line element) => DistanceFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IDistanceMeasurable<Line>.DistanceSquaredFrom(Line element) => DistanceSquaredFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IDistanceMeasurable<Ray>.DistanceFrom(Ray element) => DistanceFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IDistanceMeasurable<Ray>.DistanceSquaredFrom(Ray element) => DistanceSquaredFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IDistanceMeasurable<BoundedLine>.DistanceFrom(BoundedLine element) => DistanceFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IDistanceMeasurable<BoundedLine>.DistanceSquaredFrom(BoundedLine element) => DistanceSquaredFrom(element);
}
interface IDistanceMeasurable<in TSelf, in TOther> : IDistanceMeasurable<TOther> where TOther : IDistanceMeasurable<TSelf>;

public interface ISignedDistanceMeasurable<in T> : IDistanceMeasurable<T> {
	float SignedDistanceFrom(T element);
}
public interface ILineSignedDistanceMeasurable : ILineDistanceMeasurable, ISignedDistanceMeasurable<Line>, ISignedDistanceMeasurable<Ray>, ISignedDistanceMeasurable<BoundedLine> {
	float SignedDistanceFrom<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISignedDistanceMeasurable<Line>.SignedDistanceFrom(Line element) => SignedDistanceFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISignedDistanceMeasurable<Ray>.SignedDistanceFrom(Ray element) => SignedDistanceFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISignedDistanceMeasurable<BoundedLine>.SignedDistanceFrom(BoundedLine element) => SignedDistanceFrom(element);
}
interface ISignedDistanceMeasurable<in TSelf, in TOther> : ISignedDistanceMeasurable<TOther> where TOther : ISignedDistanceMeasurable<TSelf>;

public interface IEndogenousSurfaceDistanceMeasurable<in T> : IDistanceMeasurable<T> {
	float SurfaceDistanceFrom(T element);
	float SurfaceDistanceSquaredFrom(T element);
}
public interface ILineEndogenousSurfaceDistanceMeasurable : ILineDistanceMeasurable, IEndogenousSurfaceDistanceMeasurable<Line>, IEndogenousSurfaceDistanceMeasurable<Ray>, IEndogenousSurfaceDistanceMeasurable<BoundedLine> {
	float SurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine<TLine>;
	float SurfaceDistanceSquaredFrom<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IEndogenousSurfaceDistanceMeasurable<Line>.SurfaceDistanceFrom(Line element) => SurfaceDistanceFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IEndogenousSurfaceDistanceMeasurable<Line>.SurfaceDistanceSquaredFrom(Line element) => SurfaceDistanceSquaredFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IEndogenousSurfaceDistanceMeasurable<Ray>.SurfaceDistanceFrom(Ray element) => SurfaceDistanceFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IEndogenousSurfaceDistanceMeasurable<Ray>.SurfaceDistanceSquaredFrom(Ray element) => SurfaceDistanceSquaredFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IEndogenousSurfaceDistanceMeasurable<BoundedLine>.SurfaceDistanceFrom(BoundedLine element) => SurfaceDistanceFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IEndogenousSurfaceDistanceMeasurable<BoundedLine>.SurfaceDistanceSquaredFrom(BoundedLine element) => SurfaceDistanceSquaredFrom(element);
}
interface IEndogenousSurfaceDistanceMeasurable<in TSelf, in TOther> : IEndogenousSurfaceDistanceMeasurable<TOther> where TOther : IExogenousSurfaceDistanceMeasurable<TSelf>;
public interface ISignedEndogenousSurfaceDistanceMeasurable<in T> : ISignedDistanceMeasurable<T>, IEndogenousSurfaceDistanceMeasurable<T> {
	float SignedSurfaceDistanceFrom(T element);
}
public interface ILineSignedEndogenousSurfaceDistanceMeasurable : ILineEndogenousSurfaceDistanceMeasurable, ISignedEndogenousSurfaceDistanceMeasurable<Line>, ISignedEndogenousSurfaceDistanceMeasurable<Ray>, ISignedEndogenousSurfaceDistanceMeasurable<BoundedLine> {
	float SignedSurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISignedEndogenousSurfaceDistanceMeasurable<Line>.SignedSurfaceDistanceFrom(Line element) => SignedSurfaceDistanceFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISignedEndogenousSurfaceDistanceMeasurable<Ray>.SignedSurfaceDistanceFrom(Ray element) => SignedSurfaceDistanceFrom(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISignedEndogenousSurfaceDistanceMeasurable<BoundedLine>.SignedSurfaceDistanceFrom(BoundedLine element) => SignedSurfaceDistanceFrom(element);
}
interface ISignedEndogenousSurfaceDistanceMeasurable<in TSelf, in TOther> : ISignedEndogenousSurfaceDistanceMeasurable<TOther> where TOther : ISignedExogenousSurfaceDistanceMeasurable<TSelf>;
public interface IExogenousSurfaceDistanceMeasurable<in T> : IDistanceMeasurable<T> {
	float DistanceFromSurfaceOf(T element);
	float DistanceSquaredFromSurfaceOf(T element);
}
public interface ILineExogenousSurfaceDistanceMeasurable : ILineDistanceMeasurable, IExogenousSurfaceDistanceMeasurable<Line>, IExogenousSurfaceDistanceMeasurable<Ray>, IExogenousSurfaceDistanceMeasurable<BoundedLine> {
	float DistanceFromSurfaceOf<TLine>(TLine line) where TLine : ILine<TLine>;
	float DistanceSquaredFromSurfaceOf<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IExogenousSurfaceDistanceMeasurable<Line>.DistanceFromSurfaceOf(Line element) => DistanceFromSurfaceOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IExogenousSurfaceDistanceMeasurable<Line>.DistanceSquaredFromSurfaceOf(Line element) => DistanceSquaredFromSurfaceOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IExogenousSurfaceDistanceMeasurable<Ray>.DistanceFromSurfaceOf(Ray element) => DistanceFromSurfaceOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IExogenousSurfaceDistanceMeasurable<Ray>.DistanceSquaredFromSurfaceOf(Ray element) => DistanceSquaredFromSurfaceOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IExogenousSurfaceDistanceMeasurable<BoundedLine>.DistanceFromSurfaceOf(BoundedLine element) => DistanceFromSurfaceOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float IExogenousSurfaceDistanceMeasurable<BoundedLine>.DistanceSquaredFromSurfaceOf(BoundedLine element) => DistanceSquaredFromSurfaceOf(element);
}
interface IExogenousSurfaceDistanceMeasurable<in TSelf, in TOther> : IExogenousSurfaceDistanceMeasurable<TOther> where TOther : IEndogenousSurfaceDistanceMeasurable<TSelf>;
public interface ISignedExogenousSurfaceDistanceMeasurable<in T> : ISignedDistanceMeasurable<T>, IExogenousSurfaceDistanceMeasurable<T> {
	float SignedDistanceFromSurfaceOf(T element);
}
public interface ILineSignedExogenousSurfaceDistanceMeasurable : ILineExogenousSurfaceDistanceMeasurable, ISignedExogenousSurfaceDistanceMeasurable<Line>, ISignedExogenousSurfaceDistanceMeasurable<Ray>, ISignedExogenousSurfaceDistanceMeasurable<BoundedLine> {
	float SignedDistanceFromSurfaceOf<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISignedExogenousSurfaceDistanceMeasurable<Line>.SignedDistanceFromSurfaceOf(Line element) => SignedDistanceFromSurfaceOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISignedExogenousSurfaceDistanceMeasurable<Ray>.SignedDistanceFromSurfaceOf(Ray element) => SignedDistanceFromSurfaceOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	float ISignedExogenousSurfaceDistanceMeasurable<BoundedLine>.SignedDistanceFromSurfaceOf(BoundedLine element) => SignedDistanceFromSurfaceOf(element);
}
interface ISignedExogenousSurfaceDistanceMeasurable<in TSelf, in TOther> : ISignedExogenousSurfaceDistanceMeasurable<TOther> where TOther : ISignedEndogenousSurfaceDistanceMeasurable<TSelf>;
#endregion

#region Containment testable
public interface IContainer<in T> {
	bool Contains(T element);
}
public interface ILineContainer : IContainer<Line>, IContainer<Ray>, IContainer<BoundedLine> {
	bool Contains<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IContainer<Line>.Contains(Line element) => Contains(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IContainer<Ray>.Contains(Ray element) => Contains(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IContainer<BoundedLine>.Contains(BoundedLine element) => Contains(element);
}
interface IContainer<in TSelf, in TOther> : IContainer<TOther> where TOther : IContainable<TSelf>;
public interface IContainable<in T> {
	bool IsContainedWithin(T element);
}
public interface ILineContainable : IContainable<Line>, IContainable<Ray>, IContainable<BoundedLine> {
	bool IsContainedWithin<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IContainable<Line>.IsContainedWithin(Line element) => IsContainedWithin(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IContainable<Ray>.IsContainedWithin(Ray element) => IsContainedWithin(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IContainable<BoundedLine>.IsContainedWithin(BoundedLine element) => IsContainedWithin(element);
}
interface IContainable<in TSelf, in TOther> : IContainer<TOther> where TOther : IContainable<TSelf>;
#endregion

#region Closest point discoverable
public interface IClosestEndogenousPointDiscoverable<in T> {
	Location PointClosestTo(T element);
}
public interface ILineClosestEndogenousPointDiscoverable : IClosestEndogenousPointDiscoverable<Line>, IClosestEndogenousPointDiscoverable<Ray>, IClosestEndogenousPointDiscoverable<BoundedLine> {
	Location PointClosestTo<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousPointDiscoverable<Line>.PointClosestTo(Line element) => PointClosestTo(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousPointDiscoverable<Ray>.PointClosestTo(Ray element) => PointClosestTo(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousPointDiscoverable<BoundedLine>.PointClosestTo(BoundedLine element) => PointClosestTo(element);
}
interface IClosestEndogenousPointDiscoverable<in TSelf, in TOther> : IClosestEndogenousPointDiscoverable<TOther> where TOther : IClosestExogenousPointDiscoverable<TSelf>;
public interface IClosestExogenousPointDiscoverable<in T> {
	Location ClosestPointOn(T element);
}
public interface ILineClosestExogenousPointDiscoverable : IClosestExogenousPointDiscoverable<Line>, IClosestExogenousPointDiscoverable<Ray>, IClosestExogenousPointDiscoverable<BoundedLine> {
	Location ClosestPointOn<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousPointDiscoverable<Line>.ClosestPointOn(Line element) => ClosestPointOn(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousPointDiscoverable<Ray>.ClosestPointOn(Ray element) => ClosestPointOn(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousPointDiscoverable<BoundedLine>.ClosestPointOn(BoundedLine element) => ClosestPointOn(element);
}
interface IClosestExogenousPointDiscoverable<in TSelf, in TOther> : IClosestExogenousPointDiscoverable<TOther> where TOther : IClosestEndogenousPointDiscoverable<TSelf>;

public interface IClosestEndogenousSurfacePointDiscoverable<in T> : IClosestEndogenousPointDiscoverable<T> {
	Location SurfacePointClosestTo(T element);
}
public interface ILineClosestEndogenousSurfacePointDiscoverable : ILineClosestEndogenousPointDiscoverable, IClosestEndogenousSurfacePointDiscoverable<Line>, IClosestEndogenousSurfacePointDiscoverable<Ray>, IClosestEndogenousSurfacePointDiscoverable<BoundedLine> {
	Location SurfacePointClosestTo<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousSurfacePointDiscoverable<Line>.SurfacePointClosestTo(Line element) => SurfacePointClosestTo(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousSurfacePointDiscoverable<Ray>.SurfacePointClosestTo(Ray element) => SurfacePointClosestTo(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestEndogenousSurfacePointDiscoverable<BoundedLine>.SurfacePointClosestTo(BoundedLine element) => SurfacePointClosestTo(element);
}
interface IClosestEndogenousSurfacePointDiscoverable<in TSelf, in TOther> : IClosestEndogenousSurfacePointDiscoverable<TOther> where TOther : IClosestExogenousSurfacePointDiscoverable<TSelf>;
public interface IClosestExogenousSurfacePointDiscoverable<in T> : IClosestExogenousPointDiscoverable<T> {
	Location ClosestPointToSurfaceOf(T element);
}
public interface ILineClosestExogenousSurfacePointDiscoverable : ILineClosestExogenousPointDiscoverable, IClosestExogenousSurfacePointDiscoverable<Line>, IClosestExogenousSurfacePointDiscoverable<Ray>, IClosestExogenousSurfacePointDiscoverable<BoundedLine> {
	Location ClosestPointToSurfaceOf<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousSurfacePointDiscoverable<Line>.ClosestPointToSurfaceOf(Line element) => ClosestPointToSurfaceOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousSurfacePointDiscoverable<Ray>.ClosestPointToSurfaceOf(Ray element) => ClosestPointToSurfaceOf(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Location IClosestExogenousSurfacePointDiscoverable<BoundedLine>.ClosestPointToSurfaceOf(BoundedLine element) => ClosestPointToSurfaceOf(element);
}
interface IClosestExogenousSurfacePointDiscoverable<in TSelf, in TOther> : IClosestExogenousSurfacePointDiscoverable<TOther> where TOther : IClosestEndogenousSurfacePointDiscoverable<TSelf>;
#endregion

#region Intersectable / Relatable
public interface IIntersectable<in T> {
	bool IsIntersectedBy(T element);
}
public interface ILineIntersectable : IIntersectable<Line>, IIntersectable<Ray>, IIntersectable<BoundedLine> {
	bool IsIntersectedBy<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IIntersectable<Line>.IsIntersectedBy(Line element) => IsIntersectedBy(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IIntersectable<Ray>.IsIntersectedBy(Ray element) => IsIntersectedBy(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IIntersectable<BoundedLine>.IsIntersectedBy(BoundedLine element) => IsIntersectedBy(element);
}
interface IIntersectable<in TSelf, in TOther> : IIntersectable<TOther> where TOther : IIntersectable<TSelf>;
public interface IIntersectionDeterminable<in T, TIntersection> : IIntersectable<T> where TIntersection : struct {
	TIntersection? IntersectionWith(T element);
}
public interface ILineIntersectionDeterminable<TIntersection> : IIntersectionDeterminable<Line, TIntersection>, IIntersectionDeterminable<Ray, TIntersection>, IIntersectionDeterminable<BoundedLine, TIntersection> where TIntersection : struct {
	TIntersection? IntersectionWith<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	TIntersection? IIntersectionDeterminable<Line, TIntersection>.IntersectionWith(Line element) => IntersectionWith(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	TIntersection? IIntersectionDeterminable<Ray, TIntersection>.IntersectionWith(Ray element) => IntersectionWith(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	TIntersection? IIntersectionDeterminable<BoundedLine, TIntersection>.IntersectionWith(BoundedLine element) => IntersectionWith(element);
}
interface IIntersectionDeterminable<in TSelf, in TOther, TIntersection> : IIntersectionDeterminable<TOther, TIntersection> where TOther : IIntersectionDeterminable<TSelf, TIntersection> where TIntersection : struct;
public interface IRelatable<in T, out TRelationship> {
	TRelationship RelationshipTo(T element);
}
public interface ILineRelatable<out TRelationship> : IRelatable<Line, TRelationship>, IRelatable<Ray, TRelationship>, IRelatable<BoundedLine, TRelationship> {
	TRelationship RelationshipTo<TLine>(TLine line) where TLine : ILine<TLine>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	TRelationship IRelatable<Line, TRelationship>.RelationshipTo(Line element) => RelationshipTo(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	TRelationship IRelatable<Ray, TRelationship>.RelationshipTo(Ray element) => RelationshipTo(element);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	TRelationship IRelatable<BoundedLine, TRelationship>.RelationshipTo(BoundedLine element) => RelationshipTo(element);
}
interface IRelatable<in TSelf, in TOther, out TRelationship> : IRelatable<TOther, TRelationship> where TOther : IRelatable<TSelf, TRelationship>;
#endregion