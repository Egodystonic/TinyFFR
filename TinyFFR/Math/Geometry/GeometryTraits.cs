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

public interface ILengthAdjustable<out TSelf> where TSelf : ILengthAdjustable<TSelf> {
	TSelf WithLength(float newLength);
	TSelf ShortenedBy(float lengthDecrease);
	TSelf LengthenedBy(float lengthIncrease);
	TSelf WithMaxLength(float maxLength);
	TSelf WithMinLength(float minLength);
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

#region Angle/reflection/projection/parallelization/orthogonalization
public interface IAngleMeasurable<in TOther> {
	Angle AngleTo(TOther other);
}
public interface ILineAngleMeasurable : IAngleMeasurable<Line>, IAngleMeasurable<Ray>, IAngleMeasurable<BoundedRay>;
public interface IAngleMeasurable<in TSelf, in TOther> : IAngleMeasurable<TOther> where TSelf : IAngleMeasurable<TSelf, TOther> where TOther : IAngleMeasurable<TSelf> {
	static abstract Angle operator ^(TSelf self, TOther other);
	static abstract Angle operator ^(TOther other, TSelf self);
}


public interface IReflectable<in TOther, TReflection> where TReflection : struct {
	TReflection? ReflectedBy(TOther element);
}
public interface IReflectionTarget<in TOther, TReflection> where TReflection : struct {
	TReflection? ReflectionOf(TOther element);
}
public interface ILineReflectionTarget<TReflection> : IReflectionTarget<Line, TReflection>, IReflectionTarget<Ray, TReflection>, IReflectionTarget<BoundedRay, TReflection> where TReflection : struct;
public interface ILineReflectionTarget : IReflectionTarget<Line, Ray>, IReflectionTarget<Ray, Ray>, IReflectionTarget<BoundedRay, BoundedRay>;
interface IReflectionTarget<in TSelf, in TOther, TReflection> : IReflectionTarget<TOther, TReflection> where TOther : IReflectable<TSelf, TReflection> where TSelf : IReflectionTarget<TOther, TReflection> where TReflection : struct;


public interface IProjectable<out TSelf, in TOther> {
	TSelf ProjectedOnTo(TOther element);
}
public interface IProjectionTarget<TOther> {
	TOther ProjectionOf(TOther element);
}
public interface ILineProjectionTarget : IProjectionTarget<Line>, IProjectionTarget<Ray>, IProjectionTarget<BoundedRay>;
interface IProjectionTarget<in TSelf, TOther> : IProjectionTarget<TOther> where TOther : IProjectable<TOther, TSelf> where TSelf : IProjectionTarget<TOther>;


public interface IParallelizable<out TSelf, in TOther> {
	TSelf ParallelizedWith(TOther element);
}
public interface IParallelizationTarget<TOther> {
	TOther ParallelizationOf(TOther element);
}
public interface ILineParallelizationTarget : IParallelizationTarget<Line>, IParallelizationTarget<Ray>, IParallelizationTarget<BoundedRay>;
interface IParallelizationTarget<in TSelf, TOther> : IParallelizationTarget<TOther> where TOther : IParallelizable<TOther, TSelf> where TSelf : IParallelizationTarget<TOther>;


public interface IOrthogonalizable<out TSelf, in TOther> {
	TSelf OrthogonalizedAgainst(TOther element);
}
public interface IOrthogonalizationTarget<TOther> {
	TOther OrthogonalizationOf(TOther element);
}
public interface ILineOrthogonalizationTarget : IOrthogonalizationTarget<Line>, IOrthogonalizationTarget<Ray>, IOrthogonalizationTarget<BoundedRay>;
interface IOrthogonalizationTarget<in TSelf, TOther> : IOrthogonalizationTarget<TOther> where TOther : IOrthogonalizable<TOther, TSelf> where TSelf : IOrthogonalizationTarget<TOther>;
#endregion

#region Distance measurable
public interface IDistanceMeasurable<in TOther> {
	float DistanceFrom(TOther element);
	float DistanceSquaredFrom(TOther element);
}
public interface ILineDistanceMeasurable : IDistanceMeasurable<Line>, IDistanceMeasurable<Ray>, IDistanceMeasurable<BoundedRay>;
interface IDistanceMeasurable<in TSelf, in TOther> : IDistanceMeasurable<TOther> where TOther : IDistanceMeasurable<TSelf>;

public interface ISignedDistanceMeasurable<in TOther> : IDistanceMeasurable<TOther> {
	float SignedDistanceFrom(TOther element);
}
public interface ILineSignedDistanceMeasurable : ILineDistanceMeasurable, ISignedDistanceMeasurable<Line>, ISignedDistanceMeasurable<Ray>, ISignedDistanceMeasurable<BoundedRay>;
interface ISignedDistanceMeasurable<in TSelf, in TOther> : ISignedDistanceMeasurable<TOther> where TOther : ISignedDistanceMeasurable<TSelf>;

public interface IEndogenousSurfaceDistanceMeasurable<in TOther> : IDistanceMeasurable<TOther> {
	float SurfaceDistanceFrom(TOther element);
	float SurfaceDistanceSquaredFrom(TOther element);
}
public interface ILineEndogenousSurfaceDistanceMeasurable : ILineDistanceMeasurable, IEndogenousSurfaceDistanceMeasurable<Line>, IEndogenousSurfaceDistanceMeasurable<Ray>, IEndogenousSurfaceDistanceMeasurable<BoundedRay>;
interface IEndogenousSurfaceDistanceMeasurable<in TSelf, in TOther> : IEndogenousSurfaceDistanceMeasurable<TOther> where TOther : IExogenousSurfaceDistanceMeasurable<TSelf>;
public interface ISignedEndogenousSurfaceDistanceMeasurable<in TOther> : ISignedDistanceMeasurable<TOther>, IEndogenousSurfaceDistanceMeasurable<TOther> {
	float SignedSurfaceDistanceFrom(TOther element);
}
public interface ILineSignedEndogenousSurfaceDistanceMeasurable : ILineEndogenousSurfaceDistanceMeasurable, ISignedEndogenousSurfaceDistanceMeasurable<Line>, ISignedEndogenousSurfaceDistanceMeasurable<Ray>, ISignedEndogenousSurfaceDistanceMeasurable<BoundedRay>;
interface ISignedEndogenousSurfaceDistanceMeasurable<in TSelf, in TOther> : ISignedEndogenousSurfaceDistanceMeasurable<TOther> where TOther : ISignedExogenousSurfaceDistanceMeasurable<TSelf>;
public interface IExogenousSurfaceDistanceMeasurable<in TOther> : IDistanceMeasurable<TOther> {
	float DistanceFromSurfaceOf(TOther element);
	float DistanceSquaredFromSurfaceOf(TOther element);
}
public interface ILineExogenousSurfaceDistanceMeasurable : ILineDistanceMeasurable, IExogenousSurfaceDistanceMeasurable<Line>, IExogenousSurfaceDistanceMeasurable<Ray>, IExogenousSurfaceDistanceMeasurable<BoundedRay>;
interface IExogenousSurfaceDistanceMeasurable<in TSelf, in TOther> : IExogenousSurfaceDistanceMeasurable<TOther> where TOther : IEndogenousSurfaceDistanceMeasurable<TSelf>;
public interface ISignedExogenousSurfaceDistanceMeasurable<in TOther> : ISignedDistanceMeasurable<TOther>, IExogenousSurfaceDistanceMeasurable<TOther> {
	float SignedDistanceFromSurfaceOf(TOther element);
}
public interface ILineSignedExogenousSurfaceDistanceMeasurable : ILineExogenousSurfaceDistanceMeasurable, ISignedExogenousSurfaceDistanceMeasurable<Line>, ISignedExogenousSurfaceDistanceMeasurable<Ray>, ISignedExogenousSurfaceDistanceMeasurable<BoundedRay>;
interface ISignedExogenousSurfaceDistanceMeasurable<in TSelf, in TOther> : ISignedExogenousSurfaceDistanceMeasurable<TOther> where TOther : ISignedEndogenousSurfaceDistanceMeasurable<TSelf>;
#endregion

#region Containment testable
public interface IContainer<in TOther> {
	bool Contains(TOther element);
}
public interface ILineContainer : IContainer<Line>, IContainer<Ray>, IContainer<BoundedRay>;
interface IContainer<in TSelf, in TOther> : IContainer<TOther> where TOther : IContainable<TSelf>;
public interface IContainable<in TOther> {
	bool IsContainedWithin(TOther element);
}
public interface ILineContainable : IContainable<Line>, IContainable<Ray>, IContainable<BoundedRay>;
interface IContainable<in TSelf, in TOther> : IContainable<TOther> where TOther : IContainer<TSelf>;
#endregion

#region Closest point discoverable
public interface IClosestEndogenousPointDiscoverable<in TOther> {
	Location PointClosestTo(TOther element);
}
public interface ILineClosestEndogenousPointDiscoverable : IClosestEndogenousPointDiscoverable<Line>, IClosestEndogenousPointDiscoverable<Ray>, IClosestEndogenousPointDiscoverable<BoundedRay>;
interface IClosestEndogenousPointDiscoverable<in TSelf, in TOther> : IClosestEndogenousPointDiscoverable<TOther> where TOther : IClosestExogenousPointDiscoverable<TSelf>;
public interface IClosestExogenousPointDiscoverable<in TOther> {
	Location ClosestPointOn(TOther element);
}
public interface ILineClosestExogenousPointDiscoverable : IClosestExogenousPointDiscoverable<Line>, IClosestExogenousPointDiscoverable<Ray>, IClosestExogenousPointDiscoverable<BoundedRay>;
interface IClosestExogenousPointDiscoverable<in TSelf, in TOther> : IClosestExogenousPointDiscoverable<TOther> where TOther : IClosestEndogenousPointDiscoverable<TSelf>;

public interface IClosestEndogenousSurfacePointDiscoverable<in TOther> : IClosestEndogenousPointDiscoverable<TOther> {
	Location SurfacePointClosestTo(TOther element);
}
public interface ILineClosestEndogenousSurfacePointDiscoverable : ILineClosestEndogenousPointDiscoverable, IClosestEndogenousSurfacePointDiscoverable<Line>, IClosestEndogenousSurfacePointDiscoverable<Ray>, IClosestEndogenousSurfacePointDiscoverable<BoundedRay>;
interface IClosestEndogenousSurfacePointDiscoverable<in TSelf, in TOther> : IClosestEndogenousSurfacePointDiscoverable<TOther> where TOther : IClosestExogenousSurfacePointDiscoverable<TSelf>;
public interface IClosestExogenousSurfacePointDiscoverable<in TOther> : IClosestExogenousPointDiscoverable<TOther> {
	Location ClosestPointToSurfaceOf(TOther element);
}
public interface ILineClosestExogenousSurfacePointDiscoverable : ILineClosestExogenousPointDiscoverable, IClosestExogenousSurfacePointDiscoverable<Line>, IClosestExogenousSurfacePointDiscoverable<Ray>, IClosestExogenousSurfacePointDiscoverable<BoundedRay>;
interface IClosestExogenousSurfacePointDiscoverable<in TSelf, in TOther> : IClosestExogenousSurfacePointDiscoverable<TOther> where TOther : IClosestEndogenousSurfacePointDiscoverable<TSelf>;
#endregion

#region Intersectable / Relatable
public interface IIntersectable<in TOther> {
	bool IsIntersectedBy(TOther element);
}
public interface ILineIntersectable : IIntersectable<Line>, IIntersectable<Ray>, IIntersectable<BoundedRay>;
interface IIntersectable<in TSelf, in TOther> : IIntersectable<TOther> where TOther : IIntersectable<TSelf>;
public interface IIntersectionDeterminable<in TOther, TIntersection> : IIntersectable<TOther> where TIntersection : struct {
	TIntersection? IntersectionWith(TOther element);
}
public interface ILineIntersectionDeterminable<TIntersection> : ILineIntersectable, IIntersectionDeterminable<Line, TIntersection>, IIntersectionDeterminable<Ray, TIntersection>, IIntersectionDeterminable<BoundedRay, TIntersection> where TIntersection : struct;
interface IIntersectionDeterminable<in TSelf, in TOther, TIntersection> : IIntersectionDeterminable<TOther, TIntersection> where TOther : IIntersectionDeterminable<TSelf, TIntersection> where TIntersection : struct;
public interface IRelatable<in TOther, out TRelationship> {
	TRelationship RelationshipTo(TOther element);
}
public interface ILineRelatable<out TRelationship> : IRelatable<Line, TRelationship>, IRelatable<Ray, TRelationship>, IRelatable<BoundedRay, TRelationship>;
interface IRelatable<in TSelf, in TOther, out TRelationship> : IRelatable<TOther, TRelationship> where TOther : IRelatable<TSelf, TRelationship>;
#endregion