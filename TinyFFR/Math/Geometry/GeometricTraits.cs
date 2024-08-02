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
	TSelf IMultiplicative<TSelf, Vect, TSelf>.DividedBy(Vect vect) => ScaledBy(vect.Reciprocal ?? Vect.Zero);
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
	TReflection FastReflectedBy(TOther element);
	Angle? IncidentAngleWith(TOther element);
	Angle FastIncidentAngleWith(TOther element);
}
public interface IReflectionTarget<in TOther, TReflection> where TReflection : struct {
	TReflection? ReflectionOf(TOther element);
	TReflection FastReflectionOf(TOther element);
	Angle? IncidentAngleWith(TOther element);
	Angle FastIncidentAngleWith(TOther element);
}
public interface ILineReflectionTarget<TReflection> : IReflectionTarget<Line, TReflection>, IReflectionTarget<Ray, TReflection>, IReflectionTarget<BoundedRay, TReflection> where TReflection : struct;
public interface ILineReflectionTarget : IReflectionTarget<Line, Line>, IReflectionTarget<Ray, Ray>, IReflectionTarget<BoundedRay, BoundedRay>;
interface IReflectionTarget<in TSelf, in TOther, TReflection> : IReflectionTarget<TOther, TReflection> where TOther : IReflectable<TSelf, TReflection> where TSelf : IReflectionTarget<TOther, TReflection> where TReflection : struct;
public interface IConvexShapeReflectable<TReflection> where TReflection : struct {
	TReflection? ReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	TReflection FastReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	Angle? IncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	Angle FastIncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
}


public interface IProjectable<TSelf, in TOther> where TSelf : struct {
	TSelf? ProjectedOnTo(TOther element);
	TSelf FastProjectedOnTo(TOther element);
}
public interface IProjectionTarget<TOther> where TOther : struct {
	TOther? ProjectionOf(TOther element);
	TOther FastProjectionOf(TOther element);
}
public interface ILineProjectionTarget : IProjectionTarget<Line>, IProjectionTarget<Ray>, IProjectionTarget<BoundedRay>;
interface IProjectionTarget<in TSelf, TOther> : IProjectionTarget<TOther> where TOther : struct, IProjectable<TOther, TSelf> where TSelf : IProjectionTarget<TOther>;

public interface IParallelDiscernible<in TOther> {
	bool IsParallelTo(TOther element);
	bool IsApproximatelyParallelTo(TOther element);
	bool IsApproximatelyParallelTo(TOther element, Angle tolerance);
}
public interface IParallelizable<TSelf, in TOther> : IParallelDiscernible<TOther> where TSelf : struct {
	TSelf? ParallelizedWith(TOther element);
	TSelf FastParallelizedWith(TOther element);	
}
public interface IParallelizationTarget<TOther> : IParallelDiscernible<TOther> where TOther : struct {
	TOther? ParallelizationOf(TOther element);
	TOther FastParallelizationOf(TOther element);
}
public interface ILineParallelizationTarget : IParallelizationTarget<Line>, IParallelizationTarget<Ray>, IParallelizationTarget<BoundedRay>;
interface IParallelizationTarget<in TSelf, TOther> : IParallelizationTarget<TOther> where TOther : struct, IParallelizable<TOther, TSelf> where TSelf : IParallelizationTarget<TOther>;

public interface IOrthogonalDiscernible<in TOther> {
	bool IsOrthogonalTo(TOther element);
	bool IsApproximatelyOrthogonalTo(TOther element);
	bool IsApproximatelyOrthogonalTo(TOther element, Angle tolerance);
}
public interface IOrthogonalizable<TSelf, in TOther> : IOrthogonalDiscernible<TOther> where TSelf : struct {
	TSelf? OrthogonalizedAgainst(TOther element);
	TSelf FastOrthogonalizedAgainst(TOther element);
}
public interface IOrthogonalizationTarget<TOther> : IOrthogonalDiscernible<TOther> where TOther : struct {
	TOther? OrthogonalizationOf(TOther element);
	TOther FastOrthogonalizationOf(TOther element);
}
public interface ILineOrthogonalizationTarget : IOrthogonalizationTarget<Line>, IOrthogonalizationTarget<Ray>, IOrthogonalizationTarget<BoundedRay>;
interface IOrthogonalizationTarget<in TSelf, TOther> : IOrthogonalizationTarget<TOther> where TOther : struct, IOrthogonalizable<TOther, TSelf> where TSelf : IOrthogonalizationTarget<TOther>;
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

public interface IConvexShapeDistanceMeasurable {
	float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>; 
}
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

public interface IClosestConvexShapePointsDiscoverable {
	Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
}
#endregion

#region Intersectable / Relatable
public interface IIntersectable<in TOther> {
	bool IsIntersectedBy(TOther element);
}
public interface ILineIntersectable : IIntersectable<Line>, IIntersectable<Ray>, IIntersectable<BoundedRay>;
interface IIntersectable<in TSelf, in TOther> : IIntersectable<TOther> where TOther : IIntersectable<TSelf>;
public interface IIntersectionDeterminable<in TOther, TIntersection> : IIntersectable<TOther> where TIntersection : struct {
	TIntersection? IntersectionWith(TOther element);
	TIntersection FastIntersectionWith(TOther element);
}
public interface ILineIntersectionDeterminable<TIntersection> : ILineIntersectable, IIntersectionDeterminable<Line, TIntersection>, IIntersectionDeterminable<Ray, TIntersection>, IIntersectionDeterminable<BoundedRay, TIntersection> where TIntersection : struct;
interface IIntersectionDeterminable<in TSelf, in TOther, TIntersection> : IIntersectionDeterminable<TOther, TIntersection> where TOther : IIntersectionDeterminable<TSelf, TIntersection> where TIntersection : struct;
public interface IRelatable<in TOther, out TRelationship> {
	TRelationship RelationshipTo(TOther element);
}
public interface ILineRelatable<out TRelationship> : IRelatable<Line, TRelationship>, IRelatable<Ray, TRelationship>, IRelatable<BoundedRay, TRelationship>;
interface IRelatable<in TSelf, in TOther, out TRelationship> : IRelatable<TOther, TRelationship> where TOther : IRelatable<TSelf, TRelationship>;

public interface IConvexShapeIntersectable<TIntersection> where TIntersection : struct {
	TIntersection? IntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	TIntersection FastIntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
}
#endregion