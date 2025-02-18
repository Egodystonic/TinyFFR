// Created on 2024-03-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

// This file hosts all the trait interfaces for geometric primitives

namespace Egodystonic.TinyFFR;

#region Scale/Rotate/Translate & Transform
public interface IScalable<TSelf> :
	IMultiplicative<TSelf, float, TSelf>
	where TSelf : IScalable<TSelf> {
	TSelf IMultiplicative<TSelf, float, TSelf>.MultipliedBy(float scalar) => ScaledBy(scalar);
	TSelf IMultiplicative<TSelf, float, TSelf>.DividedBy(float scalar) => ScaledBy(1f / scalar);
	TSelf ScaledBy(float scalar);
}
public interface IPointScalable<TSelf> :
	IScalable<TSelf> where TSelf : IPointScalable<TSelf>, IScalable<TSelf> {
	TSelf ScaledBy(float scalar, Location scalingOrigin);
	TSelf ScaledFromOriginBy(float scalar);
}

public interface IIndependentAxisScalable<TSelf> :
	IScalable<TSelf>
	where TSelf : IIndependentAxisScalable<TSelf> {
	TSelf ScaledBy(Vect vect);
}
public interface IPointIndependentAxisScalable<TSelf> :
	IIndependentAxisScalable<TSelf> 
	where TSelf : IPointIndependentAxisScalable<TSelf>, IIndependentAxisScalable<TSelf> {
	TSelf ScaledBy(Vect vect, Location scalingOrigin);
	TSelf ScaledFromOriginBy(Vect vect);
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
	TSelf RotatedBy(Rotation rot, Location pivot);
	TSelf RotatedAroundOriginBy(Rotation rot);
}

public interface ITranslatable<TSelf> :
	IAdditive<TSelf, Vect, TSelf>
	where TSelf : ITranslatable<TSelf> {
	TSelf IAdditive<TSelf, Vect, TSelf>.Plus(Vect v) => MovedBy(v);
	TSelf IAdditive<TSelf, Vect, TSelf>.Minus(Vect v) => MovedBy(-v);
	TSelf MovedBy(Vect v);
}

public interface ITransformable<TSelf> :
	IIndependentAxisScalable<TSelf>,
	IRotatable<TSelf>,
	ITranslatable<TSelf>,
	IMultiplyOperators<TSelf, Transform, TSelf>
	where TSelf : ITransformable<TSelf>, IIndependentAxisScalable<TSelf>, IRotatable<TSelf>, ITranslatable<TSelf> {
	TSelf TransformedBy(Transform transform);
	static abstract TSelf operator *(Transform left, TSelf right);
}

public interface IPointTransformable<TSelf> :
	ITransformable<TSelf>, IPointIndependentAxisScalable<TSelf>, IPointRotatable<TSelf>
	where TSelf : IPointTransformable<TSelf>, ITransformable<TSelf>, IPointIndependentAxisScalable<TSelf>, IPointRotatable<TSelf> {
	TSelf TransformedBy(Transform transform, Location transformationOrigin);
	TSelf TransformedAroundOriginBy(Transform transform);
}
#endregion

#region 2D Scale/Rotate/Translate & Transform
/*
 * Maintainer's note:
 * Some interface inheritances are commented out below, mostly ones that import operator overloads.
 * The reason for this is the prevalence of CS0695 errors occurring when types try to import those operators
 * for generic type arguments that unify (e.g. XYPair<T> + XYPair<T> must be implemented as well as XYPair<T> + XYPair<float>).
 * The current C# compiler does not give us a way to resolve those overloads when e.g. T = float. If it does one day, we can
 * re-activate these interfaces.
 */


public interface IPointScalable2D<TSelf> :
	IScalable<TSelf> where TSelf : IPointScalable2D<TSelf>, IScalable<TSelf> {
	TSelf ScaledBy(float scalar, XYPair<float> scalingOrigin);
	TSelf ScaledFromOriginBy(float scalar);
}

public interface IIndependentAxisScalable2D<TSelf> :
	IScalable<TSelf>
	where TSelf : IIndependentAxisScalable2D<TSelf> {
	TSelf ScaledBy(XYPair<float> vect);
}
public interface IPointIndependentAxisScalable2D<TSelf> :
	IIndependentAxisScalable2D<TSelf>
	where TSelf : IPointIndependentAxisScalable2D<TSelf>, IIndependentAxisScalable2D<TSelf> {
	TSelf ScaledBy(XYPair<float> vect, XYPair<float> scalingOrigin);
	TSelf ScaledFromOriginBy(XYPair<float> vect);
}

public interface IRotatable2D<TSelf> /*:
	IMultiplyOperators<TSelf, Angle, TSelf>*/
	where TSelf : IRotatable2D<TSelf> {
	//static abstract TSelf operator *(Angle left, TSelf right);
	TSelf RotatedBy(Angle rot);
}

public interface IPointRotatable2D<TSelf> /*:
	IMultiplyOperators<TSelf, (Angle Rotation, XYPair<float> Pivot), TSelf>,
	IMultiplyOperators<TSelf, (XYPair<float> Pivot, Angle Rotation), TSelf>*/
	where TSelf : IPointRotatable2D<TSelf> {
	//static abstract TSelf operator *((Angle Rotation, XYPair<float> Pivot) left, TSelf right);
	//static abstract TSelf operator *((XYPair<float> Pivot, Angle Rotation) left, TSelf right);
	TSelf RotatedBy(Angle rot, XYPair<float> pivot);
	TSelf RotatedAroundOriginBy(Angle rot);
}

public interface ITranslatable2D<TSelf> /*:
	IAdditive<TSelf, XYPair<float>, TSelf>*/
	where TSelf : ITranslatable2D<TSelf> {
	//TSelf IAdditive<TSelf, XYPair<float>, TSelf>.Plus(XYPair<float> v) => MovedBy(v);
	//TSelf IAdditive<TSelf, XYPair<float>, TSelf>.Minus(XYPair<float> v) => MovedBy(-v);
	TSelf MovedBy(XYPair<float> v);
}

public interface ITransformable2D<TSelf> :
	IIndependentAxisScalable2D<TSelf>,
	IRotatable2D<TSelf>,
	ITranslatable2D<TSelf>,
	IMultiplyOperators<TSelf, Transform2D, TSelf>
	where TSelf : ITransformable2D<TSelf>, IIndependentAxisScalable2D<TSelf>, IRotatable2D<TSelf>, ITranslatable2D<TSelf> {
	TSelf TransformedBy(Transform2D transform);
	static abstract TSelf operator *(Transform2D left, TSelf right);
}

public interface IPointTransformable2D<TSelf> :
	ITransformable2D<TSelf>, IPointIndependentAxisScalable2D<TSelf>, IPointRotatable2D<TSelf>
	where TSelf : IPointTransformable2D<TSelf>, ITransformable2D<TSelf>, IPointIndependentAxisScalable2D<TSelf>, IPointRotatable2D<TSelf> {
	TSelf TransformedBy(Transform2D transform, XYPair<float> transformationOrigin);
	TSelf TransformedAroundOriginBy(Transform2D transform);
}
#endregion

#region Angle/reflection/projection/parallelization/orthogonalization
public interface IAngleMeasurable<in TOther> where TOther : allows ref struct {
	Angle AngleTo(TOther other);
}
public interface ILineAngleMeasurable : IAngleMeasurable<Line>, IAngleMeasurable<Ray>, IAngleMeasurable<BoundedRay>;
public interface IAngleMeasurable<in TSelf, in TOther> : IAngleMeasurable<TOther> where TSelf : IAngleMeasurable<TSelf, TOther>, allows ref struct where TOther : IAngleMeasurable<TSelf>, allows ref struct {
	static abstract Angle operator ^(TSelf self, TOther other);
	static abstract Angle operator ^(TOther other, TSelf self);
}


public interface IReflectable<in TOther, TReflection> where TReflection : struct where TOther : allows ref struct {
	TReflection? ReflectedBy(TOther element);
	TReflection FastReflectedBy(TOther element);
	Angle? IncidentAngleWith(TOther element);
	Angle FastIncidentAngleWith(TOther element);
}
public interface IReflectionTarget<in TOther, TReflection> where TReflection : struct where TOther : allows ref struct {
	TReflection? ReflectionOf(TOther element);
	TReflection FastReflectionOf(TOther element);
	Angle? IncidentAngleWith(TOther element);
	Angle FastIncidentAngleWith(TOther element);
}
public interface ILineReflectionTarget<TReflection> : IReflectionTarget<Line, TReflection>, IReflectionTarget<Ray, TReflection>, IReflectionTarget<BoundedRay, TReflection> where TReflection : struct;
public interface ILineReflectionTarget : IReflectionTarget<Line, Line>, IReflectionTarget<Ray, Ray>, IReflectionTarget<BoundedRay, BoundedRay>;
interface IReflectionTarget<in TSelf, in TOther, TReflection> : IReflectionTarget<TOther, TReflection> where TOther : IReflectable<TSelf, TReflection>, allows ref struct where TSelf : IReflectionTarget<TOther, TReflection>, allows ref struct where TReflection : struct;
public interface IConvexShapeReflectable<TReflection> where TReflection : struct {
	TReflection? ReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	TReflection FastReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	Angle? IncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	Angle FastIncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
}


public interface IProjectable<TSelf, in TOther> where TSelf : struct where TOther : allows ref struct {
	TSelf? ProjectedOnTo(TOther element);
	TSelf FastProjectedOnTo(TOther element);
}
public interface IProjectionTarget<TOther> where TOther : struct {
	TOther? ProjectionOf(TOther element);
	TOther FastProjectionOf(TOther element);
}
public interface ILineProjectionTarget : IProjectionTarget<Line>, IProjectionTarget<Ray>, IProjectionTarget<BoundedRay>;
interface IProjectionTarget<in TSelf, TOther> : IProjectionTarget<TOther> where TOther : struct, IProjectable<TOther, TSelf> where TSelf : IProjectionTarget<TOther>, allows ref struct;

public interface IParallelDiscernible<in TOther> where TOther : allows ref struct {
	bool IsParallelTo(TOther element);
	bool IsApproximatelyParallelTo(TOther element);
	bool IsApproximatelyParallelTo(TOther element, Angle tolerance);
}
public interface IParallelizable<TSelf, in TOther> : IParallelDiscernible<TOther> where TSelf : struct where TOther : allows ref struct {
	TSelf? ParallelizedWith(TOther element);
	TSelf FastParallelizedWith(TOther element);	
}
public interface IParallelizationTarget<TOther> : IParallelDiscernible<TOther> where TOther : struct {
	TOther? ParallelizationOf(TOther element);
	TOther FastParallelizationOf(TOther element);
}
public interface ILineParallelizationTarget : IParallelizationTarget<Line>, IParallelizationTarget<Ray>, IParallelizationTarget<BoundedRay>;
interface IParallelizationTarget<in TSelf, TOther> : IParallelizationTarget<TOther> where TOther : struct, IParallelizable<TOther, TSelf> where TSelf : IParallelizationTarget<TOther>, allows ref struct;

public interface IOrthogonalDiscernible<in TOther> where TOther : allows ref struct {
	bool IsOrthogonalTo(TOther element);
	bool IsApproximatelyOrthogonalTo(TOther element);
	bool IsApproximatelyOrthogonalTo(TOther element, Angle tolerance);
}
public interface IOrthogonalizable<TSelf, in TOther> : IOrthogonalDiscernible<TOther> where TSelf : struct where TOther : allows ref struct {
	TSelf? OrthogonalizedAgainst(TOther element);
	TSelf FastOrthogonalizedAgainst(TOther element);
}
public interface IOrthogonalizationTarget<TOther> : IOrthogonalDiscernible<TOther> where TOther : struct {
	TOther? OrthogonalizationOf(TOther element);
	TOther FastOrthogonalizationOf(TOther element);
}
public interface ILineOrthogonalizationTarget : IOrthogonalizationTarget<Line>, IOrthogonalizationTarget<Ray>, IOrthogonalizationTarget<BoundedRay>;
interface IOrthogonalizationTarget<in TSelf, TOther> : IOrthogonalizationTarget<TOther> where TOther : struct, IOrthogonalizable<TOther, TSelf> where TSelf : IOrthogonalizationTarget<TOther>, allows ref struct;
#endregion

#region Distance measurable
public interface IDistanceMeasurable<in TOther> where TOther : allows ref struct {
	float DistanceFrom(TOther element);
	float DistanceSquaredFrom(TOther element); 
}
public interface ILineDistanceMeasurable : IDistanceMeasurable<Line>, IDistanceMeasurable<Ray>, IDistanceMeasurable<BoundedRay>;
interface IDistanceMeasurable<in TSelf, in TOther> : IDistanceMeasurable<TOther> where TOther : IDistanceMeasurable<TSelf>, allows ref struct where TSelf : allows ref struct;

public interface ISignedDistanceMeasurable<in TOther> : IDistanceMeasurable<TOther> where TOther : allows ref struct {
	float SignedDistanceFrom(TOther element);
}
public interface ILineSignedDistanceMeasurable : ILineDistanceMeasurable, ISignedDistanceMeasurable<Line>, ISignedDistanceMeasurable<Ray>, ISignedDistanceMeasurable<BoundedRay>;
interface ISignedDistanceMeasurable<in TSelf, in TOther> : ISignedDistanceMeasurable<TOther> where TOther : ISignedDistanceMeasurable<TSelf>, allows ref struct where TSelf : allows ref struct;

public interface IConvexShapeDistanceMeasurable {
	float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>; 
}
#endregion

#region Containment testable
public interface IContainer<in TOther> where TOther : allows ref struct {
	bool Contains(TOther element);
}
public interface ILineContainer : IContainer<Line>, IContainer<Ray>, IContainer<BoundedRay>;
interface IContainer<in TSelf, in TOther> : IContainer<TOther> where TOther : IContainable<TSelf>, allows ref struct where TSelf : allows ref struct;
public interface IContainable<in TOther> where TOther : allows ref struct {
	bool IsContainedWithin(TOther element);
}
public interface ILineContainable : IContainable<Line>, IContainable<Ray>, IContainable<BoundedRay>;
interface IContainable<in TSelf, in TOther> : IContainable<TOther> where TOther : IContainer<TSelf>, allows ref struct where TSelf : allows ref struct;
#endregion

#region Closest point discoverable
public interface IClosestEndogenousPointDiscoverable<in TOther> where TOther : allows ref struct {
	Location PointClosestTo(TOther element);
}
public interface ILineClosestEndogenousPointDiscoverable : IClosestEndogenousPointDiscoverable<Line>, IClosestEndogenousPointDiscoverable<Ray>, IClosestEndogenousPointDiscoverable<BoundedRay>;
interface IClosestEndogenousPointDiscoverable<in TSelf, in TOther> : IClosestEndogenousPointDiscoverable<TOther> where TOther : IClosestExogenousPointDiscoverable<TSelf>, allows ref struct where TSelf : allows ref struct;
public interface IClosestExogenousPointDiscoverable<in TOther> where TOther : allows ref struct {
	Location ClosestPointOn(TOther element);
}
public interface ILineClosestExogenousPointDiscoverable : IClosestExogenousPointDiscoverable<Line>, IClosestExogenousPointDiscoverable<Ray>, IClosestExogenousPointDiscoverable<BoundedRay>;
interface IClosestExogenousPointDiscoverable<in TSelf, in TOther> : IClosestExogenousPointDiscoverable<TOther> where TOther : IClosestEndogenousPointDiscoverable<TSelf>, allows ref struct where TSelf : allows ref struct;

public interface IClosestConvexShapePointsDiscoverable {
	Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
}
#endregion

#region Intersectable / Relatable
public interface IIntersectable<in TOther> where TOther : allows ref struct {
	bool IsIntersectedBy(TOther element);
}
public interface ILineIntersectable : IIntersectable<Line>, IIntersectable<Ray>, IIntersectable<BoundedRay>;
interface IIntersectable<in TSelf, in TOther> : IIntersectable<TOther> where TOther : IIntersectable<TSelf>, allows ref struct where TSelf : allows ref struct;
public interface IIntersectionDeterminable<in TOther, TIntersection> : IIntersectable<TOther> where TIntersection : struct where TOther : allows ref struct {
	TIntersection? IntersectionWith(TOther element);
	TIntersection FastIntersectionWith(TOther element);
}
public interface ILineIntersectionDeterminable<TIntersection> : ILineIntersectable, IIntersectionDeterminable<Line, TIntersection>, IIntersectionDeterminable<Ray, TIntersection>, IIntersectionDeterminable<BoundedRay, TIntersection> where TIntersection : struct;
interface IIntersectionDeterminable<in TSelf, in TOther, TIntersection> : IIntersectionDeterminable<TOther, TIntersection> where TOther : IIntersectionDeterminable<TSelf, TIntersection>, allows ref struct where TSelf : allows ref struct where TIntersection : struct;
public interface IRelatable<in TOther, out TRelationship> where TOther : allows ref struct {
	TRelationship RelationshipTo(TOther element);
}
public interface ILineRelatable<out TRelationship> : IRelatable<Line, TRelationship>, IRelatable<Ray, TRelationship>, IRelatable<BoundedRay, TRelationship>;
interface IRelatable<in TSelf, in TOther, out TRelationship> : IRelatable<TOther, TRelationship> where TOther : IRelatable<TSelf, TRelationship>, allows ref struct where TSelf : allows ref struct;

public interface IConvexShapeIntersectable<TIntersection> where TIntersection : struct {
	TIntersection? IntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	TIntersection FastIntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
}
#endregion

#region Dimensionality / Extents
public interface ILengthAdjustable<out TSelf> where TSelf : ILengthAdjustable<TSelf> {
	TSelf WithLength(float newLength);
	TSelf WithLengthDecreasedBy(float lengthDecrease);
	TSelf WithLengthIncreasedBy(float lengthIncrease);
	TSelf WithMaxLength(float maxLength);
	TSelf WithMinLength(float minLength);
}

public interface IPhysicalValidityDeterminable {
	bool IsPhysicallyValid { get; }
}
#endregion